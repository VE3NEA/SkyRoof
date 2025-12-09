namespace SkyRoof
{
  partial class Ft4ConsolePanel
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

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      splitContainer1 = new SplitContainer();
      AudioWaterfall = new AudioWaterfallWidget();
      MessageListWidget = new Ft4MessageListWidget();
      panel1 = new Panel();
      ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
      splitContainer1.Panel2.SuspendLayout();
      splitContainer1.SuspendLayout();
      SuspendLayout();
      // 
      // splitContainer1
      // 
      splitContainer1.Dock = DockStyle.Fill;
      splitContainer1.FixedPanel = FixedPanel.Panel1;
      splitContainer1.Location = new Point(0, 0);
      splitContainer1.Name = "splitContainer1";
      splitContainer1.Orientation = Orientation.Horizontal;
      // 
      // splitContainer1.Panel1
      // 
      splitContainer1.Panel1.Controls.Add(AudioWaterfall);
      // 
      // splitContainer1.Panel2
      // 
      splitContainer1.Panel2.Controls.Add(panel1);
      splitContainer1.Panel2.Controls.Add(MessageListWidget);
      splitContainer1.Size = new Size(800, 450);
      splitContainer1.SplitterDistance = 241;
      splitContainer1.TabIndex = 2;
      // 
      // AudioWaterfal
      // 
      AudioWaterfall.Dock = DockStyle.Fill;
      AudioWaterfall.Location = new Point(0, 0);
      AudioWaterfall.Name = "AudioWaterfal";
      AudioWaterfall.Size = new Size(800, 241);
      AudioWaterfall.TabIndex = 2;
      // 
      // MessageListWidget
      // 
      MessageListWidget.Dock = DockStyle.Fill;
      MessageListWidget.Font = new Font("Courier New", 11.25F);
      MessageListWidget.Location = new Point(0, 0);
      MessageListWidget.Margin = new Padding(4, 3, 4, 3);
      MessageListWidget.Name = "MessageListWidget";
      MessageListWidget.Size = new Size(800, 205);
      MessageListWidget.TabIndex = 3;
      // 
      // panel1
      // 
      panel1.Dock = DockStyle.Right;
      panel1.Location = new Point(549, 0);
      panel1.Name = "panel1";
      panel1.Size = new Size(251, 205);
      panel1.TabIndex = 4;
      // 
      // Ft4ConsolePanel
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(800, 450);
      Controls.Add(splitContainer1);
      Name = "Ft4ConsolePanel";
      Text = "FT4 Console";
      FormClosing += Ft4ConsolePanel_FormClosing;
      splitContainer1.Panel2.ResumeLayout(false);
      ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
      splitContainer1.ResumeLayout(false);
      ResumeLayout(false);
    }

    #endregion

    private SplitContainer splitContainer1;
    private AudioWaterfallWidget AudioWaterfall;
    private Ft4MessageListWidget MessageListWidget;
    private Panel panel1;
  }
}