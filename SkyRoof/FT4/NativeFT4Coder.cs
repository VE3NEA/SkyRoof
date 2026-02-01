
using System.Runtime.InteropServices;
using System.Text;

namespace VE3NEA
{
  public class NativeFT4Coder
  {
    public enum QsoStage
    {
      CALLING,
      REPLYING,
      REPORT,
      ROGER_REPORT,
      ROGERS,
      SIGNOFF,
    }

    public const int SAMPLES_PER_SYMBOL = 576 * 4; // 2304
    private const int SYMBOLS_PER_MESSAGE = 16 + 87; // 103

    public const int SAMPLING_RATE = 48_000;
    public const int DEFAULT_AUDIO_FREQUENCY = 1500;

    public const int DECODE_SAMPLE_COUNT = 21 * 3456 * 4; // 290304, 6.048s
    public const int DECODE_MAX_CHARS = 16384; // buffer size for all decoded messages

    public const int ENCODE_MESSAGE_LENGTH = 37;
    public const int ENCODE_SAMPLE_COUNT = (SYMBOLS_PER_MESSAGE + 2) * SAMPLES_PER_SYMBOL; // 241920, 5.04s
    public const double ENCODE_SECONDS = ENCODE_SAMPLE_COUNT / (long)SAMPLING_RATE;

    public const double TIMESLOT_SECONDS = 7.5;
    public const double DECODE_SECONDS = DECODE_SAMPLE_COUNT / (double)SAMPLING_RATE;

    public const int MAX_CALL_LENGTH = 12;
    public const long SLOTS_PER_DAY = 11520; // 86400 / 7.5
    public const int TIME_SLOTS_PER_MINUTE = 4;

    [DllImport("ft4_coder", CallingConvention = CallingConvention.Cdecl)]
    public static extern void encode_ft4(byte[] message, ref float txAudioFrequency, float[] audioSamples);

    [DllImport("ft4_coder", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void decode_ft4(
        [In] float[] audio_samples,
        ref QsoStage nQSOProgress,
        ref int nfqso,
        ref int nfb,
        [In] byte[] mycall,
        [In] byte[] hiscall,
        DecodedMessageCallback callback);

    // callback delegate
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void DecodedMessageCallback(IntPtr message);
  }
}
