using System.Diagnostics;
using FontAwesome;
using Newtonsoft.Json.Linq;
using Serilog;
using VE3NEA;
using WeifenLuo.WinFormsUI.Docking;

namespace SkyRoof
{
  public partial class Ft4ConsolePanel : DockContent
  {
    private readonly Context ctx;
    private InputSoundcard<float> Soundcard = new();
    public Ft4Decoder Decoder = new();
    public Ft4Sender Sender = new();
    public Ft4QsoSequencer Sequencer;
    public WsjtxUdpSender WsjtxUdpSender;
    public int TxCountdown;

    public Ft4ConsolePanel()
    {
      InitializeComponent();
    }

    public Ft4ConsolePanel(Context ctx)
    {
      InitializeComponent();

      this.ctx = ctx;
      MessageListWidget.ctx = ctx;
      Log.Information("Creating Ft4ConsolePanel");

      ctx.Ft4ConsolePanel = this;
      ctx.MainForm.Ft4ConsoleMNU.Checked = true;

      Decoder.Decode(); // prime the decoding engine
      Decoder.MessageDecoded += Ft4Decoder_MessageDecoded;
      Soundcard.SamplesAvailable += (s, a) => AddSamplesFromSoundcard(a);
      Soundcard.Retry = true;

      AudioWaterfall.Ft4Decoder = Decoder;
      AudioWaterfall.MouseDown += AudioWaterfall_MouseDown;

      MessageListWidget.MessageHover += MessageListWidget_MessageHover;

      WsjtxUdpSender = new(ctx);
      WsjtxUdpSender.HighlightCallsignReceived += WsjtxUdpSender_HighlightCallsignReceived;

      Sender.BeforeTransmit += Ft4Sender_BeforeTransmit;
      Sender.AfterTransmit += Ft4Sender_AfterTransmit;

      Sequencer = new(ctx.Settings.User.Call, ctx.Settings.User.Square);

      ApplySettings();

      // disable mousewheel on the spinners
      RxSpinner.MouseWheel += (o, e) => ((HandledMouseEventArgs)e).Handled = true;
      TxSpinner.MouseWheel += (o, e) => ((HandledMouseEventArgs)e).Handled = true;
    }

    private void Ft4ConsolePanel_Shown(object sender, EventArgs e)
    {
      SplitContainer.SplitterDistance = ctx.Settings.Ft4Console.SplitterDistance;
    }

    private void Ft4ConsolePanel_FormClosing(object sender, FormClosingEventArgs e)
    {
      Log.Information("Closing Ft4ConsolePanel");
      ctx.Ft4ConsolePanel = null;
      ctx.MainForm.Ft4ConsoleMNU.Checked = false;
      ctx.Settings.Ft4Console.SplitterDistance = SplitContainer.SplitterDistance;

      Sender?.Dispose();
      Sender = null;
      ctx.FrequencyControl.SetPtt(false);
      ctx.FrequencyControl.SetXit(0);
      Soundcard?.Dispose();
      Soundcard = null;
      Decoder?.Dispose();
      Decoder = null;
      WsjtxUdpSender.HighlightCallsignReceived -= WsjtxUdpSender_HighlightCallsignReceived;
      WsjtxUdpSender?.Dispose();
      WsjtxUdpSender = null;
    }

    public void ApplySettings()
    {
      var sett = ctx.Settings.Ft4Console;

      Soundcard.Enabled = false;
      Soundcard.SetDeviceId(sett.Receive.RxSoundcard);
      Soundcard.Enabled = sett.Receive.AudioSource == Ft4AudioSource.Soundcard;

      AudioWaterfall.Brightness = sett.Waterfall.Brightness;
      AudioWaterfall.Contrast = sett.Waterfall.Contrast;
      AudioWaterfall.Bandwidth = sett.Receive.Bandwidth;
      AudioWaterfall.SetBandwidth();

      MessageListWidget.ApplySettings(ctx.Settings);
      foreach (DecodedItem item in MessageListWidget.listBox.Items)
        item.SetColors(ctx.Settings.Ft4Console.Messages);
      MessageListWidget.listBox.Refresh();

      WsjtxUdpSender.Port = ctx.Settings.Ft4Console.UdpSender.Port;
      WsjtxUdpSender.Host = ctx.Settings.Ft4Console.UdpSender.Host;
      WsjtxUdpSender.SetEnabled(ctx.Settings.Ft4Console.UdpSender.Enabled);

      Decoder.MyCall = ctx.Settings.User.Call;
      Decoder.CutoffFrequency = sett.Receive.Bandwidth - 50;

      Sender.Soundcard.SetDeviceId(sett.Transmit.TxSoundcard);
      Sender.XitEnabled = sett.Transmit.XitEnabled;
      TxGainSlider.Value = sett.Transmit.TxGain;

      Sequencer.MyCall = ctx.Settings.User.Call;
      Sequencer.MySquare = ctx.Settings.User.Square;
      TxMessageLabel.Text = $"{Sequencer.Message}";
      Sender.SetMessage(Sequencer.Message!);
      UpdateMessageButtons();

      UpdateTxButtons();
    }

    private void TxAmplitudeSlider_ValueChanged(object sender, EventArgs e)
    {
      ctx.Settings.Ft4Console.Transmit.TxGain = TxGainSlider.Value;
      Sender.Soundcard.Volume = Dsp.FromDb2(TxGainSlider.Value);
      toolTip1.SetToolTip(TxGainSlider, $"TX Gain  {TxGainSlider.Value:F0} dB");
    }



    //--------------------------------------------------------------------------------------------------------------
    //                                                decoder
    //--------------------------------------------------------------------------------------------------------------
    public void AddSamplesFromSdr(DataEventArgs<float> e)
    {
      if (ctx.Settings.Ft4Console.Receive.AudioSource == Ft4AudioSource.SDR)
      {
        Decoder.StartProcessing(e);
        if (AudioWaterfall.CanProcess) AudioWaterfall.SpectrumAnalyzer.StartProcessing(e);
        AmplitudeBar.UpdateAmplitude(e, 0.01f);
      }
    }

    public void AddSamplesFromSoundcard(DataEventArgs<float> e)
    {
      if (ctx.Settings.Ft4Console.Receive.AudioSource == Ft4AudioSource.Soundcard)
      {
        Decoder?.StartProcessing(e);
        if (AudioWaterfall.CanProcess) AudioWaterfall.SpectrumAnalyzer?.StartProcessing(e);
        AmplitudeBar.UpdateAmplitude(e);
      }
    }

    private void Ft4Decoder_MessageDecoded(object? sender, DataEventArgs<string> e)
    {
      BeginInvoke(() =>
      {
        MessageListWidget.BeginUpdateItems();

        AddSeparator(Decoder.DecodedSlotNumber);

        if (e.Data.Length > 0)
        {
          double frequency = ctx.FrequencyControl.RadioLink.CorrectedDownlinkFrequency;
          var message = MakeDecodedItem(e.Data[0], e.Utc);

          // to sequencer
          RxMessageToSequencer(message);

          // to listbox
          if (message.FromMe) MessageListWidget.AddMessageFromMe(message);
          else MessageListWidget.AddMessage(message);

          // to udp sender
          WsjtxUdpSender.SendDecodedMessage(message, frequency);

          // to file
          SaveMessageToFile(message);
        }

        MessageListWidget.EndUpdateItems();
      });
    }

    private void SaveMessageToFile(DecodedItem message)
    {
      if (!ctx.Settings.Ft4Console.Messages.ArchiveToFile) return;

      double frequency = message.Type == DecodedItemType.RxMessage ?
        ctx.FrequencyControl.RadioLink.CorrectedDownlinkFrequency :
        ctx.FrequencyControl.RadioLink.CorrectedUplinkFrequency;
      string satellite = ctx.SatelliteSelector.SelectedSatellite.name;

      string archiveFolder = Path.Combine(Utils.GetUserDataFolder(), "FT4");
      Directory.CreateDirectory(archiveFolder);
      string filePath = Path.Combine(archiveFolder, DateTime.Today.ToString("yyyy") + ".txt");

      string text = message.ToArchiveString(frequency, satellite);

      if (text != "")
        using (StreamWriter writer = File.AppendText(filePath))
          writer.WriteLine(text);
    }

    private void WsjtxUdpSender_HighlightCallsignReceived(object? sender, HighlightCallsignEventArgs e)
    {
      MessageListWidget.HighlightCallsign(e);
    }




    //--------------------------------------------------------------------------------------------------------------
    //                                                mouse
    //--------------------------------------------------------------------------------------------------------------
    private void AudioWaterfall_MouseDown(object? sender, MouseEventArgs e)
    {
      AudioWaterfall.SetFrequenciesFromMouseClick(e);
      UpdateControls();
      ctx.MainForm.FrequencyWidget.SetXit(Sender.XitOffset);
    }

    private void AudioWaterfall_MouseMove(object sender, MouseEventArgs e)
    {
      AudioWaterfall.ShowLeftBarTooltip(e.Location);

      if (e.X >= AudioWaterfallWidget.LEFT_BAR_WIDTH && e.Y > AudioWaterfallWidget.TOP_BAR_HEIGHT)
      {
        (long slotNumber, int audioFreq) = AudioWaterfall.GetSlotAndFreq(e.Location);
        var item = MessageListWidget.FindItem(slotNumber, audioFreq);
        AudioWaterfall.ShowCallsign(item);
      }
    }

    private void MessageListWidget_MessageHover(object? sender, Ft4MessageListWidget.Ft4MessageEventArgs? e)
    {
      AudioWaterfall.ShowCallsign(MessageListWidget.HotItem);
    }

    private void EnableTxBtn_MouseDown(object sender, MouseEventArgs e)
    {
      if (!CheckTxEnabled()) return;

      if (Sender.Mode == SenderMode.Sending && TxCountdown > 0)
      {
        TxCountdown = 0;
        if (Sender.SenderPhase == SendingStage.Idle) Sender.Stop();
      }
      else
      {
        Sender.StartSending();
        if (Sender.Mode == SenderMode.Sending) // no sending if was tuning 
          TxCountdown = ctx.Settings.Ft4Console.Transmit.TxWatchDog * 4;
      }

      UpdateTxButtons();
    }

    private void HaltTxBtn_MouseDown(object sender, MouseEventArgs e)
    {
      Sender.Stop();
      UpdateTxButtons();
    }

    private void TuneBtn_MouseDown(object sender, MouseEventArgs e)
    {
      if (!CheckTxEnabled()) return;

      if (Sender.Mode == SenderMode.Tuning)
        Sender.Stop();
      else
        Sender.StartTuning();

      UpdateTxButtons();
    }




    //--------------------------------------------------------------------------------------------------------------
    //                                                controls
    //--------------------------------------------------------------------------------------------------------------
    private void UpdateTxButtons()
    {
      bool tuning = Sender.Mode == SenderMode.Tuning;
      bool sendEnabled = Sender.Mode == SenderMode.Sending && TxCountdown > 0;
      //bool sending = Ft4Sender.stage == SendStage.Sending;


      EnableTxBtn.BackColor = sendEnabled ? Color.LightCoral : Color.Transparent;
      HaltTxBtn.BackColor = Sender.SenderPhase == SendingStage.Sending ? Color.Red : Color.Transparent;
      TuneBtn.BackColor = tuning ? Color.LightCoral : Color.Transparent;

      TuneBtn.Refresh();
      EnableTxBtn.Refresh();
      HaltTxBtn.Refresh();
    }

    private void UpdateControls()
    {
      if (Sender.TxOdd)
      {
        OddRadioBtn.Checked = true;
        OddEvenGroupBox.Text = "TX Odd";
      }
      else
      {
        EvenRadioBtn.Checked = true;
        OddEvenGroupBox.Text = "TX Even";
      }

      RxSpinner.Value = AudioWaterfall.RxAudioFrequency;
      TxSpinner.Value = AudioWaterfall.TxAudioFrequency;
    }

    private bool CheckTxEnabled()
    {
      if (ctx.Settings.Ft4Console.Transmit.Enabled) return true;

      MessageBox.Show("FT4 transmit is not enabled in Settings.", "SkyRoof", MessageBoxButtons.OK, MessageBoxIcon.Information);
      return false;
    }

    private void TxSpinner_ValueChanged(object sender, EventArgs e)
    {
      if (TxSpinner.Value != Sender.TxAudioFrequency)
      {
        AudioWaterfall.TxAudioFrequency = Sender.TxAudioFrequency = (int)TxSpinner.Value;
        Sender.SetMessage(Sequencer.Message!);
        ctx.MainForm.FrequencyWidget.SetXit(Sender.XitOffset);
      }
    }

    private void RxSpinner_ValueChanged(object sender, EventArgs e)
    {
      if (RxSpinner.Value != Decoder.RxAudioFrequency)
        AudioWaterfall.RxAudioFrequency = Decoder.RxAudioFrequency = (int)RxSpinner.Value;
    }

    private void TxToRxBtn_Click(object sender, EventArgs e)
    {
      RxSpinner.Value = TxSpinner.Value;
    }

    private void RxToTxBtn_Click(object sender, EventArgs e)
    {
      TxSpinner.Value = RxSpinner.Value;
    }

    private void OddRadioBtn_CheckedChanged(object sender, EventArgs e)
    {
      Sender.TxOdd = OddRadioBtn.Checked;
      UpdateControls();
    }

    private void toolTip1_Popup(object sender, PopupEventArgs e)
    {
      if (e.AssociatedControl == AmplitudeBar) toolTip1.SetToolTip(AmplitudeBar, AmplitudeBar.GetTooltip());
    }




    //--------------------------------------------------------------------------------------------------------------
    //                                           message buttons
    //--------------------------------------------------------------------------------------------------------------
    private Ft4MessageType GetButtonMessage(object btn)
    {
      return (Ft4MessageType)int.Parse((string)((Button)btn).Tag!);
    }

    public void UpdateMessageButtons()
    {
      foreach (Button btn in TxMessagesPanel.Controls)
      {
        var msg = GetButtonMessage(btn);
        btn.Enabled = Sequencer.IsMessageAvailable(msg);
        btn.BackColor = msg == Sequencer.MessageType ? Color.LightCoral : SystemColors.Control;
      }

      TxMessageLabel.Text = Sequencer.Message;
    }




    //--------------------------------------------------------------------------------------------------------------
    //                                           message list
    //--------------------------------------------------------------------------------------------------------------
    private DecodedItem MakeDecodedItem(string s, DateTime utc)
    {
      DecodedItem item = new();
      item.ParseMessage(s, utc);

      item.Type = DecodedItemType.RxMessage;
      item.Utc = utc;
      item.SlotNumber = Decoder.DecodedSlotNumber;
      item.ToMe = item.Parse.DXCallsign == ctx.Settings.User.Call;
      item.FromMe = item.Parse.DECallsign == ctx.Settings.User.Call;
      item.SetColors(ctx.Settings.Ft4Console.Messages);

      // callsign color from logger if not receiving wsjtx colors
      if (!WsjtxUdpSender.Active)
        item.SetCallsignColors(ctx.LoggerInterface);

      return item;
    }

    private DecodedItem MakeTxItem()
    {
      Sender.Slot.Utc = DateTime.UtcNow;

      var item = new DecodedItem();
      item.Type = DecodedItemType.TxMessage;
      item.Utc = DateTime.UtcNow;
      item.SlotNumber = Sender.Slot.SlotNumber;

      item.Tokens = [
        new DecodedItem.DisplayToken(" TX  0.0"),
        new DecodedItem.DisplayToken($"{Sender.TxAudioFrequency,4:D}")
      ];
      item.Tokens.AddRange(DecodedItem.SplitWords(Sequencer.Message ?? ""));
      item.RawText = "       " + item.Tokens.Select(t => t.text).Aggregate((a, b) => a + " " + b);

      return item;
    }

    private void AddTxMessageToList(DecodedItem item)
    {
      AddSeparator(Sender.Slot.SlotNumber);
      MessageListWidget.AddTxMessage(item);
    }

    private void AddSeparator(long slot)
    {
      DateTime slotTime = DateTime.MinValue + TimeSpan.FromSeconds(slot * NativeFT4Coder.TIMESLOT_SECONDS);

      var satelliteName = ctx.SatelliteSelector.SelectedSatellite.name;
      var bandName = ctx.FrequencyControl.GetBandName(false);

      var separator = new DecodedItem();
      separator.Type = DecodedItemType.Separator;
      separator.SlotNumber = slot;
      separator.Utc = slotTime;

      separator.Tokens = [
        new(FontAwesomeIcons.Circle),
        new($"{slotTime:HH:mm:ss}"),
        new(satelliteName),
        new(bandName),
        new(new string('-', 20)) // '̶'
        ];

      separator.Tokens[0].fgBrush = separator.Odd ? Brushes.Olive : Brushes.Teal;

      MessageListWidget.AddSeparator(separator);
    }




    //--------------------------------------------------------------------------------------------------------------
    //                                                sender
    //--------------------------------------------------------------------------------------------------------------
    private void Ft4Sender_BeforeTransmit(object? sender, EventArgs e)
    {
      Ft4TimeBar1.Transmitting = Sender.Mode == SenderMode.Sending;

      BeginInvoke(() =>
      {
        ctx.MainForm.FrequencyWidget.SetXit(Sender.XitOffset);
        ctx.MainForm.FrequencyWidget.SetPtt(true);
        UpdateTxButtons();

        if (Ft4TimeBar1.Transmitting) // as opposed to tuning
        {
          var item = MakeTxItem();
          AddTxMessageToList(item);
          SaveMessageToFile(item);
        }
      });
    }

    private void Ft4Sender_AfterTransmit(object? sender, EventArgs e)
    {
      Ft4TimeBar1.Transmitting = false;

      BeginInvoke(() =>
      {
        ctx.MainForm.FrequencyWidget.SetPtt(false);
        ctx.MainForm.FrequencyWidget.SetXit(0);

        if (Sender.Mode == SenderMode.Sending) // and not tuning
        {
          TxCountdown--;
          if (TxCountdown <= 0) Sender.Stop();

          if (Sequencer.MessageType == Ft4MessageType.RR73 || Sequencer.MessageType == Ft4MessageType._73)
          {
            TxCountdown = 0;
            Sender.Stop();
            QsoInfo qso = GetQsoInfo();
            bool newQso = Sequencer.LastHisCall != Sequencer.HisCall;
            if (newQso) LoqFt4QsoDialog.PopUp(ctx, qso);
            LogBtn.Enabled = true; // enable log button after first QSO
            Sequencer.Reset();
            Sender.SetMessage(Sequencer.Message!);
          }
        }

        UpdateTxButtons();
        UpdateMessageButtons();
      });
    }

    private QsoInfo GetQsoInfo()
    {
      string sat = ctx.SatelliteSelector.SelectedSatellite?.LotwName ?? "";
      double freq = ctx.FrequencyControl.RadioLink.CorrectedUplinkFrequency;
      string band = ctx.FrequencyControl.GetBandName(true);
      
      DateTime utc = Sender.Slot.CurrentSlotStartTime;
      if (utc == DateTime.MinValue) utc = DateTime.UtcNow;

      QsoInfo qso = new();

      qso.StationCallsign = Sequencer.MyCall;
      qso.MyGridSquare = Sequencer.MySquare;
      qso.Utc = DateTime.UtcNow;
      qso.Call = Sequencer.HisCall ?? Sequencer.LastHisCall ?? "NO CALLSIGN"; 
      qso.Band = band;
      qso.Mode = "FT4";
      qso.Sat = sat;
      qso.Grid = Sequencer.HisSquare ?? "";
      qso.State = "";
      qso.Sent = Sequencer.IntToReport(Sequencer.HisSnr);
      qso.Recv = Sequencer.IntToReport(Sequencer.MySnr);
      qso.Name = "";
      qso.Notes = "";
      qso.TxFreq = (ulong)freq;

      return qso;
    }

    private void LogBtn_MouseClick(object sender, MouseEventArgs e)
    {
      LoqFt4QsoDialog.PopUp(ctx, GetQsoInfo());
    }




    //--------------------------------------------------------------------------------------------------------------
    //                                             sequencer
    //--------------------------------------------------------------------------------------------------------------
    // any received message
    private void RxMessageToSequencer(DecodedItem message)
    {
      if (Sender.Mode != SenderMode.Sending) return;
      if (!message.ToMe) return;

      if (Sequencer.ProcessMessage(message, false)) SetTxMessage(Sender.TxOdd);
    }

    // click on someone's message
    private void MessageListWidget_MessageClick(object sender, Ft4MessageListWidget.Ft4MessageEventArgs e)
    {
      if (e.Item.FromMe && ModifierKeys == Keys.Control)
        AdjustUplinkOffset(e.Item);

      else if (Sender.Mode != SenderMode.Tuning)
        if (Sequencer.ProcessMessage(e.Item, true))
          SetTxMessage(!e.Item.Odd);
    }

    // message selection buttons
    private void MessageBtn_Click(object sender, EventArgs e)
    {
      if (!CheckTxEnabled()) return;

      var msg = GetButtonMessage(sender);
      if (Sequencer.ForceMessage(msg)) SetTxMessage(Sender.TxOdd);
    }

    private void SetTxMessage(bool odd)
    {
      bool wasSending = Sender.SenderPhase == SendingStage.Sending;
      bool oddChanged = Sender.TxOdd != odd;

      Sender.TxOdd = odd;

      if (Sequencer.RxAudioFrequency != Decoder.RxAudioFrequency)
      {
        Decoder.RxAudioFrequency = Sequencer.RxAudioFrequency;
        AudioWaterfall.RxAudioFrequency = Sequencer.RxAudioFrequency;

        if (ModifierKeys == Keys.Control)
        {
          AudioWaterfall.TxAudioFrequency = Sequencer.RxAudioFrequency;
          Sender.TxAudioFrequency = Sequencer.RxAudioFrequency;
        }
      }

      Sender.SetMessage(Sequencer.Message!);
      if (wasSending && oddChanged) Sender.Stop();
      Sender.StartSending();
      TxCountdown = ctx.Settings.Ft4Console.Transmit.TxWatchDog * NativeFT4Coder.TIME_SLOTS_PER_MINUTE;

      TxMessageLabel.Text = Sequencer.Message!;
      UpdateTxButtons();
      UpdateMessageButtons();
      UpdateControls();
      ctx.MainForm.FrequencyWidget.SetXit(Sender.XitOffset);

      // changing the message on the fiy, show it in the list
      if (wasSending && !oddChanged)
      {
        var item = MakeTxItem();
        AddTxMessageToList(item);
        SaveMessageToFile(item);
      }
    }

    //public void TestQDateTime()
    //{
    //  for (int h = 0; h < 48; h += 6)
    //  {
    //    var utc = DateTime.UtcNow.Date + TimeSpan.FromHours(h);

    //    // c# write
    //    long julianDay = (long)Math.Floor(utc.ToOADate()) + 2415019;
    //    uint milliseconds = (uint)(utc.TimeOfDay.TotalMilliseconds);

    //    // c# original read
    //    var date = DateTime.FromOADate(julianDay - 2415018.5).Date;
    //    DateTime outUtc = date.AddMilliseconds(milliseconds);

    //    // Delphi read
    //    double delphiDouble = julianDay - 2415019 + milliseconds / 86400000d;
    //    DateTime delphiUtc = DateTime.SpecifyKind(DateTime.FromOADate(delphiDouble), DateTimeKind.Unspecified);


    //    Debug.WriteLine($"{utc:yyyy-MM-dd HH:mm}  {julianDay,10}  {milliseconds / 86400000d:F3}  c#: {outUtc:yyyy-MM-dd HH:mm}  Delphi: {delphiDouble:F3}  {delphiUtc:yyyy-MM-dd HH:mm}");
    //  }
    //}




    //--------------------------------------------------------------------------------------------------------------
    //                                         transmitter frequency
    //--------------------------------------------------------------------------------------------------------------
    private void AdjustUplinkOffset(DecodedItem item)
    {
      double error = item.Decode.OffsetFrequencyHz - AudioWaterfall.TxAudioFrequency;

      if (Math.Abs(error) <= 1000)
        ctx.FrequencyControl.AdjustUplinkOffset(error);
      else
        Console.Beep();
    }
  }
}
