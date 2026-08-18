using System.Text;
using WeifenLuo.WinFormsUI.Docking;

namespace SkyRoof
{
  public class UiSettings
  {
    public Size MainWindowSize { get; set; }
    public Point MainWindowLocation { get; set; }
    public string? DockingLayoutString { get; set; }
    public bool ClockUtcMode { get; set; }

    // applied on startup, selected in the Tools | Theme menu. Not shown in the settings dialog,
    // so it carries no DisplayName/Description attributes
    public ThemeMode Theme { get; set; } = ThemeMode.Light;
    public SatelliteGroupsFormSettings SatelliteGroupsForm { get; set; } = new();
    public SatelliteDetailsFormSettings SatelliteDetailsForm { get; set; } = new();
    public SatelliteDetailsPanelSettings SatelliteDetailsPanel { get; set; } = new();
    public SatellitePassesPanelSettings SatellitePassesPanel { get; set; } = new();
    public string DefaultDockingString => Encoding.UTF8.GetString(Properties.Resources.default_docking);

    // column widths of Details-view ListViews, keyed by a caller-supplied name so the same
    // control's widths are saved when its panel/form closes and restored when it is recreated
    public Dictionary<string, int[]> ListViewColumnWidths { get; set; } = new();

    public void SaveColumnWidths(string key, ListView listView)
    {
      ListViewColumnWidths[key] = listView.Columns.Cast<ColumnHeader>().Select(c => c.Width).ToArray();
    }

    public void RestoreColumnWidths(string key, ListView listView)
    {
      if (!ListViewColumnWidths.TryGetValue(key, out var widths)) return;
      int count = Math.Min(widths.Length, listView.Columns.Count);
      for (int i = 0; i < count; i++) listView.Columns[i].Width = widths[i];
    }

    // width of the label column of PropertyGrids, keyed by a caller-supplied name
    public Dictionary<string, int> PropertyGridLabelWidths { get; set; } = new();

    public void SavePropertyGridLabelWidth(string key, PropertyGrid grid)
    {
      int width = VE3NEA.PropertyGridEx.GetLabelWidth(grid);
      if (width > 0) PropertyGridLabelWidths[key] = width;
    }

    public void RestorePropertyGridLabelWidth(string key, PropertyGrid grid)
    {
      if (PropertyGridLabelWidths.TryGetValue(key, out int width))
        VE3NEA.PropertyGridEx.SetLabelWidth(grid, width);
    }

    public void StoreWindowPosition(Form form)
    {
      if (form.WindowState == FormWindowState.Normal)
      {
        MainWindowLocation = form.Location;
        MainWindowSize = form.Size;
      }
      else
      {
        MainWindowLocation = form.RestoreBounds.Location;
        MainWindowSize = form.RestoreBounds.Size;
      }
    }
    public void RestoreWindowPosition(Form form)
    {
      if (MainWindowSize.Width > 0)
      {
        form.Location = MainWindowLocation;
        form.Size = MainWindowSize;
      }
    }

    internal void StoreDockingLayout(DockPanel dockHost)
    {
      using (var stream = new MemoryStream())
      {
        dockHost.SaveAsXml(stream, Encoding.UTF8);
        DockingLayoutString = Encoding.UTF8.GetString(stream.ToArray());
      }
    }

    internal bool RestoreDockingLayout(MainForm form)
    {
      if (string.IsNullOrEmpty(DockingLayoutString)) return false;

      DeserializeDockContent desCont = new DeserializeDockContent(form.MakeDockContentFromPersistString);

      using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(DockingLayoutString)))
        form.DockHost.LoadFromXml(stream, desCont);

      return true;
    }

    internal void ResetDockingLayout(MainForm form)
    {
      form.ctx.ClosePanels();
      DockingLayoutString = DefaultDockingString;
      RestoreDockingLayout(form);
    }
  }
  

  public class SatellitePassesPanelSettings
  {
    public int RadioButtoIndex { get; set; }
  }

  public class SatelliteDetailsPanelSettings
  {
    public int SplitterDistance { get; set; } = -1;
  }

  public class SatelliteDetailsFormSettings
  {
    public Size Size { get; set; }
    public int SplitterDistance { get; set; } = -1;
  }

  public class SatelliteGroupsFormSettings
  {
    public Size Size { get; set; }
  }
}
