using Serilog;
using WeifenLuo.WinFormsUI.Docking;

namespace SkyRoof
{
  public partial class WaterfallPanel : DockContent
  {
    public Context ctx;
    private double SdrCenterFrequency => ctx?.FrequencyControl?.GetSdrRfCenter() ?? ctx?.Sdr?.Info?.Frequency ?? SdrConst.UHF_CENTER_FREQUENCY;
    private double MaxBandwidth => ctx?.Sdr?.Info?.MaxBandwidth ?? SdrConst.MAX_BANDWIDTH;
    private double SamplingRate => ctx?.Sdr?.Info?.SampleRate ?? SdrConst.MAX_BANDWIDTH;

    public WaterfallPanel()
    {
      InitializeComponent();
    }

    public WaterfallPanel(Context ctx)
    {
      Log.Information("Creating WaterfallPanel");
      this.ctx = ctx;

      InitializeComponent();

      ctx.WaterfallPanel = this;
      ctx.MainForm.WaterfallMNU.Checked = true;

      SplitContainer.SplitterDistance = ctx.Settings.Waterfall.SplitterDistance;
      ctx.MainForm.CreateSpectrumAnalyzer();
      ApplySettings();
      ctx.MainForm.ConfigureWaterfall();

      ScaleControl.ctx = ctx;
      ScaleControl.BuildLabels();
      ScaleControl.MouseMove += ScaleControl_MouseMove;
      ScaleControl.MouseDown += ScaleControl_MouseDown;
      ScaleControl.MouseUp += ScaleControl_MouseUp; 
      ScaleControl.MouseLeave += ScaleControl_MouseLeave;
      ScaleControl.MouseWheel += ScaleControl_MouseWheel;

      WaterfallControl.OpenglControl.MouseMove += WaterfallControl_MouseMove;
      WaterfallControl.OpenglControl.MouseDown += WaterfallControl_MouseDown;
      WaterfallControl.OpenglControl.MouseUp += WaterfallControl_MouseUp;
      WaterfallControl.OpenglControl.MouseWheel += WaterfallControl_MouseWheel;
    }

    private void WaterfallPanel_FormClosing(object sender, FormClosingEventArgs e)
    {
      Log.Information("Closing WaterfallPanel");
      ctx.MainForm.DestroySpectrumAnalyzer();
      ctx.WaterfallPanel = null;
      ctx.MainForm.WaterfallMNU.Checked = false;

      ctx.Settings.Waterfall.SplitterDistance = SplitContainer.SplitterDistance;

      ctx.SdrPasses.UpdateFrequencyRange();
      ScaleControl.BuildLabels();
    }

    private WaterfallQuality lastAppliedQuality = WaterfallQuality.Auto;
    private WaterfallSildersDlg? openSlidersDlg;

    public void ApplySettings()
    {
      var sett = ctx.Settings.Waterfall;
      WaterfallControl.Quality = sett.Quality;
      WaterfallControl.PerfCountersEnabled = sett.ShowPerformanceCounters;
      WaterfallControl.Brightness = sett.Brightness;
      WaterfallControl.Contrast = sett.Contrast;
      int paletteIndex = Math.Min(ctx.PaletteManager.Palettes.Count() - 1, sett.PaletteIndex);
      WaterfallControl.SetPalette(ctx.PaletteManager.Palettes[paletteIndex]);
      WaterfallControl.Refresh();

      ctx.MainForm.SetWaterfallSpeed();
      openSlidersDlg?.SyncPerformanceCountersFromSettings();

      if (sett.Quality != lastAppliedQuality)
      {
        lastAppliedQuality = sett.Quality;
        if (WaterfallControl.RecreateTexturesIfNeeded())
        {
          // Spectrum analyzer depends on WaterfallControl.SpectraWidth.
          ctx.MainForm.DestroySpectrumAnalyzer();
          ctx.MainForm.CreateSpectrumAnalyzer();
          ctx.MainForm.ConfigureWaterfall();
        }
      }
    }

    internal void SetPassband()
    {
      ScaleControl.CenterFrequency = SdrCenterFrequency;
      ScaleControl.VisibleBandwidth = ctx.Sdr.Info.MaxBandwidth;

      WaterfallControl.Zoom = ctx.Sdr.Info.SampleRate / ctx.Sdr.Info.MaxBandwidth;
      WaterfallControl.Pan = 0;

      ctx.SdrPasses.UpdateFrequencyRange();
      ScaleControl.BuildLabels();
    }

    public void SetCenterFrequency(double frequency)
    {

      ScaleControl.CenterFrequency = frequency;
      ValidateWaterfallViewport();
      ScaleControl.Refresh();

      if (ctx.Sdr != null)
      {
        WaterfallControl.Pan = (SdrCenterFrequency - ScaleControl.CenterFrequency) / ScaleControl.VisibleBandwidth * 2;
        WaterfallControl.OpenglControl.Refresh();
      }
    }

    internal void ClearWaterfall()
    {
      WaterfallControl.IndexedTexture.ClearBitmap();
      WaterfallControl.OpenglControl.Refresh();
    }

    private void SlidersBtn_Click(object sender, EventArgs e)
    {
      if (openSlidersDlg != null && !openSlidersDlg.IsDisposed && openSlidersDlg.Visible)
      {
        openSlidersDlg.Close();
        return;
      }

      openSlidersDlg = new WaterfallSildersDlg(ctx);
      openSlidersDlg.FormClosed += (_, _) => openSlidersDlg = null;
      openSlidersDlg.Location = WaterfallControl.PointToScreen(new Point(2, 2));
      openSlidersDlg.Show();
    }

    internal bool IsPointOnSlidersButton(Point screenPoint)
    {
      var bounds = new Rectangle(SlidersBtn.PointToScreen(Point.Empty), SlidersBtn.Size);
      return bounds.Contains(screenPoint);
    }

    private const double MinHzPerPixel = 20;
    private void ValidateWaterfallViewport()
    {
      double minBandwidth = ScaleControl.width * MinHzPerPixel;
      double visibleBandwidth = Math.Min(MaxBandwidth, Math.Max(minBandwidth, ScaleControl.VisibleBandwidth));

      double minFreq = SdrCenterFrequency - MaxBandwidth / 2 + visibleBandwidth / 2;
      double maxFreq = SdrCenterFrequency + MaxBandwidth / 2 - visibleBandwidth / 2;
      double centerFrequency = Math.Max(minFreq, Math.Min(maxFreq, ScaleControl.CenterFrequency));

      ScaleControl.VisibleBandwidth = visibleBandwidth;
      ScaleControl.CenterFrequency = centerFrequency;
      WaterfallControl.VisibleBandwidth = visibleBandwidth;
    }

    private void WaterfallControl_Resize(object sender, EventArgs e)
    {
      ScaleControl.HistoryRowCount = WaterfallControl.Height;
      ValidateWaterfallViewport();
    }






    //----------------------------------------------------------------------------------------------
    //                                   waterfall mouse 
    //----------------------------------------------------------------------------------------------
    Point MouseMovePos;
    int MouseDownX;
    double MouseDownFrequency;
    bool Dragging;

    private void WaterfallControl_MouseDown(object? sender, MouseEventArgs e)
    {
      MouseDownX = MouseMovePos.X = e.X;
      MouseDownFrequency = ScaleControl.CenterFrequency;
    }

    private void WaterfallControl_MouseMove(object? sender, MouseEventArgs e)
    {
      if (e.Location == MouseMovePos) return;
      MouseMovePos = e.Location;

      // dragging
      if (e.Button == MouseButtons.Left)
      {
        var dx = MouseMovePos.X - MouseDownX;
        if (!Dragging && Math.Abs(dx) > 2)
        {
          Dragging = true;
          WaterfallControl.Cursor = Cursors.NoMoveHoriz;
        }

        if (Dragging)
        {
          double frequency = MouseDownFrequency - dx * ScaleControl.VisibleBandwidth / ScaleControl.width;
          SetCenterFrequency(frequency);
        }
      }
      // moving over transponder
      else
      {
        var label = ScaleControl.GetTransponderUnderCursor(e.X);
        if (label == null)
          WaterfallControl.Cursor = Cursors.Cross;
        else
          WaterfallControl.Cursor = Cursors.PanSouth;
      }
    }

    private void WaterfallControl_MouseUp(object? sender, MouseEventArgs e)
    {
      if (!Dragging)
        HandleFrequencyClick(e.X, int.MaxValue);

      Dragging = false;
      WaterfallControl.Cursor = Cursors.Cross;
    }

    private void WaterfallControl_MouseWheel(object? sender, MouseEventArgs e)
    {
      double dx = e.X - ScaleControl.width / 2;
      double freq = ScaleControl.CenterFrequency + dx * ScaleControl.VisibleBandwidth / ScaleControl.width;

      ScaleControl.VisibleBandwidth = ScaleControl.VisibleBandwidth * Math.Pow(1.2, -e.Delta / 120);
      ValidateWaterfallViewport();

      ScaleControl.CenterFrequency = freq - dx * ScaleControl.VisibleBandwidth / ScaleControl.width;

      ValidateWaterfallViewport();

      WaterfallControl.Zoom = SamplingRate / ScaleControl.VisibleBandwidth;
      WaterfallControl.Pan = (SdrCenterFrequency - ScaleControl.CenterFrequency) / ScaleControl.VisibleBandwidth * 2;

      ScaleControl.Refresh();
      WaterfallControl.OpenglControl.Refresh();

      label1.Text = $"{ScaleControl.VisibleBandwidth / ScaleControl.Size.Width:F0} Hz/pix";
    }




    //----------------------------------------------------------------------------------------------
    //                                    scale mouse 
    //----------------------------------------------------------------------------------------------
    private void ScaleControl_MouseMove(object? sender, MouseEventArgs e)
    {
      if (e.Location == MouseMovePos) return;
      MouseMovePos = e.Location;

      // dragging
      if (e.Button == MouseButtons.Left)
      {
        var dx = e.X - MouseDownX;

        if (!Dragging && Math.Abs(dx) > 2 && ScaleControl.IsMouseInFilter(MouseDownX))
        {
          Dragging = true;
          MouseDownFrequency = ctx.FrequencyControl.GetDraggableFrequency();
          ScaleControl.Cursor = Cursors.NoMoveHoriz;
        }

        if (Dragging)
        {
          // adjust manual correction, or transponder offset, or RIT
          double freq = MouseDownFrequency + dx * ScaleControl.VisibleBandwidth / ScaleControl.width;
          ctx.FrequencyControl.SetDraggableFrequency(freq);
          ScaleControl.Refresh();
        }

        return;
      }

      // satellite label hover
      var label = ScaleControl.GetLabelUnderCursor(e.Location);
      if (label == null) toolTip1.ToolTipTitle = null;

      if (label != null)
      {
        if (toolTip1.ToolTipTitle != label.Pass.Satellite.name)
        {
          var parts = label.Pass.GetTooltipText(true);
          string tooltip = $"{parts[0]}  ({parts[2]})\n{parts[4]}\n{parts[5]}\n{label.Tooltip}";

          Point location = new((int)label.Rect.Right + 1, (int)label.Rect.Top);
          toolTip1.ToolTipTitle = label.Pass.Satellite.name;
          toolTip1.Show(tooltip, ScaleControl, location);
          ScaleControl.Cursor = Cursors.Hand;
        }
        return;
      }

      // transponder span hover
      label = ScaleControl.GetTransponderUnderCursor(e.X);
      if (label != null)
      {
        ScaleControl.Cursor = Cursors.PanSouth;
        toolTip1.Hide(ScaleControl);
        return;
      }

      // no hover
      toolTip1.Hide(ScaleControl);
      toolTip1.ToolTipTitle = null;
      ScaleControl.Cursor = Cursors.Cross;
    }

    private void ScaleControl_MouseDown(object? sender, MouseEventArgs e)
    {
      MouseDownX = MouseMovePos.X = e.X;
      MouseDownFrequency = ScaleControl.CenterFrequency;
    }

    private void ScaleControl_MouseUp(object? sender, MouseEventArgs e)
    {
      if (e.Button == MouseButtons.Left && !Dragging) HandleFrequencyClick(e.X, e.Y);

      if (e.Button == MouseButtons.Right) ctx.FrequencyControl.ToggleRit();

      if (ScaleControl.GetTransponderUnderCursor(MouseDownX) != null)
        ScaleControl.Cursor = Cursors.PanSouth;
      else
        ScaleControl.Cursor = Cursors.Cross;

      Dragging = false;
    }


    private void ScaleControl_MouseLeave(object? sender, EventArgs e)
    {
      toolTip1.Hide(ScaleControl);
    }

    private void ScaleControl_MouseWheel(object? sender, MouseEventArgs e)
    {
      var freq = ctx.FrequencyControl.RadioLink.CorrectedDownlinkFrequency;
      if (freq == null) return;
      var x = ScaleControl.FreqToPixel((double)freq);
      if (Math.Abs(x - e.X) > 200) return;

      int step = ModifierKeys.HasFlag(Keys.Alt) ? 500 : 20;
      ctx.FrequencyControl.IncrementDownlinkFrequency(e.Delta > 0 ? step : -step);
      ScaleControl.Refresh();
    }

    private void HandleFrequencyClick(int x, int y)
    {
      var label = ScaleControl.GetLabelUnderCursor(new(x, y));

      // label clicked
      if (label != null)
      {
        ctx.SatelliteSelector.SetSelectedTransmitter(label.Transmitters.First());
        ctx.SatelliteSelector.SetSelectedPass(label.Pass);
        return;
      }

      // ctrl-click, rit offset
      else if (ModifierKeys.HasFlag(Keys.Control))
      {
        ctx.FrequencyControl.SetRitFrequency(ScaleControl.PixelToFreq(x));
        return;
      }

      // tarnasponder bar clicked
      label = ScaleControl.GetTransponderUnderCursor(x);

      // tune to offset in transponder passband
      if (label != null)
      {
        double offset = ScaleControl.PixelToNominalFreq(label.Pass, DateTime.UtcNow, x) - label.Transponder!.DownlinkLow;
        ctx.FrequencyControl.SetTransponderOffset(label.Transponder, offset);
      }

      // tune to terrestrial frequency
      else
      {
        ctx.FrequencyControl.SetTerrestrialFrequency(ScaleControl.PixelToFreq(x));
      }
    }


    internal void BringInView(double value)
    {
      if (!ScaleControl.IsFrequencyVisible(value)) SetCenterFrequency(value);
    }




    //----------------------------------------------------------------------------------------------
    //                                    popup menu
    //----------------------------------------------------------------------------------------------
    private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
    {
      var label = ScaleControl.GetLabelUnderCursor(MouseMovePos);
      e.Cancel = label == null;
      if (e.Cancel) return;

      var sat = label.Pass.Satellite;

      SelectTransmitterMNU.Enabled = label!.Transmitters.Count > 1;
      if (SelectTransmitterMNU.Enabled)
      {
        SelectTransmitterMNU.DropDownItems.Clear();
        foreach (var tx in label.Transmitters)
        {
          var item = new ToolStripMenuItem(tx.description);
          item.Click += (s, e) => ctx.SatelliteSelector.SetSelectedTransmitter(tx);
          SelectTransmitterMNU.DropDownItems.Add(item);
        }
      }

      AddToGroupMNU.DropDownItems.Clear();
      foreach (var group in ctx.Settings.Satellites.SatelliteGroups)
      {
        var item = new ToolStripMenuItem(group.Name);
        item.Click += (s, e) => ctx.SatelliteSelector.AddToGroup(sat, group);
        item.Enabled = !group.SatelliteIds.Contains(sat.sat_id);
        AddToGroupMNU.DropDownItems.Add(item);
      }

      ReportToAmsatMNU.Enabled = sat.AmsatEntries.Any();
      ReportToAmsatMNU.Tag = sat;
    }

    private void ReportToAmsatMNU_Click(object sender, EventArgs e)
    {
      if (string.IsNullOrEmpty(ctx.Settings.User.Call))
      {
        MessageBox.Show("Please set your callsign in Settings before sending a report.",
          "Callsign not set", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
      }

      var item = (ToolStripMenuItem)sender;
      var sat = (SatnogsDbSatellite)item.Tag!;
      AmsatReportDialog.SendReport(ctx, sat);
    }

    private void satelliteDetailsToolStripMenuItem_Click(object sender, EventArgs e)
    {
      var label = ScaleControl.GetLabelUnderCursor(MouseMovePos);
      var sat = label.Pass.Satellite;
      SatelliteDetailsForm.ShowSatellite(sat, ctx.MainForm);
    }
  }
}
