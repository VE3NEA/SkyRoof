using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Security.Policy;
using Serilog;
using WeifenLuo.WinFormsUI.Docking;

namespace SkyRoof
{
  public partial class TimelinePanel : DockContent
  {
    private const double MaxPixelsPerMinute = 60; // at max zoom, 1 pixel = 1 second
    private int ScaleHeight;
    private int TopMargin;

    private static readonly TimeSpan HistoryTimeSpan = TimeSpan.FromMinutes(-30);
    private static readonly TimeSpan PredictionTimeSpan = TimeSpan.FromDays(2);
    private static double TotalMinutes = PredictionTimeSpan.Subtract(HistoryTimeSpan).TotalMinutes;

    private Context ctx;
    private double PixelsPerMinute;
    private DateTime LastAdvanceTime;
    private TimeSpan LeftSpan = TimeSpan.FromMinutes(5); // Now minus time at leftmost pixel
    double Zoom = 16;



    public TimelinePanel() { InitializeComponent(); } // for visual designer

    public TimelinePanel(Context ctx)
    {
      Log.Information("Creating TimelinePanel");
      this.ctx = ctx;
      InitializeComponent();
      
      ctx.TimelinePanel = this;
      ctx.MainForm.TimelineMNU.Checked = true;

      MouseWheel += SatelliteTimelineControl_MouseWheel;
    }

    private void TimelinePanel_FormClosing(object sender, FormClosingEventArgs e)
    {
      Log.Information("Closing TimelinePanel");
      ctx.TimelinePanel = null;
      ctx.MainForm.TimelineMNU.Checked = false;
    }

    private void TimelinePanel_Resize(object sender, EventArgs e)
    {
      Invalidate();
    }

    internal void Advance()
    {
      if ((DateTime.UtcNow - LastAdvanceTime).TotalMinutes < 0.5 / PixelsPerMinute) return;
      LastAdvanceTime = DateTime.UtcNow;

      Invalidate();
    }





    //----------------------------------------------------------------------------------------------
    //                                          draw
    //----------------------------------------------------------------------------------------------
    private static readonly Brush ShadowBrush = new SolidBrush(Color.FromArgb(25, Color.Black));

    private void TimelinePanel_Paint(object sender, PaintEventArgs e)
    {
      ValidateZoom();
      ValidateLeftSpan();
      var now = DateTime.Now;

      DrawBg(e.Graphics, now);
      DrawDateLabels(e.Graphics, now);
      DrawtimeLabels(e.Graphics, now);
      DrawElevationlabels(e.Graphics);
      DrawPasses(e.Graphics, now);
    }

    // todo: bg brightness for day/night
    private void DrawBg(Graphics g, DateTime now)
    {
      // chart
      var rect = new RectangleF(0, 0, ClientSize.Width, Math.Max(1, ClientSize.Height - ScaleHeight));
      LinearGradientBrush lgb = new LinearGradientBrush(rect, Color.SkyBlue, Color.White, LinearGradientMode.Vertical);
      g.FillRectangle(lgb, rect);

      // time scale
      int textHeight = TextRenderer.MeasureText("0", Font, Size, TextFormatFlags.NoPadding).Height;
      ScaleHeight = textHeight * 2 + 7;
      TopMargin = textHeight;
      rect = new RectangleF(0, ClientSize.Height - ScaleHeight, ClientSize.Width, ScaleHeight);
      g.FillRectangle(Brushes.Silver, rect);

      // past time shadow
      float x = TimeToPixel(now, now);
      rect = new RectangleF(0, 0, x, ClientSize.Height - ScaleHeight);
      g.FillRectangle(ShadowBrush, rect);

    }




    //----------------------------------------------------------------------------------------------
    //                                    draw labels
    //----------------------------------------------------------------------------------------------
    private void DrawDateLabels(Graphics g, DateTime now)
    {
      var date1 = now.Date;
      float x1 = -1;
      float x2;

      do
      {
        var date2 = date1.AddDays(1);
        x2 = Math.Min(ClientSize.Width, TimeToPixel(date2, now));
        g.DrawLine(Pens.Black, x2, ClientSize.Height - ScaleHeight, x2, ClientSize.Height);

        string label = $"{date1:MMM dd}";
        var size = TextRenderer.MeasureText(label, Font, Size, TextFormatFlags.NoPadding);
        size.Width += 3;

        if (x2 - x1 > size.Width + 15)
          g.DrawString(label, Font, Brushes.Black, (x1 + x2 - size.Width) / 2, ClientSize.Height - size.Height - 1);

        date1 = date2;
        x1 = Math.Max(-1, x2);
      }
      while (x2 < ClientSize.Width);
    }


    double[] Steps = [1, 2, 5, 10, 20, 30, 60, 120, 240, 360, 720];
    double[] SmallSteps = [1 / 6d, 1 / 2d, 1, 2, 5, 5, 15, 30, 60, 60, 120];

    private void DrawtimeLabels(Graphics g, DateTime now)
    {
      // compute steps
      TimeSpan step = TimeSpan.Zero;
      TimeSpan smallStep = TimeSpan.Zero;

      for (int i = 0; i < Steps.Count(); i++)
        if (Steps[i] * PixelsPerMinute > 59)
        {
          step = TimeSpan.FromMinutes(Steps[i]);
          smallStep = TimeSpan.FromMinutes(SmallSteps[i]);
          break;
        }
      if (step == TimeSpan.Zero) return;

      // draw ticks
      var time = TruncateTime(now.Add(HistoryTimeSpan), smallStep);
      var x = TimeToPixel(time, now);
      while (x < ClientSize.Width)
      {
        g.DrawLine(Pens.Black, x, ClientSize.Height - ScaleHeight, x, ClientSize.Height - ScaleHeight + 5);
        time = time.Add(smallStep);
        x = TimeToPixel(time, now);
      }

      // draw labels
      time = TruncateTime(now.Add(HistoryTimeSpan), step);
      x = TimeToPixel(time, now);
      while (x < ClientSize.Width)
      {
        string label = $"{time:HH:mm}";
        var size = TextRenderer.MeasureText(label, Font, Size, TextFormatFlags.NoPadding);
        size.Width += 3;
        g.DrawString(label, Font, Brushes.Black, x - size.Width / 2, ClientSize.Height - ScaleHeight + 7);

        g.DrawLine(Pens.Black, x, ClientSize.Height - ScaleHeight, x, ClientSize.Height - ScaleHeight + 10);
        time = time.Add(step);
        x = TimeToPixel(time, now);
      }
    }

    private void DrawElevationlabels(Graphics g)
    {
      float y = ClientSize.Height - ScaleHeight;
      float dy = (y - TopMargin) / 3;
      for (int el = 0; el <= 90; el += 30)
      {
        g.DrawLine(Pens.Black, 0, y, 7, y);
        g.DrawString($"{el:00}°", Font, Brushes.Black, 8, y - TopMargin);
        y -= dy;
      }
    }




    //----------------------------------------------------------------------------------------------
    //                                    draw passes
    //----------------------------------------------------------------------------------------------
    private readonly Dictionary<SatnogsDbSatellite, int> SatColorIndices = new();

    // hand-picked colors for the humps
    private const string ColorsString =
      "#FF0000,#FF8000,#FFBF00,#B2B200,#ACE600,#00E600,#00CC66,#00CCCC,#00ACE6,#1A8CFF,#9999FF,#B266FF,#FF33FF,#FF3399";
    private static Color[] SatColors = ColorsString.Split([',']).Select(ColorTranslator.FromHtml).ToArray();
    private static Pen[] SatPens = SatColors.Select(c => new Pen(c)).ToArray();
    private static Brush[] SatBrushes = SatColors.Select(c => new SolidBrush(Color.FromArgb(50, c))).ToArray();

    private Dictionary<RectangleF, SatellitePass> SatLabelRects = new();

    private int getColorIndex(SatnogsDbSatellite sat)
    {
      if (!SatColorIndices.ContainsKey(sat))
        SatColorIndices[sat] = SatColorIndices.Count % SatBrushes.Length;
      return SatColorIndices[sat];
    }

    private void DrawPasses(Graphics g, DateTime now)
    {
      if (ctx.GroupPasses == null) return;
      var passes = ctx.GroupPasses.GetPassesSnapshot().Where(p => !p.Geostationary);

      SatLabelRects.Clear();

      now = now.ToUniversalTime();
      float y0 = ClientSize.Height - ScaleHeight;
      float scaleY = (y0 - TopMargin) / 90f;

      g.SmoothingMode = SmoothingMode.AntiAlias;

      foreach (var pass in passes)
      {
        // skip if not visible
        var firstUtc = pass.StartTime;
        var firstX = TimeToPixel(firstUtc, now);
        var lastX = TimeToPixel(pass.EndTime, now);
        if (lastX < 0 || firstX > ClientSize.Width) continue;

        // hump
        var points = pass.ComputeHump(firstX, y0, (float)PixelsPerMinute, scaleY);
        int colorIndex = getColorIndex(pass.Satellite);
        g.FillPolygon(SatBrushes[colorIndex], points);
        g.DrawLines(SatPens[colorIndex], points);

        // label
        string text = pass.Satellite.name;
        var size = TextRenderer.MeasureText(text, Font, Size, TextFormatFlags.NoPadding);
        size.Width += 3;
        var rect = new RectangleF(
          TimeToPixel(pass.CulminationTime, now) - size.Width / 2,
          y0 - (float)pass.MaxElevation * scaleY - size.Height,
          size.Width + 1, size.Height); // without +1 the last char is sometimes truncated

        g.DrawString(text, Font, Brushes.Black, rect.Left, rect.Top);
        SatLabelRects[rect] = pass;
      }
    }




    //----------------------------------------------------------------------------------------------
    //                                    viewport
    //----------------------------------------------------------------------------------------------
    private void ValidateZoom()
    {
      double minPixelsPerMinute = ClientRectangle.Width / TotalMinutes;
      double maxZoom = MaxPixelsPerMinute / minPixelsPerMinute;
      Zoom = Math.Max(1, Math.Min(maxZoom, Zoom));
      PixelsPerMinute = minPixelsPerMinute * Zoom;
    }
    private void ValidateLeftSpan()
    {
      var maxLeftOffset = TimeSpan.FromMinutes(ClientRectangle.Width / PixelsPerMinute - TotalMinutes);
      if (LeftSpan > -HistoryTimeSpan) LeftSpan = -HistoryTimeSpan;
      if (LeftSpan < maxLeftOffset) LeftSpan = maxLeftOffset;
    }

    private float TimeToPixel(DateTime time, DateTime now)
    {
      return (float)((time - (now - LeftSpan)).TotalMinutes * PixelsPerMinute);
    }

    private DateTime PixelToTime(float x, DateTime now)
    {
      return now - LeftSpan + TimeSpan.FromMinutes(x / PixelsPerMinute);
    }

    DateTime TruncateTime(DateTime time, TimeSpan span)
    {
      return new DateTime((time.Ticks / span.Ticks) * span.Ticks, time.Kind);
    }




    //----------------------------------------------------------------------------------------------
    //                                        mouse
    //----------------------------------------------------------------------------------------------
    private int MouseDownX;
    private TimeSpan MouseDownLeftSpan;
    private Point MouseMovePos;
    private bool Dragging;

    private void SatelliteTimelineControl_MouseWheel(object? sender, MouseEventArgs e)
    {
      var now = DateTime.Now;
      var timeUnderCursor = PixelToTime(e.X, now);

      int dZoom = e.Delta / 120;// WHEEL_DELTA;
      Zoom *= Math.Pow(2, dZoom / 4d);
      ValidateZoom();

      LeftSpan -= timeUnderCursor - PixelToTime(e.X, now);

      Invalidate();
    }

    private void SatelliteTimelineControl_MouseDown(object sender, MouseEventArgs e)
    {
      MouseDownX = e.X;
      MouseDownLeftSpan = LeftSpan;

      if (!Dragging)
      {
        var rect = GetSatRectAt(new PointF(e.X, e.Y));
        if (!rect.IsEmpty && e.Button == MouseButtons.Left)
          {
            var pass = SatLabelRects[rect];
            var sat = pass.Satellite;
            ctx.SatelliteSelector.SetSelectedSatellite(sat);
            ctx.SatelliteSelector.SetSelectedPass(pass);
          }
      }
    }

    private void SatelliteTimelineControl_MouseMove(object sender, MouseEventArgs e)
    {
      if (e.X == MouseMovePos.X) return;
      MouseMovePos = new Point(e.X, e.Y);

      // dragging: scroll
      if (Dragging)
      {
        Cursor = Cursors.NoMoveHoriz;
        LeftSpan = MouseDownLeftSpan - TimeSpan.FromMinutes((MouseDownX - MouseMovePos.X) / PixelsPerMinute);
        Invalidate();
        Update();
      }

      // moving with button pressed: switch to dragging
      else if (e.Button == MouseButtons.Left && Math.Abs(e.X - MouseDownX) > 2)
      {
        Dragging = true;
        //Cursor = Cursors.NoMoveHoriz;
        Cursor = Cursors.SizeWE;
        toolTip1.Hide(this);
      }

      // moving over label: show tooltip
      else
      {
        var rect = GetSatRectAt(new PointF(e.X, e.Y));
        if (!rect.IsEmpty)
        {
          Cursor = Cursors.Hand;

          var pass = SatLabelRects[rect];
          string tooltip = pass.Satellite.GetTooltipText() + "\n\n" + string.Join("\n", pass.GetTooltipText(false));
          if (tooltip != toolTip1.GetToolTip(this))
          {
            toolTip1.ToolTipTitle = pass.Satellite.name;
            toolTip1.Show(tooltip, this, (int)rect.Right, (int)rect.Top);
          }
        }

        // hide tooltip
        else
        {
          Cursor = Cursors.Default;
          toolTip1.Hide(this);
        }
      }
    }

    private void SatelliteTimelineControl_MouseUp(object sender, MouseEventArgs e)
    {
      Dragging = false;
      Cursor = Cursors.Default;
    }

    private RectangleF GetSatRectAt(PointF point)
    {
      return SatLabelRects.Keys.FirstOrDefault(rect => rect.Contains(point));
    }

    private void TimelinePanel_MouseLeave(object sender, EventArgs e)
    {
      toolTip1.Hide(this);
    }
  }
}
