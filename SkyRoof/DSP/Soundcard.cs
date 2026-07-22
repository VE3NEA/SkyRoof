using System.Runtime.InteropServices;
using CSCore;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using CSCore.SoundOut;
using CSCore.Streams;
using MathNet.Numerics;
using Serilog;

namespace VE3NEA
{
  public class AudioDeviceEntry
  {
    public string Id, Name;
    public AudioDeviceEntry(string id, string name) { Id = id; Name = name; }
  }




  //-----------------------------------------------------------------------------------------------
  //                                     base class
  //-----------------------------------------------------------------------------------------------

  // <T> is float or Complex32

  public abstract class Soundcard : IDisposable
  {
    protected enum SoundcardState { Stopped, Starting, Running, Stopping }


    protected const int DEFAULT_SAMPLING_RATE = 48_000;
    protected readonly int SamplingRate;
    protected MMDevice? mmDevice;
    protected bool enabled;
    private System.Timers.Timer? Timer;
    protected SoundcardState State = SoundcardState.Stopped;

    public bool Retry;
    public bool Enabled { get => enabled; set => SetEnabled(value); }
    public bool IsRunning => State == SoundcardState.Running;
    
    public event EventHandler? StateChanged;
   

    public Soundcard(string? audioDeviceId = null, int? samplingRate = null)
    {
      SamplingRate = samplingRate ?? DEFAULT_SAMPLING_RATE;
      SetDeviceId(audioDeviceId);
    }




    //-----------------------------------------------------------------------------------------------
    //                                     start / stop
    //-----------------------------------------------------------------------------------------------
    private void SetEnabled(bool value)
    {
      if (value == Enabled) return;
      enabled = value;

      if (value) Start(); else Stop();
    }

    protected void Start()
    {
      if (State != SoundcardState.Stopped) return;
      State = SoundcardState.Starting;

      try
      {
        if (mmDevice?.DeviceState != DeviceState.Active)
          throw new Exception($"Audio device not active: {GetDisplayName()}");

        DoStart();
        State = SoundcardState.Running;
        OnStateChanged();
      }
      catch (Exception e)
      {
        Log.Error(e, $"Error starting {GetType().Name}");
        Stop();
      }
    }

    protected void Stop()
    {
      if (State == SoundcardState.Stopped) return;
      State = SoundcardState.Stopping;

      DoStop();
      Cleanup();

      State = SoundcardState.Stopped;
      OnStateChanged();
    }

    protected void OnStateChanged()
    {
      StateChanged?.Invoke(this, EventArgs.Empty);

      EnableRetry(Retry && Enabled && !IsRunning);
    }

    protected void Soundcard_Stopped(object? sender, StoppedEventArgs e)
    {
      if (!IsCurrentSoundcard(sender)) return;
      if (State == SoundcardState.Stopped || State == SoundcardState.Stopping) return;

      ThreadPool.QueueUserWorkItem(_ => Stop());
    }




    //-----------------------------------------------------------------------------------------------
    //                                        retry
    //-----------------------------------------------------------------------------------------------
    private void EnableRetry(bool value)
    {
      if (Timer != null)
      {
        Timer.Elapsed -= Timer_Elapsed;
        Timer.Stop();
        Timer = null;
      }

      if (value)
      {
        Timer = new();
        Timer.Interval = 3000;
        Timer.Elapsed += Timer_Elapsed;
        Timer.Start();
      }
    }

    private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
      Start();
      EnableRetry(!IsRunning);
    }




    //-----------------------------------------------------------------------------------------------
    //                                        get / set
    //-----------------------------------------------------------------------------------------------
    public void SetDeviceId(string? deviceId)
    {
      //if (deviceId == mmDevice?.DeviceID) return;

      bool wasEnabled = enabled;
      Enabled = false;

      if (deviceId == null) return;

      using (var deviceEnumerator = new MMDeviceEnumerator())
      {
        try 
        { 
          mmDevice = deviceEnumerator.GetDevice(deviceId); 
        } 
        catch (Exception ex)
        {
          Log.Error(ex, "Error setting soundcard device ID");
          return; 
        }
      }

      Enabled = wasEnabled;
    }

    internal string GetDisplayName()
    {
      try
      {
        return mmDevice?.FriendlyName ?? "None Selected";
      }
      catch
      {
        // when the device is disconnected, we are not notified about this,
        // but some internal variable in MMDevice becomes null
        // then a call to FriendlyName throws an exception
        return "Device Failed";
      }
    }




    //-----------------------------------------------------------------------------------------------
    //                                     device list
    //-----------------------------------------------------------------------------------------------
    
    //TODO: return Dictionary instead of array
    public static AudioDeviceEntry[] ListDevices(DataFlow direction)
    {
      using (var deviceEnumerator = new MMDeviceEnumerator())
      using (var deviceCollection = deviceEnumerator.EnumAudioEndpoints(direction, DeviceState.Active))
        return deviceCollection.Select(s => new AudioDeviceEntry(s.DeviceID, s.FriendlyName)).ToArray();
    }

    public static string? GetDefaultSoundcardId(DataFlow direction)
    {
      try
      {
        using (var deviceEnumerator = new MMDeviceEnumerator())
          return deviceEnumerator.GetDefaultAudioEndpoint(direction, Role.Multimedia)?.DeviceID;
      }
      catch (Exception ex)
      {
        Log.Error(ex, $"Default {direction} audio device not found.");
        return null;
      }
}

    public static string? GetFirstVacId(DataFlow direction)
    {
      using (var deviceEnumerator = new MMDeviceEnumerator())
      using (var deviceCollection = deviceEnumerator.EnumAudioEndpoints(direction, DeviceState.Active))
        return deviceCollection.FirstOrDefault(d => d.FriendlyName.Contains("Virtual"))?.DeviceID;
    }



    public void Dispose()
    {
      Enabled = false;
    }

    protected abstract void DoStart();
    protected abstract void DoStop();
    protected abstract void Cleanup();
    protected abstract bool IsCurrentSoundcard(object? sender);
  }




  //-----------------------------------------------------------------------------------------------
  //                                     output soundcard
  //-----------------------------------------------------------------------------------------------
  public class OutputSoundcard<T> : Soundcard
  {
    private WaveSource<T> waveSource;
    private WasapiOut? wasapiOut;
    private float volume;

    public RingBuffer<T> Buffer => waveSource.Buffer;
    public float Volume { get => volume; set => SetVolume(value); }


    public OutputSoundcard(string? audioDeviceId = null, int? samplingRate = null) 
      : base(audioDeviceId, samplingRate)
    {
      waveSource = new WaveSource<T>(SamplingRate);
    }

    protected override bool IsCurrentSoundcard(object? sender)
    {
      return ReferenceEquals(sender, wasapiOut);
    }

    protected override void DoStart()
    {
        waveSource.Buffer.Clear();

        wasapiOut = new WasapiOut(false, AudioClientShareMode.Shared, 200);
        wasapiOut.Device = mmDevice;
        wasapiOut.Initialize(waveSource);
        wasapiOut.Volume = volume;
        wasapiOut.Stopped += Soundcard_Stopped;
        wasapiOut.Play();
    }

    protected override void DoStop()
    {
      try { wasapiOut?.Stop(); } catch { }
    }

    protected override void Cleanup()
    {
      if (wasapiOut != null)
      {
        wasapiOut.Stopped -= Soundcard_Stopped;
        wasapiOut.Dispose();
        wasapiOut = null;
      }

      waveSource.Buffer.Clear();
    }

    private void SetVolume(float value)
    {
      volume = value;
      if (wasapiOut != null) wasapiOut.Volume = value;
    }

    public void AddSamples(T[] samples, int offset = 0, int? count = null)
    {
      if (Enabled) waveSource.AddSamples(samples, offset, count);
    }
  }




  //-----------------------------------------------------------------------------------------------
  //                                     input soundcard
  //-----------------------------------------------------------------------------------------------
  public class InputSoundcard<T> : Soundcard
  {
    private WasapiCapture? soundIn;
    private ISampleSource? SampleSource;
    private DataEventArgsPool<float> ArgsPool = new();

    private Thread? ReaderThread;
    private volatile bool stopping;

    public event EventHandler<DataEventArgs<float>>? SamplesAvailable;

    public InputSoundcard(string? audioDeviceId = null, int? samplingRate = null)
      : base(audioDeviceId)
    {
    }

    protected override bool IsCurrentSoundcard(object? sender)
    {
      return ReferenceEquals(sender, soundIn);
    }

    protected override void DoStart()
    {
      int channelCount = typeof(T) == typeof(Complex32) ? 2 : 1;
      WaveFormat format = new WaveFormat(SamplingRate, 32, channelCount, AudioEncoding.IeeeFloat);

      soundIn = new WasapiCapture(false, AudioClientShareMode.Shared, 200, format);
      soundIn.Device = mmDevice;
      soundIn.Initialize();

      SampleSource = new SoundInSource(soundIn).ToSampleSource();

      // in the shared mode the number of channels is not under our control, convert locally
      if (SampleSource.WaveFormat.Channels > channelCount) SampleSource = SampleSource.ToMono();
      else if (SampleSource.WaveFormat.Channels < channelCount) SampleSource = SampleSource.ToStereo();

      SampleSource = SampleSource.ChangeSampleRate(SamplingRate);
      
      soundIn.Stopped += Soundcard_Stopped;
      soundIn.Start();

      StartReaderThread();
    }

    protected override void DoStop()
    {
      try { soundIn?.Stop(); } catch { }
    }

    protected override void Cleanup()
    {
      StopReaderThread();

      if (soundIn != null)
      {
        soundIn.Stopped -= Soundcard_Stopped;
        soundIn.Dispose();
        soundIn = null;
      }

      SampleSource = null;
    }

    private void StartReaderThread()
    {
      stopping = false;

      ReaderThread = new Thread(ReaderLoop)
      {
        IsBackground = true,
        Name = "InputSoundcardReader"
      };

      ReaderThread.Start();
    }

    private void StopReaderThread()
    {
      stopping = true;
      ReaderThread?.Join();
      ReaderThread = null;
    }

    private const int blockSize = 4800;
    DataEventArgs<float> Args = new();
    private void ReaderLoop()
    {
      Args.Data = new float[blockSize];

      while (!stopping)
        try
        {
          if (SampleSource == null) break;

          Args.Count = SampleSource.Read(Args.Data, 0, blockSize);

          if (Args.Count > 0)
          {
            Args.Utc = DateTime.UtcNow;
            // this event is always processed synchronously inThreadedProcessor#StartProcessing
            SamplesAvailable?.Invoke(this, Args);
          }
          else
            Thread.Sleep(20); // avoid busy spin if device starves
        }
        // device stopped or was disconnected
        catch (ObjectDisposedException) {}
        catch (InvalidOperationException) {}
        catch (COMException) {}
    }
  }
}
