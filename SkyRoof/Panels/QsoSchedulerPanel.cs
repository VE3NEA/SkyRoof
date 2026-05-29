using System.Reflection.Emit;
using Serilog;
using SGPdotNET.Observation;
using SGPdotNET.CoordinateSystem;
using SGPdotNET.Util;
using VE3NEA;
using WeifenLuo.WinFormsUI.Docking;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace SkyRoof
{
  public partial class QsoSchedulerPanel : DockContent
  {
    internal class OverlapPass
    {
      public SatnogsDbSatellite Satellite;
      public SatellitePass MyPass;
      public SatellitePass DxPass;
      public string MySquare = "";
      public string DxSquare = "";
      public DateTime CommonStart;
      public DateTime CommonEnd;
      public double MyMaxElevation;
      public double DxMaxElevation;
      public PointF[] MyMiniPath = Array.Empty<PointF>();
      public PointF[] DxMiniPath = Array.Empty<PointF>();
      public bool Geostationary;
      public int OrbitNumber;
    }

    private readonly Context ctx = null!;
    private bool changing;
    private List<ListViewItem> Items = new();

    private readonly Font BoldFont;
    private readonly Pen PathPen = new Pen(Brushes.Teal, 2);

    public QsoSchedulerPanel()
    {
      InitializeComponent();
    }

    public QsoSchedulerPanel(Context ctx)
    {
      Log.Information("Creating QsoScheduler");
      this.ctx = ctx;
      InitializeComponent();

      ctx.QsoSchedulerPanel = this;
      ctx.MainForm.QsoSchedulerMNU.Checked = true;

      BoldFont = new Font(PredictionList.Font, FontStyle.Bold);

      int rowHeight = TextRenderer.MeasureText("0", Font, Size, TextFormatFlags.NoPadding).Height * 2 + 15;
      PredictionList.SetRowHeight(rowHeight);
      PredictionList.SetTooltipDelay(1500);

      SetSatelliteList();
    }

    private void QsoScheduler_FormClosing(object sender, FormClosingEventArgs e)
    {
      Log.Information("Closing QsoScheduler");
      ctx.QsoSchedulerPanel = null;
      ctx.MainForm.QsoSchedulerMNU.Checked = false;
    }

    private void DxSquareEdit_TextChanged(object sender, EventArgs e)
    {
      bool ok = GridSquare.IsValid(DxSquareEdit.Text.Trim());
      DxSquareEdit.BackColor = ok ? SystemColors.Window : Color.FromArgb(0xFF, 0xDD, 0xDD);

      UpdatePredictions();
    }

    private void SatelliteComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      UpdatePredictions();
    }

    private void PredictionList_Resize(object sender, EventArgs e)
    {
      PredictionList.Columns[0].Width = PredictionList.ClientSize.Width;
    }

    private void PredictionList_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
    {
      e.Item = Items[e.ItemIndex];
    }

    private void PredictionList_MouseDown(object sender, MouseEventArgs e)
    {
      if (e.Button != MouseButtons.Left) return;
      var item = PredictionList.GetItemAt(e.X, e.Y);
      if (item == null) return;

      using var popup = new QsoScheduleForm((OverlapPass)item.Tag!);
      popup.ShowDialog(this);
    }

    public void SetSatelliteList()
    {
      SatelliteComboBox.Items.Clear();
      SatelliteComboBox.Items.AddRange(ctx.SatelliteSelector.GroupSatellites);
      UpdatePredictions();
    }

    public void UpdatePredictions()
    {
      Items.Clear();
      PredictionList.VirtualListSize = 0;

      bool ok = GridSquare.IsValid(DxSquareEdit.Text.Trim()) && SatelliteComboBox.SelectedIndex >= 0;

      if (ok) ComputePredictions();

      PredictionList.Invalidate();
    }

    private void ComputePredictions()
    {
      var satellite = SatelliteComboBox.SelectedItem as SatnogsDbSatellite;
      if (satellite == null || satellite.Tracker == null || !satellite.Tracker.Enabled) return;

      var now = DateTime.UtcNow;
      var endTime = now + TimeSpan.FromDays(14);

      // ground stations: me uses configured altitude, DX at sea level
      var mySquare = ctx.Settings.User.Square;
      var dxSquare = DxSquareEdit.Text.Trim();
      var myStation = MakeStation(mySquare, ctx.Settings.User.Altitude);
      var dxStation = MakeStation(dxSquare, 0);

      // raw passes for both stations
      bool geo = satellite.Tracker.IsGeoStationary();
      List<SatelliteVisibilityPeriod> myVis, dxVis;
      if (geo)
      {
        myVis = satellite.Tracker.ComputeGeostationaryPasses(myStation);
        dxVis = satellite.Tracker.ComputeGeostationaryPasses(dxStation);
      }
      else
      {
        myVis = satellite.Tracker.ComputePasses(myStation, now, endTime);
        dxVis = satellite.Tracker.ComputePasses(dxStation, now, endTime);
      }

      var myPasses = myVis.Select(p => new SatellitePass(myStation, satellite, p)).ToList();
      var dxPasses = dxVis.Select(p => new SatellitePass(dxStation, satellite, p)).ToList();

      // build overlapping passes
      var minOverlap = TimeSpan.FromSeconds(30);
      var overlaps = new List<OverlapPass>();

      foreach (var my in myPasses)
        foreach (var dx in dxPasses)
        {
          var start = my.StartTime > dx.StartTime ? my.StartTime : dx.StartTime;
          var end = my.EndTime < dx.EndTime ? my.EndTime : dx.EndTime;
          if (end - start < minOverlap) continue;

          var overlap = new OverlapPass
          {
            Satellite = satellite,
            MyPass = my,
            DxPass = dx,
            MySquare = mySquare,
            DxSquare = dxSquare,
            CommonStart = start,
            CommonEnd = end,
            Geostationary = geo,
            OrbitNumber = my.OrbitNumber
          };
          overlap.MyMiniPath = ComputeMiniPath(my, start, end);
          overlap.DxMiniPath = ComputeMiniPath(dx, start, end);
          overlap.MyMaxElevation = MaxElevationInRange(my, start, end);
          overlap.DxMaxElevation = MaxElevationInRange(dx, start, end);

          overlaps.Add(overlap);
        }

      Items = overlaps
        .OrderBy(o => o.CommonStart)
        .Select(o => new ListViewItem { Tag = o })
        .ToList();

      PredictionList.VirtualListSize = Items.Count;
    }

    private static GroundStation MakeStation(string square, double altitudeMeters)
    {
      var pos = GridSquare.ToGeoPoint(square);
      var location = new GeodeticCoordinate(
        Angle.FromRadians(pos.LatitudeRad),
        Angle.FromRadians(pos.LongitudeRad),
        altitudeMeters / 1000d);
      return new GroundStation(location);
    }

    private const int MiniPathSteps = 10;
    private static PointF[] ComputeMiniPath(SatellitePass pass, DateTime start, DateTime end)
    {
      var duration = end - start;
      if (duration <= TimeSpan.Zero) return Array.Empty<PointF>();
      var step = duration / MiniPathSteps;
      var path = new PointF[MiniPathSteps + 1];

      for (int i = 0; i <= MiniPathSteps; i++)
      {
        var utc = start + step * i;
        var observation = pass.GetObservationAt(utc);
        if (observation == null) continue;
        double ro = 1 - observation.Elevation.Radians / Utils.HalfPi;
        double phi = Utils.HalfPi - observation.Azimuth.Radians;
        path[i] = new PointF((float)(ro * Math.Cos(phi)), (float)(ro * Math.Sin(phi)));
      }
      return path;
    }

    private static double MaxElevationInRange(SatellitePass pass, DateTime start, DateTime end)
    {
      if (pass.Geostationary) return pass.MaxElevation;

      const int Steps = 30;
      var duration = end - start;
      if (duration <= TimeSpan.Zero) return 0;
      var step = duration / Steps;
      double max = 0;

      for (int i = 0; i <= Steps; i++)
      {
        var utc = start + step * i;
        var observation = pass.GetObservationAt(utc);
        if (observation == null) continue;
        double el = observation.Elevation.Degrees;
        if (el > max) max = el;
      }
      return max;
    }



    //----------------------------------------------------------------------------------------------
    //                                        draw
    //----------------------------------------------------------------------------------------------
    private void PredictionList_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
    {
      e.DrawBackground();

      var overlap = (OverlapPass)e.Item.Tag!;
      int h = e.Bounds.Height - 1;
      int w = e.Bounds.Width - 2 * h;
      bool now = overlap.CommonStart < DateTime.UtcNow;

      // sat name
      string text = overlap.Satellite.name;
      var size = e.Graphics.MeasureString(text, BoldFont);
      var rect = new RectangleF(e.Bounds.X, e.Bounds.Y, size.Width, size.Height);
      var brush = Brushes.White;
      if (overlap.Satellite.Flags.HasFlag(SatelliteFlags.Uhf)) brush = Brushes.LightCyan;
      else if (overlap.Satellite.Flags.HasFlag(SatelliteFlags.Vhf)) brush = Brushes.LightGoldenrodYellow;
      e.Graphics.FillRectangle(brush, rect);
      e.Graphics.DrawString(text, BoldFont, Brushes.Black, rect);

      // orbit number
      text = $"#{overlap.OrbitNumber}";
      size = e.Graphics.MeasureString(text, PredictionList.Font);
      rect = new RectangleF(rect.Right + 10, rect.Y, size.Width, size.Height);
      e.Graphics.DrawString(text, PredictionList.Font, Brushes.Black, rect);

      // start/end time for the common segment
      text = overlap.Geostationary ?
        "Geostationary" : $"{overlap.CommonStart.ToLocalTime():yyyy-MM-dd  HH:mm:ss}  to  {overlap.CommonEnd.ToLocalTime():HH:mm:ss}";
      size = e.Graphics.MeasureString(text, PredictionList.Font);
      rect = new RectangleF(e.Bounds.X, e.Bounds.Y + e.Bounds.Height - size.Height - 2, size.Width, size.Height);
      e.Graphics.DrawString(text, PredictionList.Font, Brushes.Black, rect);

      // duration of common segment + max elevation per station
      string elevText = $"Me: {overlap.MyMaxElevation:F0}°, DX: {overlap.DxMaxElevation:F0}°";
      text = overlap.Geostationary ?
        elevText : $"{Utils.TimespanToString(overlap.CommonEnd - overlap.CommonStart, false)}   {elevText}";
      size = e.Graphics.MeasureString(text, PredictionList.Font);
      rect = new RectangleF(e.Bounds.X + w - size.Width, e.Bounds.Y + e.Bounds.Height - size.Height - 2, size.Width, size.Height);
      e.Graphics.DrawString(text, PredictionList.Font, Brushes.Black, rect);

      // wait time to start of common segment
      text = now ? "Now" : $"in {Utils.TimespanToString(overlap.CommonStart - DateTime.UtcNow)}";
      brush = now ? Brushes.Green : Brushes.Black;
      size = e.Graphics.MeasureString(text, BoldFont);
      rect = new RectangleF(e.Bounds.X + w - size.Width, e.Bounds.Y, size.Width, size.Height);
      e.Graphics.DrawString(text, BoldFont, brush, rect);

      // two mini sky-views, side by side: me on left, dx on right
      DrawMiniSkyView(e.Graphics, new RectangleF(e.Bounds.X + w, e.Bounds.Y, h, h), overlap.MyMiniPath);
      DrawMiniSkyView(e.Graphics, new RectangleF(e.Bounds.X + w + h, e.Bounds.Y, h, h), overlap.DxMiniPath);

      // item separator
      var sep = new RectangleF(e.Bounds.X, e.Bounds.Y + h, e.Bounds.Width, 1);
      e.Graphics.FillRectangle(Brushes.Gray, sep);
    }

    private void DrawMiniSkyView(Graphics g, RectangleF rect, PointF[] path)
    {
      rect.Inflate(-5, -5);
      var radius = rect.Width / 2;
      var center = new PointF(rect.Left + radius, rect.Top + radius);

      // grid: horizon, 45° ring, cross hairs
      g.DrawEllipse(Pens.Silver, rect);
      g.DrawLine(Pens.Silver, rect.Left, center.Y, rect.Right, center.Y);
      g.DrawLine(Pens.Silver, center.X, rect.Top, center.X, rect.Bottom);
      var inner = rect;
      inner.Inflate(-radius / 2, -radius / 2);
      g.DrawEllipse(Pens.Silver, inner);

      if (path != null && path.Length > 1)
      {
        var points = path.Select(p =>
          new PointF(center.X + p.X * radius, center.Y - p.Y * radius)).ToArray();
        g.DrawLines(PathPen, points);

        // green start, red end
        g.FillEllipse(Brushes.Green, points.First().X - 3, points.First().Y - 3, 6, 6);
        g.FillEllipse(Brushes.Red, points.Last().X - 3, points.Last().Y - 3, 6, 6);
      }
    }
  }
}
