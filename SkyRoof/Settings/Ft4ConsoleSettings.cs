using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSCore.CoreAudioAPI;
using VE3NEA;

public enum FtrAudioSource
{
  SDR,
  SoundCard,
}

namespace SkyRoof
{
  public class Ft4ConsoleSettings
  {
    [DefaultValue(FtrAudioSource.SDR)]
    public FtrAudioSource AudioSource { get; set; } = FtrAudioSource.SDR;

    [DisplayName("RX Soundcard")]
    [TypeConverter(typeof(InputSoundcardNameConverter))]
    public string? RxSoundcard { get; set; } = Soundcard.GetDefaultSoundcardId(DataFlow.Capture);

    [DisplayName("TX Soundcard")]
    [TypeConverter(typeof(OutputSoundcardNameConverter))]
    public string? TxSoundcard { get; set; } = Soundcard.GetDefaultSoundcardId(DataFlow.Render);

    [DisplayName("RX Gain, dB")]
    [Description(" Amplify or attenuate RX data, dB")]
    [DefaultValue(0)]
    public int RxGain { get; set; } = 0;

    [DisplayName("TX Gain, dB")]
    [Description(" Amplify or attenuate TX data, dB")]
    [DefaultValue(0)]
    public int TxGain { get; set; } = 0;

    public bool EnableTransmit { get; set; } = false;



    public override string ToString() { return string.Empty; }
  }
}
