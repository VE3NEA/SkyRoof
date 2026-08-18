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

    // Text on a list row. The band tints are light colors in both themes, so a tinted row keeps
    // the light theme's text colors - the system ones would be white on pale cyan. An untinted
    // row is drawn straight on the list surface and follows it.
    public static Color RowText(bool tinted, bool inactive)
    {
      if (inactive) return tinted ? Color.Gray : SystemColors.GrayText;
      return tinted ? Color.Black : SystemColors.WindowText;
    }

    public static Brush RowTextBrush(bool tinted, bool inactive)
    {
      if (inactive) return tinted ? Brushes.Gray : SystemBrushes.GrayText;
      return tinted ? Brushes.Black : SystemBrushes.WindowText;
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

    // frequency scale: the accent marks the pass that is happening now
    public static Color ScaleAccent => Pick(Color.Blue, Color.Aqua);

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
