using System.Text;
using SharpGL.VertexBuffers;
using SharpGL;
using WeifenLuo.WinFormsUI.Docking;
using SharpGL.Shaders;
using VE3NEA;
using System.Diagnostics;
using System.Drawing.Imaging;
using Serilog;
using static System.Windows.Forms.AxHost;

namespace SkyRoof
{
  public partial class EarthViewPanel : DockContent
  {
    private readonly Context ctx;
    private OpenGL gl;
    private ShaderProgram ShaderProgram;
    private VertexBufferArray VertexBufferArray;
    private uint[] TextureIds = new uint[1];
    private int VertexCount;
    private SpriteRenderer SpriteRenderer;
    private Sprite SatelliteSprite, HomeSprite, NorthSprite, SouthSprite;
    private PolylineRenderer PolylineRenderer;
    private double DimmingRadius;
    private double SatFootprint;
    private double Azimuth;

    public GeoPoint Home, Center, SatGeoPoint;
    public double Zoom = 1;
    public float Brightness = Theme.EarthMapBrightness; // the fragment shader scales the map by this
    public SatnogsDbSatellite Satellite;

    public enum EarthViewMode { RealTime, Pass }
    private EarthViewMode Mode = EarthViewMode.RealTime;
    private SatellitePass? Pass;
    private List<GeoPoint>? CoveragePolygon;
    private readonly Color PolygonColor = Color.FromArgb(230, 0, 110, 255);


    public EarthViewPanel() { InitializeComponent(); }

    public EarthViewPanel(Context ctx)
    {
      this.ctx = ctx;
      Log.Information("Creating EarthViewPanel");
      InitializeComponent();

      ctx.EarthViewPanel = this;
      ctx.MainForm.EarthViewMNU.Checked = true;

      SetGridSquare();
      SetSatellite();

      openglControl1.MouseWheel += OpenglControl1_MouseWheel;
    }

    public void SetGridSquare()
    {
      Home = GridSquare.ToGeoPoint(ctx.Settings.User.Square);
    }

    private void EarthViewPanel_FormClosing(object sender, FormClosingEventArgs e)
    {
      Log.Information("Closing EarthViewPanel");
      ctx.EarthViewPanel = null;
      ctx.MainForm.EarthViewMNU.Checked = false;
    }

    private void openglControl1_Resize(object sender, EventArgs e)
    {
      openglControl1.Invalidate();
      ValidateZoom();
    }

    private void OpenglControl1_MouseWheel(object? sender, MouseEventArgs e)
    {
      Zoom *= 1 + e.Delta / 1200f;
      ValidateZoom();
    }

    private void ValidateZoom()
    {
      var diam = Math.Min(openglControl1.ClientSize.Width, openglControl1.ClientSize.Height);
      if (diam > 0) Zoom = Math.Max(1f, Math.Min(25000 / diam, Zoom));
    }

    internal void SetSatellite(SatnogsDbSatellite? satellite = null)
    {
      Satellite = satellite ?? ctx.SatelliteSelector.SelectedSatellite;
      label1.Text = Satellite.name;

      ComputeSatLocation();
      Zoom = 0.9 * Math.PI / SatFootprint;
      openglControl1.Invalidate();
    }
    public void Advance()
    {
      if (Satellite == null) return;

      // on AOS, switch from pass mode to real time
      if (Mode == EarthViewMode.Pass && Pass != null && Pass.IsActive())
        RealTimeRadioBtn.Checked = true;

      ComputeSatLocation();
      openglControl1.Invalidate();
    }

    internal void SetPass(SatellitePass? pass)
    {
      Pass = pass;
      CoveragePolygon = pass?.GetCoveragePolygon();

      PassRadioBtn.Enabled = pass != null;
      PassRadioBtn.Text = pass == null ? "Selected Pass" : $"Orbit #{pass.OrbitNumber} of {pass.Satellite.name}";

      // real time mode if the pass is currently active, otherwise pass mode for the upcoming pass
      if (pass != null && !pass.IsActive())
        PassRadioBtn.Checked = true;
      else
        RealTimeRadioBtn.Checked = true;

      ComputeSatLocation();

      // zoom so the whole coverage polygon fits the rectangular panel
      ZoomToFitPolygon();

      openglControl1.Invalidate();
    }

    // set the zoom so that every polygon vertex fits within the rectangular panel.
    // the projection scales linearly with Zoom, so we project at Zoom = 1 and fit the
    // x and y extents independently against the [-1, 1] clip space.
    private void ZoomToFitPolygon()
    {
      if (CoveragePolygon == null || CoveragePolygon.Count == 0) return;

      var size = openglControl1.ClientSize;
      if (size.Width == 0 || size.Height == 0) return;
      double diam = Math.Min(size.Width, size.Height);

      double maxX = 0, maxY = 0;
      foreach (var g in CoveragePolygon)
      {
        var path = g - Center;
        double ro = path.DistanceRad / Math.PI;
        double phi = Geo.HalfPi - path.AzimuthRad;
        double x = Math.Abs(ro * Math.Cos(phi) * diam / size.Width);
        double y = Math.Abs(ro * Math.Sin(phi) * diam / size.Height);
        if (x > maxX) maxX = x;
        if (y > maxY) maxY = y;
      }

      double maxXY = Math.Max(maxX, maxY);
      if (maxXY > 1e-6) Zoom = 0.9 / maxXY;
    }

    private void radioButton_CheckedChanged(object sender, EventArgs e)
    {
      var radioBtn = (RadioButton)sender;
      if (!radioBtn.Checked) return;

      Mode = PassRadioBtn.Checked ? EarthViewMode.Pass : EarthViewMode.RealTime;

      ComputeSatLocation();
      openglControl1.Invalidate();
    }

    private void ComputeSatLocation()
    {
      var p = Satellite.Tracker.Predict();
      var nextP = Satellite.Tracker.Predict(DateTime.UtcNow.AddSeconds(10));

      if (p == null || nextP == null)
      {
        SatGeoPoint = Home;
        SatFootprint = Math.PI;
        Azimuth = 0;
        SatelliteSprite.Enabled = false;
      }
      else
      {
        SatGeoPoint = new(p.Latitude.Degrees, p.Longitude.Degrees);
        var nextCenter = new GeoPoint(nextP.Latitude.Degrees, nextP.Longitude.Degrees);
        SatFootprint = p.GetFootprintAngle().Radians;
        Azimuth = (nextCenter - SatGeoPoint).AzimuthRad;
        SatelliteSprite.Enabled = true;
      }

      if (Mode == EarthViewMode.Pass)
      {
        // center on the user, do not show the satellite footprint circle or the satellite
        Center = Home;
        DimmingRadius = Math.PI;
        SatelliteSprite.Enabled = false;
      }
      else
      {
        // center on the satellite, show its footprint circle
        Center = (p == null) ? Home : SatGeoPoint;
        DimmingRadius = SatFootprint;
      }
    }
    




    //----------------------------------------------------------------------------------------------
    //                                 init opengl objects
    //----------------------------------------------------------------------------------------------
    private void openglControl1_OpenGLInitialized(object sender, EventArgs e)
    {
      gl = openglControl1.OpenGL;
      CheckError(false);

      SpriteRenderer = new(gl);
      CheckError(false);
      PolylineRenderer = new(gl);
      CheckError(false);
      SatelliteSprite = new Sprite(gl, Properties.Resources.satellite);
      HomeSprite = new Sprite(gl, Properties.Resources.ant);
      NorthSprite = new Sprite(gl, Properties.Resources.N);
      SouthSprite = new Sprite(gl, Properties.Resources.S);

      gl.Disable(OpenGL.GL_DEPTH_TEST);
      CheckError();

      LoadShaders();
      CreateVba();
      LoadTexture();

      gl.Uniform1(ShaderProgram.GetUniformLocation(gl, "worldMapTexture"), 0);
      CheckError();
    }


    private void LoadShaders()
    {
      ShaderProgram = new ShaderProgram();
      CheckError();

      ShaderProgram.Create(gl,
        Encoding.ASCII.GetString(Properties.Resources.EarthVertexShader),
        Encoding.ASCII.GetString(Properties.Resources.EarthFragmentShader),
        null);
      CheckError();

      ShaderProgram.AssertValid(gl);
      CheckError();

      ShaderProgram.Bind(gl);
      CheckError();
    }


    private const int PHI_STEPS = 192;
    private const int RO_STEPS = 96;
    private const float TwoPi = (float)(2 * Math.PI);

    private void CreateVba()
    {
      CheckError(false);

      VertexBufferArray = new VertexBufferArray();
      CheckError();
      VertexBufferArray.Create(gl);
      CheckError();
      VertexBufferArray.Bind(gl);
      CheckError();

      // generate a grid of polar coordinates
      VertexCount = RO_STEPS * (PHI_STEPS + 1) * 2;
      var data = new float[VertexCount * 2];
      int idx = 0;

      for (int ro = 0; ro < RO_STEPS; ro++)
        for (int phi = 0; phi <= PHI_STEPS; phi++)
        {
          data[idx++] = ro / (float)RO_STEPS;
          data[idx++] = phi * TwoPi / PHI_STEPS;
          data[idx++] = (ro + 1) / (float)RO_STEPS;
          data[idx++] = phi * TwoPi / PHI_STEPS;
        }

      var dataBuffer = new VertexBuffer();
      CheckError();
      dataBuffer.Create(gl);
      CheckError();
      dataBuffer.Bind(gl);
      CheckError();
      dataBuffer.SetData(gl, 0, data, false, 2);
      CheckError();

      VertexBufferArray.Unbind(gl);
      CheckError();
    }

    private void LoadTexture()
    {
      CheckError(false);

      gl.GenTextures(1, TextureIds);
      CheckError();

      gl.BindTexture(OpenGL.GL_TEXTURE_2D, TextureIds[0]);
      CheckError(false); // produces false positives

      gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_LINEAR);    // shrink
      gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_LINEAR);    // stretch
      gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_T, OpenGL.GL_CLAMP_TO_EDGE); // lat
      gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_S, OpenGL.GL_REPEAT);        // lon


      //using (Bitmap bitmap = new Bitmap(@"C:\Users\Alex\Desktop\sat\maps\worldMapTexture.bmp"))
      //using (Bitmap bitmap = new Bitmap(Properties.Resources.NaturalEarth))
      //using (Bitmap bitmap = new Bitmap(Properties.Resources.dxcc))
      using (Bitmap bitmap = Utils.BitmapFromBytes(Properties.Resources.dxcc))
      {
        BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
          ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

        gl.TexImage2D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_RGB, bitmap.Width, bitmap.Height, 0,
          OpenGL.GL_BGR, OpenGL.GL_UNSIGNED_BYTE, data.Scan0);

        bitmap.UnlockBits(data);
      }
      CheckError();
    }




    //----------------------------------------------------------------------------------------------
    //                                        draw
    //----------------------------------------------------------------------------------------------
    private void openglControl1_OpenGLDraw(object sender, SharpGL.RenderEventArgs args)
    {
      var gl = openglControl1.OpenGL;
      CheckError(false);

      ShaderProgram.Bind(gl);
      CheckError();

      gl.ActiveTexture(OpenGL.GL_TEXTURE0);
      CheckError();
      gl.BindTexture(OpenGL.GL_TEXTURE_2D, TextureIds[0]);
      CheckError();

      VertexBufferArray.Bind(gl);
      CheckError();

      ShaderProgram.SetUniform1(gl, "in_homeLat", (float)Center.LatitudeRad);
      CheckError();
      ShaderProgram.SetUniform1(gl, "in_homeLon", (float)Center.LongitudeRad);
      CheckError();
      ShaderProgram.SetUniform1(gl, "in_footprint", (float)DimmingRadius);
      CheckError();
      ShaderProgram.SetUniform1(gl, "in_aspect", openglControl1.ClientSize.Width / (float)openglControl1.ClientSize.Height);
      CheckError();
      ShaderProgram.SetUniform1(gl, "in_zoom", (float)Zoom);
      CheckError();
      ShaderProgram.SetUniform1(gl, "in_brightness", Brightness);
      CheckError();

      gl.ClearColor(Theme.EarthSpace.R / 255f, Theme.EarthSpace.G / 255f, Theme.EarthSpace.B / 255f, 1);
      CheckError();
      gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT);
      CheckError();
      gl.DrawArrays(OpenGL.GL_TRIANGLE_STRIP, 0, VertexCount);
      CheckError();

      VertexBufferArray.Unbind(gl);
      CheckError();
      ShaderProgram.Unbind(gl);
      CheckError();

      SetSpriteAttributes();
      SpriteRenderer.DrawSprites([SatelliteSprite, HomeSprite, NorthSprite, SouthSprite]);
      CheckError();

      DrawCoveragePolygon();
    }

    private void DrawCoveragePolygon()
    {
      if (CoveragePolygon == null || CoveragePolygon.Count < 2) return;

      // in real time mode, only show the polygon while the pass is in progress
      if (Mode == EarthViewMode.RealTime && (Pass == null || !Pass.IsActive())) return;

      // skip if any vertex is too close to the antipode, where the projection becomes singular
      foreach (var g in CoveragePolygon)
        if ((g - Center).DistanceRad > 0.9 * Math.PI) return;

      var points = new PointF[CoveragePolygon.Count];
      for (int i = 0; i < CoveragePolygon.Count; i++)
        points[i] = ComputeLocation(CoveragePolygon[i]);

      PolylineRenderer.DrawPolyline(points, PolygonColor, 3f);
      CheckError();
    }

    private void SetSpriteAttributes()
    {
      // control size, pixels
      var size = openglControl1.ClientSize;
      // diameter of the inscribed circle, pixels
      var diam = Math.Min(size.Width, size.Height);

      // scale sprites to look nice at all control sizes
      var scale = 0.6f + diam * 0.0007f;
      SatelliteSprite.Scale = scale;
      HomeSprite.Scale = scale;
      NorthSprite.Scale = 0.4f * scale;
      SouthSprite.Scale = 0.4f * scale;

      // satellite points in the direction of the next position
      SatelliteSprite.Angle = (float)(-Azimuth + Math.PI / 4);

      // satellite location (at the center in real-time mode, elsewhere in pass mode)
      SatelliteSprite.Location = ComputeLocation(SatGeoPoint);

      // home location in the azimuthal projection
      HomeSprite.Location = ComputeLocation(Home);

      // N and S at the poles
      NorthSprite.Location = ComputeLocation(GeoPoint.NorthPole);
      SouthSprite.Location = ComputeLocation(GeoPoint.SouthPole);
    }

    private PointF ComputeLocation(GeoPoint point)
    {
      var path = point - Center;
      var ro = path.DistanceRad / Math.PI * Zoom;
      var phi = Geo.HalfPi - path.AzimuthRad;
      var size = openglControl1.ClientSize;
      var diam = Math.Min(size.Width, size.Height);

      return new PointF(
        (float)(ro * Math.Cos(phi) * diam / size.Width),
        (float)(ro * Math.Sin(phi) * diam / size.Height));
    }



    //----------------------------------------------------------------------------------------------
    //                                   error handling
    //----------------------------------------------------------------------------------------------
    private void CheckError(bool log = true)
    {
      ExceptionLogger.CheckOpenglError(gl, log);
    }
  }
}
