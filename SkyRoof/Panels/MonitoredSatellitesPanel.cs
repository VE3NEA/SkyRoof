using Serilog;
using WeifenLuo.WinFormsUI.Docking;
using VE3NEA;

namespace SkyRoof
{
  public class MonitoredSatellitesPanel : DockContent
  {
    private readonly Context ctx;
    private readonly ListView listView = new();
    private readonly Button upBtn = new();
    private readonly Button downBtn = new();
    private readonly Button removeBtn = new();
    private readonly Label minElLabel = new();
    private readonly TrackBar minElTrackBar = new();
    private readonly Label minElValueLabel = new();
    private readonly ToolTip minElToolTip = new();
    private readonly ToolTip listColumnToolTip = new();
    private readonly System.Windows.Forms.Timer nextPassTimer = new();
    private readonly System.Windows.Forms.Timer minElSaveTimer = new() { Interval = 400 };
    private int? DragSourceIndex;
    private bool refreshingList;

    private const string MinElevationToolTip =
      "The minimum elevation required for a higher priority satellite to interrupt a lower priority one during a pass.";
    private const string MaxPassToolTip = "Max elevation of the next pass";
    private const string NextPassToolTip = "Time until the next pass";
    private const string AudioRecordToolTip = "Enable audio recording for this satellite";
    private const string IqRecordToolTip = "Enable I/Q recording for this satellite.";
    private const int SatelliteColumnIndex = 1;
    private const int SatelliteColumnMaxWidth = 240;
    private const int NextColumnIndex = 3;
    private const int MaxColumnIndex = 4;
    private const int AudioColumnIndex = 5;
    private const int IqColumnIndex = 6;

    public MonitoredSatellitesPanel(Context ctx)
    {
      Log.Information("Creating MonitoredSatellitesPanel");
      this.ctx = ctx;
      Text = "Monitored Satellites";

      ctx.MonitoredSatellitesPanel = this;
      ctx.MainForm.MonitoredSatellitesMNU.Checked = true;

      BuildUi();
      RefreshList();

      nextPassTimer.Interval = 1000;
      nextPassTimer.Tick += (s, e) => UpdateNextPassCountdown();
      nextPassTimer.Start();

      minElSaveTimer.Tick += (s, e) =>
      {
        minElSaveTimer.Stop();
        ctx.Settings.SaveToFile();
      };
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
      Log.Information("Closing MonitoredSatellitesPanel");
      nextPassTimer.Stop();
      nextPassTimer.Dispose();
      minElSaveTimer.Stop();
      minElSaveTimer.Dispose();
      minElToolTip.Dispose();
      listColumnToolTip.Dispose();
      ctx.MonitoredSatellitesPanel = null;
      ctx.MainForm.MonitoredSatellitesMNU.Checked = false;
      base.OnFormClosing(e);
    }

    private void BuildUi()
    {
      var top = new FlowLayoutPanel
      {
        Dock = DockStyle.Top,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        WrapContents = false,
        FlowDirection = FlowDirection.LeftToRight,
        Padding = new Padding(8, 6, 8, 6),
      };

      ConfigureIconButton(upBtn, RotateBitmap(Properties.Resources.arrow_down_16, RotateFlipType.Rotate180FlipNone), "Move up");
      upBtn.Click += (s, e) => MoveSelected(-1);

      ConfigureIconButton(downBtn, Properties.Resources.arrow_down_16, "Move down");
      downBtn.Click += (s, e) => MoveSelected(+1);

      ConfigureGlyphButton(removeBtn, "\uE74D", "Remove");
      removeBtn.Click += (s, e) => RemoveSelected();

      top.Controls.Add(upBtn);
      top.Controls.Add(downBtn);
      top.Controls.Add(removeBtn);

      minElLabel.Text = "Min Elevation:";
      minElLabel.AutoSize = true;
      minElLabel.Margin = new Padding(8, 6, 4, 0);
      top.Controls.Add(minElLabel);

      minElTrackBar.Minimum = 0;
      minElTrackBar.Maximum = 90;
      minElTrackBar.TickFrequency = 10;
      minElTrackBar.SmallChange = 1;
      minElTrackBar.LargeChange = 5;
      minElTrackBar.AutoSize = false;
      minElTrackBar.Height = 24;
      minElTrackBar.Width = 120;
      minElTrackBar.Margin = new Padding(0, 2, 4, 0);
      minElTrackBar.Value = Math.Max(0, Math.Min(90, ctx.Settings.Satellites.AutoMonitorMinElevationDeg));
      minElValueLabel.Text = $"{minElTrackBar.Value}°";
      minElTrackBar.ValueChanged += (s, e) =>
      {
        ctx.Settings.Satellites.AutoMonitorMinElevationDeg = minElTrackBar.Value;
        minElValueLabel.Text = $"{minElTrackBar.Value}°";
        minElSaveTimer.Stop();
        minElSaveTimer.Start();
      };
      minElTrackBar.MouseUp += (s, e) =>
      {
        minElSaveTimer.Stop();
        ctx.Settings.SaveToFile();
      };
      top.Controls.Add(minElTrackBar);

      minElValueLabel.AutoSize = true;
      minElValueLabel.Margin = new Padding(0, 6, 0, 0);
      top.Controls.Add(minElValueLabel);

      minElToolTip.SetToolTip(minElLabel, MinElevationToolTip);
      minElToolTip.SetToolTip(minElTrackBar, MinElevationToolTip);
      minElToolTip.SetToolTip(minElValueLabel, MinElevationToolTip);

      listView.Dock = DockStyle.Fill;
      listView.View = View.Details;
      listView.FullRowSelect = true;
      listView.MultiSelect = false;
      listView.AllowDrop = true;
      listView.Columns.Add("#", 40);
      listView.Columns.Add("Satellite", MeasureColumnHeaderWidth(listView, "Satellite"));
      listView.Columns.Add("Transmitter", 130);
      listView.Columns.Add("Next Pass", MeasureNextPassColumnWidth(listView));
      listView.Columns[NextColumnIndex].TextAlign = HorizontalAlignment.Right;
      listView.Columns.Add("Max", 50);
      listView.Columns[MaxColumnIndex].TextAlign = HorizontalAlignment.Right;
      listView.Columns.Add("Audio", MeasureColumnHeaderWidth(listView, "Audio"));
      listView.Columns.Add("I/Q", 45);
      listView.DoubleClick += (s, e) => SelectInApp();
      listView.MouseMove += ListView_MouseMove;
      listView.MouseLeave += (s, e) => listColumnToolTip.SetToolTip(listView, "");
      listView.MouseDown += ListView_MouseDown;
      listView.ItemDrag += ListView_ItemDrag;
      listView.DragEnter += ListView_DragEnter;
      listView.DragOver += ListView_DragOver;
      listView.DragDrop += ListView_DragDrop;
      listView.DragLeave += (s, e) => DragSourceIndex = null;

      Controls.Add(listView);
      Controls.Add(top);
    }

    public void RefreshList()
    {
      if (IsDisposed) return;

      nextPassTimer.Stop();
      refreshingList = true;
      listView.BeginUpdate();
      try
      {
        listView.Items.Clear();

        if (ctx.SatnogsDb == null)
          return;

        var ids = ctx.Settings.Satellites.MonitoredSatelliteIds
          .Where(id => !string.IsNullOrWhiteSpace(id))
          .Distinct()
          .ToArray();

        for (int i = 0; i < ids.Length; i++)
        {
          var sat = ctx.SatnogsDb?.GetSatellite(ids[i]);
          if (sat == null) continue;

          var cust = ctx.Settings.Satellites.SatelliteCustomizations.GetOrCreate(sat.sat_id);
          string txName = "";
          if (!string.IsNullOrEmpty(cust.SelectedTransmitterId))
          {
            var tx = sat.Transmitters.FirstOrDefault(t => t.uuid == cust.SelectedTransmitterId);
            txName = tx?.description ?? "";
          }
          if (string.IsNullOrEmpty(txName) && sat.Transmitters.Count > 0)
            txName = sat.Transmitters[0].description;

          string audio = cust.AutoRecordMode == AutoRecordMode.Audio ? "☑" : "☐";
          string iq = cust.AutoRecordMode == AutoRecordMode.Iq ? "☑" : "☐";

          var item = new ListViewItem([(i + 1).ToString(), sat.name, txName, "", "", audio, iq]);
          item.Tag = sat;
          listView.Items.Add(item);
        }
      }
      finally
      {
        listView.EndUpdate();
        refreshingList = false;
        if (!IsDisposed) nextPassTimer.Start();
      }

      ResizeSatelliteColumn();
      UpdateNextPassCountdown();
    }

    private void ResizeSatelliteColumn()
    {
      if (listView.Columns.Count <= SatelliteColumnIndex) return;
      listView.Columns[SatelliteColumnIndex].Width = MeasureSatelliteColumnWidth(listView);
    }

    private void UpdateNextPassCountdown()
    {
      if (IsDisposed || refreshingList) return;
      if (ctx.MonitoredPasses == null) return;

      var now = DateTime.UtcNow;
      var passes = ctx.MonitoredPasses.GetPassesSnapshot();

      listView.BeginUpdate();
      try
      {
        foreach (ListViewItem item in listView.Items)
        {
          if (item.Tag is not SatnogsDbSatellite sat) continue;
          if (item.SubItems.Count <= IqColumnIndex) continue;

          var active = passes.FirstOrDefault(p => p.Satellite == sat && p.StartTime <= now && p.EndTime > now);
          if (active != null)
          {
            item.SubItems[NextColumnIndex].Text = "Now";
            item.SubItems[MaxColumnIndex].Text = $"{Math.Round(active.MaxElevation):F0}°";
            continue;
          }

          var next = passes
            .Where(p => p.Satellite == sat && p.StartTime > now)
            .OrderBy(p => p.StartTime)
            .FirstOrDefault();

          item.SubItems[NextColumnIndex].Text = next == null ? "" : Utils.TimespanToString(next.StartTime - now);
          item.SubItems[MaxColumnIndex].Text = next == null ? "" : $"{Math.Round(next.MaxElevation):F0}°";
        }
      }
      finally
      {
        listView.EndUpdate();
      }
    }

    private void ListView_MouseMove(object? sender, MouseEventArgs e)
    {
      string tip = GetColumnToolTip(e.Location);
      if (listColumnToolTip.GetToolTip(listView) != tip)
        listColumnToolTip.SetToolTip(listView, tip);
    }

    private string GetColumnToolTip(Point location)
    {
      var hit = listView.HitTest(location);
      int col = -1;
      if (hit.Item != null && hit.SubItem != null)
        col = hit.Item.SubItems.IndexOf(hit.SubItem);
      else if (IsOverColumnHeader(location))
        col = GetColumnIndexAt(location.X);

      return col switch
      {
        MaxColumnIndex => MaxPassToolTip,
        NextColumnIndex => NextPassToolTip,
        AudioColumnIndex => AudioRecordToolTip,
        IqColumnIndex => IqRecordToolTip,
        _ => "",
      };
    }

    private int GetColumnIndexAt(int x)
    {
      int left = 0;
      for (int i = 0; i < listView.Columns.Count; i++)
      {
        int right = left + listView.Columns[i].Width;
        if (x >= left && x < right) return i;
        left = right;
      }
      return -1;
    }

    private void ListView_MouseDown(object sender, MouseEventArgs e)
    {
      if (e.Button != MouseButtons.Left) return;

      var hit = listView.HitTest(e.Location);
      if (hit.Item == null) return;
      if (hit.Item.Tag is not SatnogsDbSatellite sat) return;

      int col = hit.Item.SubItems.IndexOf(hit.SubItem);
      if (col != AudioColumnIndex && col != IqColumnIndex) return;

      var cust = ctx.Settings.Satellites.SatelliteCustomizations.GetOrCreate(sat.sat_id);
      if (col == AudioColumnIndex)
        cust.AutoRecordMode = cust.AutoRecordMode == AutoRecordMode.Audio ? AutoRecordMode.Off : AutoRecordMode.Audio;
      else
        cust.AutoRecordMode = cust.AutoRecordMode == AutoRecordMode.Iq ? AutoRecordMode.Off : AutoRecordMode.Iq;

      ctx.Settings.SaveToFile();
      RefreshList();
    }

    private void SelectInApp()
    {
      if (listView.SelectedItems.Count == 0) return;
      var sat = listView.SelectedItems[0].Tag as SatnogsDbSatellite;
      if (sat == null) return;
      ctx.SatelliteSelector.SetSelectedSatellite(sat);
    }

    private void MoveSelected(int delta)
    {
      if (listView.SelectedItems.Count == 0) return;
      var sat = listView.SelectedItems[0].Tag as SatnogsDbSatellite;
      if (sat == null) return;

      var ids = ctx.Settings.Satellites.MonitoredSatelliteIds;
      int idx = ids.IndexOf(sat.sat_id);
      if (idx < 0) return;
      int newIdx = idx + delta;
      if (newIdx < 0 || newIdx >= ids.Count) return;

      (ids[idx], ids[newIdx]) = (ids[newIdx], ids[idx]);
      ctx.Settings.SaveToFile();

      RefreshList();
      if (listView.Items.Count == 0) return;
      listView.Items[Math.Max(0, Math.Min(newIdx, listView.Items.Count - 1))].Selected = true;
    }

    private void RemoveSelected()
    {
      if (listView.SelectedItems.Count == 0) return;
      var sat = listView.SelectedItems[0].Tag as SatnogsDbSatellite;
      if (sat == null) return;

      ctx.Settings.Satellites.MonitoredSatelliteIds.RemoveAll(id => id == sat.sat_id);
      ctx.Settings.SaveToFile();
      ctx.MonitoredPasses?.FullRebuild();

      RefreshList();
    }

    private void ListView_ItemDrag(object sender, ItemDragEventArgs e)
    {
      if (listView.SelectedItems.Count == 0) return;
      DragSourceIndex = listView.SelectedItems[0].Index;
      DoDragDrop("MonitoredSatellitesPanelReorder", DragDropEffects.Move);
    }

    private void ListView_DragEnter(object sender, DragEventArgs e)
    {
      if (DragSourceIndex == null)
      {
        e.Effect = DragDropEffects.None;
        return;
      }

      e.Effect = DragDropEffects.Move;
    }

    private void ListView_DragOver(object sender, DragEventArgs e)
    {
      if (DragSourceIndex == null)
      {
        e.Effect = DragDropEffects.None;
        return;
      }

      e.Effect = DragDropEffects.Move;
      var pt = listView.PointToClient(new Point(e.X, e.Y));
      var hoverItem = listView.GetItemAt(pt.X, pt.Y);
      if (hoverItem != null)
        listView.InsertionMark.Index = hoverItem.Index;
      else
        listView.InsertionMark.Index = listView.Items.Count;
    }

    private void ListView_DragDrop(object sender, DragEventArgs e)
    {
      if (DragSourceIndex == null) return;

      int src = DragSourceIndex.Value;
      DragSourceIndex = null;
      listView.InsertionMark.Index = -1;

      if (src < 0 || src >= listView.Items.Count) return;

      var pt = listView.PointToClient(new Point(e.X, e.Y));
      var dstItem = listView.GetItemAt(pt.X, pt.Y);
      int dst = dstItem?.Index ?? listView.Items.Count - 1;
      dst = Math.Max(0, Math.Min(dst, listView.Items.Count - 1));
      if (dst == src) return;

      var srcSat = listView.Items[src].Tag as SatnogsDbSatellite;
      if (srcSat == null) return;

      var ids = ctx.Settings.Satellites.MonitoredSatelliteIds;
      int srcIdIndex = ids.IndexOf(srcSat.sat_id);
      if (srcIdIndex < 0) return;

      // map ListView dst index to sat id
      string? dstSatId = (listView.Items[dst].Tag as SatnogsDbSatellite)?.sat_id;
      if (dstSatId == null) return;
      int dstIdIndex = ids.IndexOf(dstSatId);
      if (dstIdIndex < 0) return;

      // move the id in the priority list
      var moved = ids[srcIdIndex];
      ids.RemoveAt(srcIdIndex);
      if (dstIdIndex > srcIdIndex) dstIdIndex -= 1;
      ids.Insert(dstIdIndex, moved);

      ctx.Settings.SaveToFile();
      RefreshList();

      if (listView.Items.Count == 0) return;
      int selectIdx = Math.Max(0, Math.Min(dst, listView.Items.Count - 1));
      listView.Items[selectIdx].Selected = true;
    }

    private static void ConfigureIconButton(Button btn, Image image, string toolTip)
    {
      btn.Text = "";
      btn.Image = image;
      btn.ImageAlign = ContentAlignment.MiddleCenter;
      btn.Size = new Size(28, 28);
      btn.Margin = new Padding(0, 0, 4, 0);
      btn.UseVisualStyleBackColor = true;
      btn.AccessibleName = toolTip;
    }

    private static void ConfigureGlyphButton(Button btn, string mdl2Glyph, string toolTip)
    {
      btn.Text = mdl2Glyph;
      btn.Font = new Font("Segoe MDL2 Assets", 10f);
      btn.TextAlign = ContentAlignment.MiddleCenter;
      btn.Size = new Size(28, 28);
      btn.Margin = new Padding(0, 0, 8, 0);
      btn.UseVisualStyleBackColor = true;
      btn.AccessibleName = toolTip;
    }

    private static Image RotateBitmap(Image source, RotateFlipType rotate)
    {
      var bmp = new Bitmap(source);
      bmp.RotateFlip(rotate);
      return bmp;
    }

    private static int MeasureColumnHeaderWidth(Control control, string headerText)
    {
      int textWidth = TextRenderer.MeasureText(headerText, control.Font).Width;
      return textWidth + 16;
    }

    private static int MeasureNextPassColumnWidth(Control control)
    {
      int headerWidth = MeasureColumnHeaderWidth(control, "Next Pass");
      int sampleWidth = TextRenderer.MeasureText("23h 59m 59s", control.Font).Width + 16;
      return Math.Max(headerWidth, sampleWidth);
    }

    private static int MeasureSatelliteColumnWidth(ListView listView)
    {
      int width = MeasureColumnHeaderWidth(listView, "Satellite");
      foreach (ListViewItem item in listView.Items)
      {
        if (item.SubItems.Count <= SatelliteColumnIndex) continue;
        int nameWidth = TextRenderer.MeasureText(item.SubItems[SatelliteColumnIndex].Text, listView.Font).Width + 16;
        width = Math.Max(width, nameWidth);
      }
      return Math.Min(SatelliteColumnMaxWidth, width);
    }

    private bool IsOverColumnHeader(Point location)
    {
      if (listView.Items.Count > 0)
        return location.Y < listView.GetItemRect(0).Top;
      return location.Y < TextRenderer.MeasureText("Ag", listView.Font).Height + 8;
    }
  }
}

