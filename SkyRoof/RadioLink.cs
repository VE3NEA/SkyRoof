using System.Diagnostics;

namespace SkyRoof
{
  public class RadioLink
  {
    public SatnogsDbSatellite? Sat;
    public SatnogsDbTransmitter? Tx;
    public SatelliteCustomization? SatCust;
    public TransmitterCustomization? TxCust;

    // non-persistent
    public bool IsTerrestrial = true;
    public bool RitEnabled;
    public double RitOffset;
    public double XitOffset;

    // persistent
    public Slicer.Mode DownlinkMode 
    { 
      get => TxCust!.DownlinkMode; 
      set => TxCust!.DownlinkMode = value; 
    }
    public Slicer.Mode UplinkMode 
    { 
      get => TxCust!.UplinkMode; 
      set => TxCust!.UplinkMode = value; 
    }
    public double TransponderOffset
    {
      get => TxCust!.TransponderOffset;
      set => TxCust!.TransponderOffset = value;
    }
    public double CtcssTone
    {
      get => TxCust!.CtcssTone;
      set => TxCust!.CtcssTone = value;
    }
    public bool CtcssEnabled
    {
      get => TxCust!.CtcssEnabled;
      set => TxCust!.CtcssEnabled = value;
    }
    public double DownlinkManualCorrection
    {
      get => SatCust!.DownlinkManualCorrection;
      set => SatCust!.DownlinkManualCorrection = (int)value;
    }
    public double UplinkManualCorrection
    {
      get => SatCust!.UplinkManualCorrection;
      set => SatCust!.UplinkManualCorrection = (int)value;
    }

    public bool DownlinkDopplerCorrectionEnabled 
    { 
      get => SatCust!.DownlinkDopplerCorrectionEnabled; 
      set => SatCust!.DownlinkDopplerCorrectionEnabled = value; 
    }
    public bool UplinkDopplerCorrectionEnabled
    {
      get => SatCust!.UplinkDopplerCorrectionEnabled;
      set => SatCust!.UplinkDopplerCorrectionEnabled = value;
    }
    public bool DownlinkManualCorrectionEnabled
    {
      get => SatCust!.DownlinkManualCorrectionEnabled;
      set => SatCust!.DownlinkManualCorrectionEnabled = value;
    }
    public bool UplinkManualCorrectionEnabled
    {
      get => SatCust!.UplinkManualCorrectionEnabled;
      set => SatCust!.UplinkManualCorrectionEnabled = value;
    }

    // computed
    public double DownlinkFrequency, CorrectedDownlinkFrequency;
    public double UplinkFrequency, CorrectedUplinkFrequency;
    public double DopplerFactor = 0;
    public bool IsAboveHorizon;
    // true when the propagator returned a valid observation this tick (independent of elevation);
    // false means DopplerFactor was forced to 0, a discontinuity the rate estimator must not ramp
    public bool HasObservation;

    // estimates the doppler rate for the Slicer's continuous correction (see DopplerRateEstimator)
    private readonly DopplerRateEstimator DopplerEstimator = new();

    // downlink offset doppler rate (Hz/s) for the Slicer's continuous correction; 0 when correction
    // is off. CorrectedDownlinkFrequency *= (1 - DopplerFactor), so the offset's doppler term is
    // -DownlinkFrequency * DopplerFactor and its time derivative is -DownlinkFrequency * factorRate.
    public double DownlinkDopplerRate =>
      DownlinkDopplerCorrectionEnabled ? -DownlinkFrequency * DopplerEstimator.FactorRate : 0;
    public bool HasUplink => !IsTerrestrial && UplinkFrequency > 0 && SatnogsDbTransmitter.IsHamFrequency(UplinkFrequency);
    public bool IsTransponder => Tx != null &&
      Tx.downlink_high.HasValue && Tx.downlink_high != Tx.downlink_low &&
      Tx.uplink_low.HasValue && Tx.uplink_high.HasValue;
    public bool IsCrossBand => HasUplink && 
      ((SatnogsDbTransmitter.IsUhfFrequency(UplinkFrequency) != SatnogsDbTransmitter.IsUhfFrequency(DownlinkFrequency)) 
      ||
      (SatnogsDbTransmitter.IsVhfFrequency(UplinkFrequency) != SatnogsDbTransmitter.IsVhfFrequency(DownlinkFrequency)));


    public void ObserveSatellite(SatellitePasses engine)
    {
      var now = DateTime.UtcNow;
      var observation = engine.ObserveSatellite(Sat, now);

      if (observation == null)
      {
        DopplerFactor = 0;
        IsAboveHorizon = false;
        HasObservation = false;
      }
      else
      {
        DopplerFactor = observation.RangeRate / 3e5;
        IsAboveHorizon = observation.Elevation > 0;
        HasObservation = true;
      }

      // runs only here, the one place DopplerFactor is refreshed, so the estimate cannot be
      // desynced by the many other callers of ComputeFrequencies
      DopplerEstimator.Update(DopplerFactor, HasObservation, Sat, now);
    }

    internal void ComputeFrequencies()
    {
      if (IsTerrestrial)
      {
        CorrectedDownlinkFrequency = DownlinkFrequency;
        if (RitEnabled) CorrectedDownlinkFrequency += RitOffset;
        CorrectedUplinkFrequency = UplinkFrequency = 0;
        DopplerFactor = 0;
      }

      else
      {
        // downlink nominal
        DownlinkFrequency = Tx!.DownlinkLow;
        if (IsTransponder) DownlinkFrequency += TransponderOffset;

        // downlink corrected
        CorrectedDownlinkFrequency = DownlinkFrequency;
        if (RitEnabled) CorrectedDownlinkFrequency += RitOffset;
        if (DownlinkDopplerCorrectionEnabled) CorrectedDownlinkFrequency *= 1 - DopplerFactor;
        if (DownlinkManualCorrectionEnabled) CorrectedDownlinkFrequency += DownlinkManualCorrection;

        // uplink nominal
        if (IsTransponder)
          if (Tx.invert) UplinkFrequency = (double)Tx.uplink_high! - TransponderOffset;
          else UplinkFrequency = (double)Tx.uplink_low! + TransponderOffset;
        else if (Tx.uplink_low.HasValue) UplinkFrequency = (double)Tx.uplink_low;
        else UplinkFrequency = 0;

        // uplink corrected
        CorrectedUplinkFrequency = UplinkFrequency;
        if (UplinkFrequency > 0)
        {
          if (UplinkDopplerCorrectionEnabled) CorrectedUplinkFrequency *= 1 + DopplerFactor;
          if (UplinkManualCorrectionEnabled) CorrectedUplinkFrequency += UplinkManualCorrection;
          CorrectedUplinkFrequency += XitOffset;
        }
      }
    }

    // dragging changes either the absolute frequency (terrestrial),
    // or the transponder offset (transponder),
    // or the manual correction (transmitter)
    internal double GetDraggableFrequency()
    {
      if (IsTerrestrial) return DownlinkFrequency;
      else if (IsTransponder) return TransponderOffset;
      else return DownlinkManualCorrection;
    }

    internal void SetDraggableFrequency(double freq)
    {
      if (IsTerrestrial)
        DownlinkFrequency = freq;

      else if (!IsAboveHorizon)
      {
        Console.Beep();
        return;
      }

      else if (IsTransponder)
      {
        freq = Math.Max(0, Math.Min(freq, (double)(Tx!.uplink_high! - Tx!.uplink_low!)));
        TransponderOffset = freq;
      }

      else
      {
        freq = Math.Max(-25000, Math.Min(25000, freq));
        DownlinkManualCorrection = freq;
      }

      ComputeFrequencies();
    }

    internal void IncrementDownlinkFrequency(int delta)
    {
      // RIT
      if (RitEnabled)
      {
        long newOffset = (long)RitOffset + delta;
        RitOffset = Math.Max(-25000, Math.Min(25000, newOffset));
      }

      // terrestrial
      else if (IsTerrestrial) 
        DownlinkFrequency += delta;

      //else if (!IsAboveHorizon)
      //{
      //  Console.Beep();
      //  return;
      //}

      // transponder
      else if (IsTransponder)
      {
        long newOffset = (long)TransponderOffset + delta;
        long maxOffset = (long)Tx!.uplink_high! - (long)Tx!.uplink_low!;
        TransponderOffset = Math.Max(0, Math.Min(maxOffset, newOffset));
      }

      // transmitter
      else
      {
        double newOffset = DownlinkManualCorrection + delta;
        DownlinkManualCorrection = Math.Max(-25000, Math.Min(25000, newOffset));
      }

      ComputeFrequencies();
    }

    public void IncrementUplinkFrequency(int delta)
    {
      if (IsTerrestrial)
        UplinkFrequency += delta;
      else
      {
        double newOffset = UplinkManualCorrection + delta;
        if (newOffset >= -25000 && newOffset <= 25000)
          UplinkManualCorrection = newOffset;
      }

      ComputeFrequencies();
    }
  }
}
