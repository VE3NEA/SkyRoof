using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using System.Xml.Linq;
using MathNet.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using static VE3NEA.NativeSoapySdr;

namespace VE3NEA
{
  public class SoapySdrDevice : IDisposable
  {
    private IntPtr Device;
    private SoapySdrStream? Stream;
    private Thread? Thread;

    public readonly SoapySdrDeviceInfo Info;
    private bool Started;
    private bool Stopping;

    public double Frequency { get => Info.Frequency; set => SetFrequency(value); }
    public bool Enabled { get => enabled; set => SetEnabled(value); }
    public int NormalizedGain { get => GetNormalizedGain();  set => SetNormalizedGain(value); }
    public bool IsSingleGain => Info.Properties.GetByName("Single Gain")?.Value == "true";

    public bool CanChangeGain => IsSingleGain && Info.Properties.GetByName("AGC")?.Value != "true";

    private bool enabled;

    public event EventHandler? StateChanged;
    public event EventHandler<DataEventArgs<Complex32>>? DataAvailable;


    public SoapySdrDevice(SoapySdrDeviceInfo info)
    { 
      Info = info;
    }




    //----------------------------------------------------------------------------------------------
    //                                     start / stop
    //----------------------------------------------------------------------------------------------
    private void SetEnabled(bool value)
    {
      if (value == enabled) return;
      enabled = value;

      if (value)
      {
        TryStart(true);
      }
      else
      {
        Stop();
        Started = false;
        StateChanged?.Invoke(this, EventArgs.Empty);
      }
    }

    private void TryStart(bool logErrors)
    {
      if (logErrors) Log.Information($"Starting SDR: '{Info.Name}' {JsonConvert.SerializeObject(Info)}");

      try
      {
        Start();
        Started = true;
        Log.Information($"SDR started: {Info?.Name}");
        StateChanged?.Invoke(this, EventArgs.Empty);
      }
      catch (Exception ex)
      {
        Stop();
        Started = false;
        if (logErrors) Log.Error(ex, $"Error starting {Info?.Name}");
      }
    }

    public void Retry()
    {
      if (IsRunning()) return;

      // was started but is no longer running
      if (Started) 
      {
        Started = false;
        Log.Error($"{Info.Name} failed, restarting");
        StateChanged?.Invoke(this, EventArgs.Empty);
      }

      if (Enabled && !Started) TryStart(false);
    }

    public bool IsRunning()
    {
      return Device != IntPtr.Zero; 
      //Thread?.IsAlive ?? false;
    }




    //----------------------------------------------------------------------------------------------
    //                                     worker thread
    //----------------------------------------------------------------------------------------------
    public void Start()
    {      
      if (!SoapySdr.DeviceExists(Info.KwArgs)) 
        throw new Exception($"Device {Info.Name} is no longer available");

      Device = SoapySdr.CreateDevice(Info.KwArgs);
      SetAllParams();

      Thread = new Thread(new ThreadStart(ThreadProcedure));
      Thread.IsBackground = true;
      Thread.Name = GetType().Name;
      Stopping = false;
      Thread.Start();
      Thread.Priority = ThreadPriority.Highest;
    }

    public void Stop()
    {
      if (!IsRunning()) return;

      Stopping = true;
      Thread?.Join();
      Thread = null;
      Device = IntPtr.Zero;
    }

    private void ThreadProcedure()
    {
      Log.Information($"Starting SDR Read thread");

      Stream = new(Device);

      while (!Stopping)
        try
        {
          Stream.ReadStream();
          Stream.Args.Utc = DateTime.UtcNow;
          DataAvailable?.Invoke(this, Stream.Args);
        }
        catch (Exception ex)
        {
          Log.Error(ex, $"{Info.Name} read failed");
          break;
        }

      Log.Information($"Terminating SDR Read thread");
      Stream.Dispose();
      Stream = null;
      SoapySdr.ReleaseDevice(Device);
      Device = IntPtr.Zero;

      StateChanged?.Invoke(this, EventArgs.Empty);
    }




    //----------------------------------------------------------------------------------------------
    //                                     set params
    //----------------------------------------------------------------------------------------------
    private void SetAllParams()
    {
      SdrProperty? setting;      

      // settings
      var settings = Info.Properties.Where(p => !string.IsNullOrEmpty(p.ArgInfo.Key));
      foreach (var sett in settings)
      {
        SoapySDRDevice_writeSetting(Device, sett.ArgInfo.Key, sett.Value);
        SoapySdr.CheckError();
      }

      // antenna
      setting = Info.Properties.First(p => p.Name == "Antenna");
      SoapySDRDevice_setAntenna(Device, Direction.Rx, 0, setting.Value);
      SoapySdr.CheckError();

      // DC
      setting = Info.Properties.FirstOrDefault(p => p.Name == "DCOffsetMode");
      if (setting != null)
      {
        SoapySDRDevice_setDCOffsetMode(Device, Direction.Rx, 0, setting.Value == "true");
        SoapySdr.CheckError();
      }

      // IQ
      setting = Info.Properties.FirstOrDefault(p => p.Name == "IQBalanceMode");
      if (setting != null)
      {
        SoapySDRDevice_setIQBalanceMode(Device, Direction.Rx, 0, setting.Value == "true");
        SoapySdr.CheckError();
      }

      // AGC
      setting = Info.Properties.FirstOrDefault(p => p.Name == "AGC");
      if (setting != null)
      {
        SoapySDRDevice_setGainMode(Device, Direction.Rx, 0, setting.Value == "true");
        SoapySdr.CheckError();
      }

      // gains

      

      if (IsSingleGain)
        SetGain(Info.Gain);
      else
      {
        var gainSettings = Info.Properties.Where(p => p.Category == "Stage Gains");
        foreach (var sett in gainSettings)
        {
          SoapySDRDevice_setGainElement(Device, Direction.Rx, 0, sett.Name, double.Parse(sett.Value));
          SoapySdr.CheckError();
        }
      }

      // non-browsable 

      SoapySDRDevice_setSampleRate(Device, Direction.Rx, 0, Info.SampleRate);
      SoapySdr.CheckError();
      
      SoapySDRDevice_setBandwidth(Device, Direction.Rx, 0, Info.HardwareBandwidth);
      SoapySdr.CheckError();

      SetFrequency(Frequency);
      
    }

    public bool IsFrequencySupported(double frequency)
    {
      return Info.FrequencyRange.Any(r => frequency >= r.minimum && frequency <= r.maximum);
    }

    private void SetFrequency(double frequency)
    {
      if (IsFrequencySupported(frequency))
      {
        Info.Frequency = frequency;

        if (Device != IntPtr.Zero)
        {
          double correctedFrequency = frequency * (1 + Info.Ppm * 1e-6);
          SoapySDRDevice_setFrequency(Device, Direction.Rx, 0, correctedFrequency, IntPtr.Zero);
          SoapySdr.CheckError();
        }
      }
      else
        Log.Error($"Attempted to set an invalid frequency for {Info.Name}: {frequency} Hz");
    }

    private void SetGain(double value)
    {
      Info.Gain = (float)Math.Min(Info.GainRange.maximum, Math.Max(Info.GainRange.minimum, value));

      if (CanChangeGain && Device != IntPtr.Zero)
      {
        SoapySDRDevice_setGain(Device, Direction.Rx, 0, Info.Gain);
        SoapySdr.CheckError();
      }
    }

    private void SetNormalizedGain(int value)
    {
      double gain = Info.GainRange.minimum + value / 100d * (Info.GainRange.maximum - Info.GainRange.minimum);
      SetGain(gain);
    }

    private int GetNormalizedGain()
    {
      double normalized = (Info.Gain - Info.GainRange.minimum) / (Info.GainRange.maximum - Info.GainRange.minimum);
      return (int)Math.Round(100 * normalized);
    }





    //----------------------------------------------------------------------------------------------
    //                                     IDispose
    //----------------------------------------------------------------------------------------------
    public void Dispose()
    {
      Stop();
    }
  }
}
