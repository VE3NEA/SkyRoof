using System.Data;
using System.Drawing.Drawing2D;
using WeifenLuo.WinFormsUI.Docking;
using VE3NEA;
using SGPdotNET.Observation;
using Serilog;

namespace SkyRoof
{
  public partial class SkyViewPanel : DockContent
  {
    private readonly Context ctx;
    private SatellitePass? Pass;
    private Dictionary<RectangleF, SatellitePass> SatLabelRects = new();
    
    private readonly Font RegularFont, BoldFont;
    private readonly Image OkImage, XMarkImage, ArrowImage, SatImage;
    private readonly Brush BlueBrush = new SolidBrush(Color.FromArgb(230, 249, 255));
    private readonly Brush SilverBrush = new SolidBrush(Color.FromArgb(242, 242, 242));
    private readonly Brush PinkBrush = new SolidBrush(Color.FromArgb(50, 255, 0, 0));


    public SkyViewPanel() { InitializeComponent(); }

    public SkyViewPanel(Context ctx)
    {
      Log.Information("Creating SkyViewPanel");
      this.ctx = ctx;
      InitializeComponent();
      
      ctx.SkyViewPanel = this;
      ctx.MainForm.SkyViewMNU.Checked = true;

      RegularFont = new Font(OrbitRadioBtn.Font, FontStyle.Regular);
      BoldFont = new Font(OrbitRadioBtn.Font, FontStyle.Bold);

      // icons from https://www.iconsdb.com/
      using (var ms = new MemoryStream(Properties.Resources.ok)) { OkImage = Image.FromStream(ms); }
      using (var ms = new MemoryStream(Properties.Resources.xmark)) { XMarkImage = Image.FromStream(ms); }
      using (var ms = new MemoryStream(Properties.Resources.arrow)) { ArrowImage = Image.FromStream(ms); }
      using (var ms = new MemoryStream(Properties.Resources.satellite)) { SatImage = Image.FromStream(ms); }

      Utils.SetDoubleBuffered(DrawPanel, true);
    }

    private void SkyViewPanel_FormClosing(object sender, FormClosingEventArgs e)
    {
      Log.Information("Closing SkyViewPanel");
      ctx.SkyViewPanel = null;
      ctx.MainForm.SkyViewMNU.Checked = false;
    }
    
    private void radioButton_CheckedChanged(object sender, EventArgs e)
    {
      var radioBtn = (RadioButton)sender;
      if (!radioBtn.Checked) return;

      OrbitRadioBtn.Font = OrbitRadioBtn.Checked ? BoldFont : RegularFont;

      SetLabels();
      DrawPanel.Invalidate();
    }

    private void panel_Resize(object sender, EventArgs e)
    {
      DrawPanel.Invalidate();
    }




    //----------------------------------------------------------------------------------------------
    //                                  public interface
    //----------------------------------------------------------------------------------------------
    internal void SetPass(SatellitePass? pass)
    {
      if (pass == null) return;

      Pass = pass;
      OrbitRadioBtn.Text = $"Orbit #{Pass.OrbitNumber} of {Pass.Satellite.name}";
      OrbitRadioBtn.Enabled = true;

      if (ctx.SatelliteSelector.GroupSatellites.Contains(pass.Satellite) &&
        pass.StartTime < DateTime.UtcNow && pass.EndTime > DateTime.UtcNow)
        RealTimeRadioBtn.Checked = true;
      else
        OrbitRadioBtn.Checked = true;

      SetLabels();
      DrawPanel.Invalidate();
    }

    internal void ClearPass()
    {
      Pass = null;
      OrbitRadioBtn.Enabled = false;
      OrbitRadioBtn.Text = "Selected Orbit";
      RealTimeRadioBtn.Checked = true;
    }

    public void Advance() // on timer tick
    {
      var now = DateTime.UtcNow;  
      if (OrbitRadioBtn.Checked 
        && ctx.SatelliteSelector.GroupSatellites.Contains(Pass!.Satellite)
        && Pass!.StartTime < now 
        && Pass!.EndTime > now) 
          RealTimeRadioBtn.Checked = true;

      SetLabels();
      DrawPanel.Invalidate();
    }




    //----------------------------------------------------------------------------------------------
    //                                          draw
    //----------------------------------------------------------------------------------------------
    private void panel_Paint(object sender, PaintEventArgs e)
    {
      var g = e.Graphics;
      g.SmoothingMode = SmoothingMode.AntiAlias;
      DrawBackground(g);
      DrawRotatorPosition(g);

      SatLabelRects.Clear();

      if (OrbitRadioBtn.Checked) DrawPass(g, Pass);
      else DrawRealTime(g);
    }

    private void DrawRotatorPosition(Graphics g)
    {
      if (!ctx.RotatorControl.IsRunning()) return;
      if (ctx.RotatorControl.AntBearing == null) return; 

      // Use Az/El (radians) for coordinate calculations
      var center = AzElToXY(ctx.RotatorControl.AntBearing.Az, ctx.RotatorControl.AntBearing.El);
      float size = 4 * Math.Min(16, 0.04f * Radius + 5);

      RectangleF rect = new(center.X - size / 2, center.Y - size / 2, size, size);
      var brush = PinkBrush;
      g.FillEllipse(brush, rect);
    }

    private void DrawBackground(Graphics g)
    {
      var sizeN = g.MeasureString("N", DrawPanel.Font);
      var sizeS = g.MeasureString("S", DrawPanel.Font);
      var sizeE = g.MeasureString("E", DrawPanel.Font);
      var sizeW = g.MeasureString("W", DrawPanel.Font);

      Center = new PointF(DrawPanel.Width / 2, DrawPanel.Height / 2);
      float radiusX = Center.X - Math.Max(sizeE.Width, sizeW.Width) - 4;
      float radiusY = Center.Y - Math.Max(sizeN.Width, sizeS.Width) - 4;
      Radius = Math.Min(radiusX, radiusY);

      // fill bg 
      g.FillRectangle(Brushes.White, DrawPanel.ClientRectangle);
      RectangleF rect = new RectangleF(Center.X - Radius, Center.Y - Radius, 2 * Radius, 2 * Radius);
      var brush = OrbitRadioBtn.Checked ? SilverBrush : BlueBrush;
      g.FillEllipse(brush, rect);

      // circles
      var pen = OrbitRadioBtn.Checked ? Pens.Gray : Pens.Teal;
      for (float r = Radius / 3; r <= 1.1 * Radius; r += Radius / 3)
      {
        rect = new RectangleF(Center.X - r, Center.Y - r, 2 * r, 2 * r);
        g.DrawEllipse(pen, rect);
      }

      // straight lines
      g.DrawLine(pen, Center.X - Radius, Center.Y, Center.X + Radius, Center.Y);
      g.DrawLine(pen, Center.X, Center.Y - Radius, Center.X, Center.Y + Radius);

      // compass labels
      g.DrawString("N", DrawPanel.Font, Brushes.Teal, Center.X - sizeN.Width / 2, Center.Y - Radius - sizeN.Height - 2);
      g.DrawString("S", DrawPanel.Font, Brushes.Teal, Center.X - sizeS.Width / 2, Center.Y + Radius + 2);
      g.DrawString("E", DrawPanel.Font, Brushes.Teal, Center.X + Radius + 2, Center.Y - sizeE.Height / 2);
      g.DrawString("W", DrawPanel.Font, Brushes.Teal, Center.X - Radius - sizeW.Width - 2, Center.Y - sizeW.Height / 2);
    }

    private void DrawRealTime(Graphics g)
    {
      var now = DateTime.UtcNow;
      var passes = ctx.GroupPasses.GetPassesSnapshot().Where(p => now > p.StartTime && now < p.EndTime);
      foreach (var pass in passes) DrawPass(g, pass);
    }

    private void DrawPass(Graphics g, SatellitePass? pass)
    {
      if (pass == null) return;

      // compute track points
      var track = pass.Track;
      var points = new PointF[track.Count];
      for (int i = 0; i < track.Count; i++)
        points[i] = TrackPointToXY(pass.Track[i]);

      // draw track
      var pen = new Pen(Color.Green, 2f);
      g.DrawLines(pen, points);
      var startPoint = TrackPointToXY(pass.Track.First());
      var endPoint = TrackPointToXY(pass.Track.Last());

      // draw end icons
      float size = Math.Min(16, 0.04f * Radius + 5);

      g.TranslateTransform(startPoint.X, startPoint.Y);
      g.ScaleTransform(size / OkImage.Width, size / OkImage.Height);
      DrawImage(g, OkImage, true);
      g.ResetTransform();

      g.TranslateTransform(endPoint.X, endPoint.Y);
      g.ScaleTransform(size / XMarkImage.Width, size / XMarkImage.Height);
      DrawImage(g, XMarkImage, true);
      g.ResetTransform();

      // draw arrow
      size *= 0.8f;
      int idx = (int)(pass.Track.Count * (1f / 2));
      DrawArrow(g, pass, idx, size);

      // sat
      var now = DateTime.UtcNow;
      if (pass.StartTime < now && pass.EndTime > now) DrawSat(g, pass);
    }




    //----------------------------------------------------------------------------------------------
    //                                   helper functions
    //----------------------------------------------------------------------------------------------
    private void DrawSat(Graphics g, SatellitePass pass)
    {
      var now = DateTime.UtcNow;
      var obs = pass.GetObservationAt(now);
      if (obs == null) return;

      var location = ObservationToXY(obs);
      float angle = ComputeDirection(pass, now);
      float scale = Math.Min(1, 0.5f + 0.0013f * Radius);

      g.TranslateTransform(location.X, location.Y);
      g.RotateTransform(angle + 45);
      g.ScaleTransform(scale, scale);
      DrawImage(g, SatImage);
      g.ResetTransform();

      string text = pass.Satellite.name;
      var font = (pass.Satellite == ctx.SatelliteSelector.SelectedSatellite) ? BoldFont : RegularFont;
      var size = g.MeasureString(text, font);
      var offset = SatImage.Height / 3f * scale;
      var rect = new RectangleF(location.X + offset, location.Y - size.Height - offset, size.Width, size.Height);
      if (rect.Right > DrawPanel.Width) rect.X -= 2 * offset + size.Width;
      g.DrawString(text, font, Brushes.Black, rect);
      SatLabelRects[rect] = pass;
    }

    private void DrawImage(Graphics g, Image image, bool whiteBg = false)
    {
      var size = image.Size;
      var rect = new RectangleF(-size.Width / 2, -size.Height / 2, size.Width, size.Height);
      if (whiteBg) g.FillEllipse(Brushes.White, rect); // give the transpatent symbol inside the circle a white bg
      g.DrawImage(image, rect);
    }

    private float ComputeDirection(SatellitePass pass, DateTime utc)
    {
      if (pass.Geostationary) return 0;

      var obs1 = pass.GetObservationAt(utc.AddSeconds(-10));
      if (obs1 == null) return 0;

      var obs2 = pass.GetObservationAt(utc.AddSeconds(10));
      if (obs2 == null) return 0;

      var p1 = ObservationToXY(obs1);
      var p2 = ObservationToXY(obs2);
      return (float)(Math.Atan2(p2.Y - p1.Y, p2.X - p1.X) * 180 / Math.PI);
    }

    private void DrawArrow(Graphics g, SatellitePass pass, int idx, float size)
    {
      var arrowPoint = TrackPointToXY(pass.Track[idx]);
      var p1 = TrackPointToXY(pass.Track[idx - 1]);
      var p2 = TrackPointToXY(pass.Track[idx + 1]);
      float angle = (float)(Math.Atan2(p2.Y - p1.Y, p2.X - p1.X) * 180 / Math.PI);

      g.TranslateTransform(arrowPoint.X, arrowPoint.Y);
      g.RotateTransform(angle);
      g.ScaleTransform(size / ArrowImage.Width, size / ArrowImage.Height);
      DrawImage(g, ArrowImage);
      g.ResetTransform();
    }

    private PointF TrackPointToXY(SatellitePass.TrackPoint point)
    {
      return ObservationToXY(point.Observation);
    }

    private PointF ObservationToXY(TopocentricObservation obs)
    {
      return AzElToXY(obs.Azimuth.Radians, obs.Elevation.Radians);
    }

    private PointF AzElToXY(double azimuth, double elevation)
    {
      double ro = 1 - elevation / Utils.HalfPi;
      double phi = Utils.HalfPi - azimuth;

      return new PointF(
        Center.X + (float)(Radius * ro * Math.Cos(phi)),
        Center.Y - (float)(Radius * ro * Math.Sin(phi))
        );
    }

    private RectangleF GetSatRectAt(PointF point)
    {
      return SatLabelRects.Keys.FirstOrDefault(rect => rect.Contains(point));
    }




    // currently not used
    //https://stackoverflow.com/questions/3519835
    PathGradientBrush? GradientBrush;
    private PointF Center;
    private float Radius;

    private PathGradientBrush GetRadialBrush(RectangleF bounds)
    {
      if (GradientBrush != null) return GradientBrush;

      using (var ellipsePath = new GraphicsPath())
      {
        ellipsePath.AddEllipse(bounds);
        GradientBrush = new PathGradientBrush(ellipsePath);
        GradientBrush.CenterPoint = new PointF(bounds.Width / 2f, bounds.Height / 2f);
        GradientBrush.CenterColor = Color.Black;
        GradientBrush.SurroundColors = [Color.SkyBlue];
        GradientBrush.FocusScales = new PointF(0, 0);
      }
      return GradientBrush;
    }




    //----------------------------------------------------------------------------------------------
    //                                        mouse
    //----------------------------------------------------------------------------------------------
    Point MouseMovePos;

    private void DrawPanel_MouseMove(object sender, MouseEventArgs e)
    {
      if (e.Location == MouseMovePos) return;
      MouseMovePos = e.Location;

      var rect = GetSatRectAt(new PointF(e.X, e.Y));
      if (!rect.IsEmpty)
      {
        Cursor = Cursors.Hand;
        var pass = SatLabelRects[rect];

        string tooltip = pass.Satellite.GetTooltipText() + "\n\n" + string.Join("\n", pass.GetTooltipText(false));
        if (tooltip != toolTip1.GetToolTip(DrawPanel))
        {
          toolTip1.ToolTipTitle = pass.Satellite.name;
          toolTip1.Show(tooltip, DrawPanel, (int)rect.Right, (int)rect.Bottom);
        }
      }
      else
      {
        Cursor = Cursors.Default;
        toolTip1.Hide(this);
      }
    }

    private void DrawPanel_MouseDown(object sender, MouseEventArgs e)
    {
      var rect = GetSatRectAt(new PointF(e.X, e.Y));
      if (!rect.IsEmpty && e.Button == MouseButtons.Left)
        {
          var sat = SatLabelRects[rect].Satellite;
        ctx.SatelliteSelector.SetSelectedSatellite(sat);
      }
    }

    private void DrawPanel_MouseLeave(object sender, EventArgs e)
    {
      toolTip1.Hide(this);
    }




    //----------------------------------------------------------------------------------------------
    //                                        labels
    //----------------------------------------------------------------------------------------------
    private void SetLabels()
    {
      FlowPanel.SuspendLayout();

      if (RealTimeRadioBtn.Checked) ShowSatLabels();
      else if (Pass != null) ShowPassLabels();
      else FlowPanel.Controls.Clear();

      FlowPanel.ResumeLayout();
    }

    private void ShowPassLabels()
    {
      EnsureLabels(ORBIT_LABEL_COUNT);
      var tooltip = Pass.GetTooltipText();

      FlowPanel.Controls[0].Text = tooltip[0];
      FlowPanel.Controls[1].Text = $"{tooltip[1]}   {tooltip[2]}";
      FlowPanel.Controls[2].Text = tooltip[3];
      FlowPanel.Controls[3].Text = tooltip[4];
    }

    private void ShowSatLabels()
    {
      var now = DateTime.UtcNow;
      var passes = ctx.GroupPasses.GetPassesSnapshot().Where(p => now > p.StartTime && now < p.EndTime).ToArray();

      EnsureLabels(passes.Length * 3);

      // set label text
      for (int i = 0; i < passes.Length; i++)
      {
        var tooltip = passes[i].GetTooltipText();
        bool selected = passes[i].Satellite == ctx.SatelliteSelector.SelectedSatellite;
        var observation = passes[i].GetObservationAt(now);
        if (observation == null) continue;

        FlowPanel.Controls[i * 3].Font = selected ? BoldFont : RegularFont;
        FlowPanel.Controls[i * 3].Text = passes[i].Satellite.name;
        FlowPanel.Controls[i * 3 + 1].Text = tooltip[0];
        FlowPanel.Controls[i * 3 + 2].Text = $"Az. {observation.Azimuth.Degrees:F1}°  El. {observation.Elevation.Degrees:F1}°";
      }
    }

    private const int ORBIT_LABEL_COUNT = 4;
    private void EnsureLabels(int count)
    {
      int currentCount = FlowPanel.Controls.Count;
      if (currentCount == count) return;

      // delete if more than needed
      for (int i = currentCount - 1; i >= count; i--)
        FlowPanel.Controls.Remove(FlowPanel.Controls[i]);

      // add if less than needed
      for (int i = currentCount; i < count; i++)
        FlowPanel.Controls.Add(new Label() { AutoSize = true });

      // regular font
      foreach (Label label in FlowPanel.Controls)
        label.Font = RegularFont;

      // 3 labels per line if sat labels
      bool breaksNeeded = count != ORBIT_LABEL_COUNT;
      for (int i = 2; i < count; i += 3)
        FlowPanel.SetFlowBreak(FlowPanel.Controls[i], breaksNeeded && i % 3 == 2);
    }
  }
}
