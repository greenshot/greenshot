# Stamps

Stamps are small images which make a capture clearer or more personal: a check mark, an arrow, a logo, a signature, a sticker.
You collect them in a folder, group them in categories, give them names and tags, find them with a search and insert them into the editor, where they are
elements like any other: move, resize, delete. They also go to other programs, with the clipboard or by dragging.
The plugin is `Greenshot.Plugin.Stamps`, a **beta** feature of the full Greenshot (not in Greenshot Light).

- [For users](#for-users)
- [For developers](#for-developers)

---

## For users

### Turn it on

The stamps are only there for beta testers: in the settings, *Expert*, check *Enable to enable beta-test features*
(`BetaTester=True` in `[Core]` of greenshot.ini) and restart Greenshot. The installer has them as *Stamps plug-in (beta)*,
in the full and the custom installation.

### Open the stamps

- In the editor: *Plugins*, *Stamps...*, or **Ctrl+Shift+K**. The window opens at the right edge of the editor (over it when the
  screen has no room) and stays above it. The stamps go into the capture which is shown in the editor window you used last.
- From the tray menu: *Stamps...*. Without an editor the window is a window of its own, to give stamps to other programs.
- With a hotkey from everywhere: set `Hotkey` in `[Stamps]` of greenshot.ini, e.g. `Hotkey=Ctrl + Shift + Alt + S`. Pressed in
  an editor it opens the window for that editor.

### Use a stamp

| What | How |
|---|---|
| Find | Type in *Search*: every word must be part of the name, a tag, the category or the file name. Down or Enter goes to the stamps. The groups of the categories close and open with a click on their header, or with the keyboard: Up from the first row of a group goes to its header, there Left closes it, Right opens it (Space and Enter switch it) and Down goes on. A search shows all it finds, also in closed groups. |
| Insert into the editor | Double-click, Enter, or *Insert*. The stamp lands in the middle of the visible part of the capture, selected; a big one is made smaller to fit. The editor gets the keyboard, so the arrow keys move it. Ctrl+Z removes it. |
| Give it to another program | *Copy* or Ctrl+C: the clipboard gets the file, PNG and DIBV5 (with transparency), and for an SVG also the SVG itself. Or drag the stamp to the program: it gets the file. |
| Filter by tags | The **+** next to *Filter* adds a tag of the list: only the stamps with all the tags of the filter are shown. The **x** of a tag removes it, *Clear all* removes all. |
| Name and tags | Select the stamp and type into *Name*; Enter or leaving the field saves it. F2 goes to *Name*. Its tags are shown under the stamps: the **+** adds one from the list of tags, the **x** removes one. A tag which isn't in the list yet is added with *New tag* in the same list, it goes into the list and to the stamp. |
| Add | *Add...* (several files at once), drop image files or folders onto the window, drop an image from a web page, or *Paste* (Ctrl+V) an image from the clipboard, which is saved as PNG. The files are copied into the stamps folder. |
| Categories | Every stamp is in a category: a sub folder of the stamps folder. The stamps without one are in *General*, the stamps folder itself. Choose another category in *Category* or type a new name (`/` for a category in a category); the file moves into that folder. Added stamps go into the category of the selected stamp, dropped ones into the category of the group they are dropped on. A dropped folder becomes a category with its name, its sub folders categories in it. |
| Delete | *Delete* or the Delete key, after a question. The file is deleted from the stamps folder. |
| Show the folder | The buttons for the stamps folder (*Add...*, *Paste*, *Open folder*) are below a line, apart from those of the selected stamp. *Open folder* shows the stamps folder in Explorer, with the file of the selected stamp selected. F5 reads the folder again. |

The list shows the format of each stamp (SVG, PNG, ...) under its name. Every image Greenshot can open is a stamp: PNG, SVG, ICO, JPG, GIF, BMP, TIFF, WebP and the others the editor opens.
An SVG stays a vector element in the editor, it stays sharp when it is resized.

An image dragged from a web page is kept in its own format: the file the browser gives, else the image downloaded from its
address (through the proxy settings of Greenshot), else the picture as PNG.

### The stamps folder

The stamps are the files in one folder, the sub folders are the categories: by default `Stamps` next to greenshot.ini
(`%APPDATA%\Greenshot\Stamps`, for the portable Greenshot next to Greenshot.exe). `LibraryPath` in `[Stamps]` of greenshot.ini
sets another one, e.g. a shared folder of a team (environment variables like `%USERPROFILE%` work).

Files copied into the folder by hand show up too, named after the file and without tags. The names and the tags are kept in
`stamps.json` in the same folder, so they travel with the files:

```json
{
  "version": 1,
  "stamps": {
    "check.svg": { "name": "Done", "tags": [ "green", "ok" ] },
    "Logos/company.png": { "name": "Company", "tags": [ "logo" ] }
  },
  "tags": [ "green", "logo", "ok", "sticker" ]
}
```

The key is the path of the file in the folder, with `/` between sub folders. `tags` is the list of tags to choose from,
also those no stamp has yet; a tag a stamp has is always in it. A change reads the file again before it writes
it, so in a shared folder the changes of the others are kept.

---

## For developers

The plugin only references `Greenshot.Base`, not the editor: everything it needs of the editor is in two generic contracts,
which every plugin can use, so the plugin carries over to a new editor.

- **`IEditorWindow`** (`Greenshot.Base/Interfaces/Forms`): an editor window without WinForms types: `Handle` (to own and place
  a window of the plugin), `CurrentSurface` (the capture shown, a window can have tabs), `Activate()`, `Activated` and `Closed`.
  `ImageEditorForm` implements it, a plugin gets it through `IEditorPlugin.InitializeEditor` (`editorForm is IEditorWindow`).
- **`ISurface.InsertElements`**: adds elements in the middle of the visible part of the canvas, made smaller when they don't fit,
  as one undo step, and selects them instead of the selection before.

The elements and the previews come from the file format handlers of Greenshot (`IFileFormatHandler.LoadDrawablesFromStream`
and `TryLoadFromStream`), so the plugin supports what Greenshot opens; a `.greenshot` file is a capture, not a stamp.

| Part | What it does |
|---|---|
| `StampsPlugin` | Registers everything only for beta testers: the editor menu entry, the tray menu entry, the hotkey; shows the window and keeps it above the editor window used last |
| `Editor/EditorTracker` | The `IEditorPlugin`: adds *Stamps...* with Ctrl+Shift+K, knows the editor window which was active last. The only class which sees WinForms |
| `Library/StampLibrary`, `Stamp` | The folder and `stamps.json`: load, search, import, add, change, move to a category, delete. No image is decoded here |
| `Library/DroppedImages` | What was dropped, read during the drop: files and folders, the virtual files of a browser, image urls, the image |
| `Library/StampImages` | Previews, elements and clipboard content, with the file format handlers |
| `ViewModels`, `Views/StampsWindow` | The WPF window |

For the drop, `Greenshot.Base` has two generic helpers: `ClipboardHelper.GetVirtualImageFiles` (the image files of a drop or the
clipboard as they are) and `ImageDownloader.DownloadImageFileAsync` (an image download kept in its format, which the content
decides, see `ImageFileContent.GuessExtension`).

Dragging a stamp gives only the file (`CF_HDROP`): the editor takes an image format before a file, so with a PNG next to it an
SVG dropped into the editor would become a bitmap. Copying adds PNG and DIBV5 for programs which don't take files.

Tests: `Greenshot.Tests/Stamps/StampLibraryTests.cs` (folder, `stamps.json`, search, categories, import, move, delete),
`Greenshot.Tests/Core/ImageFileContentTests.cs`, the drop and download tests in `ClipboardTests` and `ImageDownloaderTests`, and
`Greenshot.Tests/Editor/SurfaceInsertElementsTests.cs` (where inserted elements land, selection, undo).
