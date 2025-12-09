using System.Collections.Generic;
using System.Drawing;
using Devcorp.Controls.Design;
using WsjtxUtils.WsjtxMessages.Messages;
using WsjtxUtils.WsjtxMessages.QsoParsing;
using static SkyRoof.Ft4MessageListWidget;

namespace SkyRoof
{
  public enum WkdStatus
  {
    Dupe,
    NotNeeded,
    Unknown,
    DxMarathonDupe,
    DxMarathon,
    UnconfirmedSquare6m,
    UnconfirmedDxcc6m,
    NewSquare6m,
    NewDxcc6m,
    NeededBandCountry,
    NeededModeCountry,
    NewDxcc,
    WatchList
  }

  public class DecodedItem
  {
    private static readonly string[] CqWords = ["CQ", "73", "RR73"];

    public class DisplayToken
    {
      public readonly string text;
      public bool Underlined;
      public Brush bgBrush = Brushes.Transparent;
      public Brush fgBrush = Brushes.Black;
      public bool AppendSpace = true;

      public DisplayToken(string text)
      {
        this.text = text;
      }
    }

    public DecodedItemType Type;
    public int SlotNumber;
    public bool Odd => SlotNumber % 2 == 1;

    public WkdStatus WkdStatus;
    public bool ToMe;
    public bool FromMe;

    public Decode Decode;
    public WsjtxQso Parse;
    public List<DisplayToken> Tokens;
    public DateTime Utc;

    public bool IsClickable()
    {
      return false;
    }

    public string? GetTooltip()
    {
      return "tooltip!";
    }

    public void ParseMessage(string message, DateTime receivedAt)
    {
      var decode = new Decode();

      Decode = ParseMessageString(message, receivedAt);
      Parse = WsjtxQsoParser.ParseDecode(Decode);
      TokenizeMessage();
    }

    public static Decode ParseMessageString(string message, DateTime receivedAt)
    {
      var decode = new Decode();
      decode.Id = "id???"; //{!}
      decode.New = true;
      decode.Time = (uint)((receivedAt - DateTime.UtcNow.Date).TotalMilliseconds);
      decode.Snr = int.Parse(message.Substring(7, 3));
      decode.OffsetTimeSeconds = float.Parse(message.Substring(11, 4));
      decode.OffsetFrequencyHz = uint.Parse(message.Substring(16, 4));
      decode.Mode = message[21].ToString();
      if (message.Length > 24) decode.Message = message.Substring(24, message.Length - 24).Trim();
      decode.LowConfidence = message.Length > 42 && message[42] == '?';
      decode.OffAir = false;

      return decode;
    }

    private void TokenizeMessage()
    {
      Tokens =
      [
        new DisplayToken($"{Decode.Snr,3:D}"),
        new DisplayToken($"{Decode.OffsetTimeSeconds,4:F1}"),
        new DisplayToken($"{Decode.OffsetFrequencyHz,4:D}"),
      ];

      string text = Decode.Message.PadRight(40);
      Tokens.AddRange(text[..35].Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(s => new DisplayToken(s)).ToList());

      Tokens.AddRange([
        new DisplayToken(text[36..37]),
        new DisplayToken(text[38..40])
       ]);

      var s = string.Join("", Tokens.Select(t => t.text));
    }

    public void SetColors(Ft4MessagesSettings settings)
    {
      if (Type == DecodedItemType.RxMessage)
      {
        // the first token is SNR
        Tokens[0].bgBrush = BrushFromSnr(Decode.Snr, settings.BkColors.Snr);

        // the last two tokens are "?" and Ap
        Tokens[Tokens.Count - 2].bgBrush = Decode.LowConfidence ?
          new SolidBrush(settings.BkColors.Ap) : Brushes.Transparent;
        Tokens[Tokens.Count - 1].bgBrush = Tokens[Tokens.Count - 1].text == "  " ?
          Brushes.Transparent : new SolidBrush(settings.BkColors.Ap);

        // CQ words
        for (int i = 2; i < Tokens.Count; i++)
          if (CqWords.Contains(Tokens[i].text))
            Tokens[i].bgBrush = new SolidBrush(settings.BkColors.CqWord);
      }
    }

    private static Brush BrushFromSnr(int? snr, Color color)
    {
      if (snr == null) return Brushes.Transparent;

      float snrFraction = 0.2f * Math.Min(1, (24 + (int)snr) / 24f);
      snrFraction = 0.99f - snrFraction;

      var hsl = ColorSpaceHelper.RGBtoHSL(color);
      hsl = new HSL(hsl.Hue, hsl.Saturation, snrFraction);
      return new SolidBrush(ColorSpaceHelper.HSLtoColor(hsl));
    }
  }
}