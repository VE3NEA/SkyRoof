using System.ComponentModel;

namespace SkyRoof
{
  public class UserSettings
  {
    [DisplayName("Callsign")]
    [Description("Your callsign")]
    public string Call { get; set; } = "";

    [DisplayName("Grid Square")]
    [Description("Your grid square")]
    public string Square { get; set; } = "";

    [DisplayName("Altitude")]
    [Description("Your altitude above the sea level, in meters")]
    public int Altitude { get; set; } = 0;

    public override string ToString() { return ""; }
  }
}