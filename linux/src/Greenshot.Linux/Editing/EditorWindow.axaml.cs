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
using Avalonia.Automation;
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
    private readonly List<EditorTab> _tabs = new();
    private readonly Lazy<EmojiPicker> _emojiPicker = new(() => new EmojiPicker());
    private readonly Dictionary<EditorTool, ToggleButton> _toolButtons = new();
    private EditorTab _tab = null!;
    private TextBox? _textEditor;
    private DrawableElement? _editedElement;
    private bool _closeConfirmed;

    // For the XAML previewer only
    public EditorWindow()
    {
        InitializeComponent();
        _host = null!;
    }

    internal EditorWindow(IEditorHost host, EditorSurface surface, CaptureDetails details, string? filePath)
        : this(host, new EditorTab(host, surface, details, filePath))
    {
    }

    /// <summary>
    /// A window for a tab, also one which comes from another window
    /// </summary>
    internal EditorWindow(IEditorHost host, EditorTab tab)
    {
        InitializeComponent();
        _host = host;
        Icon = AppIcons.Window;
        CreateToolButtons();
        CreateExportMenu();
        CreateRecipesMenu();

        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
        Scroller.AddHandler(PointerWheelChangedEvent, OnPointerWheel, RoutingStrategies.Tunnel);
        Opened += (_, _) => FitWindowToImage();
        Closing += OnClosing;
        Activated += (_, _) => LastActive = this;
        Closed += (_, _) =>
        {
            if (LastActive == this)
            {
                LastActive = null;
            }

            foreach (var open in _tabs)
            {
                open.Dispose();
            }
        };

        AddTab(tab);
    }

    /// <summary>
    /// The editor window used last, new captures open as a tab in it (OpenCaptureAsTab)
    /// </summary>
    internal static EditorWindow? LastActive { get; private set; }

    /// <summary>
    /// The tab which is shown
    /// </summary>
    internal EditorTab Tab => _tab;

    internal IReadOnlyList<EditorTab> Tabs => _tabs;

    #region Tabs

    /// <summary>
    /// Add an image as tab and show it
    /// </summary>
    internal void AddTab(EditorTab tab)
    {
        tab.Window = this;
        _tabs.Add(tab);
        tab.Surface.Changed += () => OnTabChanged(tab);
        tab.Controller.StateChanged += () =>
        {
            if (tab == _tab)
            {
                UpdateToolButtons();
            }
        };
        tab.Controller.TextEditRequested += element =>
        {
            if (tab == _tab)
            {
                BeginTextEdit(element);
            }
        };
        tab.Controller.EmojiPickRequested += element => _emojiPicker.Value.Show(tab.Canvas, emoji => tab.Controller.SetEmoji(element, emoji));
        tab.PropertyBar.CutSettingsRequested += cut => _ = EditCutSettingsAsync(cut);
        tab.PropertyBar.FocusReturned += () => tab.Canvas.Focus();
        SelectTab(tab);
        if (IsVisible)
        {
            // A tab added to an open window: large images are zoomed out, the window keeps its size
            Dispatcher.UIThread.Post(() => ZoomToFitIfLarger(), DispatcherPriority.Loaded);
        }
    }

    internal void SelectTab(EditorTab tab)
    {
        if (_tab == tab)
        {
            UpdateTabStrip();
            return;
        }

        CommitTextEdit();
        if (_tab != null)
        {
            _tab.ScrollOffset = Scroller.Offset;
            CanvasHost.Children.Remove(_tab.Canvas);
        }

        _tab = tab;
        CanvasHost.Children.Insert(0, tab.Canvas);
        PropertyBarHost.Content = tab.PropertyBar;
        tab.PropertyBar.Update();
        Dispatcher.UIThread.Post(() =>
        {
            Scroller.Offset = tab.ScrollOffset;
            tab.Canvas.Focus();
        }, DispatcherPriority.Loaded);
        UpdateTitle();
        UpdateToolButtons();
        UpdateStatus(null);
        UpdateTabStrip();
    }

    private void OnTabChanged(EditorTab tab)
    {
        if (tab == _tab)
        {
            OnSurfaceChanged();
        }

        UpdateTabStrip();
    }

    /// <summary>
    /// Close a tab, asking to save it when it has changes; the window closes with its last tab
    /// </summary>
    internal async Task<bool> CloseTabAsync(EditorTab tab)
    {
        if (!await ConfirmCloseAsync(tab))
        {
            return false;
        }

        if (_tabs.Count == 1)
        {
            _closeConfirmed = true;
            Close();
            return true;
        }

        RemoveTab(tab);
        tab.Dispose();
        return true;
    }

    /// <summary>
    /// Take the tab out of this window, the next tab is shown
    /// </summary>
    private void RemoveTab(EditorTab tab)
    {
        int index = _tabs.IndexOf(tab);
        if (tab == _tab)
        {
            SelectTab(_tabs[index + 1 < _tabs.Count ? index + 1 : index - 1]);
        }

        _tabs.Remove(tab);
        tab.Window = null;
        UpdateTabStrip();
    }

    /// <summary>
    /// The tab gets a window of its own
    /// </summary>
    private void MoveToNewWindow(EditorTab tab)
    {
        if (_tabs.Count < 2)
        {
            return;
        }

        RemoveTab(tab);
        var window = new EditorWindow(_host, tab);
        window.Show();
        window.Activate();
    }

    private void MoveTab(EditorTab tab, int delta)
    {
        int index = _tabs.IndexOf(tab);
        int target = index + delta;
        if (target < 0 || target >= _tabs.Count)
        {
            return;
        }

        (_tabs[index], _tabs[target]) = (_tabs[target], _tabs[index]);
        UpdateTabStrip();
    }

    private void SelectNextTab(int delta)
    {
        if (_tabs.Count > 1)
        {
            SelectTab(_tabs[(_tabs.IndexOf(_tab) + delta + _tabs.Count) % _tabs.Count]);
        }
    }

    /// <summary>
    /// The "+" after the tabs: a new empty image in the size and color of the settings
    /// </summary>
    private void NewImage()
    {
        var settings = _host.EditorSettings;
        var size = settings.DefaultEditorSize;
        var image = ImageEffects.NewBitmap(size.Width, size.Height);
        image.Erase(settings.NewImageColor);
        var details = new CaptureDetails { Title = Texts.Editor("new_image"), DateTime = DateTime.Now };
        AddTab(new EditorTab(_host, new EditorSurface(image), details, null));
    }

    /// <summary>
    /// The tabs, like the tab strip of Windows: a click selects, the cross or the middle button closes, the context menu
    /// moves a tab. Shown when the editor uses tabs or when there are more than one.
    /// </summary>
    private void UpdateTabStrip()
    {
        TabStripHost.IsVisible = _host.EditorSettings.EditorTabs || _tabs.Count > 1;
        if (!TabStripHost.IsVisible)
        {
            return;
        }

        TabStrip.Children.Clear();
        AutomationProperties.SetName(TabStrip, Texts.Editor("tab_list"));
        foreach (var tab in _tabs)
        {
            string title = (tab.Surface.IsModified ? "*" : string.Empty) + tab.Title;
            var text = new TextBlock { Text = title, MaxWidth = 220, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            var close = new Button
            {
                Content = "✕",
                Padding = new Thickness(4, 0),
                MinHeight = 0,
                Background = Brushes.Transparent,
                Focusable = false,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            };
            ToolTip.SetTip(close, Texts.Editor("close_tab"));
            AutomationProperties.SetName(close, Texts.Editor("close_tab"));
            close.Click += (_, _) => _ = CloseTabAsync(tab);
            var button = new ToggleButton
            {
                Content = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 6, Children = { text, close } },
                IsChecked = tab == _tab,
                Padding = new Thickness(8, 2, 2, 2),
                Focusable = false,
            };
            ToolTip.SetTip(button, tab.Description);
            AutomationProperties.SetName(button, tab.Surface.IsModified ? Texts.Editor("tab_modified", tab.Title) : tab.Title);
            button.Click += (_, _) => SelectTab(tab);
            button.PointerReleased += (_, e) =>
            {
                if (e.InitialPressMouseButton == MouseButton.Middle)
                {
                    _ = CloseTabAsync(tab);
                    e.Handled = true;
                }
            };
            button.ContextMenu = TabMenu(tab);
            TabStrip.Children.Add(button);
        }

        var add = new Button { Content = "+", Padding = new Thickness(10, 2), Focusable = false };
        ToolTip.SetTip(add, Texts.Editor("new_image"));
        AutomationProperties.SetName(add, Texts.Editor("new_image"));
        add.Click += (_, _) => NewImage();
        TabStrip.Children.Add(add);
    }

    private ContextMenu TabMenu(EditorTab tab)
    {
        MenuItem Item(string text, Action action, bool enabled = true)
        {
            var item = new MenuItem { Header = Texts.Label(text), IsEnabled = enabled };
            item.Click += (_, _) => action();
            return item;
        }

        int index = _tabs.IndexOf(tab);
        return new ContextMenu
        {
            ItemsSource = new Control[]
            {
                Item(Texts.Editor("close_tab"), () => _ = CloseTabAsync(tab)),
                Item(Texts.Editor("move_tab_left"), () => MoveTab(tab, -1), index > 0),
                Item(Texts.Editor("move_tab_right"), () => MoveTab(tab, +1), index < _tabs.Count - 1),
                Item(Texts.Editor("move_tab_to_new_window"), () => MoveToNewWindow(tab), _tabs.Count > 1),
            },
        };
    }

    #endregion

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
        AddTool(EditorTool.Emoji, new TextBlock { Text = EmojiElement.DefaultEmoji, FontFamily = EmojiPicker.EmojiFont, FontSize = 15, Width = 16, Height = 18 },
            Texts.Editor("element_emoji") + " (M)");
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

    private void AddTool(EditorTool tool, string icon, string tooltip) => AddTool(tool, IconImage(icon), tooltip);

    private void AddTool(EditorTool tool, Control content, string tooltip)
    {
        var button = new ToggleButton { Content = content, Padding = new Thickness(6), Focusable = false };
        ToolTip.SetTip(button, tooltip);
        button.Click += (_, _) =>
        {
            CommitTextEdit();
            _tab.Controller.Tool = tool;
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

    /// <summary>
    /// The recipes of Editor triggers, in their groups like the Recipes menu of the Windows editor
    /// </summary>
    private void CreateRecipesMenu()
    {
        var triggers = _host.EditorRecipeTriggers;
        foreach (var group in triggers.GroupBy(t => t.Group ?? "Recipes")
                     .OrderBy(g => g.Key == "Recipes" ? 0 : 1).ThenBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase))
        {
            var target = RecipesMenu;
            if (group.Key != "Recipes")
            {
                target = new MenuItem { Header = group.Key };
                RecipesMenu.Items.Add(target);
            }

            foreach (var trigger in group.OrderBy(t => t.Order).ThenBy(t => t.MenuItemText, StringComparer.CurrentCultureIgnoreCase))
            {
                var item = new MenuItem { Header = trigger.MenuItemText };
                item.Click += (_, _) =>
                {
                    CommitTextEdit();
                    trigger.Fire(_tab);
                };
                target.Items.Add(item);
            }
        }

        // The recipe manager and the recipe editor, like the Recipe Editor plugin adds them on Windows
        if (triggers.Count > 0)
        {
            RecipesMenu.Items.Add(new Separator());
        }

        var manager = new MenuItem { Header = Texts.WithoutAccessKey(Texts.Core("contextmenu_managerecipes")) };
        manager.Click += (_, _) => _host.RecipeEditor.OpenRecipeManager();
        RecipesMenu.Items.Add(manager);
        var editor = new MenuItem { Header = Texts.WithoutAccessKey(Texts.Core("contextmenu_recipeeditor")) };
        editor.Click += (_, _) => _host.RecipeEditor.OpenEditor();
        RecipesMenu.Items.Add(editor);
    }

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
        // While the window is built there is no tab yet
        if (_tab is null)
        {
            return;
        }

        foreach (var (tool, button) in _toolButtons)
        {
            button.IsChecked = tool == _tab.Controller.Tool;
        }

        UndoMenu.IsEnabled = _tab.Surface.CanUndo;
        RedoMenu.IsEnabled = _tab.Surface.CanRedo;
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
            Width = Math.Clamp(_tab.Surface.Width + chromeWidth, MinWidth, available.Width * 0.95);
            Height = Math.Clamp(_tab.Surface.Height + chromeHeight, MinHeight, available.Height * 0.95);
        }

        Dispatcher.UIThread.Post(() =>
        {
            ZoomToFitIfLarger();
            _tab.Canvas.Focus();
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
        Title = $"{(_tab.Surface.IsModified ? "*" : string.Empty)}{_tab.Title} - {Texts.Editor("title")}";
        UpdateTabStrip();
    }

    private void UpdateStatus(string? message)
    {
        if (message != null)
        {
            _tab.StatusText = message;
        }

        StatusText.Text = _tab.StatusText ?? string.Empty;

        SizeText.Text = string.Create(CultureInfo.CurrentCulture, $"{_tab.Surface.Width} × {_tab.Surface.Height}");
        ZoomText.Text = string.Create(CultureInfo.CurrentCulture, $"{_tab.Canvas.Zoom * 100:0}%");
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
                case Key.Tab:
                    SelectNextTab(-1);
                    return true;
            }

            return false;
        }

        if (control)
        {
            switch (key)
            {
                case Key.Z:
                    _tab.Surface.Undo();
                    return true;
                case Key.Y:
                    _tab.Surface.Redo();
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
                    _tab.Controller.Duplicate();
                    return true;
                case Key.A:
                    _tab.Controller.SelectAll();
                    return true;
                case Key.S:
                    _ = SaveAsync();
                    return true;
                case Key.O:
                    _ = OpenAsync();
                    return true;
                case Key.W:
                    // The tab, the window closes with its last tab
                    _ = CloseTabAsync(_tab);
                    return true;
                case Key.Tab or Key.PageDown:
                    SelectNextTab(+1);
                    return true;
                case Key.PageUp:
                    SelectNextTab(-1);
                    return true;
                case Key.Q:
                    CommitTextEdit();
                    _tab.Controller.DropShadow();
                    return true;
                case Key.B:
                    ApplyImageEffect(image => ImageEffects.Border(image));
                    return true;
                case Key.T:
                    CommitTextEdit();
                    _tab.Controller.TornEdges();
                    UpdateToolButtons();
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
                _tab.Controller.Escape();
                return true;
            case Key.Enter when _tab.Controller.Tool == EditorTool.Crop:
                _tab.Controller.ConfirmCrop();
                return true;
            case Key.Enter when _tab.Surface.Selection is [{ HasText: true } element]:
                BeginTextEdit(element);
                return true;
            case Key.Delete or Key.Back:
                _tab.Controller.DeleteSelection();
                return true;
            case Key.Left or Key.Right or Key.Up or Key.Down:
                return Move(key, shift ? 10 : 1);
            case Key.PageUp:
                _tab.Surface.Arrange(EditorSurface.ZOrder.Up);
                return true;
            case Key.PageDown:
                _tab.Surface.Arrange(EditorSurface.ZOrder.Down);
                return true;
            case Key.Home:
                _tab.Surface.Arrange(EditorSurface.ZOrder.Top);
                return true;
            case Key.End:
                _tab.Surface.Arrange(EditorSurface.ZOrder.Bottom);
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
            Key.M => EditorTool.Emoji,
            _ => null,
        };
        if (tool == EditorTool.Crop && _tab.Controller.Tool == EditorTool.Crop)
        {
            // C again: the next crop mode, like on Windows
            _tab.Controller.CycleCropMode();
            return true;
        }

        if (tool != null)
        {
            _tab.Controller.Tool = tool.Value;
            UpdateToolButtons();
            return true;
        }

        return false;
    }

    private bool Move(Key key, int step) => key switch
    {
        Key.Left => _tab.Controller.MoveSelection(-step, 0),
        Key.Right => _tab.Controller.MoveSelection(step, 0),
        Key.Up => _tab.Controller.MoveSelection(0, -step),
        _ => _tab.Controller.MoveSelection(0, step),
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
            FontSize = Math.Max(6, style.FontSize * _tab.Canvas.Zoom),
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
            Width = Math.Max(60, rect.Width * _tab.Canvas.Zoom),
            MinHeight = Math.Max(30, rect.Height * _tab.Canvas.Zoom),
            Padding = new Thickness(2),
        };
        Canvas.SetLeft(editor, rect.Left * _tab.Canvas.Zoom);
        Canvas.SetTop(editor, rect.Top * _tab.Canvas.Zoom);
        editor.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape || (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Control)))
            {
                CommitTextEdit();
                _tab.Canvas.Focus();
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
        _tab.Controller.CommitText(element, editor.Text ?? string.Empty);
    }

    #endregion

    #region Zoom

    private void SetZoom(double zoom)
    {
        CommitTextEdit();
        _tab.Canvas.Zoom = zoom;
        UpdateStatus(null);
    }

    private void ZoomStep(int direction)
    {
        double current = _tab.Canvas.Zoom;
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

        double zoom = Math.Min((viewport.Width - 32) / _tab.Surface.Width, (viewport.Height - 32) / _tab.Surface.Height);
        SetZoom(Math.Clamp(zoom, 0.05, 1));
    }

    /// <summary>
    /// Zoom out when the image doesn't fit
    /// </summary>
    private void ZoomToFitIfLarger()
    {
        var viewport = Scroller.Viewport;
        if (_tab.Surface.Width > viewport.Width - 32 || _tab.Surface.Height > viewport.Height - 32)
        {
            ZoomToFit();
        }
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
        _tab.Controller.ApplyEffect(effect);
    }

    private void ShrinkCanvas()
    {
        if (_tab.Surface.Width > 50 && _tab.Surface.Height > 50)
        {
            ApplyImageEffect(image => ImageEffects.ResizeCanvas(image, -25, -25, -25, -25));
        }
    }

    private async Task ResizeAsync()
    {
        var size = await Dialogs.AskSizeAsync(this, _tab.Surface.Width, _tab.Surface.Height);
        if (size is { } newSize && (newSize.Width != _tab.Surface.Width || newSize.Height != _tab.Surface.Height))
        {
            ApplyImageEffect(image => ImageEffects.Resize(image, newSize.Width, newSize.Height));
        }
    }

    private void Copy()
    {
        _elementClipboard = _tab.Controller.CopySelection() ?? _elementClipboard;
    }

    private void Cut()
    {
        _elementClipboard = _tab.Controller.CutSelection() ?? _elementClipboard;
    }

    /// <summary>
    /// Paste the copied elements; without copied elements the image on the clipboard is pasted
    /// </summary>
    private async Task PasteAsync()
    {
        if (_elementClipboard != null)
        {
            _tab.Controller.PasteElements(_elementClipboard);
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

        _tab.Controller.PasteImage(bitmap);
    }

    /// <summary>
    /// The image with the elements as capture for a destination
    /// </summary>
    private Capture CreateCapture()
    {
        CommitTextEdit();
        var details = new CaptureDetails { DateTime = _tab.CaptureDetails.DateTime, Title = _tab.CaptureDetails.Title, Mode = _tab.CaptureDetails.Mode };
        return new Capture(_tab.Surface.Flatten(), details);
    }

    private async Task<bool> SaveAsync()
    {
        if (_tab.FilePath == null)
        {
            var destination = _host.ExportDestinations.FirstOrDefault(d => d.Designation == WellKnownDestinations.FileDefault);
            if (destination == null)
            {
                return await SaveAsAsync();
            }

            var result = await ExportAsync(destination);
            if (result?.FilePath is { } saved)
            {
                _tab.FilePath = saved;
                UpdateTitle();
            }

            return result?.Success == true;
        }

        return await SaveToAsync(_tab.FilePath);
    }

    private async Task<bool> SaveAsAsync()
    {
        CommitTextEdit();
        var suggested = _tab.FilePath ?? FileDestination.GetDefaultPath(_host.Settings, _tab.CaptureDetails);
        var saveDialog = new SaveDialog(_host.FileChooser);
        var path = await saveDialog.AskAsync(suggested, includeGreenshotFormat: true);
        if (path == null)
        {
            return false;
        }

        var success = await SaveToAsync(path);
        if (success)
        {
            _tab.FilePath = path;
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
                GreenshotFile.Save(path, _tab.Surface.Image, _tab.Surface.SerializeElements());
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

            _tab.Surface.IsModified = false;
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
                _tab.Surface.IsModified = false;
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
        if (_closeConfirmed || _host.EditorSettings.SuppressSaveDialogAtClose || !_tabs.Any(t => t.Surface.IsModified))
        {
            return;
        }

        // Every image with changes is asked about, like closing the tabs one by one
        e.Cancel = true;
        foreach (var tab in _tabs.Where(t => t.Surface.IsModified).ToList())
        {
            if (!await ConfirmCloseAsync(tab))
            {
                return;
            }
        }

        _closeConfirmed = true;
        Close();
    }

    /// <summary>
    /// May the tab be closed? With changes the user is asked whether to save them first (it is shown for the question)
    /// </summary>
    private async Task<bool> ConfirmCloseAsync(EditorTab tab)
    {
        CommitTextEdit();
        if (!tab.Surface.IsModified || _host.EditorSettings.SuppressSaveDialogAtClose)
        {
            return true;
        }

        SelectTab(tab);
        var choice = await Dialogs.AskSaveChangesAsync(this);
        return choice == SaveChoice.Discard || (choice == SaveChoice.Save && await SaveAsync());
    }

    #endregion

    #region Menu handlers

    private void OnSave(object? sender, RoutedEventArgs e) => _ = SaveAsync();

    private void OnSaveAs(object? sender, RoutedEventArgs e) => _ = SaveAsAsync();

    private void OnCopyImage(object? sender, RoutedEventArgs e) => _ = CopyImageAsync();

    private void OnOpen(object? sender, RoutedEventArgs e) => _ = OpenAsync();

    private void OnClose(object? sender, RoutedEventArgs e) => _ = CloseTabAsync(_tab);

    private void OnUndo(object? sender, RoutedEventArgs e) => _tab.Surface.Undo();

    private void OnRedo(object? sender, RoutedEventArgs e) => _tab.Surface.Redo();

    private void OnCut(object? sender, RoutedEventArgs e) => Cut();

    private void OnCopy(object? sender, RoutedEventArgs e) => Copy();

    private void OnPaste(object? sender, RoutedEventArgs e) => _ = PasteAsync();

    private void OnPasteImage(object? sender, RoutedEventArgs e) => _ = PasteImageAsync();

    private void OnDuplicate(object? sender, RoutedEventArgs e) => _tab.Controller.Duplicate();

    private void OnDelete(object? sender, RoutedEventArgs e) => _tab.Controller.DeleteSelection();

    private void OnSelectAll(object? sender, RoutedEventArgs e) => _tab.Controller.SelectAll();

    private void OnBringToFront(object? sender, RoutedEventArgs e) => _tab.Surface.Arrange(EditorSurface.ZOrder.Top);

    private void OnBringForward(object? sender, RoutedEventArgs e) => _tab.Surface.Arrange(EditorSurface.ZOrder.Up);

    private void OnSendBackward(object? sender, RoutedEventArgs e) => _tab.Surface.Arrange(EditorSurface.ZOrder.Down);

    private void OnSendToBack(object? sender, RoutedEventArgs e) => _tab.Surface.Arrange(EditorSurface.ZOrder.Bottom);

    private async void OnCounterStart(object? sender, RoutedEventArgs e)
    {
        var start = await Dialogs.AskNumberAsync(this, Texts.Label(Texts.Editor("counter")), Texts.Linux("counter_start_question"), _tab.Surface.CounterStart, 0, 9999);
        if (start is { } value && value != _tab.Surface.CounterStart)
        {
            _tab.Surface.Checkpoint();
            _tab.Surface.CounterStart = value;
            _tab.Surface.Invalidate();
        }
    }

    private void OnRedactText(object? sender, RoutedEventArgs e)
    {
        CommitTextEdit();
        _ = EditorToolDialogs.RedactTextAsync(this, _tab.Controller, _host.Settings);
    }

    private void OnInsertQrCode(object? sender, RoutedEventArgs e) => _ = EditorToolDialogs.InsertQrCodeAsync(this, _tab.Controller);

    private void OnCropTool(object? sender, RoutedEventArgs e)
    {
        _tab.Controller.Tool = EditorTool.Crop;
        UpdateToolButtons();
    }

    private void OnAutoCrop(object? sender, RoutedEventArgs e)
    {
        if (!_tab.Controller.AutoCrop())
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

    private void OnDropShadow(object? sender, RoutedEventArgs e)
    {
        CommitTextEdit();
        _tab.Controller.DropShadow();
    }

    private void OnTornEdge(object? sender, RoutedEventArgs e)
    {
        CommitTextEdit();
        _tab.Controller.TornEdges();
        UpdateToolButtons();
    }

    /// <summary>
    /// The settings of the drop shadow, stored and applied with OK (the right click of Windows)
    /// </summary>
    private async void OnDropShadowSettings(object? sender, RoutedEventArgs e)
    {
        var settings = _host.EditorSettings.DropShadowEffectSettings;
        if (await EffectSettingsDialogs.EditDropShadowAsync(this, settings))
        {
            _host.EditorSettings.DropShadowEffectSettings = settings;
            _host.EditorSettings.Save();
            OnDropShadow(sender, e);
        }
    }

    /// <summary>
    /// The settings of the torn edges, stored and applied with OK; with torn edges selected their settings are changed
    /// </summary>
    private async void OnTornEdgeSettings(object? sender, RoutedEventArgs e)
    {
        if (_tab.Surface.Selection.OfType<TornEdgeElement>().FirstOrDefault() is { } selected)
        {
            await EditCutSettingsAsync(selected);
            return;
        }

        var settings = _host.EditorSettings.TornEdgeEffectSettings;
        if (await EffectSettingsDialogs.EditTornEdgeAsync(this, settings, showEdges: true, () => _host.EditorSettings.RecentColors))
        {
            _host.EditorSettings.TornEdgeEffectSettings = settings;
            _host.EditorSettings.Save();
            OnTornEdge(sender, e);
        }
    }

    /// <summary>
    /// The settings of selected cut edges or torn edges
    /// </summary>
    private async Task EditCutSettingsAsync(CutElement cut)
    {
        var settings = cut.GetSettings();
        if (await EffectSettingsDialogs.EditTornEdgeAsync(this, settings, showEdges: cut is TornEdgeElement, () => _host.EditorSettings.RecentColors))
        {
            _tab.Controller.ApplyCutSettings(cut, settings);
        }
    }

    private void OnGrayscale(object? sender, RoutedEventArgs e) => ApplyImageEffect(ImageEffects.Grayscale);

    private void OnInvert(object? sender, RoutedEventArgs e) => ApplyImageEffect(ImageEffects.Invert);

    private void OnClear(object? sender, RoutedEventArgs e) => ApplyImageEffect(ImageEffects.Clear);

    private void OnZoomIn(object? sender, RoutedEventArgs e) => ZoomStep(+1);

    private void OnZoomOut(object? sender, RoutedEventArgs e) => ZoomStep(-1);

    private void OnZoomActual(object? sender, RoutedEventArgs e) => SetZoom(1);

    private void OnZoomFit(object? sender, RoutedEventArgs e) => ZoomToFit();

    #endregion
}
