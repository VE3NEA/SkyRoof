using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using VE3NEA;

namespace SkyRoof
{
  public partial class AudioWaterfallWidget : UserControl
  {
    private const int TOP_BAR_HEIGHT = 31;
    private const int LEFT_BAR_WIDTH = 15;
    private const int SPECTRUM_SIZE = 4096;
    private const int WATERFALL_BANDWIDTH = 4100;
    private const int BmpWidth = (int)(SPECTRUM_SIZE * WATERFALL_BANDWIDTH / (SdrConst.AUDIO_SAMPLING_RATE / 2));
    private const int BmpHeight = 1024;

    private readonly Palette Palette = new Palette();
    private readonly Bitmap WaterfallBmp = new Bitmap(BmpWidth, BmpHeight, PixelFormat.Format32bppRgb);
    private readonly Bitmap LeftBmp = new Bitmap(2, BmpHeight, PixelFormat.Format32bppRgb);
    private readonly int[] leftBarSlots = new int[BmpHeight];

    public readonly SpectrumAnalyzer<float> SpectrumAnalyzer = new(SPECTRUM_SIZE, 12000, BmpWidth); //{!}
    public int RxAudioFrequency = 1500;
    public int TxAudioFrequency = 1500;

    private int WriteRow;
    private int LastSlot;

    public Ft4Decoder? Ft4Decoder;
    internal int Brightness = 50;
    internal int Contrast = 50;

    public AudioWaterfallWidget()
    {
      InitializeComponent();

      SpectrumAnalyzer.SpectrumAvailable += (s, e) => BeginInvoke(() => AppendSpectrum(e.Data));      
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
      float pixelsPerHz = WaterfallWidth / (float)WATERFALL_BANDWIDTH;


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
      for (float f = 0; f <= WATERFALL_BANDWIDTH; f += 100)
      {
        x = LEFT_BAR_WIDTH + f * pixelsPerHz;
        e.Graphics.DrawLine(Pens.Black, x, TOP_BAR_HEIGHT - 12, x, TOP_BAR_HEIGHT);
        if (f % 500 == 0)
        {
          string freqText = f.ToString();
          x -= e.Graphics.MeasureString(freqText, Font).Width / 2;
          e.Graphics.DrawString(freqText, Font, Brushes.Black, x, 0);
        }
      }

      // callsign


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


    //----------------------------------------------------------------------------------------------
    //                                      spectra
    //----------------------------------------------------------------------------------------------
    private unsafe void AppendSpectrum(float[] spectrum)
    {
      if (WaterfallBmp == null || WaterfallBmp.Width != spectrum.Length) return;

      if (--WriteRow < 0) WriteRow = WaterfallBmp.Height - 1;

      // left bar slot number
      int slotNumber = Ft4Decoder?.DecodedSlotNumber ?? 0;
      leftBarSlots[WriteRow] = slotNumber;


      // separator
      if (slotNumber != LastSlot)
      {
        AddSeparator(Color.Lime);
        LastSlot = slotNumber;
      }

      // left bar
      Color leftColor = (slotNumber & 1) == 1 ? Color.Olive : Color.Teal;
      LeftBmp.SetPixel(0, WriteRow, leftColor);
      LeftBmp.SetPixel(1, WriteRow, leftColor);

      // waterfall
      var brightness = -80 + Brightness;
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

      int WaterfallWidth = ClientRectangle.Width - LEFT_BAR_WIDTH;
      float HzPerPixel = WATERFALL_BANDWIDTH / (float)WaterfallWidth;
      int audioFrequency = (int)((e.X - LEFT_BAR_WIDTH) * HzPerPixel);

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
  }
}
