namespace SkyRoof
{
  public class CatControl
  {
    public Context ctx;
    public CatControlEngine? Rx, Tx;
    private bool rxEngineIncludesTx;
    private bool rxEngineCrossband;

    /// <summary>
    /// Ensures CAT engines match settings and link topology. Does not restart engines
    /// when only frequency/mode changed — <see cref="FrequencyWidget"/> pushes those via
    /// SetRxFrequency / SetRxMode / SetTxFrequency on the live engines.
    /// </summary>
    internal void ApplyTune()
    {
      var link = ctx.FrequencyControl.RadioLink;
      bool crossband = link.IsCrossBand;
      bool wantRx = ctx.Settings.Cat.RxCat.Enabled;
      bool wantTx = ctx.Settings.Cat.TxCat.Enabled && link.HasUplink;
      bool wantShare = wantTx && wantRx && IsSameEngine(ctx.Settings.Cat.TxCat, ctx.Settings.Cat.RxCat);
      bool rxAlsoTx = wantShare; // wantShare already implies wantTx

      SyncRxEngine(wantRx, rxAlsoTx, crossband);
      SyncTxEngine(wantTx, wantShare, crossband);

      ctx.MainForm.ShowCatStatus();
    }

    internal void ApplySettings()
    {
      DestroyAllEngines();
      ApplyTune();
    }

    private void SyncRxEngine(bool wantRx, bool rxAlsoTx, bool crossband)
    {
      if (!wantRx)
      {
        if (Rx != null) RemoveRxEngine();
        return;
      }

      if (Rx == null)
      {
        Rx = CreateRxEngine();
        Rx.Start(true, rxAlsoTx, crossband);
        rxEngineIncludesTx = rxAlsoTx;
        rxEngineCrossband = crossband;
        if (rxAlsoTx) Tx = Rx;
        return;
      }

      if (RxEngineConfigNeedsUpdate(rxAlsoTx, crossband))
        ReconfigureRxEngine(rxAlsoTx, crossband);
    }

    private bool RxEngineConfigNeedsUpdate(bool rxAlsoTx, bool crossband) =>
      rxEngineIncludesTx != rxAlsoTx || rxEngineCrossband != crossband;

    private void SyncTxEngine(bool wantTx, bool wantShare, bool crossband)
    {
      if (!wantTx)
      {
        if (Tx != null) RemoveTxEngine();
        return;
      }

      if (wantShare)
      {
        if (Rx == null)
        {
          if (Tx != null) RemoveTxEngine();
          return;
        }

        if (Tx != null && Tx != Rx)
          RemoveTxEngine();

        Tx = Rx;
        return;
      }

      if (Tx == Rx)
        Tx = null;

      // a live standalone TX engine is kept as-is: it runs in TxOnly mode, where crossband
      // is not used, so a crossband change does not require rebuilding it
      if (Tx != null) return;

      Tx = CreateTxEngine();
      Tx.Start(false, true, crossband);
    }

    private void ReconfigureRxEngine(bool rxAlsoTx, bool crossband)
    {
      // merging TX into the shared RX engine: dispose a standalone TX engine first so
      // RemoveRxEngine + Tx = Rx does not leak the old TX thread/CAT connection.
      if (rxAlsoTx && Tx != null && Tx != Rx)
        RemoveTxEngine();

      RemoveRxEngine();
      Rx = CreateRxEngine();
      Rx.Start(true, rxAlsoTx, crossband);
      rxEngineIncludesTx = rxAlsoTx;
      rxEngineCrossband = crossband;
      if (rxAlsoTx) Tx = Rx;
    }

    private void RemoveRxEngine()
    {
      if (Rx == null) return;
      if (Tx == Rx) Tx = null;
      Rx.Dispose();
      Rx = null;
      rxEngineIncludesTx = false;
      rxEngineCrossband = false;
    }

    private void RemoveTxEngine()
    {
      if (Tx == null) return;
      if (Tx != Rx)
      {
        Tx.Dispose();
        Tx = null;
      }
      else
        Tx = null;
    }

    internal void DestroyAllEngines()
    {
      if (Tx != null && Tx != Rx) Tx.Dispose();
      if (Tx == Rx) Tx = null;
      Rx?.Dispose();
      Rx = null;
      Tx = null;
      rxEngineIncludesTx = false;
      rxEngineCrossband = false;
    }

    private CatControlEngine CreateRxEngine()
    {
      var engine = new CatControlEngine(ctx.Settings.Cat.RxCat, ctx.Settings.Cat);
      engine.RxTuned += (s, e) => ctx.FrequencyControl.RxTuned();
      // when this engine is shared (Tx == Rx) it also drives the uplink, so wire TxTuned too;
      // an RX-only engine never fires it, so the extra subscription is harmless there
      engine.TxTuned += (s, e) => ctx.FrequencyControl.TxTuned();
      engine.StatusChanged += (s, e) => ctx.MainForm.ShowCatStatus();
      return engine;
    }

    private CatControlEngine CreateTxEngine()
    {
      var engine = new CatControlEngine(ctx.Settings.Cat.TxCat, ctx.Settings.Cat);
      engine.TxTuned += (s, e) => ctx.FrequencyControl.TxTuned();
      engine.StatusChanged += (s, e) => ctx.MainForm.ShowCatStatus();
      return engine;
    }

    private static bool IsSameEngine(CatRadioSettings txCat, CatRadioSettings rxCat)
    {
      return rxCat.Enabled
        && txCat.Enabled
        && IsSameHostPort(rxCat, txCat);
        //&& rxCat.RadioType == txCat.RadioType;
    }

    public static bool IsSameHostPort(CatRadioSettings txCat, CatRadioSettings rxCat)
    {
      if (rxCat.Port != txCat.Port) return false;
      if (string.IsNullOrEmpty(rxCat.Host) || string.IsNullOrEmpty(txCat.Host))
        return false;

      string host1 = rxCat.Host;
      string host2 = txCat.Host;

      if (host1 == "127.0.0.1") host1 = "localhost";
      if (host2 == "127.0.0.1") host2 = "localhost";

      return string.Equals(host1, host2, StringComparison.OrdinalIgnoreCase);
    }
  }
}