using System.ComponentModel;
using System.Drawing.Design;

namespace SkyRoof
{
  //----------------------------------------------------------------------------------------------
  //                                  TransverterBand
  //----------------------------------------------------------------------------------------------
  // Defines RF frequency range and LO offset for a single transverter band.
  // LO is subtracted from RF to obtain the IF (RF - LO = IF).
  // No [TypeConverter] on the class — Newtonsoft.Json 13 would otherwise see the default
  // TypeConverter as string-convertible (via CanConvertTo(string)) and serialize each band
  // as its ToString(), which round-trips through ConvertFrom(string) and throws on load.
  public class TransverterBand
  {
    [DefaultValue("")]
    public string Name { get; set; } = string.Empty;

    [DisplayName("RF Low (Hz)")]
    [Description("Lowest RF frequency (Hz) in this transverter band, inclusive")]
    public long RfLow { get; set; }

    [DisplayName("RF High (Hz)")]
    [Description("Highest RF frequency (Hz) in this transverter band, inclusive")]
    public long RfHigh { get; set; }

    [DisplayName("LO Offset (Hz)")]
    [Description("Local oscillator offset (Hz). IF = RF - LO")]
    public long LoOffset { get; set; }

    public override string ToString()
    {
      return string.IsNullOrEmpty(Name) ? $"{RfLow / 1e6:F3}-{RfHigh / 1e6:F3} MHz" : Name;
    }
  }




  //----------------------------------------------------------------------------------------------
  //                                  TransverterSettings
  //----------------------------------------------------------------------------------------------
  public class TransverterSettings
  {
    [DisplayName("SDR Offset Enabled")]
    [Description("Subtract the SDR LO offset from the SDR center frequency")]
    [DefaultValue(false)]
    public bool SdrOffsetEnabled { get; set; } = false;

    [DisplayName("RX CAT Offset Enabled")]
    [Description("Subtract the CAT LO offset from the RX frequency sent to the radio")]
    [DefaultValue(false)]
    public bool RxCatOffsetEnabled { get; set; } = false;

    [DisplayName("TX CAT Offset Enabled")]
    [Description("Subtract the CAT LO offset from the TX frequency sent to the radio")]
    [DefaultValue(false)]
    public bool TxCatOffsetEnabled { get; set; } = false;

    // null means the field was absent from JSON (pre-feature install).
    // an empty list is a deliberate user choice and must be preserved.
    [DisplayName("SDR Bands")]
    [Description("Transverter bands for the SDR path. Right-click and Reset to restore defaults.")]
    [Editor("System.ComponentModel.Design.CollectionEditor, System.Design", typeof(UITypeEditor))]
    public List<TransverterBand>? SdrBands { get; set; }

    [DisplayName("CAT Bands")]
    [Description("Transverter bands for the CAT path. Right-click and Reset to restore defaults.")]
    [Editor("System.ComponentModel.Design.CollectionEditor, System.Design", typeof(UITypeEditor))]
    public List<TransverterBand>? CatBands { get; set; }


    internal void SetDefaults()
    {
      if (SdrBands == null) SdrBands = BuildDefaultSdrBands();
      if (CatBands == null) CatBands = BuildDefaultCatBands();
    }

    // Invoked from the PropertyGrid Reset context-menu command on the band lists.
    public void ResetSdrBands() => SdrBands = BuildDefaultSdrBands();
    public void ResetCatBands() => CatBands = BuildDefaultCatBands();

    private static List<TransverterBand> BuildDefaultSdrBands() => new()
    {
      new TransverterBand { Name = "VHF 2m",    RfLow = 144_000_000, RfHigh = 148_000_000, LoOffset = 116_000_000 },
      new TransverterBand { Name = "UHF 70cm",  RfLow = 432_000_000, RfHigh = 438_000_000, LoOffset = 407_000_000 },
    };

    private static List<TransverterBand> BuildDefaultCatBands() => new()
    {
      new TransverterBand { Name = "VHF 2m",               RfLow = 145_800_000, RfHigh = 146_000_000, LoOffset = 116_000_000 },
      new TransverterBand { Name = "UHF 70cm AO-7 uplink", RfLow = 432_000_000, RfHigh = 434_000_000, LoOffset = 404_000_000 },
      new TransverterBand { Name = "UHF 70cm",             RfLow = 435_000_000, RfHigh = 437_000_000, LoOffset = 407_000_000 },
    };


    //----------------------------------------------------------------------------------------------
    //                                  lookup helpers
    //----------------------------------------------------------------------------------------------
    public TransverterBand? GetSdrBand(double rfFrequency)
    {
      if (SdrBands == null) return null;
      return SdrBands.FirstOrDefault(b => rfFrequency >= b.RfLow && rfFrequency <= b.RfHigh);
    }

    public TransverterBand? GetCatBand(double rfFrequency)
    {
      if (CatBands == null) return null;
      return CatBands.FirstOrDefault(b => rfFrequency >= b.RfLow && rfFrequency <= b.RfHigh);
    }

    public long GetSdrLoOffset(double rfFrequency)
    {
      return GetSdrBand(rfFrequency)?.LoOffset ?? 0;
    }

    public long GetCatLoOffset(double rfFrequency)
    {
      return GetCatBand(rfFrequency)?.LoOffset ?? 0;
    }

    public override string ToString() { return string.Empty; }
  }
}
