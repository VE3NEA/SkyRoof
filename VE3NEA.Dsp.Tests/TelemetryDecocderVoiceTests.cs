using FluentAssertions;
using SkyRoof;
using VE3NEA.SkyTlm.Audio;
using VE3NEA.SkyTlm.Core;
using Xunit;

namespace VE3NEA.Dsp.Tests
{
  /// <summary>
  /// The voice wiring this repo owns: that a HADES telemetry decoder builds an audio assembler at all, and
  /// that disposing it drains the message still being received. The decoding itself is VE3NEA.SkyTlm's and
  /// is tested there — what can only break here is the three lines that connect the two.
  /// </summary>
  public class TelemetryDecocderVoiceTests
  {
    // HADES-SA's downlink: 800 bps FSK, GENESIS framing. Deviation is not needed to build the decoder.
    private static SignalParams HadesParams =>
      new(Baud: 800, Modulation.FSK, Framing.HADES, SampleRate: 48000, Deviation: 800);

    [Fact]
    public void HadesTelemetry_BuildsAVoiceAssembler()
    {
      using var decoder = new TelemetryDecocder(HadesParams, noradId: 68446, telemetry: true, sstv: false,
        fmEngine: null);

      // if this is null the native codec2 binding failed to load — see the Log.Error in the constructor,
      // which deliberately swallows that so telemetry survives it
      decoder.Voice.Should().NotBeNull("HADES framing means HADES-SA, which sends codec2 voice");
    }

    [Fact]
    public void WithoutTelemetry_ThereIsNoVoiceAssembler()
    {
      // voice rides the telemetry frames, so with no telemetry pipeline there is nothing to feed it
      using var decoder = new TelemetryDecocder(HadesParams, noradId: 68446, telemetry: false, sstv: false,
        fmEngine: null);

      decoder.Voice.Should().BeNull();
    }

    [Fact]
    public void NonHadesTelemetry_HasNoVoiceAssembler()
    {
      var ax25 = new SignalParams(Baud: 9600, Modulation.FSK, Framing.AX25G3RUH, SampleRate: 48000,
        Deviation: 4800);
      using var decoder = new TelemetryDecocder(ax25, noradId: 1, telemetry: true, sstv: false,
        fmEngine: null);

      decoder.Voice.Should().BeNull("no other satellite we decode sends codec2");
    }

    [Fact]
    public void Dispose_FlushesTheMessageStillBeingReceived()
    {
      // The reason Flush at LOS is not optional: nothing on air marks the end of a voice message, so a pass
      // that ends mid-message — which off air is the normal case — would otherwise announce nothing at all
      // and the recording would be lost rather than merely truncated.
      var decoder = new TelemetryDecocder(HadesParams, noradId: 68446, telemetry: true, sstv: false,
        fmEngine: null);
      decoder.Voice.Should().NotBeNull();

      VoiceProduct? completed = null;
      decoder.Voice!.VoiceCompleted += p => completed = p;
      decoder.Voice.Push(VoiceFrame(number: 0));
      decoder.Voice.Push(VoiceFrame(number: 1));

      decoder.Dispose();

      completed.Should().NotBeNull("Dispose must drain the assembler, not just drop it");
      completed!.SubFramesReceived.Should().Be(2);
      completed.Wav.Should().NotBeEmpty("a truncated message is still a playable file");
    }

    /// <summary>A HADES-SA type-11 frame as the deframer emits it: type/address byte, sub-frame number,
    /// 35 payload bytes. The payload content does not matter here — what is under test is the wiring, and
    /// the codec decodes any 35 bytes to something.</summary>
    private static Frame VoiceFrame(int number)
    {
      var bytes = new byte[37];
      bytes[0] = (11 << 4) | 3;
      bytes[1] = (byte)number;
      for (int i = 2; i < bytes.Length; i++) bytes[i] = (byte)(i * 7);

      // CrcValid is null because the special packet types carry no HADES CRC — not because it failed
      return new Frame
      {
        Bytes = bytes,
        CrcValid = null,
        Framing = Framing.HADES,
        TimeSeconds = 0.4 * number
      };
    }
  }
}
