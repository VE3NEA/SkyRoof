namespace SkyRoof
{
  partial class RecorderPanel
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
      StatusBar = new StatusStrip();
      StatusLabel = new ToolStripStatusLabel();
      ButtonPanel = new Panel();
      RecordBtn = new Button();
      RecordMenuBtn = new Button();
      SaveBtn = new Button();
      SaveMenuBtn = new Button();
      LoadBtn = new Button();
      PlaybackBtn = new Button();
      toolTip1 = new ToolTip(components);
      GainSlider = new TrackBar();
      RecordMenu = new ContextMenuStrip(components);
      RecordAudioMNU = new ToolStripMenuItem();
      RecordIqMNU = new ToolStripMenuItem();
      SaveMenu = new ContextMenuStrip(components);
      SaveMp3MNU = new ToolStripMenuItem();
      SaveWavMNU = new ToolStripMenuItem();
      ContainerPanel = new Panel();
      WaveformWidget = new WaveformWidget();
      SliderPanel = new Panel();
      StatusBar.SuspendLayout();
      ButtonPanel.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)GainSlider).BeginInit();
      RecordMenu.SuspendLayout();
      SaveMenu.SuspendLayout();
      ContainerPanel.SuspendLayout();
      SliderPanel.SuspendLayout();
      SuspendLayout();
      // 
      // StatusBar
      // 
      StatusBar.Items.AddRange(new ToolStripItem[] { StatusLabel });
      StatusBar.Location = new Point(0, 150);
      StatusBar.Name = "StatusBar";
      StatusBar.Size = new Size(612, 22);
      StatusBar.TabIndex = 0;
      StatusBar.Text = "statusStrip1";
      // 
      // StatusLabel
      // 
      StatusLabel.Name = "StatusLabel";
      StatusLabel.Size = new Size(39, 17);
      StatusLabel.Text = "Ready";
      // 
      // ButtonPanel
      // 
      ButtonPanel.BackColor = SystemColors.Control;
      ButtonPanel.Controls.Add(RecordBtn);
      ButtonPanel.Controls.Add(RecordMenuBtn);
      ButtonPanel.Controls.Add(SaveBtn);
      ButtonPanel.Controls.Add(SaveMenuBtn);
      ButtonPanel.Controls.Add(LoadBtn);
      ButtonPanel.Controls.Add(PlaybackBtn);
      ButtonPanel.Dock = DockStyle.Bottom;
      ButtonPanel.Location = new Point(0, 115);
      ButtonPanel.Name = "ButtonPanel";
      ButtonPanel.Size = new Size(612, 35);
      ButtonPanel.TabIndex = 1;
      // 
      // RecordBtn
      // 
      RecordBtn.Location = new Point(3, 4);
      RecordBtn.Name = "RecordBtn";
      RecordBtn.Size = new Size(57, 27);
      RecordBtn.TabIndex = 0;
      RecordBtn.Text = "Record";
      toolTip1.SetToolTip(RecordBtn, "Record Audio");
      RecordBtn.UseVisualStyleBackColor = true;
      RecordBtn.Click += RecordBtn_Click;
      // 
      // RecordMenuBtn
      // 
      RecordMenuBtn.Location = new Point(55, 4);
      RecordMenuBtn.Name = "RecordMenuBtn";
      RecordMenuBtn.Size = new Size(22, 27);
      RecordMenuBtn.TabIndex = 1;
      RecordMenuBtn.Text = "▼";
      RecordMenuBtn.UseVisualStyleBackColor = true;
      RecordMenuBtn.Click += RecordMenuBtn_Click;
      // 
      // SaveBtn
      // 
      SaveBtn.Enabled = false;
      SaveBtn.Location = new Point(84, 4);
      SaveBtn.Name = "SaveBtn";
      SaveBtn.Size = new Size(57, 27);
      SaveBtn.TabIndex = 2;
      SaveBtn.Text = "Save";
      toolTip1.SetToolTip(SaveBtn, "Save Recording");
      SaveBtn.UseVisualStyleBackColor = true;
      SaveBtn.Click += SaveBtn_Click;
      // 
      // SaveMenuBtn
      // 
      SaveMenuBtn.Enabled = false;
      SaveMenuBtn.Location = new Point(136, 4);
      SaveMenuBtn.Name = "SaveMenuBtn";
      SaveMenuBtn.Size = new Size(22, 27);
      SaveMenuBtn.TabIndex = 3;
      SaveMenuBtn.Text = "▼";
      SaveMenuBtn.UseVisualStyleBackColor = true;
      SaveMenuBtn.Click += SaveMenuBtn_Click;
      // 
      // LoadBtn
      // 
      LoadBtn.Location = new Point(166, 4);
      LoadBtn.Name = "LoadBtn";
      LoadBtn.Size = new Size(84, 27);
      LoadBtn.TabIndex = 4;
      LoadBtn.Text = "Load";
      toolTip1.SetToolTip(LoadBtn, "Load Recording");
      LoadBtn.UseVisualStyleBackColor = true;
      LoadBtn.Click += LoadBtn_Click;
      // 
      // PlaybackBtn
      // 
      PlaybackBtn.Location = new Point(256, 4);
      PlaybackBtn.Name = "PlaybackBtn";
      PlaybackBtn.Size = new Size(84, 27);
      PlaybackBtn.TabIndex = 5;
      PlaybackBtn.Text = "Play Back";
      toolTip1.SetToolTip(PlaybackBtn, "Play Back");
      PlaybackBtn.UseVisualStyleBackColor = true;
      PlaybackBtn.Click += PlaybackBtn_Click;
      // 
      // GainSlider
      // 
      GainSlider.Dock = DockStyle.Fill;
      GainSlider.LargeChange = 1;
      GainSlider.Location = new Point(0, 0);
      GainSlider.Maximum = 40;
      GainSlider.Name = "GainSlider";
      GainSlider.Orientation = Orientation.Vertical;
      GainSlider.Size = new Size(27, 115);
      GainSlider.TabIndex = 0;
      GainSlider.TickFrequency = 20;
      toolTip1.SetToolTip(GainSlider, "Gain 0 dB");
      GainSlider.ValueChanged += GainSlider_ValueChanged;
      // 
      // RecordMenu
      // 
      RecordMenu.Items.AddRange(new ToolStripItem[] { RecordAudioMNU, RecordIqMNU });
      RecordMenu.Name = "RecordMenu";
      RecordMenu.Size = new Size(147, 48);
      // 
      // RecordAudioMNU
      // 
      RecordAudioMNU.Name = "RecordAudioMNU";
      RecordAudioMNU.Size = new Size(146, 22);
      RecordAudioMNU.Text = "Record Audio";
      RecordAudioMNU.Click += RecordAudioMNU_Click;
      // 
      // RecordIqMNU
      // 
      RecordIqMNU.Name = "RecordIqMNU";
      RecordIqMNU.Size = new Size(146, 22);
      RecordIqMNU.Text = "Record I/Q";
      RecordIqMNU.Click += RecordIqMNU_Click;
      // 
      // SaveMenu
      // 
      SaveMenu.Items.AddRange(new ToolStripItem[] { SaveMp3MNU, SaveWavMNU });
      SaveMenu.Name = "SaveMenu";
      SaveMenu.Size = new Size(129, 48);
      // 
      // SaveMp3MNU
      // 
      SaveMp3MNU.Name = "SaveMp3MNU";
      SaveMp3MNU.Size = new Size(128, 22);
      SaveMp3MNU.Text = "Save .mp3";
      SaveMp3MNU.Click += SaveMp3MNU_Click;
      // 
      // SaveWavMNU
      // 
      SaveWavMNU.Name = "SaveWavMNU";
      SaveWavMNU.Size = new Size(128, 22);
      SaveWavMNU.Text = "Save .wav";
      SaveWavMNU.Click += SaveWavMNU_Click;
      // 
      // ContainerPanel
      // 
      ContainerPanel.Controls.Add(WaveformWidget);
      ContainerPanel.Controls.Add(SliderPanel);
      ContainerPanel.Dock = DockStyle.Fill;
      ContainerPanel.Location = new Point(0, 0);
      ContainerPanel.Name = "ContainerPanel";
      ContainerPanel.Size = new Size(612, 115);
      ContainerPanel.TabIndex = 3;
      // 
      // WaveformWidget
      // 
      WaveformWidget.BackColor = Color.Black; // fixed: instrument display
      WaveformWidget.Dock = DockStyle.Fill;
      WaveformWidget.Location = new Point(0, 0);
      WaveformWidget.Name = "WaveformWidget";
      WaveformWidget.Size = new Size(585, 115);
      WaveformWidget.TabIndex = 0;
      WaveformWidget.SeekRequested += WaveformWidget_SeekRequested;
      // 
      // SliderPanel
      // 
      SliderPanel.BackColor = SystemColors.Control;
      SliderPanel.Controls.Add(GainSlider);
      SliderPanel.Dock = DockStyle.Right;
      SliderPanel.Location = new Point(585, 0);
      SliderPanel.Name = "SliderPanel";
      SliderPanel.Size = new Size(27, 115);
      SliderPanel.TabIndex = 1;
      // 
      // RecorderPanel
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(612, 172);
      Controls.Add(ContainerPanel);
      Controls.Add(ButtonPanel);
      Controls.Add(StatusBar);
      Font = new Font("Segoe UI", 9F);
      Name = "RecorderPanel";
      Text = "Recorder";
      FormClosing += RecorderPanel_FormClosing;
      Shown += RecorderPanel_Shown;
      StatusBar.ResumeLayout(false);
      StatusBar.PerformLayout();
      ButtonPanel.ResumeLayout(false);
      ((System.ComponentModel.ISupportInitialize)GainSlider).EndInit();
      RecordMenu.ResumeLayout(false);
      SaveMenu.ResumeLayout(false);
      ContainerPanel.ResumeLayout(false);
      SliderPanel.ResumeLayout(false);
      SliderPanel.PerformLayout();
      ResumeLayout(false);
      PerformLayout();
    }

    #endregion

    private StatusStrip StatusBar;
    private ToolStripStatusLabel StatusLabel;
    private Panel ButtonPanel;
    private Button RecordBtn;
    private Button RecordMenuBtn;
    private ContextMenuStrip RecordMenu;
    private ToolStripMenuItem RecordAudioMNU;
    private ToolStripMenuItem RecordIqMNU;
    private Button SaveBtn;
    private Button SaveMenuBtn;
    private ContextMenuStrip SaveMenu;
    private ToolStripMenuItem SaveMp3MNU;
    private ToolStripMenuItem SaveWavMNU;
    private Button LoadBtn;
    private Button PlaybackBtn;
    private Panel ContainerPanel;
    private WaveformWidget WaveformWidget;
    private Panel SliderPanel;
    private TrackBar GainSlider;
    private ToolTip toolTip1;
  }
}
