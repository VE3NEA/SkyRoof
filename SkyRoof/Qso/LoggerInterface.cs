using System.Runtime.InteropServices;
using Serilog;
using VE3NEA;

namespace SkyRoof
{
  public class QsoInfo
  {
    public string StationCallsign { get; set; } = string.Empty;
    public string MyGridSquare{ get; set; } = string.Empty;
    public DateTime Utc { get; set; } = DateTime.UtcNow;
    public string Call { get; set; } = string.Empty;
    public string Band { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;

    public string Sat { get; set; } = string.Empty;
    public string Grid { get; set; }= string.Empty;
    public string State { get; set; } = string.Empty;
    public string Sent { get; set; }= string.Empty;
    public string Recv { get; set; }= string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    

    public string StatusString = string.Empty;
    public string BackColor = "#FFFFFF";
    public string ForeColor = "#000000";
    
    internal AdifEntry ToAdifEntry()
    {
      AdifEntry entry = new() {
        ["STATION_CALLSIGN"] = StationCallsign,
        ["MY_GRIDSQUARE"] = MyGridSquare,
        ["QSO_DATE"] = Utc.ToString("yyyyMMdd"),
        ["TIME_ON"] = Utc.ToString("HHmmss"),
        ["CALL"] = Call,
        ["BAND"] = Band,
        ["MODE"] = Mode,
      };

      if (Grid != "") entry["GRIDSQUARE"] = Grid;
      if (State != "") entry["STATE"] = State;
      if (Sent != "") entry["RST_SENT"] = Sent;
      if (Recv != "") entry["RST_RCVD"] = Recv;
      if (Name != "") entry["NAME"] = Name;
      if (Notes != "") entry["COMMENT"] = Notes;
      if (Sat != "") entry["SAT_NAME"] = Sat;
      if (Sat != "") entry["PROP_MODE"] = "SAT";

      return entry;
    }
  }

  public static class LoggerInterfaceDll
  {
    public const string DllName = "LoggerInterface.dll";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Init();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern string SaveQso(string json);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern string Augment(string json);
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern string GetStatus(string json);
  }

  public class LoggerInterface
  {
    private bool DllAvailable;
    private AdifLogger? AdifLogger;

    public LoggerInterface(Context ctx)
    {
      try
      {
        LoggerInterfaceDll.Init();
        DllAvailable = true;
        Log.Information("LoggerInterface DLL loaded successfully.");
      }
      catch (Exception ex)
      {
        Log.Warning(ex, "Unable to load LoggerInterface.dll. Using AdifLogger.");
        DllAvailable = false;
        AdifLogger = new(ctx.Settings.QsoEntry.NewFileEvery);
      }
    }

    public void SaveQso(QsoInfo qso)
    {
      string json = System.Text.Json.JsonSerializer.Serialize(qso);
      Log.Information($"Saving QSO: {json}");

      string result = DllAvailable ? SaveQsoUsingDll(json) : SaveQsoToAdif(qso);

      if (string.IsNullOrEmpty(result))
        Log.Information("QSO Saved.");
      else
        MessageBox.Show("Save QSO failed: " + result, "Save QSO", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    public QsoInfo Augment(QsoInfo qso)
    {
      if (string.IsNullOrEmpty(qso.Call) || qso.Call.Trim().Length < 3) return qso;

      string json = System.Text.Json.JsonSerializer.Serialize(qso);

      try
      {
        if (DllAvailable)
        {
          json = LoggerInterfaceDll.Augment(json);
          return System.Text.Json.JsonSerializer.Deserialize<QsoInfo>(json)!;
        }
        else
          return AdifLogger!.Augment(qso);
      }
      catch (Exception ex)
      {
        Log.Error(ex, $"Error augmenting QSO {json}.");
        return qso;
      }
    }
    public QsoInfo GetStatus(QsoInfo qso)
    {
      if (string.IsNullOrEmpty(qso.Call) || qso.Call.Trim().Length < 3) return qso;

      string json = System.Text.Json.JsonSerializer.Serialize(qso);

      try
      {
        if (DllAvailable)
        {
          json = LoggerInterfaceDll.GetStatus(json);
          return System.Text.Json.JsonSerializer.Deserialize<QsoInfo>(json)!;
        }
        else
          return AdifLogger!.GetStatus(qso);
      }
      catch (Exception ex)
      {
        Log.Error(ex, $"Error getting QSO status {json}.");
        return qso;
      }
    }

    private string SaveQsoUsingDll(string json)
    {
      try
      {
        return LoggerInterfaceDll.SaveQso(json);
      }
      catch (Exception ex)
      {
        return ex.Message;
      }
    }

    private string SaveQsoToAdif(QsoInfo qso)
    {
      try
      {
        AdifLogger!.SaveQso(qso);
        return string.Empty;
      }
      catch (Exception ex)
      {
        return ex.Message;
      }
    }
  }
}
