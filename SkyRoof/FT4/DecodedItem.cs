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
    public class DisplayToken
    {
      public readonly string text;
      public bool Underlined;
      public Brush bgBrush = Brushes.Transparent;
      public Brush fgBrush = Brushes.Black;
      public bool AppendSpace;

      public DisplayToken(string text)
      {
        this.text = text;
      }
    }

    public DecodedItemType Type;
    public int SlotNumber;

    public WkdStatus WkdStatus;
    public bool ToMe;
    public bool Odd => SlotNumber % 2 == 1;

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

    /*
       -15  0.1 2229 ~  CQ AC3DH FM29                       ? a1
000000  -7  0.2 2397 +  CQ SM/UA1CBX
000000 -11  0.1 1531 +  CQ VE3NEA FN03
<     >< ><        >    <                                  >^ <>  
    */

    private void TokenizeMessage()
    {      
      // FIX BUG: Decode.Message is just message, all other params are properties of Decode

      Tokens = new List<DisplayToken>();
      string text = Decode.Message.PadRight(64);

      Tokens.Add(new DisplayToken(text[7..9]));
      Tokens.Add(new DisplayToken(text[10..19]));
      Tokens.AddRange(text[24..60].Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(s => new DisplayToken(s)).ToList());
      Tokens.Add(new DisplayToken(text[60..60]));
      Tokens.Add(new DisplayToken(text[62..63]));

      //text[64] = '?', text[66-67] = 'a1'

      //Tokens[1].bgBrush = BrushFromSnr(msg.Tokens[1].text, Color.Red);

      SetColors();
    }

    public void SetColors()
    {
      Tokens[0].bgBrush = BrushFromSnr(Decode.Snr, Color.Red);

      HighlightWords();

      Tokens[Tokens.Count - 1].bgBrush = Decode.LowConfidence ? Brushes.Orange : Brushes.Transparent;
      Tokens[Tokens.Count - 2].bgBrush = Tokens[Tokens.Count - 2].text == "  " ?
        Brushes.Transparent : Brushes.LightBlue;
      
      foreach (var token in Tokens)
        ;
    }

    private readonly string[] CqWords = new string[] { "CQ", "73", "RR73" };

    private void HighlightWords()
    {
      for (int i = 2; i < Tokens.Count; i++)
        if (CqWords.Contains(Tokens[i].text))
          Tokens[i].bgBrush = Brushes.Yellow;
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