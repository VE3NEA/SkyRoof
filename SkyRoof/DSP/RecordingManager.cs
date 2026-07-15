using MathNet.Numerics;
using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Serilog;
using VE3NEA;

namespace SkyRoof
{
  public class RecordingManager
  {
    private readonly Context ctx;
    public event EventHandler? Changed;

    private const int RECORDING_MINUTES = 30;
    private const int BUFFER_SIZE = SdrConst.AUDIO_SAMPLING_RATE * 60 * RECORDING_MINUTES;
    private const int PLAYBACK_PREBUFFER_CHUNKS = 10;
    private const int PLAYBACK_TIMER_INTERVAL_MS = 20;
    private const int PLAYBACK_SAMPLES_PER_TICK = SdrConst.AUDIO_SAMPLING_RATE * PLAYBACK_TIMER_INTERVAL_MS / 1000;

    private float[]? audioBuffer;
    private Complex32[]? iqBuffer;
    private int samplesInBuffer;
    private int playbackPosition;
    private bool speakerEnabledForPlayback;
    private readonly object playbackLock = new();
    private DateTime recordingStartTime;
    private RecordingEvents recordingEvents = new();

    public RecordingManager(Context ctx)
    {
      this.ctx = ctx;
    }

    public float[]? AudioBuffer => audioBuffer;
    public Complex32[]? IqBuffer => iqBuffer;
    public int SamplesInBuffer => samplesInBuffer;
    public int PlaybackPosition => playbackPosition;
    public bool IsRecording { get; private set; }
    public bool IsPlayingBack { get; private set; }
    public bool IsRecordingAudio => iqBuffer == null;
    public RecordingEvents RecordingEvents => recordingEvents;
    public DateTime RecordingStartTime => recordingStartTime;
    public bool HasData => samplesInBuffer > 0;

    public void StartRecording(bool isAudio)
    {
      if (isAudio)
      {
        audioBuffer = new float[BUFFER_SIZE];
        iqBuffer = null;
        Log.Information("Starting audio recording");
      }
      else
      {
        iqBuffer = new Complex32[BUFFER_SIZE];
        audioBuffer = null;
        Log.Information("Starting I/Q recording");
      }

      samplesInBuffer = 0;
      playbackPosition = 0;
      recordingStartTime = DateTime.UtcNow;
      recordingEvents.Start(ctx);
      IsRecording = true;
    }

    public TimeSpan StopRecording()
    {
      IsRecording = false;
      return DateTime.UtcNow - recordingStartTime;
    }

    public bool AddIqSamples(DataEventArgs<Complex32> e)
    {
      if (!IsRecording || iqBuffer == null || IsRecordingAudio) return false;
      return AddSamples(e.Data, iqBuffer);
    }

    public bool AddAudioSamples(DataEventArgs<float> e)
    {
      if (!IsRecording || audioBuffer == null || !IsRecordingAudio) return false;
      return AddSamples(e.Data, audioBuffer);
    }

    public bool StartPlayback()
    {
      if (samplesInBuffer <= 0) return false;

      if (audioBuffer != null && !ctx.SpeakerSoundcard.Enabled)
      {
        ctx.SpeakerSoundcard.Enabled = true;
        speakerEnabledForPlayback = true;
      }

      if (playbackPosition >= samplesInBuffer) playbackPosition = 0;
      IsPlayingBack = true;
      PreparePlaybackOutputs();
      PrebufferPlayback();
      if (playbackPosition >= samplesInBuffer)
      {
        StopPlayback(true);
        return false;
      }

      return true;
    }

    public void StopPlayback(bool resetPosition = false)
    {
      IsPlayingBack = false;
      if (resetPosition) playbackPosition = 0;

      if (speakerEnabledForPlayback)
      {
        ctx.SpeakerSoundcard.Enabled = false;
        speakerEnabledForPlayback = false;
      }
    }

    public bool PumpPlaybackChunk()
    {
      lock (playbackLock)
      {
        int samplesRemaining = samplesInBuffer - playbackPosition;
        if (samplesRemaining <= 0) return false;

        int count = Math.Min(PLAYBACK_SAMPLES_PER_TICK, samplesRemaining);
        if (audioBuffer != null)
          ctx.MainForm.RoutePlaybackAudio(audioBuffer, playbackPosition, count);
        else if (iqBuffer != null)
          ctx.MainForm.RoutePlaybackIq(iqBuffer, playbackPosition, count);

        playbackPosition += count;
        return playbackPosition < samplesInBuffer;
      }
    }

    public bool SeekPlaybackToPosition(int position)
    {
      if (!IsPlayingBack || samplesInBuffer <= 0) return false;

      lock (playbackLock)
        playbackPosition = Math.Clamp(position, 0, Math.Max(0, samplesInBuffer - 1));

      PreparePlaybackOutputs();
      PrebufferPlayback();
      if (playbackPosition >= samplesInBuffer)
      {
        StopPlayback(true);
        return false;
      }

      return true;
    }

    public string GetPlaybackStatus()
    {
      TimeSpan elapsed = TimeSpan.FromSeconds(playbackPosition / (double)SdrConst.AUDIO_SAMPLING_RATE);
      TimeSpan total = TimeSpan.FromSeconds(samplesInBuffer / (double)SdrConst.AUDIO_SAMPLING_RATE);
      int percent = samplesInBuffer <= 0 ? 0 : (int)Math.Round(100.0 * playbackPosition / samplesInBuffer);
      percent = Math.Clamp(percent, 0, 100);
      return $"Playing back {(IsRecordingAudio ? "Audio" : "I/Q")} {elapsed:mm\\:ss} of {total:mm\\:ss} ({percent}%)";
    }

    public string GetRecordingStatus()
    {
      TimeSpan elapsed = DateTime.UtcNow - recordingStartTime;
      return $"Recording {(IsRecordingAudio ? "Audio" : "I/Q")} {elapsed:mm\\:ss}";
    }

    public bool LoadRecording(string filename)
    {
      string ext = Path.GetExtension(filename).ToLowerInvariant();
      bool isMp3 = ext == ".mp3";

      using var reader = new AudioFileReader(filename);

      iqBuffer = null;
      audioBuffer = null;
      IsRecording = false;
      IsPlayingBack = false;

      if (isMp3)
      {
        ISampleProvider source = reader;
        if (reader.WaveFormat.SampleRate != SdrConst.AUDIO_SAMPLING_RATE)
          source = new WdlResamplingSampleProvider(source, SdrConst.AUDIO_SAMPLING_RATE);
        audioBuffer = ReadAudioSamples(source);
      }
      else
      {
        if (reader.WaveFormat.SampleRate != SdrConst.AUDIO_SAMPLING_RATE)
          throw new InvalidOperationException($"Unsupported sample rate: {reader.WaveFormat.SampleRate} Hz");

        if (reader.WaveFormat.Channels == 1) audioBuffer = ReadAudioSamples(reader);
        else if (reader.WaveFormat.Channels == 2) iqBuffer = ReadIqSamples(reader);
        else throw new InvalidOperationException($"Unsupported channel count: {reader.WaveFormat.Channels}");
      }

      playbackPosition = 0;
      LoadRecordingEvents(filename);
      Changed?.Invoke(this, EventArgs.Empty);
      return HasData;
    }

    public void SaveRecording(string fileName, bool saveAsMp3)
    {
      if (saveAsMp3) SaveRecordingAsMp3(fileName);
      else SaveRecordingAsWav(fileName);

      SaveRecordingEvents(fileName);
    }

    public bool RememberRecordingEvents()
    {
      bool changed = IsRecording && recordingEvents.RememberChanges(ctx);
      if (changed) Changed?.Invoke(this, EventArgs.Empty);
      return changed;
    }

    public bool RememberQsoSaved(string callsign)
    {
      bool changed = IsRecording && recordingEvents.RememberQsoSaved(callsign);
      if (changed) Changed?.Invoke(this, EventArgs.Empty);
      return changed;
    }

    public double GetVisibleDurationSeconds()
    {
      int sampleCount = samplesInBuffer == 0 || IsRecording ? BUFFER_SIZE : samplesInBuffer;
      return sampleCount / (double)SdrConst.AUDIO_SAMPLING_RATE;
    }

    public string GetRecordingEventsFileName(string recordingFileName)
    {
      return recordingFileName + ".json";
    }

    // shared file naming so the manual RecorderPanel and the headless AutoSelector name files identically:
    // yyyy-MM-dd_HH_mm_ss[_<SatName>] with a .mp3 / .wav / .iq.wav extension
    public static string BuildRecordingFileName(DateTime startLocal, string? satelliteName, string ext, bool isIqWav)
    {
      string fileName = $"{startLocal:yyyy-MM-dd_HH_mm_ss}";
      if (!string.IsNullOrWhiteSpace(satelliteName)) fileName += "_" + Utils.SanitizeFileNamePart(satelliteName);

      return isIqWav ? fileName + ".iq.wav" : fileName + "." + ext;
    }

    private bool AddSamples<T>(T[] source, T[] buffer)
    {
      int samplesToWrite = Math.Min(source.Length, buffer.Length - samplesInBuffer);
      if (samplesToWrite <= 0) return false;

      Array.Copy(source, 0, buffer, samplesInBuffer, samplesToWrite);
      samplesInBuffer += samplesToWrite;
      return samplesInBuffer >= buffer.Length;
    }

    private void PreparePlaybackOutputs()
    {
      if (audioBuffer != null)
      {
        ctx.SpeakerSoundcard.Buffer.Clear();
        if (ctx.Settings.OutputStream.Type == DataStreamType.AudioToVac)
          ctx.AudioVacSoundcard.Buffer.Clear();
      }
      else if (iqBuffer != null && ctx.Settings.OutputStream.Type == DataStreamType.IqToVac)
        ctx.IqVacSoundcard.Buffer.Clear();
    }

    private void PrebufferPlayback()
    {
      for (int i = 0; i < PLAYBACK_PREBUFFER_CHUNKS; i++)
        if (!PumpPlaybackChunk()) break;
    }

    private float[] ReadAudioSamples(ISampleProvider reader)
    {
      int channels = reader.WaveFormat.Channels;
      float[] samples = new float[BUFFER_SIZE];
      float[] buffer = new float[4096 * channels];
      samplesInBuffer = 0;

      int samplesRead;
      while ((samplesRead = reader.Read(buffer, 0, buffer.Length)) > 0 && samplesInBuffer < BUFFER_SIZE)
      {
        int framesRead = samplesRead / channels;
        int framesToCopy = Math.Min(framesRead, BUFFER_SIZE - samplesInBuffer);

        if (channels == 1) Array.Copy(buffer, 0, samples, samplesInBuffer, framesToCopy);
        else
          for (int i = 0; i < framesToCopy; i++)
          {
            float sum = 0;
            for (int channel = 0; channel < channels; channel++)
              sum += buffer[i * channels + channel];

            samples[samplesInBuffer + i] = sum / channels;
          }

        samplesInBuffer += framesToCopy;
      }

      return samples;
    }

    private Complex32[] ReadIqSamples(ISampleProvider reader)
    {
      Complex32[] samples = new Complex32[BUFFER_SIZE];
      float[] buffer = new float[4096 * 2];
      samplesInBuffer = 0;

      int samplesRead;
      while ((samplesRead = reader.Read(buffer, 0, buffer.Length)) > 0 && samplesInBuffer < BUFFER_SIZE)
      {
        int framesRead = samplesRead / 2;
        int framesToCopy = Math.Min(framesRead, BUFFER_SIZE - samplesInBuffer);

        for (int i = 0; i < framesToCopy; i++)
          samples[samplesInBuffer + i] = new Complex32(buffer[2 * i], buffer[2 * i + 1]);

        samplesInBuffer += framesToCopy;
      }

      return samples;
    }

    private void SaveRecordingEvents(string recordingFileName)
    {
      recordingEvents.Save(GetRecordingEventsFileName(recordingFileName));
    }

    private void LoadRecordingEvents(string recordingFileName)
    {
      recordingEvents = RecordingEvents.Load(GetRecordingEventsFileName(recordingFileName));
    }

    private void SaveRecordingAsWav(string fileName)
    {
      if (audioBuffer != null)
      {
        WriteWav(fileName, audioBuffer, 1, samplesInBuffer);
        return;
      }

      if (iqBuffer == null)
        throw new InvalidOperationException("No recording to save");

      float[] interleaved = new float[samplesInBuffer * 2];
      for (int i = 0; i < samplesInBuffer; i++)
      {
        interleaved[2 * i] = iqBuffer[i].Real;
        interleaved[2 * i + 1] = iqBuffer[i].Imaginary;
      }

      WriteWav(fileName, interleaved, 2, samplesInBuffer);
    }

    private void SaveRecordingAsMp3(string filename)
    {
      if (audioBuffer == null)
        throw new InvalidOperationException("No audio data available for MP3 export");

      const int targetSampleRate = 16000;
      const int targetBitrate = 24000;

      short[] pcm = new short[samplesInBuffer];
      for (int i = 0; i < samplesInBuffer; i++)
        pcm[i] = (short)(Math.Clamp(audioBuffer[i], -1f, 1f) * short.MaxValue);

      byte[] inputBytes = new byte[pcm.Length * sizeof(short)];
      Buffer.BlockCopy(pcm, 0, inputBytes, 0, inputBytes.Length);

      var inputFormat = new WaveFormat(SdrConst.AUDIO_SAMPLING_RATE, 16, 1);

      using var inputStream = new MemoryStream(inputBytes, writable: false);
      using var rawSource = new RawSourceWaveStream(inputStream, inputFormat);
      using var resampled = new MediaFoundationResampler(rawSource, targetSampleRate)
      {
        ResamplerQuality = 60
      };

      MediaFoundationEncoder.EncodeToMp3(resampled, filename, targetBitrate);
    }

    private void WriteWav(string fileName, float[] samples, int channels, int framesCount)
    {
      if (framesCount <= 0) return;

      int floatsToWrite = framesCount * channels;
      int bytesToWrite = floatsToWrite * sizeof(float);

      byte[] buffer = new byte[bytesToWrite];
      Buffer.BlockCopy(samples, 0, buffer, 0, bytesToWrite);

      WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(SdrConst.AUDIO_SAMPLING_RATE, channels);
      using var writer = new WaveFileWriter(fileName, format);
      writer.Write(buffer, 0, buffer.Length);
    }
  }
}
