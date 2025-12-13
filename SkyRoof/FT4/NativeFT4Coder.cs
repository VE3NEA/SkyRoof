
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

    private const int SAMPLES_PER_SYMBOL = 576 * 4; // 2304
    private const int SYMBOLS_PER_MESSAGE = 16 + 87; // 103

    public const int SAMPLING_RATE = 48_000;

    public const int DECODE_SAMPLE_COUNT = 21 * 3456 * 4; // 290304, 6.048s
    public const int DECODE_MAX_CHARS = 16384; // buffer size for all decoded messages

    public const int ENCODE_MESSAGE_LENGTH = 37;
    public const int ENCODE_SAMPLE_COUNT = (SYMBOLS_PER_MESSAGE + 2) * SAMPLES_PER_SYMBOL; // 241920, 5.04s

    public const double TIMESLOT_SECONDS = 7.5;
    public const double DECODE_SECONDS = DECODE_SAMPLE_COUNT / (double)SAMPLING_RATE;


    [DllImport("ft4_coder", EntryPoint = "encode_ft4_f_", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void encode(string message, ref float tx_audio_frequency, [Out] float[] audio_samples);


    [DllImport("ft4_coder", EntryPoint = "decode_ft4_f_", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void decode(float[] audio_samples, ref QsoStage QsoStage,
      ref int rx_audio_frequency, ref int cutoff_frequency, string my_call, string his_call, StringBuilder decoded_messages);
  }
}
