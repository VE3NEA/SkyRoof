using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MathNet.Numerics;
using VE3NEA;

namespace SkyRoof
{
  public unsafe class Ft4Decoder : ThreadedProcessor<float>
  {
    private int CurrentSlotNumber, DecodedSlotNumber;
    private float[] Samples = [];
    private int SampleCount = 0;
    DateTime DataUtc;

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

      StringBuilder output_messages = new StringBuilder();
      output_messages.Append(' ', NativeFT4Coder.DECODE_MAX_CHARS);


      NativeFT4Coder.decode(samples, output_messages);


      string messages = output_messages.ToString().Trim();

      SampleCount = 0;
      DecodedSlotNumber = CurrentSlotNumber;

      SlotDecoded?.Invoke(this, new DecodeEventArgs(messages, DataUtc - TimeSpan.FromSeconds(NativeFT4Coder.DECODE_SECONDS)));
    }
  }
}
