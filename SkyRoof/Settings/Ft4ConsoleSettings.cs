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

    [DisplayName("TX Gain")]
    [Description(" Amplify or attenuate TX data, dB")]
    [DefaultValue(0)]
    public int TxGain { get; set; } = 0;

    [DisplayName("Enable Transmit")]
    [DefaultValue(false)]
    public bool EnableTransmit { get; set; } = false;

    [DisplayName("Waterfall Brightness")]
    [DefaultValue(50)]
    public int WaterfallBrightness { get; set; } = 50;

    [DisplayName("Waterfall Contrast")]
    [DefaultValue(50)]
    public int WaterfallContrast { get; set; } = 50;

    public override string ToString() { return string.Empty; }
  }
}
