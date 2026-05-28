namespace SkyRoof
{
  partial class WaterfallSildersDlg
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
      components = new System.ComponentModel.Container();
      BrightnessTrackbar = new TrackBar();
      ContrastTrackbar = new TrackBar();
      label1 = new Label();
      label2 = new Label();
      BrightnessLabel = new Label();
      ContrastLabel = new Label();
      SpeedLabel = new Label();
      label3 = new Label();
      SpeedTrackbar = new TrackBar();
      label4 = new Label();
      PaletteComboBox = new ComboBox();
      PerfLabel = new Label();
      perfTimer = new System.Windows.Forms.Timer(components);
      toolTip1 = new ToolTip(components);
      panel1 = new Panel();
      ((System.ComponentModel.ISupportInitialize)BrightnessTrackbar).BeginInit();
      ((System.ComponentModel.ISupportInitialize)ContrastTrackbar).BeginInit();
      ((System.ComponentModel.ISupportInitialize)SpeedTrackbar).BeginInit();
      panel1.SuspendLayout();
      SuspendLayout();
      // 
      // BrightnessTrackbar
      // 
      BrightnessTrackbar.AutoSize = false;
      BrightnessTrackbar.LargeChange = 1;
      BrightnessTrackbar.Location = new Point(5, 4);
      BrightnessTrackbar.Maximum = 100;
      BrightnessTrackbar.Name = "BrightnessTrackbar";
      BrightnessTrackbar.Size = new Size(150, 32);
      BrightnessTrackbar.TabIndex = 1;
      BrightnessTrackbar.TickFrequency = 10;
      toolTip1.SetToolTip(BrightnessTrackbar, "Brightness");
      BrightnessTrackbar.ValueChanged += Trackbar_ValueChanged;
      // 
      // ContrastTrackbar
      // 
      ContrastTrackbar.AutoSize = false;
      ContrastTrackbar.LargeChange = 1;
      ContrastTrackbar.Location = new Point(5, 42);
      ContrastTrackbar.Maximum = 100;
      ContrastTrackbar.Name = "ContrastTrackbar";
      ContrastTrackbar.Size = new Size(150, 32);
      ContrastTrackbar.TabIndex = 2;
      ContrastTrackbar.TickFrequency = 10;
      toolTip1.SetToolTip(ContrastTrackbar, "Contrast");
      ContrastTrackbar.ValueChanged += Trackbar_ValueChanged;
      // 
      // label1
      // 
      label1.ForeColor = Color.SteelBlue;
      label1.Location = new Point(156, 9);
      label1.Name = "label1";
      label1.Size = new Size(20, 20);
      label1.TabIndex = 9;
      label1.Text = "=";
      label1.TextAlign = ContentAlignment.MiddleCenter;
      // 
      // label2
      // 
      label2.ForeColor = Color.SteelBlue;
      label2.Location = new Point(156, 46);
      label2.Name = "label2";
      label2.Size = new Size(20, 20);
      label2.TabIndex = 9;
      label2.Text = "=";
      label2.TextAlign = ContentAlignment.MiddleCenter;
      // 
      // BrightnessLabel
      // 
      BrightnessLabel.AutoSize = true;
      BrightnessLabel.Location = new Point(181, 11);
      BrightnessLabel.Name = "BrightnessLabel";
      BrightnessLabel.Size = new Size(25, 15);
      BrightnessLabel.TabIndex = 9;
      BrightnessLabel.Text = "100";
      // 
      // ContrastLabel
      // 
      ContrastLabel.AutoSize = true;
      ContrastLabel.Location = new Point(181, 48);
      ContrastLabel.Name = "ContrastLabel";
      ContrastLabel.Size = new Size(25, 15);
      ContrastLabel.TabIndex = 9;
      ContrastLabel.Text = "100";
      // 
      // SpeedLabel
      // 
      SpeedLabel.AutoSize = true;
      SpeedLabel.Location = new Point(181, 86);
      SpeedLabel.Name = "SpeedLabel";
      SpeedLabel.Size = new Size(25, 15);
      SpeedLabel.TabIndex = 9;
      SpeedLabel.Text = "100";
      // 
      // label3
      // 
      label3.ForeColor = Color.SteelBlue;
      label3.Location = new Point(156, 84);
      label3.Name = "label3";
      label3.Size = new Size(20, 20);
      label3.TabIndex = 9;
      label3.Text = "=";
      label3.TextAlign = ContentAlignment.MiddleCenter;
      // 
      // SpeedTrackbar
      // 
      SpeedTrackbar.AutoSize = false;
      SpeedTrackbar.LargeChange = 1;
      SpeedTrackbar.Location = new Point(5, 79);
      SpeedTrackbar.Maximum = 4;
      SpeedTrackbar.Name = "SpeedTrackbar";
      SpeedTrackbar.Size = new Size(150, 32);
      SpeedTrackbar.TabIndex = 3;
      toolTip1.SetToolTip(SpeedTrackbar, "Scrolling Speed, pixels/s");
      SpeedTrackbar.ValueChanged += Trackbar_ValueChanged;
      // 
      // label4
      // 
      label4.ForeColor = Color.SteelBlue;
      label4.Location = new Point(156, 119);
      label4.Name = "label4";
      label4.Size = new Size(20, 20);
      label4.TabIndex = 9;
      label4.Text = "=";
      label4.TextAlign = ContentAlignment.MiddleCenter;
      // 
      // PaletteComboBox
      // 
      PaletteComboBox.BackColor = SystemColors.Control;
      PaletteComboBox.DrawMode = DrawMode.OwnerDrawFixed;
      PaletteComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
      PaletteComboBox.FormattingEnabled = true;
      PaletteComboBox.Items.AddRange(new object[] { "1", "2", "3" });
      PaletteComboBox.Location = new Point(11, 117);
      PaletteComboBox.Name = "PaletteComboBox";
      PaletteComboBox.Size = new Size(138, 24);
      PaletteComboBox.TabIndex = 4;
      toolTip1.SetToolTip(PaletteComboBox, "Palette");
      PaletteComboBox.DrawItem += comboBox1_DrawItem;
      PaletteComboBox.SelectedIndexChanged += Trackbar_ValueChanged;
      // 
      // PerfLabel
      // 
      PerfLabel.Font = new Font("Segoe UI", 8F);
      PerfLabel.ForeColor = Color.DimGray;
      PerfLabel.Location = new Point(5, 146);
      PerfLabel.Name = "PerfLabel";
      PerfLabel.Size = new Size(198, 46);
      PerfLabel.TabIndex = 11;
      PerfLabel.Text = "Perf: —";
      // 
      // perfTimer
      // 
      perfTimer.Interval = 500;
      perfTimer.Tick += perfTimer_Tick;
      // 
      // panel1
      // 
      panel1.BorderStyle = BorderStyle.FixedSingle;
      panel1.Controls.Add(label4);
      panel1.Dock = DockStyle.Fill;
      panel1.Location = new Point(0, 0);
      panel1.Name = "panel1";
      panel1.Size = new Size(208, 198);
      panel1.TabIndex = 10;
      // 
      // WaterfallSildersDlg
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(208, 198);
      ControlBox = false;
      Controls.Add(PerfLabel);
      Controls.Add(PaletteComboBox);
      Controls.Add(SpeedLabel);
      Controls.Add(label3);
      Controls.Add(SpeedTrackbar);
      Controls.Add(ContrastLabel);
      Controls.Add(BrightnessLabel);
      Controls.Add(label2);
      Controls.Add(label1);
      Controls.Add(ContrastTrackbar);
      Controls.Add(BrightnessTrackbar);
      Controls.Add(panel1);
      FormBorderStyle = FormBorderStyle.None;
      KeyPreview = true;
      Name = "WaterfallSildersDlg";
      ShowIcon = false;
      ShowInTaskbar = false;
      StartPosition = FormStartPosition.Manual;
      Text = "WaterfallSildersDlg";
      Deactivate += WaterfallSildersDlg_Deactivate;
      KeyPress += WaterfallSildersDlg_KeyPress;
      ((System.ComponentModel.ISupportInitialize)BrightnessTrackbar).EndInit();
      ((System.ComponentModel.ISupportInitialize)ContrastTrackbar).EndInit();
      ((System.ComponentModel.ISupportInitialize)SpeedTrackbar).EndInit();
      panel1.ResumeLayout(false);
      ResumeLayout(false);
      PerformLayout();
    }

    #endregion

    public TrackBar BrightnessTrackbar;
    public TrackBar ContrastTrackbar;
    private Label label1;
    private Label label2;
    private Label BrightnessLabel;
    private Label ContrastLabel;
    private Label SpeedLabel;
    private Label label3;
    public TrackBar SpeedTrackbar;
    private Label label4;
    private ComboBox PaletteComboBox;
    private Label PerfLabel;
    private System.Windows.Forms.Timer perfTimer;
    private ToolTip toolTip1;
    private Panel panel1;
  }
}