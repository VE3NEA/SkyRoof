using System.Windows.Forms;
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

      MessageListWidget.MessageHover += MessageListWidget_MessageHover;

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
      foreach (DecodedItem item in MessageListWidget.listBox.Items)
        item.SetColors(ctx.Settings.Ft4Console.Messages);
      MessageListWidget.listBox.Refresh();
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
        AudioWaterfall.SpectrumAnalyzer?.StartProcessing(e);
      }
    }

    private void Ft4Decoder_SlotDecoded(object? sender, DataEventArgs<string> e)
    {
      MessageListWidget.BeginInvoke(() =>
      {
        MessageListWidget.BeginUpdateItems();

        MessageListWidget.CheckAddSeparator(Ft4Decoder.DecodedSlotNumber,
          ctx.SatelliteSelector.SelectedSatellite.name, ctx.FrequencyControl.GetBandName(false));

        if (e.Data.Length > 0)
          foreach (string message in e.Data)
          {
            DecodedItem item = new();
            item.ParseMessage(message, e.Utc);

            item.Type = DecodedItemType.RxMessage;
            item.Utc = e.Utc;
            item.SlotNumber = Ft4Decoder.DecodedSlotNumber;
            item.ToMe = item.Parse.DXCallsign == ctx.Settings.User.Call;
            item.FromMe = item.Parse.DECallsign == ctx.Settings.User.Call;
            item.SetColors(ctx.Settings.Ft4Console.Messages);
            item.SetCallsignColors(ctx.LoggerInterface);

            MessageListWidget.AddItem(item);
          }

        MessageListWidget.EndUpdateItems();
      });
    }

    private void AudioWaterfall_MouseMove(object sender, MouseEventArgs e)
    {
      AudioWaterfall.ShowLeftBarTooltip(e.Location);

      if (e.X >= AudioWaterfallWidget.LEFT_BAR_WIDTH)
      {
        (int slotNumber, int audioFreq) = AudioWaterfall.GetSlotAndFreq(e.Location);
        var item = MessageListWidget.FindItem(slotNumber, audioFreq);
        AudioWaterfall.ShowCallsign(item);
      }
    }

    private void MessageListWidget_MessageHover(object? sender, Ft4MessageEventArgs? e)
    {
      AudioWaterfall.ShowCallsign(MessageListWidget.HotItem);
    }
  }
}
