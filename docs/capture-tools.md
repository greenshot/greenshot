# Capture tools: adding a new way to select on the screen

The interactive capture shows a frozen capture of the whole screen in the `CaptureWindow` (WPF, `src/Greenshot/UI/Capture`).
What the user can do on it comes from **capture tools**: the region, window and text selections are each one tool class in
`src/Greenshot/UI/Capture/Tools`. This document explains how to write another one, and what is possible and what is not,
with a color picker as the example.

## How it fits together

```
CaptureWindow (ICaptureToolHost)                 ICaptureTool (derive from CaptureTool)
 - frozen capture, cursor, fix mode (shift)       - Mode, ShortcutKey
 - crosshair, zoomer, hotspots (QR codes...)      - ShowsZoomer, ShowsCrosshair
 - selection rectangle + its animation            - Activate / Deactivate
 - size rulers and labels                         - OnMouseMove / OnMouseDown / OnMouseUp
 - keys: arrows, Esc, M, Z, F, Space, shortcuts   - OnKeyDown (sees keys first)
 - Accept / Cancel  ──────────────────────────►    - Draw (its own layer)
```

The window does everything the tools share and passes the input to the **active tool**. A tool only contains what makes it different.

| Tool | Mode | Key | What it does |
|---|---|---|---|
| `RegionCaptureTool` | `Region` | Space toggles | Drag or Enter/Enter for a rectangle, includes the pixel under the cursor |
| `WindowCaptureTool` | `Window` | Space toggles | The selection animates to the (child) window under the cursor, D for debug info |
| `TextCaptureTool` | `Text` | T | Extends the region tool: shows the OCR lines, a click selects a line |

## Adding a tool, step by step

1. Create a class in `src/Greenshot/UI/Capture/Tools` that derives from `CaptureTool`. It has empty implementations of everything,
   override only what you need. `Host` (the `ICaptureToolHost`) is set when the tool is activated.
2. Give it a `Mode`, the `CaptureMode` the flow sees when your tool made the selection. It must be unique among the tools:
   the window starts with the tool whose mode the recipe asked for, and Space switches by mode. For a new kind of selection
   add a value at the end of `CaptureMode` (`src/Greenshot.Base/Interfaces/CaptureMode.cs`).
3. Optionally a `ShortcutKey`, which switches to the tool.
4. Add one line to the `_tools` list at the top of `CaptureWindow.xaml.cs`. Wrap it in `#if !GREENSHOT_LIGHT` if the tool should not be in the Light edition.

### The life of a tool

| Call | When |
|---|---|
| `Activate(host)` | The tool becomes active: when the window opens with it, or when the user switches to it. Reset your state here, a tool instance lives as long as the window and can be activated several times. |
| `OnMouseMove()` | Every mouse move, also when the arrow keys move the cursor. `Host.CursorPosition` is the new position. |
| `OnMouseDown()` / `OnMouseUp()` | Left button. Not called for a press on a hotspot, which opens the hotspot's menu instead. The mouse is captured in between, `OnMouseUp` comes only after an `OnMouseDown`. |
| `OnKeyDown(key)` | Before the window handles the key. Return true when you handled it. |
| `Draw(dc)` | When you call `Host.Redraw()`, when the tool becomes active, and when the detected features changed (e.g. the OCR finished). Draw everything, the layer is cleared first. |
| `Deactivate()` | Another tool becomes active. Clean up what you showed (selection, labels); your layer is redrawn by the next tool. |

`ShowsZoomer` is read when the tool becomes active, `ShowsCrosshair` after every mouse move, so it can depend on the state
(the region tool hides the crosshair while dragging).

### What the host offers

| Member | Use |
|---|---|
| `Capture` | The `ICapture` of the whole screen: the image, its `CaptureDetails` (title, metadata, detected features). |
| `ScreenBounds` | Where the capture is on the virtual screen, to convert to screen coordinates. |
| `CursorPosition` | The cursor in capture pixels, already corrected for the fix mode (shift keeps it to one direction). |
| `GetPixelColor(point)` | The color of a pixel of the frozen capture (a 1x1 copy, cheap enough for every mouse move). |
| `Windows`, `FindWindowUnderCursor(children)` | The visible windows in z-order, the (child) window under the cursor. |
| `ShowSelection(rect, animate, completed)`, `HideSelection()`, `IsSelectionVisible`, `IsSelectionAnimating` | The one selection rectangle. Animated with the window selection animation of the XAML; jumps in a remote desktop session. |
| `ShowLabels(rect, size, fadeIn, debugText)`, `ClearLabels()` | The size rulers at the sides of a rectangle and the size in its middle. |
| `Redraw()` | Redraw your layer (calls your `Draw`). |
| `FindResource(key)` | The brushes and other resources of `CaptureWindow.xaml`, e.g. `RulerBackgroundBrush`, `OcrHighlightBrush`. |
| `Accept(rect, window)` | Close the window with a selection. |
| `Cancel()` | Close the window without a selection. |
| `ActiveTool` | To check in a callback (animation completed, OCR finished) whether you are still active. |

## Example: a color picker

The color picker needs the pixel under the cursor, shows it next to the cursor, and copies it as `#RRGGBB` on a click.
It keeps the zoomer, which already magnifies the pixels around the cursor and marks the one under it.

1. Add `ColorPicker` at the end of the `CaptureMode` enum.
2. Add the class below.
3. Add `new ColorPickerTool()` to `_tools` in `CaptureWindow`.

C now switches to the color picker, Space goes back to the region selection.

```csharp
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Greenshot.Base.Interfaces;
using CaptureMode = Greenshot.Base.Interfaces.CaptureMode;

namespace Greenshot.UI.Capture.Tools
{
    /// <summary>
    /// Shows the color under the cursor, a click (or Enter) copies it as #RRGGBB to the clipboard
    /// </summary>
    public class ColorPickerTool : CaptureTool
    {
        private const int SwatchSize = 32;
        private Color _color = Colors.Transparent;

        public override CaptureMode Mode => CaptureMode.ColorPicker;

        public override Key ShortcutKey => Key.C;

        // The zoomer shows the pixels around the cursor, the crosshair where exactly the cursor is
        public override bool ShowsZoomer => true;

        public override bool ShowsCrosshair => true;

        public override void Activate(ICaptureToolHost host)
        {
            base.Activate(host);
            // Nothing of another tool should stay visible
            Host.HideSelection();
            Host.ClearLabels();
            _color = Host.GetPixelColor(Host.CursorPosition);
        }

        public override void OnMouseMove()
        {
            // The swatch moves with the cursor, so it is redrawn on every move
            _color = Host.GetPixelColor(Host.CursorPosition);
            Host.Redraw();
        }

        public override void OnMouseUp() => PickColor();

        public override bool OnKeyDown(Key key)
        {
            if (key != Key.Return)
            {
                return false;
            }
            PickColor();
            return true;
        }

        /// <summary>
        /// A swatch with the color and its value, above left of the cursor (the zoomer is at another corner, mostly bottom right)
        /// </summary>
        public override void Draw(DrawingContext drawingContext)
        {
            var cursor = Host.CursorPosition;
            var swatch = new Rect(cursor.X - 12 - SwatchSize, cursor.Y - 12 - SwatchSize, SwatchSize, SwatchSize);
            var border = new Pen(Brushes.White, 1);
            border.Freeze();
            drawingContext.DrawRectangle(new SolidColorBrush(_color), border, swatch);

            // One unit is one pixel in the capture window: the font size is in pixels, pixelsPerDip is 1
            var text = new FormattedText(ToHex(_color), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 12, Brushes.White, 1.0);
            var textBackground = new Rect(swatch.X, swatch.Y - text.Height - 2, text.Width + 4, text.Height);
            drawingContext.DrawRectangle((Brush)Host.FindResource("RulerBackgroundBrush"), null, textBackground);
            drawingContext.DrawText(text, new Point(textBackground.X + 2, textBackground.Y));
        }

        private void PickColor()
        {
            string hex = ToHex(_color);
            // There is no color in the selection result: close the window without a capture, then do the work
            Host.Cancel();
            Clipboard.SetText(hex);
        }

        private static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
```

This example was compiled against the current code, but not run.

Points to note:

- **The pixel:** `Host.GetPixelColor` reads the frozen capture, not the live screen, so it is the color of what was captured.
  Outside of the capture it returns transparent. Areas between monitors of different sizes are part of the capture, with whatever the capture has there.
- **Redrawing on every move** is fine for a few shapes: the layer is a `DrawingVisual`, there is no layout pass. Keep `Draw` small,
  it redraws the whole layer.
- **The result:** a tool can only return a rectangle and a window (`Accept`), see the restrictions below. The color picker therefore
  closes the window with `Cancel()` and does its work itself, like the hotspot actions (QR code) do. The flow ends there as "cancelled",
  so no destination runs.
- **Placement:** the swatch is drawn above left of the cursor, where the zoomer usually isn't. The zoomer only avoids the selection
  rectangle, not what a tool draws.

## What is possible

- Any number of tools, each with its own drawing, keys and mouse handling.
- Reuse by inheritance: `TextCaptureTool` is a `RegionCaptureTool` which adds the OCR lines and a click on a line.
- Using the detected features (`Capture.CaptureDetails.Features`, e.g. OCR lines, QR codes, windows) and starting work in the background,
  like the OCR of the text tool. Call `Host.Redraw()` when it is done.
- Changing the capture details before accepting, e.g. the text tool sets the capture mode, the window tool the title.
- A fixed size rectangle: `ShowSelection` with a rectangle of that size at the cursor in `OnMouseMove`, `Accept` it in `OnMouseUp`.
- Unit tests without a screen: implement `ICaptureToolHost` with a fake and call the tool's methods, see
  `src/Greenshot.Tests/Forms/RegionCaptureToolTests.cs`. They need WPF, so they run on Windows only.

## Restrictions

- **The result is a rectangle and a window.** `Accept` fills the `SelectionResult` of the flow (`SelectedRegion`, `SelectedWindow`,
  `FinalMode`). The flow crops the capture to the rectangle and, for `Text`, extracts the text. A tool with another kind of result
  (a color, a list of rectangles) either does its work itself and calls `Cancel()`, or needs an extension of `SelectionResult`,
  `ICaptureToolHost.Accept` and `InteractiveSelectionStep` so a recipe can use the result.
- **One layer per tool.** `Draw` redraws the whole layer, below the selection and its labels and above the hotspots.
  A large static drawing which should not be redrawn on every move would need a second layer (a change in the window).
- **One selection rectangle and one set of labels**, shared by all tools.
- **The window's keys:** the arrow keys move the cursor (Ctrl for 10 pixels), Shift fixes a direction, Escape cancels, M toggles the
  captured mouse cursor, Z the zoomer, F topmost, Space switches between region and window, and the shortcut keys of the tools switch
  to them. A tool sees every key first and could take one of them, don't take Escape or the arrow keys.
- **Hotspots come first:** a press on a hotspot (e.g. a QR code) opens its menu and never reaches the tool.
- **The UI thread:** everything is called on the UI thread of the window. Don't block in a handler, run longer work as a task and
  call `Host.Redraw()` (on the UI thread) when it finished. Use `Host.ActiveTool == this` to check you are still active.
- **Coordinates:** everything is in pixels of the capture, 0,0 is the top left of the virtual screen. One unit in `Draw` is one
  pixel, also on high DPI screens, so give font sizes in pixels and use 1.0 as pixelsPerDip.
- **Remote desktop:** the window shows no animations there (`OptimizeForRDP`, `DisableRDPOptimizing`). `ShowSelection` takes
  care of that, a tool should not add animations of its own, and keep redrawing to what changed.
- **Not from plugins (yet):** the tool list is in `CaptureWindow`. Tools from plugins would need the list to come from the
  service provider (`SimpleServiceProvider.Current.GetAllInstances<ICaptureTool>()`), which is a small change, but the
  interfaces would then be public API.
