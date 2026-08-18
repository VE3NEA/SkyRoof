namespace SkyRoof
{
  partial class SatelliteDetailsWidget
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
      splitContainer1 = new SplitContainer();
      SatellitePropertyGrid = new VE3NEA.PropertyGridEx();
      SatAkaLabel = new Label();
      listView1 = new VE3NEA.ListViewEx();
      columnHeader1 = new ColumnHeader();
      columnHeader2 = new ColumnHeader();
      columnHeader3 = new ColumnHeader();
      LinksPanel = new FlowLayoutPanel();
      ImageLabel = new LinkLabel();
      WebsiteLabel = new LinkLabel();
      SatnogsLabel = new LinkLabel();
      label2 = new Label();
      toolTip1 = new VE3NEA.ToolTipEx(components);
      SatNameLabel = new Label();
      ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
      splitContainer1.Panel1.SuspendLayout();
      splitContainer1.Panel2.SuspendLayout();
      splitContainer1.SuspendLayout();
      LinksPanel.SuspendLayout();
      SuspendLayout();
      // 
      // splitContainer1
      // 
      splitContainer1.Dock = DockStyle.Fill;
      splitContainer1.Location = new Point(0, 23);
      splitContainer1.Name = "splitContainer1";
      splitContainer1.Orientation = Orientation.Horizontal;
      // 
      // splitContainer1.Panel1
      // 
      splitContainer1.Panel1.Controls.Add(SatellitePropertyGrid);
      splitContainer1.Panel1.Controls.Add(SatAkaLabel);
      splitContainer1.Panel1MinSize = 0;
      // 
      // splitContainer1.Panel2
      // 
      splitContainer1.Panel2.Controls.Add(listView1);
      splitContainer1.Panel2.Controls.Add(LinksPanel);
      splitContainer1.Panel2.Controls.Add(label2);
      splitContainer1.Panel2MinSize = 0;
      splitContainer1.Size = new Size(477, 746);
      splitContainer1.SplitterDistance = 376;
      splitContainer1.TabIndex = 1;
      // 
      // SatellitePropertyGrid
      // 
      SatellitePropertyGrid.BackColor = SystemColors.Control;
      SatellitePropertyGrid.DisabledItemForeColor = SystemColors.WindowText;
      SatellitePropertyGrid.Dock = DockStyle.Fill;
      SatellitePropertyGrid.HelpVisible = false;
      SatellitePropertyGrid.Location = new Point(0, 23);
      SatellitePropertyGrid.Name = "SatellitePropertyGrid";
      SatellitePropertyGrid.PropertySort = PropertySort.Categorized;
      SatellitePropertyGrid.Size = new Size(477, 353);
      SatellitePropertyGrid.TabIndex = 2;
      SatellitePropertyGrid.ToolbarVisible = false;
      // 
      // SatAkaLabel
      // 
      SatAkaLabel.Dock = DockStyle.Top;
      SatAkaLabel.Location = new Point(0, 0);
      SatAkaLabel.Name = "SatAkaLabel";
      SatAkaLabel.Size = new Size(477, 23);
      SatAkaLabel.TabIndex = 3;
      SatAkaLabel.Text = "___";
      SatAkaLabel.TextAlign = ContentAlignment.MiddleCenter;
      // 
      // listView1
      // 
      listView1.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader3, columnHeader2 });
      listView1.Dock = DockStyle.Fill;
      listView1.FullRowSelect = true;
      listView1.LabelWrap = false;
      listView1.Location = new Point(0, 23);
      listView1.MultiSelect = false;
      listView1.Name = "listView1";
      listView1.ShowItemToolTips = true;
      listView1.Size = new Size(477, 324);
      listView1.Sorting = SortOrder.Ascending;
      listView1.TabIndex = 3;
      listView1.UseCompatibleStateImageBehavior = false;
      listView1.View = View.Details;
      // 
      // columnHeader1
      // 
      columnHeader1.Text = "Transmitter";
      columnHeader1.Width = 200;
      // 
      // columnHeader2
      // 
      columnHeader2.Text = "Uplink";
      columnHeader2.Width = 120;
      // 
      // columnHeader3
      // 
      columnHeader3.Text = "Downlink";
      columnHeader3.Width = 120;
      // 
      // LinksPanel
      // 
      LinksPanel.Controls.Add(ImageLabel);
      LinksPanel.Controls.Add(WebsiteLabel);
      LinksPanel.Controls.Add(SatnogsLabel);
      LinksPanel.Dock = DockStyle.Bottom;
      LinksPanel.Location = new Point(0, 347);
      LinksPanel.Name = "LinksPanel";
      LinksPanel.Size = new Size(477, 19);
      LinksPanel.TabIndex = 2;
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
      // label2
      // 
      label2.Dock = DockStyle.Top;
      label2.Font = new Font("Segoe UI", 9F);
      label2.Location = new Point(0, 0);
      label2.Name = "label2";
      label2.Size = new Size(477, 23);
      label2.TabIndex = 1;
      label2.Text = "Transmitters";
      label2.TextAlign = ContentAlignment.MiddleCenter;
      // 
      // SatNameLabel
      // 
      SatNameLabel.Dock = DockStyle.Top;
      SatNameLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
      SatNameLabel.Location = new Point(0, 0);
      SatNameLabel.Name = "SatNameLabel";
      SatNameLabel.Size = new Size(477, 23);
      SatNameLabel.TabIndex = 2;
      SatNameLabel.Text = "___";
      SatNameLabel.TextAlign = ContentAlignment.MiddleCenter;
      // 
      // SatelliteDetailsControl
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      Controls.Add(splitContainer1);
      Controls.Add(SatNameLabel);
      Name = "SatelliteDetailsControl";
      Size = new Size(477, 769);
      splitContainer1.Panel1.ResumeLayout(false);
      splitContainer1.Panel2.ResumeLayout(false);
      ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
      splitContainer1.ResumeLayout(false);
      LinksPanel.ResumeLayout(false);
      LinksPanel.PerformLayout();
      ResumeLayout(false);
    }

    #endregion

    public SplitContainer splitContainer1;
    private Label label2;
    public VE3NEA.PropertyGridEx SatellitePropertyGrid;
    public Label SatAkaLabel;
    private FlowLayoutPanel LinksPanel;
    private LinkLabel ImageLabel;
    private LinkLabel WebsiteLabel;
    private LinkLabel SatnogsLabel;
    private VE3NEA.ListViewEx listView1;
    private ColumnHeader columnHeader1;
    private ColumnHeader columnHeader2;
    private ColumnHeader columnHeader3;
    private VE3NEA.ToolTipEx toolTip1;
    public Label SatNameLabel;
  }
}
