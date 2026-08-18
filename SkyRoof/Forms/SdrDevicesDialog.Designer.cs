using SharpGL.SceneGraph.Primitives;

namespace SkyRoof
{
  partial class SdrDevicesDialog
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
      okBtn = new Button();
      cancelBtn = new Button();
      label1 = new Label();
      Grid = new VE3NEA.PropertyGridEx();
      DeviceListMenu = new ContextMenuStrip(components);
      SelectSdrMNU = new ToolStripMenuItem();
      DeleteSdrMNU = new ToolStripMenuItem();
      imageList1 = new ImageList(components);
      toolTip = new VE3NEA.ToolTipEx(components);
      listBox1 = new VE3NEA.ListBoxEx();
      label2 = new Label();
      DeviceListMenu.SuspendLayout();
      SuspendLayout();
      // 
      // okBtn
      // 
      okBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
      okBtn.DialogResult = DialogResult.OK;
      okBtn.Location = new Point(24, 324);
      okBtn.Margin = new Padding(4, 3, 4, 3);
      okBtn.Name = "okBtn";
      okBtn.Size = new Size(88, 27);
      okBtn.TabIndex = 6;
      okBtn.Text = "OK";
      okBtn.UseVisualStyleBackColor = true;
      // 
      // cancelBtn
      // 
      cancelBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
      cancelBtn.DialogResult = DialogResult.Cancel;
      cancelBtn.Location = new Point(120, 324);
      cancelBtn.Margin = new Padding(4, 3, 4, 3);
      cancelBtn.Name = "cancelBtn";
      cancelBtn.Size = new Size(88, 27);
      cancelBtn.TabIndex = 7;
      cancelBtn.Text = "Cancel";
      cancelBtn.UseVisualStyleBackColor = true;
      // 
      // label1
      // 
      label1.AutoSize = true;
      label1.Location = new Point(12, 9);
      label1.Name = "label1";
      label1.Size = new Size(71, 15);
      label1.TabIndex = 8;
      label1.Text = "SDR Devices";
      // 
      // Grid
      // 
      Grid.BackColor = SystemColors.Control;
      Grid.Location = new Point(297, 27);
      Grid.Margin = new Padding(4, 3, 4, 3);
      Grid.Name = "Grid";
      Grid.Size = new Size(249, 324);
      Grid.TabIndex = 1;
      Grid.ToolbarVisible = false;
      // 
      // DeviceListMenu
      // 
      DeviceListMenu.Items.AddRange(new ToolStripItem[] { SelectSdrMNU, DeleteSdrMNU });
      DeviceListMenu.Name = "PropertyGridMenu";
      DeviceListMenu.Size = new Size(108, 48);
      DeviceListMenu.Opening += DeviceListMenu_Opening;
      // 
      // SelectSdrMNU
      // 
      SelectSdrMNU.Name = "SelectSdrMNU";
      SelectSdrMNU.Size = new Size(107, 22);
      SelectSdrMNU.Text = "Select";
      SelectSdrMNU.Click += SelectSdrMNU_Click;
      // 
      // DeleteSdrMNU
      // 
      DeleteSdrMNU.Name = "DeleteSdrMNU";
      DeleteSdrMNU.Size = new Size(107, 22);
      DeleteSdrMNU.Text = "Delete";
      DeleteSdrMNU.Click += DeleteSdrMNU_Click;
      // 
      // imageList1
      // 
      imageList1.ColorDepth = ColorDepth.Depth32Bit;
      imageList1.ImageSize = new Size(16, 16);
      imageList1.TransparentColor = Color.Transparent;
      // 
      // listBox1
      // 
      listBox1.ContextMenuStrip = DeviceListMenu;
      listBox1.DisplayMember = "Name";
      listBox1.DrawMode = DrawMode.OwnerDrawFixed;
      listBox1.Location = new Point(12, 27);
      listBox1.Name = "listBox1";
      listBox1.Size = new Size(268, 100);
      listBox1.TabIndex = 9;
      listBox1.DrawItem += listBox1_DrawItem;
      listBox1.SelectedIndexChanged += listBox1_SelectedIndexChanged;
      listBox1.MouseDown += listBox1_MouseDown;
      // 
      // label2
      // 
      label2.AutoSize = true;
      label2.Location = new Point(310, 9);
      label2.Name = "label2";
      label2.Size = new Size(111, 15);
      label2.TabIndex = 10;
      label2.Text = "SDR Device Settings";
      // 
      // SdrDevicesDialog
      // 
      AcceptButton = okBtn;
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      CancelButton = cancelBtn;
      ClientSize = new Size(558, 363);
      Controls.Add(label2);
      Controls.Add(listBox1);
      Controls.Add(Grid);
      Controls.Add(label1);
      Controls.Add(cancelBtn);
      Controls.Add(okBtn);
      MaximizeBox = false;
      MinimizeBox = false;
      Name = "SdrDevicesDialog";
      ShowInTaskbar = false;
      StartPosition = FormStartPosition.CenterParent;
      Text = "SDR Devices";
      FormClosing += SdrDevicesDialog_FormClosing;
      Shown += SdrDevicesDialog_Shown;
      DeviceListMenu.ResumeLayout(false);
      ResumeLayout(false);
      PerformLayout();
    }

    #endregion

    private Button okBtn;
    private Button cancelBtn;
    private Label label1;
    private VE3NEA.PropertyGridEx Grid;
    private ImageList imageList1;
    private VE3NEA.ToolTipEx toolTip;
    private ContextMenuStrip DeviceListMenu;
    private ToolStripMenuItem SelectSdrMNU;
    private VE3NEA.ListBoxEx listBox1;
    private Label label2;
    private ToolStripMenuItem DeleteSdrMNU;
  }
}