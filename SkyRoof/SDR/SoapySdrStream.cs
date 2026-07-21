using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using MathNet.Numerics;
using Serilog;
using static VE3NEA.NativeSoapySdr;

namespace VE3NEA
{
  internal class SoapySdrStream : IDisposable
  {
    private const long timeout = 1_000_000; // microseconds
    private const double MAX_PAD_SECONDS = 0.2; // pad at most the first 200 ms of a gap

    private readonly IntPtr Device;
    private readonly double SampleRate;
    private IntPtr Stream;
    private nint SamplesPerBlock;
    private float[] Floats;
    private GCHandle BuffHandle;
    private nint BuffsPtr;

    // timeline reconciliation
    private readonly Stopwatch Clock = new();
    private long SampleCounter; // samples delivered so far, padding included
    private long BaseTimeNs;
    private long SkippedNs;     // total gap duration that was too long to pad
    private bool TimeBaseValid;

    public DataEventArgs<Complex32> Args = new();

    // zeros to insert before Args to compensate for the samples the driver lost
    public int PadCount;
    public DataEventArgs<Complex32> PadArgs = new();

    public SoapySdrStream(IntPtr device, double sampleRate)
    {
      Device = device;
      SampleRate = sampleRate;
      SetupStream();
      CreateBuffers();
      ActivateStream();
    }

    private void SetupStream()
    {
      nint[] channels = [0];
      IntPtr ptr = IntPtr.Zero;

      try
      {
        int size = Marshal.SizeOf(typeof(nint));
        ptr = Marshal.AllocHGlobal(size);
        Marshal.Copy(channels, 0, ptr, 1);
        Stream =  SoapySDRDevice_setupStream(Device, Direction.Rx, "CF32", ptr, 1, IntPtr.Zero);
        SoapySdr.CheckError();
      }
      finally
      {
        if (ptr != IntPtr.Zero) Marshal.FreeHGlobal(ptr);
      }
    }

    private void CreateBuffers()
    {
      SamplesPerBlock = SoapySDRDevice_getStreamMTU(Device, Stream);
      nint floatsPerBlock = 2 * SamplesPerBlock;

      // Complex32 output buffer
      Args.Data = new Complex32[SamplesPerBlock];

      // silence buffer, stays all zeros
      PadArgs.Data = new Complex32[SamplesPerBlock];

      // float output buffer 
      Floats = new float[floatsPerBlock];

      // native buffer
      BuffHandle = GCHandle.Alloc(Floats, GCHandleType.Pinned);
      IntPtr[] buffsArray = [BuffHandle.AddrOfPinnedObject()];
      BuffsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(IntPtr)));
      Marshal.Copy(buffsArray, 0, BuffsPtr, 1);
    }

    private void ActivateStream()
    {
      SoapySDRDevice_activateStream(Device, Stream, SoapySdrFlags.None, 0, 0);
      SoapySdr.CheckError();
    }

    public void ReadStream()
    {
      int sampleCount = SoapySDRDevice_readStream(Device, Stream, BuffsPtr, SamplesPerBlock, out SoapySdrFlags flags, out long timeNs, timeout);
      SoapySdr.CheckError();

      Debug.Assert(sampleCount <= SamplesPerBlock);

      if (sampleCount < 0)
      {
        var errorCode = (SoapySdrError)sampleCount;
        sampleCount = 0;
        if (errorCode == SoapySdrError.SOAPY_SDR_OVERFLOW)
          Log.Error("SoapySDR readStream overflow");
        else
        throw new Exception($"Error code {errorCode}");
      }

      PadCount = MeasureGap(flags, timeNs);
      SampleCounter += PadCount + sampleCount;

      Args.Count = sampleCount;
      for (int i = 0; i < Args.Count; i++)
        Args.Data[i] = new Complex32(Floats[i * 2], Floats[i * 2 + 1]);
    }

    // the driver discards samples on overflow and does not report how many. compare the stream timeline
    // with the samples delivered so far and return the number of zeros needed to keep the stream aligned
    // with real time. the driver timestamp is used when the stream provides one, the wall clock otherwise
    private int MeasureGap(SoapySdrFlags flags, long timeNs)
    {
      if (SampleRate <= 0) return 0;
      bool hasTime = flags.HasFlag(SoapySdrFlags.SOAPY_SDR_HAS_TIME) && timeNs > 0;

      // the first read establishes the time base for both clocks
      if (!TimeBaseValid)
      {
        TimeBaseValid = true;
        BaseTimeNs = timeNs;
        Clock.Restart();
        return 0;
      }

      long elapsedNs = hasTime ? timeNs - BaseTimeNs : (long)(Clock.Elapsed.TotalSeconds * 1e9);
      long expectedNs = (long)(SampleCounter * 1e9 / SampleRate) + SkippedNs;

      // both clocks lag the sample count by the driver latency and jitter by a block or two,
      // so anything shorter than the deadband is not a gap
      long deadbandNs = (long)((hasTime ? 2 : 4) * SamplesPerBlock * 1e9 / SampleRate);
      long gapNs = elapsedNs - expectedNs - deadbandNs;
      if (gapNs <= 0) return 0;

      // a gap longer than the limit is a device failure rather than a glitch. pad the start of it and
      // account for the rest as skipped, so that it is not reported again by the following reads
      long padNs = Math.Min(gapNs, (long)(MAX_PAD_SECONDS * 1e9));
      SkippedNs += gapNs - padNs;

      Log.Warning($"IQ stream gap of {gapNs / 1e6:F0} ms, padding {padNs / 1e6:F0} ms with zeros");
      return (int)(padNs * SampleRate / 1e9);
    }

    public void Dispose()
    {
      //dispose of the stream
      if (Stream != IntPtr.Zero)
      {
        SoapySDRDevice_deactivateStream(Device, Stream, 0, out long timeNs);
        SoapySDRDevice_closeStream(Device, Stream);
      }

      // release native buffer
      if (BuffsPtr != IntPtr.Zero) Marshal.FreeHGlobal(BuffsPtr);
      if (BuffHandle.IsAllocated) BuffHandle.Free();
    }
  }
}
