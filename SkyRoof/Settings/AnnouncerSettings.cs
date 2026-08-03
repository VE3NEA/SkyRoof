using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSCore.CoreAudioAPI;
using VE3NEA;
using static SkyRoof.AosAnnouncement;

namespace SkyRoof
{
  public class AnnouncerSettings
  {
    [DisplayName("Audio Device")]
    [Description("Soundcard for voice announcements")]
    [TypeConverter(typeof(OutputSoundcardNameConverter))]
    public string? Soundcard { get; set; } = VE3NEA.Soundcard.GetDefaultSoundcardId(DataFlow.Render);

    [DisplayName("Voice")]
    [Description("The voice to use for announcments")]
    [TypeConverter(typeof(VoiceNameConverter))]
    public string? Voice { get; set; }

    [DefaultValue(50)]
    public int Volume { get; set; } = 50;

    [DisplayName("AOS Announcement #1")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public AosAnnouncement Announcement1 { get; set; } = new(2, "Satellite {name} is rising in {minutes} minutes");

    [DisplayName("AOS Announcement #2")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public AosAnnouncement Announcement2 { get; set; } = new(0, "Satellite {name} is rising now");

    [DisplayName("Position Announcement")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public PositionAnnouncement PositionAnnouncement { get; set; } = new();

    public override string ToString() { return ""; }
  }


  public class AosAnnouncement
  {
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;

    [DisplayName("Minutes before AOS")]
    public int Minutes { get; set; }

    public string Message { get; set; }


    public override string ToString() { return ""; }

    public AosAnnouncement(int minutes, string message)
    {
      Minutes = minutes;
      Message = message;
    }
  }


  public class PositionAnnouncement
  {
    [DefaultValue(false)]
    public bool Enabled { get; set; } = false;

    [DisplayName("Degrees Between Announcements")]
    [Description("The angle between the previously announced and current satellite positions")]
    [DefaultValue(10)]
    public int Degrees { get; set; } = 10;

    public string Message { get; set; } = "Azimuth {azimuth} degrees, elevation {elevation} degrees";


    public override string ToString() { return ""; }
  }
}
