using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using VE3NEA;
using WsjtxUtils.WsjtxMessages.Messages;

namespace SkyRoof
{
  public partial class AudioWaterfallWidget : UserControl
  {
    public const int TOP_BAR_HEIGHT = 31;
    public const int LEFT_BAR_WIDTH = 15;
    private const int SPECTRUM_SIZE = 8192;
    private const int SPECTRA_PER_SECOND = 4;
    private const int BmpHeight = 1024;

    private readonly Palette Palette = new Palette();
    private Bitmap WaterfallBmp;
    private readonly Bitmap LeftBmp = new Bitmap(2, BmpHeight, PixelFormat.Format32bppRgb);
    private readonly int[] leftBarSlots = new int[BmpHeight];
    private DecodedItem? HotItem;
    private int BmpWidth;
    private int WriteRow;
    private int LastSlot;

    public SpectrumAnalyzer<float> SpectrumAnalyzer;
    public int RxAudioFrequency = 1500;
    public int TxAudioFrequency = 1500;
    public int Bandwidth = 0;
    public bool CanProcess = false;
    public Ft4Decoder? Ft4Decoder;
    public int Brightness = 50;
    public int Contrast = 50;

    public AudioWaterfallWidget()
    {
      InitializeComponent();
    }

    // SpeftrumAnalyzer cannot be created in the design mode, otherwise
    // user control is silently deleted from the form.
    // Design mode can be detected only in OnHandleCreated
    protected override void OnHandleCreated(EventArgs e)
    {
      base.OnHandleCreated(e);

      if (!IsInDesignMode()) SetBandwidth();
    }

    public void SetBandwidth()
    {
      if (Handle == IntPtr.Zero) return;

      CanProcess = false;

      BmpWidth = SPECTRUM_SIZE * Bandwidth / (SdrConst.AUDIO_SAMPLING_RATE / 2);

      WaterfallBmp?.Dispose();
      WaterfallBmp = new Bitmap(BmpWidth, BmpHeight, PixelFormat.Format32bppRgb);

      SpectrumAnalyzer?.Dispose();
      SpectrumAnalyzer = new SpectrumAnalyzer<float>(SPECTRUM_SIZE, SdrConst.AUDIO_SAMPLING_RATE / SPECTRA_PER_SECOND, BmpWidth);
      SpectrumAnalyzer.SpectrumAvailable += (s, ev) => BeginInvoke(() => AppendSpectrum(ev.Data));

      CanProcess = true;
    }

    private bool IsInDesignMode()
    {
      Control? c = this;
      while (c != null)
        if (c.Site?.DesignMode == true) return true; 
        else c = c.Parent;
      return false;
    }

    //----------------------------------------------------------------------------------------------
    //                                      paint
    //----------------------------------------------------------------------------------------------
    protected override void OnPaintBackground(PaintEventArgs e)
    {
      // do not paint background
    }

    private void AudioWaterfallWidget_Resize(object sender, EventArgs e)
    {
      Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      base.OnPaint(e);

      int WaterfallHeight = ClientRectangle.Height - TOP_BAR_HEIGHT;
      int WaterfallWidth = ClientRectangle.Width - LEFT_BAR_WIDTH;
      float pixelsPerHz = WaterfallWidth / (float)Bandwidth;

      if (WaterfallBmp == null)
      {
        e.Graphics.FillRectangle(SystemBrushes.Control, ClientRectangle);
        return;
      }

      // top bar bg
      var rect = new Rectangle(0, 0, ClientRectangle.Width, TOP_BAR_HEIGHT);
      e.Graphics.FillRectangle(SystemBrushes.Control, rect);

      // rx and tx marks
      float dx = Ft4Decoder.FT4_SIGNAL_BANDWIDTH * pixelsPerHz;

      float x = LEFT_BAR_WIDTH + TxAudioFrequency * pixelsPerHz;
      rect = new Rectangle((int)x, 0, (int)dx, TOP_BAR_HEIGHT / 2 - 1);
      e.Graphics.FillRectangle(Brushes.LightCoral, rect);

      x = LEFT_BAR_WIDTH + RxAudioFrequency * pixelsPerHz;
      rect = new Rectangle((int)x, TOP_BAR_HEIGHT / 2 + 1, (int)dx, TOP_BAR_HEIGHT - 1);
      e.Graphics.FillRectangle(Brushes.LightGreen, rect);

      // scale
      for (float f = 0; f <= Bandwidth; f += 100)
      {
        x = LEFT_BAR_WIDTH + f * pixelsPerHz;

        if (f % 500 == 0)
        {
          e.Graphics.DrawLine(Pens.Black, x, TOP_BAR_HEIGHT - 12, x, TOP_BAR_HEIGHT);
          string freqText = f.ToString();
          x -= e.Graphics.MeasureString(freqText, Font).Width / 2;
          e.Graphics.DrawString(freqText, Font, Brushes.Black, x, 0);
        }
        else
          e.Graphics.DrawLine(Pens.Black, x, TOP_BAR_HEIGHT - 7, x, TOP_BAR_HEIGHT);
      }

      // callsign
      if (HotItem?.Type == Ft4MessageListWidget.DecodedItemType.RxMessage)
      {
        x = LEFT_BAR_WIDTH + pixelsPerHz * HotItem.Decode.OffsetFrequencyHz; 
        DrawTriangle(e.Graphics, (int)x, Pens.Green, Brushes.Lime);

        var bgBrush = Brushes.White;
        var fgBrush = Brushes.Black;
        var token = HotItem.Tokens.FirstOrDefault(t => t.text == HotItem.Parse.DECallsign);
        if (token != null)
        {
          if ((token.bgBrush as SolidBrush)?.Color != Color.Transparent) bgBrush = token.bgBrush;
          if ((token.fgBrush as SolidBrush)?.Color != Color.Silver) fgBrush = token.fgBrush;
        }

        var size = e.Graphics.MeasureString(HotItem.Parse.DECallsign, Font);
        var textRect = new RectangleF(x + 13, 4, size.Width, size.Height);
        var borderRect = textRect; borderRect.Inflate(1, 1);
        e.Graphics.FillRectangle(Brushes.Green, borderRect);
        e.Graphics.FillRectangle(bgBrush, textRect);
        e.Graphics.DrawString(HotItem.Parse.DECallsign, Font, fgBrush, textRect.Left, textRect.Top);
      }

      // waterfall
      e.Graphics.CompositingMode = CompositingMode.SourceCopy;
      e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;

      int segmentHeight = Math.Min(WaterfallHeight, WaterfallBmp.Height - WriteRow);
      var SrcRect = new Rectangle(0, WriteRow, WaterfallBmp.Width, segmentHeight);
      var DstRect = new Rectangle(LEFT_BAR_WIDTH, TOP_BAR_HEIGHT, WaterfallWidth, segmentHeight);
      e.Graphics.DrawImage(WaterfallBmp, DstRect, SrcRect, GraphicsUnit.Pixel);

      // left bar bg
      SrcRect = new Rectangle(0, WriteRow, 1, segmentHeight);
      DstRect = new Rectangle(0, TOP_BAR_HEIGHT, LEFT_BAR_WIDTH, segmentHeight);
      e.Graphics.DrawImage(LeftBmp, DstRect, SrcRect, GraphicsUnit.Pixel);

      if (segmentHeight < WaterfallHeight)
      {
        SrcRect = new Rectangle(0, 0, WaterfallBmp.Width, WaterfallHeight - segmentHeight);
        DstRect = new Rectangle(LEFT_BAR_WIDTH, TOP_BAR_HEIGHT + segmentHeight, WaterfallWidth, WaterfallHeight - segmentHeight);
        e.Graphics.DrawImage(WaterfallBmp, DstRect, SrcRect, GraphicsUnit.Pixel);

        SrcRect = new Rectangle(0, 0, 1, WaterfallHeight - segmentHeight);
        DstRect = new Rectangle(0, TOP_BAR_HEIGHT + segmentHeight, LEFT_BAR_WIDTH, WaterfallHeight - segmentHeight);
        e.Graphics.DrawImage(LeftBmp, DstRect, SrcRect, GraphicsUnit.Pixel);
      }
    }

    const int TRI_SIZE = 7;
    private void DrawTriangle(Graphics g, int x, Pen pen, Brush brush)
    {
      var p = new Point(x, TOP_BAR_HEIGHT - 2);
      Point[] points = { p, new Point(p.X - TRI_SIZE, p.Y - TRI_SIZE), new Point(p.X + TRI_SIZE, p.Y - TRI_SIZE), p };
      g.FillPolygon(brush, points);
      g.DrawLines(pen, points);
    }




    //----------------------------------------------------------------------------------------------
    //                                      spectra
    //----------------------------------------------------------------------------------------------
    private unsafe void AppendSpectrum(float[] spectrum)
    {
      if (!CanProcess || WaterfallBmp == null || WaterfallBmp.Width != spectrum.Length) return;

      if (--WriteRow < 0) WriteRow = WaterfallBmp.Height - 1;

      // left bar slot number
      int currentSlotNumber = Ft4Decoder?.CurrentSlotNumber ?? 0;
      leftBarSlots[WriteRow] = currentSlotNumber;


      // separator
      if (currentSlotNumber != LastSlot)
      {
        AddSeparator(Color.Lime);
        LastSlot = currentSlotNumber;
      }

      // left bar
      Color leftColor = (currentSlotNumber & 1) == 1 ? Color.Olive : Color.Teal;
      LeftBmp.SetPixel(0, WriteRow, leftColor);
      LeftBmp.SetPixel(1, WriteRow, leftColor);

      // waterfall
      var brightness = -60 + Brightness;
      var contrast = Contrast;
      var rect = new Rectangle(0, WriteRow, WaterfallBmp.Size.Width, 1);
      var data = WaterfallBmp.LockBits(rect, ImageLockMode.ReadWrite, WaterfallBmp.PixelFormat);
      var ptr = (int*)data.Scan0;

      for (int i = 0; i < WaterfallBmp.Width; i++)
      {
        byte v = (byte)Math.Max(0, Math.Min(255, (int)(brightness + spectrum[i] * contrast)));
        *ptr++ = Palette.Colors[v];
      }

      WaterfallBmp.UnlockBits(data);

      Invalidate();
      Update();
    }

    private void AddSeparator(Color color)
    {
      LeftBmp.SetPixel(0, WriteRow, color);
      LeftBmp.SetPixel(1, WriteRow, color);
      Graphics.FromImage(WaterfallBmp).DrawLine(new Pen(color), 0, WriteRow, WaterfallBmp.Width, WriteRow);

      if (--WriteRow < 0) WriteRow = WaterfallBmp.Height - 1;
    }

    public void SetFrequenciesFromMouseClick(MouseEventArgs e)
    {
      if (e.Button != MouseButtons.Left) return;
      if (e.X <= LEFT_BAR_WIDTH) return;

      int audioFrequency = PixelToFrequency(e.X);

      if ((ModifierKeys & Keys.Control) != Keys.None)
        { 
        RxAudioFrequency = audioFrequency;
        TxAudioFrequency = audioFrequency;
      }
      else if ((ModifierKeys & Keys.Shift) != Keys.None)
        TxAudioFrequency = audioFrequency;
      else
        RxAudioFrequency = audioFrequency;
    }

    public int PixelToFrequency(int x)
    {
      if (x <= LEFT_BAR_WIDTH) return 0;
      int WaterfallWidth = ClientRectangle.Width - LEFT_BAR_WIDTH;
      float HzPerPixel = Bandwidth / (float)WaterfallWidth;
      return (int)((x - LEFT_BAR_WIDTH) * HzPerPixel);
    }

    internal void ShowCallsign(DecodedItem hotItem)
    {
      HotItem = hotItem;
      Refresh();
    }

    internal (int slotNumber, int audioFreq) GetSlotAndFreq(Point location)
    {
      int index = WriteRow + (location.Y - TOP_BAR_HEIGHT);
      if (index >= BmpHeight) index -= BmpHeight;
      int slot = leftBarSlots[index];

      int frequency = PixelToFrequency(location.X);
      return (slot, frequency);
    }

    public void ShowLeftBarTooltip(Point p)
    {
      if (p.X > LEFT_BAR_WIDTH)
      {
        toolTip1.SetToolTip(this, "");
        return;
      }

      (int slot, float freq) = GetSlotAndFreq(p);
      string odd = (slot & 1) == 1 ? "Odd (2-nd)" : "Even (1-st)";
      DateTime start = DateTime.Today + TimeSpan.FromSeconds(slot * NativeFT4Coder.TIMESLOT_SECONDS);

      string tooltip = $"slot {slot}\n{start:HH:mm:ss.f} Z\n{odd}";
      toolTip1.SetToolTip(this, tooltip);
    }
  }
}
