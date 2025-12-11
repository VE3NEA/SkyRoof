using System.Security.Policy;
using System.Text;
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
    public string MyCall = " ";
    public string TheirCall = " ";

    public int CurrentSlotNumber { get; private set; }
    public int DecodedSlotNumber { get; private set; }

    public event EventHandler<DataEventArgs<string>>? SlotDecoded;

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

      bool samplesAvailable = SampleCount >= NativeFT4Coder.DECODE_SAMPLE_COUNT;
      bool slotEnded = samplesIntoSlot >= NativeFT4Coder.DECODE_SAMPLE_COUNT; ;
      bool slotDecoded = CurrentSlotNumber == DecodedSlotNumber;
      return samplesAvailable && slotEnded && !slotDecoded;
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

      NativeFT4Coder.decode(samples, ref stage, ref RxAudioFrequency, MyCall, TheirCall, decodedMessages);

      string messagesStr = decodedMessages.ToString().Trim();
      string[] messages = messagesStr.Split(['\n'], StringSplitOptions.RemoveEmptyEntries);

      SlotDecoded?.Invoke(this, new DataEventArgs<string>(messages, DataUtc - TimeSpan.FromSeconds(NativeFT4Coder.DECODE_SECONDS)));

      SampleCount = 0;
      DecodedSlotNumber = CurrentSlotNumber;
    }
  }
}
