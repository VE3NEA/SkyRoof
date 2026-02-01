using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Serilog;
using VE3NEA;
using static VE3NEA.NativeFT4Coder;

namespace SkyRoof
{
  public unsafe class Ft4Decoder : ThreadedProcessor<float>
  {
    private float[] ReceivedSamples = [];
    private int SampleCount = 0;
    DateTime DataUtc;
    private float[] SlotSamples = new float[NativeFT4Coder.DECODE_SAMPLE_COUNT];
    private Ft4DecodeBuffers? Buffers = new();
    private Ft4Slot Slot = new();

    public const int FT4_SIGNAL_BANDWIDTH = 83; // Hz
    public int RxAudioFrequency = NativeFT4Coder.DEFAULT_AUDIO_FREQUENCY;
    public int CutoffFrequency = 4000;
    public string MyCall = " ";
    public string TheirCall = " ";

    public long DecodedSlotNumber { get; private set; }

    public event EventHandler<DataEventArgs<string>>? MessageDecoded;

    protected override void Process(DataEventArgs<float> args)
    {
      AppendData(args);

      if (GetSlotToDecode()) Decode(SlotSamples);
    }

    private void AppendData(DataEventArgs<float> args)
    {
      int neededLength = SampleCount + args.Count;
      if (ReceivedSamples.Length < neededLength) Array.Resize(ref ReceivedSamples, neededLength);

      Array.Copy(args.Data, 0, ReceivedSamples, SampleCount, args.Count);
      SampleCount += args.Count;

      DataUtc = args.Utc;
    }

    private bool GetSlotToDecode()
    {
      Slot.Utc = DataUtc;
      int slotStartIndex = SampleCount - Slot.SamplesIntoSlot;
      int slotEndIndex = slotStartIndex + NativeFT4Coder.DECODE_SAMPLE_COUNT;

      // is new slot available?
      bool slotAvailable = slotStartIndex >= 0 && slotEndIndex <= SampleCount;
      bool slotDecoded = Slot.SlotNumber == DecodedSlotNumber;
      if (!slotAvailable || slotDecoded) return false;

      // extract slot
      Array.Copy(ReceivedSamples, slotStartIndex, SlotSamples, 0, NativeFT4Coder.DECODE_SAMPLE_COUNT);

      // dump used samples
      int samplesToKeep = SampleCount - slotEndIndex;
      Array.Copy(ReceivedSamples, slotEndIndex, ReceivedSamples, 0, samplesToKeep);
      SampleCount = samplesToKeep;

      return true;
    }

    public void Decode(float[]? samples = null)
    {
      samples ??= SlotSamples;

      // scale to -1..1
      float max = samples.Max(Math.Abs);
      if (max > 0)
        for (int i = 0; i < samples.Length; i++)
          samples[i] /= max;

      QsoStage stage = NativeFT4Coder.QsoStage.CALLING;
      Buffers!.SetMyCall(MyCall);

      DecodedSlotNumber = Slot.SlotNumber;

      NativeFT4Coder.decode_ft4(samples, ref stage, ref RxAudioFrequency, ref CutoffFrequency,
        Buffers.MyCall, Buffers.HisCall, DecodedMessageCallback);

      // saparator if no messages decoded
      MessageDecoded?.Invoke(this, new([], Slot.CurrentSlotStartTime));
    }

    private void DecodedMessageCallback(IntPtr messagePtr)
    {
      try
      {
        string? message = Marshal.PtrToStringAnsi(messagePtr);
        if (!string.IsNullOrEmpty(message))
          MessageDecoded?.Invoke(this, new([message], Slot.CurrentSlotStartTime));
      }
      catch (Exception ex)
      {
        Log.Error(ex, "Error in FT4 decoded message callback");
      }
    }

    public override void Dispose()
    {
      base.Dispose();
      Buffers?.Dispose();
      Buffers = null;
    }
  }
}
