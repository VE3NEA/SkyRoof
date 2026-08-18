namespace SkyRoof
{
  partial class AudioWaterfallWidget
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
      toolTip1 = new VE3NEA.ToolTipEx(components);
      SuspendLayout();
      // 
      // AudioWaterfallWidget
      // 
      AutoScaleDimensions = new SizeF(7F, 17F);
      AutoScaleMode = AutoScaleMode.Font;
      Cursor = Cursors.Cross;
      DoubleBuffered = true;
      Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
      Name = "AudioWaterfallWidget";
      Size = new Size(472, 182);
      Resize += AudioWaterfallWidget_Resize;
      ResumeLayout(false);
    }

    #endregion

    private VE3NEA.ToolTipEx toolTip1;
  }
}
