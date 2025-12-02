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
      richTextBox1 = new RichTextBox();
      ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
      splitContainer1.Panel1.SuspendLayout();
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
      splitContainer1.Panel2.Controls.Add(richTextBox1);
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
      // richTextBox1
      // 
      richTextBox1.Dock = DockStyle.Fill;
      richTextBox1.Font = new Font("Courier New", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
      richTextBox1.Location = new Point(0, 0);
      richTextBox1.Name = "richTextBox1";
      richTextBox1.Size = new Size(800, 205);
      richTextBox1.TabIndex = 1;
      richTextBox1.Text = "";
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
      splitContainer1.Panel1.ResumeLayout(false);
      splitContainer1.Panel2.ResumeLayout(false);
      ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
      splitContainer1.ResumeLayout(false);
      ResumeLayout(false);
    }

    #endregion

    private SplitContainer splitContainer1;
    private AudioWaterfallWidget AudioWaterfall;
    private RichTextBox richTextBox1;
  }
}