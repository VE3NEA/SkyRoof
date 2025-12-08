using MathNet.Numerics;
using Serilog;
using VE3NEA;
using WeifenLuo.WinFormsUI.Docking;
using static SkyRoof.Ft4MessageListWidget;

namespace SkyRoof
{
  public partial class Ft4ConsolePanel : DockContent
  {
    private readonly Context ctx;
    private InputSoundcard<float> Soundcard = new();
    public Ft4Decoder Ft4Decoder = new();

    public Ft4ConsolePanel()
    {
      InitializeComponent();
    }

    public Ft4ConsolePanel(Context ctx)
    {
      InitializeComponent();

      this.ctx = ctx;
      Log.Information("Creating Ft4ConsolePanel");

      ctx.Ft4ConsolePanel = this;
      ctx.MainForm.Ft4ConsoleMNU.Checked = true;

      Ft4Decoder.SlotDecoded += Ft4Decoder_SlotDecoded;
      Soundcard.SamplesAvailable += (s, a) => AddSamplesFromSoundcard(a);

      AudioWaterfall.MouseDown += AudioWaterfall_MouseDown;

      AudioWaterfall.Ft4Decoder = Ft4Decoder;

      ApplySettings();
    }

    private void AudioWaterfall_MouseDown(object? sender, MouseEventArgs e)
    {
      AudioWaterfall.SetFrequenciesFromMouseClick(e);
      Ft4Decoder.RxAudioFrequency = AudioWaterfall.RxAudioFrequency;
      Ft4Decoder.TxAudioFrequency = AudioWaterfall.TxAudioFrequency;
    }

    private void Ft4ConsolePanel_FormClosing(object sender, FormClosingEventArgs e)
    {
      Log.Information("Closing Ft4ConsolePanel");
      ctx.Ft4ConsolePanel = null;
      ctx.MainForm.Ft4ConsoleMNU.Checked = false;

      Soundcard?.Dispose();
      Soundcard = null;
      Ft4Decoder?.Dispose();
      Ft4Decoder = null;
    }

    public void ApplySettings()
    {
      var sett = ctx.Settings.Ft4Console;

      Soundcard.Enabled = false;
      Soundcard.SetDeviceId(sett.RxSoundcard);
      Soundcard.Enabled = sett.AudioSource == Ft4AudioSource.Soundcard;

      Ft4Decoder.MyCall = ctx.Settings.User.Call;

      AudioWaterfall.Brightness = sett.Waterfall.Brightness;
      AudioWaterfall.Contrast = sett.Waterfall.Contrast;

      MessageListWidget.ApplySettings(ctx.Settings.Ft4Console.Messages);
    }

    public void AddSamplesFromSdr(DataEventArgs<float> e)
    {
      if (ctx.Settings.Ft4Console.AudioSource == Ft4AudioSource.SDR)
      {
        Ft4Decoder.StartProcessing(e);
        AudioWaterfall.SpectrumAnalyzer.StartProcessing(e);
      }
    }

    public void AddSamplesFromSoundcard(DataEventArgs<float> e)
    {
      if (ctx.Settings.Ft4Console.AudioSource == Ft4AudioSource.Soundcard)
      {
        Ft4Decoder.StartProcessing(e);
        AudioWaterfall.SpectrumAnalyzer.StartProcessing(e);
      }
    }

    private void Ft4Decoder_SlotDecoded(object? sender, DataEventArgs<string> e)
    {
      richTextBox1.BeginInvoke(() =>
      {
        richTextBox1.AppendText($"{e.Utc:HH:mm:ss.f}-------------------------------\n");
        if (e.Data.Length > 0) richTextBox1.AppendText($"{string.Join("\n", e.Data)}\n");
                
        if (e.Data.Length == 0) return;
        MessageListWidget.BeginUpdateItems();


        foreach (string message in e.Data)
        {
          DecodedItem item = new();
          item.Type = DecodedItemType.RxMessage;
          item.Utc = e.Utc;
          item.SlotNumber = Ft4Decoder.DecodedSlotNumber;
          item.ParseMessage(message, e.Utc);
          item.SetColors();

          MessageListWidget.AddItem(item);
        }

        MessageListWidget.EndUpdateItems();
      });
    }
  }
}
