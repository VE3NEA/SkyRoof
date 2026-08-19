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

      // e.DrawText() runs the lines of a multi-line tooltip together into a single clipped one,
      // so the text is drawn here instead. Without a title the inset is measured off the native
      // rendering - two pixels in on the sides, three above the top of the bounds - which lands
      // the lines where the framework itself used to put them.
      const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.NoPrefix;
      var rect = e.Bounds;
      rect.Inflate(-2, 3);

      // the native control reserves room for the title but paints none of it, so without this a
      // titled tooltip would show its headline as an empty strip
      if (!string.IsNullOrEmpty(ToolTipTitle))
      {
        rect = e.Bounds;
        rect.Inflate(-3, -2);
        using var titleFont = new Font(e.Font, FontStyle.Bold);
        var titleHeight = TextRenderer.MeasureText(e.Graphics, ToolTipTitle, titleFont, rect.Size, flags).Height;
        TextRenderer.DrawText(e.Graphics, ToolTipTitle, titleFont, rect, ForeColor, flags);
        rect.Y += titleHeight;
        rect.Height -= titleHeight;
      }

      TextRenderer.DrawText(e.Graphics, e.ToolTipText, e.Font, rect, ForeColor, flags);
    }
  }
}
