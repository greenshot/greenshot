# Greenshot for Linux (Wayland)

A Linux version of Greenshot, built for Wayland desktops. It is a separate application next to the Windows code
in `src/`: the Windows version uses WPF, WinForms, GDI+ and Win32, none of which exist on Linux. The UI is
[Avalonia](https://avaloniaui.net/), images are handled with SkiaSharp, and the screen is captured through the
compositor or the xdg-desktop-portal.

The goal is every feature of Greenshot 1.4 that Wayland allows, with the closest equivalent for the ones it doesn't.
This is **milestone 1: capturing**. The editor, the plugins and the settings window follow, see [Roadmap](#roadmap).

## What works now

| Feature | Status |
|---|---|
| Capture region, with frozen screen, crosshair, zoomer, size label, keys (F1) and info (I) panels | ✅ |
| Capture full screen (all monitors) | ✅ |
| Capture last region | ✅ |
| Capture window: hover or Tab to pick, or click a window in region mode | ✅ Sway, Hyprland |
| Capture active window | ✅ Sway, Hyprland |
| Mouse pointer in the capture (`CaptureMousepointer`) | ✅ with grim |
| Destinations: Save, Save as… (desktop file dialog), Copy to clipboard, Picker | ✅ |
| Filename patterns (`${capturetime}`, `${title}`, `${NUM}`, `${YYYY}`, padding, substrings, …) | ✅ same as Windows |
| Notifications with thumbnail; click opens the file | ✅ |
| Tray icon and menu | ✅ (StatusNotifierItem) |
| Command line, single instance: `greenshot capture region` etc. | ✅ |
| Global hotkeys via the GlobalShortcuts portal | ✅ KDE, GNOME 48+, Hyprland |
| `greenshot.ini` in the Windows format, unknown keys are kept | ✅ |

## Requirements

- .NET 10 SDK (to build)
- One way to capture the screen:
  - `grim` on wlroots compositors (Sway, Hyprland, river, Wayfire, …), the fastest and includes the mouse pointer, or
  - `xdg-desktop-portal` with a backend for the desktop (GNOME, KDE, wlr), used automatically when grim can't capture
- `wl-clipboard` (`wl-copy`) for the clipboard
- A notification daemon (mako, dunst, GNOME, KDE) for notifications
- A tray that supports StatusNotifierItem (KDE, Waybar, the AppIndicator extension on GNOME)
- XWayland (Avalonia renders through it)

Fedora: `sudo dnf install dotnet-sdk-10.0 grim wl-clipboard xdg-desktop-portal`

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

`~/.config/greenshot/greenshot.ini`, the same format and key names as on Windows (tray menu: *Edit the settings file*).
The settings used so far: `OutputDestinations` (`Picker`, `FileDefault`, `FileWithDialog`, `Clipboard`), `OutputFilePath`,
`OutputFileFilenamePattern`, `OutputFileFormat` (`png`, `jpg`, `webp`), `OutputFileJpegQuality`, `OutputFileAllowOverwrite`,
`OutputFileCopyPathToClipboard`, `CaptureMousepointer`, `CaptureDelay`, `PlayCameraSound`, `ShowTrayNotification`,
the four hotkeys, and the Linux only `CaptureBackend` (`Auto`, `Grim`, `Portal`).

The log is `~/.local/state/greenshot/greenshot.log`; `GREENSHOT_DEBUG=1` adds details.

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
  src/Greenshot.Linux          Avalonia: tray, capture screen, picker, the capture flow (GreenshotController)
  tests/Greenshot.Linux.Tests  xunit
  packaging/                   desktop file, install script
```

- A capture starts with a `ScreenSnapshot`: one picture of the bounding box of all outputs and the layout,
  so layout coordinates (logical) and image pixels convert both ways, also with mixed scales.
- The capture screen works in image pixels (`SelectionModel`), so selections are pixel exact on HiDPI screens.
- D-Bus is spoken with Tmds.DBus.Protocol. Its `MessageWriter` is a `ref struct`: always pass it by `ref`,
  a copy writes bytes the message doesn't account for and the bus disconnects Greenshot.

## Roadmap

1. **Capturing** (this milestone)
2. **Editor**: the Greenshot editor on Avalonia + Skia: shapes, arrows, text, speech bubbles, counters, highlight,
   obfuscate (blur, pixelate), crop, resize, effects (border, shadow, torn edge, grayscale, invert), undo/redo,
   `.greenshot` files; the Editor destination; open images and the clipboard in the editor
3. **Settings window, welcome window, languages** (the existing `Languages/*.xml`)
4. **Destinations**: printer (Print portal), email (Email portal / xdg-email), OCR and text capture (Tesseract
   instead of Windows.Media.Ocr), QR codes (ZXing.Net), PDF
5. **Plugins**: Jira / Confluence, Box, Dropbox, custom uploader (.sxcu), external command, recipes, MCP server.
   Most of their logic is portable once `Greenshot.Base` is split into a UI free part
6. **Window capture on GNOME and KDE** through the ScreenCast portal (PipeWire), fixed size and text capture modes
7. **Packaging**: Flatpak (portals make it a good fit), RPM, AUR

Not possible on Linux: the Office plugin (COM automation of Microsoft Office). LibreOffice could take its place.
