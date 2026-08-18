using VE3NEA;

namespace SkyRoof
{
  public partial class PathOptimizerForm : Form
  {
    private const double ROTATION_SPEED = 6.0; // degrees per second
    // shared, so CellFormatting (fires per cell per paint) doesn't leak a GDI font handle each time
    private Font? BoldFont;

    public PathOptimizerForm()
    {
      InitializeComponent();

      dataGridView.EnableHeadersVisualStyles = false;
      dataGridView.DefaultCellStyle.SelectionBackColor = Color.Transparent;
      dataGridView.DefaultCellStyle.SelectionForeColor = SystemColors.WindowText;
      dataGridView.CellFormatting += DataGridView_CellFormatting;
      dataGridView.CellClick += (s, e) => dataGridView.ClearSelection();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
      // Close form when Escape key is pressed
      if (keyData == Keys.Escape)
      {
        Close();
        return true;
      }
      
      return base.ProcessCmdKey(ref msg, keyData);
    }

    internal void UpdateContents(OptimizedRotationPath? path)
    {
      dataGridView.Rows.Clear();

      if (path?.Optimizer?.BestPaths == null)
      {
        Text = "Optimized Rotator Paths";
        return;
      }
      Text = $"Optimized Rotator Paths for {path.Satellite!.name}";

      var currentDirection = path.Optimizer.CurrentDirection;
      var bestPaths = path.Optimizer.BestPaths;
      if (currentDirection != null) bestPaths = bestPaths.OrderBy(path => currentDirection.RotationTime(path[0])).ToList();

      // populate grid
      foreach (var p in bestPaths)
      {
        if (p.Count == 0) continue;

        var firstPoint = p[0];
        var lastPoint = p[^1];
        double rotationTime = currentDirection != null ? currentDirection.RotationTime(firstPoint) : 0;
        double displayRotationTime = Trig.DinR * rotationTime / ROTATION_SPEED;

        string currentDirStr = currentDirection != null ? $"Az: {currentDirection.AzDeg:F0}�, El: {currentDirection.ElDeg:F0}�" : "";
        string firstPointStr = $"Az: {firstPoint.AzDeg:F0}�, El: {firstPoint.ElDeg:F0}�";
        string lastPointStr = $"Az: {lastPoint.AzDeg:F0}�, El: {lastPoint.ElDeg:F0}�";

        int rowIdx = dataGridView.Rows.Add(
            currentDirStr,
            $"{displayRotationTime:F0} s",
            firstPointStr,
            lastPointStr
        );

        var row = dataGridView.Rows[rowIdx];

        row.Cells[0].Tag = currentDirection;
        row.Cells[1].Tag = displayRotationTime; // Store the numeric value for easier comparison
        row.Cells[2].Tag = firstPoint;
        row.Cells[3].Tag = lastPoint;
      }
      
      dataGridView.ClearSelection();
    }

    private void DataGridView_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
      if (e.ColumnIndex == 1 && e.RowIndex == 0)
        e.CellStyle.Font = BoldFont ??= new Font(dataGridView.Font, FontStyle.Bold);
      else
        e.CellStyle.Font = dataGridView.Font;

      if (e.RowIndex >= 0)
      {
        Bearing? bearing = null;
        if (e.ColumnIndex == 0 && dataGridView.Rows[e.RowIndex].Cells[0].Tag is Bearing b0)
          bearing = b0;
        else if (e.ColumnIndex == 2 && dataGridView.Rows[e.RowIndex].Cells[2].Tag is Bearing b2)
          bearing = b2;
        else if (e.ColumnIndex == 3 && dataGridView.Rows[e.RowIndex].Cells[3].Tag is Bearing b3)
          bearing = b3;

        if (bearing != null)
        {
          if (bearing.ElDeg > 90) e.CellStyle.BackColor = Color.SkyBlue;
          if (bearing.AzDeg > 360) e.CellStyle.ForeColor = Color.Red;

          // Make sure cell style is preserved when selected
          e.CellStyle.SelectionBackColor = e.CellStyle.BackColor;
          e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
        }
      }
    }
  }
}