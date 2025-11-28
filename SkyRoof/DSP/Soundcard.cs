using CSCore;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using CSCore.SoundOut;
using CSCore.Streams;
using MathNet.Numerics;
using VE3NEA;
using static SkyRoof.Slicer;

namespace VE3NEA
{
  public class Entry
  {
    public string Id, Name;
    public Entry(string id, string name) { Id = id; Name = name; }
  }




  //-----------------------------------------------------------------------------------------------
  //                                     base class
  //-----------------------------------------------------------------------------------------------
  
  // <T> is float or Complex32
  
  public abstract class Soundcard : IDisposable
  {
    protected const int DEFAULT_SAMPLING_RATE = 48_000;
    protected readonly int SamplingRate;
    protected MMDevice? mmDevice;
    protected bool enabled;

    public bool Enabled { get => enabled; set => SetEnabled(value); }

    public event EventHandler? StateChanged;

    public Soundcard(string? audioDeviceId = null, int? samplingRate = null)
    {
      SetDeviceId(audioDeviceId);
      SamplingRate = samplingRate ?? DEFAULT_SAMPLING_RATE;
    }


    private void SetEnabled(bool value)
    {
      if (value == Enabled) return;
      enabled = value;

      if (value) Start(); else Stop();
    }

    public void SetDeviceId(string? deviceId)
    {
      bool wasEnabled = enabled;
      Enabled = false;
      if (deviceId == null) return;

      using (var deviceEnumerator = new MMDeviceEnumerator())
        try { mmDevice = deviceEnumerator.GetDevice(deviceId); } catch { return; }

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
        // but, but some internal variable in MMDevice becomes null
        // then a call to FriendlyName throws an exception
        return "Device Failed";
      }
    }

    protected void OnStateChanged()
    {
      StateChanged?.Invoke(this, EventArgs.Empty);
    } 

    public static Entry[] ListDevices(DataFlow direction)
    {
      using (var deviceEnumerator = new MMDeviceEnumerator())
      using (var deviceCollection = deviceEnumerator.EnumAudioEndpoints(direction, DeviceState.Active))
        return deviceCollection.Select(s => new Entry(s.DeviceID, s.FriendlyName)).ToArray();
    }

    public static string? GetDefaultSoundcardId(DataFlow direction)
    {
      using (var deviceEnumerator = new MMDeviceEnumerator())
        return deviceEnumerator.GetDefaultAudioEndpoint(direction, Role.Multimedia)?.DeviceID;
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

    public abstract bool IsRunning();
    protected abstract void Start();
    protected abstract void Stop();
  }




  //-----------------------------------------------------------------------------------------------
  //                                     output soundcard
  //-----------------------------------------------------------------------------------------------
  public class OutputSoundcard<T> : Soundcard
  {
    private WaveSource<T> waveSource;
    private WasapiOut? wasapiOut;
    private float volume;
    public float Volume { get => volume; set => SetVolume(value); }


    public OutputSoundcard(string? audioDeviceId = null, int? samplingRate = null) : base(audioDeviceId)
    {
      waveSource = new WaveSource<T>(SamplingRate);
    }

    protected override void Start()
    {
      if (mmDevice == null) return;

      try
      {
        waveSource.ClearBuffer();

        wasapiOut = new WasapiOut(false, AudioClientShareMode.Shared, 100);
        wasapiOut.Device = mmDevice;
        wasapiOut.Initialize(waveSource);
        wasapiOut.Volume = volume;
        wasapiOut.Stopped += (s,a) => OnStateChanged();
        wasapiOut.Play();

        OnStateChanged();
      }
      catch
      {
        Stop();
      }
    }

    protected override void Stop()
    {
      wasapiOut?.Stop();
      wasapiOut?.Dispose();
      wasapiOut = null;

      OnStateChanged();
    }

    public override bool IsRunning()
    {
      return wasapiOut?.PlaybackState == PlaybackState.Playing;
    }

    private void SetVolume(float value)
    {
      volume = value;
      if (wasapiOut != null) wasapiOut.Volume = value;
    }

    public void AddSamples(T[] samples)
    {
      if (Enabled) waveSource.AddSamples(samples);
    }

  }




  //-----------------------------------------------------------------------------------------------
  //                                     input soundcard
  //-----------------------------------------------------------------------------------------------
  public class InputSoundcard<T> : Soundcard
  {
    private WasapiCapture? soundIn;
    private ISampleSource? SampleSource;

    public event EventHandler? SamplesAvailable;

    public InputSoundcard(string? audioDeviceId = null, int? samplingRate = null) : base(audioDeviceId)
    {
    }

    protected override void Start()
    {
      try
      {
        if (mmDevice?.DeviceState != DeviceState.Active) return;

        soundIn?.Dispose();

        // init audio device
        int channelCount = typeof(T) == typeof(Complex32) ? 2 : 1;
        WaveFormat format = new WaveFormat(SamplingRate, 32, channelCount, AudioEncoding.IeeeFloat);

        soundIn = new WasapiCapture(false, AudioClientShareMode.Shared, 100, format);
        soundIn.Device = mmDevice;
        soundIn.Initialize();
        soundIn.Stopped += (s,a) => OnStateChanged();
        soundIn.DataAvailable += SoundIn_DataAvailable;

        // create sample source wrapper
        SampleSource = new SoundInSource(soundIn).ToSampleSource();
        SampleSource = SampleSource.ChangeSampleRate(SamplingRate);

        soundIn.Start();
      }
      catch
      {
        Stop();
      }
    }

    protected override void Stop()
    {
      if (IsRunning()) soundIn!.Stop();
      soundIn?.Dispose();
      soundIn = null;
      SampleSource = null;

    }

    public override bool IsRunning()
    {
      return soundIn?.RecordingState == RecordingState.Recording;
    }


    private void SoundIn_DataAvailable(object? sender, DataAvailableEventArgs e)
    {
      throw new NotImplementedException();
    }
  }
}
