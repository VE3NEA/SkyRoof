
namespace SkyRoof
{
  partial class RotatorWidget
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
      SatelliteAzimuthLabel = new Label();
      label1 = new Label();
      label2 = new Label();
      SatelliteElevationLabel = new Label();
      TrackCheckbox = new CheckBox();
      AntennaElevationLabel = new Label();
      AntennaAzimuthLabel = new Label();
      StopBtn = new Button();
      toolTip1 = new VE3NEA.ToolTipEx(components);
      SuspendLayout();
      // 
      // SatelliteAzimuthLabel
      // 
      SatelliteAzimuthLabel.BackColor = Color.Black;
      SatelliteAzimuthLabel.Cursor = Cursors.Hand;
      SatelliteAzimuthLabel.Font = new Font("Microsoft Sans Serif", 16F);
      SatelliteAzimuthLabel.ForeColor = Color.Aqua;
      SatelliteAzimuthLabel.Location = new Point(8, 18);
      SatelliteAzimuthLabel.Name = "SatelliteAzimuthLabel";
      SatelliteAzimuthLabel.Size = new Size(59, 34);
      SatelliteAzimuthLabel.TabIndex = 28;
      SatelliteAzimuthLabel.Text = "180°";
      SatelliteAzimuthLabel.TextAlign = ContentAlignment.MiddleCenter;
      toolTip1.SetToolTip(SatelliteAzimuthLabel, "Satellite Azimuth\r\n\r\nClick for manual rotator control");
      SatelliteAzimuthLabel.Click += AzEl_Click;
      // 
      // label1
      // 
      label1.AutoSize = true;
      label1.Location = new Point(8, 1);
      label1.Name = "label1";
      label1.Size = new Size(52, 15);
      label1.TabIndex = 29;
      label1.Text = "Azimuth";
      // 
      // label2
      // 
      label2.AutoSize = true;
      label2.Location = new Point(75, 1);
      label2.Name = "label2";
      label2.Size = new Size(55, 15);
      label2.TabIndex = 31;
      label2.Text = "Elevation";
      // 
      // SatelliteElevationLabel
      // 
      SatelliteElevationLabel.BackColor = Color.Black;
      SatelliteElevationLabel.Cursor = Cursors.Hand;
      SatelliteElevationLabel.Font = new Font("Microsoft Sans Serif", 16F);
      SatelliteElevationLabel.ForeColor = Color.Aqua;
      SatelliteElevationLabel.Location = new Point(75, 18);
      SatelliteElevationLabel.Name = "SatelliteElevationLabel";
      SatelliteElevationLabel.Size = new Size(59, 34);
      SatelliteElevationLabel.TabIndex = 30;
      SatelliteElevationLabel.Text = "90°";
      SatelliteElevationLabel.TextAlign = ContentAlignment.MiddleCenter;
      toolTip1.SetToolTip(SatelliteElevationLabel, "Satellite Elevation\r\n\r\nClick for manual rotator control");
      SatelliteElevationLabel.Click += AzEl_Click;
      // 
      // TrackCheckbox
      // 
      TrackCheckbox.AutoSize = true;
      TrackCheckbox.Location = new Point(146, 13);
      TrackCheckbox.Name = "TrackCheckbox";
      TrackCheckbox.Size = new Size(53, 19);
      TrackCheckbox.TabIndex = 32;
      TrackCheckbox.Text = "Track";
      TrackCheckbox.UseVisualStyleBackColor = true;
      TrackCheckbox.CheckedChanged += TrackCheckbox_CheckedChanged;
      // 
      // AntennaElevationLabel
      // 
      AntennaElevationLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
      AntennaElevationLabel.Location = new Point(75, 56);
      AntennaElevationLabel.Name = "AntennaElevationLabel";
      AntennaElevationLabel.Size = new Size(59, 15);
      AntennaElevationLabel.TabIndex = 34;
      AntennaElevationLabel.Text = "90.0°";
      AntennaElevationLabel.TextAlign = ContentAlignment.MiddleCenter;
      toolTip1.SetToolTip(AntennaElevationLabel, "Antenna Elevation");
      // 
      // AntennaAzimuthLabel
      // 
      AntennaAzimuthLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
      AntennaAzimuthLabel.Location = new Point(8, 56);
      AntennaAzimuthLabel.Name = "AntennaAzimuthLabel";
      AntennaAzimuthLabel.Size = new Size(59, 15);
      AntennaAzimuthLabel.TabIndex = 33;
      AntennaAzimuthLabel.Text = "360.0°";
      AntennaAzimuthLabel.TextAlign = ContentAlignment.MiddleCenter;
      toolTip1.SetToolTip(AntennaAzimuthLabel, "Antenna Azimuth");
      // 
      // StopBtn
      // 
      StopBtn.Location = new Point(146, 44);
      StopBtn.Name = "StopBtn";
      StopBtn.Size = new Size(53, 22);
      StopBtn.TabIndex = 35;
      StopBtn.Text = "STOP";
      StopBtn.UseVisualStyleBackColor = true;
      StopBtn.Click += StopBtn_Click;
      // 
      // RotatorControl
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      BorderStyle = BorderStyle.FixedSingle;
      Controls.Add(StopBtn);
      Controls.Add(AntennaElevationLabel);
      Controls.Add(AntennaAzimuthLabel);
      Controls.Add(TrackCheckbox);
      Controls.Add(label2);
      Controls.Add(SatelliteElevationLabel);
      Controls.Add(label1);
      Controls.Add(SatelliteAzimuthLabel);
      Name = "RotatorControl";
      Size = new Size(210, 73);
      ResumeLayout(false);
      PerformLayout();
    }

    #endregion

    private Label SatelliteAzimuthLabel;
    private Label label1;
    private Label label2;
    private Label SatelliteElevationLabel;
    private Label AntennaElevationLabel;
    private Label AntennaAzimuthLabel;
    private Button StopBtn;
    public CheckBox TrackCheckbox;
    private VE3NEA.ToolTipEx toolTip1;
  }
}
