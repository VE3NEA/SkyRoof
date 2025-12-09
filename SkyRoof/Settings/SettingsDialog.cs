using System.Text.RegularExpressions;
using Accessibility;
using VE3NEA;

namespace SkyRoof

{
  internal partial class SettingsDialog : Form
  {
    private readonly Context ctx;
    private readonly List<string> ChangedFields = new();

    internal SettingsDialog()
    {
      InitializeComponent();
    }

    internal SettingsDialog(Context ctx, string section = null)
    {
      InitializeComponent();
      this.ctx = ctx;
      grid.SelectedObject = Utils.DeepClone(ctx.Settings);
      grid.ExpandAllGridItems();
      //grid.ExpandTopLevelProperties();
      SelectSection(section);
    }

    private void SelectSection(string section)
    {
      if (section == null) return;

      var gridItem = grid.GetItemByFullName(section);
      grid.ExpandAndSelect(gridItem);
    }

    private void resetToolStripMenuItem_Click(object sender, EventArgs e)
    {
      grid.ResetSelectedProperty();

      string label = PropertyGridEx.GetItemProperty(grid.SelectedGridItem, "HelpKeyword");
      ChangedFields.Add(label);
    }

    private void applyBtn_Click(object sender, EventArgs e)
    {
      ctx.Settings = Utils.DeepClone((Settings)grid.SelectedObject);
      ApplyChangedSettings();
    }

    private void okBtn_Click(object sender, EventArgs e)
    {
      ctx.Settings = (Settings)grid.SelectedObject;
      ApplyChangedSettings();
    }




    //--------------------------------------------------------------------------------------------------------------
    //                                        validate changes
    //--------------------------------------------------------------------------------------------------------------
    private void grid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
    {
      bool canChange = true;
      string label = PropertyGridEx.GetItemProperty(e.ChangedItem, "HelpKeyword");

      switch (label)
      {
        case "SkyRoof.UserSettings.Call":
          canChange = ValidateField(e, Utils.CallsignRegex, "callsign", CharacterCasing.Upper);
          break;

        case "SkyRoof.UserSettings.Square":
          canChange = ValidateField(e, Utils.GridSquare6Regex, "grid square", CharacterCasing.Upper);
          break;

        case "SkyRoof.UserSettings.Altitude":
          ValidateInt(e, 8849);
          break;

        case "SkyRoof.AnnouncerSettings.Voice":
          ctx.Announcer.SayVoiceName(e.ChangedItem.Value!.ToString());
          break;

        case "SkyRoof.AnnouncerSettings.Volume":
          ValidateInt(e, 100);
          break;

        case "SkyRoof.AosAnnouncement.Minutes":
          ValidateInt(e, 5);
          break;

        case "SkyRoof.PositionAnnouncement.Degrees":
          ValidateInt(e, 30, 1);
          break;

        case "SkyRoof.OutputStreamSettings.Gain":
          ValidateInt(e, 60, -60);
          break;
      }

      if (canChange) ChangedFields.Add(label);
    }

    private bool ValidateField(PropertyValueChangedEventArgs e, Regex regEx, string fieldName, CharacterCasing casing)
    {
      string newValue = e.ChangedItem.Value.ToString();
      string cleanValue = Utils.SetCasing(newValue.Trim(), casing);

      if (!regEx.IsMatch(cleanValue))
      {
        e.ChangedItem.PropertyDescriptor.SetValue(e.ChangedItem.Parent.Value, e.OldValue);
        MessageBox.Show($"Invalid {fieldName}: \"{newValue}\"", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        return false;
      }

      if (cleanValue != newValue) e.ChangedItem.PropertyDescriptor.SetValue(e.ChangedItem.Parent.Value, cleanValue);
      return true;
    }

    private void ValidateInt(PropertyValueChangedEventArgs e, int max, int min = 0)
    {
      int cleanValue = Math.Max(min, Math.Min(max, (int)e.ChangedItem.Value));
      e.ChangedItem.PropertyDescriptor.SetValue(e.ChangedItem.Parent.Value, cleanValue);
    }




    //--------------------------------------------------------------------------------------------------------------
    //                                            apply changes
    //--------------------------------------------------------------------------------------------------------------
    private void ApplyChangedSettings()
    {
      if (ChangedFields.Contains("SkyRoof.UserSettings.Square") ||
          ChangedFields.Contains("SkyRoof.UserSettings.Altitude"))
        ctx.MainForm.SetLocation();

      if (ChangedFields.Exists(s => s.StartsWith("SkyRoof.OutputStreamSettings.")))
        ctx.MainForm.ApplyOutputStreamSettings();

      if (ChangedFields.Exists(s => s.StartsWith("SkyRoof.AudioSettings.")))
        ctx.MainForm.ApplyAudioSettings();

      if (ChangedFields.Exists(s => s.StartsWith("SkyRoof.Announcement.Minutes")) ||
          ChangedFields.Exists(s => s.StartsWith("SkyRoof.Announcement.Enabled")))
        ctx.Announcer.RebuildQueue();

      if (ChangedFields.Exists(s => s.StartsWith("SkyRoof.CatSettings.")) ||
        ChangedFields.Exists(s => s.StartsWith("SkyRoof.CatRadioSettings")))
      {
        ctx.CatControl.ApplySettings();
        ctx.MainForm.ShowCatStatus();
      }

      if (ChangedFields.Exists(s => s.StartsWith("SkyRoof.RotatorSettings.")))
        ctx.RotatorControl.ApplySettings(true);

      if (ChangedFields.Exists(s => s.StartsWith("SkyRoof.AmsatSettings.")))
        if (ctx.Settings.Amsat.Enabled)
          ctx.AmsatStatusLoader.GetStatusesAsync();
        else
          ctx.GroupViewPanel?.ShowAmsatStatuses();

      if (ChangedFields.Exists(s => s.StartsWith("SkyRoof.QsoEntrySettings.")))
        ctx.QsoEntryPanel?.ApplySettings();

      if (ChangedFields.Exists(s => 
        s.StartsWith("SkyRoof.Ft4ConsoleSettings") ||
        s.StartsWith("SkyRoof.Ft4WaterfallSettings.") ||
        s.StartsWith("SkyRoof.Ft4MessagesSettings.") ||
        s.StartsWith("SkyRoof.Ft4BackgroundColors.") || 
        s.Contains("SkyRoof.UserSettings.Call")))
          ctx.Ft4ConsolePanel?.ApplySettings();

      ChangedFields.Clear();
    }
  }
}
