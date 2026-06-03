namespace SkyRoof
{
  public class CatControl
  {
    public Context ctx;
    public CatControlEngine? Rx, Tx;
    private bool rxEngineIncludesTx;

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
      bool rxAlsoTx = wantTx && wantShare;

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
        if (rxAlsoTx) Tx = Rx;
        return;
      }

      if (rxEngineIncludesTx != rxAlsoTx)
        ReconfigureRxEngine(rxAlsoTx, crossband);
    }

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

      if (Tx != null) return;

      Tx = CreateTxEngine();
      Tx.Start(false, true, crossband);
    }

    private void ReconfigureRxEngine(bool rxAlsoTx, bool crossband)
    {
      RemoveRxEngine();
      Rx = CreateRxEngine();
      Rx.Start(true, rxAlsoTx, crossband);
      rxEngineIncludesTx = rxAlsoTx;
      if (rxAlsoTx) Tx = Rx;
    }

    private void RemoveRxEngine()
    {
      if (Rx == null) return;
      if (Tx == Rx) Tx = null;
      Rx.Dispose();
      Rx = null;
      rxEngineIncludesTx = false;
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

    private void DestroyAllEngines()
    {
      if (Tx != null && Tx != Rx) Tx.Dispose();
      if (Tx == Rx) Tx = null;
      Rx?.Dispose();
      Rx = null;
      Tx = null;
      rxEngineIncludesTx = false;
    }

    private CatControlEngine CreateRxEngine()
    {
      var engine = new CatControlEngine(ctx.Settings.Cat.RxCat, ctx.Settings.Cat);
      engine.RxTuned += (s, e) => ctx.FrequencyControl.RxTuned();
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
