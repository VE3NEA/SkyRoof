using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace SkyRoof
{
  /// <summary>
  /// Downloads and installs the FM speech-to-text model pack (integration plan C1): the int8 sherpa
  /// GigaSpeech zipformer, hosted as a single flat .zip on the SkyRoof repo's dedicated <c>asr-models</c>
  /// GitHub Release. It is our own single versioned pack, so the expected SHA-256 is baked in (no manifest);
  /// a mismatch aborts the install. Files extract flat into the destination
  /// (<see cref="TelemetryPanel.FmModelDir"/> = <c>&lt;DataFolder&gt;\ASR_models</c>).
  /// </summary>
  public static class FmModelDownloader
  {
    public const string Url =
      "https://github.com/VE3NEA/SkyRoof/releases/download/asr-models/asr-sherpa-gigaspeech-int8.zip";
    public const string Sha256 = "01EF2CEFB77F18565831B76ACD35ED93870F1492982B22F1959E6DCC0C3CED2D";
    // approximate download size, for the progress bar before the server reports Content-Length
    public const long ApproxBytes = 56_753_742;
    // the pretty size shown in the download prompt ("~54 Mb")
    public const int ApproxMb = 54;

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(15) };

    /// <summary>Download the pack to a temp file, verify its SHA-256, and extract it into
    /// <paramref name="destDir"/> (created if needed), reporting 0..100 percent. Throws on network error,
    /// integrity failure, or cancellation.</summary>
    public static async Task DownloadAndInstallAsync(string destDir, IProgress<int>? progress,
      CancellationToken ct)
    {
      Directory.CreateDirectory(destDir);
      string tmp = Path.Combine(Path.GetTempPath(), $"asr-sherpa-int8-{Guid.NewGuid():N}.zip");
      try
      {
        using (var resp = await Http.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead, ct))
        {
          resp.EnsureSuccessStatusCode();
          long total = resp.Content.Headers.ContentLength ?? ApproxBytes;
          using var src = await resp.Content.ReadAsStreamAsync(ct);
          using var dst = File.Create(tmp);
          var buf = new byte[81920];
          long done = 0;
          int read;
          int lastPercent = -1;
          while ((read = await src.ReadAsync(buf, ct)) > 0)
          {
            await dst.WriteAsync(buf.AsMemory(0, read), ct);
            done += read;
            int percent = (int)Math.Min(100, done * 100 / Math.Max(1, total));
            if (percent != lastPercent) { progress?.Report(percent); lastPercent = percent; }
          }
        }

        // integrity: our own pack, so the expected hash is fixed and baked in
        string hash;
        using (var fs = File.OpenRead(tmp))
          hash = Convert.ToHexString(SHA256.HashData(fs));
        if (!hash.Equals(Sha256, StringComparison.OrdinalIgnoreCase))
          throw new InvalidDataException("The downloaded FM speech model failed its integrity check.");

        ZipFile.ExtractToDirectory(tmp, destDir, overwriteFiles: true);
      }
      finally
      {
        try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
      }
    }
  }
}
