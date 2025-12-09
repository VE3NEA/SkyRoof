using System.ComponentModel;
using CSCore.CoreAudioAPI;
using VE3NEA;

public enum Ft4AudioSource
{
  SDR,
  Soundcard,
}

namespace SkyRoof
{
  public class Ft4ConsoleSettings
  {
    [DefaultValue(Ft4AudioSource.SDR)]
    public Ft4AudioSource AudioSource { get; set; } = Ft4AudioSource.SDR;

    [DisplayName("RX Soundcard")]
    [TypeConverter(typeof(InputSoundcardNameConverter))]
    public string? RxSoundcard { get; set; } = Soundcard.GetDefaultSoundcardId(DataFlow.Capture);

    [DisplayName("TX Soundcard")]
    [TypeConverter(typeof(OutputSoundcardNameConverter))]
    public string? TxSoundcard { get; set; } = Soundcard.GetDefaultSoundcardId(DataFlow.Render);

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public Ft4WaterfallSettings Waterfall { get; set; } = new ();

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public Ft4MessagesSettings Messages { get; set; } = new ();

    [DisplayName("Enable Transmit")]
    [DefaultValue(false)]
    public bool EnableTransmit { get; set; } = false;

    [DisplayName("TX Gain")]
    [Description(" Amplify or attenuate TX data, dB")]
    [DefaultValue(0)]
    public int TxGain { get; set; } = 0;

    public override string ToString() { return string.Empty; }
  }

  public class Ft4WaterfallSettings
  {
    [DisplayName("Bandwidth")]
    [Description("Waterfall Bandwidth, Hz")]
    [DefaultValue(4000)]
    public int Bandwidth { get; set; } = 4000;

    [DisplayName("Waterfall Brightness")]
    [DefaultValue(50)]
    public int Brightness { get; set; } = 50;

    [DisplayName("Waterfall Contrast")]
    [DefaultValue(50)]
    public int Contrast { get; set; } = 50;

    public override string ToString() { return ""; }
  }

  public class Ft4MessagesSettings
  {
    [DisplayName("Text Color")]
    public Color TextColor { get; set; } = SystemColors.WindowText;

    [DisplayName("Font Size")]
    [DefaultValue(11f)]
    public float FontSize { get; set; } = 11f;

    [DisplayName("Background Colors")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public Ft4BackgroundColors BkColors { get; set; } = new();

    public override string ToString() { return ""; }
  }

  public class Ft4BackgroundColors
  {
    [DisplayName("Window")]
    [DefaultValue(typeof(Color), "White")]
    public Color Window { get; set; } = Color.White; // SystemColors.Window;

    [DisplayName("Separator")]
    [DefaultValue(typeof(Color), "233, 233, 233")]
    public Color Separator { get; set; } = Color.FromArgb(233, 233, 233);

    [DisplayName("Transmitted Message")]
    [DefaultValue(typeof(Color), "Red")]
    public Color TxMessage { get; set; } = Color.Red;

    [DisplayName("Message To Me")]
    [DefaultValue(typeof(Color), "255, 175, 175")]
    public Color ToMe { get; set; } = Color.FromArgb(255, 175, 175);

    [DisplayName("Message From Me")]
    [DefaultValue(typeof(Color), "175, 255, 175")]
    public Color FromMe { get; set; } = Color.FromArgb(175, 255, 175);

    [DisplayName("CQ Word")]
    [DefaultValue(typeof(Color), "Yellow")]
    public Color CqWord { get; set; } = Color.Yellow;

    [DisplayName("Ap")]
    [Description("Ap mark (a priori information used by decoder")]
    [DefaultValue(typeof(Color), "Orange")]
    public Color Ap { get; set; } = Color.Orange;

    [DisplayName("Hot Item")]
    [DefaultValue(typeof(Color), "20, 0, 0, 255")]
    public Color Hot { get; set; } = Color.FromArgb(20, 0, 0, 255);

    [DisplayName("SNR")]
    [Description("Shades of this color will represent the SNR of the received signals")]
    [DefaultValue(typeof(Color), "Red")]
    public Color Snr { get; set; } = Color.Red;

    public override string ToString() { return ""; }
  }
}
