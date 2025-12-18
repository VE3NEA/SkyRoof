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
    private float[]? Slot;

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

      var slot = GetSlotToDecode();
      if (slot != null)
      {
        Slot = slot;
        Decode(Slot);
      }
    }

    private void AppendData(DataEventArgs<float> args)
    {

      int neededLength = SampleCount + args.Data.Length;
      if (Samples.Length < neededLength) Array.Resize(ref Samples, neededLength);

      Array.Copy(args.Data, 0, Samples, SampleCount, args.Data.Length);
      SampleCount += args.Data.Length;

      //var delay = (args.Utc - DataUtc).TotalMilliseconds;
      //int expectedCount = (int)(delay * NativeFT4Coder.SAMPLING_RATE / 1000);
      //Debug.WriteLine($"Ft4Decoder: Appended {args.Data.Length} samples, delay{delay:F1}, diff {args.Data.Length - expectedCount}");

      DataUtc = args.Utc;
    }

    private float[]? GetSlotToDecode()
    {
      // find slot boundaries
      double secondsSinceMidnight = (DataUtc - DataUtc.Date).TotalSeconds;
      CurrentSlotNumber = (int)Math.Truncate(secondsSinceMidnight / NativeFT4Coder.TIMESLOT_SECONDS);

      double secondsIntoSlot = secondsSinceMidnight - (CurrentSlotNumber * NativeFT4Coder.TIMESLOT_SECONDS);
      int samplesIntoSlot = (int)(secondsIntoSlot * NativeFT4Coder.SAMPLING_RATE);

      int slotStartIndex = SampleCount - samplesIntoSlot;
      int slotEndIndex = slotStartIndex + NativeFT4Coder.DECODE_SAMPLE_COUNT;

      // is new slot available?
      bool slotAvailable = slotStartIndex >= 0 && slotEndIndex <= SampleCount;
      bool slotDecoded = CurrentSlotNumber == DecodedSlotNumber;
      if (!slotAvailable || slotDecoded) return null;

      // extract slot
      var slot = Samples.Skip(slotStartIndex).Take(NativeFT4Coder.DECODE_SAMPLE_COUNT).ToArray();

      // dump used samples
      int samplesToKeep = SampleCount - slotEndIndex;
      Array.Copy(Samples, slotEndIndex, Samples, 0, samplesToKeep);
      SampleCount = samplesToKeep;

      return slot;
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



    public void SaveSamples()
    {
      WriteWav(Slot);
    }

    public void PlayBackSamples()
    {
      Decode(ReadWav());
    }

    private static float[] ReadWav()
    {
      float[] samples;
      string filePath = Path.Combine(Utils.GetUserDataFolder(), "samples.wav");

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

      string filePath = Path.Combine(Utils.GetUserDataFolder(), "samples.wav");
      var format = WaveFormat.CreateIeeeFloatWaveFormat(NativeFT4Coder.SAMPLING_RATE, 1);

      using var writer = new WaveFileWriter(filePath, format);
      writer.Write(buffer, 0, buffer.Length);
    }
  }
}
