using System.ComponentModel;

namespace SkyRoof
{
  public class TelemetrySettings
  {
    [DisplayName("Save to File")]
    [Description("Save decoded frames to a file")]
    [DefaultValue(false)]
    public bool ArchiveToFile { get; set; }

    [DisplayName("KISS Server")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public KissServerSettings KissServer { get; set; } = new();

    [DisplayName("SatNOGS Upload")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public SatnogsUploaderSettings SatnogsUploader { get; set; } = new();

    [Browsable(false)]
    [DefaultValue(247)]
    public int SplitterDistance { get; set; } = 247;

    [Browsable(false)]
    [DefaultValue(416)]
    public int ImageSplitterDistance { get; set; } = 416;

    // set by the "do not show this message again" checkbox on the FM speech model-download prompt
    [Browsable(false)]
    [DefaultValue(false)]
    public bool SuppressFmModelPrompt { get; set; }


    public override string ToString() { return string.Empty; }
  }
}
