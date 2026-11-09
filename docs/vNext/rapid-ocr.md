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

Nothing large is installed. When RapidOCR is chosen the first time, Greenshot asks before it downloads about 35 MB:

| File | From | Size |
|---|---|---|
| `onnxruntime.dll` 1.31.1 (MIT) | Microsoft's release `onnxruntime-win-x64-1.31.1.zip` on github.com/microsoft/onnxruntime; only this file is read out of the zip with range requests | 6 MB transferred, 17 MB on disk |
| `onnxruntime-LICENSE.txt` | the same zip | 1 KB |
| `PP-OCRv6_det_small.onnx` (Apache 2.0) | the RapidOCR model files | 10 MB |
| `PP-OCRv6_rec_small.onnx` (Apache 2.0) | the RapidOCR model files | 21 MB |

- The files go to the folder `RapidOCR` next to greenshot.ini (so the portable Greenshot keeps them next to itself). The uninstaller
  removes the folder.
- Every file has a fixed size and SHA-256 in the code. A download with another hash is not saved; before the first OCR all files
  are checked again, and a file which changed is never loaded (Windows OCR is used, choosing RapidOCR again downloads it again).
- Until the download is done, and when it fails or is declined, Windows OCR recognizes the text. A notification says when RapidOCR
  is ready or why it failed; declining switches the setting back.

## How it reads

Greenshot runs the steps of RapidOCR 3.10 itself (ported to C#, Apache 2.0); RapidOcrNet targets only .NET 8+:

1. The image is kept between 30 and 2000 pixels per side; low or very wide images get black rows above and below.
2. The detection model gives a probability map of text pixels (the shorter side at least 736 pixels). Areas above 0.3 become the
   smallest rotated rectangle around them, kept when their mean is above 0.5, grown by area x 1.6 / perimeter.
3. Each line is cut out upright and read by the recognition model (48 pixels high); greedy CTC decoding gives the characters and
   where each word is, so the word boxes fit the text selection of Greenshot. Lines with a mean probability below 0.5 are dropped.
4. No orientation model: text on screen isn't upside down.

On the impressum page of getgreenshot.org with underlined links it reads all five links and the e-mail address exactly, where
Windows OCR read `https;L'githumconu'grrensh@/greenshot,'` and `getgceenshQt@gmail.com`.

## Code

- `Greenshot.Base/Interfaces/Ocr/IOcrEngine.cs`: an engine (id, name, ready, prepare); `Win10OcrProvider` is one.
  `OcrEngineSelector` (Greenshot) is the `IOcrProvider` everybody uses, it picks the configured engine when it is ready.
- `Greenshot.Plugin.RapidOcr/Recognition`: `BgrImage` (pixels as the models read them), `TextBoxFinder` (DB post processing),
  `TextModels` (detector, recognizer, CTC), `RapidOcrPipeline`.
- `Greenshot.Plugin.RapidOcr/Files`: `RapidOcrFiles` (the pinned files, download, check), `RemoteZip` (one entry of a zip on a
  server by range requests).
- Tests: `Greenshot.Tests/RapidOcr`. With `GREENSHOT_RAPIDOCR_MODELS` (folder with `det.onnx` and `rec.onnx`) and
  `GREENSHOT_OCR_SAMPLES` (folder with PNG captures) `Samples_AreRead` writes `<name>.rapidocr.txt` per capture;
  `GREENSHOT_RAPIDOCR_ONLINE` downloads onnxruntime.dll from the release and checks its hash.
