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
    

    public string StatusString { get; set; } = string.Empty;
    public string BackColor { get; set; } = "#FFFFFF";
    public string ForeColor { get; set; } = "#000000";
    
    internal ulong TxFreq;

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

      if (Mode == "MFSK") entry["SUBMODE"] = "FT4";
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
    private readonly Context ctx;

    public LoggerInterface(Context ctx)
    {
      this.ctx = ctx;

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
      {
        Log.Information("QSO Saved.");
        ctx.RecorderPanel?.RememberQsoSaved(qso.Call);
      }
      else
        MessageBox.Show("Save QSO failed: " + result, "Save QSO", MessageBoxButtons.OK, MessageBoxIcon.Error);

        ctx.MainForm.BeginInvoke(() => { CheckSendAmsatStatus(qso.Utc); });
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

    (string satName, int orbit) LastSentInfo = ("", 0);
    private void CheckSendAmsatStatus(DateTime utc)
    {
      // do not post old qso
      var minutesAgo = (DateTime.UtcNow - utc).TotalMinutes;
      if (minutesAgo < 0 || minutesAgo > 30) return;

      // get info
      var sat = ctx.SatelliteSelector.SelectedSatellite;
      if (sat == null || sat.AmsatEntries.Count == 0) return;

      // a qso is usually entered in the last seconds of the pass or just after LOS, so the pass that just
      // ended still counts as the pass this qso was made on. only a pass that has not started yet is not
      var pass = ctx.HamPasses.GetCurrentOrNextPass(sat, SatellitePasses.LosGracePeriod);
      if (pass == null || pass.StartTime > DateTime.UtcNow) return;

      // ask only once
      var info = (sat.AmsatEntries[0], pass.OrbitNumber);
      if (info != LastSentInfo) AmsatReportDialog.SendReport(ctx, sat);
      LastSentInfo = info;
    }
  }
}
