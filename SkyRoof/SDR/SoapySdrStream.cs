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

    // the stream arg that most drivers use for the depth of their ring buffer, and the depth to ask for.
    // typical driver defaults are 8..16 buffers, which at the rates the fast devices run is only some
    // tens of milliseconds. the depth costs memory and, after a long stall, latency: the reader drains
    // the backlog as fast as the CPU allows and the extra samples end up sitting in the audio buffer
    private const string BUFFER_COUNT_KEY = "buffers";
    private const int TARGET_BUFFER_COUNT = 64;

    private readonly IntPtr Device;
    private IntPtr Stream;
    private nint SamplesPerBlock;
    private GCHandle BuffHandle;
    private nint BuffsPtr;

    public DataEventArgs<Complex32> Args = new();

    public SoapySdrStream(IntPtr device)
    {
      Device = device;
      SetupStream();
      CreateBuffers();
      ActivateStream();
    }

    private void SetupStream()
    {
      nint[] channels = [0];
      IntPtr ptr = IntPtr.Zero;
      IntPtr argsPtr = IntPtr.Zero;

      try
      {
        int size = Marshal.SizeOf(typeof(nint));
        ptr = Marshal.AllocHGlobal(size);
        Marshal.Copy(channels, 0, ptr, 1);
        argsPtr = BuildStreamArgs();
        Stream =  SoapySDRDevice_setupStream(Device, Direction.Rx, "CF32", ptr, 1, argsPtr);
        SoapySdr.CheckError();
      }
      finally
      {
        if (ptr != IntPtr.Zero) Marshal.FreeHGlobal(ptr);
        FreeKwargs(argsPtr);
      }
    }

    // a driver overflows when its ring buffer fills up before ReadStream drains it, so a deeper ring is
    // what buys the reader time across a scheduler, GC or disk hiccup. only the number of buffers is
    // raised and not their length, so the stream MTU, and with it the audio latency, stays as it was.
    // the key is written only if the driver advertises it, with the value clamped to the advertised range
    private IntPtr BuildStreamArgs()
    {
      var args = new Dictionary<string, string>();

      foreach (var info in GetStreamArgsInfo())
      {
        if (info.Key != BUFFER_COUNT_KEY || info.Type != SoapySDRArgInfoType.Int) continue;

        double count = TARGET_BUFFER_COUNT;
        if (info.Range.maximum > info.Range.minimum) count = Math.Min(count, info.Range.maximum);
        // never ask for a shallower ring than the driver would have used on its own
        if (int.TryParse(info.Value, out int defaultCount)) count = Math.Max(count, defaultCount);

        args[info.Key] = ((int)count).ToString();
      }

      if (args.Count == 0) return IntPtr.Zero;

      Log.Information($"SDR stream args: {string.Join(", ", args.Select(a => $"{a.Key}={a.Value}"))}");
      return AllocKwargs(args);
    }

    private SoapySDRArgInfo[] GetStreamArgsInfo()
    {
      IntPtr ptr = SoapySDRDevice_getStreamArgsInfo(Device, Direction.Rx, 0, out nint length);
      SoapySdr.CheckError();

      if (ptr == IntPtr.Zero || length <= 0) return Array.Empty<SoapySDRArgInfo>();
      return SoapySdrHelper.MarshalArgsInfoArray(ptr, length);
    }

    // build the native SoapySDRKwargs entirely in unmanaged memory: the pointer arrays must stay put
    // until setupStream has read them, which a managed array does not guarantee. the size field is a
    // size_t in the C API, so it is written as a full pointer-sized value rather than through the
    // managed struct, whose size member is declared as an int
    private static IntPtr AllocKwargs(Dictionary<string, string> args)
    {
      IntPtr keys = Marshal.AllocHGlobal(args.Count * IntPtr.Size);
      IntPtr vals = Marshal.AllocHGlobal(args.Count * IntPtr.Size);

      int index = 0;
      foreach (var arg in args)
      {
        Marshal.WriteIntPtr(keys, index * IntPtr.Size, Marshal.StringToHGlobalAnsi(arg.Key));
        Marshal.WriteIntPtr(vals, index * IntPtr.Size, Marshal.StringToHGlobalAnsi(arg.Value));
        index++;
      }

      IntPtr result = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SoapySDRKwargs)));
      Marshal.WriteIntPtr(result, 0, args.Count);
      Marshal.WriteIntPtr(result, IntPtr.Size, keys);
      Marshal.WriteIntPtr(result, 2 * IntPtr.Size, vals);
      return result;
    }

    private static void FreeKwargs(IntPtr argsPtr)
    {
      if (argsPtr == IntPtr.Zero) return;

      nint count = Marshal.ReadIntPtr(argsPtr, 0);
      IntPtr keys = Marshal.ReadIntPtr(argsPtr, IntPtr.Size);
      IntPtr vals = Marshal.ReadIntPtr(argsPtr, 2 * IntPtr.Size);

      for (int i = 0; i < count; i++)
      {
        Marshal.FreeHGlobal(Marshal.ReadIntPtr(keys, i * IntPtr.Size));
        Marshal.FreeHGlobal(Marshal.ReadIntPtr(vals, i * IntPtr.Size));
      }

      Marshal.FreeHGlobal(keys);
      Marshal.FreeHGlobal(vals);
      Marshal.FreeHGlobal(argsPtr);
    }

    private void CreateBuffers()
    {
      SamplesPerBlock = SoapySDRDevice_getStreamMTU(Device, Stream);

      // Complex32 output buffer. CF32 is a pair of interleaved floats per sample, which is exactly the
      // layout of Complex32, so the driver writes straight into this buffer and the read thread is
      // spared a conversion pass over every block
      Args.Data = new Complex32[SamplesPerBlock];

      // native buffer
      BuffHandle = GCHandle.Alloc(Args.Data, GCHandleType.Pinned);
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
        // an overflow is not logged: writing to the log file from this thread stalls the reader for as
        // long as the write takes, which is itself a way to provoke the next overflow
        if (errorCode != SoapySdrError.SOAPY_SDR_OVERFLOW)
          throw new Exception($"Error code {errorCode}");
      }

      // the driver has already written the samples into Args.Data
      Args.Count = sampleCount;
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
