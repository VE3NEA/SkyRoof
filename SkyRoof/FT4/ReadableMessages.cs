using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WsjtxUtils.WsjtxMessages;
using WsjtxUtils.WsjtxMessages.Messages;

namespace SkyRoof
{
  public class ReadableHighlightCallsign : HighlightCallsign, IWsjtxDirectionOut
  {
    public Color BackColor { get => QColorToColor(BackgroundColor); }
    public Color ForeColor { get => QColorToColor(ForegroundColor); }

    public override void ReadMessage(WsjtxMessageReader reader)
    {
      base.ReadMessage(reader);

      Callsign = reader.ReadString();
      BackgroundColor = ReadQColor(reader);
      ForegroundColor = ReadQColor(reader);
      HighlightLast = reader.ReadBool();
    }

    public static QColor ReadQColor(WsjtxMessageReader reader)
    {
      reader.ReadEnum<QColorSpec>(); // spec
      ushort alpha = reader.ReadUInt16();
      ushort red = reader.ReadUInt16();
      ushort green = reader.ReadUInt16();
      ushort blue = reader.ReadUInt16();
      reader.ReadUInt16(); // padding

      Color color = ColorFromUshorts(alpha, red, green, blue);
      return new QColor(color);
    }

    private static Color ColorFromUshorts(ushort alpha, ushort red, ushort green, ushort blue)
    {
      return Color.FromArgb(              
        (byte)(alpha >> 8),
        (byte)(red >> 8),
        (byte)(green >> 8),
        (byte)(blue >> 8)
        );
    }

    private static Color QColorToColor(QColor qColor)
    {
      return ColorFromUshorts(qColor.Alpha, qColor.Red, qColor.Green, qColor.Blue);
    }
  }
}
