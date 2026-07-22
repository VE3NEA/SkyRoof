using System.ComponentModel;
using System.DirectoryServices.ActiveDirectory;
using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Serilog;
using SGPdotNET.TLE;
using VE3NEA;

namespace SkyRoof
{
  public class SatnogsDb
  {
    private readonly string DataFolder, DownloadsFolder;
    private Dictionary<string, SatnogsDbSatellite> SatelliteList = new();
    private readonly HttpClient DownloadHttpClient = new();
    private CancellationTokenSource cts;
    private JsonSerializerSettings JsonSettings = new();

    public IEnumerable<SatnogsDbSatellite> Satellites { get => SatelliteList.Values; }
    public bool Loaded { get => loaded; }
    private bool loaded;

    public event EventHandler<ProgressChangedEventArgs>? DownloadProgress;
    public event EventHandler? TleUpdated;
    public event EventHandler? ListUpdated;


    public SatnogsDb()
    {
      DataFolder = Utils.GetUserDataFolder();
      DownloadsFolder = Path.Combine(DataFolder, "Downloads");
      Directory.CreateDirectory(DownloadsFolder);

      // merge the embedded default transmitter overrides into the user-editable file, keeping user edits
      string txOverrideFile = Path.Combine(DataFolder, "transmitters-override.json");
      MergeOverrideFile(txOverrideFile);

      JsonSettings.Converters.Add(new IsoDateTimeConverter { DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ssK" });

      // GitHub's codeload host is happier with an explicit User-Agent.
      DownloadHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("SkyRoof");
    }

    public void LoadFromFile()
    {
      SatelliteList.Clear();

      try
      {
        loaded = false;
        string path = Path.Combine(DataFolder, "Satellites.json");
        if (!File.Exists(path)) return;

        string json = File.ReadAllText(path);
        var satellites = JsonConvert.DeserializeObject<SatnogsDbSatelliteList>(json);
        SatelliteList = satellites.ToDictionary(s => s.sat_id);

        foreach (var sat in satellites)
          foreach (var tx in sat.Transmitters)
            tx.Satellite = sat;

        loaded = SatelliteList.Count > 0;
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Failed to load satellite list");
        SatelliteList.Clear();
      }
    }

    public void SaveToFile()
    {
      string path = Path.Combine(DataFolder, "Satellites.json");
      File.WriteAllText(path, JsonConvert.SerializeObject(Satellites));
    }

    internal void ReplaceSatelliteList(SatnogsDb db)
    {
      SatelliteList = db.SatelliteList;
      loaded = SatelliteList.Count > 0;
      ListUpdated?.Invoke(this, EventArgs.Empty);
    }

    public SatnogsDbSatellite? GetSatellite(string satId)
    {
      return SatelliteList.GetValueOrDefault(satId);
    }


    //----------------------------------------------------------------------------------------------
    //                              download satellite data
    //----------------------------------------------------------------------------------------------
    public void AbortDownload()
    {
      cts.Cancel();
    }

    public async Task DownloadAll()
    {
      cts = new CancellationTokenSource();

      await Download("satellites");
      Log.Information("satellites downloaded");
      DownloadProgress?.Invoke(this, new(25, null));

      await Download("transmitters");
      Log.Information("transmitters downloaded");
      DownloadProgress?.Invoke(this, new(50, null));

      await Download("tle");
      Log.Information("tle downloaded");
      DownloadProgress?.Invoke(this, new(70, null));

      await DownloadJE9PEL();
      Log.Information("JE9PEL downloaded");
      DownloadProgress?.Invoke(this, new(85, null));

      // gr-satellites satyaml is enrichment only — a failure here must not abort the download.
      try
      {
        await DownloadSatyaml();
        Log.Information("satyaml downloaded");
      }
      catch (OperationCanceledException) { throw; }
      catch (Exception ex) { Log.Warning(ex, "satyaml download/extract failed (enrichment skipped)"); }
      DownloadProgress?.Invoke(this, new(99, null));
    }

    public async Task DownloadTle()
    {
      cts = new CancellationTokenSource();
      await Download("tle");

      try
      {
        ImportSatnogsTle();
        SaveToFile();
        TleUpdated?.Invoke(this, EventArgs.Empty);
      }
      catch (Exception ex)
      {
        Log.Error(ex, "TLE import failed");
        throw;
      }
    }

    private async Task Download(string name)
    {
      string url = $"https://db.satnogs.org/api/{name}/?format=json";
      string json = await DownloadHttpClient.GetStringAsync(url, cts.Token);
      cts.Token.ThrowIfCancellationRequested();

      File.WriteAllText(Path.Combine(DownloadsFolder, $"{name}.json"), json);
    }

    private async Task DownloadJE9PEL()
    {
      string url = "http://www.ne.jp/asahi/hamradio/je9pel/satslist.csv";
      string csv = await DownloadHttpClient.GetStringAsync(url, cts.Token);
      cts.Token.ThrowIfCancellationRequested();

      File.WriteAllText(Path.Combine(DownloadsFolder, "JE9PEL.csv"), csv);
    }

    // Download the gr-satellites repo zip and extract only python/satyaml/*.yml into Downloads\satyaml.
    private async Task DownloadSatyaml()
    {
      const string url = "https://codeload.github.com/daniestevez/gr-satellites/zip/refs/heads/main";
      byte[] zipBytes = await DownloadHttpClient.GetByteArrayAsync(url, cts.Token);
      cts.Token.ThrowIfCancellationRequested();

      string zipPath = Path.Combine(DownloadsFolder, "gr-satellites.zip");
      File.WriteAllBytes(zipPath, zipBytes);
      ExtractSatyaml(zipPath);
    }

    private void ExtractSatyaml(string zipPath)
    {
      string destDir = Path.Combine(DownloadsFolder, "satyaml");
      Directory.CreateDirectory(destDir);

      // start clean so a re-download does not leave stale .yml files behind
      foreach (string f in Directory.EnumerateFiles(destDir, "*.yml")) File.Delete(f);

      using ZipArchive archive = ZipFile.OpenRead(zipPath);
      int count = 0;
      foreach (ZipArchiveEntry entry in archive.Entries)
      {
        string full = entry.FullName.Replace('\\', '/');
        if (string.IsNullOrEmpty(entry.Name) ||
            !full.Contains("/python/satyaml/") ||
            !full.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
          continue;

        entry.ExtractToFile(Path.Combine(destDir, entry.Name), overwrite: true);
        count++;
      }
      Log.Information($"satyaml: extracted {count} .yml files");
    }




    //----------------------------------------------------------------------------------------------
    //                                import satellite data
    //----------------------------------------------------------------------------------------------
    private bool CheckFilesPresent()
    {
      return
        File.Exists(Path.Combine(DownloadsFolder, "satellites.json")) &&
        File.Exists(Path.Combine(DownloadsFolder, "transmitters.json")) &&
        File.Exists(Path.Combine(DownloadsFolder, "tle.json")) &&
        File.Exists(Path.Combine(DownloadsFolder, "JE9PEL.csv"));
    }
    public void ImportAll()
    {
      try
      {
        if (!CheckFilesPresent()) throw new Exception("Satellite download(s) missing");

        SatelliteList.Clear();

        ImportSatnogsSatellites();
        ImportSatnogsTransmitters();
        ImportSatnogsTle();
        ImportJE9PEL();

        // enrichment only — never let a satyaml problem abort the import
        try { ImportSatyaml(); }
        catch (Exception ex) { Log.Warning(ex, "satyaml import failed (enrichment skipped)"); }

        // local hand-curated overrides on top of satyaml — enrichment only, never abort the import
        try { ImportTransmitterOverrides(); }
        catch (Exception ex) { Log.Warning(ex, "transmitter overrides import failed (enrichment skipped)"); }

        var satNames = new SatelliteNames();

        foreach (var sat in Satellites)
        {
          if (sat.norad_cat_id != null)
          {
            sat.LotwName = satNames.Lotw.GetValueOrDefault(sat.norad_cat_id.Value);
            if (sat.LotwName != null) { sat.name = sat.LotwName; sat.names += ", " + sat.LotwName; }
          }

          sat.BuildAllNames();
          sat.SetFlags();
        }
        
        // remove satellites without transmitters
        var satellitesWithoutTransmitters = SatelliteList.Where(kvp => kvp.Value.Transmitters.Count == 0).ToList();
        foreach (var sat in satellitesWithoutTransmitters)
        {
          SatelliteList.Remove(sat.Key);
          Log.Debug($"Removed satellite {sat.Value.name} (ID: {sat.Key}) because it has no transmitters");
        }

        SaveToFile();
        loaded = SatelliteList.Count > 0;
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Error importing satellite list");
        throw;
      }
    }

    private void ImportSatnogsSatellites()
    {
      string json = File.ReadAllText(Path.Combine(DownloadsFolder, "satellites.json"));
      SatnogsDbSatelliteList satellites = JsonConvert.DeserializeObject<SatnogsDbSatelliteList>(json, JsonSettings)!;
      SatelliteList = satellites.ToDictionary(s => s.sat_id);
    }

    private void ImportSatnogsTransmitters()
    {
      string json = File.ReadAllText(Path.Combine(DownloadsFolder, "transmitters.json"));
      SatnogsDbTransmitterList transmitters = JsonConvert.DeserializeObject<SatnogsDbTransmitterList>(json, JsonSettings)!;

      // only transmitters with downlink frequency are imported.
      // currently 3 transmitters out of 2K+ have downlink_low=null: KOSEN-2, CHUBUSAT-2 and CHUBUSAT-3
      foreach (SatnogsDbTransmitter t in transmitters.Where(t => t.downlink_low.HasValue))
      {
        var sat = SatelliteList.GetValueOrDefault(t.sat_id);
        if (sat != null)
        {
          t.DownlinkMode = ModeMnemonic.ToModeName(t.mode_id, t.mode);
          sat.Transmitters.Add(t);
          t.Satellite = sat;
        }
      }
    }

    private void ImportSatnogsTle()
    {
      string json = File.ReadAllText(Path.Combine(DownloadsFolder, "tle.json"));
      SatnogsDbTleList tles = JsonConvert.DeserializeObject<SatnogsDbTleList>(json, JsonSettings)!;

      foreach (SatnogsDbTle tle in tles)
        if (SatelliteList.TryGetValue(tle.sat_id, out SatnogsDbSatellite sat))
          sat.Tle = tle;
    }

    // Match gr-satellites satyaml entries to transmitters by NORAD + nearest baud, attach gr_sats.
    private void ImportSatyaml()
    {
      string dir = Path.Combine(DownloadsFolder, "satyaml");
      SatyamlDb? satyaml = SatyamlDb.Load(dir);
      if (satyaml == null)
      {
        Log.Warning("satyaml folder not found; skipping gr-satellites enrichment");
        return;
      }

      int matched = 0;
      foreach (var sat in Satellites)
      {
        if (sat.norad_cat_id == null) continue;
        foreach (var tx in sat.Transmitters)
        {
          GrSatsInfo? info = satyaml.Find(sat.norad_cat_id.Value, tx.baud);
          if (info != null) { tx.gr_sats = info; matched++; }
        }
      }
      Log.Information($"satyaml: enriched {matched} transmitters");
    }

    // Apply hand-curated GrSatsInfo overrides from Data\transmitters-override.json into the authoritative
    // manual layer (tx.manual), which the resolver ranks above the satyaml enrichment. The file maps a
    // transmitter uuid to a partial GrSatsInfo object that lists only the fields to override; absent fields
    // are left untouched (PopulateObject merges in place). This is how we supply data the SatNOGS DB lacks
    // and gr-satellites has no satyaml entry for (e.g. FSK deviation), or correct wrong DB values (e.g. baud).
    private void ImportTransmitterOverrides()
    {
      string path = Path.Combine(DataFolder, "transmitters-override.json");
      if (!File.Exists(path)) return;

      var overrides = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(File.ReadAllText(path));
      if (overrides == null) return;

      // index every transmitter by uuid for a quick lookup
      var byUuid = new Dictionary<string, SatnogsDbTransmitter>();
      foreach (var sat in Satellites)
        foreach (var tx in sat.Transmitters)
          byUuid[tx.uuid] = tx;

      int applied = 0;
      foreach (var (uuid, fields) in overrides)
      {
        if (!byUuid.TryGetValue(uuid, out var tx))
        {
          Log.Warning($"transmitter override: uuid {uuid} not found in the imported transmitters");
          continue;
        }

        tx.manual ??= new GrSatsInfo();
        JsonConvert.PopulateObject(fields.ToString(), tx.manual);
        applied++;
      }
      Log.Information($"transmitter overrides: applied {applied} of {overrides.Count}");
    }

    // Merge the embedded default transmitters-override.json into the user-editable copy without clobbering
    // user edits. Records are keyed by transmitter uuid: user-only records are kept, new shipped records are
    // added, and a uuid present in both is refreshed from the shipped default unless the user has claimed it
    // with "read_only": true, in which case the user's copy is kept. The file is rewritten in shipped order,
    // with user-only records appended. read_only is a file-merge signal only and plays no part in resolution.
    private void MergeOverrideFile(string path)
    {
      var shipped = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(
        System.Text.Encoding.UTF8.GetString(Properties.Resources.transmitters_override)) ?? new();

      Dictionary<string, JObject> user = new();
      if (File.Exists(path))
        try { user = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(File.ReadAllText(path)) ?? new(); }
        catch (Exception ex) { Log.Warning($"transmitters-override.json is unreadable, refreshing from default: {ex.Message}"); }

      // shipped order first; a shipped record is refreshed unless the user has claimed it with read_only
      var merged = new Dictionary<string, JObject>();
      foreach (var (uuid, shippedRec) in shipped)
        merged[uuid] = user.TryGetValue(uuid, out var claimed) && claimed.Value<bool?>("read_only") == true
          ? claimed : shippedRec;

      // then append the records that exist only in the user file
      foreach (var (uuid, userRec) in user)
        if (!shipped.ContainsKey(uuid)) merged[uuid] = userRec;

      File.WriteAllText(path, JsonConvert.SerializeObject(merged, Formatting.Indented));
    }

    // example: RS-44;44909;145.935-145.995;435.670-435.610;435.605 ;SSB CW;RS44;Operational
    private void ImportJE9PEL()
    {
      Dictionary<int, List<JE9PELtransmitter>> dict = new();

      foreach (string line in File.ReadAllLines(Path.Combine(DownloadsFolder, "JE9PEL.csv")))
        try
        {
          JE9PELtransmitter tx = new(line);
          if (tx.NoradId == 0) continue;
          dict.TryAdd(tx.NoradId, new());
          dict[tx.NoradId].Add(tx);
        }
        catch (Exception ex)
        {
          Log.Error($"Error in JE9PEL.csv: {ex.Message} Line: '{line}'");
        }

      // insert in satnogs db
      foreach (var sat in Satellites)
        if (sat.norad_cat_id != null)
          if (dict.ContainsKey((int)sat.norad_cat_id))
          {
            sat.JE9PELtransmitters = dict[(int)sat.norad_cat_id];
            sat.JE9PEL_Callsigns = sat.JE9PELtransmitters.SelectMany(t => t.Call.Split(' ')).Where(c => c != "").Distinct().ToList();
            sat.JE9PEL_Names = sat.JE9PELtransmitters.SelectMany(t => t.Name.Split(' ')).Where(c => c != "").Distinct().ToList();
          }
    }

    internal void Customize(Dictionary<string, SatelliteCustomization> satelliteCustomizations)
    {
      foreach (var cust in satelliteCustomizations.Values)
      {
        if (cust.sat_id == null) continue;

        if (!SatelliteList.TryGetValue(cust.sat_id, out SatnogsDbSatellite? sat)) return;

        if (!string.IsNullOrEmpty(cust.Name))
        {
          sat.name = cust.Name;
          sat.BuildAllNames();
        }
      }
    }

    internal void LoadTleFromFile(string tleFileName)
    {
      Log.Information($"Loading TLE from file: {tleFileName}");

      string tleContent = File.ReadAllText(tleFileName);
      SatnogsDbTleList tles;

      try
      {
        if (Path.GetExtension(tleFileName).ToLower() == ".json")
          tles = JsonConvert.DeserializeObject<SatnogsDbTleList>(tleContent, JsonSettings)!;
        else
          tles = TlesFromText(tleContent);

        foreach (var tle in tles)
        {
          var sat = Satellites.FirstOrDefault(s => s.norad_cat_id == tle.norad_cat_id);
          if (sat != null) sat.Tle = tle;
        }

        SaveToFile();
        TleUpdated?.Invoke(this, EventArgs.Empty);
        Log.Information($"TLE loaded");
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Load TLE from file failed");
        throw;
      }
    }

    private SatnogsDbTleList TlesFromText(string tleContent)
    {
      string[] lines = tleContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
      if (lines.Length % 3 != 0)
        throw new ArgumentException("TLE file must have a multiple of 3 lines (name, line1, line2).");

      for (int i = 0; i < lines.Length; i += 3)
        if (lines[i].Length < 1 || lines[i + 1].Length < 69 || lines[i + 2].Length < 69 ||
          !lines[i + 1].StartsWith("1 ") || !lines[i + 2].StartsWith("2 "))
          throw new ArgumentException("Invalid TLE format detected.");

      SatnogsDbTleList tles = new();

      for (int i = 0; i < lines.Length; i += 3)
        tles.Add(new SatnogsDbTle
        {
          tle0 = lines[i].Trim(),
          tle1 = lines[i + 1].Trim(),
          tle2 = lines[i + 2].Trim(),
          tle_source = "Local file",
          updated = DateTime.Now,
          norad_cat_id = int.Parse(lines[i + 1].Substring(2, 5).Trim(), CultureInfo.InvariantCulture)
        });

      return tles;
    }
  }
}