/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shell;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// The title bar of Greenshot's WPF windows, the same in every window: icon, title, optional buttons (the content) and the caption buttons.
    /// It decides which title bar the window gets: with the "Same as Windows" theme (and with high contrast) the window keeps the title bar
    /// of Windows and this one hides, with the light or dark theme the window extends its client area over the frame (WindowChrome) and
    /// this one is drawn in the theme colors. It follows changes of the theme while the window is open.
    /// Put it in the first row of the window's root panel:
    /// <code>&lt;wpf:ThemedTitleBar ShowThemeToggle="True"&gt;&lt;Button .../&gt;&lt;/wpf:ThemedTitleBar&gt;</code>
    /// The title and icon are those of the window, unless <see cref="Title"/> or <see cref="Icon"/> are set.
    /// </summary>
    [ContentProperty(nameof(Content))]
    public class ThemedTitleBar : Border
    {
        /// <summary>
        /// The height of the title bar, the same for all windows
        /// </summary>
        public const double BarHeight = 36;

        private const string CaptionFont = "Segoe Fluent Icons, Segoe MDL2 Assets";
        private const string MinimizeGlyph = "";
        private const string MaximizeGlyph = "";
        private const string RestoreGlyph = "";
        private const string CloseGlyph = "";

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(ThemedTitleBar),
            new PropertyMetadata(null, (d, e) => ((ThemedTitleBar)d).UpdateTitle()));

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(ImageSource), typeof(ThemedTitleBar),
            new PropertyMetadata(null, (d, e) => ((ThemedTitleBar)d).UpdateTitle()));

        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(nameof(Content), typeof(object), typeof(ThemedTitleBar),
            new PropertyMetadata(null, (d, e) => ((ThemedTitleBar)d).OnContentChanged()));

        public static readonly DependencyProperty ShowThemeToggleProperty = DependencyProperty.Register(nameof(ShowThemeToggle), typeof(bool), typeof(ThemedTitleBar),
            new PropertyMetadata(false, (d, e) => ((ThemedTitleBar)d).UpdateButtons()));

        public static readonly DependencyProperty ShowMinimizeProperty = DependencyProperty.Register(nameof(ShowMinimize), typeof(bool?), typeof(ThemedTitleBar),
            new PropertyMetadata(null, (d, e) => ((ThemedTitleBar)d).UpdateButtons()));

        public static readonly DependencyProperty ShowMaximizeProperty = DependencyProperty.Register(nameof(ShowMaximize), typeof(bool?), typeof(ThemedTitleBar),
            new PropertyMetadata(null, (d, e) => ((ThemedTitleBar)d).UpdateButtons()));

        public static readonly DependencyProperty ShowCloseProperty = DependencyProperty.Register(nameof(ShowClose), typeof(bool), typeof(ThemedTitleBar),
            new PropertyMetadata(true, (d, e) => ((ThemedTitleBar)d).UpdateButtons()));

        private readonly Grid _captionPart;
        private readonly StackPanel _captionButtons;
        private readonly Image _iconImage;
        private readonly TextBlock _titleText;
        private readonly ContentPresenter _contentPresenter;
        private readonly Button _themeToggleButton;
        private readonly Button _minimizeButton;
        private readonly Button _maximizeButton;
        private readonly Button _closeButton;
        // Without an icon of its own, Windows shows the icon of the exe in the title bar: this one does the same
        private static readonly Lazy<ImageSource> ApplicationIcon = new Lazy<ImageSource>(LoadApplicationIcon);
        private Window _window;
        private WindowStyle _customWindowStyle;

        public ThemedTitleBar()
        {
            MinHeight = BarHeight;
            BorderThickness = new Thickness(0, 0, 0, 1);
            Bind(BackgroundProperty, nameof(ThemeManager.TitleBarBrush));
            Bind(BorderBrushProperty, nameof(ThemeManager.BorderBrush));
            Bind(TextElement.ForegroundProperty, nameof(ThemeManager.ForegroundBrush));

            var root = new Grid();
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Icon and title, dragging them moves the window (the caption area of WindowChrome)
            var titlePanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 8, 0) };
            _iconImage = new Image { Width = 16, Height = 16, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
            RenderOptions.SetBitmapScalingMode(_iconImage, BitmapScalingMode.HighQuality);
            _titleText = new TextBlock { FontSize = 12, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
            titlePanel.Children.Add(_iconImage);
            titlePanel.Children.Add(_titleText);
            _captionPart = new Grid();
            _captionPart.Children.Add(titlePanel);
            root.Children.Add(_captionPart);

            // The window's own buttons, they stay visible (in a small bar) with the title bar of Windows
            _contentPresenter = new ContentPresenter { VerticalAlignment = VerticalAlignment.Center };
            WindowChrome.SetIsHitTestVisibleInChrome(_contentPresenter, true);
            Grid.SetColumn(_contentPresenter, 1);
            root.Children.Add(_contentPresenter);

            var buttons = _captionButtons = new StackPanel { Orientation = Orientation.Horizontal };
            WindowChrome.SetIsHitTestVisibleInChrome(buttons, true);
            Grid.SetColumn(buttons, 2);
            _themeToggleButton = CreateCaptionButton(null, false);
            _themeToggleButton.FontFamily = SystemFonts.MessageFontFamily;
            _themeToggleButton.FontSize = 13;
            _themeToggleButton.Click += (s, e) => ThemeManager.Instance.ToggleTheme();
            _minimizeButton = CreateCaptionButton(MinimizeGlyph, false);
            _minimizeButton.Click += (s, e) => SystemCommands.MinimizeWindow(_window);
            _maximizeButton = CreateCaptionButton(MaximizeGlyph, false);
            _maximizeButton.Click += (s, e) =>
            {
                if (_window.WindowState == WindowState.Maximized)
                {
                    SystemCommands.RestoreWindow(_window);
                }
                else
                {
                    SystemCommands.MaximizeWindow(_window);
                }
            };
            _closeButton = CreateCaptionButton(CloseGlyph, true);
            _closeButton.Click += (s, e) => _window?.Close();
            buttons.Children.Add(_themeToggleButton);
            buttons.Children.Add(_minimizeButton);
            buttons.Children.Add(_maximizeButton);
            buttons.Children.Add(_closeButton);
            root.Children.Add(buttons);
            Child = root;

            // Added in code to a panel which isn't in the window yet: the window is known when loaded
            Loaded += (s, e) => AttachToWindow();
            PropertyChangedEventManager.AddHandler(ThemeManager.Instance, OnThemeChanged, string.Empty);
            UpdateThemeToggle();
        }

        /// <summary>
        /// The title, the title of the window when not set
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// The icon, the icon of the window when not set
        /// </summary>
        public ImageSource Icon
        {
            get => (ImageSource)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>
        /// The window's own buttons (e.g. help or pin), shown left of the caption buttons
        /// </summary>
        public object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        /// <summary>
        /// Show the button which switches between light and dark
        /// </summary>
        public bool ShowThemeToggle
        {
            get => (bool)GetValue(ShowThemeToggleProperty);
            set => SetValue(ShowThemeToggleProperty, value);
        }

        /// <summary>
        /// Show the minimize button, when not set: if the window can be minimized (its ResizeMode)
        /// </summary>
        public bool? ShowMinimize
        {
            get => (bool?)GetValue(ShowMinimizeProperty);
            set => SetValue(ShowMinimizeProperty, value);
        }

        /// <summary>
        /// Show the maximize button, when not set: if the window can be resized (its ResizeMode)
        /// </summary>
        public bool? ShowMaximize
        {
            get => (bool?)GetValue(ShowMaximizeProperty);
            set => SetValue(ShowMaximizeProperty, value);
        }

        /// <summary>
        /// Show the close button, it closes the window (like the close button of Windows)
        /// </summary>
        public bool ShowClose
        {
            get => (bool)GetValue(ShowCloseProperty);
            set => SetValue(ShowCloseProperty, value);
        }

        /// <summary>
        /// True when the window has the title bar of Windows and this one only shows the window's own buttons (if any)
        /// </summary>
        public bool IsSystemTitleBar { get; private set; }

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
            AttachToWindow();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            // In XAML the window is known here already, before it is shown: no flicker of the wrong title bar
            AttachToWindow();
        }

        private void AttachToWindow()
        {
            var window = Window.GetWindow(this);
            if (window == null || ReferenceEquals(window, _window))
            {
                return;
            }

            _window = window;
            _customWindowStyle = window.WindowStyle;
            window.StateChanged += (s, e) => UpdateButtons();
            window.SourceInitialized += (s, e) =>
            {
                // The title bar of Windows in the colors of the theme before the window shows, not only when it's loaded: no light flash
                WindowFrameTheme.Attach(window);
                // With the frame of Windows the window is white for a moment before WPF drew it: hidden until then.
                // Without it nothing shows in that moment, the window doesn't need to wait.
                if (ThemeManager.Instance.UseSystemTitleBar && !window.AllowsTransparency)
                {
                    WindowFrameTheme.CloakUntilRendered(window);
                }
                UpdateMode();
            };
            DependencyPropertyDescriptor.FromProperty(Window.TitleProperty, typeof(Window)).AddValueChanged(window, (s, e) => UpdateTitle());
            DependencyPropertyDescriptor.FromProperty(Window.IconProperty, typeof(Window)).AddValueChanged(window, (s, e) => UpdateTitle());
            DependencyPropertyDescriptor.FromProperty(Window.ResizeModeProperty, typeof(Window)).AddValueChanged(window, (s, e) => UpdateMode());
            UpdateTitle();
            UpdateMode();
        }

        private void OnThemeChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ThemeManager.UseSystemTitleBar) && e.PropertyName != nameof(ThemeManager.CurrentPalette))
            {
                return;
            }

            // The theme manager raises this on its own thread, the window can live on another one
            if (Dispatcher.CheckAccess())
            {
                UpdateMode();
                UpdateThemeToggle();
            }
            else if (!Dispatcher.HasShutdownStarted)
            {
                _ = Dispatcher.InvokeAsync(() =>
                {
                    UpdateMode();
                    UpdateThemeToggle();
                });
            }
        }

        private void OnContentChanged()
        {
            _contentPresenter.Content = Content;
            UpdateMode();
        }

        private void UpdateTitle()
        {
            _titleText.Text = Title ?? _window?.Title;
            var icon = Icon ?? _window?.Icon ?? ApplicationIcon.Value;
            _iconImage.Source = icon;
            _iconImage.Visibility = icon == null ? Visibility.Collapsed : Visibility.Visible;
        }

        private void UpdateThemeToggle()
        {
            bool isDark = ThemeManager.Instance.IsDarkTheme;
            _themeToggleButton.Content = isDark ? "☀️" : "🌙";
            _themeToggleButton.ToolTip = isDark ? "Switch to Light Mode" : "Switch to Dark Mode";
        }

        /// <summary>
        /// Give the window the title bar of Windows or this one, depending on the theme
        /// </summary>
        private void UpdateMode()
        {
            var window = _window;
            if (window == null)
            {
                return;
            }

            // A layered window can't have the frame of Windows
            bool useSystemTitleBar = ThemeManager.Instance.UseSystemTitleBar && !window.AllowsTransparency;
            IsSystemTitleBar = useSystemTitleBar;
            bool canResize = window.ResizeMode == ResizeMode.CanResize || window.ResizeMode == ResizeMode.CanResizeWithGrip;
            if (useSystemTitleBar)
            {
                bool hadChrome = WindowChrome.GetWindowChrome(window) != null;
                WindowChrome.SetWindowChrome(window, null);
                if (window.WindowStyle == WindowStyle.None)
                {
                    window.WindowStyle = WindowStyle.SingleBorderWindow;
                }

                if (hadChrome)
                {
                    // Switched while the window is open: WPF leaves the old frame of Windows 7, also after its own (queued) frame update
                    WindowFrameTheme.RestoreSystemFrame(window);
                    _ = Dispatcher.InvokeAsync(() => WindowFrameTheme.RestoreSystemFrame(window), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            else
            {
                WindowChrome.SetWindowChrome(window, new WindowChrome
                {
                    CaptionHeight = BarHeight - 1,
                    ResizeBorderThickness = canResize ? new Thickness(5) : new Thickness(0),
                    CornerRadius = new CornerRadius(0),
                    GlassFrameThickness = new Thickness(0),
                    NonClientFrameEdges = NonClientFrameEdges.None,
                    UseAeroCaptionButtons = false
                });
                if (window.WindowStyle != _customWindowStyle)
                {
                    window.WindowStyle = _customWindowStyle;
                }

                // Windows 11 doesn't round a window without a frame by itself
                WindowFrameTheme.SetRoundedCorners(window);
            }

            // With the title bar of Windows only the window's own buttons remain, without them nothing
            _captionPart.Visibility = useSystemTitleBar ? Visibility.Collapsed : Visibility.Visible;
            _captionButtons.Visibility = useSystemTitleBar ? Visibility.Collapsed : Visibility.Visible;
            MinHeight = useSystemTitleBar ? 0 : BarHeight;
            Visibility = useSystemTitleBar && Content == null ? Visibility.Collapsed : Visibility.Visible;
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            _themeToggleButton.Visibility = ShowThemeToggle ? Visibility.Visible : Visibility.Collapsed;
            var resizeMode = _window?.ResizeMode ?? ResizeMode.CanResize;
            bool canMinimize = resizeMode != ResizeMode.NoResize;
            bool canMaximize = resizeMode == ResizeMode.CanResize || resizeMode == ResizeMode.CanResizeWithGrip;
            _minimizeButton.Visibility = (ShowMinimize ?? canMinimize) ? Visibility.Visible : Visibility.Collapsed;
            _maximizeButton.Visibility = (ShowMaximize ?? canMaximize) ? Visibility.Visible : Visibility.Collapsed;
            _maximizeButton.Content = _window?.WindowState == WindowState.Maximized ? RestoreGlyph : MaximizeGlyph;
            _closeButton.Visibility = ShowClose ? Visibility.Visible : Visibility.Collapsed;
        }

        private static ImageSource LoadApplicationIcon()
        {
            try
            {
                using var icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetEntryAssembly()?.Location ?? string.Empty);
                if (icon == null)
                {
                    return null;
                }

                var source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                source.Freeze();
                return source;
            }
            catch
            {
                // No entry assembly (e.g. tests) or no icon: no icon in the title bar
                return null;
            }
        }

        private void Bind(DependencyProperty property, string themeProperty)
        {
            SetBinding(property, new Binding(themeProperty) { Source = ThemeManager.Instance });
        }

        /// <summary>
        /// A flat caption button like those of Windows, the close button turns red under the mouse
        /// </summary>
        private static Button CreateCaptionButton(string glyph, bool isClose)
        {
            var button = new Button
            {
                Content = glyph,
                Width = 46,
                Height = BarHeight - 1,
                MinWidth = 0,
                Padding = new Thickness(0),
                FontFamily = new FontFamily(CaptionFont),
                FontSize = 10,
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                Focusable = false,
                IsTabStop = false
            };
            button.SetBinding(Control.ForegroundProperty, new Binding(nameof(ThemeManager.ForegroundBrush)) { Source = ThemeManager.Instance });

            var border = new FrameworkElementFactory(typeof(Border), "Bd");
            border.SetBinding(Border.BackgroundProperty, new Binding("Background") { RelativeSource = RelativeSource.TemplatedParent });
            var content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(content);
            var template = new ControlTemplate(typeof(Button)) { VisualTree = border };
            var hover = new Trigger { Property = IsMouseOverProperty, Value = true };
            if (isClose)
            {
                hover.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xE8, 0x11, 0x23)), "Bd"));
                hover.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            }
            else
            {
                hover.Setters.Add(new Setter(Border.BackgroundProperty, new Binding(nameof(ThemeManager.ButtonHoverBrush)) { Source = ThemeManager.Instance }, "Bd"));
            }

            template.Triggers.Add(hover);
            var pressed = new Trigger { Property = Button.IsPressedProperty, Value = true };
            pressed.Setters.Add(new Setter(OpacityProperty, 0.8));
            template.Triggers.Add(pressed);
            button.Template = template;
            return button;
        }
    }
}
