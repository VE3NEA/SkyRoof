using System.Drawing.Drawing2D;
using System.Text;
using SGPdotNET.Observation;
using VE3NEA;

namespace SkyRoof
{
  public partial class QsoScheduleForm : Form
  {
    private readonly QsoSchedulerPanel.OverlapPass overlap = null!;
    private readonly Image OkImage = null!;
    private readonly Image XMarkImage = null!;
    private readonly Brush SkyBrush = new SolidBrush(Color.FromArgb(230, 249, 255));
    private readonly Pen FullPathPen = new Pen(Color.Silver, 2);
    private readonly Pen CommonPathPen = new Pen(Color.Green, 5);
    private readonly Font TitleFont;

    public QsoScheduleForm()
    {
      InitializeComponent();
    }

    internal QsoScheduleForm(QsoSchedulerPanel.OverlapPass overlap) : this()
    {
      this.overlap = overlap;

      using (var ms = new MemoryStream(Properties.Resources.ok)) { OkImage = Image.FromStream(ms); }
      using (var ms = new MemoryStream(Properties.Resources.xmark)) { XMarkImage = Image.FromStream(ms); }

      TitleFont = new Font(Font, FontStyle.Bold);

      Utils.SetDoubleBuffered(ChartsPanel, true);

      Text = $"QSO Schedule  -  {overlap.Satellite.name}  #{overlap.OrbitNumber}";
      SetupDetails();
    }




    //----------------------------------------------------------------------------------------------
    //                                       details text
    //----------------------------------------------------------------------------------------------
    private void SetupDetails()
    {
      var sb = new StringBuilder();
      sb.AppendLine($"Satellite:  {overlap.Satellite.name}     Orbit:  #{overlap.OrbitNumber}" +
        (overlap.Geostationary ? "   (Geostationary)" : ""));

      var localStart = overlap.CommonStart.ToLocalTime();
      var localEnd = overlap.CommonEnd.ToLocalTime();
      sb.AppendLine($"Common segment (local):   {localStart:yyyy-MM-dd  HH:mm:ss}  to  {localEnd:HH:mm:ss}");
      sb.AppendLine($"Common segment (UTC):     {overlap.CommonStart:yyyy-MM-dd  HH:mm:ss}  to  {overlap.CommonEnd:HH:mm:ss}");

      var duration = overlap.CommonEnd - overlap.CommonStart;
      sb.AppendLine($"Duration:  {Utils.TimespanToString(duration, false)}");

      var now = DateTime.UtcNow;
      string wait;
      if (overlap.CommonStart > now) wait = $"Starts in  {Utils.TimespanToString(overlap.CommonStart - now)}";
      else if (overlap.CommonEnd > now) wait = $"In progress, ends in  {Utils.TimespanToString(overlap.CommonEnd - now)}";
      else wait = "Ended.";
      sb.AppendLine(wait);

      sb.Append($"Max elevation -  Me ({overlap.MySquare}):  {overlap.MyMaxElevation:F0}°     " +
        $"DX ({overlap.DxSquare}):  {overlap.DxMaxElevation:F0}°");

      DetailsLabel.Text = sb.ToString();
    }




    //----------------------------------------------------------------------------------------------
    //                                          paint
    //----------------------------------------------------------------------------------------------
    private void ChartsPanel_Resize(object sender, EventArgs e)
    {
      ChartsPanel.Invalidate();
    }

    private void ChartsPanel_Paint(object sender, PaintEventArgs e)
    {
      var g = e.Graphics;
      g.SmoothingMode = SmoothingMode.AntiAlias;
      g.Clear(Color.White);

      var bounds = ChartsPanel.ClientRectangle;
      int half = bounds.Width / 2;
      var leftRect = new Rectangle(bounds.X, bounds.Y, half, bounds.Height);
      var rightRect = new Rectangle(bounds.X + half, bounds.Y, bounds.Width - half, bounds.Height);

      DrawChart(g, leftRect, overlap.MySquare, overlap.MyPass);
      DrawChart(g, rightRect, overlap.DxSquare, overlap.DxPass);
    }

    private void DrawChart(Graphics g, Rectangle area, string label, SatellitePass pass)
    {
      // grid-square label above the chart
      var labelSize = g.MeasureString(label, TitleFont);
      g.DrawString(label, TitleFont, Brushes.Black,
        area.X + (area.Width - labelSize.Width) / 2, area.Y + 6);

      int top = (int)labelSize.Height + 14;
      var chartArea = new Rectangle(area.X, area.Y + top, area.Width, area.Height - top);

      DrawBackground(g, chartArea, out var center, out var radius);
      DrawPath(g, pass, center, radius);
    }

    private void DrawBackground(Graphics g, Rectangle area, out PointF center, out float radius)
    {
      var sizeN = g.MeasureString("N", Font);
      var sizeS = g.MeasureString("S", Font);
      var sizeE = g.MeasureString("E", Font);
      var sizeW = g.MeasureString("W", Font);

      center = new PointF(area.X + area.Width / 2f, area.Y + area.Height / 2f);
      float radiusX = area.Width / 2f - Math.Max(sizeE.Width, sizeW.Width) - 4;
      float radiusY = area.Height / 2f - Math.Max(sizeN.Height, sizeS.Height) - 4;
      radius = Math.Min(radiusX, radiusY);
      if (radius < 10) return;

      // background disc - light teal sky
      var rect = new RectangleF(center.X - radius, center.Y - radius, 2 * radius, 2 * radius);
      g.FillEllipse(SkyBrush, rect);

      // elevation rings at 30/60 deg
      for (float r = radius / 3; r <= 1.1 * radius; r += radius / 3)
      {
        var r2 = new RectangleF(center.X - r, center.Y - r, 2 * r, 2 * r);
        g.DrawEllipse(Pens.Teal, r2);
      }

      // cross-hairs
      g.DrawLine(Pens.Teal, center.X - radius, center.Y, center.X + radius, center.Y);
      g.DrawLine(Pens.Teal, center.X, center.Y - radius, center.X, center.Y + radius);

      // compass labels
      g.DrawString("N", Font, Brushes.Teal, center.X - sizeN.Width / 2, center.Y - radius - sizeN.Height - 2);
      g.DrawString("S", Font, Brushes.Teal, center.X - sizeS.Width / 2, center.Y + radius + 2);
      g.DrawString("E", Font, Brushes.Teal, center.X + radius + 2, center.Y - sizeE.Height / 2);
      g.DrawString("W", Font, Brushes.Teal, center.X - radius - sizeW.Width - 2, center.Y - sizeW.Height / 2);
    }

    private void DrawPath(Graphics g, SatellitePass pass, PointF center, float radius)
    {
      if (radius < 10) return;

      // full pass arc in silver
      var fullPoints = pass.Track
        .Where(t => t.Observation != null)
        .Select(t => AzElToXY(center, radius, t.Observation!.Azimuth.Radians, t.Observation.Elevation.Radians))
        .ToArray();
      if (fullPoints.Length < 2) return;

      g.DrawLines(FullPathPen, fullPoints);

      // common segment in green, finely sampled so the endpoints land exactly at common start/end
      const int CommonSteps = 30;
      var commonDur = overlap.CommonEnd - overlap.CommonStart;
      var commonPoints = new List<PointF>();
      if (commonDur > TimeSpan.Zero)
      {
        var step = commonDur / CommonSteps;
        for (int i = 0; i <= CommonSteps; i++)
        {
          var utc = overlap.CommonStart + step * i;
          var obs = pass.GetObservationAt(utc);
          if (obs == null) continue;
          commonPoints.Add(AzElToXY(center, radius, obs.Azimuth.Radians, obs.Elevation.Radians));
        }
        if (commonPoints.Count >= 2)
          g.DrawLines(CommonPathPen, commonPoints.ToArray());
      }

      // AOS/LOS icons at the ends of the WHOLE pass
      float iconSize = Math.Min(20, 0.05f * radius + 8);

      DrawIcon(g, OkImage, fullPoints.First(), iconSize);
      DrawIcon(g, XMarkImage, fullPoints.Last(), iconSize);
    }

    private static PointF AzElToXY(PointF center, float radius, double azimuth, double elevation)
    {
      double ro = 1 - elevation / Utils.HalfPi;
      double phi = Utils.HalfPi - azimuth;
      return new PointF(
        center.X + (float)(radius * ro * Math.Cos(phi)),
        center.Y - (float)(radius * ro * Math.Sin(phi)));
    }

    private static void DrawIcon(Graphics g, Image image, PointF location, float size)
    {
      g.TranslateTransform(location.X, location.Y);
      g.ScaleTransform(size / image.Width, size / image.Height);
      var rect = new RectangleF(-image.Width / 2f, -image.Height / 2f, image.Width, image.Height);
      // white background inside the circle so transparent icons stay visible
      g.FillEllipse(Brushes.White, rect);
      g.DrawImage(image, rect);
      g.ResetTransform();
    }
  }
}
