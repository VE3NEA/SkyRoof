using System.Data;
using Serilog;
using VE3NEA;
using WeifenLuo.WinFormsUI.Docking;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

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
    // shared, so we don't leak a GDI font handle per item on every rebuild
    private Font BoldFont, StrikeoutFont;
    // sat the context menu was opened on, captured at Opening time (see SatelliteDetailsMNU_Click)
    private SatnogsDbSatellite? ContextMenuSat;



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
      ctx.Settings.Ui.RestoreColumnWidths("GroupViewPanel", listView1);
      LoadGroup();
    }

    private void GroupViewPanel_FormClosing(object sender, FormClosingEventArgs e)
    {
      Log.Information("Closing GroupViewPanel");
      ctx.GroupViewPanel = null;
      ctx.MainForm.GroupViewMNU.Checked = false;
      ctx.Settings.Ui.SaveColumnWidths("GroupViewPanel", listView1);
    }

    public void LoadGroup()
    {
      // select group or default
      var sett = ctx.Settings.Satellites;
      var group = sett.SatelliteGroups.First(g => g.Id == sett.SelectedGroupId);
      Items = group.SatelliteIds.Select(id => ItemFromSat(ctx.SatnogsDb.GetSatellite(id))).ToArray();
      ShowAmsatStatuses();

      PopulateListView();

      ShowSelectedSat();
      GroupNameLabel.Text = $"Group:   {group.Name}";
    }

    // fill the (non-virtual) list from the Items array, preserving its order
    private void PopulateListView()
    {
      listView1.BeginUpdate();
      listView1.Items.Clear();
      listView1.Items.AddRange(Items);
      listView1.EndUpdate();
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

      if (sat.Transmitters.Any(t => t.IsUhf() && t.HasUplink())) item.BackColor = Theme.UhfTint;
      else if (sat.Transmitters.Any(t => t.IsVhf() && t.HasUplink())) item.BackColor = Theme.VhfTint;

      BoldFont ??= new Font(listView1.Font, FontStyle.Bold);
      StrikeoutFont ??= new Font(listView1.Font, FontStyle.Strikeout);

      if (sat.Flags.HasFlag(SatelliteFlags.Ham)) item.Font = BoldFont;
      item.ForeColor = Theme.RowText(!sat.IsAlive());
      if (sat.IsAlive() && sat.Tle == null) item.Font = StrikeoutFont;

      return item;
    }

    public void ShowSelectedSat()
    {
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
      var now = DateTime.UtcNow;
      bool changed = false;

      foreach (var item in Items)
      {
        var data = item.Tag as ItemData;
        if (data.Pass == null || data.Pass.EndTime < now)
        {
          // re-sort only when the next-pass orbit number actually changes,
          // so satellites with no upcoming pass (Pass stays null) don't force a sort every second
          int? oldOrbit = data.Pass?.OrbitNumber;
          data.Pass = ctx.HamPasses.GetNextPass(data.Sat);
          if (data.Pass?.OrbitNumber != oldOrbit) changed = true;
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

      // only auto-sort while the list is ordered by the Next Pass column
      if (changed && SortColumn == 2) SortItems();
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

      PopulateListView();
    }




    //----------------------------------------------------------------------------------------------
    //                                        events
    //----------------------------------------------------------------------------------------------
    private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
    {
      SortColumn = e.Column;
      SortItems();
    }

    private void listView1_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (MouseButtons == MouseButtons.Right) return;

      if (listView1.SelectedIndices.Count == 0) return;

      var data = (ItemData)Items[listView1.SelectedIndices[0]].Tag!;

      ctx.SatelliteSelector.SetSelectedSatellite(data.Sat);
      ctx.PassesPanel?.ShowPasses();
      if (data.Pass != null) ctx.SatelliteSelector.SetSelectedPass(data.Pass);
    }

    private void SatelliteDetailsMNU_Click(object sender, EventArgs e)
    {
      // use the sat captured when the menu opened: a timer-driven list rebuild
      // (UpdatePassTimes -> SortItems) can clear SelectedIndices while the menu is open
      if (ContextMenuSat == null) return;
      SatelliteDetailsForm.ShowSatellite(ContextMenuSat, ctx.MainForm, ctx);
    }

    private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
    {
      if (listView1.SelectedIndices.Count == 0) { e.Cancel = true; ContextMenuSat = null; return; }

      ContextMenuSat = ((ItemData)Items[listView1.SelectedIndices[0]].Tag!).Sat;
    }

    private void GroupViewPanel_MouseClick(object sender, MouseEventArgs e)
    {
      
    }
  }
}