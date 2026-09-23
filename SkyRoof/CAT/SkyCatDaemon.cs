using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Serilog;

namespace SkyRoof
{
  /// <summary>
  /// One line of daemon output. The sequence number is what lets a panel attaching mid-stream tell
  /// which lines its history snapshot already covers.
  /// </summary>
  public readonly record struct DaemonLine(long Sequence, string Text);

  /// <summary>
  /// Starts skycatd.exe, the CAT daemon of the SkyCAT package, together with SkyRoof and stops it
  /// again when SkyRoof closes. skycatd serves one radio on one TCP port, so a single daemon is
  /// started for the local CAT endpoint (RX if enabled, otherwise TX).
  ///
  /// Two rules keep this from fighting a daemon the user runs by hand: if the CAT port is already
  /// listening when we start, the running daemon is adopted rather than duplicated, and only a
  /// process that SkyRoof itself launched is ever stopped.
  ///
  /// Apart from the exit handler, the adoption probe and the output readers, which say so,
  /// everything here runs on the UI thread. The exit handler and the probe hand their results back
  /// to it rather than touching daemon state from a thread pool thread; the readers only add to a
  /// locked buffer and raise an event.
  /// </summary>
  public class SkyCatDaemon : IDisposable
  {
    public Context ctx = null!;

    /// <summary>The daemon SkyRoof started, or null when none is running or one was adopted.</summary>
    private Process? process;

    /// <summary>Kills <see cref="process"/> if SkyRoof dies without running its shutdown path.</summary>
    private JobObject? job;

    /// <summary>
    /// Identifies the configuration the current daemon was launched with, so that ApplySettings can
    /// tell an edit that requires a relaunch from an unrelated settings change. Set only for a
    /// daemon SkyRoof started; an adopted one carries <see cref="adoptedSignature"/> instead.
    /// </summary>
    private string launchSignature = string.Empty;

    /// <summary>A daemon was already listening, so it is being used but is not ours to stop.</summary>
    private volatile bool adopted;

    /// <summary>
    /// The settings that were in force when a daemon was adopted. Kept apart from launchSignature
    /// because an adopted daemon was started by somebody else, with a command line SkyRoof cannot
    /// see: a later edit has to be noticed and reported as not applying to it, rather than
    /// compared equal against our own launch and silently swallowed.
    /// </summary>
    private string adoptedSignature = string.Empty;

    // the endpoint an adopted daemon was actually found on, which is what its liveness has to be
    // re-checked against: the settings may since have been pointed at a different port
    private string adoptedHost = string.Empty;
    private ushort adoptedPort;
    private DateTime lastAdoptionCheck = DateTime.MinValue;
    private volatile bool adoptionCheckRunning;

    // skycatd is meant to run for as long as SkyRoof does, and a radio power-cycle kills it in the
    // middle of a pass, so an exit SkyRoof did not ask for is retried. The delay backs off, because
    // a misconfigured daemon exits immediately every time and must not be allowed to spin.
    private long launchTicks;
    private long exitTicks;
    private volatile int relaunchDelaySeconds = RELAUNCH_MIN_SECONDS;
    private volatile bool relaunchWanted;

    // whether a daemon SkyRoof started is alive. A flag rather than a HasExited probe, because the
    // exit handler disposes the Process as soon as it has drained it and every other reader holds
    // only a reference: asking a disposed Process anything throws.
    private volatile bool daemonRunning;

    // an exit is recorded here by the handler and reported by Poll, rather than marshalled to the
    // UI thread. A daemon can die while the main form is still in its constructor - a bad command
    // tail exits in milliseconds, and startup is exactly when that happens - and at that point
    // there is no window handle to post to, so a posted report is simply dropped.
    private volatile bool exitPending;
    private int pendingExitCode;
    private long pendingStartTicks;

    // set from the output stream when skycatd reports it cannot open the serial port, which is what
    // happens when the radio is switched off. It keeps retrying and binds its TCP port only once the
    // radio answers, so there is no point waiting out the startup timeout.
    private volatile bool serialPortUnavailable;

    // the SkyCAT panel is usually opened after the daemon has already started, so the output is
    // kept in a bounded buffer and replayed when a panel attaches
    private const int HISTORY_LINES = 5000;
    private const int ADOPTION_CHECK_SECONDS = 5;
    private const int PROBE_TIMEOUT_MS = 200;
    private const int KILL_TIMEOUT_MS = 3000;
    private const int RELAUNCH_MIN_SECONDS = 5;
    private const int RELAUNCH_MAX_SECONDS = 60;
    private const int STABLE_SECONDS = 60;
    private const int EXIT_DRAIN_MS = 2000;
    private const int EXIT_FLAG_SPINS = 50;
    private readonly Queue<string> history = new();
    private readonly object historyLock = new();
    private long lastSequence;

    /// <summary>
    /// The reason the daemon is not running, remembered so that a condition which is re-evaluated
    /// on every ApplySettings and every relaunch is reported once rather than on every pass.
    /// </summary>
    private string lastBlockReason = string.Empty;

    /// <summary>The unserved-radio warning last shown, so it is repeated only when it changes.</summary>
    private string lastUnservedNote = string.Empty;

    /// <summary>Raised for every output line, on whatever thread produced it.</summary>
    public event EventHandler<DaemonLine>? LineLogged;

    /// <summary>True while a daemon that SkyRoof started is alive.</summary>
    public bool IsRunning => daemonRunning;

    /// <summary>True when a daemon is listening but SkyRoof did not start it.</summary>
    public bool IsAdopted => adopted;


    //----------------------------------------------------------------------------------------------
    //                                      public methods
    //----------------------------------------------------------------------------------------------
    /// <summary>
    /// Brings the daemon in line with the current settings. Must be called before
    /// <see cref="CatControl.ApplySettings"/> so the port is listening before the engines connect.
    /// </summary>
    /// <param name="waitForPort">
    /// Block until the daemon is listening. Wanted at startup, where it saves the CAT engines a
    /// failed first connection, but not for a settings change or a status bar click, where it would
    /// freeze the UI for as long as the daemon takes. The engines reconnect on their own delay, so
    /// skipping the wait costs at most one retry interval.
    /// </param>
    internal void ApplySettings(bool waitForPort = false)
    {
      // the endpoint comes first, before any check on the executable: an adopted daemon is one the
      // user started by hand, and refusing to look at it because SkyRoof has no path configured
      // would drop the adoption on every pass and re-take it on the next probe
      if (!GetCatEndpoint(out var host, out ushort port))
      {
        ReportWhyNotRunning();
        StopOrDetach();
        return;
      }

      string signature = MakeSignature(host, port);

      // said here rather than in Start, so that it is said for an adopted daemon and for one that
      // was already running, not only for one SkyRoof launches. It reports only on a change.
      NoteUnservedRadio(host, port);

      // an adopted daemon is somebody else's process: it may have gone away since we last looked,
      // and the settings may since have been pointed at a different endpoint, so neither the flag
      // nor the endpoint it was adopted on can be taken on trust
      if (adopted)
      {
        if (host != adoptedHost || port != adoptedPort)
          ForgetAdoption($"the CAT endpoint is now {host}:{port}, the adopted daemon serves " +
            $"{adoptedHost}:{adoptedPort}");
        else if (!IsPortOpen(adoptedHost, adoptedPort, PROBE_TIMEOUT_MS))
          ForgetAdoption("the adopted daemon is no longer listening");
      }

      if (adopted)
      {
        lastBlockReason = string.Empty;

        // its command line is whatever somebody else launched it with, so a settings edit changes
        // nothing until that daemon is stopped. Saying so is the point of this branch: the
        // alternative is an edit that looks accepted and is then silently ignored forever.
        if (signature != adoptedSignature)
        {
          Log.Warning($"skycatd.exe on {adoptedHost}:{adoptedPort} was started outside SkyRoof, " +
            "the changed settings do not apply to it");
          Note("the running daemon was started outside SkyRoof - the changed settings do NOT " +
            "apply until that daemon is stopped");
          adoptedSignature = signature;
        }
        return;
      }

      if (IsRunning && signature == launchSignature)
      {
        // nothing to relaunch, but Stop On Exit may still have changed, and the second radio of a
        lastBlockReason = string.Empty;
        SyncJobObject();
        return;
      }

      // checked before stopping anything: an unlaunchable configuration is no reason to kill a
      // daemon that is running perfectly well on the previous one
      if (!CanLaunch())
      {
        // the CAT engines are rebuilt against the new settings straight after this returns, so a
        // daemon left running on the old ones is a mismatch the user has to be told about; "not
        // starting" on its own reads as though nothing had changed
        if (IsRunning)
          Note("the running daemon still serves the previous settings - the change is NOT in effect");
        else
          // an executable can come back - a network share reconnects, an installer finishes - and
          // this is reached only when the endpoint says a daemon is wanted, so the retry stops of
          // its own accord when the user turns CAT or Auto Start off
          RetryLater();

        return;
      }

      // a daemon that will not die still owns the CAT port, and starting now would only adopt it
      if (!Stop()) return;

      Start(host, port, signature, waitForPort);
    }

    /// <summary>
    /// Asks <see cref="Poll"/> to start a daemon once the backoff has elapsed. The caller sets
    /// <see cref="relaunchDelaySeconds"/> first if it wants to change the delay.
    /// </summary>
    private void ArmRelaunch()
    {
      Volatile.Write(ref exitTicks, Environment.TickCount64);
      relaunchWanted = true;
    }

    /// <summary>
    /// Backs the delay off one step and asks <see cref="Poll"/> to try again. Called by whichever
    /// step declined to start a daemon, so that exactly one owner accounts for each failure.
    /// </summary>
    private void RetryLater()
    {
      relaunchDelaySeconds = Math.Min(RELAUNCH_MAX_SECONDS, relaunchDelaySeconds * 2);
      ArmRelaunch();
    }

    /// <summary>Drops an adopted daemon from view, whatever it is now doing.</summary>
    private void ForgetAdoption(string reason)
    {
      Log.Information($"Forgetting the adopted skycatd.exe: {reason}");
      Note(reason);

      adopted = false;
      adoptedSignature = string.Empty;
      adoptedHost = string.Empty;
      adoptedPort = 0;
    }

    /// <summary>
    /// Called once a second by the main form. Restarts a daemon that died on its own and keeps the
    /// adopted state fresh; both have to happen whether or not the SkyCAT panel is open, and
    /// neither can be done from the exit handler, which runs on a thread pool thread.
    /// </summary>
    internal void Poll()
    {
      if (exitPending)
      {
        exitPending = false;
        OnDaemonExited(pendingExitCode, Volatile.Read(ref pendingStartTicks));
      }

      // a due relaunch is attempted, but the adoption probe runs either way. Returning early here
      // meant a wish that could not be satisfied stopped SkyRoof ever noticing a daemon the user
      // started by hand, for the rest of the session.
      if (relaunchWanted &&
        Environment.TickCount64 - Volatile.Read(ref exitTicks) >= relaunchDelaySeconds * 1000L)
      {
        relaunchWanted = false;

        // nothing is announced here. Start logs the launch it actually performs, and a
        // configuration that cannot launch has already been reported once by DoNotRun; saying
        // "restarting" on every attempt wrote a log and a panel line a minute, forever.
        // Whoever declines to start also owns arming the next attempt, so there is no re-arm
        // here either - two owners doubled the backoff twice for a single failure.
        ApplySettings();
      }

      RefreshAdoptionState();
    }

    /// <summary>
    /// Brings the job object in line with Stop On Exit without disturbing a running daemon. Both
    /// directions are honored immediately: a process can join a job at any time, and while it can
    /// never leave one, the job's kill-on-close limit - which is the whole of what this setting
    /// controls - can be lifted under it.
    /// </summary>
    private void SyncJobObject()
    {
      // the flag first, then a single read of the field: Process_Exited nulls it from a thread pool
      // thread, and re-reading it below would dereference null while reporting a job object failure
      if (!daemonRunning) return;

      var running = process;
      if (running == null) return;

      bool wanted = ctx.Settings.Cat.SkyCat.StopOnExit;

      if (wanted && job == null)
      {
        try
        {
          job = new JobObject();
          job.Assign(running);
          Log.Information("Stop On Exit enabled, the running skycatd.exe joined the job object");
          Note("Stop On Exit enabled, it now applies to the running daemon");
        }
        catch (Exception ex)
        {
          // the daemon can die between the check above and here, which disposes the Process and
          // makes this throw. That is a race, not a failure worth an error line.
          if (daemonRunning) Log.Error(ex, "Failed to put the running skycatd.exe in a job object");
          else Log.Debug(ex, "skycatd.exe exited while it was being put in a job object");
        }
      }
      else if (!wanted && job != null)
      {
        // the process stays in the job forever, but with kill-on-close gone the job no longer has
        // any hold on it, so the handle can be closed and the daemon outlives SkyRoof as asked
        if (job.Release())
        {
          job.Dispose();
          job = null;
          Log.Information("Stop On Exit disabled, the running skycatd.exe is no longer held by the " +
            "job object");
          Note("Stop On Exit disabled, it now applies to the running daemon");
        }
        else
        {
          // said rather than swallowed: the daemon will still be stopped, and a user who was told
          // the setting had been applied would find it gone anyway
          Log.Warning("Stop On Exit disabled, but the running skycatd.exe is still held by the job " +
            "object and will still be stopped; this takes effect the next time it is started");
          Note("Stop On Exit disabled, but the running daemon will still be stopped - this takes " +
            "effect the next time the daemon is started");
        }
      }
    }

    /// <summary>
    /// Re-checks whether an adopted daemon is still there. Adoption is somebody else's process, so
    /// nothing tells us when it goes away. A connected CAT engine answers the question for free; only
    /// when there is no such engine is the port probed, at most every few seconds and off the UI
    /// thread.
    /// </summary>
    internal void RefreshAdoptionState()
    {
      // our own process reports its own state, so there is nothing to probe for
      if (IsRunning || adoptionCheckRunning) return;

      // the endpoint is read here, on the calling thread, so the probe never touches the settings
      if (!GetCatEndpoint(out string host, out ushort port))
      {
        // CAT was switched off while a daemon was adopted; nothing will probe for it again, so
        // clearing it here is the only thing that stops the panel reporting it forever
        if (adopted) ForgetAdoption("CAT control is disabled");
        return;
      }

      // a CAT engine with a live connection to that endpoint has already answered what a probe would
      // ask, and it goes on answering it for nothing. Probing anyway opened a TCP connection every few
      // seconds for as long as the adoption lasted, and skycatd records each one as a client arriving
      // and leaving - twelve pairs a minute in somebody's log, to learn what the open connection was
      // saying continuously.
      //
      // The engine may only vouch for an adoption already held, never create one. Its IsRunning goes
      // on saying "connected" until a command actually fails, which is up to the receive timeout -
      // seconds, while this runs every second. Just after a daemon of ours exits there is therefore a
      // window where the engine still claims a connection to a process that is gone, and taking that
      // as "a daemon is listening" would adopt the corpse: the panel would report a daemon started
      // outside SkyRoof, and losing that adoption again resets the relaunch delay to its floor, so a
      // daemon that crash-loops would be retried at 5 s for ever instead of backing off.
      if (adopted && IsCatConnectedTo(host, port))
      {
        lastAdoptionCheck = DateTime.UtcNow;
        return;
      }

      if ((DateTime.UtcNow - lastAdoptionCheck).TotalSeconds < ADOPTION_CHECK_SECONDS) return;

      adoptionCheckRunning = true;

      Task.Run(() =>
      {
        bool listening = false;
        bool probed = false;

        try
        {
          listening = IsPortOpen(host, port, PROBE_TIMEOUT_MS);
          probed = true;
        }
        catch (Exception ex)
        {
          Log.Error(ex, "Failed to re-check the adopted skycatd.exe");
        }
        finally
        {
          lastAdoptionCheck = DateTime.UtcNow;

          // a probe that threw learned nothing, and must not be read as "nothing is listening":
          // that would drop a working adoption on the strength of a failure to look at it. The
          // finally runs either way, so a result is applied only when there is one.
          //
          // The result is posted to the UI thread rather than applied here, so releasing the guard
          // does not wait for it to be acted on. That is safe because the posted action re-checks
          // IsRunning and the endpoint before it touches anything; the guard's only job is to keep
          // one probe in flight at a time.
          if (probed) ApplyProbeResult(host, port, listening);
          adoptionCheckRunning = false;
        }
      });
    }

    /// <summary>
    /// Runs an action on the UI thread, where Start and Stop also run, or drops it when there is no
    /// longer a form to run it on. Every hand-off from a background thread goes through here.
    /// </summary>
    private void PostToUi(Action action)
    {
      var form = ctx.MainForm;
      if (form == null || form.IsDisposed || !form.IsHandleCreated) return;

      try
      {
        form.BeginInvoke(action);
      }
      catch (Exception ex)
      {
        // the form can close between the check above and the call; there is nothing left to update
        Log.Debug(ex, "Could not post a skycatd.exe update to the UI thread");
      }
    }

    /// <summary>
    /// Hands a probe result back to the UI thread. Applying it on the thread pool would let a probe
    /// that finished after a relaunch overwrite the state that relaunch had just set, which is how
    /// an adopted flag came to survive the daemon it described.
    /// </summary>
    private void ApplyProbeResult(string host, ushort port, bool listening)
    {
      PostToUi(() =>
        {
          // the probe took time, and the world may have moved on while it ran: a daemon of our own
          // may have started, or the settings may now point somewhere else entirely
          if (IsRunning) return;
          if (!GetCatEndpoint(out string now, out ushort nowPort)) return;
          if (now != host || nowPort != port) return;

          if (adopted && !listening)
          {
            ForgetAdoption("the adopted daemon is no longer listening");

            // losing an adopted daemon leaves the same hole as one of our own exiting, and Auto
            // Start promises to fill it. Without this the panel simply read "Not running" for the
            // rest of the session while a perfectly good executable sat configured and unused.
            relaunchDelaySeconds = RELAUNCH_MIN_SECONDS;
            ArmRelaunch();
          }
          else if (!adopted && listening)
          {
            // somebody started a daemon while we were not looking; take it as adopted so the panel
            // stops saying nothing is running. ApplySettings re-derives the rest when it next runs.
            Log.Information($"A skycatd.exe is listening on {host}:{port}, adopting it");
            Note($"a daemon is listening on {host}:{port}, adopting it");
            adoptedHost = host;
            adoptedPort = port;

            // the real signature, not an empty one: leaving it blank made the next ApplySettings
            // compare unequal and warn that the user's settings did not apply, about a daemon the
            // settings had never governed
            adoptedSignature = MakeSignature(host, port);
            adopted = true;
          }
        });
    }

    /// <summary>Called when SkyRoof closes. Honors the Stop On Exit setting.</summary>
    internal void Shutdown()
    {
      StopOrDetach();
    }

    /// <summary>
    /// Stops the daemon, or leaves it running when Stop On Exit says the user wants it kept.
    /// Turning Auto Start off, or switching CAT off, is not a request to kill a daemon the user
    /// asked to outlive SkyRoof itself, so both go through here rather than straight to Stop.
    /// </summary>
    private void StopOrDetach()
    {
      if (IsRunning && !ctx.Settings.Cat.SkyCat.StopOnExit)
      {
        Log.Information("Leaving skycatd.exe running, Stop On Exit is disabled");
        Note("leaving the daemon running, Stop On Exit is disabled");
        Detach();
        return;
      }

      // an adopted daemon is left alone whatever Stop On Exit says, because it is not ours to
      // stop. That is easy to mistake for the setting being ignored, so say which rule applied.
      if (adopted)
      {
        Log.Information($"Leaving the skycatd.exe on {adoptedHost}:{adoptedPort} running, " +
          "SkyRoof did not start it");
        Note("leaving the daemon running, SkyRoof did not start it");
      }

      Stop();
    }

    /// <summary>
    /// The same as <see cref="Shutdown"/>, and deliberately so. Nothing disposes the daemon today -
    /// it lives as long as the Context - and a Dispose that stopped it unconditionally would be a
    /// trap for the day something does: a daemon the user asked to outlive SkyRoof would start
    /// dying with it, with nothing in the code saying why.
    /// </summary>
    public void Dispose()
    {
      Shutdown();
    }


    //----------------------------------------------------------------------------------------------
    //                                       start / stop
    //----------------------------------------------------------------------------------------------
    /// <summary>Whether the configured executable is something that can actually be launched.</summary>
    private bool CanLaunch()
    {
      string path = ctx.Settings.Cat.SkyCat.ExePath;

      if (string.IsNullOrWhiteSpace(path))
        return DoNotRun("no executable path is configured", true);

      if (!File.Exists(path))
        return DoNotRun($"{path} does not exist", true);

      return true;
    }

    /// <summary>
    /// Explains an endpoint that yields no daemon. Auto Start being off is a deliberate choice
    /// rather than a misconfiguration, and the panel already says so, so only the two cases that
    /// look like a malfunction are reported.
    /// </summary>
    private void ReportWhyNotRunning()
    {
      var cat = ctx.Settings.Cat;

      if (!cat.SkyCat.Enabled)
      {
        lastBlockReason = string.Empty;
        return;
      }

      // mirrors GetCatEndpoint: a daemon is wanted only for an enabled radio on this machine, so
      // the two reasons it can decline are "no radio at all" and "no radio here"
      var enabled = new[] { cat.RxCat, cat.TxCat }.Where(radio => radio.Enabled).ToList();

      if (enabled.Count == 0)
        DoNotRun("CAT control is disabled", false);
      else
        DoNotRun("no CAT radio is on this computer, the CAT host is " +
          string.Join(" and ", enabled.Select(radio => radio.Host).Distinct()), false);
    }

    /// <summary>
    /// Reports why no daemon is running, once per reason. These conditions are re-tested on every
    /// ApplySettings and on every relaunch attempt, and an unconfigured executable would otherwise
    /// write the same line to the log and the panel for as long as SkyRoof is open.
    /// </summary>
    private bool DoNotRun(string reason, bool warning)
    {
      if (reason != lastBlockReason)
      {
        if (warning) Log.Warning($"Not starting skycatd.exe, {reason}");
        else Log.Information($"Not starting skycatd.exe, {reason}");

        Note($"not starting: {reason}");
        lastBlockReason = reason;
      }

      return false;
    }

    /// <summary>
    /// Points out a second radio that this daemon does not serve. skycatd serves one radio on one
    /// TCP port, so a two-radio setup needs a second daemon started by hand; without this the only
    /// symptom is a CAT indicator that never goes green.
    /// </summary>
    private void NoteUnservedRadio(string host, ushort port)
    {
      var cat = ctx.Settings.Cat;

      var unservedRadios = new List<string>();

      foreach (var (name, radio) in new[] { ("RX", cat.RxCat), ("TX", cat.TxCat) })
      {
        if (!radio.Enabled || (radio.Host == host && radio.Port == port)) continue;

        // a radio on another computer has its own daemon there, started by whoever runs that
        // machine. Naming it here told the user to start a second daemon for a rig that was never
        // this daemon's to serve, which is advice, not information.
        if (!IsLocalHost(radio.Host)) continue;

        unservedRadios.Add($"{name} CAT on {radio.Host}:{radio.Port}");
      }

      string unserved = string.Join(" and ", unservedRadios);

      // this is re-tested on every ApplySettings, so it is reported only when the answer changes
      if (unserved == lastUnservedNote) return;
      lastUnservedNote = unserved;

      if (unserved.Length == 0) return;

      Log.Warning($"{unserved} is not served by this daemon");
      Note($"{unserved} is NOT served by this daemon - skycatd serves one radio on one port, " +
        "a second radio needs a second daemon");
    }

    /// <summary>Everything that, when changed, means the running daemon has to be replaced.</summary>
    private string MakeSignature(string host, ushort port)
    {
      var settings = ctx.Settings.Cat.SkyCat;

      // StopOnExit is deliberately absent: it is handled by SyncJobObject without a relaunch, so
      // including it here would kill and restart a working daemon just to change a shutdown policy
      return $"{settings.ExePath}|{settings.Arguments}|{host}|{port}";
    }

    /// <summary>
    /// Adopts a daemon already serving <paramref name="host"/>:<paramref name="port"/>, or launches
    /// one and waits for it to start listening. Failure is logged, never thrown: CAT control retries
    /// on its own schedule, so a daemon that will not start must not hold up the rest of startup.
    /// </summary>
    private void Start(string host, ushort port, string signature, bool waitForPort)
    {
      var settings = ctx.Settings.Cat.SkyCat;

      // everything checked out, so whatever last stopped the daemon from running is stale
      lastBlockReason = string.Empty;

      // somebody is already serving this port - use it instead of starting a second daemon
      if (IsPortOpen(host, port, PROBE_TIMEOUT_MS))
      {
        Log.Information($"skycatd.exe not started, {host}:{port} is already listening");
        Note($"{host}:{port} is already listening, using the running daemon");

        // the output of a daemon somebody else started belongs to whoever started it and cannot be
        // redirected after the fact, so nothing more will appear here. Without saying so, a panel
        // that shows one line and then nothing for the rest of the session reads as broken.
        Note("its output cannot be shown here - it belongs to whoever started the daemon; " +
          "SkyRoof will report only whether it is still listening");
        adopted = true;
        adoptedHost = host;
        adoptedPort = port;
        adoptedSignature = signature;
        lastAdoptionCheck = DateTime.UtcNow;
        return;
      }

      serialPortUnavailable = false;
      string arguments = BuildArguments(settings.Arguments, port);
      Log.Information($"Starting skycatd.exe: \"{settings.ExePath}\" {arguments}");
      Note($"starting \"{settings.ExePath}\" {arguments}");

      var info = new ProcessStartInfo(settings.ExePath, arguments)
      {
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        WorkingDirectory = Path.GetDirectoryName(settings.ExePath) ?? string.Empty
      };

      try
      {
        // everything below works through this local. Once EnableRaisingEvents is set, Process_Exited
        // can null the field and dispose the object from a thread pool thread, and re-reading the
        // field after that point threw a NullReferenceException reported as "FAILED to start".
        var started = Process.Start(info) ?? throw new Exception("Process.Start returned null");

        // the state a daemon needs is published before its exit can be observed, so that a handler
        // firing immediately sees a consistent picture and stamps the backoff against this launch
        process = started;
        daemonRunning = true;
        launchSignature = signature;
        Volatile.Write(ref launchTicks, Environment.TickCount64);

        // the job object is what makes the daemon go away even when SkyRoof is killed rather than
        // closed. It is only used when Stop On Exit is set, because a job cannot be left behind.
        if (settings.StopOnExit)
        {
          job = new JobObject();
          job.Assign(started);
        }

        started.OutputDataReceived += (s, e) => LogDaemonOutput(e.Data, false);
        started.ErrorDataReceived += (s, e) => LogDaemonOutput(e.Data, true);
        started.BeginOutputReadLine();
        started.BeginErrorReadLine();

        // subscribed before the events are enabled, and enabled last of all. A daemon that fails
        // immediately - a bad model or serial port in the command tail - can exit at any point in
        // here, and .NET raises Exited once and only once, so a handler attached afterwards is
        // never called and the failure goes unreported.
        started.Exited += Process_Exited;
        started.EnableRaisingEvents = true;
      }
      catch (Exception ex)
      {
        // the process may already be running - Process.Start succeeds before the job object is
        // created - so tear it down rather than dropping the handle and orphaning the daemon
        Log.Error(ex, $"Failed to start skycatd.exe from {settings.ExePath}");
        Note($"FAILED to start: {ex.Message}");

        // Stop clears the pending relaunch, so the retry is armed after it. Without this, a launch
        // that threw on the settings-dialog or status-bar path left nothing scheduled at all. A
        // Stop that fails arms its own retry, so arming a second one here would back the delay off
        // twice for one failure.
        if (Stop()) RetryLater();
        return;
      }

      if (waitForPort) WaitForPort(host, port, settings.StartupTimeout);
    }

    /// <summary>
    /// Builds the command line, always ending in the CAT port. Any port option in the user's command
    /// tail is dropped first: the daemon has to serve the port the CAT engines connect to, and letting
    /// the two disagree gives a daemon that runs correctly and a CAT connection that never arrives.
    /// skycatd spells the option four ways - "-t 4532", "-t4532", "--port 4532" and "--port=4532" -
    /// and accepts all of them, so all four have to be recognized here.
    /// </summary>
    private string BuildArguments(string commandTail, ushort port)
    {
      var separators = new[] { ' ', '\t' };
      var tokens = (commandTail ?? string.Empty)
        .Split(separators, StringSplitOptions.RemoveEmptyEntries).ToList();

      var dropped = new List<string>();

      for (int i = 0; i < tokens.Count; )
      {
        string token = tokens[i];
        int count;

        if (token == "-t" || token == "--port")
          // the value is the next token, unless the user left the option dangling
          count = i + 1 < tokens.Count && !tokens[i + 1].StartsWith('-') ? 2 : 1;
        else if (token.StartsWith("--port=") ||
                 (token.StartsWith("-t") && token.Length > 2 && token[2..].All(char.IsDigit)))
          count = 1;
        else
        {
          i++;
          continue;
        }

        dropped.Add(string.Join(" ", tokens.GetRange(i, count)));
        tokens.RemoveRange(i, count);
      }

      if (dropped.Count > 0)
      {
        string what = string.Join(", ", dropped.Select(d => $"'{d}'"));
        Log.Information($"Ignoring {what} in the command tail, using the CAT port {port}");
        Note($"ignoring {what} in the command tail, using the CAT port {port}");
      }

      tokens.Add("-t");
      tokens.Add(port.ToString());
      return string.Join(" ", tokens);
    }

    /// <summary>
    /// Blocks until the daemon accepts connections, giving CAT control a listening port to connect
    /// to on its first attempt. Returns early when the daemon exits or reports that it cannot open
    /// the serial port, since neither will resolve within the timeout.
    /// </summary>
    private void WaitForPort(string host, ushort port, int timeoutMs)
    {
      // asking for no wait is not the same as waiting and finding nothing: without this the loop
      // below is skipped and the "did not start listening" warning is reported for a daemon that
      // was never given a chance to answer
      if (timeoutMs <= 0) return;

      var stopwatch = Stopwatch.StartNew();

      while (stopwatch.ElapsedMilliseconds < timeoutMs)
      {
        // the exit is observed through the flag, never through the Process object. This loop used
        // to hold a reference to it, which the exit handler then disposed underneath us - and a
        // daemon that dies this fast is exactly the case this loop exists to catch.
        if (!daemonRunning)
        {
          // the handler publishes daemonRunning before exitPending, so the flag may not have landed
          // yet. It is waited for rather than simply cleared, because clearing a flag that arrives a
          // moment later leaves it set - and Poll then reports the same death a second time, as
          // "unexpectedly", and backs the delay off again for it. Only Process_Exited can clear
          // daemonRunning while this loop holds the UI thread, so the wait ends; the bound is there
          // in case that ever stops being true.
          for (int i = 0; i < EXIT_FLAG_SPINS && !exitPending; i++) Thread.Sleep(1);

          // the exit is consumed here rather than left for Poll
          exitPending = false;
          Log.Error($"skycatd.exe exited during startup with code {pendingExitCode}");
          Note($"EXITED during startup with code {pendingExitCode}");

          OnDaemonExited(pendingExitCode, Volatile.Read(ref pendingStartTicks), report: false);
          return;
        }

        // the radio is off or the port is taken: skycatd stays up and retries, and opens its TCP
        // port as soon as the radio answers, so CAT control recovers on its own once it is on
        if (serialPortUnavailable)
        {
          Log.Warning("skycatd.exe cannot open the serial port, waiting for the radio");
          Note("cannot open the serial port - is the radio switched on? skycatd will keep retrying");
          return;
        }

        if (IsPortOpen(host, port, 200))
        {
          Log.Information($"skycatd.exe is listening on {host}:{port}");
          Note($"listening on {host}:{port}");
          return;
        }

        Thread.Sleep(100);
      }

      // not fatal: the control engine keeps retrying on its reconnect delay
      Log.Warning($"skycatd.exe did not start listening on {host}:{port} within {timeoutMs} ms");
      Note($"NOT listening on {host}:{port} after {timeoutMs} ms, CAT control will keep retrying");
    }

    /// <summary>
    /// Stops a daemon that SkyRoof started and forgets an adopted one. Safe to call when nothing is
    /// running, and it never touches a process SkyRoof did not launch.
    /// </summary>
    private bool Stop()
    {
      adopted = false;
      adoptedSignature = string.Empty;
      adoptedHost = string.Empty;
      adoptedPort = 0;
      launchSignature = string.Empty;

      // whoever takes the field owns the process: Process_Exited may be disposing it right now.
      // The pending relaunch is dropped only after the exchange - clearing it first let a handler
      // that won the race re-arm it afterwards and resurrect a daemon the user had just stopped.
      var stopping = Interlocked.Exchange(ref process, null);
      daemonRunning = false;
      relaunchWanted = false;

      // a recorded exit belongs to the daemon being stopped. Leaving it set had Poll report
      // "EXITED unexpectedly" and schedule a relaunch for a daemon the user had just switched off.
      exitPending = false;

      if (stopping == null)
      {
        job?.Dispose();
        job = null;
        return true;
      }

      stopping.Exited -= Process_Exited;

      try
      {
        if (!stopping.HasExited)
        {
          // skycatd is a console application, so there is no window to close politely
          Log.Information("Stopping skycatd.exe");
          Note("stopping");
          stopping.Kill(entireProcessTree: true);

          if (!stopping.WaitForExit(KILL_TIMEOUT_MS))
          {
            Log.Error($"skycatd.exe did not exit within {KILL_TIMEOUT_MS} ms");
            Note($"did NOT exit within {KILL_TIMEOUT_MS} ms, it will be stopped again shortly");
            KeepUnstopped(stopping);
            return false;
          }
        }
      }
      catch (Exception ex)
      {
        // Kill itself can fail - access denied on a process already tearing down, or a tree that
        // only partly died. Falling through to "stopped" here is what let the next Start find the
        // survivor still holding the CAT port and adopt SkyRoof's own zombie.
        Log.Error(ex, "Failed to stop skycatd.exe");

        try
        {
          if (!stopping.HasExited)
          {
            Note($"could NOT be stopped: {ex.Message}");
            KeepUnstopped(stopping);
            return false;
          }
        }
        catch (Exception check)
        {
          // unknown counts as alive: reporting a successful stop here would let the next Start find
          // the survivor still holding the CAT port and adopt SkyRoof's own zombie
          Log.Error(check, "Could not determine whether skycatd.exe is still running");
          Note("could NOT be confirmed stopped, treating it as still running");
          KeepUnstopped(stopping);
          return false;
        }
      }

      // the readers are cancelled before the handle goes: WaitForExit(int) does not wait for the
      // redirected output to finish, so without this a killed daemon's last lines can still arrive
      try
      {
        stopping.CancelOutputRead();
        stopping.CancelErrorRead();
      }
      catch (Exception ex)
      {
        Log.Debug(ex, "Could not cancel the skycatd.exe output readers");
      }

      stopping.Dispose();
      job?.Dispose();
      job = null;
      return true;
    }

    /// <summary>
    /// Puts a daemon that would not die back where it was. The handle is kept, because dropping it
    /// leaves a process that still owns the CAT port and no way to ever stop it again, and the exit
    /// subscription is restored so that the eventual death is still noticed and cleaned up. A retry
    /// is armed, because otherwise "it will be stopped again" is a promise nothing keeps.
    /// </summary>
    private void KeepUnstopped(Process stopping)
    {
      // published before the subscription, the same order Start uses. The other way round, an exit
      // landing in between found a null field, failed its compare-exchange and recorded nothing -
      // and Exited is raised once and only once, so re-subscribing could not recover it.
      daemonRunning = true;
      Interlocked.Exchange(ref process, stopping);
      stopping.Exited += Process_Exited;

      // it may already have died inside the window above, where the one-shot event is spent. The
      // handler is safe to run twice: whoever wins the compare-exchange owns the process.
      try
      {
        if (stopping.HasExited) Process_Exited(stopping, EventArgs.Empty);
      }
      catch (Exception ex)
      {
        Log.Debug(ex, "Could not re-check the skycatd.exe that would not stop");
      }

      // armed last, and only when the exit path has not taken it over: a recorded exit becomes a
      // backoff step of its own in Poll, and arming one here as well backed the delay off twice for
      // a single failure. Stop cleared this flag on the way in, so it is ours to read.
      if (!exitPending) RetryLater();
    }

    /// <summary>Gives up ownership of the daemon without killing it, for Stop On Exit = false.</summary>
    private void Detach()
    {
      var detaching = Interlocked.Exchange(ref process, null);
      daemonRunning = false;
      relaunchWanted = false;
      exitPending = false;

      adopted = false;
      adoptedSignature = string.Empty;
      adoptedHost = string.Empty;
      adoptedPort = 0;
      launchSignature = string.Empty;

      if (detaching != null)
      {
        detaching.Exited -= Process_Exited;
        detaching.Dispose();
      }

      if (job != null)
      {
        // normally SyncJobObject has already lifted this, when the setting was switched off. This is
        // the path where that did not happen - or did not work - and the job still has kill-on-close
        // set, which would take the daemon down with SkyRoof despite Stop On Exit being off.
        if (!job.Release())
          Log.Warning("skycatd.exe is still held by the job object and will still be stopped");

        job.Dispose();
        job = null;
      }
    }

    /// <summary>Notes a daemon that died on its own, so the next ApplySettings starts a fresh one.</summary>
    private void Process_Exited(object? sender, EventArgs e)
    {
      // this runs on a thread pool thread, where an escaping exception takes the application down
      // with it. Stop and Detach dispose the very process being read here, and they can be racing
      // this handler, so reading ExitCode is not safe on its own.
      Process? owned = null;

      try
      {
        if (sender is not Process exited) return;

        // whoever nulls the field owns the process. If Stop got there first it is already tearing
        // this one down, and there is nothing left here to report or clean up.
        if (Interlocked.CompareExchange(ref process, null, exited) != exited) return;

        owned = exited;

        int exitCode;
        try { exitCode = exited.ExitCode; } catch { exitCode = -1; }
        long started = Volatile.Read(ref launchTicks);

        // the reporting and the relaunch bookkeeping belong on the UI thread, where Start and Stop
        // also run: doing them here raced a relaunch that had already happened, blanking the
        // launchSignature of a healthy new daemon so that the next ApplySettings killed and rebuilt
        // it for nothing. Poll collects this on its next tick; the data is published before the
        // flag, so a reader that sees the flag sees the rest.
        pendingExitCode = exitCode;
        Volatile.Write(ref pendingStartTicks, started);

        // the two flags are written last, and after the exit code: WaitForPort reports that code
        // the moment it sees daemonRunning go false, so publishing the flag first handed it the
        // code from the previous exit
        daemonRunning = false;
        exitPending = true;
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Failed to handle the exit of skycatd.exe");
      }
      finally
      {
        if (owned != null) DrainAndDispose(owned);
      }
    }

    /// <summary>Reports an exit and asks for a replacement. Runs on the UI thread.</summary>
    /// <param name="report">
    /// False when the caller has already told the user, which WaitForPort does for an exit during
    /// startup. The relaunch bookkeeping still has to happen either way.
    /// </param>
    private void OnDaemonExited(int exitCode, long startedTicks, bool report = true)
    {
      // a replacement may already be running by the time this arrives, in which case the exit is
      // old news and none of the state below is ours to touch
      if (IsRunning) return;

      launchSignature = string.Empty;

      if (report)
      {
        Log.Warning($"skycatd.exe exited unexpectedly with code {exitCode}");
        Note($"EXITED unexpectedly with code {exitCode}");
      }

      // a daemon that ran for a while and then died is worth restarting straight away; one that
      // dies on startup is misconfigured, and retrying that every few seconds only fills the log
      bool wasStable = startedTicks != 0 &&
        Environment.TickCount64 - startedTicks >= STABLE_SECONDS * 1000L;

      relaunchDelaySeconds = wasStable
        ? RELAUNCH_MIN_SECONDS
        : Math.Min(RELAUNCH_MAX_SECONDS, relaunchDelaySeconds * 2);

      ArmRelaunch();
      Note($"restarting in {relaunchDelaySeconds} s");
    }

    /// <summary>
    /// Lets the async readers finish, then releases the handle. The wait is bounded: the
    /// parameterless WaitForExit is the overload that drains the readers, but it drains them by
    /// waiting for EOF on the redirected pipes, and a grandchild that outlives the daemon holds
    /// those open indefinitely - which would strand a thread pool thread for the session.
    /// </summary>
    private static void DrainAndDispose(Process owned)
    {
      // nothing may escape: this is called from the exit handler's finally, on a thread pool
      // thread, where an unhandled exception takes the application down
      try
      {
        // WaitForExitAsync also waits for the redirected readers to reach EOF, which is what lets
        // the daemon's last words reach the panel. A grandchild that outlives the daemon holds
        // those pipes open indefinitely, so the wait is bounded by the token.
        var timeout = new CancellationTokenSource(EXIT_DRAIN_MS);

        owned.WaitForExitAsync(timeout.Token).ContinueWith(_ =>
        {
          // the handle is released only once the wait has finished, one way or the other. The
          // blocking version disposed it while a thread was still inside WaitForExit, which left
          // that thread stranded on a pipe that would never reach EOF.
          try { owned.Dispose(); }
          catch (Exception ex) { Log.Debug(ex, "Failed to release the skycatd.exe handle"); }
          finally { timeout.Dispose(); }
        }, TaskScheduler.Default);
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Failed to release the skycatd.exe handle");
      }
    }

    /// <summary>
    /// Receives one line of daemon output on a background thread. The line always reaches the panel;
    /// it reaches the SkyRoof log only when Log Output is set. A serial port failure is noted either
    /// way, once, because it changes how startup behaves.
    /// </summary>
    private void LogDaemonOutput(string? line, bool isError)
    {
      if (string.IsNullOrWhiteSpace(line)) return;

      // the panel is an in-memory ring buffer and costs nothing, but the SkyRoof log rolls at 3 MB
      // and a verbose daemon evicts everything else from it, so copying there is opt-in
      if (ctx.Settings.Cat.SkyCat.LogOutput)
      {
        if (isError) Log.Warning($"skycatd: {line}");
        else Log.Information($"skycatd: {line}");
      }

      if (line.Contains("Failed to open serial port", StringComparison.OrdinalIgnoreCase))
      {
        if (!serialPortUnavailable) Log.Warning($"skycatd: {line}");
        serialPortUnavailable = true;
      }

      Emit(line);
    }

    /// <summary>
    /// A snapshot of the buffered output, oldest line first, together with the sequence number of
    /// its last line. A panel subscribes first and then calls this: lines that arrive in between
    /// appear both in the snapshot and at the handler, and the sequence number is what lets the
    /// panel drop the duplicates instead of losing or repeating them.
    /// </summary>
    internal string[] GetHistory(out long throughSequence)
    {
      lock (historyLock)
      {
        throughSequence = lastSequence;
        return history.ToArray();
      }
    }

    /// <summary>Empties the buffer, so a panel opened later starts from the current moment.</summary>
    internal void ClearHistory()
    {
      lock (historyLock) history.Clear();
    }

    /// <summary>
    /// Publishes one of SkyRoof's own lifecycle messages, marked with "==" so it stands apart from
    /// the daemon's output in the panel.
    /// </summary>
    private void Note(string message)
    {
      Emit($"== {message}");
    }

    /// <summary>Timestamps a line, adds it to the buffer and hands it to any attached panel.</summary>
    private void Emit(string line)
    {
      string stamped = $"{DateTime.Now:HH:mm:ss.fff}  {line}";
      long sequence;

      lock (historyLock)
      {
        sequence = ++lastSequence;
        history.Enqueue(stamped);
        while (history.Count > HISTORY_LINES) history.Dequeue();
      }

      // deliberately outside the lock: a slow subscriber must not stall the daemon's reader thread
      LineLogged?.Invoke(this, new DaemonLine(sequence, stamped));
    }


    //----------------------------------------------------------------------------------------------
    //                                         helpers
    //----------------------------------------------------------------------------------------------
    /// <summary>
    /// The local CAT endpoint a daemon would serve. It says nothing about whether one can be
    /// launched there, which is <see cref="CanLaunch"/>, and it logs nothing, so it is safe to
    /// call repeatedly from a timer.
    /// </summary>
    private bool GetCatEndpoint(out string host, out ushort port)
    {
      host = string.Empty;
      port = 0;

      var cat = ctx.Settings.Cat;
      if (!cat.SkyCat.Enabled) return false;

      // RX first, as the radio SkyRoof spends the pass talking to, but only a radio on this machine
      // can be served by a daemon started here. An RX radio on a remote rigctld must therefore not
      // hide a TX radio on this one: that split is a real configuration - an SDR and a networked rig
      // for receive, the local transceiver for transmit - and taking RX unconditionally left Auto
      // Start declining to start the daemon the local radio needed.
      var radio = LocalRadio(cat.RxCat) ?? LocalRadio(cat.TxCat);
      if (radio == null) return false;

      host = radio.Host;
      port = radio.Port;
      return true;
    }

    /// <summary>
    /// Whether a CAT engine currently holds a connection to that endpoint. The engine's connection is
    /// proof the daemon is alive that costs nothing to read: the daemon cannot have gone away while
    /// SkyRoof is exchanging commands with it over that very port.
    /// </summary>
    private bool IsCatConnectedTo(string host, ushort port)
    {
      var cat = ctx.CatControl;
      if (cat == null) return false;

      return ConnectedTo(cat.Rx) || ConnectedTo(cat.Tx);

      bool ConnectedTo(CatControlEngine? engine) =>
        engine != null && engine.IsRunning && engine.Port == port &&
        string.Equals(engine.Host, host, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The radio if it is enabled and on this machine, otherwise null.</summary>
    private static CatRadioSettings? LocalRadio(CatRadioSettings radio)
    {
      return radio.Enabled && IsLocalHost(radio.Host) ? radio : null;
    }

    /// <summary>Whether the CAT host is this machine, and so ours to start a daemon for.</summary>
    internal static bool IsLocalHost(string host)
    {
      if (string.IsNullOrWhiteSpace(host)) return true;

      return host == "127.0.0.1" || host == "::1" ||
        string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Whether anything accepts a TCP connection there within the timeout.
    ///
    /// A probe that times out has to leave the connection attempt observed. Abandoning it and
    /// disposing the client faults the attempt with SocketException 995, and a fault nobody looks
    /// at is rethrown on the finalizer thread, where SkyRoof's unobserved-task handler writes it to
    /// the log: a probe that simply found nothing listening produced a stack trace at [ERR].
    /// </summary>
    private static bool IsPortOpen(string host, ushort port, int timeoutMs)
    {
      var client = new TcpClient();

      try
      {
        var connect = client.ConnectAsync(host, port);

        // a refused connection faults inside the timeout, and Wait observes it by throwing
        if (connect.Wait(timeoutMs)) return client.Connected;

        // a timed-out one faults later, when the client below is disposed, so it needs a
        // continuation to observe it after this method has already returned
        connect.ContinueWith(t => _ = t.Exception, TaskScheduler.Default);
        return false;
      }
      catch
      {
        return false;
      }
      finally
      {
        client.Dispose();
      }
    }


    //----------------------------------------------------------------------------------------------
    //                        job object: kill the daemon if SkyRoof dies
    //----------------------------------------------------------------------------------------------
    /// <summary>
    /// A Windows job object with kill-on-close, holding the daemon. Closing SkyRoof normally runs
    /// Shutdown, but a crash or a hard kill does not; the job is what stops an orphaned daemon from
    /// holding the serial port and the TCP port after SkyRoof is gone.
    /// </summary>
    private class JobObject : IDisposable
    {
      private const int JobObjectExtendedLimitInformation = 9;
      private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;

      private IntPtr handle;

      public JobObject()
      {
        handle = CreateJobObject(IntPtr.Zero, null);
        if (handle == IntPtr.Zero)
          throw new Exception($"CreateJobObject failed, error {Marshal.GetLastWin32Error()}");

        var limits = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
        limits.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;

        int size = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
        IntPtr buffer = Marshal.AllocHGlobal(size);
        try
        {
          Marshal.StructureToPtr(limits, buffer, false);
          if (!SetInformationJobObject(handle, JobObjectExtendedLimitInformation, buffer, (uint)size))
            throw new Exception($"SetInformationJobObject failed, error {Marshal.GetLastWin32Error()}");
        }
        finally
        {
          Marshal.FreeHGlobal(buffer);
        }
      }

      /// <summary>Puts the process in the job. Logged rather than thrown: losing the safety net
      /// is not a reason to fail a launch that has otherwise succeeded.</summary>
      public void Assign(Process process)
      {
        if (!AssignProcessToJobObject(handle, process.Handle))
          Log.Warning($"AssignProcessToJobObject failed, error {Marshal.GetLastWin32Error()}");
      }

      /// <summary>
      /// Lifts kill-on-close, so that closing the handle leaves the processes in the job running.
      /// A process cannot be taken back out of a job, but the job's own limit can be cleared, which
      /// is what lets Stop On Exit be switched off for a daemon that is already running. Returns
      /// false when the limit is still in force, so the caller can say so rather than promise a
      /// daemon will survive and then take it down.
      /// </summary>
      public bool Release()
      {
        if (handle == IntPtr.Zero) return true;

        var limits = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
        limits.BasicLimitInformation.LimitFlags = 0;

        int size = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
        IntPtr buffer = Marshal.AllocHGlobal(size);

        try
        {
          Marshal.StructureToPtr(limits, buffer, false);
          if (SetInformationJobObject(handle, JobObjectExtendedLimitInformation, buffer, (uint)size))
            return true;

          Log.Warning("Could not clear kill-on-close on the job object, error " +
            Marshal.GetLastWin32Error());
          return false;
        }
        catch (Exception ex)
        {
          // nothing here is worth failing a shutdown over; the caller reports what it means
          Log.Error(ex, "Could not clear kill-on-close on the job object");
          return false;
        }
        finally
        {
          Marshal.FreeHGlobal(buffer);
        }
      }

      /// <summary>Closes the job handle, which kills every process still in it.</summary>
      public void Dispose()
      {
        if (handle == IntPtr.Zero) return;

        CloseHandle(handle);
        handle = IntPtr.Zero;
      }

      [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
      private static extern IntPtr CreateJobObject(IntPtr securityAttributes, string? name);

      [DllImport("kernel32.dll", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool SetInformationJobObject(IntPtr job, int infoClass, IntPtr info, uint infoLength);

      [DllImport("kernel32.dll", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

      [DllImport("kernel32.dll", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool CloseHandle(IntPtr handle);

      [StructLayout(LayoutKind.Sequential)]
      private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
      {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
      }

      [StructLayout(LayoutKind.Sequential)]
      private struct IO_COUNTERS
      {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
      }

      [StructLayout(LayoutKind.Sequential)]
      private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
      {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
      }
    }
  }
}
