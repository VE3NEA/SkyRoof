using System.ComponentModel;
using CSCore.CoreAudioAPI;
using VE3NEA;

namespace SkyRoof
{
  public enum DataStreamType
  {
    [Description("I/Q to VAC")]
    IqToVac,
    [Description("Audio to VAC")]
    AudioToVac,
    [Description("I/Q to UDP")]
    IqToUdp,
    [Description("Audio to UDP")]
    AudioToUdp,
  }

  public class OutputStreamSettings
  {
    public bool Enabled;

    [DefaultValue(DataStreamType.IqToVac)]
    [TypeConverter(typeof(EnumDescriptionConverter))]
    public DataStreamType Type { get; set; } = DataStreamType.IqToVac;

    [DisplayName("Gain, dB")]
    [Description(" Amplify stream data by Gain, dB")]
    [DefaultValue(0)]
    public int Gain { get; set; } = 0;

    [DisplayName("VAC Device")]
    [Description("Virtual Audio Cable device")]
    [TypeConverter(typeof(OutputSoundcardNameConverter))]
    public string? Vac { get; set; } = Soundcard.GetFirstVacId(DataFlow.Render);

    [DisplayName("UDP Port")]
    [DefaultValue((ushort)7355)]
    public ushort UdpPort { get; set; } = 7355;


    public override string ToString() { return string.Empty; }
  }
}
