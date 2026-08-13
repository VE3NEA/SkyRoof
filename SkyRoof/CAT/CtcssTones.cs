namespace SkyRoof
{
  // the standard sub-audible CTCSS (PL) tones, and the values the FM satellites use
  public static class CtcssTones
  {
    // tone used by most FM satellites that require a continuous access tone
    public const double DEFAULT_TONE = 67.0;

    // SO-50 arms its 10-minute timer on a 2-second carrier with this tone
    public const double ARMING_TONE = 74.4;

    // duration of the arming carrier, in milliseconds
    public const int ARMING_DURATION_MS = 2000;

    // the 38 tones that every radio with a CAT tone command can produce. the 11 extended tones
    // (159.8 .. 254.1) and 69.3 are omitted: the TS-2000 has neither, and the FT-847 has no
    // extended tones either. no FM satellite uses the omitted tones
    public static readonly double[] All =
    [
       67.0,  71.9,  74.4,  77.0,  79.7,  82.5,  85.4,  88.5,  91.5,  94.8,
       97.4, 100.0, 103.5, 107.2, 110.9, 114.8, 118.8, 123.0, 127.3, 131.8,
      136.5, 141.3, 146.2, 151.4, 156.7, 162.2, 167.9, 173.8, 179.9, 186.2,
      192.8, 203.5, 210.7, 218.1, 225.7, 233.6, 241.8, 250.3
    ];

    // rigctld and SkyCAT take the tone in tenths of Hz: 74.4 Hz -> 744
    public static int ToTenths(double toneHz)
    {
      return (int)Math.Round(toneHz * 10);
    }

    public static string Format(double toneHz)
    {
      return toneHz.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
    }
  }
}
