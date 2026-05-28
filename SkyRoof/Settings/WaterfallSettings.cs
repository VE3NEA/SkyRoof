using System.ComponentModel;

namespace SkyRoof
{
  public enum WaterfallQuality
  {
    [Description("Auto (maximum detail)")]
    Auto = 0,

    [Description("High")]
    High = 1,

    [Description("Medium")]
    Medium = 2,

    [Description("Low (best for older GPUs)")]
    Low = 3,
  }

  public class WaterfallSettings
  {
    public float Brightness = -0.2f;
    public float Contrast = 0.5f;
    public int Speed = 16;
    public int PaletteIndex = 0;
    public int SplitterDistance = 80;

    [DisplayName("Quality")]
    [Description("Controls internal waterfall spectrum/texture size. Lower values improve performance on older GPUs without changing SDR sampling rate.")]
    [DefaultValue(WaterfallQuality.Auto)]
    [TypeConverter(typeof(VE3NEA.EnumDescriptionConverter))]
    public WaterfallQuality Quality { get; set; } = WaterfallQuality.Auto;

    [DisplayName("Show performance counters")]
    [Description("Show FPS / upload / draw performance counters in the Waterfall sliders popup.")]
    [DefaultValue(false)]
    public bool ShowPerformanceCounters { get; set; } = false;

    public override string ToString() { return string.Empty; }
  }
}
