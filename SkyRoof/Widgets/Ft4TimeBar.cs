using VE3NEA;

namespace SkyRoof.Widgets
{
  public partial class Ft4TimeBar : UserControl
  {
    private Ft4Slot Slot = new();
    private readonly Brush RemainderBrush = new SolidBrush(Theme.BarRemainder);
    public bool Transmitting;
    public Ft4TimeBar()
    {
      InitializeComponent();
      SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      Slot.Utc = DateTime.UtcNow;

      double activeSeconds = NativeFT4Coder.ENCODE_SAMPLE_COUNT / NativeFT4Coder.SAMPLING_RATE;
      double activeFraction = activeSeconds / NativeFT4Coder.TIMESLOT_SECONDS;
      double currentFraction = Slot.SecondsIntoSlot / NativeFT4Coder.TIMESLOT_SECONDS;

      // tx colors
      var FgBrush = Brushes.Red;
      var BgBrush = Brushes.LightCoral;

      // rx colors
      if (!Transmitting || Slot.SecondsIntoSlot > activeSeconds)
      {
        FgBrush = Slot.Odd ? Brushes.Olive : Brushes.Teal;
        BgBrush = Slot.Odd ? Brushes.Khaki: Brushes.SkyBlue;
      }

      float w = ClientRectangle.Width;
      float h = ClientRectangle.Height;

      float x1 = (float)(currentFraction * w);
      float x2 = (float)(activeFraction * w);
      float x3 = Math.Max(x1, x2);

      if (x1 > 0) e.Graphics.FillRectangle(FgBrush, new RectangleF(0, 0, x1, h));
      if (x2 > x1) e.Graphics.FillRectangle(BgBrush, new RectangleF(x1, 0, x2 - x1, h));
      if (x1 < w) e.Graphics.FillRectangle(RemainderBrush, new RectangleF(x3, 0, w - x3, h));
    }

    private void timer1_Tick(object sender, EventArgs e)
    {
      Refresh();
    }
  }
}
