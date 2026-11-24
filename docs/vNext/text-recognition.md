# Text in your captures: OCR and more

Greenshot can find the text in a screenshot and give it to you as text you can paste, search and edit, instead of only a picture of
it. This page starts with what that means and which way fits your task; the details for the curious and for administrators
follow further down.

- [In short](#in-short)
  - [What OCR is](#what-ocr-is)
  - [What Greenshot does with text](#what-greenshot-does-with-text)
  - [Which way to choose](#which-way-to-choose)
- [Where the text comes from](#where-the-text-comes-from)
- [Getting text out](#getting-text-out)
- [Choosing the OCR engine](#choosing-the-ocr-engine)
- [Settings](#settings)
- [Privacy](#privacy)
- [When the text isn't right](#when-the-text-isnt-right)
- [Details: text from the applications](#details-text-from-the-applications)
- [Details: RapidOCR](#details-rapidocr)

## In short

### What OCR is

A screenshot is a picture: a grid of coloured dots. You can see the words in it, but your computer only sees dots, so you can't
copy a sentence out of it, search for a word or paste a phone number from it. **OCR** (optical character recognition, or *text
recognition*) looks at the picture and works out which letters are where, like a person reading it. The result is real text.

OCR reads very well, but not perfectly: tiny, blurry, decorated or underlined text can come out with a wrong letter. Check
important text (a password, an IBAN, a phone number) before you use it.

Greenshot often doesn't even need to guess: when you capture the screen, it also asks the programs on the screen for the text
they show (see [where the text comes from](#where-the-text-comes-from)). That text is exact. OCR only fills in what the programs
don't tell, e.g. text in an image or in a remote desktop.

### What Greenshot does with text

- **Copy text from the screen:** press `T` in the capture screen, click a line or drag over the words, and they are on the
  clipboard as text ([capturing text](#capturing-text-t)).
- **Copy all text of a capture:** send a capture to the *Windows OCR* destination (*OCR Text* in the destination picker), and all
  its text is on the clipboard.
- **Hide text:** blur and pixelate fit their strength to the height of the text, and *Redact Text* finds and covers words, e-mail
  addresses or numbers for you ([hiding-text.md](hiding-text.md)).
- **Find screenshots later:** saved images can keep the text, so Windows Search and file managers find a screenshot by what is
  written in it ([image-formats.md](image-formats.md#details-in-saved-images)).
- **Automate it:** recipes read text, put it in file names, hand it to a translator or another program; the command line and
  AI tools can ask for it too ([automating](#in-recipes-on-the-command-line-and-for-ai-tools)).
- **QR codes and barcodes** aren't text recognition, but give text too: Greenshot finds them while you select, see
  [QR codes](capture-user-guide.md#qr-codes).

### Which way to choose

| You want to... | Do this |
|---|---|
| Copy a few words or one line from the screen | Start a region capture and press `T`, then click the line or drag over the words. |
| Copy all the text of an area or window | Capture it and choose the *Windows OCR* destination (*OCR Text* in the picker), or use the recipe *OCR text to clipboard*. |
| Read small text, underlined links or e-mail addresses better, or several languages at once | Choose *RapidOCR* in the settings ([choosing the OCR engine](#choosing-the-ocr-engine)). |
| Read text in a language Windows OCR doesn't know yet | Add the language in Windows, or use RapidOCR. |
| Hide names, numbers or addresses before you share a capture | *Redact Text* in the editor, or blur and pixelate ([hiding-text.md](hiding-text.md)). |
| Do something with the text every time, e.g. translate it | A recipe with an `Ocr` step ([recipes](#in-recipes-on-the-command-line-and-for-ai-tools)). |

Greenshot Light has no OCR.

## Where the text comes from

Greenshot gets text in two ways and uses both together:

| | Text from the applications | OCR |
|---|---|---|
| How | Asks the programs on the screen which text they show (Windows UI Automation), when the screen is captured | Reads the pixels of the capture |
| Exact? | Yes, letter for letter, and links bring their target (web address, `mailto:`, `tel:`) | Nearly, it can misread a letter |
| Where it works | Most Windows programs and browsers | Everywhere: images, videos, remote desktops (Citrix, Remote Desktop, Horizon, Hyper-V, VNC only send pixels), programs without UI Automation |
| In the text mode (`T`) | Blue lines | Red lines |

OCR only reads where the applications gave no text, and only when text is needed: in the text mode, for the OCR destination and
the recipe `Ocr` step, and for the text obfuscation of the editor. It runs at most once per image. More in
[details: text from the applications](#details-text-from-the-applications).

## Getting text out

### Capturing text (T)

Press `T` in the capture screen (or choose *Capture text (OCR)*). Greenshot reads the text on the screen, which can take a moment
when the mode starts (a hint next to the mouse shows it is reading). Then every line of text it found gets a thin frame.

- **Click a line** to capture just that line. The line under the mouse is highlighted. On rotated text a click keeps only that line.
- **Drag over the text** to select several words or lines: the words inside the rectangle are highlighted while you drag.
  Dragging works like in the region mode, with `Enter`, the arrow keys and `Shift`.

The words in the selection are copied to the clipboard as text; the capture doesn't go to your destinations. This is a visible step
of the built-in region and window capture recipes (the branch *Text or image* after the selection), so a recipe of your own can do
something else with the text. With the recipe editor plugin switched on (Settings, Plugins tab), the built-in recipe *OCR text to
clipboard* starts the capture in this mode and copies only the text; give it a hotkey or a menu entry to use it (see
[capture-recipes.md](capture-recipes.md)).

The text mode needs the picture of the screen: on the live screen (*Freeze the screen while selecting* off) `T` freezes it first.

### The OCR destination

The destination *Windows OCR* (*OCR Text* in the destination picker, key `O`) puts all the text of the capture on the clipboard,
one line per text line. It uses the text the capture already has, else it runs OCR with the engine chosen in the settings.

### In the editor

- The *OCR* badge in the status bar (next to the size) shows whether the text of the capture is known: highlighted when there is
  text, grey when no text was found, grey and struck through when OCR failed (the tooltip says why).
- Blur, pixelate and *Redact Text* use the text, see [hiding-text.md](hiding-text.md).

### In recipes, on the command line and for AI tools

- The recipe step `Ocr` reads the text of the capture into `${payload.ocr.text}` (and the step variable `OcrText`); its `Language`
  parameter (e.g. `de-DE`) chooses the Windows OCR language. Every value which takes variables can use the text, e.g. a file name,
  the clipboard text or the input of an [external command](external-commands.md) (a translator). See
  [capture-recipes.md](capture-recipes.md#text-and-codes-on-the-capture) and [variables.md](variables.md).
- The command line: `greenshot-cli -r ocr -f doc.png` writes the text of an image to the console
  ([greenshot-proxy-and-integration.md](greenshot-proxy-and-integration.md)).
- AI tools: the capture tools of `greenshot-mcp.exe` take `ocr=true` to return the text too ([mcp-server.md](mcp-server.md)).

## Choosing the OCR engine

*Settings*, *Capture*, *Text recognition (OCR)*, *Recognized by*:

| | Windows OCR (default) | RapidOCR (PaddleOCR models) |
|---|---|---|
| Comes with | Windows 10 and 11 | the RapidOCR plugin; about 35 MB are downloaded once, when you choose it |
| Languages | the OCR languages installed in Windows, one at a time (*Language of the text*) | many languages with one model, no choice needed |
| Good at | normal screen text, fast | small and underlined text: links, e-mail addresses |
| Rotated text | yes, one angle per capture | yes, per line |

Every OCR of Greenshot uses the chosen engine: capturing text (`T`), the OCR destination, the OCR and text steps of recipes, the
text of the editor (blur strength, Redact Text).

**Windows OCR** needs a language installed in Windows (*Settings*, *Time & language*, *Language & region*, add a language with its
text recognition). Greenshot's *Language of the text* chooses which one it reads, *Automatic* follows the Windows language settings.
Without a recognition language no lines are found.

**RapidOCR** asks before it downloads, shows the download in a window and falls back to Windows OCR until it is ready, see
[the download](#the-download).

## Settings

| Where | Setting | What it does |
|---|---|---|
| Capture tab, *Text recognition (OCR)* | *Recognized by* (`[Win10] OcrEngine`) | Windows OCR (empty, the default) or RapidOCR. |
| Capture tab, *Text recognition (OCR)* | *Language of the text* (`[Win10] OcrLanguage`) | The Windows OCR language; not shown for RapidOCR. |
| RapidOCR plugin settings | *Keep the models loaded between recognitions* (`[RapidOcr] KeepModelsLoaded`) | Faster OCR for people who read a lot of text, about 80 MB more memory. |
| Output tab, *Details in saved images* | *Recognized text, when OCR already ran (private)* | Writes the text into saved images, so they can be found by it. |
| greenshot.ini, `[Core]` | `ReadApplicationText` (default `True`) | `False` uses only OCR, e.g. to find out where a wrong text comes from. |

## Privacy

- OCR runs on your computer, with both engines. Nothing of the capture is sent anywhere to read it. RapidOCR only downloads its
  files once.
- Neither the text nor link targets are logged, only counts and times.
- The recognized text stays with the capture, also the text under an obfuscation: see
  [what blur and pixelate don't protect](hiding-text.md#what-blur-and-pixelate-dont-protect).
- Text in saved images is a private detail: Greenshot asks before an image with it leaves your computer
  ([image-formats.md](image-formats.md#details-in-saved-images)).
- Password fields and text out of view are never read from the applications.

## When the text isn't right

- **No lines at all with Windows OCR:** no OCR language is installed in Windows, or the one chosen isn't. Add it in the Windows
  settings, or choose RapidOCR.
- **Wrong letters in links or e-mail addresses:** try RapidOCR, it reads underlined and small text much better.
- **A wrong text from a program:** set `ReadApplicationText=False` to see what OCR reads instead; the log says per capture how
  many lines came from the applications and how many from OCR.
- **RapidOCR files missing or changed:** Greenshot uses Windows OCR for that capture and asks once whether to download them again
  or to use Windows OCR from now on.

## Details: text from the applications

When Greenshot captures the screen, it also reads the text the applications show in the capture through UI Automation
(Dapplo.Windows.Automation `UiAutomationText.ReadVisibleTextAsync`). That text is exact, and a link brings its target (the web
address of a link in a browser, `mailto:` and `tel:` links). OCR only adds text where the applications gave none: text in images,
remote desktops (Citrix, Remote Desktop, Horizon, Hyper-V, VNC only show pixels here), applications without UI Automation text.

- Read in the background right after the capture of the screen (`Greenshot.Base/Capturing/ApplicationText.cs`). The OCR waits at most 2 seconds
  for it; a window which takes longer adds its text later, in place of the OCR lines under it. After 15 s a window stops reading and
  gives what it has read so far (the OCR keeps the rest). The log has one line per
  window with the program, the lines and the time.
- The lines are kept with the capture like OCR lines (`IOcrLineFeature.Source` is `UiAutomation`, `OcrWordInfo.LinkTarget` the link),
  so the text selection (T), the OCR step and destination, obfuscation and plugins all use them.
- Only what is visible in the captured pixels: a word counts when its middle isn't covered by another window, password fields and
  text out of view aren't read. Neither the text nor link targets are logged, only counts and times.
- Not for AI tool captures: windows excluded from AI tools are only blacked out in the pixels.
- Links keep their target (`OcrWordInfo.LinkTarget`): a web address, an e-mail address (`mailto:`) or a phone number (`tel:`)
  under the visible text, e.g. a link "Download" knows the address it opens.
- In the text selection (T) lines of application text are blue, OCR lines red. The log says per capture how many lines came from
  the applications (`Read N lines of application text from M windows`) and how many the OCR added.
- Expert setting `ReadApplicationText` (Core, default true): false uses only OCR, e.g. to find out where a wrong text comes from.

## Details: RapidOCR

The plugin `Greenshot.Plugin.RapidOcr` adds a second OCR engine next to Windows OCR: the PaddleOCR models (PP-OCRv6 small) on
ONNX Runtime, run the way [RapidOCR](https://github.com/RapidAI/RapidOCR) runs them. It reads small and underlined screen text
(links, e-mail addresses) much better than Windows OCR, and many languages with one model.

It is chosen in the settings, see [choosing the OCR engine](#choosing-the-ocr-engine).

### The download

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

### How it reads

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

ONNX Runtime's memory arena gives its memory back at the end of each run (the run option `memory.enable_memory_arena_shrinkage`),
and memory patterns are off (they are planned per input shape, and every capture has another one). Before, the arena kept the
largest working memory of all runs until Greenshot exited: one 2560x1440 screen about 0.9 GB in all, three screens 1.7 GB. The
models themselves take about 80 MB while they are loaded. By default they are loaded for each OCR and unloaded after it (when no
other OCR is running), which adds about 0.1-0.2 s per OCR. The plug-in setting *Keep the models loaded between recognitions*
(`[RapidOcr] KeepModelsLoaded`) keeps them in memory, for people who recognize a lot of text. The files are checked against their
SHA-256 only before the first load in a Greenshot session.

### What both engines give

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

### Code

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

### Licenses

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
