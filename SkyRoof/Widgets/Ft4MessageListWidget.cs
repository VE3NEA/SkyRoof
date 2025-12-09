using System.Timers;
using FontAwesome;
using VE3NEA;

namespace SkyRoof
{
  public partial class Ft4MessageListWidget : UserControl
  {
    public enum DecodedItemType { Separator, TxMessage, RxMessage };

    public class Ft4MessageEventArgs : EventArgs
    {
      public DecodedItem Item { get; }
      public Ft4MessageEventArgs(DecodedItem item) { Item = item; }
    }




    private readonly System.Timers.Timer freezeTimer = new System.Timers.Timer();
    private DecodedItem HotItem, ClickedItem;
    private Point lastMouseLocation;
    private bool AutoScrollMode = true;
    private bool Frozen;
    private float spaceWidth;

    private Brush SeparatorBkBrush;
    private Brush TxBkBrush;
    private Brush RxBkBrush;
    private Brush ToMeBkBrush;
    private Brush HotBkBrush;

    private DateTime lastSlotStart;
    private Font fontAwesome11;

    public event EventHandler<Ft4MessageEventArgs?>? MessageHover;
    public event EventHandler<Ft4MessageEventArgs>? MessageClick;


    public Ft4MessageListWidget()
    {
      InitializeComponent();

      freezeTimer.Interval = 2000;
      freezeTimer.Elapsed += FreezeTimer_Elapsed;

      listBox.ItemHeight += 3;
      listBox.MouseWheel += ListBox_MouseWheel;
      listBox.Scroll += ListBox_Scroll;
    }

    internal void ApplySettings(Ft4MessagesSettings settings)
    {
      // font
      Font messageFont = new Font(listBox.Font.Name, settings.FontSize);
      listBox.Font = messageFont;
      listBox.ItemHeight = messageFont.Height;
      listBox.ForeColor = settings.TextColor;

      // bg colors
      listBox.BackColor = settings.BkColors.Window;
      SeparatorBkBrush = new SolidBrush(settings.BkColors.Separator);
      TxBkBrush = new SolidBrush(settings.BkColors.TxMessage);
      RxBkBrush = new SolidBrush(settings.BkColors.Window);
      ToMeBkBrush = new SolidBrush(settings.BkColors.ToMe);
      HotBkBrush = new SolidBrush(settings.BkColors.Hot);

      fontAwesome11 = FontAwesomeFactory.Create(11);

      // callsign colors
      foreach (DecodedItem item in listBox.Items) item.SetColors();
    }


    //--------------------------------------------------------------------------------------------------------------
    //                                                mouse
    //--------------------------------------------------------------------------------------------------------------
    private void ListBox_MouseMove(object? sender, MouseEventArgs e)
    {
      if (e.Location == lastMouseLocation) return;
      lastMouseLocation = e.Location;

      // freeze
      Freeze(true);

      // hot tracking
      var newHotItem = GetItemUnderCursor();
      if (newHotItem != HotItem) listBox.Invalidate();
      HotItem = newHotItem;

      // hand cursor
      listBox.Cursor = HotItem != null && HotItem.IsClickable() ? Cursors.Hand : Cursors.Default;

      // tooltip, placemark and waterfall call
      toolTip1.Hide(listBox);

      if (HotItem == null)
      {
        ShowTooltip(null, null);
        OnMessageHover(null);
      }
      else if (HotItem.Type == DecodedItemType.Separator)
      {
        ShowSeparatorTooltip(HotItem);
        OnMessageHover(null);
      }
      else
      {
        ShowTooltip(HotItem.Parse.DECallsign, HotItem.GetTooltip());
        OnMessageHover(HotItem);
      }
    }

    private void ListBox_MouseWheel(object? sender, MouseEventArgs e)
    {
      if (listBox.Items.Count == 0) return;

      // suppress animated scrolling
      int maxTop = listBox.Items.Count - listBox.ClientSize.Height / listBox.ItemHeight;
      int delta = 0;
      if (e.Delta < 0 && listBox.TopIndex < maxTop) delta = Math.Min(SystemInformation.MouseWheelScrollLines, maxTop - listBox.TopIndex);
      else if (e.Delta > 0 && listBox.TopIndex > 0) delta = -Math.Min(SystemInformation.MouseWheelScrollLines, listBox.TopIndex);

      if (delta != 0)
      {
        listBox.TopIndex += delta;
        UpdateAutoScrollMode();
      }

        ((HandledMouseEventArgs)e).Handled = true;
    }

    private void ListBox_MouseLeave(object? sender, EventArgs e)
    {
      Freeze(false);
      HotItem = null;
      OnMessageHover(null);
      listBox.Invalidate();
    }

    private void ListBox_MouseDown(object sender, MouseEventArgs e)
    {
      ClickedItem = GetItemUnderCursor();
//      callsignMenuStrip1.ClickedCallsign = ClickedItem?.Call ?? "";
//      callsignMenuStrip1.ClickedMessage = ClickedItem?.GetText() ?? "";

      if (ClickedItem == null) return;
      if (e.Button != MouseButtons.Left) return;

      bool ctrl = (ModifierKeys & Keys.Control) > 0;
      OnMessageClick(ClickedItem);
    }

    private void listBox_MouseUp(object? sender, MouseEventArgs e)
    {
      Freeze(false);
    }



    //--------------------------------------------------------------------------------------------------------------
    //                                                scroll
    //--------------------------------------------------------------------------------------------------------------
    // scrolled by clicking on a scroll bar
    private void ListBox_Scroll(object? sender, EventArgs e)
    {
      UpdateAutoScrollMode();
    }

    // auto scroll mode is updated when the user scrolls the listbox manually,
    // using a mouse wheel or a scroll bar
    private void UpdateAutoScrollMode()
    {
      int visibleItemCount = listBox.ClientSize.Height / listBox.ItemHeight;
      AutoScrollMode = listBox.TopIndex >= listBox.Items.Count - visibleItemCount;
    }

    private void Freeze(bool value)
    {
      Frozen = value;

      freezeTimer.Stop();

      if (Frozen)
        freezeTimer.Start();
      else if (AutoScrollMode)
        Invoke(listBox.ScrollToBottom);
    }

    private void FreezeTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
      Freeze(false);
    }

    internal void CheckAndScrollToBottom()
    {
      if (AutoScrollMode && !Frozen) listBox.ScrollToBottom();
    }



    //--------------------------------------------------------------------------------------------------------------
    //                                                items
    //--------------------------------------------------------------------------------------------------------------
    private DecodedItem GetItemUnderCursor()
    {
      Point p = listBox.PointToClient(Cursor.Position);
      int index = listBox.IndexFromPoint(p);
      if (index < 0) return null;
      return (DecodedItem)listBox.Items[index];
    }

    internal void BeginUpdateItems()
    {
      listBox.BeginUpdate();
    }

    internal void EndUpdateItems()
    {
      DeleteOldItems();
      CheckAndScrollToBottom();
      listBox.EndUpdate();
      listBox.Refresh();
    }

    public const int MAX_LINE_COUNT = 5000;

    internal void DeleteOldItems()
    {
      while (listBox.Items.Count > MAX_LINE_COUNT)
        listBox.Items.RemoveAt(0);
    }

    internal void AddItem(DecodedItem item)
    {
      if (!Visible) return;

      CheckAndAddSeparator(item);
      listBox.Items.Add(item);
    }

    protected virtual void CheckAndAddSeparator(DecodedItem item)
    {
      if (item.Utc == lastSlotStart) return;
      lastSlotStart = item.Utc;

//      var separator = ctx.DecodedMessages.CreateSeparatorItem(lastSlotStart, item.Odd);
//      listBox.Items.Add(separator);
    }

    internal DecodedItem? FindMessage(int slotNumber, int audioFrequency)
    {
      foreach (DecodedItem item in listBox.Items)
      {
        if (
          item.Type == DecodedItemType.RxMessage &&
          item.SlotNumber == slotNumber &&
          audioFrequency >= item.Decode.OffsetFrequencyHz &&
          audioFrequency <= item.Decode.OffsetFrequencyHz + Ft4Decoder.FT4_SIGNAL_BANDWIDTH
          )
          return item;
      }

      return null;
    }


    //--------------------------------------------------------------------------------------------------------------
    //                                                paint
    //--------------------------------------------------------------------------------------------------------------
    private void listBox_DrawItem(object sender, DrawItemEventArgs e)
    {
      var underlinedFont = new Font(e.Font, FontStyle.Underline);

      if (e.Index < 0) return;
      var item = (DecodedItem)listBox.Items[e.Index];
      spaceWidth = e.Graphics.MeasureString("__", e.Font).Width - e.Graphics.MeasureString("_", e.Font).Width;
      PointF p = new PointF(e.Bounds.Location.X, e.Bounds.Location.Y);

      // background
      Brush bgBrush = RxBkBrush;
      if (item.ToMe == true) bgBrush = ToMeBkBrush;
      else if (item.Type == DecodedItemType.Separator) bgBrush = SeparatorBkBrush;
      else if (item.Type == DecodedItemType.TxMessage) bgBrush = TxBkBrush;
      e.Graphics.FillRectangle(bgBrush, e.Bounds);

      // hot item
      if (item == HotItem) e.Graphics.FillRectangle(HotBkBrush, e.Bounds);

      // print each token
      foreach(var token in item.Tokens)
      {
        Font font = e.Font;
        if (token.Underlined) font = underlinedFont;
        else if (token.text == FontAwesomeIcons.Circle || token.text == FontAwesomeIcons.CircleQuestion) font = fontAwesome11;

        SizeF size = e.Graphics.MeasureString(token.text, font);
        RectangleF rect = new RectangleF(p, size);

        e.Graphics.FillRectangle(token.bgBrush, rect);
        e.Graphics.DrawString(token.text, font, token.fgBrush, p);

        p.X += size.Width;
        if (token.AppendSpace) p.X += spaceWidth;
      }
    }

    private void ShowTooltip(string? title, string? tooltip)
    {
      if (toolTip1.GetToolTip(listBox) == tooltip)
        return;

      if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(tooltip))
        toolTip1.SetToolTip(listBox, "");

      else
      {
        toolTip1.SetToolTip(listBox, tooltip);
        toolTip1.ToolTipTitle = title;
      }
    }

    private void ShowSeparatorTooltip(DecodedItem hotItem)
    {
      string title = (hotItem.Odd ? "Odd (2-nd)" : "Even (1-st)");
      string tooltip = string.Format("{0:%h} hours {0:%m} minutes ago", DateTime.UtcNow - hotItem.Utc);
      ShowTooltip(title, tooltip);
    }

    private void OnMessageHover(DecodedItem? item)
    {
      MessageHover?.Invoke(this, new Ft4MessageEventArgs(item));
    }

    private void OnMessageClick(DecodedItem? item)
    {
      MessageClick?.Invoke(this, new Ft4MessageEventArgs(item));
    }

  }
}


/*
000000  -7  0.2 2397 +  CQ SM/UA1CBX
000000 -11  0.1 1531 +  CQ VE3NEA FN03


|aP|Message components
|a1|CQ   &#160; &#160;   ?   &#160; &#160;   ? 
|a2|MyCall &#160; &#160; ?   &#160; &#160;   ? 
|a3|MyCall DxCall &#160; &#160;  ? 
|a4|MyCall DxCall RRR
|a5|MyCall DxCall 73
|a6|MyCall DxCall RR73
|a7|(Call_1 or CQ) Call_2 &#160; &#160;   ?  // only ft8

.FT4 and FT8 AP decoding types for each QSO state
|State        |AP type
|CALLING STN  |   2, 3
|REPORT       |   2, 3
|ROGER_REPORT |   3, 4, 5, 6
|ROGERS       |   3, 4, 5, 6
|SIGNOFF      |   3, 1, 2
|CALLING CQ   |   1, 2
 */