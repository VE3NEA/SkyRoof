using System.Data;
using WeifenLuo.WinFormsUI.Docking;
using VE3NEA;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static SkyRoof.GroupViewPanel;
using Serilog;

namespace SkyRoof
{
  public partial class PassesPanel : DockContent
  {
    private readonly Context ctx;
    private List<ListViewItem> Items = new();

    private readonly Font BoldFont;
    private readonly Pen PathPen = new Pen(Brushes.Teal, 2);


    public PassesPanel()
    {
      InitializeComponent();
    }

    public PassesPanel(Context ctx)
    {
      Log.Information("Creating PassesPanel");
      this.ctx = ctx;
      InitializeComponent();

      ctx.PassesPanel = this;
      ctx.MainForm.SatellitePassesMNU.Checked = true;

      BoldFont = new Font(listViewEx1.Font, FontStyle.Bold);

      int rowHeight = TextRenderer.MeasureText("0", Font, Size, TextFormatFlags.NoPadding).Height * 2 + 15;
      listViewEx1.SetRowHeight(rowHeight);
      
      
      listViewEx1.SetTooltipDelay(1500);
      SetRadioButtonIndex(ctx.Settings.Ui.SatellitePassesPanel.RadioButtoIndex);
      if (listViewEx1.VirtualListSize == 0) ShowPasses();
    }




    //----------------------------------------------------------------------------------------------
    //                                        events
    //----------------------------------------------------------------------------------------------
    private void PassesPanel_FormClosing(object sender, FormClosingEventArgs e)
    {
      Log.Information("Closing PassesPanel");
      ctx.PassesPanel = null;
      ctx.MainForm.SatellitePassesMNU.Checked = false;
    }

    private void listViewEx1_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
    {
      e.Item = Items[e.ItemIndex];
    }

    private void listViewEx1_Resize(object sender, EventArgs e)
    {
      listViewEx1.Columns[0].Width = listViewEx1.ClientSize.Width;
    }

    private void radioButton_CheckedChanged(object sender, EventArgs e)
    {
      var radioBtn = (RadioButton)sender;
      if (radioBtn.Checked)
      {
        ctx.Settings.Ui.SatellitePassesPanel.RadioButtoIndex = GetRadioButtonIndex();
        if (listViewEx1.VirtualListSize > 0) listViewEx1.EnsureVisible(0);
        ShowPasses();
      }
    }

    private void listViewEx1_MouseDown(object sender, MouseEventArgs e)
    {
      var item = listViewEx1.GetItemAt(e.X, e.Y);
     
      if (item != null && e.Button == MouseButtons.Left)
      {
        var pass = (SatellitePass)item.Tag!;
        var sat = pass.Satellite;
        ctx.SatelliteSelector.SetSelectedSatellite(sat);
        ctx.SatelliteSelector.SetSelectedPass(pass);
      }
      else if (item != null && e.Button == MouseButtons.Right)
      {
        // ensure context menu applies to the row that was right-clicked
        listViewEx1.SelectedIndices.Clear();
        item.Selected = true;
      }
    }

    private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
    {
      if (listViewEx1.SelectedIndices.Count == 0) { e.Cancel = true; return; }

      var pass = (SatellitePass)Items[listViewEx1.SelectedIndices[0]].Tag!;
      bool monitored = ctx.Settings.Satellites.MonitoredSatelliteIds.Contains(pass.Satellite.sat_id);
      MonitorSatelliteMNU.Text = monitored ? "Unmonitor Satellite" : "Monitor Satellite";
    }

    private void MonitorSatelliteMNU_Click(object sender, EventArgs e)
    {
      if (listViewEx1.SelectedIndices.Count == 0) return;
      int idx = listViewEx1.SelectedIndices[0];
      if (idx < 0 || idx >= Items.Count) return;

      var pass = (SatellitePass)Items[idx].Tag!;
      ToggleMonitored(pass.Satellite);
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



    //----------------------------------------------------------------------------------------------
    //                                      items  
    //----------------------------------------------------------------------------------------------
    internal void ShowPasses()
    {
      IEnumerable<SatellitePass> passes = ctx.GroupPasses.GetPassesSnapshot();
      var startTime = DateTime.UtcNow;
      var endTime = startTime + TimeSpan.FromDays(2);

      if (CurrentSatBtn.Checked)
      {
        var sat = ctx.SatelliteSelector.SelectedSatellite;

        // sat in group, show 3-day prediction
        if (!ctx.SatelliteSelector.GroupSatellites.Contains(sat))
          passes = ctx.HamPasses.GetPassesSnapshot();
        // else show 2-hour prediction
        passes = passes.Where(p => p.Satellite == sat);
      }

      else if (GroupBtn.Checked)
      {
      }

      else
      {
        passes = ctx.HamPasses.GetPassesSnapshot();
        endTime = startTime + TimeSpan.FromHours(2);
      }

      passes = passes.Where(pass => pass.EndTime >= startTime && pass.StartTime < endTime);
      Items = CreatePassItems(passes).ToList();
      listViewEx1.VirtualListSize = Items.Count;
      listViewEx1.Invalidate();
    }

    internal void UpdatePassTimes()
    {
      // delete finished passes
      int oldCount = Items.Count;
      for (int i = Items.Count - 1; i >= 0; i--)
        if (((SatellitePass)Items[i].Tag).EndTime < DateTime.UtcNow) Items.RemoveAt(i);

      listViewEx1.VirtualListSize = Items.Count;
      listViewEx1.Invalidate();
    }

    public IEnumerable<ListViewItem> CreatePassItems(IEnumerable<SatellitePass> passes)
    {
      // wrap passes in listview items
      return passes.OrderBy(p => p.StartTime).Select(ItemForPass);
    }

    private ListViewItem ItemForPass(SatellitePass pass)
    {
      var item = new ListViewItem();
      item.Tag = pass;

      pass.MakeMiniPath();

      return item;
    }

    private int GetRadioButtonIndex()
    {
      if (CurrentSatBtn.Checked) return 0;
      if (GroupBtn.Checked) return 1;
      return 2;
    }

    private void SetRadioButtonIndex(int index)
    {
      CurrentSatBtn.Checked = index == 0;
      GroupBtn.Checked = index == 1;
      AllBtn.Checked = index == 2;
    }




    //----------------------------------------------------------------------------------------------
    //                                        draw
    //----------------------------------------------------------------------------------------------
    private void listViewEx1_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
    {
      e.DrawBackground();

      var pass = (SatellitePass)e.Item.Tag;
      int h = e.Bounds.Height - 1;
      int w = e.Bounds.Width - h;
      bool now = pass.StartTime < DateTime.UtcNow;

      // sat name
      string text = pass.Satellite.name;
      var size = e.Graphics.MeasureString(text, BoldFont);
      var rect = new RectangleF(e.Bounds.X, e.Bounds.Y, size.Width, size.Height);
      var brush = Brushes.White;
      if (pass.Satellite.Flags.HasFlag(SatelliteFlags.Uhf)) brush = Brushes.LightCyan;
      else if (pass.Satellite.Flags.HasFlag(SatelliteFlags.Vhf)) brush = Brushes.LightGoldenrodYellow;
      e.Graphics.FillRectangle(brush, rect);
      e.Graphics.DrawString(text, BoldFont, Brushes.Black, rect);

      // orbit number
      text = $"#{pass.OrbitNumber}";
      size = e.Graphics.MeasureString(text, listViewEx1.Font);
      rect = new RectangleF(rect.Right + 10, rect.Y, size.Width, size.Height);
      e.Graphics.DrawString(text, listViewEx1.Font, Brushes.Black, rect);

      // start/end time
      text = pass.Geostationary ?
        "Geostationary" : $"{pass.StartTime.ToLocalTime():yyyy-MM-dd  HH:mm:ss}  to  {pass.EndTime.ToLocalTime():HH:mm:ss}";
      size = e.Graphics.MeasureString(text, listViewEx1.Font);
      rect = new RectangleF(e.Bounds.X, e.Bounds.Y + e.Bounds.Height - size.Height - 2, size.Width, size.Height);
      e.Graphics.DrawString(text, listViewEx1.Font, Brushes.Black, rect);

      // duration / elevation
      text = pass.Geostationary ?
        $"{pass.MaxElevation:F0}°" : $"{Utils.TimespanToString(pass.EndTime - pass.StartTime, false)}   {pass.MaxElevation:F0}°";
      size = e.Graphics.MeasureString(text, listViewEx1.Font);
      rect = new RectangleF(e.Bounds.X + w - size.Width, e.Bounds.Y + e.Bounds.Height - size.Height - 2, size.Width, size.Height);
      e.Graphics.DrawString(text, listViewEx1.Font, Brushes.Black, rect);

      // wait time
      text = now ? "Now" : $"in {Utils.TimespanToString(pass.StartTime - DateTime.UtcNow)}";
      brush = now ? Brushes.Green : Brushes.Black;
      size = e.Graphics.MeasureString(text, BoldFont);
      rect = new RectangleF(e.Bounds.X + w - size.Width, e.Bounds.Y, size.Width, size.Height);
      e.Graphics.DrawString(text, BoldFont, brush, rect);


      // mini sky view

      // background
      rect = new RectangleF(e.Bounds.X + w, e.Bounds.Y, h, h);
      //e.Graphics.FillRectangle(Brushes.Aquamarine, rect);      
      rect.Inflate(-5, -5);
      //e.Graphics.FillEllipse(Brushes.White, rect);

      var radius = rect.Width / 2;
      var center = new PointF(rect.Left + radius, rect.Top + radius);

      // grid
      e.Graphics.DrawEllipse(Pens.Silver, rect);
      e.Graphics.DrawLine(Pens.Silver, rect.Left, center.Y, rect.Right, center.Y);
      e.Graphics.DrawLine(Pens.Silver, center.X, rect.Top, center.X, rect.Bottom);
      rect.Inflate(-radius / 2, -radius / 2);
      e.Graphics.DrawEllipse(Pens.Silver, rect);

      var path = ((SatellitePass)e.Item.Tag).MiniPath;
      if (path.Length > 1)
      {
        // path
        var points = path.Select(p =>
          new PointF(center.X + p.X * radius, center.Y - p.Y * radius)).ToArray();
        e.Graphics.DrawLines(PathPen, points);

        // end points
        e.Graphics.FillEllipse(Brushes.Green, points.First().X - 3, points.First().Y - 3, 6, 6);
        e.Graphics.FillEllipse(Brushes.Red, points.Last().X - 3, points.Last().Y - 3, 6, 6);
      }

      // item separator
      rect = new RectangleF(e.Bounds.X, e.Bounds.Y + h, e.Bounds.Width, 1);
      e.Graphics.FillRectangle(Brushes.Gray, rect);
    }

    private void listViewEx1_ItemMouseHover(object sender, ListViewItemMouseHoverEventArgs e)
    {
      if (e.Item == null) return;
      var pass = (SatellitePass)e.Item.Tag!;
      e.Item.ToolTipText = $"{pass.Satellite.GetTooltipText()}\n\n{string.Join("\n", pass.GetTooltipText(false))}";
    }
  }
}
