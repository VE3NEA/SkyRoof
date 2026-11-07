using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyRoof
{
  public class AmsatSettings
  {
    [DisplayName("Download Enabled")]
    [Description("Download satellite status info from the AMSAT Satellite Status API (status.amsat.org)")]
    [DefaultValue(false)]
    public bool Enabled { get; set; } = false;

    public override string ToString() { return ""; }
  }
}
