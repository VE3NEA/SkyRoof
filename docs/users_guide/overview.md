# Overview

SkyRoof is an open source, 64-bit Windows application for Hams and satellite enthusiasts,
available on the terms of the GPL v.3 license. It combines satellite tracking and SDR functions in one program,
which opens some interesting possibilities. For example, all satellite traces on the waterfall are labeled
with satellite names, the boundaries of the transponder segments follow the Doppler shift,
and all frequency tuning is done visually, with a mouse.

![Main Window](../images/main_window_TH.png)

## Features

The main features of SkyRoof are:

- detailed information about all satellites that transmit in the Ham bands;
- satellite tracking in real time;
- pass prediction for the selected satellites;
- visual representation of the current satellite position and future passes, using:
  - Sky View - the view of the sky from your location;
  - Earth View - the view of the Earth from the satellite;
  - Time Line - the satellite passes on the time scale;
  - Pass List - the details of the predicted passes;
- SDR-based waterfall display that covers the whole satellite segments on the VHF and UHF bands, with zoom and pan;
- SDR-based SSB/CW/FM receiver with RIT and Doppler tracking;
- audio and I/Q output to external programs via VAC or UDP;
- frequency scale with satellite names and transponder segments, Doppler-corrected;
- built-in telemetry decoder for FSK, AFSK, GFSK, GMSK and BPSK downlinks, with automatic frame submission to the SatNOGS network;
- built-in SSTV image decoder for the Robot and PD modes, with no external program required;
- FT4 Console for making FT4 contacts through linear-transponder satellites, without an external program;
- QSO logging with ADIF export, and a QSO Scheduler;
- audio recording of satellite passes;
- CAT control of an external transceiver;
- antenna rotator control.

The program can work without an SDR, or even without any radio at all, but many useful functions are not available in this mode.
