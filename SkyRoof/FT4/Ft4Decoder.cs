using System.Diagnostics;
using System.Text;
using NAudio.Wave;
using VE3NEA;

namespace SkyRoof
{
  public unsafe class Ft4Decoder : ThreadedProcessor<float>
  {
    private float[] Samples = [];
    private int SampleCount = 0;
    DateTime DataUtc;

    public const int FT4_SIGNAL_BANDWIDTH = 83; // Hz
    public int RxAudioFrequency = 1500;
    public int TxAudioFrequency = 1500;
    public int CutoffFrequency = 4000;
    public string MyCall = " ";
    public string TheirCall = " ";

    public int CurrentSlotNumber { get; private set; }
    public int DecodedSlotNumber { get; private set; }

    public event EventHandler<DataEventArgs<string>>? SlotDecoded;

    protected override void Process(DataEventArgs<float> args)
    {
      AppendData(args);

      if (IsSlotDataAvailable()) DecodeSlot();
    }

    private void AppendData(DataEventArgs<float> args)
    {

      int neededLength = SampleCount + args.Data.Length;
      if (Samples.Length < neededLength) Array.Resize(ref Samples, neededLength);

      Array.Copy(args.Data, 0, Samples, SampleCount, args.Data.Length);
      SampleCount += args.Data.Length;

      DataUtc = args.Utc;
    }

    private bool IsSlotDataAvailable()
    {
      var secondsSinceMidnight = (DataUtc - DataUtc.Date).TotalSeconds;
      CurrentSlotNumber = (int)Math.Truncate(secondsSinceMidnight / NativeFT4Coder.TIMESLOT_SECONDS);

      var secondsIntoSlot = secondsSinceMidnight - (CurrentSlotNumber * NativeFT4Coder.TIMESLOT_SECONDS);
      int samplesIntoSlot = (int)(secondsIntoSlot * NativeFT4Coder.SAMPLING_RATE);

      bool samplesAvailable = SampleCount >= NativeFT4Coder.DECODE_SAMPLE_COUNT;
      bool slotEnded = samplesIntoSlot >= NativeFT4Coder.DECODE_SAMPLE_COUNT;
      bool slotDecoded = CurrentSlotNumber == DecodedSlotNumber;
      return samplesAvailable && slotEnded && !slotDecoded;
    }

    private void DecodeSlot()
    {
      var samples = Samples.Skip(SampleCount - NativeFT4Coder.DECODE_SAMPLE_COUNT).Take(NativeFT4Coder.DECODE_SAMPLE_COUNT).ToArray();
      SamplesForTest = samples;
      SampleCount = 0;

      Decode(samples);
    }

    private void Decode(float[] samples)
    {
      float max = samples.Max(Math.Abs);
      if (max > 0)
        for (int i = 0; i < samples.Length; i++)
          samples[i] /= max;

      StringBuilder decodedMessages = new StringBuilder();
      decodedMessages.Append(' ', NativeFT4Coder.DECODE_MAX_CHARS);
      NativeFT4Coder.QsoStage stage = NativeFT4Coder.QsoStage.CALLING;

      NativeFT4Coder.decode(samples, ref stage, ref RxAudioFrequency, ref CutoffFrequency, MyCall, TheirCall, decodedMessages);

      string messagesStr = decodedMessages.ToString().Trim();
      string[] messages = messagesStr.Split(['\n'], StringSplitOptions.RemoveEmptyEntries);

      SlotDecoded?.Invoke(this, new DataEventArgs<string>(messages, DataUtc - TimeSpan.FromSeconds(NativeFT4Coder.DECODE_SECONDS)));

      DecodedSlotNumber = CurrentSlotNumber;
    }



    private float[] SamplesForTest = new float[NativeFT4Coder.DECODE_SAMPLE_COUNT];
    public void SaveSamples()
    {
      WriteWav(SamplesForTest);
    }

    public void PlayBackSamples()
    {
      Decode(ReadWav());
    }

    private static float[] ReadWav()
    {
      float[] samples;
      string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "samples.wav");

      using (var reader = new WaveFileReader(filePath))
      {
        Debug.Assert(reader.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat);
        Debug.Assert(reader.WaveFormat.SampleRate == NativeFT4Coder.SAMPLING_RATE);
        Debug.Assert(reader.WaveFormat.Channels == 1);
        Debug.Assert(reader.Length == NativeFT4Coder.DECODE_SAMPLE_COUNT * sizeof(float));

        var buffer = new byte[reader.Length];
        int bytesRead = reader.Read(buffer, 0, buffer.Length);
        samples = new float[bytesRead / 4];
        Buffer.BlockCopy(buffer, 0, samples, 0, bytesRead);
      }

      return samples;
    }

    private static void WriteWav(float[] samples)
    {
      // float to bytes
      byte[] buffer = new byte[samples.Length * 4];
      Buffer.BlockCopy(samples, 0, buffer, 0, buffer.Length);

      string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "samples.wav");
      var format = WaveFormat.CreateIeeeFloatWaveFormat(NativeFT4Coder.SAMPLING_RATE, 1);

      using var writer = new WaveFileWriter(filePath, format);
      writer.Write(buffer, 0, buffer.Length);
    }
  }
}
