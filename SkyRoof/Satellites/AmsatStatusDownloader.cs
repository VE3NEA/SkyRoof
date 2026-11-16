using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using VE3NEA;


namespace SkyRoof
{
  // AMSAT status of one transmitter entry of a satellite, e.g. ISS_[SSTV]
  public class AmsatTxStatus
  {
    public string Mode = "";
    public bool Active;
    // newest report of the winning kind (active or inactive)
    public DateTime LatestTime;
    // Heard + Crew Active reports in the summary window
    public int ActiveCount;
  }

  public class AmsatStatusLoader
  {
    private const string ApiUrl = "https://status.amsat.org/api/v1/";
    private const int SummaryHours = 48;
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan BackoffInterval = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan CatalogMaxAge = TimeSpan.FromHours(24);
    private static readonly TimeSpan StatusMaxAge = TimeSpan.FromHours(2);
    // the status page groups reports in 2-hour UTC windows
    private static readonly long WindowTicks = TimeSpan.FromHours(2).Ticks;
    private static readonly string[] ActiveReports = ["Heard", "Crew Active"];
    private static readonly Regex TupleRx = new(@"^(.*?)_\[([^\]]*)\]$");

    private static readonly HttpClient Client = CreateClient();

    // transmitter statuses per NORAD id
    public readonly Dictionary<int, List<AmsatTxStatus>> Statuses = [];
    public DateTime? LastSuccessTime;
    public string? LastError;
    public Context ctx;

    // API names of all transmitter entries, e.g. ISS_[SSTV]
    private List<string>? Catalog;
    private DateTime NextAttemptTime = DateTime.MinValue;
    private bool busy;

    private class ApiSatellite
    {
      [JsonProperty("name")] public string Name { get; set; } = "";
    }

    private class ApiSummaryEntry
    {
      [JsonProperty("name")] public string Name { get; set; } = "";
      [JsonProperty("report")] public string Report { get; set; } = "";
      [JsonProperty("report_count")] public int ReportCount { get; set; }
      [JsonProperty("latest_reported_time")] public DateTime? LatestReportedTime { get; set; }
    }

    private class ApiResponse<T> where T : class
    {
      [JsonProperty("data")] public T? Data { get; set; }
    }

    private class ApiException(string message, HttpStatusCode status) : Exception(message)
    {
      public readonly HttpStatusCode Status = status;
    }

    private static HttpClient CreateClient()
    {
      var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
      client.DefaultRequestHeaders.UserAgent.ParseAdd($"SkyRoof/{Utils.GetVersionNumber()} (+https://github.com/VE3NEA/SkyRoof)");
      return client;
    }

    private string CatalogFilePath => Path.Combine(Utils.GetUserDataFolder(), "Downloads", "amsat_catalog.json");

    public bool Expired => LastSuccessTime == null || DateTime.UtcNow - LastSuccessTime > StatusMaxAge;




    // ----------------------------------------------------------------------------------------------------
    //                                          status download
    // ----------------------------------------------------------------------------------------------------
    // force: ignore the 15-minute schedule and re-download the satellite list
    public async Task<bool> GetStatusesAsync(bool force = false)
    {
      if (!ctx.Settings.Amsat.Enabled || ctx.GroupViewPanel == null) return true;
      if (!force && DateTime.UtcNow < NextAttemptTime) return true;

      // guard against overlapping runs (startup + timer tick + menu) interleaving their
      // mutations of the shared satellite collection
      if (busy) return true;
      busy = true;
      try
      {
        List<string> names;
        List<ApiSummaryEntry> summary;

        // do all awaits up front: while suspended here the message pump (incl. a modal dialog)
        // runs, but no shared state is touched yet
        try
        {
          names = await GetCatalogAsync(force);
          string json = await GetJsonAsync($"summary.php?hours={SummaryHours}");
          summary = ParseData<List<ApiSummaryEntry>>(json);
        }
        catch (Exception ex)
        {
          SetError(ex);
          return false;
        }

        // apply synchronously, with no awaits in between, so the message pump cannot interleave
        // these satellite-collection mutations and the list update with other UI work
        try
        {
          if (!ReferenceEquals(names, Catalog))
          {
            Catalog = names;
            ApplyCatalog();
          }
          ParseSummary(summary);
        }
        catch (Exception ex)
        {
          SetError(ex);
          return false;
        }

        if (LastError != null) Log.Information("AMSAT status download recovered");
        LastSuccessTime = DateTime.UtcNow;
        LastError = null;
        NextAttemptTime = LastSuccessTime.Value + UpdateInterval;
        return true;
      }
      finally
      {
        busy = false;
        ctx.GroupViewPanel?.ShowAmsatStatuses();
        ctx.MainForm.ShowSatDataStatus();
      }
    }

    // called once a minute: clears expired statuses and downloads new ones when due
    public void OneMinuteTick()
    {
      if (Statuses.Count > 0 && Expired)
      {
        Statuses.Clear();
        ctx.GroupViewPanel?.ShowAmsatStatuses();
        ctx.MainForm.ShowSatDataStatus();
      }

      GetStatusesAsync().DoNotAwait();
    }

    private void SetError(Exception ex)
    {
      // log once per failure streak, the download is retried every 15 minutes
      if (LastError == null) Log.Error(ex, "AMSAT status download failed");
      LastError = ex.Message;

      bool backoff = ex is ApiException apiEx &&
        (apiEx.Status == HttpStatusCode.TooManyRequests || apiEx.Status == HttpStatusCode.ServiceUnavailable);
      NextAttemptTime = DateTime.UtcNow + (backoff ? BackoffInterval : UpdateInterval);
    }

    private async Task<List<string>> GetCatalogAsync(bool force)
    {
      string path = CatalogFilePath;
      bool fresh = File.Exists(path) && DateTime.UtcNow - File.GetLastWriteTimeUtc(path) < CatalogMaxAge;
      if (!force && fresh) return Catalog ?? ParseCatalog(File.ReadAllText(path));

      try
      {
        string json = await GetJsonAsync("catalog.php");
        var names = ParseCatalog(json);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);
        return names;
      }
      catch (Exception ex) when (File.Exists(path))
      {
        // a stale satellite list is better than none
        Log.Warning(ex, "AMSAT satellite list download failed, using the cached copy");
        return Catalog ?? ParseCatalog(File.ReadAllText(path));
      }
    }

    private List<string> ParseCatalog(string json)
    {
      var names = ParseData<List<ApiSatellite>>(json).Select(s => s.Name).Where(n => n != "").ToList();
      if (names.Count == 0) throw new Exception("0 satellites in the AMSAT satellite list");
      return names;
    }

    private static async Task<string> GetJsonAsync(string path)
    {
      using var response = await Client.GetAsync(ApiUrl + path);
      string json = await response.Content.ReadAsStringAsync();
      if (!response.IsSuccessStatusCode) throw MakeApiException(response.StatusCode, json);
      return json;
    }

    private static T ParseData<T>(string json) where T : class
    {
      try
      {
        return JsonConvert.DeserializeObject<ApiResponse<T>>(json)?.Data ?? throw new Exception("no data in the AMSAT reply");
      }
      catch
      {
        File.WriteAllText(Path.Combine(Utils.GetUserDataFolder(), "Downloads", "amsat_status_error.json"), json);
        throw;
      }
    }

    // the API reports errors as {"error":{"code","message"}}
    private static ApiException MakeApiException(HttpStatusCode status, string json)
    {
      string? message = null;
      try { message = JObject.Parse(json)["error"]?["message"]?.Value<string>(); }
      catch { }

      return new ApiException(message ?? $"HTTP error {(int)status} {status}", status);
    }




    // ----------------------------------------------------------------------------------------------------
    //                                              parsing
    // ----------------------------------------------------------------------------------------------------
    // maps the AMSAT entries to the satellites; also called after the satellite list is re-imported
    public void ApplyCatalog()
    {
      if (Catalog == null) return;

      try
      {
        // clear old entries
        foreach (var sat in ctx.SatnogsDb.Satellites) sat.AmsatEntries = [];

        // add amsat names to the sat
        foreach (var entry in Catalog)
        {
          var name = SatnogsDbSatellite.MakeSearchText(TupleRx.Match(entry).Groups[1].Value);
          var sats = ctx.SatnogsDb.Satellites.Where(s => s.SearchText.Contains($"|{name}|")).ToArray();

          if (sats.Length == 1)
            sats[0].AmsatEntries.Add(entry);
          else
            Log.Warning($"{sats.Length} satellites found for AMSAT label {name}");
        }

        // rebuild AllNames for the sat
        foreach (var sat in ctx.SatnogsDb.Satellites) sat.BuildAllNames();
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Error mapping AMSAT satellite names");
      }
    }

    private void ParseSummary(List<ApiSummaryEntry> summary)
    {
      Statuses.Clear();

      foreach (var group in summary.Where(e => e.LatestReportedTime != null).GroupBy(e => e.Name))
      {
        var sat = ctx.SatnogsDb.Satellites.FirstOrDefault(s => s.AmsatEntries.Contains(group.Key));
        if (sat?.norad_cat_id == null) continue;

        var active = group.Where(e => ActiveReports.Contains(e.Report)).ToArray();
        var inactive = group.Where(e => !ActiveReports.Contains(e.Report)).ToArray();
        DateTime? lastActive = active.Max(e => e.LatestReportedTime);
        DateTime? lastInactive = inactive.Max(e => e.LatestReportedTime);

        // conflicting reports in the latest window, the status page shows no status either
        if (lastActive != null && lastInactive != null &&
          lastActive.Value.Ticks / WindowTicks == lastInactive.Value.Ticks / WindowTicks) continue;

        bool isActive = lastActive != null && (lastInactive == null || lastActive > lastInactive);

        var status = new AmsatTxStatus
        {
          Mode = MakeModeLabel(group.Key),
          Active = isActive,
          LatestTime = (isActive ? lastActive : lastInactive)!.Value,
          ActiveCount = active.Sum(e => e.ReportCount),
        };

        int noradId = sat.norad_cat_id.Value;
        if (!Statuses.TryGetValue(noradId, out var list)) Statuses[noradId] = list = [];
        list.Add(status);
      }
    }

    // ISS_[UHF_Digi] -> UHF Digi
    private static string MakeModeLabel(string name)
    {
      var match = TupleRx.Match(name);
      return (match.Success ? match.Groups[2].Value : name).Replace('_', ' ');
    }

    // true if at least one transmitter is active, null if no status
    public bool? IsActive(int noradId)
    {
      if (!Statuses.TryGetValue(noradId, out var list)) return null;
      return list.Any(t => t.Active);
    }




    // ----------------------------------------------------------------------------------------------------
    //                                            submission
    // ----------------------------------------------------------------------------------------------------
    // returns null on success, otherwise the error message
    public async Task<string?> SendAmsatStatusAsync(string satName, string report)
    {
      try
      {
        // build message
        var now = DateTime.UtcNow - TimeSpan.FromMinutes(1); // margin for time error on the server
        var body = new JObject
        {
          ["name"] = satName,
          ["report"] = report,
          ["callsign"] = ctx.Settings.User.Call.ToUpper(),
          ["reported_at"] = now.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
        };
        string square = ctx.Settings.User.Square.ToUpper();
        if (square != "") body["grid_square"] = square;
        string bodyText = body.ToString(Formatting.None);

        // send
        using var content = new StringContent(bodyText, Encoding.UTF8, "application/json");
        using var response = await Client.PostAsync(ApiUrl + "reports.php", content);
        string json = await response.Content.ReadAsStringAsync();

        // log
        Log.Information($"AMSAT request: {bodyText}");
        Log.Information($"AMSAT reply: {Regex.Replace(json, @"\s+", " ")}");

        if (!response.IsSuccessStatusCode) throw MakeApiException(response.StatusCode, json);

        if (JObject.Parse(json)["data"]?["correction"]?.Value<bool>() == true)
          Log.Information("AMSAT report replaced an earlier one from the same 15-minute period");

        return null; // no error
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Failed to send AMSAT status");
        return ex.Message;
      }
    }
  }
}
