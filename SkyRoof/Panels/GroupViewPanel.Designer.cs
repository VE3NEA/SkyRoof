namespace SkyRoof
{
  partial class GroupViewPanel
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
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GroupViewPanel));
      listView1 = new VE3NEA.ListViewEx();
      columnHeader1 = new ColumnHeader();
      columnHeader2 = new ColumnHeader();
      columnHeader3 = new ColumnHeader();
      columnHeader4 = new ColumnHeader();
      contextMenuStrip1 = new ContextMenuStrip(components);
      SatelliteDetailsMNU = new ToolStripMenuItem();
      MonitorSatelliteMNU = new ToolStripMenuItem();
      imageList1 = new ImageList(components);
      GroupNameLabel = new Label();
      contextMenuStrip1.SuspendLayout();
      SuspendLayout();
      // 
      // listView1
      // 
      listView1.Activation = ItemActivation.OneClick;
      listView1.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2, columnHeader3, columnHeader4 });
      listView1.ContextMenuStrip = contextMenuStrip1;
      listView1.Dock = DockStyle.Fill;
      listView1.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
      listView1.FullRowSelect = true;
      listView1.Location = new Point(0, 23);
      listView1.MultiSelect = false;
      listView1.Name = "listView1";
      listView1.ShowGroups = false;
      listView1.ShowItemToolTips = true;
      listView1.Size = new Size(414, 383);
      listView1.SmallImageList = imageList1;
      listView1.Sorting = SortOrder.Ascending;
      listView1.StateImageList = imageList1;
      listView1.TabIndex = 8;
      listView1.UseCompatibleStateImageBehavior = false;
      listView1.View = View.Details;
      listView1.VirtualMode = true;
      listView1.ColumnClick += listView1_ColumnClick;
      listView1.RetrieveVirtualItem += listView1_RetrieveVirtualItem;
      listView1.SelectedIndexChanged += listView1_SelectedIndexChanged;
      listView1.MouseDown += listView1_MouseDown;
      // 
      // columnHeader1
      // 
      columnHeader1.Text = "Name";
      columnHeader1.Width = 180;
      // 
      // columnHeader2
      // 
      columnHeader2.Text = "NORAD ID";
      columnHeader2.Width = 80;
      // 
      // columnHeader3
      // 
      columnHeader3.Text = "Next Pass";
      columnHeader3.TextAlign = HorizontalAlignment.Right;
      columnHeader3.Width = 85;
      // 
      // columnHeader4
      // 
      columnHeader4.Text = "Max";
      columnHeader4.TextAlign = HorizontalAlignment.Right;
      columnHeader4.Width = 40;
      // 
      // contextMenuStrip1
      // 
      contextMenuStrip1.Items.AddRange(new ToolStripItem[] { SatelliteDetailsMNU, MonitorSatelliteMNU });
      contextMenuStrip1.Name = "contextMenuStrip1";
      contextMenuStrip1.Size = new Size(198, 48);
      contextMenuStrip1.Opening += contextMenuStrip1_Opening;
      // 
      // SatelliteDetailsMNU
      // 
      SatelliteDetailsMNU.Name = "SatelliteDetailsMNU";
      SatelliteDetailsMNU.Size = new Size(159, 22);
      SatelliteDetailsMNU.Text = "Satellite Details...";
      SatelliteDetailsMNU.Click += SatelliteDetailsMNU_Click;
      // 
      // MonitorSatelliteMNU
      // 
      MonitorSatelliteMNU.Name = "MonitorSatelliteMNU";
      MonitorSatelliteMNU.Size = new Size(197, 22);
      MonitorSatelliteMNU.Text = "Monitor Satellite";
      MonitorSatelliteMNU.Click += MonitorSatelliteMNU_Click;
      // 
      // imageList1
      // 
      imageList1.ColorDepth = ColorDepth.Depth32Bit;
      imageList1.ImageStream = (ImageListStreamer)resources.GetObject("imageList1.ImageStream");
      imageList1.TransparentColor = Color.Transparent;
      imageList1.Images.SetKeyName(0, "checkmark.bmp");
      imageList1.Images.SetKeyName(1, "ok_small.png");
      imageList1.Images.SetKeyName(2, "xmark_small.png");
      imageList1.Images.SetKeyName(3, "question_small.png");
      // 
      // GroupNameLabel
      // 
      GroupNameLabel.Dock = DockStyle.Top;
      GroupNameLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
      GroupNameLabel.Location = new Point(0, 0);
      GroupNameLabel.Name = "GroupNameLabel";
      GroupNameLabel.Size = new Size(414, 23);
      GroupNameLabel.TabIndex = 9;
      GroupNameLabel.Text = "___";
      GroupNameLabel.TextAlign = ContentAlignment.MiddleCenter;
      // 
      // GroupViewPanel
      // 
      AutoScaleDimensions = new SizeF(96F, 96F);
      AutoScaleMode = AutoScaleMode.Dpi;
      ClientSize = new Size(414, 406);
      Controls.Add(listView1);
      Controls.Add(GroupNameLabel);
      Name = "GroupViewPanel";
      Text = "Current Satellite Group";
      FormClosing += GroupViewPanel_FormClosing;
      contextMenuStrip1.ResumeLayout(false);
      ResumeLayout(false);
    }

    #endregion

    public VE3NEA.ListViewEx listView1;
    private ColumnHeader columnHeader1;
    private ColumnHeader columnHeader2;
    private ColumnHeader columnHeader3;
    public Label GroupNameLabel;
    private ImageList imageList1;
    private ColumnHeader columnHeader4;
    private ContextMenuStrip contextMenuStrip1;
    private ToolStripMenuItem SatelliteDetailsMNU;
    private ToolStripMenuItem MonitorSatelliteMNU;
  }
}