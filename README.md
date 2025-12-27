# CMI Floppy Editor

CMI Floppy Editor is an application designed to manage disk images for MAME CMI2X emulation of the Fairlight CMI Series IIx. It functions as a wrapper over functions of cmios9, ImageDisk bin2imd and imdu, and MAME floptool utilities.

## Requirements

- .NET 9 SDK
- Wine (Linux only)
- bin2imd.exe, imdu.exe, floptool.exe, and cmios9.exe - these need to be placed in the Files directory

### Sourcing requirements

bin2imd.exe, imdu.exe - a part of ImageDisk toolkit, recompiled for Win32 - https://github.com/ogdenpm/disktools
floptool.exe - a part of MAME, if you didn't build it along with MAME, you can download a binary copy - https://www.mamedev.org/release.html
cmios9.exe - https://sourceforge.net/projects/cmios9/

## Installation

1. Ensure you have the .NET 9 SDK installed on your machine. You can download it from the [official .NET website](https://dotnet.microsoft.com/download/dotnet/9.0).
2. You can usually install Wine from your distribution's repositories, e.g.:
    - Ubuntu/Debian: `sudo apt install wine`
    - Fedora: `sudo dnf install wine`
3. Download the repository to your local machine.

## Build

`cd [cmifloppyeditor source directory]`

To immediately execute in debug mode: `dotnet run`

To build release executable: `dotnet build -c Release` or `dotnet publish -c Release -r linux-x64` (for Windows replace linux-x64 with win-x64)

Compiled executable and support files can be found in `bin/Release/net9.0/<platform>` or `bin/Release/net9.0/<platform>/publish` respectively

## Features

- Open or create (with custom name and owner data) .img raw disk images
- Import, export (as WAV or VC), rename, or delete samples from the raw disk image
- Convert raw disk images to .IMD, .MFI, .MFM
- Convert .IMD disk images back to raw format

## Disclaimer

This software was developed and tested first and foremost under Linux. While it should run under Windows, this is not 100% guaranteed.

## Notes

Imported samples must be in WAV format, mono unsigned 8-bit.

Any imported sample over 16kB will be trimmed. Any imported sample under 16kB will be padded with zeroes. This is a limitation of both the Fairlight CMI Series II and the cmios9 utility.

cmios9 Copyright (C) 2001-2024 by Klaus Michael Indlekofer. All rights reserved.

ImageDisk Copyright 2005-2012 Dave Dunfield All rights reserved, some tools reworked for Win32 by Mark Ogden.

Floptool is a part of the MAME project

## License

©2025 Ondřej Badín, aka Andy the Dwemer

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
Third-party software required for the functionality of the Software is understood the be the intelectual property of the respective third parties, and as such no copies of the Software shall be distributed with this third-party software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.