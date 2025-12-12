using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Serilog;


// create custom palettes: https://htmlcolorcodes.com/resources/best-color-palette-generators/

namespace VE3NEA
{
  class Palette
  {
    public int[] Colors = new int[256];

    public Palette()
    {
      Interpolate(DefaultPalette);
    }

    internal static Palette CreateFromFile(string filePath)
    {
      var palette = new Palette();
      palette.LoadFromFile(filePath);
      return palette;
    }

    public void LoadFromFile(string fileName)
    {
      LoadFromStrings(File.ReadAllLines(fileName));
    }

    public void LoadFromStrings(string[] strings)
    {
      try
      {
        Color[] colors = strings.Select(s => Color.FromArgb(Convert.ToInt32(s.Replace("#", "0x"), 16))).ToArray();
        Interpolate(colors);
      }
      catch (Exception e) 
      {
        Log.Error(e, "Error parsing palette strings");
        MakeMonochromePalette(Color.Black);
      }
    }

    public Bitmap ToBitmap()
    {
      var rect = new Rectangle(0, 0, 256, 1);
      var bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
      var bits = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
      Marshal.Copy(Colors, 0, bits.Scan0, rect.Width);
      bmp.UnlockBits(bits);
      return bmp;    
    }

    public void Interpolate(Color[] colors)
    {
      int cnt = colors.Length - 1;
      float dI = 255f / cnt;

      Color[] interpolated = new Color[256];
      interpolated[0] = colors[0];
      int idxE = 0;

      for (int i = 1; i <= cnt; i++)
      {
        int idxB = idxE;
        idxE = (int)Math.Round(dI * i);
        interpolated[idxE] = colors[i];
        InterpolateSegment(interpolated, idxB, idxE);
      }

      for (int i = 0; i < 256; i++) Colors[i] = interpolated[i].ToArgb();
    }

    private void InterpolateSegment(Color[] colors, int idxB, int idxE)
    {
      int cnt = idxE - idxB;

      float dR = ((float)colors[idxE].R - colors[idxB].R) / cnt;
      float dG = ((float)colors[idxE].G - colors[idxB].G) / cnt;
      float dB = ((float)colors[idxE].B - colors[idxB].B) / cnt;

      for (int i = 1; i <= cnt; i++)
        colors[idxB + i] = Color.FromArgb(255,
          (byte)(colors[idxB].R + Math.Round(dR * i)),
          (byte)(colors[idxB].G + Math.Round(dG * i)),
          (byte)(colors[idxB].B + Math.Round(dB * i)));
    }

    //    // CW palette
    //    public static readonly Color[] DefaultPalette = new Color[] {
    //      Color.FromArgb(0x000010),
    //      Color.FromArgb(0x004555),
    //      Color.FromArgb(0x00FFFF),
    //      Color.FromArgb(0x00FFFF),
    //      Color.FromArgb(0xFFFF00),
    //      Color.FromArgb(0xFF0000)
    //    };

    // SkyRoof palette
    //public static readonly Color[] DefaultPalette = new Color[] {
    //  Color.FromArgb(0x000d4e),
    //  Color.FromArgb(0x00d8ff),
    //  Color.FromArgb(0xb3ff46),
    //  Color.FromArgb(0xffe900),
    //  Color.FromArgb(0xff0000),
    //};

    // FLDIgi palette
    public static readonly Color[] DefaultPalette = [
      Color.FromArgb(0x000000),
      Color.FromArgb(0x0000b1),
      Color.FromArgb(0x036ee3),
      Color.FromArgb(0x00cccc),
      Color.FromArgb(0xdfdfdf),
      Color.FromArgb(0x00ea00),
      Color.FromArgb(0xf4f400),
      Color.FromArgb(0xfa7e00),
      Color.FromArgb(0xf40000),
];

    public static Color[] MakeMonochromePalette(Color color)
    {
      return [color, Color.White];
    }
  }
}
  
