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
    public WsjtxUdpSender WsjtxUdpSender;

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
      Soundcard.Retry = true;

      AudioWaterfall.MouseDown += AudioWaterfall_MouseDown;

      AudioWaterfall.Ft4Decoder = Ft4Decoder;

      MessageListWidget.MessageHover += MessageListWidget_MessageHover;

      WsjtxUdpSender = new(ctx);
      WsjtxUdpSender.HighlightCallsignReceived += WsjtxUdpSender_HighlightCallsignReceived;

      ApplySettings();
    }

    private void WsjtxUdpSender_HighlightCallsignReceived(object? sender, HighlightCallsignEventArgs e)
    {
      MessageListWidget.HighlightCallsign(e);
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
      WsjtxUdpSender?.Dispose();
      WsjtxUdpSender = null;
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

      Ft4Decoder.CutoffFrequency = sett.Waterfall.Bandwidth - 100;
      AudioWaterfall.Bandwidth = sett.Waterfall.Bandwidth;
      AudioWaterfall.SetBandwidth(); // {!} do not call in design mode

      WsjtxUdpSender.Port = ctx.Settings.Ft4Console.UdpSender.Port;
      WsjtxUdpSender.Host = ctx.Settings.Ft4Console.UdpSender.Host;
      WsjtxUdpSender.SetEnabled(ctx.Settings.Ft4Console.UdpSender.Enabled);
    }

    public void AddSamplesFromSdr(DataEventArgs<float> e)
    {
      if (ctx.Settings.Ft4Console.AudioSource == Ft4AudioSource.SDR)
      {
        Ft4Decoder.StartProcessing(e);
        if (AudioWaterfall.CanProcess) AudioWaterfall.SpectrumAnalyzer.StartProcessing(e);
      }
    }

    public void AddSamplesFromSoundcard(DataEventArgs<float> e)
    {
      if (ctx.Settings.Ft4Console.AudioSource == Ft4AudioSource.Soundcard)
      {
        Ft4Decoder.StartProcessing(e);
        if (AudioWaterfall.CanProcess) AudioWaterfall.SpectrumAnalyzer?.StartProcessing(e);
      }
    }

    private void Ft4Decoder_SlotDecoded(object? sender, DataEventArgs<string> e)
    {
      MessageListWidget.BeginInvoke(() =>
      {
        MessageListWidget.BeginUpdateItems();

        MessageListWidget.AddSeparator(Ft4Decoder.DecodedSlotNumber,
          ctx.SatelliteSelector.SelectedSatellite.name, ctx.FrequencyControl.GetBandName(false));

        if (e.Data.Length >= 0)
        {
          double frequency = ctx.FrequencyControl.RadioLink.CorrectedDownlinkFrequency;
          string satellite = ctx.SatelliteSelector.SelectedSatellite.name;

          // to listbox
          var messages = e.Data.Select(s => MakeDecodedItem(s, e.Utc)).ToList();
          MessageListWidget.AddItems(messages);

          // to udp sender
          WsjtxUdpSender.SendDecodedMessages(messages, frequency);

          // to file
          SaveToFile(messages, frequency, satellite);
        }

        MessageListWidget.EndUpdateItems();
      });
    }

    private void SaveToFile(IEnumerable<DecodedItem> messages, double frequency, string satellite)
    {
      //if (!ctx.Settings.Ft4Console.ArchiveToFile) return;

      string archiveFolder = Path.Combine(Utils.GetUserDataFolder(), "FT4");
      Directory.CreateDirectory(archiveFolder);
      string filePath = Path.Combine(archiveFolder, DateTime.Today.ToString("yyyy") + ".txt");

      string text = string.Join('\n', messages.Select(msg => msg.ToArchiveString(frequency, satellite)).ToArray());

      using (StreamWriter writer = File.AppendText(filePath)) writer.WriteLine(text);

    }

    private DecodedItem MakeDecodedItem(string s, DateTime utc)
    {
      DecodedItem item = new();
      item.ParseMessage(s, utc);

      item.Type = DecodedItemType.RxMessage;
      item.Utc = utc;
      item.SlotNumber = Ft4Decoder.DecodedSlotNumber;
      item.ToMe = item.Parse.DXCallsign == ctx.Settings.User.Call;
      item.FromMe = item.Parse.DECallsign == ctx.Settings.User.Call;
      item.SetColors(ctx.Settings.Ft4Console.Messages);

      // callsign color from logger if not receiving wsjtx colors
      if (!WsjtxUdpSender.Active)
        item.SetCallsignColors(ctx.LoggerInterface);

      return item;
    }

    private void AudioWaterfall_MouseMove(object sender, MouseEventArgs e)
    {
      AudioWaterfall.ShowLeftBarTooltip(e.Location);

      if (e.X >= AudioWaterfallWidget.LEFT_BAR_WIDTH && e.Y > AudioWaterfallWidget.TOP_BAR_HEIGHT)
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
