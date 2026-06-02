using System.Data;
using Serilog;
using VE3NEA;
using WeifenLuo.WinFormsUI.Docking;

namespace SkyRoof
{
  public partial class GroupViewPanel : DockContent
  {
    public class ItemData
    {
      public SatnogsDbSatellite Sat;
      public SatellitePass? Pass;
      public ItemData(SatnogsDbSatellite sat) { Sat = sat; }
    }

    protected readonly Context ctx;
    private int SortColumn = 2;
    public ListViewItem[] Items;



    public GroupViewPanel()
    {
      InitializeComponent();
    }

    public GroupViewPanel(Context ctx)
    {
      Log.Information("Creating GroupViewPanel");
      this.ctx = ctx;
      InitializeComponent();

      this.ctx.GroupViewPanel = this;
      this.ctx.MainForm.GroupViewMNU.Checked = true;
      LoadGroup();
    }

    private void GroupViewPanel_FormClosing(object sender, FormClosingEventArgs e)
    {
      Log.Information("Closing GroupViewPanel");
      ctx.GroupViewPanel = null;
      ctx.MainForm.GroupViewMNU.Checked = false;
    }

    public void LoadGroup()
    {
      // select group or default
      var sett = ctx.Settings.Satellites;
      var group = sett.SatelliteGroups.First(g => g.Id == sett.SelectedGroupId);
      Items = group.SatelliteIds
        .Select(id => ctx.SatnogsDb.GetSatellite(id))
        .Where(s => s != null)
        .Cast<SatnogsDbSatellite>()
        .Select(ItemFromSat)
        .ToArray();
      ShowAmsatStatuses();

      SyncVirtualListSize();

      ShowSelectedSat();
      GroupNameLabel.Text = $"Group:   {group.Name}";
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);
      SyncVirtualListSize();
    }

    private void SyncVirtualListSize()
    {
      Items ??= Array.Empty<ListViewItem>();
      listView1.VirtualListSize = Items.Length;
      if (listView1.SelectedIndices.Count > 0 && listView1.SelectedIndices[0] >= Items.Length)
        listView1.SelectedIndices.Clear();
    }

    private bool TryGetSelectedItem(out ItemData data)
    {
      data = null!;
      if (Items == null || Items.Length == 0) return false;
      if (listView1.SelectedIndices.Count == 0) return false;

      int idx = listView1.SelectedIndices[0];
      if (idx < 0 || idx >= Items.Length) return false;
      if (Items[idx].Tag is not ItemData itemData) return false;

      data = itemData;
      return true;
    }

    private ListViewItem ItemFromSat(SatnogsDbSatellite sat)
    {
      var item = new ListViewItem([
        sat.name,
        sat.norad_cat_id?.ToString() ?? "",
        "",
        "",
        ]);

      item.Tag = new ItemData(sat);
      item.ToolTipText = sat.GetTooltipText();

      if (sat.Transmitters.Any(t => t.IsUhf() && t.HasUplink())) item.BackColor = Color.LightCyan;
      else if (sat.Transmitters.Any(t => t.IsVhf() && t.HasUplink())) item.BackColor = Color.LightGoldenrodYellow;

      if (sat.Flags.HasFlag(SatelliteFlags.Ham)) item.Font = new(item.Font, FontStyle.Bold);
      if (!sat.status.StartsWith("alive")) item.ForeColor = Color.Silver;
      else if (sat.Tle == null) item.Font = new(item.Font, FontStyle.Strikeout);

      return item;
    }

    public void ShowSelectedSat()
    {
      if (Items == null) return;

      var selectedSat = ctx.SatelliteSelector.SelectedSatellite;

      foreach (var item in Items)
        item.StateImageIndex = ((ItemData)item.Tag!).Sat == selectedSat ? 0 : -1;

      listView1.SelectedIndices.Clear();
      listView1.Invalidate();
    }

    public void ShowAmsatStatuses()
    {
      if (!ctx.Settings.Amsat.Enabled)
      {
        listView1.SmallImageList = null;
        return;
      }

      listView1.SmallImageList = imageList1;
      var statuses = ctx.AmsatStatusLoader.Statuses;

      foreach (var item in Items)
      {
        int? norad_id = ((ItemData)item.Tag!).Sat.norad_cat_id;

        if (norad_id != null && statuses.TryGetValue(norad_id.Value, out bool status))
          item.ImageIndex = status ? 1 : 2;
        else
          item.ImageIndex = -1;
      }
    }

    public void UpdatePassTimes()
    {
      if (Items == null || Items.Length == 0) return;

      var now = DateTime.UtcNow;
      bool changed = false;

      foreach (var item in Items)
      {
        if (item?.Tag is not ItemData data) continue;
        if (item.SubItems.Count < 4) continue;
        if (data.Pass == null || data.Pass.EndTime < now)
        {
          data.Pass = ctx.HamPasses.GetNextPass(data.Sat);
          changed = true;
        }

        if (data.Pass == null)
        {
          item.SubItems[2].Text = "n/a";
          item.SubItems[3].Text = "";
        }
        else if (data.Pass.StartTime < now)
        {
          item.SubItems[2].Text = "Now";
          item.SubItems[3].Text = $"{Math.Round(data.Pass.MaxElevation)}°";
        }
        else
        {
          item.SubItems[2].Text = $"{Utils.TimespanToString(data.Pass.StartTime - now)}";
          item.SubItems[3].Text = $"{Math.Round(data.Pass.MaxElevation)}°";
        }

        item.ToolTipText = $"{data.Sat.GetTooltipText()}\n\n{string.Join("\n", data.Pass?.GetTooltipText(false) ?? [])}";
      }

      if (changed) SortItems();
      SyncVirtualListSize();
      listView1.Invalidate();
    }

    private void SortItems()
    {
      switch (SortColumn)
      {
        case 0: Items = Items.OrderBy(item => item.Text).ToArray(); break;
        case 1: Items = Items.OrderBy(item => ((ItemData)item.Tag!).Sat.norad_cat_id).ToArray(); break;
        case 2: Items = Items.OrderBy(item => ((ItemData)item.Tag!).Pass?.StartTime ?? DateTime.MaxValue).ToArray(); break;
        case 3: Items = Items.OrderBy(item => ((ItemData)item.Tag!).Pass?.MaxElevation).ToArray(); break;
      }

      listView1.Invalidate();
    }




    //----------------------------------------------------------------------------------------------
    //                                        events
    //----------------------------------------------------------------------------------------------
    private void listView1_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
    {
      if (Items == null || Items.Length == 0)
      {
        e.Item = new ListViewItem("");
        return;
      }

      int idx = Math.Clamp(e.ItemIndex, 0, Items.Length - 1);
      e.Item = Items[idx];
    }

    private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
    {
      SortColumn = e.Column;
      SortItems();
    }

    private void listView1_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (MouseButtons == MouseButtons.Right) return;
      if (!TryGetSelectedItem(out var data)) return;

      ctx.SatelliteSelector.SetSelectedSatellite(data.Sat);
      ctx.PassesPanel?.ShowPasses();
      if (data.Pass != null) ctx.SatelliteSelector.SetSelectedPass(data.Pass);
    }

    private void SatelliteDetailsMNU_Click(object sender, EventArgs e)
    {
      if (!TryGetSelectedItem(out var data)) return;
      SatelliteDetailsForm.ShowSatellite(data.Sat, ctx.MainForm);
    }

    private void MonitorSatelliteMNU_Click(object sender, EventArgs e)
    {
      if (listView1.SelectedIndices.Count == 0) return;
      int idx = listView1.SelectedIndices[0];
      if (Items == null || idx < 0 || idx >= Items.Length) return;

      var data = (ItemData)Items[idx].Tag!;
      ToggleMonitored(data.Sat);
    }

    private void ToggleMonitored(SatnogsDbSatellite sat)
    {
      var ids = ctx.Settings.Satellites.MonitoredSatelliteIds;
      if (ids.Contains(sat.sat_id))
        ids.RemoveAll(id => id == sat.sat_id);
      else
        ids.Add(sat.sat_id); // add as lowest priority

      ctx.Settings.SaveToFile();
      ctx.MonitoredPasses?.FullRebuild();
      ctx.MonitoredSatellitesPanel?.RefreshList();
    }

    private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
    {
      if (!TryGetSelectedItem(out var data))
      {
        e.Cancel = true;
        return;
      }

      bool monitored = ctx.Settings.Satellites.MonitoredSatelliteIds.Contains(data.Sat.sat_id);
      MonitorSatelliteMNU.Text = monitored ? "Unmonitor Satellite" : "Monitor Satellite";
    }

    private void listView1_MouseDown(object sender, MouseEventArgs e)
    {
      if (e.Button != MouseButtons.Right) return;
      var item = listView1.GetItemAt(e.X, e.Y);
      if (item == null) return;

      // ensure the context menu applies to the row that was right-clicked
      listView1.SelectedIndices.Clear();
      item.Selected = true;
    }
  }
}