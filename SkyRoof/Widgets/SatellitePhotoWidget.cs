using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Net.Http;
using SGPdotNET.Observation;
using VE3NEA;

namespace SkyRoof
{
  public partial class SatellitePhotoWidget : UserControl
  {
    public Context ctx;

    private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
    private CancellationTokenSource? Cts;
    private string? CurrentSatId;
    private string? Url;

    public static readonly Size CacheImageSize = new(160, 160);
    private const int CacheVersion = 2;

    public SatellitePhotoWidget()
    {
      InitializeComponent();
    }

    public void SetSatellite(SatnogsDbSatellite? sat)
    {
      Url = null;

      if (sat == null)
      {
        CurrentSatId = null;
        SetImage(null);
        return;
      }

      if (sat.sat_id == CurrentSatId) return;
      CurrentSatId = sat.sat_id;

      Tooltip.SetToolTip(Picture, sat.name);

      if (string.IsNullOrEmpty(sat.image))
      {
        SetImage(null);
        return;
      }

      Url = $"https://db-satnogs.freetls.fastly.net/media/{sat.image}";
      LoadOrDownloadAsync(sat, Url).DoNotAwait();
    }

    private async Task LoadOrDownloadAsync(SatnogsDbSatellite sat, string url)
    {
      var oldCts = Cts;
      Cts = new CancellationTokenSource();
      oldCts?.Cancel();
      oldCts?.Dispose();
      var ct = Cts.Token;

      try
      {
        string cacheFile = GetCacheFilePath(sat.sat_id);
        if (File.Exists(cacheFile))
        {
          ct.ThrowIfCancellationRequested();
          SetImage(LoadBitmapNoLock(cacheFile));
          return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)!);

        byte[] bytes = await Http.GetByteArrayAsync(url, ct);
        ct.ThrowIfCancellationRequested();

        using var ms = new MemoryStream(bytes);
        using var original = Image.FromStream(ms);
        using var resized = ResizeToFit(original, CacheImageSize);

        resized.Save(cacheFile, ImageFormat.Png);
        ct.ThrowIfCancellationRequested();

        SetImage(new Bitmap(resized));
      }
      catch (OperationCanceledException)
      {
        // ignore
      }
      catch
      {
        // download or decode failure; just show blank
        SetImage(null);
      }
    }

    private static string GetCacheFilePath(string satId)
    {
      string dir = Path.Combine(Utils.GetUserDataFolder(), "sat_images");
      string baseName = Utils.SanitizeFileNamePart(satId);
      string file = $"{baseName}_v{CacheVersion}_{CacheImageSize.Width}x{CacheImageSize.Height}.png";
      return Path.Combine(dir, file);
    }

    private static Bitmap LoadBitmapNoLock(string path)
    {
      using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      using var img = Image.FromStream(fs);
      return new Bitmap(img);
    }

    private static Bitmap ResizeToFit(Image src, Size target)
    {
      var dest = new Bitmap(target.Width, target.Height, PixelFormat.Format32bppPArgb);
      dest.SetResolution(96, 96);

      using var g = Graphics.FromImage(dest);
      g.Clear(Color.Transparent);
      g.CompositingQuality = CompositingQuality.HighQuality;
      g.InterpolationMode = InterpolationMode.HighQualityBicubic;
      g.PixelOffsetMode = PixelOffsetMode.HighQuality;
      g.SmoothingMode = SmoothingMode.HighQuality;

      float scale = Math.Min((float)target.Width / src.Width, (float)target.Height / src.Height);
      int w = Math.Max(1, (int)Math.Round(src.Width * scale));
      int h = Math.Max(1, (int)Math.Round(src.Height * scale));
      int x = (target.Width - w) / 2;
      int y = (target.Height - h) / 2;

      g.DrawImage(src, new Rectangle(x, y, w, h));
      return dest;
    }

    private void SetImage(Image? img)
    {
      if (IsDisposed) { img?.Dispose(); return; }

      if (InvokeRequired)
      {
        BeginInvoke(() => SetImage(img));
        return;
      }

      var old = Picture.Image;
      Picture.Image = img;
      old?.Dispose();
    }

    private void Picture_Click(object sender, EventArgs e)
    {
      if (Url != null)
        Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true });
    }
  }
}
