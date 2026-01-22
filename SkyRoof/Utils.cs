using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;

namespace VE3NEA
{
  internal static class Utils
  {
    public const double HalfPi = Math.PI / 2;

    internal static string GetAppName()
    {
      return Path.GetFileNameWithoutExtension(Application.ExecutablePath);
    }

    internal static string GetUserDataFolder()
    {
      string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
      path = Path.Combine(path, "Afreet", "Products", GetAppName());
      Directory.CreateDirectory(path);
      return path;
    }

    internal static string GetReferenceDataFolder()
    {
      string path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
      path = Path.Combine(path, "Afreet", GetAppName());
      Directory.CreateDirectory(path);
      return path;
    }

    internal static string SetCasing(string str, CharacterCasing casing)
    {
      switch (casing)
      {
        case CharacterCasing.Upper: return str.ToUpper();
        case CharacterCasing.Lower: return str.ToLower();
        default: return str;
      }
    }

    public static T? DeepClone<T>(T source)
    {
      if (source == null) return default;

      var json = JsonConvert.SerializeObject(source);
      return (T?)JsonConvert.DeserializeObject(json, typeof(T));
    }



    public static Regex CallsignRegex = new Regex(
    // portable prefix
    @"^((?:(?:[A-PR-Z](?:(?:[A-Z](?:\d[A-Z]?)?)|(?:\d[\dA-Z]?))?)|(?:[2-9][A-Z]{1,2}\d?))\/)?" +
    // prefix
    @"((?:(?:[A-PR-Z][A-Z]?)|(?:[2-9][A-Z]{1,2}))\d)" +
    // suffix
    @"(\d{0,3}[A-Z]{1,8})" +
    // modifier
    @"(\/[\dA-Z]{1,4})?$",
    RegexOptions.Compiled
    );

    public static Regex GridSquare6Regex = new Regex(@"^(?!RR73)[A-R]{2}\d{2}([A-X]{2})$", RegexOptions.Compiled);
    public static Regex GridSquare4Regex = new Regex(@"^[A-R]{2}\d{2}$", RegexOptions.Compiled);


    // from WsjtxUtils
    public static bool IsAddressMulticast(IPAddress address)
    {
      if (address.IsIPv6Multicast)
        return true;

      var addressBytes = address.GetAddressBytes();
      if (addressBytes.Length == 4)
        return addressBytes[0] >= 224 && addressBytes[0] <= 239;

      return false;
    }

    internal static string GetVersionNumber()
    {
      var version = typeof(Utils).Assembly.GetName().Version!;
     return $"{version.Major}.{version.Minor}";
    }

    internal static string GetVersionString()
    {
      return $"{Application.ProductName} {GetVersionNumber()}";
    }

    internal static string GetCopyrightString()
    {
      return typeof(Utils).Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
    }


    // avoid warning when not awaiting for an async task. Usage: DoSomethingAsync().DoNotAwait();
    // https://stackoverflow.com/questions/14903887
    public static void DoNotAwait(this Task task) { }

    public static string TimespanToString(TimeSpan timeSpan, bool showSeconds = true)
    {
      // round to nearest second
      //long seconds = (long)Math.Round(timeSpan.TotalSeconds);
      //timeSpan = TimeSpan.FromSeconds(seconds);
      timeSpan += TimeSpan.FromMilliseconds(100); // to avoid truncation issues

      string result = "";
      if (timeSpan >= TimeSpan.FromHours(1)) result += $"{(int)timeSpan.TotalHours:D}h ";
      string minutesLabel = showSeconds ? "m " : " min";
      if (timeSpan >= TimeSpan.FromMinutes(1) || !showSeconds) result += $"{timeSpan.Minutes}{minutesLabel}";
      if (showSeconds) result += $"{timeSpan.Seconds,2:D2}s";
      return result;
    }


    // https://stackoverflow.com/questions/16192906
    public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : new()
    {
      if (!dict.TryGetValue(key, out TValue val))
      {
        val = new TValue();
        dict.Add(key, val);
      }
      return val;
    }

    //https://stackoverflow.com/questions/818415
    internal static void SetDoubleBuffered(Panel panel, bool value)
    {
      typeof(Panel).InvokeMember("DoubleBuffered",
        BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic, 
        null, panel, [value]);
    }

    public static Bitmap BitmapFromBytes(byte[] imageBytes)
    {
      using (MemoryStream ms = new MemoryStream(imageBytes))
        return new Bitmap(Image.FromStream(ms));
    }

    public static string HtmlToText(string html)
    {
      Regex reg = new Regex("<[^>]+>", RegexOptions.IgnoreCase);
      string s = reg.Replace(html, " ");
      s = HttpUtility.HtmlDecode(s);
      return s;
    }

    public static void EnsureFormVisible(Form form)
    {
      Rectangle formBounds = form.Bounds;

      // check if the form is visible on any connected screen
      bool isVisible = Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(formBounds));
      if (isVisible) return;

      var primaryArea = Screen.PrimaryScreen!.WorkingArea;

      int newWidth = Math.Min(form.Width, primaryArea.Width);
      int newHeight = Math.Min(form.Height, primaryArea.Height);

      int newX = primaryArea.Left + (primaryArea.Width - newWidth) / 2;
      int newY = primaryArea.Top + (primaryArea.Height - newHeight) / 2;

      form.StartPosition = FormStartPosition.Manual;
      form.SetBounds(newX, newY, newWidth, newHeight);
    }
  }
}
