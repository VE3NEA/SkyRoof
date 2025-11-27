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
      richTextBox1 = new RichTextBox();
      SuspendLayout();
      // 
      // richTextBox1
      // 
      richTextBox1.Dock = DockStyle.Fill;
      richTextBox1.Font = new Font("Courier New", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
      richTextBox1.Location = new Point(0, 0);
      richTextBox1.Name = "richTextBox1";
      richTextBox1.Size = new Size(800, 450);
      richTextBox1.TabIndex = 0;
      richTextBox1.Text = "";
      // 
      // Ft4ConsolePanel
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(800, 450);
      Controls.Add(richTextBox1);
      Name = "Ft4ConsolePanel";
      Text = "FT4 Console";
      FormClosing += Ft4ConsolePanel_FormClosing;
      ResumeLayout(false);
    }

    #endregion

    private RichTextBox richTextBox1;
  }
}