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
