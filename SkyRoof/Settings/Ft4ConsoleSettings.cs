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
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public Ft4ReceiveSettings Receive { get; set; } = new();

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public Ft4TransmitSettings Transmit { get; set; } = new();

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public Ft4WaterfallSettings Waterfall { get; set; } = new ();

    [DisplayName("WSJT-X UDP Packets")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public UdpSenderSettings UdpSender { get; set; } = new();

    [DisplayName("Decoded Messages")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public Ft4MessagesSettings Messages { get; set; } = new ();

    [Browsable(false)]
    [DefaultValue(266)]
    public int SplitterDistance { get; set; } = 266;


    public override string ToString() { return string.Empty; }
  }


  public class Ft4ReceiveSettings
  {
    [DisplayName("Audio Source")]
    [DefaultValue(Ft4AudioSource.SDR)]
    public Ft4AudioSource AudioSource { get; set; } = Ft4AudioSource.SDR;

    [DisplayName("Soundcard")]
    [TypeConverter(typeof(InputSoundcardNameConverter))]
    public string? RxSoundcard { get; set; } = Soundcard.GetDefaultSoundcardId(DataFlow.Capture);

    [Description("Waterfall and decoder bandwidth, Hz")]
    [DefaultValue(4000)]
    public int Bandwidth { get; set; } = 4000;

    public override string ToString() { return ""; }
  }

  public class Ft4TransmitSettings
  {
    [DefaultValue(false)]
    public bool Enabled { get; set; } = false;

    [DisplayName("Soundcard")]
    [TypeConverter(typeof(OutputSoundcardNameConverter))]
    public string? TxSoundcard { get; set; } = Soundcard.GetDefaultSoundcardId(DataFlow.Render);

    [DisplayName("Gain")]
    [Description("Amplify or attenuate TX data, dB")]
    [DefaultValue(-30)]
    public int TxGain { get; set; } = -30;

    [DisplayName("Watchdog")]
    [Description("Stop transmission after N minutes")]
    [DefaultValue(6)]
    public int TxWatchDog { get; set; } = 6;

    [DisplayName("Use XIT")]
    [Description("Enable transmitter offset when sending FT4")]
    [DefaultValue(true)]
    public bool XitEnabled { get; set; } = true;

    public override string ToString() { return ""; }
  }

  public class Ft4WaterfallSettings
  {
    [DefaultValue(50)]
    public int Brightness { get; set; } = 50;

    [DefaultValue(50)]
    public int Contrast { get; set; } = 50;

    public override string ToString() { return ""; }
  }

  public class UdpSenderSettings
  {
    [DefaultValue(false)]
    [Description("Send decoded messages as UDP packets in the WSJT-X format")]
    public bool Enabled { get; set; }

    [DisplayName("UDP Port")]
    [DefaultValue((ushort)7311)]
    public ushort Port { get; set; } = 7311;

    [DefaultValue("127.0.0.1")]
    public string Host { get; set; } = "127.0.0.1";

    public override string ToString() { return string.Empty; }
  }

  public class Ft4MessagesSettings
  {
    [DisplayName("Save to File")]
    [Description("Save decoded messages to a file")]
    [DefaultValue(false)]
    public bool ArchiveToFile { get; set; }

    [DisplayName("Text Color")]
    [DefaultValue(typeof(SystemColors), "WindowText")]
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
    [DefaultValue(typeof(Color), "LightCoral")]
    public Color TxMessage { get; set; } = Color.LightCoral;

    [DisplayName("Message To Me")]
    [DefaultValue(typeof(Color), "LightGreen")]
    public Color ToMe { get; set; } = Color.LightGreen;

    [DisplayName("Message From Me")]
    [DefaultValue(typeof(Color), "Red")]
    public Color FromMe { get; set; } = Color.Red;

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
