using System.Timers;
using FontAwesome;
using VE3NEA;

namespace SkyRoof
{
  public class Ft4MessageEventArgs : EventArgs
  {
    public DecodedItem Item { get; }
    public Ft4MessageEventArgs(DecodedItem item) { Item = item; }
  }


  public partial class Ft4MessageListWidget : UserControl
  {
    private readonly System.Timers.Timer freezeTimer = new System.Timers.Timer();
    public DecodedItem HotItem, ClickedItem;
    private Point lastMouseLocation;
    private bool AutoScrollMode = true;
    private bool Frozen;
    private float spaceWidth;

    private Brush SeparatorBkBrush;
    private Brush TxBkBrush;
    private Brush RxBkBrush;
    private Brush ToMeBkBrush;
    private Brush FromMeBkBrush;
    private Brush HotBkBrush;

    private Font fontAwesome11;

    public string MyCall;

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

    internal void ApplySettings(Settings settings)
    {
      MyCall = settings.User.Call;

      var sett = settings.Ft4Console.Messages;
      // font
      Font messageFont = new Font(listBox.Font.Name, sett.FontSize);
      listBox.Font = messageFont;
      listBox.ItemHeight = messageFont.Height;
      listBox.ForeColor = sett.TextColor;

      // bg colors
      listBox.BackColor = sett.BkColors.Window;
      SeparatorBkBrush = new SolidBrush(sett.BkColors.Separator);
      TxBkBrush = new SolidBrush(sett.BkColors.TxMessage);
      RxBkBrush = new SolidBrush(sett.BkColors.Window);
      ToMeBkBrush = new SolidBrush(sett.BkColors.ToMe);
      FromMeBkBrush = new SolidBrush(sett.BkColors.FromMe);
      HotBkBrush = new SolidBrush(sett.BkColors.Hot);

      fontAwesome11 = FontAwesomeFactory.Create(11);
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
      bool clickable = HotItem?.IsClickable() == true && HotItem.Parse.DECallsign != MyCall;
      listBox.Cursor = clickable ? Cursors.Hand : Cursors.Default;

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
      else if (HotItem.Type == DecodedItemType.RxMessage)
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
      if (e.Button != MouseButtons.Left) return;

      ClickedItem = GetItemUnderCursor();
      if (ClickedItem?.IsClickable() == true) 
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

      BackColor = Frozen ? Color.Blue : SystemColors.Control;
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

    // message from current or new slot: append
    // message from old slot: instert
    private int FindInsertionPoint(DecodedItem item)
    {
      int count = listBox.Items.Count;

      for (int i = count; i > 0; i--)
        if (((DecodedItem)listBox.Items[i - 1]).SlotNumber <= item.SlotNumber)
          return i;

      return 0;
    }

    public void AddMessage(DecodedItem item)
    {
      int index = FindInsertionPoint(item);
        listBox.Items.Insert(index, item); // todo: remove IF
    }

    // sending message 
    public void AddTxMessage(DecodedItem item)
    {
      BeginUpdateItems();
      listBox.Items.Add(item);
      EndUpdateItems();
    }

    // previously sent message has been received back from the sat
    public void AddMessageFromMe(DecodedItem item)
    {
      for (int i = listBox.Items.Count - 1; i >= 0; i--)
      {
        // replace TX message if present
        var itm = (DecodedItem)listBox.Items[i];
        if (itm.SlotNumber < item.SlotNumber) break;
        else if (itm.Type == DecodedItemType.TxMessage && itm.SlotNumber == item.SlotNumber)
        {
          listBox.Items[i] = item; 
          return;
        }
      }

      // add new RX message
      AddMessage(item);
    }

    internal void AddSeparator(DecodedItem separator)
    {
      int index = FindInsertionPoint(separator);

      // separator already present
      if (index > 0 && ((DecodedItem)listBox.Items[index - 1]).SlotNumber == separator.SlotNumber) return;

      // merge with older separators
      if (index >= 2 &&
        ((DecodedItem)listBox.Items[index - 1]).Type == DecodedItemType.Separator &&
        ((DecodedItem)listBox.Items[index - 2]).Type == DecodedItemType.Separator)

        if (((DecodedItem)listBox.Items[index - 2]).Tokens[0].text[0] == '.')
        {
          var interval = separator.Utc - ((DecodedItem)listBox.Items[index - 2]).Utc;// + TimeSpan.FromSeconds(NativeFT4Coder.TIMESLOT_SECONDS);
          var token = ((DecodedItem)listBox.Items[index - 2]).Tokens[0];
          token.text = $"... ({Utils.TimespanToString(interval)})";
          listBox.Items[index - 1] = separator;
        }
        else
        {
          ((DecodedItem)listBox.Items[index - 1]).Tokens = [new("... (7s)")];
          listBox.Items.Insert(index, separator);
        }

      // add new
      else
        listBox.Items.Insert(index, separator);
    }

    internal DecodedItem? FindItem(long slotNumber, int audioFrequency)
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




    //--------------------------------------------------------------------------------------------------------------
    //                                                paint
    //--------------------------------------------------------------------------------------------------------------
    private void listBox_DrawItem(object sender, DrawItemEventArgs e)
    {
      if (e.Index < 0) return;
      var item = (DecodedItem)listBox.Items[e.Index];
      spaceWidth = e.Graphics.MeasureString("__", e.Font).Width - e.Graphics.MeasureString("_", e.Font).Width;
      PointF p = new PointF(e.Bounds.Location.X, e.Bounds.Location.Y);

      // background
      Brush bgBrush = RxBkBrush;
      if (item.ToMe) bgBrush = ToMeBkBrush;
      else if (item.FromMe) bgBrush = FromMeBkBrush;
      else if (item.Type == DecodedItemType.RxMessage) bgBrush = RxBkBrush;
      else if (item.Type == DecodedItemType.Separator) bgBrush = SeparatorBkBrush;
      else if (item.Type == DecodedItemType.TxMessage) bgBrush = TxBkBrush;
      e.Graphics.FillRectangle(bgBrush, e.Bounds);

      // hot item
      if (item == HotItem)
        e.Graphics.FillRectangle(HotBkBrush, e.Bounds);

      // print each token
      foreach (var token in item.Tokens)
      {
        Font font = e.Font;
        if (token.Underlined) font = new Font(e.Font, FontStyle.Underline);
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
      string tooltip = string.Format("slot {0} {1:%h} hours {1:%m} minutes ago", hotItem.SlotNumber, DateTime.UtcNow - hotItem.Utc);
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

    internal void HighlightCallsign(HighlightCallsignEventArgs e)
    {
      if (DecodedItem.CqWords.Contains(e.Callsign)) return;

      for (int i = listBox.Items.Count - 1; i >= 0; i--)
      {
        var item = (DecodedItem)listBox.Items[i];

        if (item.Type != DecodedItemType.RxMessage)
          break;
        else if (item.Parse.DECallsign != e.Callsign || item.FromMe)
          continue;
        else
        {
          var token = item.Tokens.FirstOrDefault(t => t.text == e.Callsign);
          if (token == null) continue;
          token.bgBrush = new SolidBrush(e.BackColor);
          token.fgBrush = new SolidBrush(e.ForeColor);
          listBox.Invalidate();
        }
      }
    }

    private void ClearMNU_Click(object sender, EventArgs e)
    {
      listBox.Items.Clear();
    }

    private void ScrollMNU_Click(object sender, EventArgs e)
    {
      listBox.ScrollToBottom();
    }
  }
}

/*

|aP|Message components
|a1|CQ   &#160; &#160;   ?   &#160; &#160;   ? 
|a2|MyCall &#160; &#160; ?   &#160; &#160;   ? 
|a3|MyCall DxCall &#160; &#160;  ? 
|a4|MyCall DxCall RRR
|a5|MyCall DxCall 73
|a6|MyCall DxCall RR73

 */