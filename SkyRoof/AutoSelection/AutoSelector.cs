using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MathNet.Numerics;
using Serilog;
using VE3NEA;

namespace SkyRoof
{
  // Automatic satellite-selection engine (design-docs/auto_selection_plan.md, P2).
  // Each second it filters the current group's predicted passes to the group's schedule, picks a target
  // pass per the schedule's overlap mode, and retunes through the SatelliteSelector. Recording (P4) and
  // the config dialog (P3) are out of scope here.
  public class AutoSelector
  {
    public Context ctx = null!;

    // runtime-only master on/off (never persisted, always OFF at startup - plan §1.8)
    public bool Enabled { get; private set; }

    // the pass we are currently tuned to, or null in an idle gap
    public SatellitePass? ActivePass { get; private set; }

    // antenna pre-roll: when coming out of an idle gap the rotator starts slewing to the next pass's AOS
    // point this many seconds before AOS; switches between two live passes have no advance (§ grill design)
    private const int PreRollLeadSeconds = 60;

    // set while the engine performs its own SetSelected* calls so the selector events do not read them
    // as a manual override (plan §2.4)
    private bool selecting;

    // the upcoming pass the idle-gap pre-roll has already been applied to; keeps UpdateIdleTracking from
    // running on every idle tick, so the user is free to stop the rotator during the pre-roll window
    private SatellitePass? idleTrackedPass;

    // the engine's own headless recorder, separate from the manual RecorderPanel (plan §2.5); a segment
    // runs from selection to deselection/LOS and is saved as one file per contiguous segment
    private RecordingManager recorder = null!;
    private RecordMode segmentMode;
    private DateTime segmentStartLocal;
    private string? segmentSatName;

    private AutoSelectionSettings Settings => ctx.Settings.Satellites.AutoSelection;
    private string CurrentGroupId => ctx.Settings.Satellites.SelectedGroupId;

    // recording state for the status panel
    public bool IsRecording => recorder?.IsRecording == true;
    public RecordMode SegmentRecordMode => segmentMode;
    public DateTime SegmentStartLocal => segmentStartLocal;


    // subscribes to the selection events used for manual-override detection; call once after
    // ctx.SatelliteSelector has been assigned
    public void Initialize()
    {
      recorder = new RecordingManager(ctx);

      ctx.SatelliteSelector.SelectedGroupChanged += Selector_SelectionChangedByUser;
      ctx.SatelliteSelector.SelectedSatelliteChanged += Selector_SelectionChangedByUser;
      ctx.SatelliteSelector.SelectedTransmitterChanged += Selector_SelectionChangedByUser;
    }




    //----------------------------------------------------------------------------------------------
    //                                        enable / disable
    //----------------------------------------------------------------------------------------------
    public void SetEnabled(bool enabled)
    {
      if (enabled)
      {
        // the current group must have a non-empty schedule (the toggle is disabled otherwise)
        if (!Settings.Schedules.TryGetValue(CurrentGroupId, out var schedule) || schedule.Sats.Count == 0)
          return;

        Enabled = true;
        Log.Information("Auto-selection started for group {GroupId}", CurrentGroupId);
      }
      else
      {
        bool wasEnabled = Enabled;
        EndSegment();                                       // stop + save any recording in progress
        // stop the antenna if auto-selection was tracking it; leave any manual tracking alone
        if (CurrentSchedule?.TrackAntenna == true && ctx.RotatorControl.IsTracking)
          ctx.RotatorControl.StopRotation();
        Enabled = false;
        ActivePass = null;
        idleTrackedPass = null;
        if (wasEnabled) Log.Information("Auto-selection stopped");
      }
    }

    // true when the current group has a schedule that can be run (used to enable the panel toggle)
    public bool CanEnable()
    {
      return Settings.Schedules.TryGetValue(CurrentGroupId, out var schedule) && schedule.Sats.Count > 0;
    }

    // the current group's schedule, or null when the group has none
    public GroupSchedule? CurrentSchedule =>
      Settings.Schedules.TryGetValue(CurrentGroupId, out var schedule) ? schedule : null;

    // the next pass the engine will switch to, and the time of that switch (for the status panel). the
    // "next" pass depends on the overlap mode - e.g. in Finish-current the engine holds the active pass to
    // its LOS, so the next switch is at that LOS, not at the earliest upcoming AOS - so it is computed by
    // simulating the schedule forward from now under the current mode until the selection differs from the
    // active pass
    public (SatellitePass Pass, DateTime When)? GetNextSelection()
    {
      var schedule = CurrentSchedule;
      if (schedule == null) return null;

      var now = DateTime.UtcNow;
      var horizon = now.AddHours(48);
      var scheduledKeys = new HashSet<(string, int)>(schedule.Passes.Select(p => (p.SatId, p.OrbitNumber)));

      var passes = ctx.GroupPasses.Passes
        .Where(p => scheduledKeys.Contains((p.Satellite.sat_id, p.OrbitNumber)) && p.EndTime > now)
        .ToList();
      if (passes.Count == 0) return null;

      var dt = TimeSpan.FromSeconds(5);
      var simActive = ActivePass;

      for (var t = now; t < horizon; )
      {
        var upNow = passes.Where(p => p.StartTime <= t && t < p.EndTime).ToList();
        var target = ChooseTarget(schedule, upNow, t, simActive);

        // the first moment the selection differs from the pass active right now is the next switch
        if (target != null && !IsSamePass(target, ActivePass)) return (target, t);

        simActive = target;

        if (target == null)
        {
          // idle gap - jump straight to the next AOS instead of stepping through it
          DateTime? nextAos = passes.Where(p => p.StartTime > t).Select(p => (DateTime?)p.StartTime).Min();
          if (nextAos == null) return null;
          t = nextAos.Value;
        }
        else
          t += dt;
      }

      return null;
    }

    // removes the current group's schedule (Clear button) and stops the engine
    public void ClearSchedule()
    {
      SetEnabled(false);
      Settings.Schedules.Remove(CurrentGroupId);
    }




    //----------------------------------------------------------------------------------------------
    //                                            tick
    //----------------------------------------------------------------------------------------------
    // called once per second from MainForm.OneSecondTick
    public void Tick()
    {
      if (!Enabled) { ActivePass = null; return; }

      if (!Settings.Schedules.TryGetValue(CurrentGroupId, out var schedule))
      {
        SetEnabled(false);                                  // current group has no schedule (plan §2.6)
        return;
      }

      var now = DateTime.UtcNow;
      var scheduledKeys = new HashSet<(string, int)>(
        schedule.Passes.Select(p => (p.SatId, p.OrbitNumber)));

      var candidates = ctx.GroupPasses.Passes
        .Where(p => scheduledKeys.Contains((p.Satellite.sat_id, p.OrbitNumber))
                    && p.StartTime <= now && now < p.EndTime)
        .ToList();

      var target = ChooseTarget(schedule, candidates, now, ActivePass);

      // idle gap - stop any recording (LOS) but stay tuned to the last transmitter. the pre-roll runs at
      // most once per upcoming pass: once idleTrackedPass is set, this gap's antenna handling is done
      if (target == null)
      {
        EndSegment();
        ActivePass = null;
        if (idleTrackedPass == null) UpdateIdleTracking();
        return;
      }

      if (!IsSamePass(target, ActivePass))
        SelectPass(schedule, target);
      else
        ActivePass = target;                                // keep, refresh the reference
    }

    // picks the pass to be tuned at the given time, or null when none of the scheduled passes are up.
    // takes the active pass as a parameter (rather than the live field) so GetNextSelection can simulate
    // the schedule forward with a hypothetical active pass
    private SatellitePass? ChooseTarget(GroupSchedule schedule, List<SatellitePass> candidates, DateTime now, SatellitePass? activePass)
    {
      if (candidates.Count == 0) return null;

      switch (schedule.OverlapMode)
      {
        case OverlapMode.FinishCurrent:
          // hold the active pass until its LOS, otherwise enter the earliest-AOS candidate
          var current = candidates.FirstOrDefault(p => IsSamePass(p, activePass));
          return current ?? candidates.OrderBy(p => p.StartTime).First();

        case OverlapMode.HighestElevation:
          var best = candidates.OrderByDescending(p => ElevationOf(p, now)).First();
          var active = candidates.FirstOrDefault(p => IsSamePass(p, activePass));
          if (active == null || IsSamePass(best, active)) return best;
          // switch only if the challenger clears the active pass by the hysteresis margin
          double advantage = ElevationOf(best, now) - ElevationOf(active, now);
          return advantage > Settings.ElevationHysteresisDeg ? best : active;

        case OverlapMode.Priority:
          // the up candidate whose satellite has the highest priority (lowest schedule index)
          return candidates
            .OrderBy(p => PriorityIndex(schedule, p))
            .ThenBy(p => p.StartTime)
            .First();

        default:
          return null;
      }
    }

    // retunes to the given pass through the selector, guarded so the resulting events are not read as a
    // manual override
    private void SelectPass(GroupSchedule schedule, SatellitePass pass)
    {
      Log.Information("Auto-selection selected {Sat} orbit #{Orbit}", pass.Satellite.name, pass.OrbitNumber);

      EndSegment();                                         // stop + save the previous segment

      selecting = true;
      try
      {
        ctx.SatelliteSelector.SetSelectedSatellite(pass.Satellite);
        var tx = TransmitterFor(schedule, pass.Satellite);
        if (tx != null) ctx.SatelliteSelector.SetSelectedTransmitter(tx);
      }
      finally
      {
        selecting = false;
      }

      ActivePass = pass;
      idleTrackedPass = null;                               // the idle gap is over, arm the next pre-roll
      BeginSegment(schedule, pass);                         // start recording per the schedule's RecordMode
      // re-assert tracking after the selection cascade above (SelectedPassChanged -> RotatorWidget.SetPass
      // -> ResetUi) unchecked the track box; also handles the no-advance repoint at a sat-to-sat switch
      if (CurrentSchedule?.TrackAntenna == true) ctx.RotatorControl.TrackPass(pass);
    }

    // re-applies the active pass's scheduled transmitter. the engine tunes the transmitter only in
    // SelectPass (i.e. when it switches passes), so a transmitter change made in the config dialog to the
    // already-active satellite would otherwise not take effect until the next pass switch. call this after
    // the schedule is edited so such a change applies immediately (plan §1.9 override guard is respected
    // via the selecting flag)
    public void ReapplyActivePass()
    {
      if (!Enabled || ActivePass == null) return;

      var schedule = CurrentSchedule;
      // let Tick handle it if the active satellite is no longer part of the schedule
      if (schedule == null || schedule.Sats.All(s => s.SatId != ActivePass.Satellite.sat_id)) return;

      var tx = TransmitterFor(schedule, ActivePass.Satellite);
      if (tx == null) return;

      selecting = true;
      try
      {
        ctx.SatelliteSelector.SetSelectedTransmitter(tx);
      }
      finally
      {
        selecting = false;
      }
    }

    // the transmitter chosen for the satellite in the schedule, falling back to its first transmitter
    private static SatnogsDbTransmitter? TransmitterFor(GroupSchedule schedule, SatnogsDbSatellite sat)
    {
      var scheduledSat = schedule.Sats.FirstOrDefault(s => s.SatId == sat.sat_id);
      var uuid = scheduledSat?.TransmitterUuid;
      return sat.Transmitters.FirstOrDefault(t => t.uuid == uuid) ?? sat.Transmitters.FirstOrDefault();
    }




    //----------------------------------------------------------------------------------------------
    //                                     manual-override detection
    //----------------------------------------------------------------------------------------------
    // a manual satellite/transmitter/group change has already been applied by the time the event fires;
    // the engine simply stops itself and warns (plan §1.9, §2.4)
    private void Selector_SelectionChangedByUser(object? sender, EventArgs e)
    {
      if (selecting) return;                                // our own change, ignore
      if (!Enabled) return;                                 // only the first event of a user gesture stops us
                                                            // (clearing Enabled makes the cascade events fall through)

      SetEnabled(false);                                    // clears ActivePass; ends recording in P4

      // show the warning after the current selection cascade completes, so it does not block mid-cascade
      ctx.MainForm.BeginInvoke(new Action(ShowOverrideWarning));
    }

    private void ShowOverrideWarning()
    {
      MessageBox.Show(ctx.MainForm,
        "Auto-selection stopped because you changed the selection manually.",
        "Auto Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }




    //----------------------------------------------------------------------------------------------
    //                                          recording
    //----------------------------------------------------------------------------------------------
    // fed from MainForm.Slicer_Iq/AudioDataAvailable alongside the RecorderPanel feed; a no-op unless the
    // engine is currently recording a segment. a full buffer (>30-min pass) splits into sequential files.
    public void AddIqSamples(DataEventArgs<Complex32> e)
    {
      if (recorder == null || !recorder.IsRecording) return;
      if (recorder.AddIqSamples(e)) SplitSegment();
    }

    public void AddAudioSamples(DataEventArgs<float> e)
    {
      if (recorder == null || !recorder.IsRecording) return;
      if (recorder.AddAudioSamples(e)) SplitSegment();
    }

    // starts a recording segment for the pass unless the schedule's RecordMode is Off (plan §2.5)
    private void BeginSegment(GroupSchedule schedule, SatellitePass pass)
    {
      BeginSegmentInternal(schedule.Record, pass.Satellite.name);
    }

    private void BeginSegmentInternal(RecordMode mode, string? satName)
    {
      segmentMode = mode;
      segmentSatName = satName;
      if (mode == RecordMode.Off) return;

      segmentStartLocal = DateTime.Now;
      recorder.StartRecording(isAudio: mode == RecordMode.Audio);
    }

    // stops the current segment and saves it into the Recordings\Auto subfolder (audio->mp3, I/Q->iq.wav)
    private void EndSegment()
    {
      if (recorder == null || !recorder.IsRecording) return;

      recorder.StopRecording();
      if (!recorder.HasData) return;

      bool isAudio = recorder.IsRecordingAudio;
      string ext = isAudio ? "mp3" : "wav";
      string name = RecordingManager.BuildRecordingFileName(segmentStartLocal, segmentSatName, ext, !isAudio);
      string path = Path.Combine(GetAutoRecordingsPath(), name);

      try
      {
        recorder.SaveRecording(path, isAudio);
        Log.Information("Auto-selection saved recording {Path}", path);
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Auto-selection failed to save recording {Path}", path);
      }
    }

    // buffer full (a >30-min pass): save the current segment and immediately start the next one, so a long
    // pass becomes a sequence of split files. marshalled to the UI thread so the file save does not run on
    // the DSP thread that delivers the samples.
    private void SplitSegment()
    {
      ctx.MainForm.BeginInvoke(new Action(() =>
      {
        if (!Enabled || ActivePass == null || segmentMode == RecordMode.Off) return;
        var mode = segmentMode;
        var satName = segmentSatName;
        EndSegment();
        BeginSegmentInternal(mode, satName);
      }));
    }

    private static string GetAutoRecordingsPath()
    {
      string path = Path.Combine(Utils.GetUserDataFolder(), "Recordings", "Auto");
      Directory.CreateDirectory(path);
      return path;
    }




    //----------------------------------------------------------------------------------------------
    //                                       antenna tracking
    //----------------------------------------------------------------------------------------------
    // idle-gap antenna handling: while nothing is selected, auto-selection only engages the rotator
    // PreRollLeadSeconds before the next pass's AOS (slewing to the AOS point ahead of time - only the
    // antenna moves, the satellite is not selected/tuned until AOS). outside that window it does NOT touch
    // the rotator, so it never interferes with manual tracking during an idle gap; the rotator widget
    // stops tracking on its own at LOS (its path bearing goes null). TrackAntenna off => never touched.
    // the rotator's own track checkbox (RotatorControl.IsTracking) is the single source of truth for
    // whether tracking is currently on, so no shadow state is kept here (§ grill design)
    private void UpdateIdleTracking()
    {
      if (CurrentSchedule?.TrackAntenna != true) return;

      var next = GetNextSelection();
      bool inPreRoll = next != null
        && next.Value.When - DateTime.UtcNow <= TimeSpan.FromSeconds(PreRollLeadSeconds);
      if (!inPreRoll) return;

      // the pre-roll is now done for this pass, whether or not we engaged the rotator below
      idleTrackedPass = next!.Value.Pass;

      // if the antenna is already tracking, do not rebuild the path
      if (!ctx.RotatorControl.IsTracking) ctx.RotatorControl.TrackPass(idleTrackedPass);
    }

    // applies a change of the schedule's Track Antenna option made in the config dialog: switching it on
    // starts tracking the active pass right away, switching it off stops the rotator. a no-op when the
    // option did not change or when auto-selection is not running
    public void ApplyTrackAntennaChange(bool trackedBefore)
    {
      bool tracksNow = CurrentSchedule?.TrackAntenna == true;
      if (tracksNow == trackedBefore || !Enabled) return;

      idleTrackedPass = null;                               // let the idle pre-roll re-evaluate

      if (!tracksNow)
      {
        if (ctx.RotatorControl.IsTracking) ctx.RotatorControl.StopRotation();
      }
      else if (ActivePass != null)
        ctx.RotatorControl.TrackPass(ActivePass);           // in an idle gap the pre-roll takes over
    }




    //----------------------------------------------------------------------------------------------
    //                                          helpers
    //----------------------------------------------------------------------------------------------
    // pass identity is (sat_id, OrbitNumber) - the same pass may be a different object after a rebuild
    private static bool IsSamePass(SatellitePass? a, SatellitePass? b)
    {
      return a != null && b != null
        && a.Satellite.sat_id == b.Satellite.sat_id && a.OrbitNumber == b.OrbitNumber;
    }

    private static double ElevationOf(SatellitePass pass, DateTime now)
    {
      return pass.GetObservationAt(now)?.Elevation.Degrees ?? -90;
    }

    private static int PriorityIndex(GroupSchedule schedule, SatellitePass pass)
    {
      int index = schedule.Sats.FindIndex(s => s.SatId == pass.Satellite.sat_id);
      return index < 0 ? int.MaxValue : index;
    }
  }
}
