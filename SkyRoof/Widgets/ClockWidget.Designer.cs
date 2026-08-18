namespace VE3NEA.Clock
{
  partial class ClockWidget
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
      timeLabel = new Label();
      dateLabel = new Label();
      utcLabel = new Label();
      localLabel = new Label();
      SuspendLayout();
      // 
      // timeLabel
      // 
      timeLabel.AutoSize = true;
      timeLabel.Font = new Font("Microsoft Sans Serif", 16F, FontStyle.Regular, GraphicsUnit.Point, 0);
      timeLabel.ForeColor = Color.Aqua;
      timeLabel.Location = new Point(9, 3);
      timeLabel.Margin = new Padding(2, 0, 2, 0);
      timeLabel.Name = "timeLabel";
      timeLabel.RightToLeft = RightToLeft.No;
      timeLabel.Size = new Size(96, 26);
      timeLabel.TabIndex = 0;
      timeLabel.Text = "00:00:00";
      timeLabel.TextAlign = ContentAlignment.MiddleLeft;
      timeLabel.Click += this.utcLabel_Click;
      // 
      // dateLabel
      // 
      dateLabel.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
      dateLabel.ForeColor = Color.Aqua;
      dateLabel.Location = new Point(13, 30);
      dateLabel.Margin = new Padding(2, 0, 2, 0);
      dateLabel.Name = "dateLabel";
      dateLabel.RightToLeft = RightToLeft.No;
      dateLabel.Size = new Size(87, 13);
      dateLabel.TabIndex = 1;
      dateLabel.Text = "Dec 31, 2025";
      dateLabel.TextAlign = ContentAlignment.MiddleCenter;
      dateLabel.Click += dateLabel_Click;
      // 
      // utcLabel
      // 
      utcLabel.BackColor = Color.Aqua;
      utcLabel.ForeColor = Color.Black; // fixed: the aqua and teal backgrounds are fixed too
      utcLabel.Cursor = Cursors.Hand;
      utcLabel.Font = new Font("Microsoft Sans Serif", 6F, FontStyle.Bold, GraphicsUnit.Point, 0);
      utcLabel.Location = new Point(65, 51);
      utcLabel.Margin = new Padding(2, 0, 2, 0);
      utcLabel.Name = "utcLabel";
      utcLabel.RightToLeft = RightToLeft.No;
      utcLabel.Size = new Size(28, 12);
      utcLabel.TabIndex = 2;
      utcLabel.Text = "UTC";
      utcLabel.TextAlign = ContentAlignment.MiddleCenter;
      utcLabel.Click += utcLabel_Click;
      // 
      // localLabel
      // 
      localLabel.BackColor = Color.Teal;
      localLabel.ForeColor = Color.Black; // fixed: the aqua and teal backgrounds are fixed too
      localLabel.Cursor = Cursors.Hand;
      localLabel.Font = new Font("Microsoft Sans Serif", 6F, FontStyle.Bold, GraphicsUnit.Point, 0);
      localLabel.Location = new Point(17, 51);
      localLabel.Margin = new Padding(2, 0, 2, 0);
      localLabel.Name = "localLabel";
      localLabel.RightToLeft = RightToLeft.No;
      localLabel.Size = new Size(35, 12);
      localLabel.TabIndex = 3;
      localLabel.Text = "Local";
      localLabel.TextAlign = ContentAlignment.MiddleCenter;
      localLabel.Click += utcLabel_Click;
      // 
      // Clock
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      BackColor = Color.DarkBlue;
      BorderStyle = BorderStyle.FixedSingle;
      Controls.Add(localLabel);
      Controls.Add(utcLabel);
      Controls.Add(dateLabel);
      Controls.Add(timeLabel);
      Margin = new Padding(5);
      Name = "Clock";
      Size = new Size(117, 71);
      ResumeLayout(false);
      PerformLayout();
    }

    #endregion

    private System.Windows.Forms.Label timeLabel;
    private System.Windows.Forms.Label dateLabel;
    private System.Windows.Forms.Label utcLabel;
    private Label localLabel;
  }
}
