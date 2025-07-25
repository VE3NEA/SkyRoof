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
    [Description("Delay between the command cycles, ms")]
    [DefaultValue(100)]
    public int Delay { get; set; } = 100;

    [DisplayName("Log Traffic")]
    [Description("Log command traffic for debugging")]
    [DefaultValue(false)]
    public bool LogTraffic { get; set; }

    [DefaultValue(false)]
    [DisplayName("Ignore Dial Knob")]
    [Description("Tune only from the software")]
    public bool IgnoreDialKnob { get; set; } = false;

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

    [TypeConverter(typeof(RadioModelConverter))]
    [DisplayName("Radio Type")]
    [Description("Defines the capabilities of the radio")]
    [DefaultValue("Duplex transceiver")]
    public string RadioType { get; set; } = "Duplex transceiver";

    [DisplayName("Show Corrected Frequency")]
    [Description("Show the frequency with all corrections (True) or the nominal frequency (False)")]
    [DefaultValue(true)]
    public bool ShowCorrectedFrequency { get; set; } = true;

    public override string ToString() { return string.Empty; }
  }
}
