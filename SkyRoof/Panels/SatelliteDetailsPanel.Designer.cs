namespace SkyRoof
{
  partial class SatelliteDetailsPanel
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
      SatnogsLabel = new LinkLabel();
      WebsiteLabel = new LinkLabel();
      ImageLabel = new LinkLabel();
      LinksPanel = new FlowLayoutPanel();
      SatAkaLabel = new Label();
      SatellitePropertyGrid = new VE3NEA.PropertyGridEx();
      SatNameLabel = new Label();
      toolTip1 = new VE3NEA.ToolTipEx(components);
      LinksPanel.SuspendLayout();
      SuspendLayout();
      // 
      // SatnogsLabel
      // 
      SatnogsLabel.AutoSize = true;
      SatnogsLabel.Location = new Point(104, 0);
      SatnogsLabel.Name = "SatnogsLabel";
      SatnogsLabel.Size = new Size(55, 15);
      SatnogsLabel.TabIndex = 4;
      SatnogsLabel.TabStop = true;
      SatnogsLabel.Text = "SatNOGS";
      SatnogsLabel.LinkClicked += SatnogsLabel_LinkClicked;
      // 
      // WebsiteLabel
      // 
      WebsiteLabel.AutoSize = true;
      WebsiteLabel.Location = new Point(49, 0);
      WebsiteLabel.Name = "WebsiteLabel";
      WebsiteLabel.Size = new Size(49, 15);
      WebsiteLabel.TabIndex = 3;
      WebsiteLabel.TabStop = true;
      WebsiteLabel.Text = "Website";
      WebsiteLabel.LinkClicked += WebsiteLabel_LinkClicked;
      // 
      // ImageLabel
      // 
      ImageLabel.AutoSize = true;
      ImageLabel.Location = new Point(3, 0);
      ImageLabel.Name = "ImageLabel";
      ImageLabel.Size = new Size(40, 15);
      ImageLabel.TabIndex = 0;
      ImageLabel.TabStop = true;
      ImageLabel.Text = "Image";
      ImageLabel.LinkClicked += ImageLabel_LinkClicked;
      // 
      // LinksPanel
      // 
      LinksPanel.Controls.Add(ImageLabel);
      LinksPanel.Controls.Add(WebsiteLabel);
      LinksPanel.Controls.Add(SatnogsLabel);
      LinksPanel.Dock = DockStyle.Bottom;
      LinksPanel.Location = new Point(0, 565);
      LinksPanel.Name = "LinksPanel";
      LinksPanel.Size = new Size(356, 19);
      LinksPanel.TabIndex = 6;
      // 
      // SatAkaLabel
      // 
      SatAkaLabel.Dock = DockStyle.Top;
      SatAkaLabel.Location = new Point(0, 23);
      SatAkaLabel.Name = "SatAkaLabel";
      SatAkaLabel.Size = new Size(356, 23);
      SatAkaLabel.TabIndex = 7;
      SatAkaLabel.Text = "___";
      SatAkaLabel.TextAlign = ContentAlignment.MiddleCenter;      
      // 
      // SatellitePropertyGrid
      // 
      SatellitePropertyGrid.BackColor = SystemColors.Control;
      SatellitePropertyGrid.DisabledItemForeColor = SystemColors.WindowText;
      SatellitePropertyGrid.Dock = DockStyle.Fill;
      SatellitePropertyGrid.HelpVisible = false;
      SatellitePropertyGrid.Location = new Point(0, 46);
      SatellitePropertyGrid.Name = "SatellitePropertyGrid";
      SatellitePropertyGrid.PropertySort = PropertySort.Categorized;
      SatellitePropertyGrid.Size = new Size(356, 519);
      SatellitePropertyGrid.TabIndex = 8;
      SatellitePropertyGrid.ToolbarVisible = false;
      // 
      // SatNameLabel
      // 
      SatNameLabel.Dock = DockStyle.Top;
      SatNameLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
      SatNameLabel.Location = new Point(0, 0);
      SatNameLabel.Name = "SatNameLabel";
      SatNameLabel.Size = new Size(356, 23);
      SatNameLabel.TabIndex = 9;
      SatNameLabel.Text = "___";
      SatNameLabel.TextAlign = ContentAlignment.MiddleCenter;
      // 
      // SatelliteDetailsPanel
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(356, 584);
      Controls.Add(SatellitePropertyGrid);
      Controls.Add(SatAkaLabel);
      Controls.Add(LinksPanel);
      Controls.Add(SatNameLabel);
      Name = "SatelliteDetailsPanel";
      StartPosition = FormStartPosition.CenterParent;
      Text = "Satellite Details";
      FormClosing += SatelliteDetailsPanel_FormClosing;
      Shown += SatelliteDetailsPanel_Shown;
      LinksPanel.ResumeLayout(false);
      LinksPanel.PerformLayout();
      ResumeLayout(false);
    }

    #endregion
    private LinkLabel SatnogsLabel;
    private LinkLabel WebsiteLabel;
    private LinkLabel ImageLabel;
    private FlowLayoutPanel LinksPanel;
    public Label SatAkaLabel;
    public VE3NEA.PropertyGridEx SatellitePropertyGrid;
    public Label SatNameLabel;
    private VE3NEA.ToolTipEx toolTip1;
  }
}