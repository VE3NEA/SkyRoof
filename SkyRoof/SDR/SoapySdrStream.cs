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

    private readonly IntPtr Device;
    private IntPtr Stream;
    private nint SamplesPerBlock;
    private float[] Floats;
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

      Args.Count = sampleCount;
      for (int i = 0; i < Args.Count; i++)
        Args.Data[i] = new Complex32(Floats[i * 2], Floats[i * 2 + 1]);
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
