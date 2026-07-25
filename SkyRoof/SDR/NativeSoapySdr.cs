using System.Runtime.InteropServices;

namespace VE3NEA
{
  [StructLayout(LayoutKind.Sequential)]
  public class NativeSoapySdr
  {
    public enum Direction { Tx, Rx }

    public enum SoapySDRArgInfoType { Bool, Int, Float, String }

    [Flags]
    public enum SoapySdrFlags
    {
      None = 0,
      SOAPY_SDR_END_BURST = 2,
      SOAPY_SDR_HAS_TIME = 4,
      SOAPY_SDR_END_ABRUPT = 8,
      SOAPY_SDR_ONE_PACKET = 16,
      SOAPY_SDR_MORE_FRAGMENTS = 32,
      SOAPY_SDR_WAIT_TRIGGER = 64
    }

    public enum SoapySdrError
    {
      SOAPY_SDR_TIMEOUT = -1,
      SOAPY_SDR_STREAM_ERROR = -2,
      SOAPY_SDR_CORRUPTION = -3,
      SOAPY_SDR_OVERFLOW = -4,
      SOAPY_SDR_NOT_SUPPORTED = -5,
      SOAPY_SDR_TIME_ERROR = -6,
      SOAPY_SDR_UNDERFLOW = -7
    }

    public enum SoapySDRLogLevel
    {
      SOAPY_SDR_FATAL = 1,
      SOAPY_SDR_CRITICAL = 2,
      SOAPY_SDR_ERROR = 3,
      SOAPY_SDR_WARNING = 4,
      SOAPY_SDR_NOTICE = 5,
      SOAPY_SDR_INFO = 6,
      SOAPY_SDR_DEBUG = 7,
      SOAPY_SDR_TRACE = 8,
      SOAPY_SDR_SSI = 9
    }
  public struct SoapySDRKwargs
    {
      public int size;
      public IntPtr keys;
      public IntPtr vals;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SoapySDRRange
    {
      public double minimum;
      public double maximum;
      public double step;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SoapySDRArgInfo
    {
      public IntPtr key;
      public IntPtr value;
      public IntPtr name;
      public IntPtr description;
      public IntPtr units;
      public SoapySDRArgInfoType type;
      public SoapySDRRange range;
      public nint numOptions;
      public IntPtr options;
      public IntPtr optionNames;
    }



    // delegate for the log handler
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SoapySDRLogHandlerDelegate(SoapySDRLogLevel logLevel, IntPtr messagePtr);


    public const string DLL_NAME = "SoapySDR";

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int SoapySDRDevice_lastStatus();

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr SoapySDRDevice_lastError();

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr SoapySDRDevice_enumerateStrArgs(string args, out nint length);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SoapySDRKwargsList_clear(IntPtr args, nint length);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SoapySDRKwargs_clear(IntPtr args);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SoapySDRArgInfo_clear(IntPtr info);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SoapySDRDevice_make(IntPtr args);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern SoapySDRRange SoapySDRDevice_getGainRange(IntPtr device, Direction direction, nint channel);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SoapySDRDevice_getFrequencyRange(IntPtr device, Direction direction, nint channel, out nint length);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SoapySDRDevice_getSampleRateRange(IntPtr device, Direction direction, nint channel, out nint length);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SoapySDRDevice_getBandwidthRange(IntPtr device, Direction direction, nint channel, out nint length);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern double SoapySDRDevice_getGain(IntPtr device, Direction direction, nint channel);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern double SoapySDRDevice_getFrequency(IntPtr device, Direction direction, nint channel);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern double SoapySDRDevice_getSampleRate(IntPtr device, Direction direction, nint channel);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern double SoapySDRDevice_getBandwidth(IntPtr device, Direction direction, nint channel);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SoapySDRDevice_getSettingInfo(IntPtr device, out nint length);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SoapySDRDevice_unmake(IntPtr device);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern double SoapySDRDevice_getGainElement(IntPtr device, Direction direction, nint channel, string name);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SoapySDRDevice_listGains(IntPtr device, Direction direction, nint channel, out nint length);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern SoapySDRRange SoapySDRDevice_getGainElementRange(IntPtr device, Direction direction, nint channel, string name);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SoapySDRDevice_listAntennas(IntPtr device, Direction direction, nint channel, out nint length);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr SoapySDRDevice_getAntenna(IntPtr device, Direction direction, nint channel);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool SoapySDRDevice_hasDCOffsetMode(IntPtr device, Direction direction, nint channel);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool SoapySDRDevice_getDCOffsetMode(IntPtr device, Direction direction, nint channel);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool SoapySDRDevice_hasIQBalanceMode(IntPtr device, Direction direction, nint channel);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool SoapySDRDevice_getIQBalanceMode(IntPtr device, Direction direction, nint channel);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool SoapySDRDevice_hasGainMode(IntPtr device, Direction direction, nint channel);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool SoapySDRDevice_getGainMode(IntPtr device, Direction direction, nint channel);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SoapySDRDevice_readSetting(IntPtr device, string key);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SoapySDRDevice_setGain(IntPtr device, Direction direction, nint channel, double value);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SoapySDRDevice_setFrequency(IntPtr device, Direction direction, nint channel, double frequency, IntPtr args);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SoapySDRDevice_setSampleRate(IntPtr device, Direction direction, nint channel, double rate);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SoapySDRDevice_setBandwidth(IntPtr device, Direction direction, nint channel, double bw);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SoapySDRDevice_setupStream(IntPtr device, Direction direction, string format, IntPtr channels, nint numChans, IntPtr args);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SoapySDRDevice_getStreamArgsInfo(IntPtr device, Direction direction, nint channel, out nint length);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint SoapySDRDevice_getStreamMTU(IntPtr device, IntPtr stream);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern SoapySdrError SoapySDRDevice_activateStream(IntPtr device, IntPtr stream, SoapySdrFlags flags, long timeNs, nint numElems);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SoapySDRDevice_readStream(IntPtr device, IntPtr stream, IntPtr buffs, nint numElems,
       out SoapySdrFlags flags, out long timeNs, long timeoutUs);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern SoapySdrError SoapySDRDevice_deactivateStream(IntPtr device, IntPtr stream, SoapySdrFlags flags, out long timeNs);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SoapySDRDevice_enumerate(IntPtr args, out nint length);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SoapySDRDevice_closeStream(IntPtr device, IntPtr stream);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern SoapySdrError SoapySDRDevice_writeSetting(IntPtr device, string key, string value);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern SoapySdrError SoapySDRDevice_setAntenna(IntPtr device, Direction direction, nint channel, string name);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern SoapySdrError SoapySDRDevice_setDCOffsetMode(IntPtr device, Direction direction, nint channel, bool automatic);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern SoapySdrError SoapySDRDevice_setIQBalanceMode(IntPtr device, Direction direction, nint channel, bool automatic);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern SoapySdrError SoapySDRDevice_setGainMode(IntPtr device, Direction direction, nint channel, bool automatic);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern SoapySdrError SoapySDRDevice_setGainElement(IntPtr device, Direction direction, nint channel, string name, double value);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SoapySDR_registerLogHandler(SoapySDRLogHandlerDelegate handler);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SoapySDR_setLogLevel(SoapySDRLogLevel logLevel);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SoapySDR_getABIVersion();


    
  }
}
