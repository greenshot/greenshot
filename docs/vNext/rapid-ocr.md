# RapidOCR text recognition

The plugin `Greenshot.Plugin.RapidOcr` adds a second OCR engine next to Windows OCR: the PaddleOCR models (PP-OCRv6 small) on
ONNX Runtime, run the way [RapidOCR](https://github.com/RapidAI/RapidOCR) runs them. It reads small and underlined screen text
(links, e-mail addresses) much better than Windows OCR, and many languages with one model.

## Choosing it

*Settings*, *Capture*, *Text recognition (OCR)*, *Recognized by*: *Windows OCR* (default) or *RapidOCR (PaddleOCR models)*.
The choice is kept in `[Win10]` `OcrEngine` of greenshot.ini (empty: Windows OCR). The language choice is only shown for Windows
OCR; RapidOCR doesn't need one.

Every OCR of Greenshot uses the chosen engine: capturing text (T), the OCR destination, the OCR and text steps of recipes, the
text of the editor (blur strength, Redact Text).

## The download

Nothing large is installed. When RapidOCR is chosen the first time, a download window asks before it downloads about 35 MB:

| File | From | Size |
|---|---|---|
| `onnxruntime.dll` 1.31.1 (MIT) | Microsoft's release `onnxruntime-win-x64-1.31.1.zip` on github.com/microsoft/onnxruntime; only this file is read out of the zip with range requests | 6 MB transferred, 17 MB on disk |
| `onnxruntime-LICENSE.txt` | the same zip | 1 KB |
| `PP-OCRv6_det_small.onnx` (Apache 2.0) | the RapidOCR model files | 10 MB |
| `PP-OCRv6_rec_small.onnx` (Apache 2.0) | the RapidOCR model files | 21 MB |

- The files go to the folder `RapidOCR` next to greenshot.ini (so the portable Greenshot keeps them next to itself). The uninstaller
  removes the folder.
- Every file has a fixed size and SHA-256 in the code. A download with another hash is not saved; before the first OCR all files
  are checked again, and a file which changed is never loaded.
- When RapidOCR is chosen but its files are missing (e.g. the folder was deleted) or changed, Windows OCR reads that capture and
  the download window asks once: download them again, or use Windows OCR from now on (the setting changes to Windows OCR).
- The same window shows the download: a progress bar, the file and the MB received. *Cancel* stops it, *Inform me when done*
  closes the window and the download goes on; Greenshot then shows a notification when RapidOCR is ready or why it failed.
  Closing the window with its title bar does the same. When the window stays open it shows the result itself, without a notification.
- Until the download is done, and when it fails, is cancelled or declined, Windows OCR recognizes the text; the setting goes back
  to the engine chosen before (unless another one was chosen meanwhile).

## How it reads

Greenshot runs the steps of RapidOCR 3.10 itself (ported to C#, Apache 2.0); RapidOcrNet targets only .NET 8+:

1. The image is kept between 30 and 2000 pixels per side; low or very wide images get black rows above and below.
2. The detection model gives a probability map of text pixels (at the size of the capture: RapidOCR enlarges the shorter side to 736 pixels for photos, which found the same lines on captures but took up to 40 times longer). Areas above 0.3 become the
   smallest rotated rectangle around them, kept when their mean is above 0.5, grown by area x 1.6 / perimeter.
3. Each line is cut out upright and read by the recognition model (48 pixels high); greedy CTC decoding gives the characters and
   where each word is, so the word boxes fit the text selection of Greenshot. Lines with a mean probability below 0.5 are dropped.
4. No orientation model: text on screen isn't upside down.

## What both engines give

Every line and word has a box: a rectangle with the angle of the text (`OcrBox` in `Greenshot.Base/Interfaces/Plugin`: centre,
width, height, angle in degrees clockwise). RapidOCR gives it per line, the words are cut from it along the text. Windows OCR
reports the rects deskewed with one angle for the whole capture, turned around the centre of the bitmap it read; Greenshot turns
them back. `Bounds` is only the upright rectangle around the box, for code that works with rectangles. RapidOCR also gives a
score per line and word (the mean probability of the characters), Windows OCR none.

- The text tool of the capture window draws the boxes turned, selects the words whose box overlaps the selection and finds the
  line under the mouse on the turned box. A click on rotated text keeps only that line.
- The obfuscation strength uses the height of the text itself, not of the rectangle around rotated text. Obfuscations
  themselves stay upright rectangles (the editor has no rotated elements yet).

On the impressum page of getgreenshot.org with underlined links it reads all five links and the e-mail address exactly, where
Windows OCR read `https;L'githumconu'grrensh@/greenshot,'` and `getgceenshQt@gmail.com`.

## Code

- `Greenshot.Base/Interfaces/Ocr/IOcrEngine.cs`: an engine (id, name, ready, prepare); `Win10OcrProvider` is one.
  `OcrEngineSelector` (Greenshot) is the `IOcrProvider` everybody uses, it picks the configured engine when it is ready.
- `Greenshot.Plugin.RapidOcr/Recognition`: `BgrImage` (pixels as the models read them), `TextBoxFinder` (DB post processing),
  `TextModels` (detector, recognizer, CTC), `RapidOcrPipeline`.
- `Greenshot.Plugin.RapidOcr/Files`: `RapidOcrFiles` (the pinned files, download with progress, check), `RemoteZip` (one entry
  of a zip on a server by range requests).
- `Greenshot.Plugin.RapidOcr/Views/RapidOcrDownloadWindow` with `ViewModels/RapidOcrDownloadViewModel`: question, progress, result.
- Tests: `Greenshot.Tests/RapidOcr`. With `GREENSHOT_RAPIDOCR_MODELS` (folder with `det.onnx` and `rec.onnx`) and
  `GREENSHOT_OCR_SAMPLES` (folder with PNG captures) `Samples_AreRead` writes `<name>.rapidocr.txt` per capture;
  `GREENSHOT_RAPIDOCR_ONLINE` downloads onnxruntime.dll from the release and checks its hash.
