using System.ComponentModel;
using VE3NEA;

namespace SkyRoof
{
  // the denoising filter applied to an SSTV image before it is auto-saved
  public enum SstvSaveFilter
  {
    [Description("None")]
    None,
    [Description("Wiener")]
    Wiener,
    [Description("Non-local means")]
    Nlm,
  }

  public class TelemetrySettings
  {
    [DisplayName("Save to File")]
    [Description("Save decoded frames to a file")]
    [DefaultValue(false)]
    public bool ArchiveToFile { get; set; }

    [DisplayName("SSTV Auto-Save Filter")]
    [Description("Denoising filter applied to SSTV images when they are saved automatically")]
    [DefaultValue(SstvSaveFilter.Wiener)]
    [TypeConverter(typeof(EnumDescriptionConverter))]
    public SstvSaveFilter SstvSaveFilter { get; set; } = SstvSaveFilter.Wiener;

    [DisplayName("KISS Server")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public KissServerSettings KissServer { get; set; } = new();

    [DisplayName("SatNOGS Upload")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public SatnogsUploaderSettings SatnogsUploader { get; set; } = new();

    [Browsable(false)]
    [DefaultValue(247)]
    public int SplitterDistance { get; set; } = 247;

    // height of the text sub-panel below the image. This is the fixed panel of ImageSplitContainer, so it,
    // and not the splitter distance, is the quantity that survives a resize of the panel
    [Browsable(false)]
    [DefaultValue(106)]
    public int ImageTextHeight { get; set; } = 106;


    public override string ToString() { return string.Empty; }
  }
}
