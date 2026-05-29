using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Newtonsoft.Json;

/*
    "SatelliteGroups": [
      {
        "Id": "4cf18f87-1eee-4d72-a2f3-4dd9883c5d52",
        "SatelliteIds": ["REOO-2119-3738-4964-1579", "OTOF-8025-8953-4756-4779", "HHSS-6325-1344-4603-7774", "PMAW-9203-2442-8666-3249", "XTKP-9404-9174-4088-2406", "XSKZ-5603-1870-9019-3066", "KLYF-4722-0218-1441-3015", "ANJN-0857-0652-4935-2307", "HIEK-3729-5596-2727-4744", "UFYD-5782-6372-2920-6054", "IRES-5964-9687-1982-0089", "PBTZ-3525-0189-2410-5216", "JLZT-0277-4784-7719-1707"],
        "SelectedSatId": "REOO-2119-3738-4964-1579",
        "Name": "Active Ham Sats"
      },
      {
        "Id": "44e2fa3a-daad-4563-98a5-f2b0fed98114",
        "SatelliteIds": ["XTDR-0995-4168-5549-5936", "WMGA-3000-1529-8035-2634", "NGHO-5109-0535-2414-7008", "WMPC-3806-8308-1122-5138", "EEIT-0315-8565-1765-1994", "AHVS-7983-8710-8819-8034", "VMQF-4924-7475-1891-3199", "TDVY-5283-4801-7631-6209", "LEDR-0123-1783-1341-9342"],
        "SelectedSatId": "XTDR-0995-4168-5549-5936",
        "Name": "Inactive Ham Sats"
      },
      {
        "Id": "710f3a71-7396-4b2a-951e-9c21bd2bf586",
        "SatelliteIds": ["XSKZ-5603-1870-9019-3066", "AYDR-6527-9294-8555-4924"],
        "SelectedSatId": "XSKZ-5603-1870-9019-3066",
        "Name": "Orbital Stations"
      }
    ]
*/

namespace SkyRoof
{
  public class SatelliteSettings
  {
    public DateTime LastDownloadTime = DateTime.MinValue;
    public DateTime LastTleTime = DateTime.MinValue;

    public Dictionary<string, SatelliteCustomization> SatelliteCustomizations = new();
    public Dictionary<string, TransmitterCustomization> TransmitterCustomizations = new();

    public List<SatelliteGroup> SatelliteGroups = new();
    public string SelectedGroupId;
    public string SelectedSatelliteId;

    /// <summary>
    /// satellite IDs selected for monitoring, ordered by priority (index 0 = highest).
    /// </summary>
    public List<string> MonitoredSatelliteIds = new();

    /// <summary>
    /// when enabled, SkyRoof automatically selects the highest-priority monitored satellite
    /// during an active pass (and applies its last-selected transmitter).
    /// </summary>
    public bool AutoMonitorEnabled = false;

    /// <summary>
    /// minimum pass max elevation (degrees) required before auto-monitoring will switch away
    /// from a higher-priority satellite to a lower-priority one.
    /// </summary>
    public int AutoMonitorMinElevationDeg = 0;

    public void DeleteInvalidData(SatnogsDb db)
    {
      // remove deleted sats
      foreach (var group in SatelliteGroups)
        group.SatelliteIds.RemoveAll(id => db.GetSatellite(id)?.Tle == null);

      Sanitize();
    }

    public void Sanitize(bool useDefaultGroups = false)
    {
      // ensure groups are non-empty
      SatelliteGroups.RemoveAll(g => g.SatelliteIds.Count == 0);

      if (SatelliteGroups.Count == 0)
        if (useDefaultGroups)
        {
          // default groups
          string str = Encoding.UTF8.GetString(Properties.Resources.default_groups);
          SatelliteGroups = JsonConvert.DeserializeObject<List<SatelliteGroup>>(str)!;
        }
        else
          // ensure that at least 1 group is present
          SatelliteGroups =
            [new SatelliteGroup {Name = "Orbital Stations",
              SatelliteIds = ["XSKZ-5603-1870-9019-3066", "AYDR-6527-9294-8555-4924"]}];


      // ensure there is a selected group
      var selectedGroup = SatelliteGroups.FirstOrDefault(g => g.Id == SelectedGroupId);
      if (string.IsNullOrEmpty(SelectedGroupId) || selectedGroup == null)
      {
        selectedGroup = SatelliteGroups[0];
        SelectedGroupId = selectedGroup.Id;
      }

      // ensure there is a selected sat in each group
      foreach (var group in SatelliteGroups)
        if (string.IsNullOrEmpty(group.SelectedSatId) || !group.SatelliteIds.Contains(group.SelectedSatId))
          group.SelectedSatId = group.SatelliteIds[0];

      // remove monitored sats not present in any data (will be pruned later when db is available)
      MonitoredSatelliteIds = MonitoredSatelliteIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();

      AutoMonitorMinElevationDeg = Math.Max(0, Math.Min(90, AutoMonitorMinElevationDeg));
    }
  }

  public class SatelliteGroup
  {
    public string Id = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public List<string> SatelliteIds = new();
    public string? SelectedSatId;
  }

  public class SatelliteCustomization
  {
    // sat_id is needed only if the sat was renamed
    public string? sat_id;

    public string? Name;
    public string? SelectedTransmitterId;

    /// <summary>
    /// recording preference for auto-recording when this satellite is selected/auto-selected.
    /// </summary>
    public AutoRecordMode AutoRecordMode = AutoRecordMode.Off;

    public bool DownlinkDopplerCorrectionEnabled = true;
    public bool DownlinkManualCorrectionEnabled = true;
    public int DownlinkManualCorrection;

    public bool UplinkDopplerCorrectionEnabled = true;
    public bool UplinkManualCorrectionEnabled = true;
    public int UplinkManualCorrection;
  }

  public enum AutoRecordMode
  {
    Off = 0,
    Audio = 1,
    Iq = 2,
  }

  public class TransmitterCustomization
  {
    public string uuid;
    public Slicer.Mode DownlinkMode;
    public Slicer.Mode UplinkMode;
    public double TransponderOffset;
  }
}