# The new editor (experimental)

`Greenshot.WpfEditor` is a try of the editor which can replace the editor of today (`Greenshot.Editor`, WinForms, `Surface`).
It edits a [`GreenshotDocument`](document-model.md) and has nothing to do with `ISurface`, `System.Drawing` or WinForms:
`BannedSymbols.WpfEditor.txt` makes the build fail when one of them is used. The old editor stays as it is.

Only Debug builds have it: Greenshot.csproj references the project only for Debug, like the rest of the document model.

## Open it

- **Destination "Editor (new)"**: a capture sent there is mapped to a document (`SurfaceToDocument`) and opens in a tab.
- **File > Open** in the new editor: a `.greenshot` package (zip with `document.json`), a `.greenshot` file of Greenshot 1.2 - 1.4
  (read by `LegacyToDocument`, without BinaryFormatter) or an image.
- A file dropped on the window: a `.greenshot` opens in a new tab, an image is added to the document (with Ctrl it opens in a new tab).

## How it is built

| Part | What it does |
|---|---|
| `Rendering/DocumentComposition` | The canvas as a stack of small bitmaps which WPF blends on the graphics card. Every element is drawn alone by `DocumentRenderer` (the code that exports) into a bitmap of its size; a moved element keeps its bitmap and only its place changes, a changed element draws only its own bitmap again. Highlights, obfuscations and frames around everything change the pixels below them: their bitmap holds everything below them with the effect and is drawn again when something below changes. Big bitmaps (the capture) are drawn in the background. |
| `Controls/DocumentCanvas` | Shows the bitmaps with zoom and pan, draws the adorners of the tool (handles, outlines, rubber band, crop) with WPF in screen pixels, and shows a text box over an element while its text is typed. The overview (`Greenshot.Base/Wpf/ZoomOverview`, shared with the image viewer) shows the whole canvas when not all of it is visible. |
| `Tools/*` | The tools: they get the mouse and keys in canvas pixels and change the document through `IToolHost`. They don't know WPF, so another UI can use them. |
| `ViewModels/DocumentTabViewModel` | One document: its undo steps (an undo step is the document before the change, documents are immutable), the selection, the tool, the text being typed. |
| `ViewModels/PropertiesViewModel` | The panel: the style of the selection, or the style new elements of the tool get. A change of a selected element also becomes the default of its tool. |
| `Files/*` | Open and save (`.greenshot`, PNG, JPEG, BMP, GIF, TIFF, WebP), the clipboard (PNG and bitmap). |
| `Editing/*` | Text of elements, auto crop, the frame of the capture (border, drop shadow and torn edges are one frame element, so they stay editable). |
| Emoji | The emoji picker of the old editor (`Greenshot.Base/Wpf/Emoji`), with the same history (`IWpfEditorHost.EmojiHistory`). Emoji are drawn with the Twemoji font of Greenshot (`Greenshot.ImageSharpDrawing/EmojiDrawing`), in both editors and in exports. |
| `Icons/*.xaml` | One multi-color vector icon per file (`Icon.<Name>`, 16x16 grid), drawn after the icons of the old editor. Replace a file to replace an icon. |

Greenshot gives the editor its destinations through `IWpfEditorHost` (`Greenshot/DocumentModel/WpfEditorHost.cs`): the document
becomes a surface (`DocumentToSurface`) and goes to the destination like an export from the old editor.

## Using it

- Tools: Select (Esc), Rectangle (R), Ellipse (E), Line (L), Arrow (A), Freehand (F), Text (T), Speech bubble (S), Step label (I),
  Emoji (J), Highlight (H), Obfuscate (O), Cut mark (U), Crop (C). Shift keeps squares and circles, snaps lines and turns to 15 degrees,
  and moves along one axis.
- Select: click, Shift/Ctrl+click to add or remove, drag on the empty canvas for a rubber band (Alt: also what it touches),
  double-click a text, speech bubble, rectangle or ellipse to type its text (Ctrl+Enter or a click outside ends, Esc cancels).
  The round handle above an element turns it. Arrow keys move by 1 pixel, with Shift by 10.
- Zoom and pan: Ctrl+wheel zooms at the mouse, the wheel scrolls (Shift+wheel sideways), the middle button or Space with the left
  button drags the view. Ctrl+0 is 100 %, Ctrl+9 fits the window. The overview at the bottom right moves the visible part with a click or a drag.
- Crop keeps everything: the canvas becomes the cropped area, what is outside isn't drawn, an undo brings it back.
  A saved `.greenshot` still has the whole capture.

## Not done yet

- Grayscale and invert of the whole capture: the document model has no element for them yet.
- Per-destination settings and the destination picker; the editor sends to the destinations which need no menu.
