# Capture tools and overlays: extending the interactive capture

The interactive capture shows a frozen capture of the whole screen in the `CaptureWindow` (WPF, `src/Greenshot/UI/Capture`).
What the user can do on it comes from **capture tools**: the region, window and text selections are each one tool class in
`src/Greenshot/UI/Capture/Tools`. One tool is active at a time. Next to it, **overlays** can run for as long as the window is open,
e.g. a color picker which shows the color under the cursor while a region is selected. Plugins can add both.
This document explains how to write them, in Greenshot itself or in a plugin, and what is possible and what is not,
with a color picker as the example.

The interfaces are in `Greenshot.Base` (`src/Greenshot.Base/Interfaces/Capture`, namespace `Greenshot.Base.Interfaces.Capture`):
`ICaptureTool` with its base class `CaptureTool`, `ICaptureOverlay` with its base class `CaptureOverlay`, `ICaptureToolHost`
(what the window offers tools and overlays), and `ICaptureToolProvider` / `ICaptureOverlayProvider` (how a plugin adds them).

## How it fits together

```
CaptureWindow (ICaptureToolHost)                 ICaptureTool (derive from CaptureTool)
 - frozen capture, cursor, fix mode (shift)       - Mode, ShortcutKey
 - crosshair, zoomer, hotspots (QR codes...)      - ShowsZoomer, ShowsCrosshair
 - selection rectangle + its animation            - Activate / Deactivate
 - size rulers and labels                         - OnMouseMove / OnMouseDown / OnMouseUp
 - keys: arrows, Esc, M, Z, F, Space, shortcuts   - OnKeyDown (sees keys first)
 - Accept / Cancel  ──────────────────────────►    - Draw (its own layer)
                     │
                     └────────────────────────►  ICaptureOverlay (derive from CaptureOverlay), any number
                                                  - Attach, OnToolChanged
                                                  - OnMouseMove (after the tool)
                                                  - OnKeyDown (keys nobody else used)
                                                  - Draw (its own layer, above the tool's)
```

The window does everything the tools share and passes the input to the **active tool**. A tool only contains what makes it different.
The overlays get the mouse moves too, and the keys which neither the tool nor the window used, but never the mouse buttons.

**Tool or overlay?** A tool is a way to make the selection: it owns the mouse buttons and decides what is accepted. An overlay adds
information or a shortcut to whatever the user is doing, without changing how the selection works.

| Tool | Mode | Key | What it does |
|---|---|---|---|
| `RegionCaptureTool` | `Region` | Space toggles | Drag or Enter/Enter for a rectangle, includes the pixel under the cursor |
| `WindowCaptureTool` | `Window` | Space toggles | The selection animates to the (child) window under the cursor, D for debug info |
| `TextCaptureTool` | `Text` | T | Extends the region tool: shows the OCR lines, a click selects a line |

## Adding a tool, step by step

1. Create a class that derives from `CaptureTool`. It has empty implementations of everything,
   override only what you need. `Host` (the `ICaptureToolHost`) is set when the tool is activated.
2. Give it a `Mode`, the `CaptureMode` the flow sees when your tool made the selection (`FinalMode` of the selection result).
   The window starts with the first tool whose mode the recipe asked for, so a tool with the mode of a built-in tool is never
   the starting tool. In Greenshot itself a new kind of selection can get its own value at the end of `CaptureMode`
   (`src/Greenshot.Base/Interfaces/CaptureMode.cs`); a plugin uses an existing value, e.g. `Region` when it accepts a rectangle.
3. Optionally a `ShortcutKey`, which switches to the tool.
4. Make the window use it:
   - **In Greenshot:** put the class in `src/Greenshot/UI/Capture/Tools` and add it to `CreateTools` in `CaptureWindow.xaml.cs`.
     Wrap it in `#if !GREENSHOT_LIGHT` if the tool should not be in the Light edition.
   - **In a plugin:** implement `ICaptureToolProvider` and register it in the plugin's `Initialize`, see below.

### Tools from a plugin

```csharp
public class ColorPickerToolProvider : ICaptureToolProvider
{
    // Called every time a capture window opens: return new instances, a tool keeps its state for one window
    public IEnumerable<ICaptureTool> CreateTools()
    {
        yield return new ColorPickerTool();
    }
}

// In the plugin's Initialize, like the QR code plugin registers its hotspot transformer
SimpleServiceProvider.Current.AddService<ICaptureToolProvider>(new ColorPickerToolProvider());
```

The tools of the plugins come after the built-in ones, in the order the plugins were loaded. A shortcut key which is already
taken (by the window or an earlier tool) does not switch to the later tool. The Light edition loads no plugins, so it has no plugin tools.

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
| `Redraw()`, `Redraw(overlay)` | Redraw the layer of the active tool, or of an overlay (calls its `Draw`). |
| `FindResource(key)` | The brushes and other resources of `CaptureWindow.xaml`, e.g. `RulerBackgroundBrush`, `OcrHighlightBrush`. |
| `Accept(rect, window)` | Close the window with a selection. |
| `Cancel()` | Close the window without a selection. |
| `ActiveTool` | To check in a callback (animation completed, OCR finished) whether you are still active. |

## Overlays

1. Create a class that derives from `CaptureOverlay`, override what you need (see the table).
2. Make the window use it:
   - **In a plugin:** implement `ICaptureOverlayProvider`, which returns new instances for every window that opens, and register it
     in the plugin's `Initialize` with `SimpleServiceProvider.Current.AddService<ICaptureOverlayProvider>(...)`.
   - **In Greenshot:** register a provider the same way at startup; there are no built-in overlays yet.

| Call | When |
|---|---|
| `Attach(host)` | The window opened. `Host` is set. |
| `OnMouseMove()` | Every mouse move, after the active tool handled it. |
| `OnToolChanged()` | The user switched to another tool, `Host.ActiveTool` is the new one. |
| `OnKeyDown(key)` | A key which neither the active tool nor the window used (also not a tool's shortcut key). Return true when you handled it; the later overlays don't get it then. |
| `Draw(dc)` | When you call `Host.Redraw(this)`, when the window was shown, and when the detected features changed. |

Each overlay has its own layer, above the tool's layer and below the selection, in the order the overlays were added.
An error in an overlay's handler is logged and does not stop the capture.

## Example: a color picker

The color picker needs the pixel under the cursor, shows it next to the cursor whatever tool is active, and copies it as `#RRGGBB`
with C. That makes it an overlay: the user can select a region as usual, and press C when they want the color instead.
H hides the swatch. The zoomer of the region tool already magnifies the pixels around the cursor and marks the one under it.

```csharp
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Greenshot.Base.Interfaces.Capture;

namespace Greenshot.Plugin.ColorPicker
{
    /// <summary>
    /// Shows the color under the cursor next to it, whatever tool is active. C copies it as #RRGGBB to the clipboard and closes the window,
    /// H hides or shows the swatch.
    /// </summary>
    public class ColorPickerOverlay : CaptureOverlay
    {
        private const int SwatchSize = 32;
        private Color _color = Colors.Transparent;
        private bool _visible = true;

        public override void Attach(ICaptureToolHost host)
        {
            base.Attach(host);
            _color = Host.GetPixelColor(Host.CursorPosition);
        }

        public override void OnMouseMove()
        {
            if (!_visible)
            {
                return;
            }
            // The swatch moves with the cursor, so it is redrawn on every move
            _color = Host.GetPixelColor(Host.CursorPosition);
            Host.Redraw(this);
        }

        public override bool OnKeyDown(Key key)
        {
            switch (key)
            {
                case Key.C:
                    string hex = ToHex(_color);
                    // There is no color in the selection result: close the window without a capture, then do the work
                    Host.Cancel();
                    Clipboard.SetText(hex);
                    return true;
                case Key.H:
                    _visible = !_visible;
                    OnMouseMove();
                    if (!_visible)
                    {
                        // Draws nothing, which clears the layer
                        Host.Redraw(this);
                    }
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// A swatch with the color and its value, above left of the cursor (the zoomer is at another corner, mostly bottom right)
        /// </summary>
        public override void Draw(DrawingContext drawingContext)
        {
            if (!_visible)
            {
                return;
            }
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

        private static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    /// <summary>
    /// Gives every capture window a new color picker, registered in the plugin's Initialize
    /// </summary>
    public class ColorPickerOverlayProvider : ICaptureOverlayProvider
    {
        public IEnumerable<ICaptureOverlay> CreateOverlays()
        {
            yield return new ColorPickerOverlay();
        }
    }
}
```

In the plugin's `Initialize`:

```csharp
SimpleServiceProvider.Current.AddService<ICaptureOverlayProvider>(new ColorPickerOverlayProvider());
```

This example was compiled against the current code, but not run.

Points to note:

- **The pixel:** `Host.GetPixelColor` reads the frozen capture, not the live screen, so it is the color of what was captured.
  Outside of the capture it returns transparent. Areas between monitors of different sizes are part of the capture, with whatever the capture has there.
- **Redrawing on every move** is fine for a few shapes: the layer is a `DrawingVisual`, there is no layout pass. Keep `Draw` small,
  it redraws the whole layer.
- **The result:** neither tools nor overlays can return a color, see the restrictions below. The color picker therefore
  closes the window with `Cancel()` and does its work itself, like the hotspot actions (QR code) do. The flow ends there as "cancelled",
  so no destination runs.
- **Placement:** the swatch is drawn above left of the cursor, where the zoomer usually isn't. The zoomer only avoids the selection
  rectangle, not what tools and overlays draw.
- **As a tool instead:** derive from `CaptureTool`, give it a `ShortcutKey` and copy the color in `OnMouseUp`. Then it is a mode the
  user switches to, and a click picks the color, but it can't show the color during a region selection.

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
- **One layer per tool or overlay.** `Draw` redraws the whole layer; the layers are below the selection and its labels and above the hotspots.
  A large static drawing which should not be redrawn on every move would need a second layer (a change in the window).
- **One selection rectangle and one set of labels**, shared by all tools.
- **The window's keys:** the arrow keys move the cursor (Ctrl for 10 pixels), Shift fixes a direction, Escape cancels, M toggles the
  captured mouse cursor, Z the zoomer, F topmost, Space switches from region to window and from every other tool back to region,
  and the shortcut keys of the tools switch to them. A tool sees every key first and could take one of them, don't take Escape or the arrow keys.
- **Hotspots come first:** a press on a hotspot (e.g. a QR code) opens its menu and never reaches the tool.
- **The UI thread:** everything is called on the UI thread of the window. Don't block in a handler, run longer work as a task and
  call `Host.Redraw()` (on the UI thread) when it finished. Use `Host.ActiveTool == this` to check you are still active.
- **Coordinates:** everything is in pixels of the capture, 0,0 is the top left of the virtual screen. One unit in `Draw` is one
  pixel, also on high DPI screens, so give font sizes in pixels and use 1.0 as pixelsPerDip.
- **Remote desktop:** the window shows no animations there (`OptimizeForRDP`, `DisableRDPOptimizing`). `ShowSelection` takes
  care of that, a tool should not add animations of its own, and keep redrawing to what changed.
- **Plugin tools run inside the capture window:** an exception in a tool's handler is not caught per tool, so it breaks the
  interactive capture. Errors in `ICaptureToolProvider.CreateTools` and `ICaptureOverlayProvider.CreateOverlays` are logged and
  that provider is skipped; errors in an overlay's handlers are logged.
- **Overlays never get mouse buttons,** and only the keys nobody else used: they can't change how the selection works.
- **Plugin API:** the interfaces in `Greenshot.Base.Interfaces.Capture` are now used by plugins, so changing them breaks plugins.
  Add members to `CaptureTool` (with an empty implementation) rather than to `ICaptureTool` where possible.
