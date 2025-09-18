﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VE3NEA;

namespace SkyRoof
{
  public interface IControlEngineSettings
  {
    public int Delay {get; set; }
    public bool LogTraffic { get; set; }
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
