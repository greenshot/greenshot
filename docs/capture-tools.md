# Capture tools and overlays: extending the interactive capture

The interactive capture shows a frozen capture of the whole screen in the `CaptureWindow` (WPF, `src/Greenshot/Capturing/Views`).
What the user can do on it comes from **capture tools**: the region, window and text selections are each one tool class in
`src/Greenshot/Capturing/Tools`. One tool is active at a time. Next to it, **overlays** can run for as long as the window is open,
e.g. a color picker which shows the color under the cursor while a region is selected. Plugins can add both.
This document explains how to write them, in Greenshot itself or in a plugin, and what is possible and what is not,
with a color picker as the example.

The interfaces are in `Greenshot.Base` (`src/Greenshot.Base/Interfaces/Capture`, namespace `Greenshot.Base.Interfaces.Capture`):
`ICaptureTool` with its base class `CaptureTool`, `ICaptureOverlay` with its base class `CaptureOverlay`, `ICaptureToolHost`
(what the window offers tools and overlays), and `ICaptureToolProvider` / `ICaptureOverlayProvider` (how a plugin adds them).

## How it fits together

```
CaptureWindow (ICaptureToolHost)                 ICaptureTool (derive from CaptureTool)
 - frozen capture, cursor, fix mode (shift)       - Mode
 - crosshair, zoomer, hotspots (QR codes...)      - ShowsZoomer, ShowsCrosshair
 - selection rectangle + its animation            - Attach (register keys)
 - size rulers and labels                         - Activate / Deactivate
 - keys: a registry of every key, see Keys        - OnMouseMove / OnMouseDown / OnMouseUp
 - Accept / Cancel  ──────────────────────────►    - Draw (its own layer)
                     │
                     └────────────────────────►  ICaptureOverlay (derive from CaptureOverlay), any number
                                                  - Attach (register keys), OnToolChanged
                                                  - OnMouseMove (after the tool)
                                                  - Draw (its own layer, above the tool's)
```

The window does everything the tools share and passes the input to the **active tool**. A tool only contains what makes it different.
The overlays get the mouse moves too, but never the mouse buttons. Keys work through registration: the window, the tools and the
overlays register each key with a description and what it does, see Keys.

**Tool or overlay?** A tool is a way to make the selection: it owns the mouse buttons and decides what is accepted. An overlay adds
information or a shortcut to whatever the user is doing, without changing how the selection works.

| Tool | Mode | Key | What it does |
|---|---|---|---|
| `RegionCaptureTool` | `Region` | Space toggles | Drag or Enter/Enter for a rectangle, includes the pixel under the cursor |
| `WindowCaptureTool` | `Window` | Space toggles | The selection animates to the (child) window under the cursor, D for debug info |
| `TextCaptureTool` | `Text` | T | Extends the region tool: shows the OCR lines, a click selects a line |

## Adding a tool, step by step

1. Create a class that derives from `CaptureTool`. It has empty implementations of everything,
   override only what you need. `Host` (the `ICaptureToolHost`) is set in `Attach`, when the window opens.
2. Give it a `Mode`, the `CaptureMode` the flow sees when your tool made the selection (`FinalMode` of the selection result).
   The window starts with the first tool whose mode the recipe asked for, so a tool with the mode of a built-in tool is never
   the starting tool. In Greenshot itself a new kind of selection can get its own value at the end of `CaptureMode`
   (`src/Greenshot.Base/Interfaces/CaptureMode.cs`); a plugin uses an existing value, e.g. `Region` when it accepts a rectangle.
3. Register its keys in `Attach`, see Keys: `Host.RegisterToolKey` for keys which only work while the tool is active (e.g. Enter),
   and optionally `Host.RegisterKey` for a key which switches to the tool (`Host.ActivateTool(this)`), like T for the text tool.
4. Make the window use it:
   - **In Greenshot:** put the class in `src/Greenshot/Capturing/Tools` and add it to `CreateTools` in `CaptureWindow.xaml.cs`.
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

The tools of the plugins come after the built-in ones, in the order the plugins were loaded. A key which is already taken
(by the window or an earlier tool) is refused with an error, see Keys. The Light edition loads no plugins, so it has no plugin tools.

### The life of a tool

| Call | When |
|---|---|
| `Attach(host)` | Once, when the window opens, for every tool before one is activated. Register your keys here. |
| `Activate(host)` | The tool becomes active: when the window opens with it, or when the user switches to it. Reset your state here, a tool instance lives as long as the window and can be activated several times. |
| `OnMouseMove()` | Every mouse move, also when the arrow keys move the cursor. `Host.CursorPosition` is the new position. |
| `OnMouseDown()` / `OnMouseUp()` | Left button. Not called for a press on a hotspot, which opens the hotspot's menu instead. The mouse is captured in between, `OnMouseUp` comes only after an `OnMouseDown`. |
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
| `GetMonitorBounds()` | The monitor under the cursor, in capture pixels. |
| `ShowPanel(owner, contentSize, drawContent)`, `HidePanel(owner)` | A panel in Greenshot's style in a free corner, moved (animated) out of the way, see Positioning. |
| `ShowPanel(owner, content)` | The same panel with WPF content (a `UserControl`, bindings, a view model), see WPF content and binding. |
| `ToolStyle` | Greenshot's look: theme colors, font, text, panels and key caps, see Look and feel. |
| `Windows`, `FindWindowUnderCursor(children)` | The visible windows in z-order, the (child) window under the cursor. |
| `ShowSelection(rect, animate, completed)`, `HideSelection()`, `IsSelectionVisible`, `IsSelectionAnimating`, `Selection`, `SelectionSize` | The one selection rectangle (`Selection` is where it is or goes, `SelectionSize` the size the labels show). Animated with the window selection animation of the XAML; jumps in a remote desktop session. |
| `ShowLabels(rect, size, fadeIn, debugText)`, `ClearLabels()` | The size rulers at the sides of a rectangle and the size in its middle. |
| `Redraw()`, `Redraw(overlay)` | Redraw the layer of the active tool, or of an overlay (calls its `Draw`). |
| `FindResource(key)` | The brushes and other resources of `CaptureWindow.xaml`, e.g. `RulerBackgroundBrush`, `OcrHighlightBrush`. |
| `Accept(rect, window)` | Close the window with a selection. |
| `Cancel()` | Close the window without a selection. |
| `ActiveTool`, `ActivateTool(tool)` | To check in a callback (animation completed, OCR finished) whether you are still active; switch to another tool. |
| `RegisterKey(owner, key, modifiers, description, execute)`, `RegisterToolKey(tool, ...)`, `KeyBindings` | Register a key, list all keys, see Keys. |

## Overlays

1. Create a class that derives from `CaptureOverlay`, override what you need (see the table).
2. Make the window use it:
   - **In a plugin:** implement `ICaptureOverlayProvider`, which returns new instances for every window that opens, and register it
     in the plugin's `Initialize` with `SimpleServiceProvider.Current.AddService<ICaptureOverlayProvider>(...)`.
   - **In Greenshot:** add it to `CreateOverlays` in `CaptureWindow.xaml.cs`, like the built-in `HelpOverlay` (F1, a drawn panel)
     and `InfoOverlay` (I, a WPF panel bound to a view model: screen resolution, selection, window under the mouse, mouse position).

| Call | When |
|---|---|
| `Attach(host)` | The window opened. `Host` is set. Register your keys here. |
| `OnMouseMove()` | Every mouse move, after the active tool handled it. |
| `OnToolChanged()` | The user switched to another tool, `Host.ActiveTool` is the new one. |
| `Draw(dc)` | When you call `Host.Redraw(this)`, when the window was shown, and when the detected features changed. |

Each overlay has its own layer, above the tool's layer and below the selection, in the order the overlays were added.
An error in an overlay's handler is logged and does not stop the capture.

## Keys

Every key of the capture window is registered: by the window itself, by the tools and by the overlays. A registration says
which key (with Ctrl, Alt, Shift or Windows if needed), what it does, and a description for the user. The window looks up
the binding of a pressed key and calls it; a key nobody registered does nothing. Because of that the list of registrations
is complete, and the help overlay (F1) shows it.

```csharp
public override void Attach(ICaptureToolHost host)
{
    base.Attach(host);
    // Only while this tool is active
    host.RegisterToolKey(this, Key.Return, ModifierKeys.None, () => Texts.Core.CaptureKeyRegionSelect, ToggleSelection);
    // Always, also when another tool is active: switch to this tool
    host.RegisterKey(this, Key.T, ModifierKeys.None, () => Texts.Core.CaptureKeyText, () => host.ActivateTool(this));
}
```

- **The description is a callback.** It is called every time the text is needed (when the help is shown), not at the
  registration, so it returns the text in the current language. Greenshot uses its language sections
  (`Texts.Core.CaptureKey...`, keys `capture_key_...` in `greenshot.en-US.ini`); a plugin uses its own section (`Texts.Get<T>()`).
- **Conflicts are errors.** Registering a key which is already used throws a `CaptureKeyConflictException` with both
  bindings (`Requested`, `Existing`). A key registered with `RegisterKey` is always active and conflicts with every other use of
  that key. A key registered with `RegisterToolKey` is only active while its tool is; it conflicts with the always active keys and
  the other keys of the same tool, but two tools can use the same key (the region and window tools both use Enter).
  Ctrl+C and C are different keys.
- **Who wins:** the window registers first, then the built-in tools, then the plugin tools, then the built-in help and info
  overlays, then the plugin overlays. So a plugin can never take a built-in key. If you don't want the exception, look in `Host.KeyBindings` first.
  An exception in a plugin's `Attach` is logged; the tool or overlay keeps the keys it registered before.
- **Not allowed:** Ctrl, Alt or Windows alone (an `ArgumentException`): they only work together with another key.
- **Shift:** the window uses Shift (held) to keep the selection to one direction. When Shift is held and nothing is registered
  for the key with Shift, the binding without Shift is used, so the arrow keys work while Shift is held.
- **Querying:** `Host.KeyBindings` lists all registrations in order. Each `CaptureKeyBinding` has `Key`, `Modifiers`,
  `KeyText` (e.g. "Ctrl+↑"), `Description`, `Owner`, `Tool` (null for always active) and `IsActiveFor(Host.ActiveTool)`.
- Keys stay registered as long as the window is open; there is no unregistering. A handler which doesn't apply in some
  state can do nothing (the zoomer key does nothing while the tool shows no zoomer). An error in a key handler is logged.

The keys of the window and the built-in tools:

| Key | Who | What |
|---|---|---|
| Space | Window | Switch between region and window (from every other tool: back to region) |
| ↑ ↓ ← →, Ctrl+↑ ↓ ← → | Window | Move the cursor one pixel, 10 pixels |
| Shift (held) | Window | Keep the selection to one direction |
| M, Z | Window | Captured mouse cursor, zoomer |
| Esc | Window | Cancel |
| T | Text tool | Switch to the text tool |
| Enter | Region and text tool | Start or finish the selection |
| Enter, D | Window tool | Capture the window, window details |
| F1 | Help overlay | Show or hide the keys |
| I | Info overlay | Show or hide the info (screen, selection, window, mouse) |

### Example: the help overlay

The help is a built-in overlay (`src/Greenshot/Capturing/Overlays/HelpOverlay.cs`): F1 shows a panel with the keys which work now,
the keys of the active tool first, as key caps like the hotkey settings show them. Keys with the same description share a row
(the four arrow keys, left and right Shift). Because it reads `Host.KeyBindings`, keys of plugin tools and overlays are in it too,
and the descriptions are read when the panel is shown, in the current language. When the tool changes, the panel is shown again
with the keys of the new tool. Whether it is shown is remembered in the configuration (`CaptureHelpVisible` in `greenshot.ini`, on by default),
so it comes back with every capture until F1 hides it. The info overlay does the same with `CaptureInfoVisible`.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Capture;
using Greenshot.Base.Languages;

namespace Greenshot.Capturing.Overlays
{
    /// <summary>
    /// F1 shows or hides a panel with the keys which work now: those of the active tool first, then those which are always active.
    /// The list comes from the registered keys (ICaptureToolHost.KeyBindings), so keys of plugins are in it too,
    /// and the descriptions are read when the panel is shown, in the current language.
    /// Whether it is shown is remembered (CaptureHelpVisible), so it comes back with the next capture.
    /// </summary>
    public class HelpOverlay : CaptureOverlay
    {
        private static readonly ICoreConfiguration Conf = IniConfigRegistry.GetSection<ICoreConfiguration>();
        private bool _visible;

        public override void Attach(ICaptureToolHost host)
        {
            base.Attach(host);
            host.RegisterKey(this, Key.F1, ModifierKeys.None, () => Texts.Core.CaptureKeyHelp, Toggle);
            if (Conf.CaptureHelpVisible)
            {
                _visible = true;
                // Once the window is shown: then the monitor and its DPI are known
                _ = Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                {
                    if (_visible)
                    {
                        ShowHelp();
                    }
                }), DispatcherPriority.ContextIdle);
            }
        }

        /// <summary>
        /// Another tool has other keys
        /// </summary>
        public override void OnToolChanged()
        {
            if (_visible)
            {
                ShowHelp();
            }
        }

        private void Toggle()
        {
            _visible = !_visible;
            Conf.CaptureHelpVisible = _visible;
            if (_visible)
            {
                ShowHelp();
            }
            else
            {
                Host.HidePanel(this);
            }
        }

        /// <summary>
        /// One row per description: keys which do the same (e.g. the arrow keys, or left and right Shift) share a row
        /// </summary>
        public static IList<(IList<string> Keys, string Description)> GetRows(IEnumerable<CaptureKeyBinding> bindings, ICaptureTool activeTool)
        {
            var rows = new List<(IList<string> Keys, string Description)>();
            var active = (bindings ?? Enumerable.Empty<CaptureKeyBinding>()).Where(binding => binding.IsActiveFor(activeTool)).ToList();
            // The keys of the tool first, they change with the tool; OrderBy is stable, so the order of registration stays
            foreach (var binding in active.OrderBy(binding => binding.Tool == null ? 1 : 0))
            {
                string description = binding.Description;
                int index = rows.FindIndex(row => row.Description == description);
                if (index < 0)
                {
                    rows.Add((new List<string> { binding.KeyText }, description));
                }
                else if (!rows[index].Keys.Contains(binding.KeyText))
                {
                    rows[index].Keys.Add(binding.KeyText);
                }
            }
            return rows;
        }

        private void ShowHelp()
        {
            var style = Host.ToolStyle;
            double gap = style.Scale(8);
            double keyGap = style.Scale(3);
            var title = style.CreateText(Texts.Core.CaptureKeysTitle, 10, bold: true);

            // Measure first: the key caps in one column, right aligned, the descriptions next to them
            double keyColumnWidth = 0, textWidth = title.Width, height = title.Height + gap;
            var rows = new List<(IList<string> Keys, double KeysWidth, FormattedText Text, double Height)>();
            foreach (var (keys, description) in GetRows(Host.KeyBindings, Host.ActiveTool))
            {
                double keysWidth = 0, keysHeight = 0;
                foreach (var key in keys)
                {
                    var keyCap = style.MeasureKeyCap(key);
                    keysWidth += keyCap.Width + (keysWidth > 0 ? keyGap : 0);
                    keysHeight = Math.Max(keysHeight, keyCap.Height);
                }
                var text = style.CreateText(description, 9, style.MutedForeground);
                double rowHeight = Math.Max(keysHeight, text.Height) + style.Scale(4);
                keyColumnWidth = Math.Max(keyColumnWidth, keysWidth);
                textWidth = Math.Max(textWidth, text.Width);
                rows.Add((keys, keysWidth, text, rowHeight));
                height += rowHeight;
            }

            Host.ShowPanel(this, new Size(keyColumnWidth + gap + textWidth, height), dc =>
            {
                dc.DrawText(title, new Point(0, 0));
                double y = title.Height + gap;
                foreach (var (keys, keysWidth, text, rowHeight) in rows)
                {
                    double x = keyColumnWidth - keysWidth;
                    foreach (var key in keys)
                    {
                        var keyCap = style.MeasureKeyCap(key);
                        style.DrawKeyCap(dc, key, new Point(x, y + (rowHeight - keyCap.Height) / 2));
                        x += keyCap.Width + keyGap;
                    }
                    dc.DrawText(text, new Point(keyColumnWidth + gap, y + (rowHeight - text.Height) / 2));
                    y += rowHeight;
                }
            });
        }
    }
}
```

## Positioning

Tools and overlays draw on layers which cover the whole capture, in capture pixels; there is no layout, a tool decides in `Draw`
where things go. Two ways:

- **Next to the cursor**, like the color picker swatch below: use `Host.CursorPosition`. Keep it small and near the cursor; the zoomer
  is at one of the cursor's corners (mostly bottom right), so the opposite corner is usually free.
- **A panel in a corner**, like a help text: `Host.ShowPanel(this, contentSize, dc => ...)` with drawn content, or
  `Host.ShowPanel(this, content)` with WPF content (see below). The window does the rest:
  - It draws the panel in Greenshot's style (`ToolStyle.DrawPanel`: theme colors, rounded corners, padding) with your content on it.
  - It places it in the corner of the monitor under the cursor which is farthest from the cursor and doesn't cover the selection
    or other panels, at least `PanelPlacement.CursorClearance` (40 pixels) from the cursor and `PanelPlacement.Margin`
    (10) from the edge. The zoomer is not avoided: it moves with the cursor and avoids the panels itself.
  - On every mouse move it checks the place again. A panel stays where it is unless the cursor comes close, or it covers more
    than a quarter (`PanelPlacement.ToleratedOverlap`) of the selection, or the selection of it, while a completely free corner exists:
    going from one overlap to another isn't worth a jump. The selection only counts once it stayed the same for 0.3 s, so a
    window selection that follows the cursor from window to window doesn't chase the panels around. When a panel moves it
    **slides** to the new corner (`PanelMoveAnimation` in `CaptureWindow.xaml`, 0.35 s with the same easing as the window
    selection). A panel **fades** in when shown, and out with `Host.HidePanel(this)`.
  - The place is reserved: the zoomer and the other panels avoid it.
  - In a remote desktop session the panel jumps and appears without fading.

  With drawn content, call `ShowPanel` again only when the content changes, not for every move. With WPF content, change the
  view model; the panel follows its size.

`ScreenBounds` is the whole virtual screen, which can have gaps between monitors; use `GetMonitorBounds()` for "a corner of the screen".

The help overlay (see Keys) is an example of a panel: it is drawn with key caps and stays in a free corner.

### WPF content and binding

A panel can also show WPF content: `Host.ShowPanel(this, content)` with any `FrameworkElement`, usually a `UserControl` whose
`DataContext` is a view model. The overlay only updates the view model, the bindings update the text, and the window keeps the
panel in a free corner as above. The built-in info overlay (`src/Greenshot/Capturing/Overlays/InfoOverlay.cs`, the view `Capturing/Views/InfoPanelView.xaml` and its view model `Capturing/ViewModels/InfoPanelViewModel.cs`)
works this way. What the window does with the content:

- It puts it in a `Border` in Greenshot's style (`ToolStyle.CreatePanel`): theme background and border, rounded corners, padding,
  and Greenshot's font, size and foreground inherited by the content.
- The theme brushes are resources of that border, use them with `{DynamicResource CaptureTool.MutedForeground}` etc., see Look and feel.
- The content is in device independent units like any other XAML, the border scales it for the monitor.
- When the content changes size (a longer text), the panel is measured again; only the panel is laid out, not the capture.
  While shown it only grows and keeps its corner (the outer edges stay), so changing values like 99 and 100 don't make it wobble.
  It only slides when the bigger panel no longer fits there.
- Panels take no mouse input (`IsHitTestVisible` is false), clicks go to the active tool.
- Calling `ShowPanel` again with the same content does nothing; with other content it replaces the panel. `HidePanel(this)` fades it out.

Example: the cursor position and the color under it.

`CoordinatesPanel.xaml`:

```xml
<UserControl x:Class="Greenshot.Plugin.Coordinates.CoordinatesPanel"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <!-- Font, size and foreground come from the panel, the other theme brushes are resources of it -->
    <StackPanel>
        <TextBlock Text="Cursor" FontWeight="SemiBold" Margin="0,0,0,4" />
        <TextBlock Foreground="{DynamicResource CaptureTool.MutedForeground}">
            <Run Text="{Binding X, Mode=OneWay}" /><Run Text=" x " /><Run Text="{Binding Y, Mode=OneWay}" />
        </TextBlock>
        <StackPanel Orientation="Horizontal" Margin="0,4,0,0">
            <Border Width="14" Height="14" CornerRadius="2" BorderThickness="1" Margin="0,0,6,0"
                    BorderBrush="{DynamicResource CaptureTool.PanelBorder}" Background="{Binding ColorBrush}" />
            <TextBlock Text="{Binding ColorText}" Foreground="{DynamicResource CaptureTool.MutedForeground}" />
        </StackPanel>
    </StackPanel>
</UserControl>
```

`CoordinatesPanel.xaml.cs`:

```csharp
using System.Windows.Controls;

namespace Greenshot.Plugin.Coordinates
{
    public partial class CoordinatesPanel : UserControl
    {
        public CoordinatesPanel()
        {
            InitializeComponent();
        }
    }
}
```

`CoordinatesOverlay.cs`:

```csharp
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Greenshot.Base.Interfaces.Capture;

namespace Greenshot.Plugin.Coordinates
{
    /// <summary>
    /// What the panel shows, the bindings of CoordinatesPanel.xaml update it
    /// </summary>
    public class CoordinatesViewModel : INotifyPropertyChanged
    {
        private int _x;
        private int _y;
        private Color _color;

        public event PropertyChangedEventHandler PropertyChanged;

        public int X
        {
            get => _x;
            set => Set(ref _x, value);
        }

        public int Y
        {
            get => _y;
            set => Set(ref _y, value);
        }

        public Color Color
        {
            get => _color;
            set
            {
                if (Set(ref _color, value))
                {
                    OnPropertyChanged(nameof(ColorBrush));
                    OnPropertyChanged(nameof(ColorText));
                }
            }
        }

        public Brush ColorBrush => new SolidColorBrush(_color);

        public string ColorText => $"#{_color.R:X2}{_color.G:X2}{_color.B:X2}";

        private bool Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Shows the cursor position and the color under it on a panel, which the capture window keeps in a free corner
    /// </summary>
    public class CoordinatesOverlay : CaptureOverlay
    {
        private readonly CoordinatesViewModel _viewModel = new CoordinatesViewModel();

        public override void Attach(ICaptureToolHost host)
        {
            base.Attach(host);
            OnMouseMove();
            Host.ShowPanel(this, new CoordinatesPanel { DataContext = _viewModel });
        }

        public override void OnMouseMove()
        {
            // Only the view model: the bindings update the text, the window moves the panel when needed
            var cursor = Host.CursorPosition;
            _viewModel.X = cursor.X;
            _viewModel.Y = cursor.Y;
            _viewModel.Color = Host.GetPixelColor(cursor);
        }
    }

    /// <summary>
    /// Registered in the plugin's Initialize
    /// </summary>
    public class CoordinatesOverlayProvider : ICaptureOverlayProvider
    {
        public IEnumerable<ICaptureOverlay> CreateOverlays()
        {
            yield return new CoordinatesOverlay();
        }
    }
}
```

Updating three properties on every mouse move is cheap: a binding update only re-renders the text blocks, and the window moves the
panel only when the cursor comes close. This example was compiled against the current code, but not run.

## Look and feel

`Host.ToolStyle` (`CaptureToolStyle`) gives tools and overlays the look of Greenshot's WPF UI, if they want it:

| Member | What |
|---|---|
| `PanelBackground`, `PanelBorder`, `Foreground`, `MutedForeground`, `Accent`, `KeyCapBackground`, `KeyCapBorder` | Frozen brushes of the current theme (`ThemeManager.Instance.CurrentPalette`), light or dark like the rest of Greenshot. |
| `IsDarkTheme` | True for the dark theme. |
| `FontFamily`, `CreateText(text, points, brush, bold)` | Greenshot's UI font (Segoe UI); the size in points is scaled to pixels for the monitor. |
| `Scale(units)` | Converts a size in device independent units (as in XAML) to pixels, for margins and gaps. |
| `PanelPadding`, `PanelCornerRadius`, `DrawPanel(dc, rect)` | The panel as `ShowPanel` draws it, also for your own drawings (e.g. a small label). |
| `CreatePanel(content)` | The `Border` which `ShowPanel(owner, content)` puts around WPF content. |
| `PanelBackgroundKey`, `PanelBorderKey`, `ForegroundKey`, `MutedForegroundKey`, `AccentKey`, `KeyCapBackgroundKey`, `KeyCapBorderKey` | The resource keys (`CaptureTool.PanelBackground` etc.) of the brushes in WPF panel content, for `DynamicResource`. |
| `MeasureKeyCap(key)`, `DrawKeyCap(dc, key, point)` | A key cap like `KeyCapBadge` in the settings. |

The capture-specific brushes (selection, rulers, OCR highlight, hotspots) are resources of `CaptureWindow.xaml`, available with
`Host.FindResource(key)`. They don't follow the theme on purpose: they are drawn over the screenshot and need to be visible on any content.

A tool which wants its own look can ignore all of this and draw with its own brushes and fonts.

## Example: a color picker

The color picker needs the pixel under the cursor, shows it next to the cursor whatever tool is active, and copies it as `#RRGGBB`
with C. That makes it an overlay: the user can select a region as usual, and press C when they want the color instead.
H hides the swatch. The zoomer of the region tool already magnifies the pixels around the cursor and marks the one under it.

```csharp
using System.Collections.Generic;
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
            // The descriptions are shown by the help (F1); a plugin returns the text of its own language section here
            host.RegisterKey(this, Key.C, ModifierKeys.None, () => "Copy the color under the cursor", CopyColor);
            host.RegisterKey(this, Key.H, ModifierKeys.None, () => "Show or hide the color", ToggleSwatch);
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

        private void CopyColor()
        {
            string hex = ToHex(_color);
            // There is no color in the selection result: close the window without a capture, then do the work
            Host.Cancel();
            Clipboard.SetText(hex);
        }

        private void ToggleSwatch()
        {
            _visible = !_visible;
            OnMouseMove();
            if (!_visible)
            {
                // Draws nothing, which clears the layer
                Host.Redraw(this);
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
            var style = Host.ToolStyle;
            var border = new Pen(style.PanelBorder, 1);
            border.Freeze();
            drawingContext.DrawRectangle(new SolidColorBrush(_color), border, swatch);

            // The value on a small panel in Greenshot's style
            var text = style.CreateText(ToHex(_color), 9);
            double padding = style.Scale(3);
            var textPanel = new Rect(swatch.X, swatch.Y - text.Height - 2 * padding - 2, text.Width + 2 * padding, text.Height + 2 * padding);
            style.DrawPanel(drawingContext, textPanel);
            drawingContext.DrawText(text, new Point(textPanel.X + padding, textPanel.Y + padding));
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
- **Placement:** the swatch is drawn above left of the cursor, where the zoomer usually isn't. The zoomer avoids the selection and the
  panels shown with `ShowPanel`, not what is drawn elsewhere.
- **Keys:** C and H are free; if a later Greenshot version or an earlier plugin takes one of them, `RegisterKey` throws and the
  error is logged (the overlay keeps the keys it registered before). Check `Host.KeyBindings` first to pick another key instead.
- **As a tool instead:** derive from `CaptureTool`, register a key which switches to it and copy the color in `OnMouseUp`. Then it is a mode the
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
- **Keys only through registration,** and the built-in keys can't be taken (see Keys). There is no key up and no key repeat
  control: a binding is called for every key down, also when the key repeats.
- **Hotspots come first:** a press on a hotspot (e.g. a QR code) opens its menu and never reaches the tool.
- **The UI thread:** everything is called on the UI thread of the window. Don't block in a handler, run longer work as a task and
  call `Host.Redraw()` (on the UI thread) when it finished. Use `Host.ActiveTool == this` to check you are still active.
- **Coordinates:** everything is in pixels of the capture, 0,0 is the top left of the virtual screen. One unit in `Draw` is one
  pixel, also on high DPI screens, so give font sizes in pixels and use 1.0 as pixelsPerDip.
- **Key names** in `KeyText` are English ("Ctrl", "Esc"), the descriptions are translated.
- **Remote desktop:** the window shows no animations there (`OptimizeForRDP`, `DisableRDPOptimizing`). `ShowSelection` takes
  care of that, a tool should not add animations of its own, and keep redrawing to what changed.
- **Plugin tools run inside the capture window:** an exception in a tool's mouse handler or `Draw` is not caught per tool, so it
  breaks the interactive capture. Errors in key handlers and in `Attach` are logged. Errors in `ICaptureToolProvider.CreateTools` and `ICaptureOverlayProvider.CreateOverlays` are logged and
  that provider is skipped; errors in an overlay's handlers are logged.
- **Overlays never get mouse buttons,** and only the keys they registered: they can't change how the selection works.
- **Plugin API:** the interfaces in `Greenshot.Base.Interfaces.Capture` are now used by plugins, so changing them breaks plugins.
  Add members to `CaptureTool` (with an empty implementation) rather than to `ICaptureTool` where possible.
