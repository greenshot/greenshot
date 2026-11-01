# The viewer

The viewer shows a capture in a small window of its own, without the editor around it: to keep it on top of other windows and
compare it with what is on the screen, or just to look at it. It is a destination, like the editor or the clipboard, and is still
being tested: switch on *beta-test features* on the Expert tab of the settings and restart Greenshot to get it.

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

A viewer opens with the capture in its real size, or smaller when the capture is bigger than 800 × 800 pixels.

- `1` shows the capture 1:1: one pixel of the capture is one pixel of the screen. `0` goes back to the size the viewer opened with.
- The mouse wheel, `+` and `−` zoom. The window follows: it grows up to the size of the screen and shrinks to 50 × 50 pixels.
- When the capture is bigger than the window, drag it or use the arrow keys to see another part. An overview in the corner shows
  the whole capture and marks the visible part, click or drag in it to move there.
- Resizing the window with the mouse keeps the proportions of the capture and zooms with it.
- When all of the capture is visible, dragging it moves the window. With `Ctrl` dragging always moves the window.

## Title bar or only the capture

`T` switches between a thin title bar with a border, and only the capture. The title bar has the title of the capture and buttons
to export, show 1:1, fit, keep the viewer on top, show the keys and close it. Without the title bar, drag the capture to move the
window and use the keys or the menu (right mouse button).

`P` keeps the viewer on top of the other windows, `M` shows or hides the captured mouse pointer.

## Exporting the capture

`E`, the export button or *Export…* in the menu open the destination picker, the same as for a new capture: save to a file, copy
to the clipboard, open in the editor, upload, ... The picker gets a copy of what the viewer shows, so the viewer stays open.

## Keys

`H` or `F1` show the keys in the viewer.

| Key | What it does |
|---|---|
| `1` | Real size (1:1) |
| `0` | The size the viewer opened with |
| `+` `−`, mouse wheel | Zoom in and out |
| Arrow keys | Move the visible part |
| `Ctrl` + drag | Move the window |
| `E` | Export with the destination picker |
| `P` | Always on top, or not |
| `T` | Title bar and border, or only the capture |
| `M` | Show or hide the captured mouse pointer |
| `Shift+F10`, right mouse button | The menu |
| `H`, `F1` | Show or hide the keys |
| `Esc` | Close the keys, or the viewer |

## Settings

A change of *always on top*, the title bar or the mouse pointer in a viewer becomes the default for the next viewers. They are
in the `[Viewer]` section of greenshot.ini:

| Setting | Default | What it does |
|---|---|---|
| `MaxWidth`, `MaxHeight` | 800 | The biggest size in pixels a new viewer opens with, a bigger capture is shown smaller at first |
| `AlwaysOnTop` | False | Keep the viewers on top of the other windows |
| `ShowTitleBar` | True | Show the title bar and the border |
| `ShowCursor` | True | Show the captured mouse pointer |
