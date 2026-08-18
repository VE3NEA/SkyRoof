using System.Runtime.InteropServices;
using SkyRoof;

namespace VE3NEA
{
  // A TextBox whose border matches the other editors in the dark theme. .NET 10 draws a near-white
  // #ECECEC border around a plain TextBox while giving the ComboBox next to it #9B9B9B; asking for
  // the dark common-control theme gives the TextBox the same #9B9B9B.
  //
  // Measured with design-docs/theme-tools/borderprobe. The call is guarded because in the light
  // theme it also darkens the interior to #383838 - it is a dark-mode theme, not a border setting.
  public class TextBoxEx : TextBox
  {
    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hWnd, string? subAppName, string? subIdList);

    protected override void OnHandleCreated(EventArgs e)
    {
      base.OnHandleCreated(e);
      if (Theme.IsDark) SetWindowTheme(Handle, "DarkMode_CFD", null);
    }
  }
}
