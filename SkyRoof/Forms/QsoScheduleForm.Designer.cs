namespace SkyRoof
{
  partial class QsoScheduleForm
  {
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        components?.Dispose();
        OkImage?.Dispose();
        XMarkImage?.Dispose();
        SkyBrush?.Dispose();
        FullPathPen?.Dispose();
        CommonPathPen?.Dispose();
        TitleFont?.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
      DetailsLabel = new Label();
      ChartsPanel = new Panel();
      SuspendLayout();
      // 
      // DetailsLabel
      // 
      DetailsLabel.AutoSize = true;
      DetailsLabel.Dock = DockStyle.Top;
      DetailsLabel.Location = new Point(0, 0);
      DetailsLabel.Name = "DetailsLabel";
      DetailsLabel.Padding = new Padding(12, 10, 12, 10);
      DetailsLabel.Size = new Size(24, 35);
      DetailsLabel.TabIndex = 0;
      // 
      // ChartsPanel
      // 
      ChartsPanel.Dock = DockStyle.Fill;
      ChartsPanel.Location = new Point(0, 35);
      ChartsPanel.Name = "ChartsPanel";
      ChartsPanel.Size = new Size(720, 465);
      ChartsPanel.TabIndex = 1;
      ChartsPanel.Paint += ChartsPanel_Paint;
      ChartsPanel.Resize += ChartsPanel_Resize;
      // 
      // QsoScheduleForm
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(720, 500);
      Controls.Add(ChartsPanel);
      Controls.Add(DetailsLabel);
      MinimizeBox = false;
      MinimumSize = new Size(500, 380);
      Name = "QsoScheduleForm";
      ShowInTaskbar = false;
      StartPosition = FormStartPosition.CenterScreen;
      Text = "QSO Schedule";
      ResumeLayout(false);
      PerformLayout();
    }

    #endregion

    private Label DetailsLabel;
    private Panel ChartsPanel;
  }
}
