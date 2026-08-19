using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SkyRoof;

namespace VE3NEA
{
  public class ListViewEx : ListView
  {
    //--------------------------------------------------------------------------------------------------------------
    //                 prevent flicker: https://stackoverflow.com/questions/2751686
    //--------------------------------------------------------------------------------------------------------------
    // WinForms' ControlStyles.OptimizedDoubleBuffer | AllPaintingInWmPaint conflicts with the native
    // ListView owner-draw dispatch and causes some items to be skipped on paint (especially after
    // scrolling/resize). Use the native LVS_EX_DOUBLEBUFFER instead - it eliminates flicker without
    // breaking owner-draw.
    private const int WM_ERASEBKGND = 0x14;
    private const int LVM_FIRST = 0x1000;
    private const int LVM_SETEXTENDEDLISTVIEWSTYLE = LVM_FIRST + 54;
    private const int LVS_EX_DOUBLEBUFFER = 0x00010000;

    public ListViewEx()
    {
      SetStyle(ControlStyles.EnableNotifyMessage, true);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
      base.OnHandleCreated(e);
      SendMessage(Handle, LVM_SETEXTENDEDLISTVIEWSTYLE, LVS_EX_DOUBLEBUFFER, LVS_EX_DOUBLEBUFFER);
    }

    protected override void OnNotifyMessage(Message m)
    {
      if (m.Msg != WM_ERASEBKGND) base.OnNotifyMessage(m);
    }




    //--------------------------------------------------------------------------------------------------------------
    //           hide horizontal scroollbar: https://stackoverflow.com/questions/2488622
    //--------------------------------------------------------------------------------------------------------------
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, int dwNewLong);

    const int WM_NCCALCSIZE = 0x83;
    const int GWL_STYLE = -16;
    const int WS_HSCROLL = 0x00100000;

    protected override void WndProc(ref Message m)
    {
      if (HandleGroupCustomDraw(ref m)) return;

      if (m.Msg == WM_NCCALCSIZE)
      {
        int style = (int)GetWindowLongPtr(Handle, GWL_STYLE);
        if ((style & WS_HSCROLL) == WS_HSCROLL)
          SetWindowLongPtr(Handle, GWL_STYLE, style & ~WS_HSCROLL);
      }

      base.WndProc(ref m);
    }




    //--------------------------------------------------------------------------------------------------------------
    //                     set row height: https://stackoverflow.com/questions/6563863
    //--------------------------------------------------------------------------------------------------------------
    public void SetRowHeight(int height)
    {
      SmallImageList = new ImageList();
      SmallImageList.ImageSize = new Size(1, height);
    }




    //--------------------------------------------------------------------------------------------------------------
    //                   set tooltip delay: https://stackoverflow.com/questions/4899687 
    //--------------------------------------------------------------------------------------------------------------
    [DllImport("user32.dll")]
    static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
    const int LVM_GETTOOLTIPS = 0x104E;
    const int TTM_SETDELAYTIME = 0x403;
    const int TTDT_AUTOMATIC = 0;
    const int TTDT_AUTOPOP = 2;
    const int TTDT_INITIAL = 3;

    public void SetTooltipDelay(int delayMs)
    {
      var tooltip = SendMessage(Handle, LVM_GETTOOLTIPS, 0, 0);
      SendMessage(tooltip, TTM_SETDELAYTIME, TTDT_AUTOMATIC, delayMs);
    }




    //--------------------------------------------------------------------------------------------------------------
    //              resize columns depending on DPI: https://stackoverflow.com/questions/10795134
    //--------------------------------------------------------------------------------------------------------------
    protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
    {
      base.ScaleControl(factor, specified);

      foreach (ColumnHeader column in Columns)
        column.Width = (int)(column.Width * factor.Width);
    }




    //--------------------------------------------------------------------------------------------------------------
    //                                           group header text color
    //--------------------------------------------------------------------------------------------------------------
    // The group header is painted by the theme, in a blue that the dark surface swallows, and no
    // message sets its color: even the clrText of the custom draw notification is ignored there.
    // So the header is painted here instead - the text and the rule that trails it, measured off
    // the native rendering, so that the light theme still looks the way it always did.
    //
    // ListView handles the notification itself, but reads dwItemSpec as an item index, which is a
    // group id here, so the group stages are answered before base.WndProc sees them. The group is
    // announced at the CDDS_PREPAINT stage, and rcText, not rc, is the header band: rc spans the
    // whole group, items included.
    const int WM_REFLECT = 0x2000;
    const int WM_NOTIFY = 0x4E;
    const int OCM_NOTIFY = WM_REFLECT + WM_NOTIFY;
    const int NM_CUSTOMDRAW = -12;
    const int CDDS_PREPAINT = 1;
    const int CDRF_DODEFAULT = 0;
    const int CDRF_SKIPDEFAULT = 4;
    const int LVCDI_GROUP = 1;
    const int LVM_GETGROUPINFO = LVM_FIRST + 149;
    const int LVGF_HEADER = 1;

    const TextFormatFlags HeaderFormat = TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
      TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis;

    // NMHDR is a struct of its own so that its tail padding matches the native layout
    [StructLayout(LayoutKind.Sequential)]
    private struct NmHdr
    {
      public IntPtr HwndFrom;
      public IntPtr IdFrom;
      public int Code;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NmLvCustomDraw
    {
      // NMCUSTOMDRAW
      public NmHdr Hdr;
      public int DrawStage;
      public IntPtr Hdc;
      public int Left, Top, Right, Bottom;
      public IntPtr ItemSpec;
      public int ItemState;
      public IntPtr ItemLParam;
      // NMLVCUSTOMDRAW
      public int ClrText;
      public int ClrTextBk;
      public int SubItem;
      public int ItemType;
      public int ClrFace;
      public int IconEffect;
      public int IconPhase;
      public int PartId;
      public int StateId;
      public int TextLeft, TextTop, TextRight, TextBottom;
      public uint Align;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct LvGroup
    {
      public uint CbSize;
      public uint Mask;
      public IntPtr PszHeader;
      public int CchHeader;
      public IntPtr PszFooter;
      public int CchFooter;
      public int GroupId;
      public uint StateMask;
      public uint State;
      public uint Align;
      public IntPtr PszSubtitle;
      public uint CchSubtitle;
      public IntPtr PszTask;
      public uint CchTask;
      public IntPtr PszDescriptionTop;
      public uint CchDescriptionTop;
      public IntPtr PszDescriptionBottom;
      public uint CchDescriptionBottom;
      public int TitleImage;
      public int ExtendedImage;
      public int FirstItem;
      public uint CItems;
      public IntPtr PszSubsetTitle;
      public uint CchSubsetTitle;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref LvGroup lParam);

    // true if the message was a group header custom draw notification and has been answered
    private bool HandleGroupCustomDraw(ref Message m)
    {
      if (m.Msg != OCM_NOTIFY || m.LParam == IntPtr.Zero) return false;
      if (Marshal.PtrToStructure<NmHdr>(m.LParam).Code != NM_CUSTOMDRAW) return false;

      var draw = Marshal.PtrToStructure<NmLvCustomDraw>(m.LParam);
      if (draw.ItemType != LVCDI_GROUP) return false;

      if (draw.DrawStage == CDDS_PREPAINT)
      {
        DrawGroupHeader(draw);
        m.Result = CDRF_SKIPDEFAULT;
      }
      else m.Result = CDRF_DODEFAULT;

      return true;
    }

    private void DrawGroupHeader(NmLvCustomDraw draw)
    {
      var rect = Rectangle.FromLTRB(draw.TextLeft, draw.TextTop, draw.TextRight, draw.TextBottom);
      string text = GetGroupHeader((int)draw.ItemSpec);

      using var graphics = Graphics.FromHdc(draw.Hdc);
      using var backBrush = new SolidBrush(BackColor);
      graphics.FillRectangle(backBrush, rect);

      // the text sits one pixel above the center of the band, and the rule one pixel below it
      var textRect = Rectangle.FromLTRB(rect.Left + LogicalToDeviceUnits(12), rect.Top - 1, rect.Right, rect.Bottom - 1);
      TextRenderer.DrawText(graphics, text, Font, textRect, Theme.ListGroupText, HeaderFormat);

      var textSize = TextRenderer.MeasureText(graphics, text, Font, rect.Size, HeaderFormat);
      using var pen = new Pen(Theme.ListGroupRule);
      int ruleY = rect.Top + rect.Height / 2 - 1;
      graphics.DrawLine(pen, textRect.Left + textSize.Width + 4, ruleY, rect.Right - 11, ruleY);
    }

    // the header text is read back from the native control: the group is identified by its id,
    // and ListViewGroup.ID, which maps an id to a Groups entry, is internal to the framework
    private string GetGroupHeader(int groupId)
    {
      const int MaxChars = 256;
      var buffer = Marshal.AllocHGlobal(MaxChars * 2);

      try
      {
        Marshal.WriteInt16(buffer, 0, 0);
        var group = new LvGroup
        {
          CbSize = (uint)Marshal.SizeOf<LvGroup>(),
          Mask = LVGF_HEADER,
          PszHeader = buffer,
          CchHeader = MaxChars,
          GroupId = groupId
        };

        if (SendMessage(Handle, LVM_GETGROUPINFO, groupId, ref group) == -1) return string.Empty;
        return Marshal.PtrToStringUni(buffer) ?? string.Empty;
      }
      finally
      {
        Marshal.FreeHGlobal(buffer);
      }
    }
  }
}
