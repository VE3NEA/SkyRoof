namespace SkyRoof
{
  partial class Ft4MessageListWidget
  {
    /// <summary> 
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary> 
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      components = new System.ComponentModel.Container();
      listBox = new VE3NEA.ListBoxEx();
      toolTip1 = new ToolTip(components);
      SuspendLayout();
      // 
      // listBox
      // 
      listBox.Dock = DockStyle.Fill;
      listBox.DrawMode = DrawMode.OwnerDrawFixed;
      listBox.Font = new Font("Courier New", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
      listBox.FormattingEnabled = true;
      listBox.IntegralHeight = false;
      listBox.ItemHeight = 13;
      listBox.Location = new Point(0, 0);
      listBox.Name = "listBox";
      listBox.SelectionMode = SelectionMode.None;
      listBox.Size = new Size(405, 514);
      listBox.TabIndex = 4;
      listBox.Scroll += ListBox_Scroll;
      listBox.DrawItem += listBox_DrawItem;
      listBox.MouseDown += ListBox_MouseDown;
      listBox.MouseLeave += ListBox_MouseLeave;
      listBox.MouseMove += ListBox_MouseMove;
      listBox.MouseUp += listBox_MouseUp;
      // 
      // Ft4MessageListWidget
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      Controls.Add(listBox);
      Name = "Ft4MessageListWidget";
      Size = new Size(405, 514);
      ResumeLayout(false);
    }

    #endregion

    private VE3NEA.ListBoxEx listBox;
    private ToolTip toolTip1;
  }
}
