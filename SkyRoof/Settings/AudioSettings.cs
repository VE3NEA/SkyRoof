using System.ComponentModel;
using CSCore.CoreAudioAPI;
using VE3NEA;

namespace SkyRoof
{
  public class AudioSettings
  {
    // non-browsable
    public int SoundcardVolume = -25;
    public bool SpeakerEnabled = true;

    [DisplayName("Speaker Audio Device")]
    [Description("Soundcard for audio output")]
    [TypeConverter(typeof(OutputSoundcardNameConverter))]
    public string? SpeakerSoundcard { get; set; } = Soundcard.GetDefaultSoundcardId(DataFlow.Render);

    public override string ToString() { return string.Empty; }
  }
}
