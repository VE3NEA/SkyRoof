using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using SGPdotNET.Observation;
using VE3NEA;

namespace SkyRoof
{
  public partial class SatelliteSelectorWidget : UserControl
  {
    public Context ctx;
    private bool changing;
    private SatelliteGroup SelectedGroup;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SatnogsDbSatellite[] GroupSatellites { get; private set; } = [];

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SatnogsDbSatellite SelectedSatellite { get; private set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SatnogsDbTransmitter SelectedTransmitter { get; private set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SatellitePass? SelectedPass { get; private set; }

    public event EventHandler? SelectedGroupChanged;
    public event EventHandler? SelectedSatelliteChanged;
    public event EventHandler? SelectedTransmitterChanged;
    public event EventHandler? SelectedPassChanged;


    public SatelliteSelectorWidget()
    {
      InitializeComponent();
    }

    internal void AddToGroup(SatnogsDbSatellite satellite, SatelliteGroup group)
    {
      group.SatelliteIds.Add(satellite.sat_id);
      if (group == SelectedGroup) SetSelectedGroup(group);
    }




    //----------------------------------------------------------------------------------------------
    //                events fired when dropdown selection is manually changed
    //----------------------------------------------------------------------------------------------
    private void GroupComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (changing) return;

      var group = (SatelliteGroup)GroupComboBox.SelectedItem!;
      SetSelectedGroup(group);
    }

    private void SatelliteComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (changing) return;

      var satellite = (SatnogsDbSatellite)SatelliteComboBox.SelectedItem!;
      SetSelectedSatellite(satellite);
    }

    private void TransmitterComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (changing) return;

      var transmitter = (SatnogsDbTransmitter)TransmitterComboBox.SelectedItem!;
      SetSelectedTransmitter(transmitter);
    }




    //----------------------------------------------------------------------------------------------
    //                      write new selection to settings, then show in UI
    //----------------------------------------------------------------------------------------------
    private void SetSelectedGroup(SatelliteGroup group)
    {
      if (group.SatelliteIds.Count == 0) return;

      ctx.Settings.Satellites.SelectedGroupId = group.Id;
      group.SelectedSatId ??= group.SatelliteIds[0];
      ctx.Settings.Satellites.SelectedSatelliteId = group.SelectedSatId;

      ShowSelectedGroup();
    }

    public void SetSelectedSatellite(SatnogsDbSatellite satellite)
    {
      ctx.Settings.Satellites.SelectedSatelliteId = satellite.sat_id;

      if (SelectedGroup.SatelliteIds.Contains(satellite.sat_id))
        SelectedGroup.SelectedSatId = satellite.sat_id;

      ShowSelectedSatellite();
    }

    public void SetSelectedTransmitter(SatnogsDbTransmitter tx)
    {
      ctx.Settings.Satellites.SatelliteCustomizations.GetOrCreate(tx.Satellite.sat_id)
        .SelectedTransmitterId = tx.uuid;

      if (tx.Satellite != SelectedSatellite) 
        SetSelectedSatellite(tx.Satellite);
      ShowSelectedTransmitter();
    }

    public void SetSelectedPass(SatellitePass? pass)
    {
      SelectedPass = pass;

      SelectedPassChanged?.Invoke(this, EventArgs.Empty);
    }




    //----------------------------------------------------------------------------------------------
    //                               show current settings
    //----------------------------------------------------------------------------------------------
    private void ShowSelectedGroup()
    {
      // read settings
      var sett = ctx.Settings.Satellites;
      SelectedGroup = sett.SatelliteGroups.First(g => g.Id == sett.SelectedGroupId);
      GroupSatellites = SelectedGroup.SatelliteIds
        .Select(id => ctx.SatnogsDb.GetSatellite(id))
        .Where(s => s != null)
        .Cast<SatnogsDbSatellite>()
        .ToArray();

      // select item in combobox
      changing = true;
      GroupComboBox.SelectedItem = SelectedGroup;
      changing = false;

      SelectedGroupChanged?.Invoke(this, EventArgs.Empty);

      LoadSatellites();
    }

    private void ShowSelectedSatellite()
    {
      // read settings
      var sett = ctx.Settings.Satellites;
      sett.SelectedSatelliteId ??= SelectedGroup.SelectedSatId;
      SelectedSatellite = ctx.SatnogsDb.GetSatellite(sett.SelectedSatelliteId);
      if (SelectedSatellite == null)
      {
        string? fallbackId = SelectedGroup.SatelliteIds
          .FirstOrDefault(id => ctx.SatnogsDb.GetSatellite(id) != null);
        if (fallbackId == null) return;

        sett.SelectedSatelliteId = fallbackId;
        SelectedSatellite = ctx.SatnogsDb.GetSatellite(fallbackId);
      }

      SetSatelliteInCombobox();

      SelectedSatelliteChanged?.Invoke(this, EventArgs.Empty);

      LoadTransmitters();

      SetSelectedPass(ctx.HamPasses.GetNextPass(SelectedSatellite));
    }

    private void ShowSelectedTransmitter()
    {
      if (SelectedSatellite == null || SelectedSatellite.Transmitters.Count == 0) return;

      var cust = ctx.Settings.Satellites.SatelliteCustomizations.GetOrCreate(SelectedSatellite.sat_id);
      cust.SelectedTransmitterId ??= SelectedSatellite.Transmitters[0].uuid;
      SelectedTransmitter = SelectedSatellite.Transmitters.FirstOrDefault(t => t.uuid == cust.SelectedTransmitterId);
      SelectedTransmitter ??= SelectedSatellite.Transmitters[0];

      changing = true;
      TransmitterComboBox.SelectedItem = SelectedTransmitter;
      toolTip1.SetToolTip(TransmitterComboBox, SelectedTransmitter.GetTooltipText());
      changing = false;

      SelectedTransmitterChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SetSatelliteInCombobox()
    {
      changing = true;

      // if sat is not in the group, add it to dropdown with "(not in group)"
      if (!SatelliteComboBox.Items.Contains(SelectedSatellite))
        SatelliteComboBox.Items.Add(SelectedSatellite);

      // if sat in group and the dropdown contains a sat not in group,
      // delete the not-in-group one from dropdown
      else if (GroupSatellites.Contains(SelectedSatellite) && SatelliteComboBox.Items.Count > GroupSatellites.Length )
        LoadSatellites();

      SatelliteComboBox.SelectedItem = SelectedSatellite;
      toolTip1.SetToolTip(SatelliteComboBox, SelectedSatellite.GetTooltipText());

      changing = false;

    }




    //----------------------------------------------------------------------------------------------
    //                                    load lists
    //----------------------------------------------------------------------------------------------
    public void LoadSatelliteGroups()
    {
      changing = true;
      GroupComboBox.Items.Clear();
      GroupComboBox.Items.AddRange(ctx.Settings.Satellites.SatelliteGroups.ToArray());
      changing = false;

      ShowSelectedGroup();
    }

    private void LoadSatellites()
    {
      changing = true;
      SatelliteComboBox.Items.Clear();
      SatelliteComboBox.Items.AddRange(GroupSatellites);
      changing = false;

      ShowSelectedSatellite();
    }

    private void LoadTransmitters()
    {
      changing = true;
      TransmitterComboBox.Items.Clear();
      TransmitterComboBox.Items.AddRange(SelectedSatellite.Transmitters.ToArray());
      changing = false;

      ShowSelectedTransmitter();
    }




    //----------------------------------------------------------------------------------------------
    //                                   draw events
    //----------------------------------------------------------------------------------------------
    private void SatelliteComboBox_DrawItem(object sender, DrawItemEventArgs e)
    {
      if (e.Index < 0) { e.DrawBackground(); return; }

      Color backColor = Color.White;
      Color foreColor = e.ForeColor;
      Font font = e.Font;

      var sat = (SatnogsDbSatellite)SatelliteComboBox.Items[e.Index]!;

      if (sat.Flags.HasFlag(SatelliteFlags.Uhf)) backColor = Color.LightCyan;
      else if (sat.Flags.HasFlag(SatelliteFlags.Vhf)) backColor = Color.LightGoldenrodYellow;

      if (sat.Flags.HasFlag(SatelliteFlags.Ham)) font = new(font, FontStyle.Bold);
      if (!sat.status.StartsWith("alive")) foreColor = Color.Silver;
      else if (sat.Tle == null) font = new(font, FontStyle.Strikeout);

      e.Graphics.FillRectangle(new SolidBrush(backColor), e.Bounds);
      e.DrawFocusRectangle();

      string text = sat.name;
      if (!GroupSatellites.Contains(sat)) text += " (not in group)";
      e.Graphics.DrawString(text, font, Brushes.Black, e.Bounds);
    }

    private void TransmitterComboBox_DrawItem(object sender, DrawItemEventArgs e)
    {
      if (e.Index < 0) { e.DrawBackground(); return; }

      Brush bacBrush = Brushes.White;
      Brush foreBrush = Brushes.Black;

      Font font = e.Font;

      var tx = (SatnogsDbTransmitter)TransmitterComboBox.Items[e.Index];

      if (tx.IsUhf()) bacBrush = Brushes.LightCyan;
      else if (tx.IsVhf()) bacBrush = Brushes.LightGoldenrodYellow;

      if (tx.service == "Amateur") font = new(font, FontStyle.Bold);
      if (!tx.alive || tx.status != "active") foreBrush = Brushes.Silver;

      e.Graphics.FillRectangle(bacBrush, e.Bounds);
      e.Graphics.DrawString(tx.description, font, foreBrush, e.Bounds);
      e.DrawFocusRectangle();
    }
  }
}
