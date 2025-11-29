using System.Security.Policy;
using System.Text;
using VE3NEA;

namespace SkyRoof
{
  public unsafe class Ft4Decoder : ThreadedProcessor<float>
  {
    private int CurrentSlotNumber, DecodedSlotNumber;
    private float[] Samples = [];
    private int SampleCount = 0;
    DateTime DataUtc;
    public string MyCall = "";
    public string TheirCall = "";

    public event EventHandler<DecodeEventArgs>? SlotDecoded;

    protected override void Process(DataEventArgs<float> args)
    {
      AppendData(args);
      if (IsSlotDataAvailable() && SampleCount > 0)
        DecodeSlot();
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

      return CurrentSlotNumber != DecodedSlotNumber && samplesIntoSlot >= NativeFT4Coder.DECODE_SAMPLE_COUNT;
    }

    private void DecodeSlot()
    {
      var samples = Samples.Skip(SampleCount - NativeFT4Coder.DECODE_SAMPLE_COUNT).Take(NativeFT4Coder.DECODE_SAMPLE_COUNT).ToArray();

      float max = samples.Max(Math.Abs);
      if (max > 0)
        for (int i = 0; i < samples.Length; i++) 
          samples[i] /= max;

      StringBuilder decodedMessages = new StringBuilder();
      decodedMessages.Append(' ', NativeFT4Coder.DECODE_MAX_CHARS);
      NativeFT4Coder.QsoStage stage = NativeFT4Coder.QsoStage.CALLING;
      int rx_audioFrequency = 1500;
      

      NativeFT4Coder.decode(samples, ref stage, ref rx_audioFrequency, MyCall, TheirCall, decodedMessages);


      string messages = decodedMessages.ToString().Trim();

      SampleCount = 0;
      DecodedSlotNumber = CurrentSlotNumber;

      SlotDecoded?.Invoke(this, new DecodeEventArgs(messages, DataUtc - TimeSpan.FromSeconds(NativeFT4Coder.DECODE_SECONDS)));
    }
  }
}
