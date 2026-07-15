using System.Collections.Generic;
using System.Linq;

namespace SkyRoof
{
  public enum OverlapMode { FinishCurrent, HighestElevation, Priority }
  public enum RecordMode { Off, Audio, Iq }

  public class AutoSelectionSettings
  {
    // highest-elevation overlap mode only: minimum elevation advantage a challenger pass must have
    // over the active pass before the engine switches to it
    public double ElevationHysteresisDeg = 2;

    // one schedule per satellite group, keyed by SatelliteGroup.Id (mirrors SatelliteCustomizations)
    public Dictionary<string, GroupSchedule> Schedules = new();

    // NB: the global master on/off is NOT persisted here - Enabled is a runtime-only flag on the
    // AutoSelector engine, so auto-selection is always off at startup

    // reconciles the persisted schedules with the current groups; runs at settings load and save, and
    // (with the db supplied) whenever satellite data is refreshed. db is optional because at settings
    // load the SatnogsDb is not yet populated - the geostationary drop is simply skipped then
    public void SyncToGroups(SatelliteSettings sats, SatnogsDb? db = null)
    {
      // drop schedules whose group was deleted
      var groupIds = sats.SatelliteGroups.Select(g => g.Id).ToHashSet();
      foreach (var groupId in Schedules.Keys.Where(id => !groupIds.Contains(id)).ToList())
        Schedules.Remove(groupId);

      foreach (var group in sats.SatelliteGroups)
      {
        if (!Schedules.TryGetValue(group.Id, out var schedule)) continue;

        // drop scheduled sats that are no longer members of the group, or that are geostationary
        var memberIds = group.SatelliteIds.ToHashSet();
        schedule.Sats.RemoveAll(s => !memberIds.Contains(s.SatId) || IsGeoStationary(db, s.SatId));

        // drop passes whose sat is no longer scheduled
        var scheduledSatIds = schedule.Sats.Select(s => s.SatId).ToHashSet();
        schedule.Passes.RemoveAll(p => !scheduledSatIds.Contains(p.SatId));
      }
    }

    // a sat is geostationary if the db is available and reports it so; when the db is not yet loaded
    // the check is skipped, leaving such entries to be dropped at the next db-aware sync
    private static bool IsGeoStationary(SatnogsDb? db, string satId)
    {
      var sat = db?.GetSatellite(satId);
      return sat?.Tracker.IsGeoStationary() == true;
    }
  }

  public class GroupSchedule
  {
    public OverlapMode OverlapMode = OverlapMode.FinishCurrent;
    public List<ScheduledSat> Sats = new();     // list order = priority (index 0 = highest)
    public List<ScheduledPass> Passes = new();  // the ticked leaves
  }

  public class ScheduledSat
  {
    public string SatId;
    public string TransmitterUuid;
    public RecordMode Record = RecordMode.Off;
  }

  public class ScheduledPass
  {
    public string SatId;
    public int OrbitNumber;
  }
}
