using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VE3NEA;

namespace SkyRoof
{
  public partial class Ft4AmplitudeBar : UserControl
  {
    private const float Alpha = 0.1f;
    private float Amplitude = 0.01f;

    public Ft4AmplitudeBar()
    {
      InitializeComponent();
      SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
    }

    public void UpdateAmplitude(DataEventArgs<float> e, float scale=1)
    {
      float max = scale * e.Data.Take(e.Count).Max(Math.Abs);

      if (max > Amplitude)
        Amplitude = max;
      else
        Amplitude = Amplitude * (1 - Alpha) + max * Alpha;

      Invalidate();
    }

    internal string GetTooltip()
    {
      return $"RX Amplitude  {Dsp.ToDb2(Amplitude):F0} dBFS";
    }

    private void Ft4AmplitudeBar_Paint(object sender, PaintEventArgs e)
    {
      var fgBrush = Amplitude >= 0.1f ? Brushes.Red : Brushes.PaleGreen;

      float w = ClientRectangle.Width;
      float h = ClientRectangle.Height;

      float y = -Dsp.ToDb2(Amplitude);
      y = Math.Max(0, Math.Min(h, y));

      e.Graphics.FillRectangle(Brushes.White, new RectangleF(0, 0, w, y));
      e.Graphics.FillRectangle(fgBrush, new RectangleF(0, y, w, h-y));
    }
  }
}
