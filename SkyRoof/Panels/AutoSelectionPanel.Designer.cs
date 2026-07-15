namespace SkyRoof
{
  partial class AutoSelectionPanel
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
      MainLayout = new FlowLayoutPanel();
      EditPanel = new Panel();
      EditBtn = new Button();
      EnablePanel = new Panel();
      EnableCheck = new CheckBox();
      ActivePanel = new Panel();
      ActiveLabel = new Label();
      NextPanel = new Panel();
      NextLabel = new Label();
      RecPanel = new Panel();
      RecLabel = new Label();
      MainLayout.SuspendLayout();
      EditPanel.SuspendLayout();
      EnablePanel.SuspendLayout();
      ActivePanel.SuspendLayout();
      NextPanel.SuspendLayout();
      RecPanel.SuspendLayout();
      SuspendLayout();
      // 
      // MainLayout
      // 
      MainLayout.Controls.Add(EditPanel);
      MainLayout.Controls.Add(EnablePanel);
      MainLayout.Controls.Add(ActivePanel);
      MainLayout.Controls.Add(NextPanel);
      MainLayout.Controls.Add(RecPanel);
      MainLayout.Dock = DockStyle.Fill;
      MainLayout.Location = new Point(0, 0);
      MainLayout.Name = "MainLayout";
      MainLayout.Padding = new Padding(6);
      MainLayout.Size = new Size(600, 60);
      MainLayout.TabIndex = 0;
      // 
      // EditPanel
      // 
      EditPanel.AutoSize = true;
      EditPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
      EditPanel.Controls.Add(EditBtn);
      EditPanel.Location = new Point(9, 9);
      EditPanel.Name = "EditPanel";
      EditPanel.Padding = new Padding(2);
      EditPanel.Size = new Size(85, 37);
      EditPanel.TabIndex = 0;
      // 
      // EditBtn
      // 
      EditBtn.AutoSize = true;
      EditBtn.Location = new Point(5, 5);
      EditBtn.Name = "EditBtn";
      EditBtn.Size = new Size(75, 27);
      EditBtn.TabIndex = 0;
      EditBtn.Text = "Edit";
      EditBtn.UseVisualStyleBackColor = true;
      EditBtn.Click += EditBtn_Click;
      // 
      // EnablePanel
      // 
      EnablePanel.AutoSize = true;
      EnablePanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
      EnablePanel.Controls.Add(EnableCheck);
      EnablePanel.Location = new Point(100, 9);
      EnablePanel.Name = "EnablePanel";
      EnablePanel.Size = new Size(146, 36);
      EnablePanel.TabIndex = 1;
      // 
      // EnableCheck
      // 
      EnableCheck.Appearance = Appearance.Button;
      EnableCheck.AutoSize = true;
      EnableCheck.Location = new Point(3, 3);
      EnableCheck.MinimumSize = new Size(140, 30);
      EnableCheck.Name = "EnableCheck";
      EnableCheck.Size = new Size(140, 30);
      EnableCheck.TabIndex = 0;
      EnableCheck.Text = "Auto Selection: OFF";
      EnableCheck.TextAlign = ContentAlignment.MiddleCenter;
      EnableCheck.UseVisualStyleBackColor = true;
      EnableCheck.CheckedChanged += EnableCheck_CheckedChanged;
      // 
      // ActivePanel
      // 
      ActivePanel.AutoSize = true;
      ActivePanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
      ActivePanel.Controls.Add(ActiveLabel);
      ActivePanel.Location = new Point(252, 9);
      ActivePanel.Name = "ActivePanel";
      ActivePanel.Size = new Size(49, 24);
      ActivePanel.TabIndex = 2;
      // 
      // ActiveLabel
      // 
      ActiveLabel.AutoSize = true;
      ActiveLabel.Location = new Point(3, 9);
      ActiveLabel.Name = "ActiveLabel";
      ActiveLabel.Size = new Size(43, 15);
      ActiveLabel.TabIndex = 0;
      ActiveLabel.Text = "Active:";
      // 
      // NextPanel
      // 
      NextPanel.AutoSize = true;
      NextPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
      NextPanel.Controls.Add(NextLabel);
      NextPanel.Location = new Point(307, 9);
      NextPanel.Name = "NextPanel";
      NextPanel.Size = new Size(40, 24);
      NextPanel.TabIndex = 3;
      // 
      // NextLabel
      // 
      NextLabel.AutoSize = true;
      NextLabel.Location = new Point(3, 9);
      NextLabel.Name = "NextLabel";
      NextLabel.Size = new Size(34, 15);
      NextLabel.TabIndex = 0;
      NextLabel.Text = "Next:";
      // 
      // RecPanel
      // 
      RecPanel.AutoSize = true;
      RecPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
      RecPanel.Controls.Add(RecLabel);
      RecPanel.Location = new Point(353, 9);
      RecPanel.Name = "RecPanel";
      RecPanel.Size = new Size(35, 24);
      RecPanel.TabIndex = 4;
      // 
      // RecLabel
      // 
      RecLabel.AutoSize = true;
      RecLabel.Location = new Point(3, 9);
      RecLabel.Name = "RecLabel";
      RecLabel.Size = new Size(29, 15);
      RecLabel.TabIndex = 0;
      RecLabel.Text = "Rec:";
      // 
      // AutoSelectionPanel
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(600, 60);
      Controls.Add(MainLayout);
      Font = new Font("Segoe UI", 9F);
      Name = "AutoSelectionPanel";
      Text = "Auto Selection";
      FormClosing += AutoSelectionPanel_FormClosing;
      MainLayout.ResumeLayout(false);
      MainLayout.PerformLayout();
      EditPanel.ResumeLayout(false);
      EditPanel.PerformLayout();
      EnablePanel.ResumeLayout(false);
      EnablePanel.PerformLayout();
      ActivePanel.ResumeLayout(false);
      ActivePanel.PerformLayout();
      NextPanel.ResumeLayout(false);
      NextPanel.PerformLayout();
      RecPanel.ResumeLayout(false);
      RecPanel.PerformLayout();
      ResumeLayout(false);
    }

    #endregion

    private FlowLayoutPanel MainLayout;
    private Panel EditPanel;
    private Button EditBtn;
    private Panel EnablePanel;
    private CheckBox EnableCheck;
    private Panel ActivePanel;
    private Label ActiveLabel;
    private Panel NextPanel;
    private Label NextLabel;
    private Panel RecPanel;
    private Label RecLabel;
  }
}
