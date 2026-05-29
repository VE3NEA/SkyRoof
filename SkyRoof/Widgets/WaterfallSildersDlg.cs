using System.Drawing.Drawing2D;
using FontAwesome;

namespace SkyRoof
{
  public partial class WaterfallSildersDlg : Form
  {
    private const int MinClientWidth = 240;
    private const int PerfTopMargin = 8;
    private const int BottomPadding = 6;
    private const int HorizontalPadding = 10;

    private Context ctx;
    private bool changing;
    private WaterfallControl.WaterfallPerfSnapshot? lastSnapshot;
    private DateTime lastSnapshotUtc;

    public WaterfallSildersDlg()
    {
      InitializeComponent();
    }

    internal WaterfallSildersDlg(Context ctx)
    {
      this.ctx = ctx;

      InitializeComponent();

      label1.Font = ctx.AwesomeFont14;
      label1.Text = FontAwesomeIcons.Sun;
      label2.Font = ctx.AwesomeFont14;
      label2.Text = FontAwesomeIcons.CircleHalfStroke;
      label3.Font = ctx.AwesomeFont14;
      label3.Text = FontAwesomeIcons.DownLong;
      label4.Font = ctx.AwesomeFont14;
      label4.Text = FontAwesomeIcons.Palette;

      panel1.SendToBack();

      SettingsToDialog();
      PerformLayout();
      SyncPerformanceCountersFromSettings();
    }

    internal void SyncPerformanceCountersFromSettings()
    {
      bool showPerf = ctx.Settings.Waterfall.ShowPerformanceCounters;
      PerfLabel.Visible = showPerf;

      if (showPerf)
      {
        lastSnapshot = null;
        lastSnapshotUtc = DateTime.UtcNow;
        perfTimer.Start();
      }
      else
      {
        perfTimer.Stop();
      }

      UpdatePerfLayout();
    }

    private void UpdatePerfLayout()
    {
      PerformLayout();

      bool showPerf = PerfLabel.Visible;
      int slidersBottom = GetSlidersBottom();
      int width = GetSlidersClientWidth();

      int height = slidersBottom + BottomPadding;
      if (showPerf)
      {
        PerfLabel.MaximumSize = new Size(width - HorizontalPadding, 0);
        PerfLabel.Location = new Point(5, slidersBottom + PerfTopMargin);
        width = Math.Max(width, PerfLabel.Right + HorizontalPadding);
        height = PerfLabel.Bottom + BottomPadding;
      }

      if (ClientSize.Width != width || ClientSize.Height != height)
        ClientSize = new Size(width, height);
    }

    private int GetSlidersClientWidth()
    {
      int right = 0;
      foreach (Control control in EnumerSliderControls())
        right = Math.Max(right, GetControlRightEdge(control));

      return Math.Max(MinClientWidth, right + HorizontalPadding);
    }

    private static int GetControlRightEdge(Control control)
    {
      if (control.AutoSize)
        return control.Left + control.GetPreferredSize(Size.Empty).Width;

      return control.Right;
    }

    private IEnumerable<Control> EnumerSliderControls()
    {
      foreach (Control control in Controls)
      {
        if (control == PerfLabel) continue;

        if (control == panel1)
        {
          foreach (Control child in panel1.Controls)
            yield return child;
          continue;
        }

        yield return control;
      }
    }

    private int GetSlidersBottom()
    {
      int bottom = 0;
      foreach (Control control in Controls)
      {
        if (control == PerfLabel) continue;

        if (control == panel1)
        {
          foreach (Control child in panel1.Controls)
            bottom = Math.Max(bottom, child.Bottom);
          continue;
        }

        bottom = Math.Max(bottom, control.Bottom);
      }

      return bottom;
    }

    private void WaterfallSildersDlg_Deactivate(object sender, EventArgs e)
    {
      perfTimer.Stop();

      // SlidersBtn toggles visibility; don't auto-close when that button is clicked.
      if (ctx.WaterfallPanel?.IsPointOnSlidersButton(Cursor.Position) == true)
        return;

      Close();
    }


    private void WaterfallSildersDlg_KeyPress(object sender, KeyPressEventArgs e)
    {
      if (e.KeyChar == (char)Keys.Escape) Close();
    }

    private void perfTimer_Tick(object? sender, EventArgs e)
    {
      if (ctx?.Settings?.Waterfall?.ShowPerformanceCounters != true)
      {
        SyncPerformanceCountersFromSettings();
        return;
      }

      var wc = ctx?.WaterfallPanel?.WaterfallControl;
      if (wc == null)
      {
        PerfLabel.Text = "FPS —\r\ndrop —\r\nupd —\r\ndraw —";
        UpdatePerfLayout();
        return;
      }

      var snap = wc.GetPerfSnapshot();
      var now = DateTime.UtcNow;
      double dt = (now - lastSnapshotUtc).TotalSeconds;

      double uploadsPerSec = 0;
      double drawsPerSec = 0;
      double uploadMsAvg = 0;
      double drawMsAvg = 0;

      if (dt > 0.1 && lastSnapshot is WaterfallControl.WaterfallPerfSnapshot prev)
      {
        long dUploads = snap.UploadCalls - prev.UploadCalls;
        long dDraws = snap.DrawCalls - prev.DrawCalls;
        long dUploadTicks = snap.UploadTimeTicks - prev.UploadTimeTicks;
        long dDrawTicks = snap.DrawTimeTicks - prev.DrawTimeTicks;

        uploadsPerSec = dUploads / dt;
        drawsPerSec = dDraws / dt;

        if (dUploads > 0)
          uploadMsAvg = 1000.0 * dUploadTicks / snap.StopwatchFrequency / dUploads;

        if (dDraws > 0)
          drawMsAvg = 1000.0 * dDrawTicks / snap.StopwatchFrequency / dDraws;
      }

      lastSnapshot = snap;
      lastSnapshotUtc = now;

      PerfLabel.Text =
        $"FPS {snap.Fps:0.0}\r\n" +
        $"drop {snap.DroppedUploads}\r\n" +
        $"upd {uploadsPerSec:0.0}/s  {uploadMsAvg:0.00} ms\r\n" +
        $"draw {drawsPerSec:0.0}/s  {drawMsAvg:0.00} ms";

      UpdatePerfLayout();
    }
    
    private void comboBox1_DrawItem(object sender, DrawItemEventArgs e)
    {
      var rect = e.Bounds;

      if (!e.State.HasFlag(DrawItemState.ComboBoxEdit))
      {
        e.Graphics.FillRectangle(Brushes.White, e.Bounds);
        rect.Inflate(-2, -1);
        rect.Width -= 17; // make dropdown entries the same width as combo's edit box
      }

      if (e.Index >= 0)
      {
        e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
        e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        e.Graphics.DrawImage(ctx.PaletteManager.Palettes[e.Index].ToBitmap(), rect);
      }
    }


    private readonly int[] Speeds = [1, 2, 4, 8, 16];

    private void Trackbar_ValueChanged(object sender, EventArgs e)
    {
      BrightnessLabel.Text = BrightnessTrackbar.Value.ToString();
      ContrastLabel.Text = ContrastTrackbar.Value.ToString();
      SpeedLabel.Text = Speeds[SpeedTrackbar.Value].ToString();

      if (changing) return;
      DialogToSettings();
      ctx.WaterfallPanel?.ApplySettings();
      UpdatePerfLayout();
    }

    private void SettingsToDialog()
    {
      changing = true;
      var sett = ctx.Settings.Waterfall;

      BrightnessTrackbar.Value = (int)(50 * (1 + sett.Brightness));
      ContrastTrackbar.Value = (int)(100 * sett.Contrast);

      SpeedTrackbar.Maximum = Speeds.Length - 1;
      SpeedTrackbar.Value = Array.IndexOf(Speeds, sett.Speed);

      PaletteComboBox.DataSource = ctx.PaletteManager.Palettes;
      PaletteComboBox.SelectedIndex = Math.Max(0, Math.Min(ctx.PaletteManager.Palettes.Count() - 1, sett.PaletteIndex));

      BrightnessLabel.Text = BrightnessTrackbar.Value.ToString();
      ContrastLabel.Text = ContrastTrackbar.Value.ToString();
      SpeedLabel.Text = Speeds[SpeedTrackbar.Value].ToString();

      changing = false;
    }

    private void DialogToSettings()
    {
      var sett = ctx.Settings.Waterfall;
      sett.Brightness = BrightnessTrackbar.Value / 50f - 1; // 0..100 -> -1..1
      sett.Contrast = ContrastTrackbar.Value / 100f;        // 0..100 -> 0..1
      sett.Speed = Speeds[SpeedTrackbar.Value];
      sett.PaletteIndex = PaletteComboBox.SelectedIndex;
    }
  }
}
