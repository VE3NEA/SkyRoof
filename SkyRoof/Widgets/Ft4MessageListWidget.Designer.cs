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
      contextMenuStrip1 = new ContextMenuStrip(components);
      ClearMNU = new ToolStripMenuItem();
      ScrollMNU = new ToolStripMenuItem();
      toolStripMenuItem1 = new ToolStripSeparator();
      FindOnQrzMNU = new ToolStripMenuItem();
      FindOnGoogleMNU = new ToolStripMenuItem();
      toolTip1 = new ToolTip(components);
      contextMenuStrip1.SuspendLayout();
      SuspendLayout();
      // 
      // listBox
      // 
      listBox.ContextMenuStrip = contextMenuStrip1;
      listBox.Dock = DockStyle.Fill;
      listBox.DrawMode = DrawMode.OwnerDrawFixed;
      listBox.Font = new Font("Courier New", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
      listBox.FormattingEnabled = true;
      listBox.IntegralHeight = false;
      listBox.ItemHeight = 13;
      listBox.Location = new Point(1, 1);
      listBox.Name = "listBox";
      listBox.SelectionMode = SelectionMode.None;
      listBox.Size = new Size(403, 512);
      listBox.TabIndex = 4;
      listBox.Scroll += ListBox_Scroll;
      listBox.DrawItem += listBox_DrawItem;
      listBox.MouseDown += ListBox_MouseDown;
      listBox.MouseLeave += ListBox_MouseLeave;
      listBox.MouseMove += ListBox_MouseMove;
      listBox.MouseUp += listBox_MouseUp;
      // 
      // contextMenuStrip1
      // 
      contextMenuStrip1.Items.AddRange(new ToolStripItem[] { ClearMNU, ScrollMNU, toolStripMenuItem1, FindOnQrzMNU, FindOnGoogleMNU });
      contextMenuStrip1.Name = "contextMenuStrip1";
      contextMenuStrip1.Size = new Size(181, 120);
      contextMenuStrip1.Opening += contextMenuStrip1_Opening;
      // 
      // ClearMNU
      // 
      ClearMNU.Name = "ClearMNU";
      ClearMNU.Size = new Size(180, 22);
      ClearMNU.Text = "Clear";
      ClearMNU.Click += ClearMNU_Click;
      // 
      // ScrollMNU
      // 
      ScrollMNU.Name = "ScrollMNU";
      ScrollMNU.Size = new Size(180, 22);
      ScrollMNU.Text = "Scroll to Bottom";
      ScrollMNU.Click += ScrollMNU_Click;
      // 
      // toolStripMenuItem1
      // 
      toolStripMenuItem1.Name = "toolStripMenuItem1";
      toolStripMenuItem1.Size = new Size(177, 6);
      // 
      // FindOnQrzMNU
      // 
      FindOnQrzMNU.Name = "FindOnQrzMNU";
      FindOnQrzMNU.Size = new Size(180, 22);
      FindOnQrzMNU.Text = "Find on QRZ";
      FindOnQrzMNU.Click += FindOnQrzMNU_Click;
      // 
      // FindOnGoogleMNU
      // 
      FindOnGoogleMNU.Name = "FindOnGoogleMNU";
      FindOnGoogleMNU.Size = new Size(180, 22);
      FindOnGoogleMNU.Text = "Find on Google";
      FindOnGoogleMNU.Click += FindOnGoogleMNU_Click;
      // 
      // Ft4MessageListWidget
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      Controls.Add(listBox);
      Name = "Ft4MessageListWidget";
      Padding = new Padding(1);
      Size = new Size(405, 514);
      contextMenuStrip1.ResumeLayout(false);
      ResumeLayout(false);
    }

    #endregion
    private ToolTip toolTip1;
    public VE3NEA.ListBoxEx listBox;
    private ContextMenuStrip contextMenuStrip1;
    private ToolStripMenuItem ClearMNU;
    private ToolStripMenuItem ScrollMNU;
    private ToolStripSeparator toolStripMenuItem1;
    private ToolStripMenuItem FindOnQrzMNU;
    private ToolStripMenuItem FindOnGoogleMNU;
  }
}
