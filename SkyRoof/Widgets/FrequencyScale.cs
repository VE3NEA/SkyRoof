using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SGPdotNET.Observation;
using SharpGL.SceneGraph.Assets;
using VE3NEA;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace SkyRoof
{
  public partial class FrequencyScale : UserControl
  {
    private static readonly double[] TickMults = { 2, 2.5, 2 };

    public Context ctx;
    private List<TransmitterLabel> Labels = new();
    private List<TransmitterLabel> VisibleLabels = new();

    private readonly List<float> LastXPositions = new();
    private Brush BlueSpanBrush = new SolidBrush(Color.FromArgb(20, Color.Blue));
    private Brush GraySpanBrush = new SolidBrush(Color.FromArgb(20, Color.Gray));
    private Brush PassbandBrush = new SolidBrush(Color.FromArgb(200, Color.Lime));
    private Brush BgBrush;
    private readonly Font BoldFont;

    public double CenterFrequency = SdrConst.UHF_CENTER_FREQUENCY;
    internal double VisibleBandwidth = SdrConst.MAX_BANDWIDTH;
    internal int HistoryRowCount = 1500;
    internal int width;
    private int height;

    public FrequencyScale()
    {
      InitializeComponent();
      DoubleBuffered = true;

      BgBrush = new SolidBrush(BackColor);
      BoldFont = new Font(Font, FontStyle.Bold);
    }

    private void FrequencyScale_Resize(object sender, EventArgs e)
    {
      width = ClientSize.Width;
      height = ClientSize.Height;
      Invalidate();
    }

    public double FreqToPixel(double f)
    {
      double df = f - CenterFrequency;
      double dx = df * width / VisibleBandwidth;
      return width / 2d + dx;
    }

    internal double PixelToFreq(int x)
    {
      double dx = x - width / 2d;
      return CenterFrequency + dx * VisibleBandwidth / width;
    }

    public double CorrectedFreqToPixel(SatellitePass pass, DateTime time, double freq)
    {
      var cust = ctx.Settings.Satellites.SatelliteCustomizations.GetOrCreate(pass.Satellite.sat_id);

      if (cust.DownlinkDopplerCorrectionEnabled)
      {
        var obs = pass.GetObservationAt(time);
        if (obs != null) freq *= 1 - obs.RangeRate / 3e5;
      }
      if (cust.DownlinkManualCorrectionEnabled) freq += cust.DownlinkManualCorrection;

      return FreqToPixel(freq);
    }

    public double PixelToNominalFreq(SatellitePass pass, DateTime time, int x)
    {
      double freq = PixelToFreq(x);

      var cust = ctx.Settings.Satellites.SatelliteCustomizations.GetOrCreate(pass.Satellite.sat_id);
      if (cust.DownlinkDopplerCorrectionEnabled)
      {
        var obs = pass.GetObservationAt(time);
        if (obs != null) freq /= 1 - obs.RangeRate / 3e5;
      }
      if (cust.DownlinkManualCorrectionEnabled) freq -= cust.DownlinkManualCorrection;
      return freq;
    }




    //----------------------------------------------------------------------------------------------
    //                                        paint
    //----------------------------------------------------------------------------------------------
    // todo: simplied draw when only the green rect moves
    private void FrequencyScale_Paint(object sender, PaintEventArgs e)
    {
      var g = e.Graphics;
      g.FillRectangle(BgBrush, ClientRectangle);
      DrawTicks(g);
      DrawPassband(g);
      DrawTransmitters(g);
    }

    private RectangleF GetPassbandRect(bool includeRit = false)
    {
      double centerFrequency = (double)ctx.FrequencyControl.RadioLink.CorrectedDownlinkFrequency! + (ctx.Slicer?.GetModeoffset() ?? 0);

      if (ctx.FrequencyControl.RadioLink.RitEnabled && !includeRit) 
        centerFrequency -= ctx.FrequencyControl.RadioLink.RitOffset;

      double minWing = 3 * VisibleBandwidth / width;
      double wing = Math.Max(minWing, ctx.Slicer.Bandwidth / 2);

      float centerPix = (float)Math.Round(FreqToPixel(centerFrequency));
      float wingPix = (float)Math.Round(wing * width / VisibleBandwidth);

      float top = includeRit ? height - 42 : height - 45;
      return new RectangleF(centerPix - wingPix, top, 2 * wingPix, height);
    }

    private void DrawPassband(Graphics g)
    {
      if (ctx?.Slicer?.Enabled != true) return;

      // main passband
      var rect = GetPassbandRect();
      g.FillRectangle(PassbandBrush, rect);
      g.DrawRectangle(Pens.Green, rect);

      // rit
      if (ctx.FrequencyControl.RadioLink.RitEnabled)
      {
        rect = GetPassbandRect(true);
        g.DrawRectangle(Pens.Green, rect);
      }

      // carrier frequency
      double carrierFrequency = (double)ctx.FrequencyControl.RadioLink.CorrectedDownlinkFrequency!;
      float x = (float)Math.Round(FreqToPixel(carrierFrequency));
      float y = height - 62;
      g.DrawLine(Pens.Red, x, y, x, height);
      g.FillEllipse(Brushes.Red, x - 3, y - 3, 6, 6);
    }

    private void DrawTicks(Graphics g)
    {
      double leftFreq = CenterFrequency - VisibleBandwidth / 2;
      double pixPerHz = width / VisibleBandwidth;

      string sampleText = (CenterFrequency * 1e-6).ToString("F3");
      var labelSize = TextRenderer.MeasureText(sampleText, Font, Size, TextFormatFlags.NoPadding);

      //select tick step
      double TickStep = 200;
      double LabelStep = 1000;
      for (int i = 0; i <= 24; ++i)
      {
        if (LabelStep * pixPerHz > labelSize.Width + 25) break;
        LabelStep *= TickMults[i % 3];
        TickStep *= TickMults[(i + 1) % 3];
      }

      //first label's frequency
      double leftmostLabelFrequency = Math.Round(Math.Truncate(leftFreq / LabelStep) * LabelStep);
      double freq = leftmostLabelFrequency;

      //draw lagre ticks and labels
      while (true)
      {
        // large tick
        float x = (float)FreqToPixel(freq);
        if (x > width) break;
        float y = height - 0.7f * labelSize.Height;
        g.DrawLine(Pens.Black, x, y, x, height);

        // label
        string freqText = (freq * 1e-6).ToString("F3");
        x -= g.MeasureString(freqText, Font).Width / 2;
        y = height - 1.8f * labelSize.Height;
        g.DrawString(freqText, Font, Brushes.Black, x, y);

        freq += LabelStep;
      }

      //draw small ticks 
      freq = leftmostLabelFrequency;
      while (true)
      {
        float x = (float)FreqToPixel(freq);
        if (x > width) break;
        float y = height - 0.35f * labelSize.Height;
        g.DrawLine(Pens.Black, x, y, x, height);
        freq += TickStep;
      }
    }

    private bool IsLabelVisible(TransmitterLabel label)
    {
      // vertical
      var historyLength = TimeSpan.FromSeconds(HistoryRowCount / ctx.Settings.Waterfall.Speed);
      if (label.Pass.EndTime < DateTime.UtcNow - historyLength) return false;

      // horizontal
      if (label.Span != null) return true;
      return label.x >= 0 && label.x <= width;
    }

    private void DrawTransmitters(Graphics g)
    {
      if (Labels.Count == 0) return;

      // recompute labels' X
      var now = DateTime.UtcNow;
      foreach (var label in Labels) label.x = (float)CorrectedFreqToPixel(label.Pass, now, label.Frequency);
      VisibleLabels = Labels.Where(IsLabelVisible).OrderByDescending(label => label.x).ToList();

      // draw spans
      foreach (var label in VisibleLabels.Where(lb => lb.Span != null)) DrawSpan(label, g);

      // draw labels
      LastXPositions.Clear();
      foreach (var label in VisibleLabels) DrawLabel(g, label, now);

      // past triangles
      foreach (var label in VisibleLabels)
        if (label.Pass.EndTime < now)
          DrawTriangle(label, g, Brushes.Silver);

      // future triangles
      foreach (var label in VisibleLabels)
        if (label.Pass.StartTime > now && label.Pass.StartTime < now.AddMinutes(6))
          DrawTriangle(label, g, Brushes.White);

      // current triangles
      foreach (var label in VisibleLabels)
        if (label.Pass.StartTime <= now && label.Pass.EndTime >= now)
          DrawTriangle(label, g, Brushes.Lime);
    }

    private void DrawLabel(Graphics g, TransmitterLabel label, DateTime now)
    {
      if (label.x < 0 || label.x > width) return;

      bool inGroup = ctx.SatelliteSelector.GroupSatellites.Contains(label.Pass.Satellite);
      var font = inGroup ? BoldFont : Font;
      var size = TextRenderer.MeasureText(label.Pass.Satellite.name, font, Size, TextFormatFlags.NoPadding);

      // find the lowest row with enough space for the label
      int row = 0;
      while (true)
      {
        if (LastXPositions.Count <= row) LastXPositions.Add(int.MaxValue);
        if ((label.x + size.Width) < LastXPositions[row]) break;
        row++;
      }
      LastXPositions[row] = label.x;

      // rect from x, row and size
      float LastY = height - 1.8f * size.Height - row * (size.Height - 3);

      label.Rect = new RectangleF(label.x, LastY - size.Height, size.Width + 3, size.Height);

      // line
      g.DrawLine(Pens.Blue, label.x, height, label.x, LastY);

      // selected sat BG
      if (label.Transmitters.Contains(ctx.SatelliteSelector.SelectedTransmitter))
        if (ctx.FrequencyControl.RadioLink.IsTerrestrial)
          g.FillRectangle(Brushes.PaleTurquoise, label.Rect);
        else
          g.FillRectangle(Brushes.Aqua, label.Rect);

      // sat name
      var brush = Brushes.Blue;
      if (label.Pass.StartTime > now) brush = Brushes.Black;
      else if (label.Pass.EndTime < now) brush = Brushes.Gray;

      //var brush = label.Pass.StartTime <= now && label.Pass.EndTime >= now ? Brushes.Blue : Brushes.Gray;
      g.DrawString(label.Pass.Satellite.name, font, brush, label.Rect.Location);
    }

    const int SPAN_HEIGHT = 14;
    private void DrawSpan(TransmitterLabel label, Graphics g)
    {
      label.endX = (float)CorrectedFreqToPixel(label.Pass, DateTime.UtcNow, label.Frequency + (long)label.Span!);
      if (label.x > width || label.endX < 0) return;

      RectangleF r = new(label.x, height - SPAN_HEIGHT-1, label.endX - label.x, SPAN_HEIGHT);

      bool isNow = label.Pass.StartTime <= DateTime.UtcNow && label.Pass.EndTime >= DateTime.UtcNow;
      g.FillRectangle(isNow ? BlueSpanBrush : GraySpanBrush, r);
      g.DrawRectangle(isNow ? Pens.Blue : Pens.Gray, r);
    }

    private void DrawTriangle(TransmitterLabel label, Graphics g, Brush brush)
    {
      if (label.Span != null && label.Span != 0) return;
      if (label.x < 0 || label.x > width) return;

      int y = height - 1;

      PointF[] points = {
        new PointF(label.x, y),
        new PointF(label.x - 5, y-9),
        new PointF(label.x + 5, y-9),
      };

      g.FillPolygon(brush, points);
      g.DrawPolygon(Pens.Black, points);
    }




    //----------------------------------------------------------------------------------------------
    //                                        labels
    //----------------------------------------------------------------------------------------------
    public void BuildLabels()
    {
      if (ctx == null) return;

      var now = DateTime.UtcNow;
      Labels.Clear();

      bool hamBand = ctx.Sdr == null || SatnogsDbTransmitter.IsHamFrequency(ctx.Sdr.Info.Frequency);
      SatellitePasses passes = hamBand ? ctx.HamPasses : ctx.SdrPasses;
      
      foreach (var pass in passes.Passes)
        if (pass.StartTime < now.AddMinutes(6) && pass.EndTime > now.AddMinutes(-25))
        {
          var transmitters = pass.Satellite.Transmitters.Where(tx => tx.alive);
          var freqs = transmitters.Select(tx => tx.DownlinkLow).Distinct();

          foreach (var freq in freqs)
            Labels.Add(new TransmitterLabel(pass, freq));
        }
    }

    internal TransmitterLabel? GetLabelUnderCursor(Point location)
    {
      return VisibleLabels.FirstOrDefault(label => label.Rect.Contains(location));
    }

    internal bool IsFrequencyVisible(double value)
    {
      var x = FreqToPixel(value);
      return x >= 0 && x < width;
    }

    internal TransmitterLabel? GetTransponderUnderCursor(int x)
    {
      return Labels.FirstOrDefault(label => label.Transponder != null && x >= label.x  && x <= label.endX);      
    }

    internal bool IsMouseInFilter(int x)
    {
      if (ctx?.Slicer?.Enabled != true) return false;

      var rect = GetPassbandRect();
      return x >= rect.Left && x <= rect.Right;
    }
  }
}
