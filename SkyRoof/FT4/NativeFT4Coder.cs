
using System.Runtime.InteropServices;
using System.Text;

namespace VE3NEA
{
  public class NativeFT4Coder
  {
    public const int SAMPLING_RATE = 48000;

    public const int DECODE_SAMPLE_COUNT = 21 * 3456 * 4;
    public const int DECODE_MAX_CHARS = 16384; // buffer size for all messages

    public const int ENCODE_MESSAGE_LENGTH = 37;
    public const int ENCODE_SAMPLE_COUNT = (16 + 87 + 2) * 576 * 4;

    public const double TIMESLOT_SECONDS = 7.5; 
    public const double DECODE_SECONDS = DECODE_SAMPLE_COUNT / (double)SAMPLING_RATE;


    [DllImport("ft4_coder", EntryPoint = "decode_ft4_f_", CallingConvention = CallingConvention.Cdecl)]
    public static extern void decode(float[] samples, StringBuilder messages);

    [DllImport("ft4_coder", EntryPoint = "encode_ft4_f_", CallingConvention = CallingConvention.Cdecl)]
    public static extern void encode(string message, ref float audio_frequency, [Out] float[] samples);
  }
}
