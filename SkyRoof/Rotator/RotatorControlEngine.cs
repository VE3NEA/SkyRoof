using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using SkyRoof;
using VE3NEA;

namespace SkyRoof
{
  public class RotatorControlEngine : ControlEngine
  {
    public volatile Bearing? RequestedBearing, LastReadBearing, LastWrittenBearing;
    private volatile bool stopRequested = false;

    public event EventHandler? BearingChanged;

    public RotatorControlEngine(RotatorSettings settings) : base(settings.Host, settings.Port, settings)
    {
      StartThread();
    }

    protected override bool Setup()
    {
      return true;
    }

    public void RotateTo(Bearing bearing)
    {
      stopRequested = false;
      RequestedBearing = bearing;
    }

    private void OnBearingChanged()
    {
      syncContext.Post(s => BearingChanged?.Invoke(this, EventArgs.Empty), null);
    }

    // UI-thread safe: sets a request flag and returns. The processing thread
    // sends the S command from Cycle() so no socket I/O ever runs on the caller.
    public void StopRotation()
    {
      RequestedBearing = LastWrittenBearing = null;
      stopRequested = true;
    }

    protected override void Cycle()
    {
      if (TcpClient == null || !TcpClient.Connected) return;

      if (stopRequested)
      {
        SendWriteCommand("S");
        stopRequested = false;
        return;
      }

      WriteBearing();
      ReadBearing();
    }

    private void WriteBearing()
    {
      if (RequestedBearing == LastWrittenBearing) return;

      try
      {
        SendWriteCommand($"P {RequestedBearing!.AzDeg.ToString("F1", CultureInfo.InvariantCulture)} " + 
          $"{RequestedBearing.ElDeg.ToString("F1", CultureInfo.InvariantCulture)}");
      }
      catch (Exception ex)
      {
        Log.Error(ex, $"Error sending rotator position command.");
      }

      LastWrittenBearing = RequestedBearing;
    }

    private void ReadBearing()
    {
      string? reply = null;
      try
      {
        reply = SendReadCommand("p");
      }
      catch (Exception ex)
      {
        Log.Error(ex, $"Error sending rotator position read command.");
      }
      if (reply == null) return;

      var parts = reply.Trim().Split('\n');
      if(parts.Length == 1) 
        parts = (reply + ReadLine()).Trim().Split('\n');
      if (log) Log.Information($"Rotator reply parsed: {string.Join('|', parts)}");
      if (parts.Length != 2) { BadReply(reply); return; }

      if (!double.TryParse(parts[0], CultureInfo.InvariantCulture, out double azimuth)) { BadReply(reply); return; }
      if (!double.TryParse(parts[1], CultureInfo.InvariantCulture, out double elevation)) { BadReply(reply); return; }

      // Convert degrees from the protocol to radians for our Bearing class
      var bearing = new Bearing(
        azimuth * Math.PI / 180.0, 
        elevation * Math.PI / 180.0
      );
      
      if (bearing == LastReadBearing) return;

      LastReadBearing = bearing;
      OnBearingChanged();
    }
  }
}
