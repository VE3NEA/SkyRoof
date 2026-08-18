using System.ComponentModel;
using SkyRoof;

namespace VE3NEA
{
  // A ToolTip that follows the theme. The framework ignores BackColor/ForeColor on a plain ToolTip
  // under visual styles, but honors both in the owner-draw path, so this subclass sets the colors
  // and delegates the drawing back to the framework.
  //
  // Measured by reading the pixels back out of the tooltip's own DC (plan 4.5b): the classic Info
  // color paints as a murky #50503C in dark mode, while ControlLight gives #2E2E2E. The border
  // comes from SystemColors.WindowFrame and flips on its own.
  public class ToolTipEx : ToolTip
  {
    public ToolTipEx() : base()
    {
      Initialize();
    }

    public ToolTipEx(IContainer cont) : base(cont)
    {
      Initialize();
    }

    private void Initialize()
    {
      OwnerDraw = true;
      BackColor = Theme.TipBack;
      ForeColor = Theme.TipText;
      Draw += DrawTooltip;
    }

    // ToolTip.OnDraw is not virtual, so the subclass subscribes to its own Draw event
    private void DrawTooltip(object? sender, DrawToolTipEventArgs e)
    {
      e.DrawBackground();
      e.DrawBorder();

      // DrawText paints the body text only. The native control still reserves room for the title,
      // so without this a titled tooltip would show its headline as an empty strip
      if (string.IsNullOrEmpty(ToolTipTitle))
      {
        e.DrawText();
        return;
      }

      const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.NoPrefix;
      using var titleFont = new Font(e.Font, FontStyle.Bold);

      var rect = e.Bounds;
      rect.Inflate(-3, -2);
      var titleHeight = TextRenderer.MeasureText(e.Graphics, ToolTipTitle, titleFont, rect.Size, flags).Height;

      TextRenderer.DrawText(e.Graphics, ToolTipTitle, titleFont, rect, ForeColor, flags);
      rect.Y += titleHeight;
      rect.Height -= titleHeight;
      TextRenderer.DrawText(e.Graphics, e.ToolTipText, e.Font, rect, ForeColor, flags);
    }
  }
}
