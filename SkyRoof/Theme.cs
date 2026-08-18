using Newtonsoft.Json.Linq;
using Serilog;

namespace SkyRoof
{
  public enum ThemeMode { System, Light, Dark }

  // The application theme. Application.SetColorMode() lets the framework and the OS render the
  // common controls, the menus, the scroll bars and the title bar, and it flips every SystemColors
  // member, so this class only has to answer for the colors that carry a meaning of their own.
  // See design-docs/theme_switching_plan.md, sections 2, 4.1 and 4.3.
  public static class Theme
  {
    public static ThemeMode Mode { get; private set; }
    public static bool IsDark { get; private set; }

    // The theme is a startup setting: SetColorMode() and the DockPanelSuite theme must both be
    // selected before any window exists, and the DockPanel.Theme setter throws once dock content
    // is open. Called from Program.Main, before the settings are loaded into Settings.Ui, so the
    // mode is read straight from the settings file.
    public static void Initialize()
    {
      Mode = ReadModeFromSettingsFile();

      try
      {
        Application.SetColorMode(ToColorMode(Mode));
      }
      catch (Exception ex)
      {
        // Wine 9 may not implement the underlying dark mode support, stay in light mode
        Log.Warning(ex, "SetColorMode failed");
      }

      // in System mode this follows the OS setting
      IsDark = Application.IsDarkModeEnabled;

      VhfTintBrush = new SolidBrush(VhfTint);
      UhfTintBrush = new SolidBrush(UhfTint);
      NowBrush = new SolidBrush(Now);
    }

    private static SystemColorMode ToColorMode(ThemeMode mode)
    {
      return mode switch
      {
        ThemeMode.Light => SystemColorMode.Classic,
        ThemeMode.Dark => SystemColorMode.Dark,
        _ => SystemColorMode.System
      };
    }

    private static ThemeMode ReadModeFromSettingsFile()
    {
      try
      {
        string fileName = Settings.GetFileName();
        if (!File.Exists(fileName)) return ThemeMode.Light;

        var token = JObject.Parse(File.ReadAllText(fileName)).SelectToken("Ui.Theme");
        if (token == null) return ThemeMode.Light;

        // Newtonsoft writes the enum as a number, Enum.TryParse accepts both forms
        return Enum.TryParse(token.ToString(), out ThemeMode mode) ? mode : ThemeMode.Light;
      }
      catch (Exception ex)
      {
        Log.Warning(ex, "Failed to read the theme setting");
        return ThemeMode.Light;
      }
    }




    // ----------------------------------------------------------------------------------------------
    //                                        themed colors
    // ----------------------------------------------------------------------------------------------
    // Each entry names a role and gives its value in both themes. Colors that are the same in both
    // themes do not belong here: the frequency and az/el readouts, the QSO entry field rings, the
    // status LEDs, the FT4 message colors and the waterfall palette are all deliberately fixed.
    // The color sweep adds the remaining entries.
    private static Color Pick(Color light, Color dark) { return IsDark ? dark : light; }

    // tooltips: the framework paints them light in both modes, ToolTipEx repaints them
    public static Color TipBack => Pick(SystemColors.Info, SystemColors.ControlLight);
    public static Color TipText => Pick(SystemColors.InfoText, SystemColors.ControlText);

    // hyperlinks. Blue is barely readable on the dark surface, aqua replaces it there
    public static Color Link => Pick(Color.Blue, Color.Aqua);

    // Sky view. The plot surface itself is SystemColors.Window and needs no entry here. In the
    // dark theme the disks on it are pulled between two constraints: light enough for the
    // satellite icon (a #0041AC body) to read against them, dark enough for the satellite names,
    // which are WindowText. The values below are the lightest that keep the names at 3:1;
    // going lighter means painting the names dark instead.
    public static Color SkyRealTimeDisk => Pick(Color.FromArgb(230, 249, 255), Color.FromArgb(120, 138, 155));
    public static Color SkyOrbitDisk => Pick(Color.FromArgb(242, 242, 242), Color.FromArgb(125, 125, 125));

    // Band tints, marking the downlink band of a satellite or a transmitter. Dark but still
    // tinted in the dark theme, and dark enough for WindowText to clear 4.5:1 on them - which is
    // why text on a tinted row is simply the system text color in both themes.
    public static Color VhfTint => Pick(Color.LightGoldenrodYellow, Color.FromArgb(86, 74, 30));
    public static Color UhfTint => Pick(Color.LightCyan, Color.FromArgb(30, 80, 92));

    // built in Initialize, once IsDark is known: a static field initializer would run at type
    // init, which happens on the way into Initialize itself
    public static Brush VhfTintBrush { get; private set; } = Brushes.LightGoldenrodYellow;
    public static Brush UhfTintBrush { get; private set; } = Brushes.LightCyan;

    // Text on a list row, tinted or not.
    public static Color RowText(bool inactive)
    {
      return inactive ? SystemColors.GrayText : SystemColors.WindowText;
    }

    public static Brush RowTextBrush(bool inactive)
    {
      return inactive ? SystemBrushes.GrayText : SystemBrushes.WindowText;
    }

    // QSO entry: the card behind each field, and the field's own ring while untouched - the two
    // share a color on purpose, so an untouched ring disappears into its card
    public static Color QsoCard => Pick(Color.LightSkyBlue, Color.FromArgb(34, 48, 60));

    // the ring around a field the operator has edited. QsoEntryPanel stores field state in this
    // color and compares against it (plan 4.2), so it must round-trip through BackColor: keep it
    // a single Theme entry, and never restate either value as a FromArgb literal elsewhere
    public static Color QsoFieldEdited => Pick(Color.Blue, Color.DodgerBlue);

    // The unfilled part of the FT4 bars. ControlLightLight is white in light mode but #1F1F1F in
    // dark, where the bar then disappears into the panel, so the dark end is silver instead.
    public static Color BarRemainder => Pick(Color.White, Color.Silver);

    // "now" marker on a pass row, and the pass path in the mini sky views, which the marker sits
    // on: green is too dark to read on the dark row surface
    public static Color Now => Pick(Color.Green, Color.Lime);
    public static Brush NowBrush { get; private set; } = Brushes.Green;

    // timeline chart: a sky gradient behind the passes, deeper overhead than at the horizon. In
    // the dark theme both ends move down the same scale, keeping that relationship - and the top
    // is a muted MidnightBlue rather than a saturated Navy, so the labels on it stay legible
    public static Color TimelineTop => Pick(Color.SkyBlue, Color.Black);
    public static Color TimelineBottom => Pick(Color.White, Color.RoyalBlue);

    // Earth view: the space around the globe. The light value is the 0.7 gray the panel always
    // cleared to - darker than the panel around it, and the dark value is lighter than its panel,
    // so the surround stays distinct from the chrome in both themes
    public static Color EarthSpace => Pick(Color.FromArgb(179, 179, 179), Color.FromArgb(64, 64, 64));

    // the DXCC world map is a light bitmap and stays content, not chrome: the fragment shader
    // dims it as a whole in the dark theme rather than recoloring it
    public static float EarthMapBrightness => IsDark ? 0.6f : 1;

    // frequency scale: the accent marks the pass that is happening now - its label text, the line
    // under the label, and the frame around the active span
    public static Color ScaleAccent => Pick(Color.Blue, Color.SkyBlue);

    // the transponder span is a wash over the scale. A 20/255 tint that reads on #F0F0F0
    // disappears on #202020, so the dark alpha is doubled
    public static Color ScaleActiveSpan => Pick(Color.FromArgb(20, Color.Blue), Color.FromArgb(40, Color.Aqua));
    public static Color ScaleIdleSpan => Pick(Color.FromArgb(20, Color.Gray), Color.FromArgb(40, Color.Gray));

    // receiver passband: green and lime trade places, since each is invisible against the
    // other theme's background - lime on #F0F0F0 and green on #202020 are both about 1.5:1
    public static Color PassbandFill => Pick(Color.FromArgb(200, Color.Lime), Color.FromArgb(200, Color.Green));
    public static Color PassbandFrame => Pick(Color.Green, Color.Lime);
  }
}
