/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Greenshot.Linux.Core;
using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Core.Destinations;
using Greenshot.Linux.Core.Output;
using Greenshot.Linux.Destinations;
using Greenshot.Linux.Editor;
using Greenshot.Linux.Editor.Effects;
using Greenshot.Linux.Editor.Elements;
using Greenshot.Linux.Editor.Serialization;
using Greenshot.Linux.Helpers;
using SkiaSharp;

namespace Greenshot.Linux.Editing;

/// <summary>
/// The image editor, the ImageEditorForm of Windows: tools on the left, the properties on top, the image in the middle.
/// The keys are the ones of the Windows editor.
/// </summary>
public partial class EditorWindow : Window
{
    private static readonly double[] ZoomLevels = [0.25, 0.5, 2.0 / 3, 0.75, 1, 1.5, 2, 3, 4, 6, 8];

    /// <summary>
    /// Elements copied in any editor window, so they can be pasted into another one
    /// </summary>
    private static string? _elementClipboard;

    private readonly IEditorHost _host;
    private readonly EditorSurface _surface;
    private readonly EditorController _controller;
    private readonly EditorCanvas _canvas;
    private readonly Dictionary<EditorTool, ToggleButton> _toolButtons = new();
    private readonly CaptureDetails _details;
    private string? _filePath;
    private TextBox? _textEditor;
    private DrawableElement? _editedElement;
    private bool _closeConfirmed;

    // For the XAML previewer only
    public EditorWindow()
    {
        InitializeComponent();
        _host = null!;
        _surface = new EditorSurface(ImageEffects.NewBitmap(400, 300));
        _controller = new EditorController(_surface);
        _canvas = new EditorCanvas(_controller);
        _details = new CaptureDetails();
    }

    internal EditorWindow(IEditorHost host, EditorSurface surface, CaptureDetails details, string? filePath)
    {
        InitializeComponent();
        _host = host;
        _surface = surface;
        _details = details;
        _filePath = filePath;
        Icon = AppIcons.Window;

        _controller = new EditorController(surface, host.EditorSettings);
        _canvas = new EditorCanvas(_controller);
        CanvasHost.Children.Insert(0, _canvas);
        PropertyBarHost.Content = new PropertyBar(_controller, host.EditorSettings);
        CreateToolButtons();
        CreateExportMenu();

        _surface.Changed += OnSurfaceChanged;
        _controller.StateChanged += UpdateToolButtons;
        _controller.TextEditRequested += BeginTextEdit;
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
        Scroller.AddHandler(PointerWheelChangedEvent, OnPointerWheel, RoutingStrategies.Tunnel);
        Opened += (_, _) => FitWindowToImage();
        Closing += OnClosing;
        Closed += (_, _) => _surface.Dispose();

        UpdateTitle();
        UpdateStatus(null);
    }

    #region Building the window

    private void CreateToolButtons()
    {
        AddTool(EditorTool.Select, "cursor", Texts.Editor("cursortool"));
        AddTool(EditorTool.Rectangle, "layer-shape", Texts.Editor("drawrectangle"));
        AddTool(EditorTool.Ellipse, "layer-shape-ellipse", Texts.Editor("drawellipse"));
        AddTool(EditorTool.Line, "layer-shape-line", Texts.Editor("drawline"));
        AddTool(EditorTool.Arrow, "layer-shape-arrow", Texts.Editor("drawarrow"));
        AddTool(EditorTool.Freehand, "pencil-color", Texts.Editor("drawfreehand"));
        AddTool(EditorTool.Text, "layer-shape-text", Texts.Editor("drawtextbox"));
        AddTool(EditorTool.SpeechBubble, "balloon", Texts.Editor("speechbubble"));
        AddTool(EditorTool.StepLabel, "notification-counter-01", Texts.Editor("counter"));
        AddTool(EditorTool.Highlight, "highlighter-text", Texts.Editor("drawhighlighter"));
        AddTool(EditorTool.Obfuscate, "image-pixelate", Texts.Editor("obfuscate"));
        AddTool(EditorTool.Crop, "ruler-crop", Texts.Editor("crop"));

        ToolPanel.Children.Add(new Separator { Margin = new Thickness(0, 4) });
        AddAction("arrow-resize", Texts.Editor("resize"), () => _ = ResizeAsync());
        AddAction("magnifier-zoom-in", Texts.Linux("zoom_in") + " (Ctrl +)", () => ZoomStep(+1));
        AddAction("magnifier-zoom-out", Texts.Linux("zoom_out") + " (Ctrl -)", () => ZoomStep(-1));
        AddAction("magnifier-zoom-actual", Texts.Linux("zoom_actual") + " (Ctrl 0)", () => SetZoom(1));
        AddAction("magnifier-zoom-fit", Texts.Linux("zoom_fit") + " (Ctrl 9)", ZoomToFit);
        ToolPanel.Children.Add(new Separator { Margin = new Thickness(0, 4) });
        AddAction("disk-black", Texts.Label(Texts.Editor("save")) + " (Ctrl S)", () => _ = SaveAsync());
        AddAction("clipboard", Texts.Label(Texts.Editor("copyimagetoclipboard")) + " (Ctrl Shift C)", () => _ = CopyImageAsync());
        UpdateToolButtons();
    }

    private void AddTool(EditorTool tool, string icon, string tooltip)
    {
        var button = new ToggleButton { Content = IconImage(icon), Padding = new Thickness(6), Focusable = false };
        ToolTip.SetTip(button, tooltip);
        button.Click += (_, _) =>
        {
            CommitTextEdit();
            _controller.Tool = tool;
            UpdateToolButtons();
        };
        _toolButtons[tool] = button;
        ToolPanel.Children.Add(button);
    }

    private void AddAction(string icon, string tooltip, Action action)
    {
        var button = new Button { Content = IconImage(icon), Padding = new Thickness(6), Focusable = false };
        ToolTip.SetTip(button, tooltip);
        button.Click += (_, _) => action();
        ToolPanel.Children.Add(button);
    }

    private static Image IconImage(string name) => new()
    {
        Source = new Bitmap(AssetLoader.Open(new Uri($"avares://greenshot/Assets/editor/{name}.png"))),
        Width = 16,
        Height = 16,
    };

    private void CreateExportMenu()
    {
        foreach (var destination in _host.ExportDestinations)
        {
            var item = new MenuItem { Header = destination.DisplayName, IsEnabled = destination.IsAvailable };
            item.Click += (_, _) => _ = ExportAsync(destination);
            ExportMenu.Items.Add(item);
        }
    }

    private void UpdateToolButtons()
    {
        foreach (var (tool, button) in _toolButtons)
        {
            button.IsChecked = tool == _controller.Tool;
        }

        UndoMenu.IsEnabled = _surface.CanUndo;
        RedoMenu.IsEnabled = _surface.CanRedo;
    }

    /// <summary>
    /// The window fits the image like on Windows (MatchSizeToCapture), as far as the screen allows; large images are zoomed out
    /// </summary>
    private void FitWindowToImage()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        var available = screen?.WorkingArea.Size.ToSize(screen.Scaling) ?? new Size(1600, 1000);
        if (_host.EditorSettings.MatchSizeToCapture)
        {
            // The chrome: tools on the left, menu, properties and status on top and bottom, and the margin
            double chromeWidth = ToolPanel.Bounds.Width + 80;
            double chromeHeight = 150;
            Width = Math.Clamp(_surface.Width + chromeWidth, MinWidth, available.Width * 0.95);
            Height = Math.Clamp(_surface.Height + chromeHeight, MinHeight, available.Height * 0.95);
        }

        Dispatcher.UIThread.Post(() =>
        {
            var viewport = Scroller.Viewport;
            if (_surface.Width > viewport.Width - 32 || _surface.Height > viewport.Height - 32)
            {
                ZoomToFit();
            }

            _canvas.Focus();
        }, DispatcherPriority.Loaded);
    }

    #endregion

    #region Status

    private void OnSurfaceChanged()
    {
        UpdateTitle();
        UpdateToolButtons();
        UpdateStatus(null);
    }

    private void UpdateTitle()
    {
        var name = _filePath != null ? Path.GetFileName(_filePath) : _details.Title ?? "Greenshot";
        Title = $"{(_surface.IsModified ? "*" : string.Empty)}{name} - {Texts.Editor("title")}";
    }

    private void UpdateStatus(string? message)
    {
        if (message != null)
        {
            StatusText.Text = message;
        }

        SizeText.Text = string.Create(CultureInfo.CurrentCulture, $"{_surface.Width} × {_surface.Height}");
        ZoomText.Text = string.Create(CultureInfo.CurrentCulture, $"{_canvas.Zoom * 100:0}%");
    }

    #endregion

    #region Keys

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        // Typing in the text box, a number field or a combo box is not a shortcut
        if (FocusManager?.GetFocusedElement() is TextBox or ComboBox || _textEditor != null)
        {
            return;
        }

        e.Handled = HandleKey(e.Key, e.KeyModifiers);
    }

    private bool HandleKey(Key key, KeyModifiers modifiers)
    {
        bool control = modifiers.HasFlag(KeyModifiers.Control);
        bool shift = modifiers.HasFlag(KeyModifiers.Shift);
        bool alt = modifiers.HasFlag(KeyModifiers.Alt);
        if (alt)
        {
            return false;
        }

        if (control && shift)
        {
            switch (key)
            {
                case Key.S:
                    _ = SaveAsAsync();
                    return true;
                case Key.C:
                    _ = CopyImageAsync();
                    return true;
                case Key.V:
                    _ = PasteImageAsync();
                    return true;
                case Key.OemPlus or Key.Add:
                    ApplyImageEffect(image => ImageEffects.ResizeCanvas(image, 25, 25, 25, 25));
                    return true;
                case Key.OemMinus or Key.Subtract:
                    ShrinkCanvas();
                    return true;
                case Key.Left or Key.Right or Key.Up or Key.Down:
                    return Move(key, 10);
            }

            return false;
        }

        if (control)
        {
            switch (key)
            {
                case Key.Z:
                    _surface.Undo();
                    return true;
                case Key.Y:
                    _surface.Redo();
                    return true;
                case Key.C:
                    Copy();
                    return true;
                case Key.X:
                    Cut();
                    return true;
                case Key.V:
                    _ = PasteAsync();
                    return true;
                case Key.D:
                    _controller.Duplicate();
                    return true;
                case Key.A:
                    _controller.SelectAll();
                    return true;
                case Key.S:
                    _ = SaveAsync();
                    return true;
                case Key.O:
                    _ = OpenAsync();
                    return true;
                case Key.W:
                    Close();
                    return true;
                case Key.Q:
                    ApplyImageEffect(image => ImageEffects.DropShadow(image));
                    return true;
                case Key.B:
                    ApplyImageEffect(image => ImageEffects.Border(image));
                    return true;
                case Key.T:
                    ApplyImageEffect(image => ImageEffects.TornEdge(image));
                    return true;
                case Key.I:
                    ApplyImageEffect(ImageEffects.Invert);
                    return true;
                case Key.G:
                    ApplyImageEffect(ImageEffects.Grayscale);
                    return true;
                case Key.Delete:
                    ApplyImageEffect(ImageEffects.Clear);
                    return true;
                case Key.OemComma:
                    ApplyImageEffect(ImageEffects.RotateCounterClockwise);
                    return true;
                case Key.OemPeriod:
                    ApplyImageEffect(ImageEffects.RotateClockwise);
                    return true;
                case Key.OemPlus or Key.Add:
                    ZoomStep(+1);
                    return true;
                case Key.OemMinus or Key.Subtract:
                    ZoomStep(-1);
                    return true;
                case Key.D0 or Key.NumPad0:
                    SetZoom(1);
                    return true;
                case Key.D9 or Key.NumPad9:
                    ZoomToFit();
                    return true;
                case Key.Left or Key.Right or Key.Up or Key.Down:
                    return Move(key, 10);
            }

            return false;
        }

        switch (key)
        {
            case Key.Escape:
                _controller.Escape();
                return true;
            case Key.Enter when _controller.Tool == EditorTool.Crop:
                _controller.ConfirmCrop();
                return true;
            case Key.Enter when _surface.Selection is [{ HasText: true } element]:
                BeginTextEdit(element);
                return true;
            case Key.Delete or Key.Back:
                _controller.DeleteSelection();
                return true;
            case Key.Left or Key.Right or Key.Up or Key.Down:
                return Move(key, shift ? 10 : 1);
            case Key.PageUp:
                _surface.Arrange(EditorSurface.ZOrder.Up);
                return true;
            case Key.PageDown:
                _surface.Arrange(EditorSurface.ZOrder.Down);
                return true;
            case Key.Home:
                _surface.Arrange(EditorSurface.ZOrder.Top);
                return true;
            case Key.End:
                _surface.Arrange(EditorSurface.ZOrder.Bottom);
                return true;
            case Key.Z:
                _ = ResizeAsync();
                return true;
        }

        // Like on Windows the tool letters only work without modifiers, Shift + a letter does nothing
        if (shift)
        {
            return false;
        }

        EditorTool? tool = key switch
        {
            Key.R => EditorTool.Rectangle,
            Key.E => EditorTool.Ellipse,
            Key.L => EditorTool.Line,
            Key.F => EditorTool.Freehand,
            Key.A => EditorTool.Arrow,
            Key.T => EditorTool.Text,
            Key.S => EditorTool.SpeechBubble,
            Key.I => EditorTool.StepLabel,
            Key.H => EditorTool.Highlight,
            Key.O => EditorTool.Obfuscate,
            Key.C => EditorTool.Crop,
            _ => null,
        };
        if (tool != null)
        {
            _controller.Tool = tool.Value;
            UpdateToolButtons();
            return true;
        }

        return false;
    }

    private bool Move(Key key, int step) => key switch
    {
        Key.Left => _controller.MoveSelection(-step, 0),
        Key.Right => _controller.MoveSelection(step, 0),
        Key.Up => _controller.MoveSelection(0, -step),
        _ => _controller.MoveSelection(0, step),
    };

    #endregion

    #region Text editing

    /// <summary>
    /// A text box over the element; the text is taken over when it loses the focus, with Esc or Ctrl+Enter
    /// </summary>
    private void BeginTextEdit(DrawableElement element)
    {
        CommitTextEdit();
        if (element is not TextElement textElement)
        {
            return;
        }

        var rect = element.NormalizedBounds;
        var style = element.Style;
        var editor = new TextBox
        {
            Text = textElement.Text,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily(style.FontFamily),
            FontSize = Math.Max(6, style.FontSize * _canvas.Zoom),
            FontWeight = style.FontBold ? FontWeight.Bold : FontWeight.Normal,
            FontStyle = style.FontItalic ? FontStyle.Italic : FontStyle.Normal,
            Foreground = new SolidColorBrush(ColorButton.ToAvalonia(style.LineColor.WithAlpha(255))),
            Background = new SolidColorBrush(Color.FromArgb(0xE8, 0xFF, 0xFF, 0xFF)),
            TextAlignment = style.TextHorizontalAlignment switch
            {
                Editor.Fields.TextHorizontalAlignment.Left => TextAlignment.Left,
                Editor.Fields.TextHorizontalAlignment.Right => TextAlignment.Right,
                _ => TextAlignment.Center,
            },
            Width = Math.Max(60, rect.Width * _canvas.Zoom),
            MinHeight = Math.Max(30, rect.Height * _canvas.Zoom),
            Padding = new Thickness(2),
        };
        Canvas.SetLeft(editor, rect.Left * _canvas.Zoom);
        Canvas.SetTop(editor, rect.Top * _canvas.Zoom);
        editor.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape || (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Control)))
            {
                CommitTextEdit();
                _canvas.Focus();
                e.Handled = true;
            }
        };
        editor.LostFocus += (_, _) => CommitTextEdit();
        _textEditor = editor;
        _editedElement = element;
        TextOverlay.Children.Add(editor);
        Dispatcher.UIThread.Post(() =>
        {
            editor.Focus();
            editor.SelectAll();
        }, DispatcherPriority.Input);
    }

    private void CommitTextEdit()
    {
        if (_textEditor is not { } editor || _editedElement is not { } element)
        {
            return;
        }

        _textEditor = null;
        _editedElement = null;
        TextOverlay.Children.Remove(editor);
        _controller.CommitText(element, editor.Text ?? string.Empty);
    }

    #endregion

    #region Zoom

    private void SetZoom(double zoom)
    {
        CommitTextEdit();
        _canvas.Zoom = zoom;
        UpdateStatus(null);
    }

    private void ZoomStep(int direction)
    {
        double current = _canvas.Zoom;
        double next = direction > 0
            ? ZoomLevels.FirstOrDefault(z => z > current + 0.001, ZoomLevels[^1])
            : ZoomLevels.LastOrDefault(z => z < current - 0.001, ZoomLevels[0]);
        SetZoom(next);
    }

    private void ZoomToFit()
    {
        var viewport = Scroller.Viewport;
        if (viewport.Width <= 0 || viewport.Height <= 0)
        {
            return;
        }

        double zoom = Math.Min((viewport.Width - 32) / _surface.Width, (viewport.Height - 32) / _surface.Height);
        SetZoom(Math.Clamp(zoom, 0.05, 1));
    }

    private void OnPointerWheel(object? sender, PointerWheelEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return;
        }

        ZoomStep(e.Delta.Y > 0 ? +1 : -1);
        e.Handled = true;
    }

    #endregion

    #region Commands

    private void ApplyImageEffect(Func<SKBitmap, EffectResult> effect)
    {
        CommitTextEdit();
        _controller.ApplyEffect(effect);
    }

    private void ShrinkCanvas()
    {
        if (_surface.Width > 50 && _surface.Height > 50)
        {
            ApplyImageEffect(image => ImageEffects.ResizeCanvas(image, -25, -25, -25, -25));
        }
    }

    private async Task ResizeAsync()
    {
        var size = await Dialogs.AskSizeAsync(this, _surface.Width, _surface.Height);
        if (size is { } newSize && (newSize.Width != _surface.Width || newSize.Height != _surface.Height))
        {
            ApplyImageEffect(image => ImageEffects.Resize(image, newSize.Width, newSize.Height));
        }
    }

    private void Copy()
    {
        _elementClipboard = _controller.CopySelection() ?? _elementClipboard;
    }

    private void Cut()
    {
        _elementClipboard = _controller.CutSelection() ?? _elementClipboard;
    }

    /// <summary>
    /// Paste the copied elements; without copied elements the image on the clipboard is pasted
    /// </summary>
    private async Task PasteAsync()
    {
        if (_elementClipboard != null)
        {
            _controller.PasteElements(_elementClipboard);
            return;
        }

        await PasteImageAsync();
    }

    private async Task PasteImageAsync()
    {
        var data = await SystemClipboard.GetImageAsync();
        using var bitmap = data != null ? SKBitmap.Decode(data) : null;
        if (bitmap == null)
        {
            UpdateStatus(Texts.Linux("no_clipboard_image"));
            return;
        }

        _controller.PasteImage(bitmap);
    }

    /// <summary>
    /// The image with the elements as capture for a destination
    /// </summary>
    private Capture CreateCapture()
    {
        CommitTextEdit();
        var details = new CaptureDetails { DateTime = _details.DateTime, Title = _details.Title, Mode = _details.Mode };
        return new Capture(_surface.Flatten(), details);
    }

    private async Task<bool> SaveAsync()
    {
        if (_filePath == null)
        {
            var destination = _host.ExportDestinations.FirstOrDefault(d => d.Designation == WellKnownDestinations.FileDefault);
            if (destination == null)
            {
                return await SaveAsAsync();
            }

            var result = await ExportAsync(destination);
            if (result?.FilePath is { } saved)
            {
                _filePath = saved;
                UpdateTitle();
            }

            return result?.Success == true;
        }

        return await SaveToAsync(_filePath);
    }

    private async Task<bool> SaveAsAsync()
    {
        CommitTextEdit();
        var suggested = _filePath ?? FileDestination.GetDefaultPath(_host.Settings, _details);
        var saveDialog = new SaveDialog(_host.FileChooser);
        var path = await saveDialog.AskAsync(suggested, includeGreenshotFormat: true);
        if (path == null)
        {
            return false;
        }

        var success = await SaveToAsync(path);
        if (success)
        {
            _filePath = path;
            UpdateTitle();
        }

        return success;
    }

    /// <summary>
    /// .greenshot keeps the elements editable, other formats are flattened
    /// </summary>
    private async Task<bool> SaveToAsync(string path)
    {
        CommitTextEdit();
        try
        {
            if (Path.GetExtension(path).Equals(GreenshotFile.Extension, StringComparison.OrdinalIgnoreCase))
            {
                GreenshotFile.Save(path, _surface.Image, _surface.SerializeElements());
                Log.Info($"Saved the editor to {path}");
            }
            else
            {
                using var capture = CreateCapture();
                var result = await FileDestination.SaveAsync(_host.Settings, capture, path);
                if (!result.Success)
                {
                    UpdateStatus(result.Message);
                    return false;
                }
            }

            _surface.IsModified = false;
            UpdateTitle();
            UpdateStatus(Texts.Linux("saved_to", path));
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error($"Saving to {path} failed", ex);
            UpdateStatus($"Could not save {path}: {ex.Message}");
            return false;
        }
    }

    private async Task CopyImageAsync()
    {
        var destination = _host.ExportDestinations.FirstOrDefault(d => d.Designation == WellKnownDestinations.Clipboard);
        if (destination != null)
        {
            await ExportAsync(destination);
        }
    }

    private async Task<ExportResult?> ExportAsync(IDestination destination)
    {
        using var capture = CreateCapture();
        var result = await destination.ExportAsync(capture);
        await _host.ReportAsync(destination, result);
        if (result.Success)
        {
            if (result.FilePath != null)
            {
                _surface.IsModified = false;
                UpdateTitle();
            }

            UpdateStatus(result.FilePath != null ? Texts.Linux("saved_to", result.FilePath) : result.Message);
        }
        else if (!result.Cancelled)
        {
            UpdateStatus(result.Message);
        }

        return result;
    }

    private async Task OpenAsync()
    {
        if (_host.FileChooser == null)
        {
            UpdateStatus(Texts.Linux("no_file_dialog"));
            return;
        }

        var path = await _host.FileChooser.OpenFileAsync(Texts.Linux("open_image"), SaveDialog.OpenFilters);
        if (path != null)
        {
            await _host.OpenFileAsync(path);
        }
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        CommitTextEdit();
        if (_closeConfirmed || !_surface.IsModified || _host.EditorSettings.SuppressSaveDialogAtClose)
        {
            return;
        }

        e.Cancel = true;
        var choice = await Dialogs.AskSaveChangesAsync(this);
        if (choice == SaveChoice.Cancel || (choice == SaveChoice.Save && !await SaveAsync()))
        {
            return;
        }

        _closeConfirmed = true;
        Close();
    }

    #endregion

    #region Menu handlers

    private void OnSave(object? sender, RoutedEventArgs e) => _ = SaveAsync();

    private void OnSaveAs(object? sender, RoutedEventArgs e) => _ = SaveAsAsync();

    private void OnCopyImage(object? sender, RoutedEventArgs e) => _ = CopyImageAsync();

    private void OnOpen(object? sender, RoutedEventArgs e) => _ = OpenAsync();

    private void OnClose(object? sender, RoutedEventArgs e) => Close();

    private void OnUndo(object? sender, RoutedEventArgs e) => _surface.Undo();

    private void OnRedo(object? sender, RoutedEventArgs e) => _surface.Redo();

    private void OnCut(object? sender, RoutedEventArgs e) => Cut();

    private void OnCopy(object? sender, RoutedEventArgs e) => Copy();

    private void OnPaste(object? sender, RoutedEventArgs e) => _ = PasteAsync();

    private void OnPasteImage(object? sender, RoutedEventArgs e) => _ = PasteImageAsync();

    private void OnDuplicate(object? sender, RoutedEventArgs e) => _controller.Duplicate();

    private void OnDelete(object? sender, RoutedEventArgs e) => _controller.DeleteSelection();

    private void OnSelectAll(object? sender, RoutedEventArgs e) => _controller.SelectAll();

    private void OnBringToFront(object? sender, RoutedEventArgs e) => _surface.Arrange(EditorSurface.ZOrder.Top);

    private void OnBringForward(object? sender, RoutedEventArgs e) => _surface.Arrange(EditorSurface.ZOrder.Up);

    private void OnSendBackward(object? sender, RoutedEventArgs e) => _surface.Arrange(EditorSurface.ZOrder.Down);

    private void OnSendToBack(object? sender, RoutedEventArgs e) => _surface.Arrange(EditorSurface.ZOrder.Bottom);

    private async void OnCounterStart(object? sender, RoutedEventArgs e)
    {
        var start = await Dialogs.AskNumberAsync(this, Texts.Label(Texts.Editor("counter")), Texts.Linux("counter_start_question"), _surface.CounterStart, 0, 9999);
        if (start is { } value && value != _surface.CounterStart)
        {
            _surface.Checkpoint();
            _surface.CounterStart = value;
            _surface.Invalidate();
        }
    }

    private void OnRedactText(object? sender, RoutedEventArgs e)
    {
        CommitTextEdit();
        _ = EditorToolDialogs.RedactTextAsync(this, _controller, _host.Settings);
    }

    private void OnInsertQrCode(object? sender, RoutedEventArgs e) => _ = EditorToolDialogs.InsertQrCodeAsync(this, _controller);

    private void OnCropTool(object? sender, RoutedEventArgs e)
    {
        _controller.Tool = EditorTool.Crop;
        UpdateToolButtons();
    }

    private void OnAutoCrop(object? sender, RoutedEventArgs e)
    {
        if (!_controller.AutoCrop())
        {
            UpdateStatus(Texts.Editor("autocrop_not_possible"));
        }
    }

    private void OnResize(object? sender, RoutedEventArgs e) => _ = ResizeAsync();

    private void OnRotateClockwise(object? sender, RoutedEventArgs e) => ApplyImageEffect(ImageEffects.RotateClockwise);

    private void OnRotateCounterClockwise(object? sender, RoutedEventArgs e) => ApplyImageEffect(ImageEffects.RotateCounterClockwise);

    private void OnEnlargeCanvas(object? sender, RoutedEventArgs e) => ApplyImageEffect(image => ImageEffects.ResizeCanvas(image, 25, 25, 25, 25));

    private void OnShrinkCanvas(object? sender, RoutedEventArgs e) => ShrinkCanvas();

    private void OnBorder(object? sender, RoutedEventArgs e) => ApplyImageEffect(image => ImageEffects.Border(image));

    private void OnDropShadow(object? sender, RoutedEventArgs e) => ApplyImageEffect(image => ImageEffects.DropShadow(image));

    private void OnTornEdge(object? sender, RoutedEventArgs e) => ApplyImageEffect(image => ImageEffects.TornEdge(image));

    private void OnGrayscale(object? sender, RoutedEventArgs e) => ApplyImageEffect(ImageEffects.Grayscale);

    private void OnInvert(object? sender, RoutedEventArgs e) => ApplyImageEffect(ImageEffects.Invert);

    private void OnClear(object? sender, RoutedEventArgs e) => ApplyImageEffect(ImageEffects.Clear);

    private void OnZoomIn(object? sender, RoutedEventArgs e) => ZoomStep(+1);

    private void OnZoomOut(object? sender, RoutedEventArgs e) => ZoomStep(-1);

    private void OnZoomActual(object? sender, RoutedEventArgs e) => SetZoom(1);

    private void OnZoomFit(object? sender, RoutedEventArgs e) => ZoomToFit();

    #endregion
}
