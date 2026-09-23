using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Drawing.Design;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.Design;
using VE3NEA;

namespace SkyRoof
{
  public interface IControlEngineSettings
  {
    public int Delay {get; set; }
    public bool LogTraffic { get; set; }
    public int SendTimeout { get; set; }
    public int ReceiveTimeout { get; set; }
    public int ReconnectDelay { get; set; }
  }

  public class CatSettings : IControlEngineSettings
  {
    [Description("Delay between the command cycles")]
    [DisplayName("Delay (ms)")]
    [DefaultValue(100)]
    public int Delay { get; set; } = 500;

    [DisplayName("Log Traffic")]
    [Description("Log command traffic for debugging")]
    [DefaultValue(false)]
    public bool LogTraffic { get; set; }

    [DisplayName("Send Timeout")]
    [Description("TCP send timeout in milliseconds")]
    [DefaultValue(1000)]
    public int SendTimeout { get; set; } = 1000;

    [DisplayName("Receive Timeout")]
    [Description("TCP receive timeout in milliseconds. Increase for high-latency networks or remote rigctld.")]
    [DefaultValue(3000)]
    public int ReceiveTimeout { get; set; } = 3000;

    [DisplayName("Reconnect Delay")]
    [Description("Milliseconds to wait between reconnect attempts after a connection is lost.")]
    [DefaultValue(5000)]
    public int ReconnectDelay { get; set; } = 5000;

    [DefaultValue(false)]
    [DisplayName("Ignore Dial Knob")]
    [Description("Tune only from the software")]
    public bool IgnoreDialKnob { get; set; } = false;

    [DefaultValue(10)]
    [DisplayName("Tuning Step (Hz)")]
    [Description("The frequencies sent to the radio will be rounded to this step.")]
    public int TuningStep { get; set; } = 10;

    [DisplayName("RX CAT")]
    [Description("RX CAT Control via rigctld.exe")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public CatRadioSettings RxCat { get; set; } = new();

    [DisplayName("TX CAT")]
    [Description("TX CAT Control via rigctld.exe")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public CatRadioSettings TxCat { get; set; } = new();

    [DisplayName("SkyCAT Daemon")]
    [Description("Start and stop skycatd.exe together with SkyRoof")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public SkyCatSettings SkyCat { get; set; } = new();

    public override string ToString() { return string.Empty; }
  }

  // skycatd.exe is the CAT daemon of the SkyCAT package. It serves one radio on one TCP port, so
  // a single daemon is started for the local CAT endpoint; two-radio setups still run their own.
  public class SkyCatSettings
  {
    [DefaultValue(false)]
    [DisplayName("Auto Start")]
    [Description("Start skycatd.exe when SkyRoof starts, and restart it if it exits. Ignored when CAT control is disabled or the radio is on a remote host. skycatd serves one radio on one port, so a second radio needs a second daemon started by hand.")]
    public bool Enabled { get; set; } = false;

    [DefaultValue(true)]
    [DisplayName("Stop On Exit")]
    [Description("Stop skycatd.exe when SkyRoof closes, and whenever its configuration stops applying - Auto Start switched off, CAT control switched off, or the CAT host changed to another computer. Only a daemon that SkyRoof started is ever stopped. Changing this applies to a daemon that is already running, in either direction.")]
    public bool StopOnExit { get; set; } = true;

    [DefaultValue("")]
    [DisplayName("Executable")]
    [Description(@"Full path to skycatd.exe, e.g. C:\Program Files\SkyCAT\skycatd.exe")]
    [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
    public string ExePath { get; set; } = "";

    [DefaultValue("")]
    [DisplayName("Command Tail")]
    [Description(@"Command line arguments for skycatd.exe, e.g. -m IC-9700 -r COM9 -s 115200 -f. Do not pass -t: the daemon has to serve the TCP Port from the CAT settings, which SkyRoof appends for you.")]
    public string Arguments { get; set; } = "";

    [DefaultValue(false)]
    [DisplayName("Log Output")]
    [Description("Copy skycatd.exe output into the SkyRoof log. The SkyCAT panel shows the output either way, so leave this off when running skycatd with a verbose option.")]
    public bool LogOutput { get; set; } = false;

    [DefaultValue(5000)]
    [DisplayName("Startup Timeout")]
    [Description("Milliseconds to wait for skycatd.exe to start listening on the CAT port before giving up, up to 30000. Applies when SkyRoof starts; a later settings change never waits.")]
    public int StartupTimeout { get; set; } = 5000;

    public override string ToString() { return string.Empty; }
  }

  public class CatRadioSettings
  {
    [DefaultValue("127.0.0.1")]
    [Description("rigctld host")]
    public string Host { get; set; } = "127.0.0.1";

    [DisplayName("TCP Port")]
    [Description("rigctld port")]
    [DefaultValue((ushort)4532)]
    public ushort Port { get; set; } = 4532;

    [DefaultValue(false)]
    public bool Enabled { get; set; }

    [DisplayName("Show Corrected Frequency")]
    [Description("Show the frequency with all corrections (True) or the nominal frequency (False)")]
    [DefaultValue(true)]
    public bool ShowCorrectedFrequency { get; set; } = true;

    public override string ToString() { return string.Empty; }
  }
}
