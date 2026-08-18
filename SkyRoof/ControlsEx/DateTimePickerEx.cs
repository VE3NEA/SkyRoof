using System.Runtime.InteropServices;
using SkyRoof;

namespace VE3NEA
{
  // A DateTimePicker that asks for the dark common-control theme, the way TextBoxEx does.
  //
  // Measured, and it does NOT work: the field stays white in dark mode. "DarkMode_CFD",
  // "DarkMode_Explorer", and either of them followed by a WM_THEMECHANGED were all tried on the
  // QsoEntryPanel picker and none changed a pixel - SysDateTimePick32 paints its client area from
  // its own theme data and ignores the dark theme class, the same dead end as the MonthCalendar in
  // plan 4.5a. The call is left in place because it costs nothing and is correct for the day the
  // control starts honoring it; making the field dark before then needs the control overpainted in
  // WndProc, which is a different mechanism and a bigger change.
  //
  // The drop-down calendar is a window of its own and stays light either way. Its colors can be set
  // through CalendarMonthBackground / CalendarForeColor if that is ever worth doing.
  public class DateTimePickerEx : DateTimePicker
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
