# The stand-alone editor (Greenshot.Editor.Standalone)

Experimental, Debug builds only. `greenshot-editor.exe` is an editor which is a program of its own: .NET 10, Avalonia for
the window and the newest ImageSharp (4.x, with ImageSharp.Drawing 3.x) for the drawing. It edits a
[`GreenshotDocument`](document-model.md), like the [WPF editor](wpf-editor.md) whose tools, view models, icons and texts
it took over. It has no System.Drawing, WinForms, WPF or Dapplo.Windows in it, so the same code builds and runs on
Windows, Linux and macOS (the tests run on Linux).

Why a program of its own: Greenshot.exe runs on .NET Framework 4.8, where neither Avalonia 12 nor ImageSharp 4 run.
A second process also keeps the memory of open documents out of the tray program, and an editor which crashes doesn't
take Greenshot with it.

## What it does

- Opens `.greenshot` documents and images (PNG, JPEG, GIF, BMP, WebP, TIFF and what ImageSharp reads), saves documents
  and images, copies the image to the clipboard.
- Tabs, one document each. Undo and redo per tab.
- The tools of the WPF editor: select, rectangle, ellipse, line, arrow, freehand, text, speech bubble, step label,
  emoji, highlight, obfuscate, cut mark and crop, with the same keys (R, E, L, A, F, T, S, I, J, H, O, U, C).
- A selected element shows its handles, no line around it. Esc while typing ends the typing and keeps the text.
- Crop (Enter applies it) also removes the elements which are completely outside of what is kept.
- A panel with the properties of the selection or of the tool, the canvas (background, border, shadow, torn edges,
  resize) and the layers.
- The menu "Effects" in the top bar: border, drop shadow, torn edges (with the shadow the torn edges of Greenshot
  have), grayscale, invert, turn and auto crop. Grayscale and invert change the capture, not what is drawn on it.
- Stamps: the images of the stamps folder (the one of the [stamps plugin](stamps.md), `stamps.json` gives names and
  tags) are placed with a click, files are imported, and the selection can be kept as a new stamp. This is part of
  the editor, it has no plugins.
- "Send to": the destinations of the running Greenshot (see below).
- Light and dark, following the system (setting `Theme`).

Not there: printing (Avalonia has none, use Greenshot's printer destination through "Send to"), plugins, and
languages other than English and German.

## Where its parts are

| Folder | What |
|---|---|
| `Rendering/` | `DocumentRenderer` draws a document, or a part of it, with ImageSharp. `DocumentDiff` tells which area two documents differ in, so only that is drawn again |
| `Controls/` | `DocumentCanvas` shows the document: `TabSurface` keeps tiles (512 pixels) of the drawn document current on a pool thread, the canvas draws the tiles, the handles of the selection and the text box for typing |
| `Tools/`, `Editing/` | The tools and what they change, taken from the WPF editor |
| `ViewModels/`, `Views/` | The window. The view models don't know Avalonia's controls, the window is behind `IEditorView` |
| `Ipc/` | The pipe to Greenshot (`GreenshotLink`), and one editor per user (`SingleInstance`) |
| `Stamps/`, `Files/`, `Settings/`, `Diagnostics/` | The stamps folder, reading and writing files, `greenshot-editor.ini`, the log |

Settings are in `greenshot-editor.ini` (section `[Editor]`) and the log is `greenshot-editor.log`, where Greenshot
keeps its own: next to the program for a portable Greenshot, else in the folders of the user. The texts are in
`Languages/greenshot-editor.<language>.ini`. The editor uses the language of Greenshot's settings (`greenshot.ini`),
unless its own setting `Language` names one.

## How it talks to Greenshot

The editor is in the folder `Editor` below Greenshot.exe (they can't share a folder: both bring DLLs with the same
names for different .NET versions).

**Greenshot to editor.** The destination "Editor (stand-alone)" turns the capture into a document, writes it as a
`.greenshot` file into the handover folder and starts `Editor\greenshot-editor.exe --handover <file>`. There is one
editor per user: a second one gives its arguments to the first over the pipe `GreenshotEditor_<user>` (only for the
same user) and exits. The editor opens the file as a new, unsaved document and removes the file.

**Editor to Greenshot.** The editor is a client of [Greenshot's pipe](greenshot-proxy-and-integration.md) with the
source `editor`. The messages are those of the pipe: 4 bytes length, then JSON.

| Command | Request | Reply |
|---|---|---|
| `EDITOR_DESTINATIONS` | | `destinations`: list of `designation` and `name`, and `language` |
| `EDITOR_EXPORT` | `files`: one file of the handover folder, `parameters.destination`, `parameters.title` | `status` `ok`, or `error` with `stderr` |
| `WATCH` | | Stays open: when it ends, Greenshot is gone |

The document doesn't go through the pipe (a message is at most 64 MB, and a capture has no upper size): the editor
writes it as a file into the handover folder and the request names it. Greenshot turns the document into a surface
and gives it to the destination, like an export from the old editor.

The handover folder is `GreenshotEditor` in the temporary folder of the user. Files in it are short-lived: who reads
a file removes it. Once the capture history exists, the history takes this place.

Who may do what:

- Greenshot lets the source `editor` in only when the program on the other end is `Editor\greenshot-editor.exe` of
  its own folder (`IpcClientVerifier`), and the source may only use `EDITOR_DESTINATIONS`, `EDITOR_EXPORT` and
  `VERSION`. It can't capture, run recipes or change settings.
- Greenshot reads and removes only `.greenshot` files directly in the handover folder, whatever the request names.
  The editor does the same for `--handover`: any other file is opened like a file the user chose, and never removed.
- The editor only talks to a pipe whose server is the Greenshot.exe one folder up.

An editor which is not in the folder `Editor` of a Greenshot runs alone: it opens, edits and saves, and has no "Send to".

## How fast it starts

The log of the editor (`greenshot-editor.log`) has a line for every step of the start, beginning with "Start:", with
the time since the process was started. Greenshot logs how long converting the capture, writing the file and starting
the process took ("Stand-alone editor:").

- Whether another editor runs is asked from a named mutex, which answers at once. Only when there is one, the pipe is used.
- While the window is built, a thread of the pool lists the fonts and reads and draws a small picture (`WarmUp`), and
  another loads the settings and texts, so that code is loaded and compiled when the capture arrives.
- About half of what is left is compiling (Avalonia, ImageSharp and the editor). `dotnet publish -r win-x64` compiles
  them ahead (`PublishReadyToRun`); a plain build, which is what the solution does today, doesn't. Native AOT (below)
  leaves no compiling at all.
- The capture in the handed over file is stored, not compressed a second time.

## Native AOT

The editor can be published as native code, without the .NET runtime and without compiling while it runs:

```
dotnet publish src/Greenshot.Editor.Standalone -c Debug -r win-x64 -p:EditorAot=true -o src/Greenshot/bin/Debug/net480/Editor
```

This needs the C++ build tools of Visual Studio ("Desktop development with C++") for the linker. The result is one
`greenshot-editor.exe` with the Skia and HarfBuzz libraries, the language packs and the emoji font next to it. On Linux
it shows the first capture 0.2 seconds after its start (Release; a Debug publish 0.4), where the normal build takes
2.1 and a ReadyToRun build 1.3. The Release executable is 45 MB, 60 MB with the two native libraries.

What makes it work, and what a change has to keep:

- No reflection over types while running. The JSON of a document is written and read by generated code
  (`DocumentJsonContext` in Greenshot.Document, used by every program which reads documents). A new type of the document
  is listed there, a new enum also in its `EnumConverters`. A value with a default which a file may leave out gets its
  default in a constructor marked `[JsonConstructor]`, not only at the property: generated code sets every `init`
  property, a missing one to 0 or null. The editor and its tests switch the reflection of System.Text.Json off
  (`JsonSerializerIsReflectionEnabledByDefault`), so a forgotten type fails in the tests and not only in the AOT build.
- The texts in the XAML (`{l:Text Key}`) ask the language pack by key, bindings in the XAML are compiled ones.
- Publishing with `-p:EditorAot=true` switches the AOT and trim analyzers on. What is left are warnings in the code
  Dapplo.Ini generates for the settings (it says enums need code made while running; the editor's settings are text,
  numbers and a switch, and they are read in the AOT build) and one inside System.Formats.Nrbf (old `.greenshot`
  files; they open in the AOT build).

Native AOT and plugins as DLLs exclude each other: a program compiled this way can't load .NET code which wasn't
compiled into it. Content (stamps, language packs, plugin elements with their picture in a document) is data and works.

Checked by hand on Linux only: start, texts, settings read, a document with every kind of element, an old document.
Not checked in the AOT build: saving the settings, the clipboard, file dialogs, stamps, the pipe to Greenshot, Windows.

## Building

The projects `Greenshot.Editor.Standalone` and `Greenshot.Editor.Standalone.Tests` are in `Greenshot.sln` and build
in the Debug configurations. After a build the editor copies itself into `Editor` next to a Greenshot.exe built with
the same configuration, so build Greenshot first and the editor after it.

ImageSharp 4 and ImageSharp.Drawing 3 need a Six Labors license. A Debug build works without one (with a warning),
a Release build fails without. The key comes from the environment variable `SIXLABORS_LIC` (the repository secret of
that name in a workflow), or from a file `sixlabors.lic` next to the project. That file is in `.gitignore`: the key is
a secret and is never committed. Only the editor uses these versions, Greenshot itself stays on ImageSharp 2.

Avalonia brings a package (`Avalonia.BuildServices`) which sends statistics about every build to Avalonia. Both
projects exclude its assets, so building Greenshot doesn't contact anyone.

The tests need no screen: the window runs on Avalonia.Headless, drawn by Skia, and the tests draw, move and type
with its mouse and keyboard. With the environment variable `GREENSHOT_TEST_OUTPUT` set to a folder they save what the
window showed as PNG.

## Open

- Not yet run on Windows: the pipe to a real Greenshot, the clipboard, file dialogs, high DPI and more than one screen.
- The other languages, and the "Editor (stand-alone)" name as a language key.
- Release builds, the installer and code signing of `greenshot-editor.exe`.
