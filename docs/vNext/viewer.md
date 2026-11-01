# The viewer

The viewer shows a capture in a small window of its own, without the editor around it: to keep it on top of other windows and
compare it with what is on the screen, or just to look at it. It is a destination, like the editor or the clipboard.

- [Opening a capture in the viewer](#opening-a-capture-in-the-viewer)
- [Zoom, 1:1 and moving around](#zoom-11-and-moving-around)
- [Title bar or only the capture](#title-bar-or-only-the-capture)
- [Exporting the capture](#exporting-the-capture)
- [Keys](#keys)
- [Settings](#settings)

## Opening a capture in the viewer

Choose *Show in a viewer* as a destination in the settings, or pick it in the destination picker (letter `V`). Every capture opens
in a new viewer, there can be as many as you like. A viewer has its own copy of the capture: it isn't saved, and it is gone when
you close the viewer. The first time, Greenshot tells you so.

## Zoom, 1:1 and moving around

A viewer opens with the capture 1:1, one pixel of the capture is one pixel of the screen, in a window as big as the capture.
A capture bigger than the screen gets a window as big as the screen and can be moved around. The *Viewer* tab of the settings
can instead open a capture smaller, so the window is at most 800 × 800 pixels (or another size).

- Zooming keeps the window size: the mouse wheel, `+` and `−` zoom in and out, around the mouse or the middle.
- `1` shows the capture 1:1 again, `0` zooms so all of the capture is visible in the window.
- When not all of the capture is visible, drag it or use the arrow keys to see another part. An overview in the corner shows
  the whole capture and marks the visible part, click or drag in it to move there.
- `W` sizes the window to the zoomed capture, as far as the screen allows.
- Resizing the window with the mouse keeps the proportions of the capture, the zoom stays.
- When all of the capture is visible, dragging it moves the window. With `Ctrl` dragging always moves the window.

## Title bar or only the capture

`T` switches between a thin title bar with a border, and only the capture. The title bar has the title of the capture and buttons
to export, show 1:1, fit, keep the viewer on top, show the keys and close it. Without the title bar, drag the capture to move the
window and use the keys or the menu (right mouse button).

`P` keeps the viewer on top of the other windows, `M` shows or hides the captured mouse pointer.

## Exporting the capture

`E`, the export button or *Export…* in the menu open the destination window, the same one recipes show: save to a file, copy
to the clipboard, open in the editor, upload, ... or forward the capture to a recipe, which runs with it as if it had captured it.
The destination or recipe gets a copy of what the viewer shows, so the viewer stays open. When an export fails, the window comes
back with the error.

## Keys

`H` or `F1` show the keys in the viewer.

| Key | What it does |
|---|---|
| `1` | Real size (1:1) |
| `0` | All of the capture in the window |
| `+` `−`, mouse wheel | Zoom in and out, the window keeps its size |
| `W` | Window to the size of the zoomed capture |
| Arrow keys | Move the visible part |
| `Ctrl` + drag | Move the window |
| `E` | Export, or forward to a recipe |
| `P` | Always on top, or not |
| `T` | Title bar and border, or only the capture |
| `M` | Show or hide the captured mouse pointer |
| `Shift+F10`, right mouse button | The menu |
| `H`, `F1` | Show or hide the keys |
| `Esc` | Close the keys, or the viewer |

## Settings

The *Viewer* tab of the settings ([greenshot://settings?tab=viewer](greenshot://settings?tab=viewer)) has the size of a new viewer,
always on top, the title bar and the mouse pointer. A change of *always on top*, the title bar or the mouse pointer in a viewer
becomes the default for the next viewers too. They are in the `[Viewer]` section of greenshot.ini:

| Setting | Default | What it does |
|---|---|---|
| `OpenAtRealSize` | True | Open with the capture 1:1, the window at most as big as the screen |
| `MaxWidth`, `MaxHeight` | 800 | With `OpenAtRealSize` False: the biggest size in pixels a new viewer opens with, a bigger capture is shown smaller |
| `AlwaysOnTop` | False | Keep the viewers on top of the other windows |
| `ShowTitleBar` | True | Show the title bar and the border |
| `ShowCursor` | True | Show the captured mouse pointer |
