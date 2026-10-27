# Greenshot for Linux (Wayland)

A Linux version of Greenshot, built for Wayland desktops. It is a separate application next to the Windows code
in `src/`: the Windows version uses WPF, WinForms, GDI+ and Win32, none of which exist on Linux. The UI is
[Avalonia](https://avaloniaui.net/), images are handled with SkiaSharp, and the screen is captured through the
compositor or the xdg-desktop-portal.

The goal is every feature of Greenshot 1.4 that Wayland allows, with the closest equivalent for the ones it doesn't.
Done so far: **milestone 1, capturing**, **milestone 2, the image editor**, **milestone 3, settings and languages** and
**milestone 4, more destinations**, **milestone 5, plugins** and the engine of **milestone 6, recipes**. The recipe editor,
the full command line and the MCP server follow, see [Roadmap](#roadmap).

## What works now

| Feature | Status |
|---|---|
| Capture region, with frozen screen, crosshair, zoomer, size label, keys (F1) and info (I) panels | ✅ |
| Capture full screen (all monitors) | ✅ |
| Capture last region | ✅ |
| Capture window: hover or Tab to pick, or click a window in region mode | ✅ Sway, Hyprland |
| Capture active window | ✅ Sway, Hyprland |
| Mouse pointer in the capture (`CaptureMousepointer`) | ✅ with grim |
| Destinations: Open in image editor, Save, Save as… (desktop file dialog), Copy to clipboard, Picker | ✅ |
| Destinations: Printer (print dialog of the desktop, the print options of Windows), E-Mail (mail program with the capture attached) | ✅ Print / Email portal, else `lp` / `xdg-email` |
| Destinations: copy the text (OCR), copy the text of QR codes and barcodes (the link opens from the notification) | ✅ OCR needs tesseract |
| Capture text: `T` on the capture screen, the text of the selection goes to the clipboard | ✅ needs tesseract |
| Filename patterns (`${capturetime}`, `${title}`, `${NUM}`, `${YYYY}`, padding, substrings, …) | ✅ same as Windows |
| Notifications with thumbnail; click opens the file | ✅ |
| Tray icon and menu | ✅ (StatusNotifierItem) |
| Command line, single instance: `greenshot capture region` etc. | ✅ |
| Global hotkeys via the GlobalShortcuts portal | ✅ KDE, GNOME 48+, Hyprland |
| `greenshot.ini` in the Windows format, unknown keys are kept | ✅ |
| Settings window: language, theme, autostart, notifications, hotkeys, capture, output, destinations, editor | ✅ |
| Welcome window at the first start, with a check of what the desktop offers (capturing, clipboard, tray, notifications, hotkeys) | ✅ |
| The 40 languages of the Windows version (shared files, see [Languages](#languages)) | ✅ |

### Plugins

The plugins of the Windows version are built in; Settings → Plugins switches them on or off and has their settings.
The keys are the ones of Windows: `ExcludePluginNames` and `ExcludeDestinations` for the user, `BlockedPluginNames`,
`AllowedPluginNames`, `BlockedDestinations` and `AllowedDestinations` for an administrator in `greenshot-fixed.ini`
(`/etc/greenshot/` or next to the program), see docs/vNext/plugins-and-destinations.md.

| Plugin | Status |
|---|---|
| External command: open the capture in GIMP, Krita, Pinta, KolourPaint (added when installed) or any program | ✅ `[ExternalCommand]` like on Windows |
| PDF: save as PDF with page size, margins and scaling | ✅ |
| Custom uploader: ShareX `.sxcu` files, with the checks of Windows, your values, the reminder and the list of uploads | ✅ uploaders and uploads in the keyring |
| Box, Dropbox | ✅ with your own app key, see below |
| Jira: attach to an issue of your favourite filters or by key, with a comment that can show the capture; issues seen in window titles become destinations | ✅ titles on Sway, Hyprland |
| Confluence: attach to a page found by search or in the page tree, a new version or another name when the name is used | ✅ |
| MCP server | ⏳ next |

Keys, tokens and uploaders are kept in the keyring of the desktop (Secret Service: GNOME Keyring, KWallet, KeePassXC) with
`secret-tool`, or without it in `~/.config/greenshot/secrets.ini` readable only by you. The Windows version obfuscates them
in greenshot.ini instead.

Box and Dropbox need an app registered with the service: the official Windows build has its keys from the build server,
which a self built Linux version doesn't. Register an app in the developer console of the service with the redirect
address `http://127.0.0.1` and enter its key and secret in the settings (`ClientId` and `ClientSecret` in `[Box]` or
`[Dropbox]`, or `GREENSHOT_BOX_CLIENT_ID` and so on). The first upload opens the browser to sign in.

Jira and Confluence (the Atlassian plugin of Windows) work with Cloud and Data Center: Cloud with your e-mail address and
an API token, Data Center with user name and password or a personal access token. The credentials go to the keyring when
"Remember" is checked; Jira and Confluence on the same site share them. Windows watches the titles of all windows for issue
keys (a browser showing GS-123, a mail about it) and offers those issues in the picker; Wayland has no such hook, so on Sway
and Hyprland Greenshot asks the compositor for the titles every 5 seconds, elsewhere only the title of the capture counts.

### The image editor

| Feature | Status |
|---|---|
| Tools: select, rectangle, ellipse, line, arrow, freehand, text, speech bubble, step label, highlight, obfuscate, crop | ✅ |
| Highlight: text marker, area (blur and darken the rest), grayscale the rest, magnify | ✅ |
| Obfuscate: pixelate, blur | ✅ |
| Properties per tool: line, fill, thickness, shadow, arrow heads, font, size, bold, italic, alignment, …, the last used values are remembered | ✅ |
| Select, move, resize with handles, rubber band, Shift for squares and 15° lines | ✅ |
| Undo / redo, cut / copy / paste / duplicate of elements, paste an image from the clipboard | ✅ |
| Bring to front / forward / backward / back, step label counter start | ✅ |
| Image: crop, auto crop, resize, rotate, enlarge / shrink the canvas, border, drop shadow, torn edges, grayscale, invert, clear | ✅ |
| Zoom (Ctrl + wheel, Ctrl +/-/0/9) | ✅ |
| Save, save as (also as editable `.greenshot`), copy, export to every destination, ask to save when closing | ✅ |
| Open images and `.greenshot` files: tray menu, `greenshot <file>`, "Open with" in file managers, the clipboard | ✅ |
| `.greenshot` files of the Windows editor | ⚠️ the image opens, its elements can't be read yet |
| Redact text (OCR): find text and pixelate or blur it; insert a QR code | ✅ |
| Tabs, emoji, cut out, settings of drop shadow and torn edges | ⏳ later |

The keys are the ones of the Windows editor: R rectangle, E ellipse, L line, A arrow, F freehand, T text, S speech bubble,
I step label, H highlight, O obfuscate, C crop (Enter applies, Esc cancels), Z resize, Esc select tool;
Ctrl+Z/Y undo/redo, Ctrl+C/X/V/D copy/cut/paste/duplicate, Delete, Ctrl+A, arrows move (Shift or Ctrl: 10 pixels),
PageUp/PageDown/Home/End arrange, Ctrl+S save, Ctrl+Shift+S save as, Ctrl+Shift+C copy the image,
Ctrl+Q drop shadow, Ctrl+B border, Ctrl+T torn edges, Ctrl+G grayscale, Ctrl+I invert, Ctrl+, / Ctrl+. rotate,
Ctrl+Shift++ / Ctrl+Shift+- enlarge / shrink the canvas.

Linux `.greenshot` files have the layout of the Windows ones (a PNG, then the elements and a marker), but store the
elements as JSON with the marker `GreenshotJ1.00`. The Windows editor can't read their elements; any image viewer
shows them, they start with a PNG.

## Requirements

- .NET 10 SDK (to build)
- One way to capture the screen:
  - `grim` on wlroots compositors (Sway, Hyprland, river, Wayfire, …), the fastest and includes the mouse pointer, or
  - `xdg-desktop-portal` with a backend for the desktop (GNOME, KDE, wlr), used automatically when grim can't capture
- `wl-clipboard` (`wl-copy`) for the clipboard
- `tesseract` and its language packs for OCR, capturing text and redacting text (optional),
  Fedora: `sudo dnf install tesseract tesseract-langpack-eng tesseract-langpack-deu`
- A notification daemon (mako, dunst, GNOME, KDE) for notifications
- A tray that supports StatusNotifierItem (KDE, Waybar, the AppIndicator extension on GNOME)
- XWayland (Avalonia renders through it)

Fedora: `sudo dnf install dotnet-sdk-10.0 grim wl-clipboard xdg-desktop-portal`

### Recipes

The recipes of Windows (docs/vNext/capture-recipes.md) run on Linux: the same `.gsrecipe.json` files, steps, options, automatic
steps (border, drop shadow, caption, …), triggers and approval. The built-in captures are recipes too, so automatic steps
switched on in Settings → Recipes apply to them.

- Import a recipe with the tray menu *Recipes → Import Recipe…*, by opening a `.gsrecipe.json` file with Greenshot, or with
  `RecipeFiles` in `[Recipe]`. Before it runs the first time it shows what it does; approved recipes are kept in
  `~/.local/state/greenshot/recipe_trust.dat`, encrypted with a key in the keyring.
- Run one with `greenshot recipe <id> [key=value …]`, its hotkey, its tray menu entry or the editor's Recipes menu.
- The tray's *Quick preferences* has the switches recipes mark with `quickSettings` under *Automatic steps*.
- Triggers: hotkey (portal or a compositor binding to `greenshot recipe <id>`, listed in Settings → General), context
  menu, editor menu, clipboard change (`wl-paste --watch`), command line.
- Steps the Linux version does differently: `RecordVideo` uses `wf-recorder`; window sources need Sway or Hyprland, like
  window capture.

Still to come: the recipe manager and the visual recipe editor, `--list-recipes`, `--info`, recipe output on stdout, and
the MCP server.

## Build, run, test

```
cd linux
dotnet build
dotnet run --project src/Greenshot.Linux                # starts in the tray
dotnet run --project src/Greenshot.Linux -- capture region
dotnet test
```

`packaging/install.sh` publishes a release build to `~/.local/lib/greenshot`, links `~/.local/bin/greenshot`
and adds the menu entry and icons. `install.sh --autostart` also starts Greenshot with the session.

## Hotkeys

Wayland doesn't let an application grab keys. Greenshot uses the GlobalShortcuts portal where the desktop offers it
(KDE Plasma, GNOME 48+, Hyprland); the desktop asks you to confirm the keys at the first start.

Elsewhere bind the commands in the compositor. They talk to the running Greenshot, which starts if it doesn't run yet.

Sway (`~/.config/sway/config`):

```
bindsym Print exec greenshot capture region
bindsym Alt+Print exec greenshot capture activewindow
bindsym Ctrl+Print exec greenshot capture fullscreen
bindsym Shift+Print exec greenshot capture last
exec greenshot
```

Hyprland (`~/.config/hypr/hyprland.conf`):

```
bind = , Print, exec, greenshot capture region
bind = ALT, Print, exec, greenshot capture activewindow
bind = CTRL, Print, exec, greenshot capture fullscreen
bind = SHIFT, Print, exec, greenshot capture last
exec-once = greenshot
```

## Settings

The tray menu has *Preferences…*, `greenshot settings` opens them too. They are stored in
`~/.config/greenshot/greenshot.ini`, the same format and key names as on Windows (the settings window has a button to open it).
The settings used so far: `OutputDestinations` (`Picker`, `FileDefault`, `FileWithDialog`, `Clipboard`), `OutputFilePath`,
`OutputFileFilenamePattern`, `OutputFileFormat` (`png`, `jpg`, `webp`), `OutputFileJpegQuality`, `OutputFileAllowOverwrite`,
`OutputFileCopyPathToClipboard`, `CaptureMousepointer`, `CaptureDelay`, `PlayCameraSound`, `ShowTrayNotification`,
the four hotkeys, and the Linux only `CaptureBackend` (`Auto`, `Grim`, `Portal`).

The log is `~/.local/state/greenshot/greenshot.log`; `GREENSHOT_DEBUG=1` adds details.

## Languages

The language packs are the ones of the Windows version, `src/Greenshot/Languages/greenshot.<language>.ini`, linked into
the build, so a translation helps both. Texts only Linux has are in
`linux/src/Greenshot.Linux/Languages/greenshot-linux.<language>.ini` (section `[Linux]`); a missing text falls back to
English. Without a `Language` in the settings, the language of the system (`LANGUAGE`, `LC_MESSAGES`, `LANG`) is used,
the closest one available (de-AT finds de-DE). Own files can go to `~/.config/greenshot/Languages`.

Texts which mention Windows (e.g. the autostart) have a Linux text instead. XAML uses `{l:Text Editor.save}`,
like `wpf:Text` on Windows.

## What Wayland changes

| Windows | Linux | Why |
|---|---|---|
| One capture window over all monitors | One per monitor, sharing the selection | Wayland places each window on one output |
| The crosshair is at the mouse at once | It appears when the mouse or an arrow key moves | Applications can't ask where the pointer is |
| Window capture everywhere | Sway and Hyprland; GNOME and KDE later through the portal window picker | Applications can't list other windows |
| Hotkeys always | GlobalShortcuts portal or compositor bindings | Applications can't grab keys |
| Capture a window behind others | Only what is visible | The compositor only gives the composed screen |

## Code

```
linux/
  src/Greenshot.Linux.Core     no UI: settings, filename patterns, screen grabbers, compositor queries,
                               D-Bus portals and notifications, destinations, single instance socket
  src/Greenshot.Linux.Editor   no UI: the editor model, drawing elements, Skia rendering, undo, effects, .greenshot files
  src/Greenshot.Linux.Recipes  no UI: the recipe engine of Windows (model, validation, DAG engine, steps, triggers, approval)
  src/Greenshot.Linux.Plugins  no UI: the plugins (external command, PDF, custom uploader, Box, Dropbox, Jira, Confluence)
  src/Greenshot.Linux          Avalonia: tray, capture screen, picker, editor window, the capture flow (GreenshotController)
  tests/Greenshot.Linux.Tests  xunit
  packaging/                   desktop file, install script
```

- A capture starts with a `ScreenSnapshot`: one picture of the bounding box of all outputs and the layout,
  so layout coordinates (logical) and image pixels convert both ways, also with mixed scales.
- The capture screen works in image pixels (`SelectionModel`), so selections are pixel exact on HiDPI screens.
- The editor is UI free (`EditorController`, `EditorSurface`), the window only passes the mouse and keys on. The same Skia
  rendering draws the editor view (through Avalonia's Skia lease) and the exported image. Filters read what was drawn
  below them, like on Windows. Undo keeps the elements as JSON and the image by reference: effects never change an image
  in place, they return a new one.
- D-Bus is spoken with Tmds.DBus.Protocol. Its `MessageWriter` is a `ref struct`: always pass it by `ref`,
  a copy writes bytes the message doesn't account for and the bus disconnects Greenshot.

## Roadmap

1. **Capturing** ✅
2. **Editor** ✅ (see above for what is left: tabs, emoji, cut out, effect settings, reading Windows `.greenshot` elements
   with `System.Formats.Nrbf`)
3. **Settings window, welcome window, languages** ✅ (translations of the Linux only texts are still to do)
4. **Destinations** ✅ printer, email, OCR and text capture (tesseract), QR codes (ZXing.Net). Still to do: the QR frames
   on the capture screen and the QR generator with its WiFi / vCard / calendar types
5. **Plugins**: external command, PDF, custom uploader, Box, Dropbox, Jira, Confluence ✅
6. **Recipes**: the engine, steps, triggers, approval, options and quick preferences ✅; the recipe manager and the visual
   editor, the full command line and the MCP server next
7. **Window capture on GNOME and KDE** through the ScreenCast portal (PipeWire), fixed size and text capture modes
8. **Packaging**: Flatpak (portals make it a good fit), RPM, AUR

Not possible on Linux: the Office plugin (COM automation of Microsoft Office). LibreOffice could take its place.
