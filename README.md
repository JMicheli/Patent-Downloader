# Patent Downloader

A lightweight WPF application for downloading PDFs given an input list of patents.

## Usage

Prepare a text file containing patent numbers separated by newlines:

```text
US10534234
us10534235
US 10,534,236
US10534237
US10534238
US 10534239
```

This file can be loaded in Patent Downloader to specify patents to download.

Patent Downloader is not particularly picky about formatting, and will accept inputs with spaces, commas, and without case sensitivity. Unwanted unicode characters are also sanitized out. However, it is imporant that each patent be separated by a newline (*i.e.*, by pressing <kbd>Enter</kbd>).

After this, you can press the "Start" button to begin the download process. Patents will be downloaded to the same directory as the input text file.

## Building from source

The application can be built from Visual Studio (noting to set the Release configuration) or from the command line with the following commands:

Restore NuGet packages

`dotnet restore`

Build

`dotnet build --configuration Release`

The output will be located in `PDL/bin/Release/`.

© Joseph W. Micheli 2021, all rights reserved. See `license.txt` for further information.
