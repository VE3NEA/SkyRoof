using System.Runtime.InteropServices;
using Serilog;
using WeifenLuo.WinFormsUI.Docking;

namespace SkyRoof
{
  /// <summary>
  /// Live view of skycatd.exe. The daemon runs without a console window of its own, so this panel
  /// is where its output can be watched: it replays what the daemon has already printed and then
  /// follows along, the same way the Telemetry panel does for the decoders.
  /// </summary>
  public partial class SkyCatPanel : DockContent
  {
    private readonly Context ctx = null!;

    /// <summary>
    /// Lines received from the daemon thread, waiting for the next flush. They are batched because
    /// with -vvv the daemon can produce hundreds of lines a second, and appending to the TextBox
    /// once per line makes the UI crawl.
    /// </summary>
    private readonly List<DaemonLine> pending = new();
    private readonly object pendingLock = new();
    private System.Windows.Forms.Timer? flushTimer;

    // the highest sequence covered by the last snapshot taken from the daemon. Lines at or below it
    // are already in the box; everything above it arrives through the event exactly once. Set when the
    // panel opens and again whenever the view is caught up after being frozen, and touched only on the
    // UI thread.
    private long shownThrough;

    // how many lines arrived while the view was frozen, so the state line can say so rather than leave
    // a motionless box looking like a daemon that has gone quiet
    private long linesWhileFrozen;

    private const int FLUSH_INTERVAL_MS = 200;
    private const int MAX_BOX_LINES = 5000;   // matches the daemon's history buffer

    public SkyCatPanel()
    {
      InitializeComponent();
    }

    public SkyCatPanel(Context ctx)
    {
      InitializeComponent();

      this.ctx = ctx;
      Log.Information("Creating SkyCatPanel");

      ctx.SkyCatPanel = this;
      ctx.MainForm.SkyCatMNU.Checked = true;

      // subscribed before the snapshot is taken, so that a line printed while this constructor
      // runs cannot fall through the gap between the two. That makes duplicates possible instead,
      // which the sequence numbers take care of at flush time.
      ctx.SkyCatDaemon.LineLogged += Daemon_LineLogged;

      // whatever the daemon printed before this panel existed
      SetBoxLines(ctx.SkyCatDaemon.GetHistory(out shownThrough));
      ScrollToEnd();

      flushTimer = new System.Windows.Forms.Timer { Interval = FLUSH_INTERVAL_MS };
      flushTimer.Tick += FlushTimer_Tick;
      flushTimer.Start();

      ShowState();
    }

    /// <summary>
    /// Detaches from the daemon. The daemon outlives this panel, so leaving the handler attached
    /// would keep the closed form alive and queue lines nobody will ever read.
    /// </summary>
    private void SkyCatPanel_FormClosed(object? sender, FormClosedEventArgs e)
    {
      ctx.SkyCatDaemon.LineLogged -= Daemon_LineLogged;

      flushTimer?.Stop();
      flushTimer?.Dispose();
      flushTimer = null;

      ctx.SkyCatPanel = null;
      ctx.MainForm.SkyCatMNU.Checked = false;
    }


    //----------------------------------------------------------------------------------------------
    //                                        output
    //----------------------------------------------------------------------------------------------
    /// <summary>
    /// Queues one line. Raised on the daemon's output thread, so this may not touch the controls;
    /// the flush timer moves the batch onto the UI thread.
    /// </summary>
    private void Daemon_LineLogged(object? sender, DaemonLine line)
    {
      lock (pendingLock)
      {
        pending.Add(line);

        // a verbose daemon plus a stalled UI thread would otherwise grow this without bound, only
        // for TrimToMaxLines to throw all but the last screenful away again
        if (pending.Count > MAX_BOX_LINES) pending.RemoveRange(0, pending.Count - MAX_BOX_LINES);
      }
    }

    /// <summary>
    /// Moves whatever has queued up since the last tick into the text box, unless the view is frozen.
    ///
    /// Clearing Auto scroll freezes the box rather than merely stopping it scrolling. Trimming is what
    /// makes that necessary: the box holds 5000 lines and a daemon run with -vvv fills that in a couple
    /// of minutes, so the lines being read are deleted from the top while they are being read. A scroll
    /// position inside text that is being destroyed underneath it cannot be held, and trying to hold it
    /// looked exactly like the box scrolling on its own. The state line reports the freeze, since the
    /// name of the box promises less than it now does.
    /// </summary>
    private void FlushTimer_Tick(object? sender, EventArgs e)
    {
      DaemonLine[] batch;

      if (!AutoScrollCheck.Checked)
      {
        // the daemon goes on buffering, so nothing is lost that its own 5000 lines still hold, and the
        // box is rebuilt from that buffer when Auto scroll comes back on
        lock (pendingLock)
        {
          linesWhileFrozen += pending.Count;
          pending.Clear();
        }

        ShowState();
        return;
      }

      // ShowState is deliberately not called under this lock: it reaches back into the daemon,
      // which takes the history lock, while the daemon's reader thread takes them the other way
      lock (pendingLock)
      {
        batch = pending.ToArray();
        pending.Clear();
      }

      // Emit numbers a line under the daemon's history lock but raises the event outside it, so
      // lines can arrive out of order - the stdout and stderr readers, the UI thread and the exit
      // handler all emit. Sorting restores the daemon's order within a batch, which is as far as
      // this can go: a line that misses its flush entirely is still appended after the one that
      // overtook it. Ordering across batches would mean holding lines back, which is not worth a
      // 200 ms delay on output that is being watched live.
      Array.Sort(batch, (a, b) => a.Sequence.CompareTo(b.Sequence));

      // only the snapshot boundary needs filtering. shownThrough is deliberately NOT advanced past
      // it: past the boundary every line reaches the handler exactly once and pending is emptied
      // each flush, so there is nothing to deduplicate - and advancing it to the last line of a
      // batch discarded any lower-numbered line that arrived in a later one.
      var lines = batch.Where(l => l.Sequence > shownThrough).Select(l => l.Text).ToArray();

      if (lines.Length > 0) AppendLines(lines);

      ShowState();
    }

    /// <summary>
    /// Freezes the box, or catches it up again. Called when Auto scroll is ticked or cleared.
    /// </summary>
    private void AutoScrollCheck_CheckedChanged(object? sender, EventArgs e)
    {
      // InitializeComponent runs before the constructor assigns ctx, and both branches below reach
      // through it. Nothing fires this during initialization today, because the designer emits the
      // Checked property before the event subscription, but that is the designer's ordering rather
      // than anything this class controls - and the parameterless constructor never sets ctx at all.
      if (ctx == null) return;

      if (AutoScrollCheck.Checked) Resync();
      else linesWhileFrozen = 0;

      ShowState();
    }

    /// <summary>
    /// Rebuilds the box from the daemon's buffer and goes to the end. This is how the view catches up
    /// after being frozen: the box stood still while the buffer moved on, possibly turning over
    /// completely, so there is nothing sensible to append to and a fresh snapshot is both simpler and
    /// correct. It is the same handshake the constructor uses, so a line that races it appears in both
    /// the snapshot and the queue and is filtered out by its sequence number.
    /// </summary>
    private void Resync()
    {
      lock (pendingLock) pending.Clear();

      SetBoxLines(ctx.SkyCatDaemon.GetHistory(out shownThrough));
      ScrollToEnd();
      linesWhileFrozen = 0;
    }

    /// <summary>
    /// Appends a batch as a single edit, trims the box back to its line limit and goes to the end. Only
    /// ever reached with Auto scroll ticked: with it clear the box is frozen and nothing is appended.
    /// </summary>
    private void AppendLines(string[] lines)
    {
      // the append, the trim and the scroll are one visible step. Trimming replaces the whole text,
      // which puts the view back at the top, so without this the box flickered top to bottom on every
      // flush - five times a second on a verbose daemon.
      SuspendRedraw();

      try
      {
        OutputBox.AppendText(string.Join(Environment.NewLine, lines) + Environment.NewLine);
        TrimToMaxLines();
        ScrollToEnd();
      }
      finally
      {
        ResumeRedraw();
      }
    }

    /// <summary>Drops the oldest lines so an all-day session cannot grow the box without bound.</summary>
    private void TrimToMaxLines()
    {
      var current = OutputBox.Lines;
      if (current.Length <= MAX_BOX_LINES) return;

      SetBoxLines(current[^MAX_BOX_LINES..]);
    }


    //----------------------------------------------------------------------------------------------
    //                                      scroll position
    //----------------------------------------------------------------------------------------------
    // Handled through the edit control directly: EM_LINESCROLL moves the view without touching the
    // caret or the selection, and it works while painting is suspended, which ScrollToCaret does not.
    private const int WM_SETREDRAW = 0x000B;
    private const int EM_LINESCROLL = 0x00B6;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    /// <summary>Stops the box painting, so that a batch of edits is shown as one.</summary>
    private void SuspendRedraw()
    {
      if (OutputBox.IsHandleCreated)
        SendMessage(OutputBox.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
    }

    /// <summary>Lets the box paint again, and asks it to, since it ignored everything in between.</summary>
    private void ResumeRedraw()
    {
      if (!OutputBox.IsHandleCreated) return;

      SendMessage(OutputBox.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
      OutputBox.Invalidate();
    }

    /// <summary>
    /// Replaces the box contents, keeping the trailing newline that the Lines setter drops. Without
    /// it the next AppendText would land on the end of the last line instead of below it.
    /// </summary>
    private void SetBoxLines(string[] lines)
    {
      // the Lines getter yields a trailing empty element whenever the text ends in a newline, so
      // drop any such elements first and add exactly one terminator back
      int count = lines.Length;
      while (count > 0 && lines[count - 1].Length == 0) count--;

      OutputBox.Text = count == 0
        ? string.Empty
        : string.Join(Environment.NewLine, lines, 0, count) + Environment.NewLine;
    }

    /// <summary>Scrolls to the newest line.</summary>
    private void ScrollToEnd()
    {
      OutputBox.SelectionStart = OutputBox.TextLength;
      OutputBox.SelectionLength = 0;

      // EM_LINESCROLL rather than ScrollToCaret, because this has to work while painting is suspended,
      // which it is inside AppendLines: ScrollToCaret did nothing there and the box stopped following
      // the tail with Auto scroll set. A delta past the end clamps at the last line.
      if (OutputBox.IsHandleCreated)
        SendMessage(OutputBox.Handle, EM_LINESCROLL, IntPtr.Zero, (IntPtr)(MAX_BOX_LINES + 1));
      else
        OutputBox.ScrollToCaret();
    }

    /// <summary>
    /// Clears the box, the pending batch and the daemon's buffer together, so that what was cleared
    /// does not reappear when the panel is closed and opened again.
    /// </summary>
    private void ClearBtn_Click(object sender, EventArgs e)
    {
      lock (pendingLock) pending.Clear();

      ctx.SkyCatDaemon.ClearHistory();
      OutputBox.Clear();

      // the count goes with them. Clearing a frozen panel otherwise left the state line reporting
      // thousands of lines waiting, about lines that no longer existed in the box, the queue or the
      // daemon's buffer.
      linesWhileFrozen = 0;
      ShowState();
    }

    /// <summary>
    /// Describes the daemon: running under SkyRoof, running but started elsewhere, disabled, or
    /// simply not running. Refreshed on every tick, so it follows the daemon without extra events.
    /// </summary>
    private void ShowState()
    {
      var daemon = ctx.SkyCatDaemon;

      // an adopted daemon is not ours and nothing tells us when it stops; this is throttled and
      // probes on a background thread, so calling it every tick is cheap
      daemon.RefreshAdoptionState();

      string state;

      if (daemon.IsRunning) state = "Running, started by SkyRoof";
      else if (daemon.IsAdopted) state = "Running, started outside SkyRoof";
      else if (!ctx.Settings.Cat.SkyCat.Enabled) state = "Auto Start is off";
      else state = "Not running";

      // a frozen box and a daemon that has gone quiet look identical, so the difference is spelled out,
      // with a count that shows the output is still being collected
      if (!AutoScrollCheck.Checked)
        state += linesWhileFrozen == 0
          ? "   -   paused"
          : $"   -   paused, {linesWhileFrozen:N0} new line{(linesWhileFrozen == 1 ? "" : "s")}";

      StateLabel.Text = state;
    }
  }
}
