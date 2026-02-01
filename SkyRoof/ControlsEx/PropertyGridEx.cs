using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VE3NEA
{
  internal class PropertyGridEx : PropertyGrid
  {
    protected override void OnHandleCreated(EventArgs e)
    {
      base.OnHandleCreated(e);
      GetPropertyGridView().MouseClick += grid_MouseClick;
    }
    private void grid_MouseClick(object sender, MouseEventArgs e)
    {
      var entry = GetEntryAtPoint(e.X, e.Y);
      if (entry == null || !(entry.Value is VisibleSettings)) return;

      var settings = (VisibleSettings)entry.Value;
      bool oldValue = settings.Visible;
      settings.Visible = !settings.Visible;

      Refresh();

      var args = new PropertyValueChangedEventArgs(entry, entry);
      OnPropertyValueChanged(args);
    }


    // access item's property by name
    internal static string GetItemProperty(GridItem item, string propertyName)
    {
      var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty;
      return (string)item.GetType().GetProperty(propertyName, flags).GetValue(item);
    }

    // https://stackoverflow.com/questions/4086105/
    public void ExpandTopLevelProperties(GridItem? root = null, bool twoLevels = false)
    {
      root ??= GetRoot();
      root.Expanded = true;

      if (root != null)
        foreach (GridItem item in root.GridItems)
          if (item.GridItemType == GridItemType.Property)
          {
            item.Expanded = true;
            if (twoLevels && item.Expandable)
              foreach (GridItem subitem in item.GridItems)
                subitem.Expanded = true;
          }
    }

    private Control GetPropertyGridView()
    {
      foreach (Control control in Controls)
        if (control.GetType().Name == "PropertyGridView")
          return control;

      return null;
    }

    private GridItem GetRoot()
    {
      GridItem root = SelectedGridItem;
      while (root?.Parent != null) root = root.Parent;
      return root;
    }

    private GridItem GetChildByName(GridItem parent, string fullName)
    {
      if (parent == null) return null;

      foreach (GridItem item in parent.GridItems)
        if (item.GridItemType != GridItemType.Property)
          continue;
        else if (GetItemProperty(item, "HelpKeyword") == fullName)
          return item;
        else
        {
          var result = GetChildByName(item, fullName);
          if (result != null) return result;
        }

      return null;
    }

    internal GridItem GetItemByFullName(string fullName)
    {
      return GetChildByName(GetRoot(), fullName);
    }

    private GridItem GetEntryAtPoint(int x, int y)
    {
      var grid = GetPropertyGridView();
      var flags = BindingFlags.Instance | BindingFlags.NonPublic;
      var FindPosition = grid.GetType().GetMethod("FindPosition", flags);
      if (FindPosition == null) return null;

      var point = (Point)FindPosition.Invoke(grid, new object[] { x, y });

      Point invalidPoint = new Point(int.MinValue, int.MinValue);
      if (point == invalidPoint || point.X != 2) return null;

      var GetGridEntryFromRow = grid.GetType().GetMethod("GetGridEntryFromRow", flags);
      return (GridItem)GetGridEntryFromRow.Invoke(grid, new object[] { point.Y });
    }

    internal void ExpandAndSelect(GridItem gridItem)
    {
      if (gridItem == null) return;

      ExpandAndSelect(gridItem.Parent);

      gridItem.Expanded = true;
      gridItem.Select();

    }
  }




  //--------------------------------------------------------------------------------------------------------------
  //                                          VisibleSettings     
  //--------------------------------------------------------------------------------------------------------------
  // expandable settings with a Visible property controlled by a checkbox
  internal class VisibleSettings
  {
    [Browsable(false)]
    public bool Visible { get; set; }

    internal VisibleSettings(bool value) { Visible = value; }

    public override string ToString() { return "Visible"; }
  }




  //--------------------------------------------------------------------------------------------------------------
  //                                    checkbox in the property grid
  //--------------------------------------------------------------------------------------------------------------
  // https://stackoverflow.com/questions/37659850
  public class CheckboxEditor : UITypeEditor
  {
    public override bool GetPaintValueSupported(ITypeDescriptorContext context)
    { return true; }

    public override void PaintValue(PaintValueEventArgs e)
    {
      var rect = e.Bounds;
      rect.Inflate(1, 1);
      var settings = (VisibleSettings)e.Value;
      var checkmark = (settings.Visible) ? ButtonState.Checked : ButtonState.Normal;
      ControlPaint.DrawCheckBox(e.Graphics, rect, ButtonState.Flat | checkmark);
    }
  }
}
