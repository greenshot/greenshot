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
| `onnxruntime-ThirdPartyNotices.txt` | the same zip: the notices of the libraries built into onnxruntime.dll | 32 KB transferred, 340 KB on disk |
| `PP-OCRv6_det_small.onnx` (Apache 2.0) | the Greenshot release `rapidocr-models-1` on github.com/greenshot/greenshot: unchanged copies of the RapidOCR model files, with their licenses | 10 MB |
| `PP-OCRv6_rec_small.onnx` (Apache 2.0) | the Greenshot release `rapidocr-models-1` on github.com/greenshot/greenshot: unchanged copies of the RapidOCR model files, with their licenses | 21 MB |

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

1. The image is kept between 30 and 2000 pixels per side for the detection; low or very wide images get black rows above and below.
   The lines are cut out of the capture itself (RapidOCR cuts them from the shrunk image: on captures over 2000 pixels, e.g. two
   screens, that read `https:/github.com`, `gmall.com`, `Telemedla` where the capture itself is read right).
   Captures over 4000 pixels per side (several screens) are searched in overlapping tiles of at most 4000 pixels, so the detection
   never sees text at less than half its size; parts of one line from two tiles become one line. At a quarter of the size the
   detection found 22 of about 580 lines on three screens of text, in tiles all of them.
2. The detection model gives a probability map of text pixels (at the size of the capture: RapidOCR enlarges the shorter side to 736 pixels for photos, which found the same lines on captures but took up to 40 times longer). Areas above 0.3 become the
   smallest rotated rectangle around them, kept when their mean is above 0.5, grown by area x 1.6 / perimeter.
3. Each line is cut out upright and read by the recognition model (48 pixels high); greedy CTC decoding gives the characters and
   where each word is, so the word boxes fit the text selection of Greenshot. Lines with a mean probability below 0.5 are dropped.
4. No orientation model: text on screen isn't upside down.

Memory: the large buffers of a run (the pixels of the capture, the model inputs and outputs, the mask of the detection) are outside
the managed heap: the pixels and the mask in native memory, the model inputs and outputs as ONNX Runtime values read as spans. So an
OCR of a large capture puts nothing on the large object heap, which is only freed by a full garbage collection. The download goes
straight to the disk (the zip entry of onnxruntime.dll through a temporary file), it is never in memory as a whole.

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

## Licenses

All parts may be used and passed on with Greenshot (GPL version 3 or later), also commercially:

| Part | License | Copyright |
|---|---|---|
| The steps of RapidOCR, ported to C# (`RapidOcrPipeline`, `TextBoxFinder`, `TextModels`) | Apache 2.0 | RapidOCR Authors |
| PP-OCRv6 small models, as RapidOCR converts them to ONNX | Apache 2.0 | Baidu / PaddleOCR rights holders |
| ONNX Runtime (`Microsoft.ML.OnnxRuntime.dll`, the downloaded `onnxruntime.dll`) | MIT | Microsoft |

What these licenses ask, and how the plug-in does it:

- The license texts go with the code and the files: the plug-in folder has `Licenses` with `NOTICE.txt`, the Apache license of
  RapidOCR, RapidOCR's `MODEL_LICENSES.md` and the MIT license of ONNX Runtime; the download adds the license and the third-party
  notices of `onnxruntime.dll` next to it.
- Changed files say so: the three ported files have a header with the copyright of the RapidOCR Authors and what Greenshot changed.
- The models are used unchanged (fixed SHA-256, the same as in RapidOCR's `MODEL_LICENSES.md`). When Greenshot hosts them itself
  (a Greenshot release), the Apache license and `MODEL_LICENSES.md` are uploaded next to them.
- The names RapidOCR, PaddleOCR and ONNX Runtime only say where the parts come from; nothing may suggest that their owners endorse
  Greenshot.
