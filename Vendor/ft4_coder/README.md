# FT4 Coder

This project builds a DLL with the functions that encode a text message into FT4 audio and decode audio into a set of FT4 messages.
The repository includes wsjt-x as a sub-module, so the DLL contains the original wsjt-x code.

## Pre-requisites

- mingw32 to compile and link the binary (.dll);
- cv2pdb to generate debug info file (.pdb).

## Build

Run `build.bat` to build the project.

## Use

See the `Example` folder for the C# interop declarations and a sample C# application that encodes and decodes FT4.


Copyright (c) 2025 Alex VE3NEA
