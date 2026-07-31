using VE3NEA.SkyTlm.Core;

namespace SkyRoof.Satellites
{
  /// <summary>
  /// Pairing of transmitters that share one downlink frequency, so that selecting either member of an
  /// SSTV + telemetry pair starts both decoders. Candidates are the transmitters of the selected
  /// transmitter's own satellite whose <c>downlink_low</c> is exactly equal — the same test the discovery
  /// hypothesis source uses (<c>TelemetryPanel.CoChannelParams</c>). A frequency tolerance is deliberately
  /// not used: exact equality is predictable, and anything more than ~2 kHz away is beyond the demod's CFO
  /// search anyway. Pure functions over the DB objects, with no dependency on the panel.
  /// </summary>
  internal static class CoChannel
  {
    /// <summary>True if any transmitter on this frequency advertises SSTV, <b>including the argument
    /// itself</b>, so a single transmitter that carries both modes keeps working through this same path.
    /// The DB <c>alive</c> flag is ignored: most SSTV rows are marked dead while their telemetry siblings
    /// are live, and the SSTV decoder self-gates on VIS/sync, so an inactive one costs only filter CPU.</summary>
    public static bool HasCoChannelSstv(SatnogsDbSatellite? satellite, SatnogsDbTransmitter? transmitter)
    {
      if (transmitter == null) return false;
      if (SignalParamsResolver.HasSstv(transmitter)) return true;
      return CoChannelSiblings(satellite, transmitter).Any(SignalParamsResolver.HasSstv);
    }

    /// <summary>The co-channel transmitter that should drive the telemetry pipeline when the selection
    /// itself is not telemetry-decodable (an SSTV selection). Ranked <c>alive</c> descending, then baud
    /// descending; exactly one is returned, or null when the frequency carries no decodable sibling.
    /// <c>alive</c> ranks but never filters — the flag is unreliable enough that the discovery hypothesis
    /// source ignores it outright — and a wrong pick is cheap, because the pipeline self-gates and simply
    /// decodes nothing.</summary>
    public static SatnogsDbTransmitter? RankedTelemetrySibling(SatnogsDbSatellite? satellite, SatnogsDbTransmitter? transmitter)
    {
      if (transmitter == null) return null;
      return CoChannelSiblings(satellite, transmitter)
        .Select(tx => (Tx: tx, Params: SignalParamsResolver.Resolve(tx)))
        .Where(c => SignalParamsResolver.IsTelemetryDecodable(c.Params))
        .OrderByDescending(c => c.Tx.alive)
        .ThenByDescending(c => c.Params!.Baud)
        .Select(c => c.Tx)
        .FirstOrDefault();
    }

    // transmitters of the same satellite on exactly the same downlink, excluding the argument. A null
    // downlink_low (three transmitters in the DB have one) matches nothing, not every other null.
    private static IEnumerable<SatnogsDbTransmitter> CoChannelSiblings(SatnogsDbSatellite? satellite, SatnogsDbTransmitter transmitter)
    {
      if (satellite == null || transmitter.downlink_low == null) return Enumerable.Empty<SatnogsDbTransmitter>();
      return satellite.Transmitters.Where(tx =>
        !ReferenceEquals(tx, transmitter) && tx.downlink_low == transmitter.downlink_low);
    }
  }
}
