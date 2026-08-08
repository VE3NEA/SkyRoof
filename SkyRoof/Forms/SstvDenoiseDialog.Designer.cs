namespace SkyRoof
{
  partial class SstvDenoiseDialog
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
      PreviewPanel = new Panel();
      PreviewBox = new PictureBox();
      SidePanel = new Panel();
      AlgorithmGroupBox = new GroupBox();
      NoneRadio = new RadioButton();
      WienerRadio = new RadioButton();
      NlmRadio = new RadioButton();
      WienerGroupBox = new GroupBox();
      WienerWidthLabel = new Label();
      WienerWidthSpinner = new NumericUpDown();
      WienerHeightLabel = new Label();
      WienerHeightSpinner = new NumericUpDown();
      WienerFloorLabel = new Label();
      WienerFloorSpinner = new NumericUpDown();
      WienerChromaLabel = new Label();
      WienerChromaSpinner = new NumericUpDown();
      NlmGroupBox = new GroupBox();
      NlmStrengthLabel = new Label();
      NlmStrengthSpinner = new NumericUpDown();
      NlmPatchLabel = new Label();
      NlmPatchSpinner = new NumericUpDown();
      NlmSearchLabel = new Label();
      NlmSearchSpinner = new NumericUpDown();
      NlmChromaLabel = new Label();
      NlmChromaSpinner = new NumericUpDown();
      NlmTwoPassCheckBox = new CheckBox();
      SkipNoiseBandsCheckBox = new CheckBox();
      BottomPanel = new Panel();
      StatusLabel = new Label();
      ApplyBtn = new Button();
      OkBtn = new Button();
      CancelBtn = new Button();
      PreviewPanel.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)PreviewBox).BeginInit();
      SidePanel.SuspendLayout();
      AlgorithmGroupBox.SuspendLayout();
      WienerGroupBox.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)WienerWidthSpinner).BeginInit();
      ((System.ComponentModel.ISupportInitialize)WienerHeightSpinner).BeginInit();
      ((System.ComponentModel.ISupportInitialize)WienerFloorSpinner).BeginInit();
      ((System.ComponentModel.ISupportInitialize)WienerChromaSpinner).BeginInit();
      NlmGroupBox.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)NlmStrengthSpinner).BeginInit();
      ((System.ComponentModel.ISupportInitialize)NlmPatchSpinner).BeginInit();
      ((System.ComponentModel.ISupportInitialize)NlmSearchSpinner).BeginInit();
      ((System.ComponentModel.ISupportInitialize)NlmChromaSpinner).BeginInit();
      BottomPanel.SuspendLayout();
      SuspendLayout();
      //
      // PreviewPanel
      //
      PreviewPanel.AutoScroll = true;
      PreviewPanel.BackColor = SystemColors.ControlDark;
      PreviewPanel.Controls.Add(PreviewBox);
      PreviewPanel.Dock = DockStyle.Fill;
      PreviewPanel.Location = new Point(0, 0);
      PreviewPanel.Name = "PreviewPanel";
      PreviewPanel.Size = new Size(654, 596);
      PreviewPanel.TabIndex = 0;
      PreviewPanel.ClientSizeChanged += PreviewPanel_ClientSizeChanged;
      //
      // PreviewBox
      //
      PreviewBox.Location = new Point(0, 0);
      PreviewBox.Name = "PreviewBox";
      PreviewBox.Size = new Size(640, 480);
      PreviewBox.SizeMode = PictureBoxSizeMode.Normal;
      PreviewBox.TabIndex = 0;
      PreviewBox.TabStop = false;
      //
      // SidePanel
      //
      SidePanel.Controls.Add(ApplyBtn);
      SidePanel.Controls.Add(SkipNoiseBandsCheckBox);
      SidePanel.Controls.Add(NlmGroupBox);
      SidePanel.Controls.Add(WienerGroupBox);
      SidePanel.Controls.Add(AlgorithmGroupBox);
      SidePanel.Dock = DockStyle.Right;
      SidePanel.Location = new Point(654, 0);
      SidePanel.Name = "SidePanel";
      SidePanel.Size = new Size(250, 596);
      SidePanel.TabIndex = 1;
      //
      // AlgorithmGroupBox
      //
      AlgorithmGroupBox.Controls.Add(NlmRadio);
      AlgorithmGroupBox.Controls.Add(WienerRadio);
      AlgorithmGroupBox.Controls.Add(NoneRadio);
      AlgorithmGroupBox.Location = new Point(10, 10);
      AlgorithmGroupBox.Name = "AlgorithmGroupBox";
      AlgorithmGroupBox.Size = new Size(230, 104);
      AlgorithmGroupBox.TabIndex = 0;
      AlgorithmGroupBox.TabStop = false;
      AlgorithmGroupBox.Text = "Algorithm";
      //
      // NoneRadio
      //
      NoneRadio.AutoSize = true;
      NoneRadio.Location = new Point(14, 25);
      NoneRadio.Name = "NoneRadio";
      NoneRadio.Size = new Size(54, 19);
      NoneRadio.TabIndex = 0;
      NoneRadio.Text = "None";
      NoneRadio.UseVisualStyleBackColor = true;
      NoneRadio.CheckedChanged += MethodRadio_CheckedChanged;
      //
      // WienerRadio
      //
      WienerRadio.AutoSize = true;
      WienerRadio.Location = new Point(14, 50);
      WienerRadio.Name = "WienerRadio";
      WienerRadio.Size = new Size(62, 19);
      WienerRadio.TabIndex = 1;
      WienerRadio.Text = "Wiener";
      WienerRadio.UseVisualStyleBackColor = true;
      WienerRadio.CheckedChanged += MethodRadio_CheckedChanged;
      //
      // NlmRadio
      //
      NlmRadio.AutoSize = true;
      NlmRadio.Checked = true;
      NlmRadio.Location = new Point(14, 75);
      NlmRadio.Name = "NlmRadio";
      NlmRadio.Size = new Size(122, 19);
      NlmRadio.TabIndex = 2;
      NlmRadio.TabStop = true;
      NlmRadio.Text = "Non-Local Means";
      NlmRadio.UseVisualStyleBackColor = true;
      NlmRadio.CheckedChanged += MethodRadio_CheckedChanged;
      //
      // WienerGroupBox
      //
      WienerGroupBox.Controls.Add(WienerChromaSpinner);
      WienerGroupBox.Controls.Add(WienerChromaLabel);
      WienerGroupBox.Controls.Add(WienerFloorSpinner);
      WienerGroupBox.Controls.Add(WienerFloorLabel);
      WienerGroupBox.Controls.Add(WienerHeightSpinner);
      WienerGroupBox.Controls.Add(WienerHeightLabel);
      WienerGroupBox.Controls.Add(WienerWidthSpinner);
      WienerGroupBox.Controls.Add(WienerWidthLabel);
      WienerGroupBox.Location = new Point(10, 122);
      WienerGroupBox.Name = "WienerGroupBox";
      WienerGroupBox.Size = new Size(230, 150);
      WienerGroupBox.TabIndex = 3;
      WienerGroupBox.TabStop = false;
      WienerGroupBox.Text = "Wiener";
      //
      // WienerWidthLabel
      //
      WienerWidthLabel.AutoSize = true;
      WienerWidthLabel.Location = new Point(12, 30);
      WienerWidthLabel.Name = "WienerWidthLabel";
      WienerWidthLabel.Size = new Size(88, 15);
      WienerWidthLabel.TabIndex = 0;
      WienerWidthLabel.Text = "Window Width";
      //
      // WienerWidthSpinner
      //
      WienerWidthSpinner.Increment = new decimal(new int[] { 2, 0, 0, 0 });
      WienerWidthSpinner.Location = new Point(146, 27);
      WienerWidthSpinner.Maximum = new decimal(new int[] { 21, 0, 0, 0 });
      WienerWidthSpinner.Minimum = new decimal(new int[] { 3, 0, 0, 0 });
      WienerWidthSpinner.Name = "WienerWidthSpinner";
      WienerWidthSpinner.Size = new Size(70, 23);
      WienerWidthSpinner.TabIndex = 1;
      WienerWidthSpinner.Value = new decimal(new int[] { 9, 0, 0, 0 });
      //
      // WienerHeightLabel
      //
      WienerHeightLabel.AutoSize = true;
      WienerHeightLabel.Location = new Point(12, 60);
      WienerHeightLabel.Name = "WienerHeightLabel";
      WienerHeightLabel.Size = new Size(92, 15);
      WienerHeightLabel.TabIndex = 2;
      WienerHeightLabel.Text = "Window Height";
      //
      // WienerHeightSpinner
      //
      WienerHeightSpinner.Increment = new decimal(new int[] { 2, 0, 0, 0 });
      WienerHeightSpinner.Location = new Point(146, 57);
      WienerHeightSpinner.Maximum = new decimal(new int[] { 15, 0, 0, 0 });
      WienerHeightSpinner.Minimum = new decimal(new int[] { 3, 0, 0, 0 });
      WienerHeightSpinner.Name = "WienerHeightSpinner";
      WienerHeightSpinner.Size = new Size(70, 23);
      WienerHeightSpinner.TabIndex = 3;
      WienerHeightSpinner.Value = new decimal(new int[] { 5, 0, 0, 0 });
      //
      // WienerFloorLabel
      //
      WienerFloorLabel.AutoSize = true;
      WienerFloorLabel.Location = new Point(12, 90);
      WienerFloorLabel.Name = "WienerFloorLabel";
      WienerFloorLabel.Size = new Size(58, 15);
      WienerFloorLabel.TabIndex = 4;
      WienerFloorLabel.Text = "Gain Floor";
      //
      // WienerFloorSpinner
      //
      WienerFloorSpinner.DecimalPlaces = 2;
      WienerFloorSpinner.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
      WienerFloorSpinner.Location = new Point(146, 87);
      WienerFloorSpinner.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
      WienerFloorSpinner.Name = "WienerFloorSpinner";
      WienerFloorSpinner.Size = new Size(70, 23);
      WienerFloorSpinner.TabIndex = 5;
      WienerFloorSpinner.Value = new decimal(new int[] { 25, 0, 0, 131072 });
      //
      // WienerChromaLabel
      //
      WienerChromaLabel.AutoSize = true;
      WienerChromaLabel.Location = new Point(12, 120);
      WienerChromaLabel.Name = "WienerChromaLabel";
      WienerChromaLabel.Size = new Size(91, 15);
      WienerChromaLabel.TabIndex = 6;
      WienerChromaLabel.Text = "Color Strength";
      //
      // WienerChromaSpinner
      //
      WienerChromaSpinner.DecimalPlaces = 1;
      WienerChromaSpinner.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
      WienerChromaSpinner.Location = new Point(146, 117);
      WienerChromaSpinner.Maximum = new decimal(new int[] { 16, 0, 0, 0 });
      WienerChromaSpinner.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
      WienerChromaSpinner.Name = "WienerChromaSpinner";
      WienerChromaSpinner.Size = new Size(70, 23);
      WienerChromaSpinner.TabIndex = 7;
      WienerChromaSpinner.Value = new decimal(new int[] { 4, 0, 0, 0 });
      //
      // NlmGroupBox
      //
      NlmGroupBox.Controls.Add(NlmTwoPassCheckBox);
      NlmGroupBox.Controls.Add(NlmChromaSpinner);
      NlmGroupBox.Controls.Add(NlmChromaLabel);
      NlmGroupBox.Controls.Add(NlmSearchSpinner);
      NlmGroupBox.Controls.Add(NlmSearchLabel);
      NlmGroupBox.Controls.Add(NlmPatchSpinner);
      NlmGroupBox.Controls.Add(NlmPatchLabel);
      NlmGroupBox.Controls.Add(NlmStrengthSpinner);
      NlmGroupBox.Controls.Add(NlmStrengthLabel);
      NlmGroupBox.Location = new Point(10, 284);
      NlmGroupBox.Name = "NlmGroupBox";
      NlmGroupBox.Size = new Size(230, 186);
      NlmGroupBox.TabIndex = 4;
      NlmGroupBox.TabStop = false;
      NlmGroupBox.Text = "Non-Local Means";
      //
      // NlmStrengthLabel
      //
      NlmStrengthLabel.AutoSize = true;
      NlmStrengthLabel.Location = new Point(12, 30);
      NlmStrengthLabel.Name = "NlmStrengthLabel";
      NlmStrengthLabel.Size = new Size(52, 15);
      NlmStrengthLabel.TabIndex = 0;
      NlmStrengthLabel.Text = "Strength";
      //
      // NlmStrengthSpinner
      //
      NlmStrengthSpinner.DecimalPlaces = 2;
      NlmStrengthSpinner.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
      NlmStrengthSpinner.Location = new Point(146, 27);
      NlmStrengthSpinner.Maximum = new decimal(new int[] { 32, 0, 0, 65536 });
      NlmStrengthSpinner.Minimum = new decimal(new int[] { 5, 0, 0, 131072 });
      NlmStrengthSpinner.Name = "NlmStrengthSpinner";
      NlmStrengthSpinner.Size = new Size(70, 23);
      NlmStrengthSpinner.TabIndex = 1;
      NlmStrengthSpinner.Value = new decimal(new int[] { 60, 0, 0, 131072 });
      //
      // NlmPatchLabel
      //
      NlmPatchLabel.AutoSize = true;
      NlmPatchLabel.Location = new Point(12, 60);
      NlmPatchLabel.Name = "NlmPatchLabel";
      NlmPatchLabel.Size = new Size(63, 15);
      NlmPatchLabel.TabIndex = 2;
      NlmPatchLabel.Text = "Patch Size";
      //
      // NlmPatchSpinner
      //
      NlmPatchSpinner.Location = new Point(146, 57);
      NlmPatchSpinner.Maximum = new decimal(new int[] { 3, 0, 0, 0 });
      NlmPatchSpinner.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
      NlmPatchSpinner.Name = "NlmPatchSpinner";
      NlmPatchSpinner.Size = new Size(70, 23);
      NlmPatchSpinner.TabIndex = 3;
      NlmPatchSpinner.Value = new decimal(new int[] { 3, 0, 0, 0 });
      //
      // NlmSearchLabel
      //
      NlmSearchLabel.AutoSize = true;
      NlmSearchLabel.Location = new Point(12, 90);
      NlmSearchLabel.Name = "NlmSearchLabel";
      NlmSearchLabel.Size = new Size(80, 15);
      NlmSearchLabel.TabIndex = 4;
      NlmSearchLabel.Text = "Search Radius";
      //
      // NlmSearchSpinner
      //
      NlmSearchSpinner.Location = new Point(146, 87);
      NlmSearchSpinner.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
      NlmSearchSpinner.Minimum = new decimal(new int[] { 3, 0, 0, 0 });
      NlmSearchSpinner.Name = "NlmSearchSpinner";
      NlmSearchSpinner.Size = new Size(70, 23);
      NlmSearchSpinner.TabIndex = 5;
      NlmSearchSpinner.Value = new decimal(new int[] { 10, 0, 0, 0 });
      //
      // NlmChromaLabel
      //
      NlmChromaLabel.AutoSize = true;
      NlmChromaLabel.Location = new Point(12, 120);
      NlmChromaLabel.Name = "NlmChromaLabel";
      NlmChromaLabel.Size = new Size(91, 15);
      NlmChromaLabel.TabIndex = 6;
      NlmChromaLabel.Text = "Color Strength";
      //
      // NlmChromaSpinner
      //
      NlmChromaSpinner.DecimalPlaces = 1;
      NlmChromaSpinner.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
      NlmChromaSpinner.Location = new Point(146, 117);
      NlmChromaSpinner.Maximum = new decimal(new int[] { 16, 0, 0, 0 });
      NlmChromaSpinner.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
      NlmChromaSpinner.Name = "NlmChromaSpinner";
      NlmChromaSpinner.Size = new Size(70, 23);
      NlmChromaSpinner.TabIndex = 7;
      NlmChromaSpinner.Value = new decimal(new int[] { 4, 0, 0, 0 });
      //
      // NlmTwoPassCheckBox
      //
      NlmTwoPassCheckBox.AutoSize = true;
      NlmTwoPassCheckBox.Location = new Point(14, 154);
      NlmTwoPassCheckBox.Name = "NlmTwoPassCheckBox";
      NlmTwoPassCheckBox.Size = new Size(146, 19);
      NlmTwoPassCheckBox.TabIndex = 8;
      NlmTwoPassCheckBox.Text = "Remove Residual Dots";
      NlmTwoPassCheckBox.UseVisualStyleBackColor = true;
      //
      // SkipNoiseBandsCheckBox
      //
      SkipNoiseBandsCheckBox.AutoSize = true;
      SkipNoiseBandsCheckBox.Location = new Point(24, 482);
      SkipNoiseBandsCheckBox.Name = "SkipNoiseBandsCheckBox";
      SkipNoiseBandsCheckBox.Size = new Size(150, 19);
      SkipNoiseBandsCheckBox.TabIndex = 3;
      SkipNoiseBandsCheckBox.Text = "Skip Noise-Only Bands";
      SkipNoiseBandsCheckBox.UseVisualStyleBackColor = true;
      //
      // BottomPanel
      //
      BottomPanel.Controls.Add(CancelBtn);
      BottomPanel.Controls.Add(OkBtn);
      BottomPanel.Controls.Add(StatusLabel);
      BottomPanel.Dock = DockStyle.Bottom;
      BottomPanel.Location = new Point(0, 596);
      BottomPanel.Name = "BottomPanel";
      BottomPanel.Size = new Size(904, 44);
      BottomPanel.TabIndex = 2;
      //
      // StatusLabel
      //
      StatusLabel.AutoSize = true;
      StatusLabel.Location = new Point(14, 15);
      StatusLabel.Name = "StatusLabel";
      StatusLabel.Size = new Size(0, 15);
      StatusLabel.TabIndex = 0;
      //
      // ApplyBtn
      //
      ApplyBtn.Location = new Point(24, 518);
      ApplyBtn.Name = "ApplyBtn";
      ApplyBtn.Size = new Size(90, 27);
      ApplyBtn.TabIndex = 4;
      ApplyBtn.Text = "Apply";
      ApplyBtn.UseVisualStyleBackColor = true;
      ApplyBtn.Click += ApplyBtn_Click;
      //
      // OkBtn
      //
      OkBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      OkBtn.Location = new Point(658, 10);
      OkBtn.Name = "OkBtn";
      OkBtn.Size = new Size(75, 25);
      OkBtn.TabIndex = 2;
      OkBtn.Text = "OK";
      OkBtn.UseVisualStyleBackColor = true;
      OkBtn.Click += OkBtn_Click;
      //
      // CancelBtn
      //
      CancelBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
      CancelBtn.DialogResult = DialogResult.Cancel;
      CancelBtn.Location = new Point(739, 10);
      CancelBtn.Name = "CancelBtn";
      CancelBtn.Size = new Size(75, 25);
      CancelBtn.TabIndex = 3;
      CancelBtn.Text = "Cancel";
      CancelBtn.UseVisualStyleBackColor = true;
      //
      // SstvDenoiseDialog
      //
      AcceptButton = OkBtn;
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      CancelButton = CancelBtn;
      ClientSize = new Size(904, 640);
      Controls.Add(PreviewPanel);
      Controls.Add(SidePanel);
      Controls.Add(BottomPanel);
      MinimizeBox = false;
      Name = "SstvDenoiseDialog";
      ShowInTaskbar = false;
      StartPosition = FormStartPosition.CenterParent;
      Text = "Denoise Image";
      PreviewPanel.ResumeLayout(false);
      ((System.ComponentModel.ISupportInitialize)PreviewBox).EndInit();
      SidePanel.ResumeLayout(false);
      SidePanel.PerformLayout();
      AlgorithmGroupBox.ResumeLayout(false);
      AlgorithmGroupBox.PerformLayout();
      WienerGroupBox.ResumeLayout(false);
      WienerGroupBox.PerformLayout();
      ((System.ComponentModel.ISupportInitialize)WienerWidthSpinner).EndInit();
      ((System.ComponentModel.ISupportInitialize)WienerHeightSpinner).EndInit();
      ((System.ComponentModel.ISupportInitialize)WienerFloorSpinner).EndInit();
      ((System.ComponentModel.ISupportInitialize)WienerChromaSpinner).EndInit();
      NlmGroupBox.ResumeLayout(false);
      NlmGroupBox.PerformLayout();
      ((System.ComponentModel.ISupportInitialize)NlmStrengthSpinner).EndInit();
      ((System.ComponentModel.ISupportInitialize)NlmPatchSpinner).EndInit();
      ((System.ComponentModel.ISupportInitialize)NlmSearchSpinner).EndInit();
      ((System.ComponentModel.ISupportInitialize)NlmChromaSpinner).EndInit();
      BottomPanel.ResumeLayout(false);
      BottomPanel.PerformLayout();
      ResumeLayout(false);
    }

    #endregion

    private Panel PreviewPanel;
    private PictureBox PreviewBox;
    private Panel SidePanel;
    private GroupBox AlgorithmGroupBox;
    private RadioButton NoneRadio;
    private RadioButton WienerRadio;
    private RadioButton NlmRadio;
    private GroupBox WienerGroupBox;
    private Label WienerWidthLabel;
    private NumericUpDown WienerWidthSpinner;
    private Label WienerHeightLabel;
    private NumericUpDown WienerHeightSpinner;
    private Label WienerFloorLabel;
    private NumericUpDown WienerFloorSpinner;
    private Label WienerChromaLabel;
    private NumericUpDown WienerChromaSpinner;
    private GroupBox NlmGroupBox;
    private Label NlmStrengthLabel;
    private NumericUpDown NlmStrengthSpinner;
    private Label NlmPatchLabel;
    private NumericUpDown NlmPatchSpinner;
    private Label NlmSearchLabel;
    private NumericUpDown NlmSearchSpinner;
    private Label NlmChromaLabel;
    private NumericUpDown NlmChromaSpinner;
    private CheckBox NlmTwoPassCheckBox;
    private CheckBox SkipNoiseBandsCheckBox;
    private Panel BottomPanel;
    private Label StatusLabel;
    private Button ApplyBtn;
    private Button OkBtn;
    private Button CancelBtn;
  }
}
