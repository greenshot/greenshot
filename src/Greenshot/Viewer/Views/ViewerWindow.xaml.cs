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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes;
using Greenshot.Base.Threading;
using Greenshot.Base.Wpf;
using Greenshot.Editor.Destinations;
using Greenshot.Recipes;
using Greenshot.Recipes.Views;
using Greenshot.Viewer.ViewModels;
using log4net;
using Screen = System.Windows.Forms.Screen;

namespace Greenshot.Viewer.Views
{
    /// <summary>
    /// A viewer: a small window with only the capture (and an optional thin title bar), which can be zoomed and panned.
    /// Sizes are calculated in screen pixels, so 1:1 really shows one pixel of the capture as one pixel of the screen.
    /// The window follows the zoom: it grows up to the work area of the screen and shrinks to <see cref="MinimumPixels"/>,
    /// when the capture is bigger than that, the visible part can be moved and the overview shows where it is.
    /// </summary>
    public partial class ViewerWindow : Window
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ViewerWindow));
        private static readonly List<ViewerWindow> OpenViewers = new List<ViewerWindow>();
        private const int MinimumPixels = 50;
        private const double MaximumZoom = 16;
        private const double ZoomStep = 1.25;
        private const double OverviewSize = 160;
        private const int WmSizing = 0x0214;

        private readonly ViewerViewModel _viewModel;
        private double _zoom = 1;
        // The part of the zoomed capture which is visible, in screen pixels: the left/top of the capture is at -offset
        private double _offsetX;
        private double _offsetY;
        private Point? _panStart;
        private double _panStartOffsetX;
        private double _panStartOffsetY;
        private bool _overviewDragging;
        private double _overviewScale;

        public ViewerWindow(ViewerViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = viewModel;
            InitializeComponent();
            HelpKeys.ItemsSource = CreateHelpEntries();
            UpdateChrome();
            viewModel.PropertyChanged += ViewModelPropertyChanged;
            SourceInitialized += (_, _) => HwndSource.FromHwnd(Handle)?.AddHook(WndProc);
            Loaded += OnLoaded;
            PreviewKeyDown += OnPreviewKeyDown;
            MouseRightButtonUp += (_, args) =>
            {
                args.Handled = true;
                ShowMenu(true);
            };
            Closed += OnClosed;
            OpenViewers.Add(this);
        }

        private IntPtr Handle => new WindowInteropHelper(this).EnsureHandle();

        private int CaptureWidth => _viewModel.Image?.PixelWidth ?? 1;

        private int CaptureHeight => _viewModel.Image?.PixelHeight ?? 1;

        /// <summary>
        /// The smallest zoom: the whole capture fits into the smallest window
        /// </summary>
        private double MinimumZoom => Math.Min(1.0, (double)MinimumPixels / Math.Max(CaptureWidth, CaptureHeight));

        private DpiScale Dpi => VisualTreeHelper.GetDpi(this);

        /// <summary>
        /// The size of the visible part of the capture in screen pixels
        /// </summary>
        private Size ViewportPixels => new Size(Viewport.ActualWidth * Dpi.DpiScaleX, Viewport.ActualHeight * Dpi.DpiScaleY);

        /// <summary>
        /// Close all viewers
        /// </summary>
        public static void CloseAll()
        {
            foreach (var viewer in OpenViewers.ToList())
            {
                viewer.Close();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // The capture 1:1 in a window as big as the capture, at most the work area: a bigger capture can be panned.
            // Or (setting) the capture smaller, so the window isn't bigger than the maximum size.
            var configuration = Dapplo.Ini.IniConfigRegistry.GetSection<IViewerConfiguration>();
            _zoom = 1;
            if (!configuration.OpenAtRealSize)
            {
                int maxWidth = Math.Max(MinimumPixels, configuration.MaxWidth);
                int maxHeight = Math.Max(MinimumPixels, configuration.MaxHeight);
                _zoom = Math.Max(MinimumZoom, Math.Min(1.0, Math.Min((double)maxWidth / CaptureWidth, (double)maxHeight / CaptureHeight)));
            }

            ApplyWindowSize(true);
            Activate();
            Viewport.Focus();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            OpenViewers.Remove(this);
            _viewModel.PropertyChanged -= ViewModelPropertyChanged;
            _viewModel.Dispose();
        }

        private void ViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ViewerViewModel.ShowTitleBar):
                    // The visible part keeps its size, the window gets bigger or smaller by the title bar and the border
                    var viewport = ViewportPixels;
                    UpdateChrome();
                    ApplyWindowSize(false, viewport);
                    break;
                case nameof(ViewerViewModel.AlwaysOnTop):
                    UpdateChrome();
                    break;
                case nameof(ViewerViewModel.ShowHelp):
                    if (_viewModel.ShowHelp)
                    {
                        HelpScroller.Focus();
                    }
                    else
                    {
                        Viewport.Focus();
                    }
                    break;
            }
        }

        /// <summary>
        /// The border goes with the title bar, the always on top button shows the state
        /// </summary>
        private void UpdateChrome()
        {
            Frame.BorderThickness = new Thickness(_viewModel.ShowTitleBar ? 1 : 0);
            // Pinned or not pinned
            AlwaysOnTopButton.Content = _viewModel.AlwaysOnTop ? "" : "";
        }

        /// <summary>
        /// Size the window: for the zoomed capture or the given visible part, at least the minimum, at most the work area of the screen.
        /// Only when the viewer opens, the title bar is switched or the user asks for it (W): zooming keeps the window size.
        /// </summary>
        /// <param name="center">Center the window on the screen of the mouse (when it's shown), else keep its location if possible</param>
        /// <param name="viewportPixels">The size of the visible part in screen pixels, null for the zoomed capture</param>
        private void ApplyWindowSize(bool center, Size? viewportPixels = null)
        {
            if (!IsLoaded)
            {
                return;
            }

            UpdateLayout();
            var dpi = Dpi;
            var chromeWidth = (ActualWidth - Viewport.ActualWidth) * dpi.DpiScaleX;
            var chromeHeight = (ActualHeight - Viewport.ActualHeight) * dpi.DpiScaleY;
            var screen = center ? Screen.FromPoint(System.Windows.Forms.Cursor.Position) : Screen.FromHandle(Handle);
            var workArea = screen.WorkingArea;

            var viewport = viewportPixels ?? new Size(CaptureWidth * _zoom, CaptureHeight * _zoom);
            int width = (int)Math.Round(Math.Min(Math.Max(viewport.Width, MinimumPixels) + chromeWidth, workArea.Width));
            int height = (int)Math.Round(Math.Min(Math.Max(viewport.Height, MinimumPixels) + chromeHeight, workArea.Height));

            int x;
            int y;
            if (center)
            {
                x = workArea.Left + (workArea.Width - width) / 2;
                y = workArea.Top + (workArea.Height - height) / 2;
            }
            else
            {
                // Keep the top left corner, but stay inside the work area
                var location = PointToScreen(new Point(0, 0));
                x = Math.Max(workArea.Left, Math.Min((int)location.X, workArea.Right - width));
                y = Math.Max(workArea.Top, Math.Min((int)location.Y, workArea.Bottom - height));
            }

            // Twice: moving to a monitor with another DPI makes WPF scale the window, the second call sets the pixel size again
            var flags = WindowPos.SWP_NOZORDER | WindowPos.SWP_NOACTIVATE;
            User32Api.SetWindowPos(Handle, IntPtr.Zero, x, y, width, height, flags);
            User32Api.SetWindowPos(Handle, IntPtr.Zero, x, y, width, height, flags);
            UpdateLayout();
            LayoutCapture();
        }

        /// <summary>
        /// Place the capture in the viewport for the zoom and the offset, and show the overview when not all of it is visible
        /// </summary>
        private void LayoutCapture()
        {
            var dpi = Dpi;
            var viewport = ViewportPixels;
            double width = CaptureWidth * _zoom;
            double height = CaptureHeight * _zoom;

            // Centered when it's smaller than the viewport, else the offset decides which part is visible
            double left;
            if (width <= viewport.Width)
            {
                _offsetX = 0;
                left = Math.Round((viewport.Width - width) / 2);
            }
            else
            {
                _offsetX = Math.Round(Math.Max(0, Math.Min(_offsetX, width - viewport.Width)));
                left = -_offsetX;
            }

            double top;
            if (height <= viewport.Height)
            {
                _offsetY = 0;
                top = Math.Round((viewport.Height - height) / 2);
            }
            else
            {
                _offsetY = Math.Round(Math.Max(0, Math.Min(_offsetY, height - viewport.Height)));
                top = -_offsetY;
            }

            CaptureImage.Width = width / dpi.DpiScaleX;
            CaptureImage.Height = height / dpi.DpiScaleY;
            Canvas.SetLeft(CaptureImage, left / dpi.DpiScaleX);
            Canvas.SetTop(CaptureImage, top / dpi.DpiScaleY);
            // Pixels stay pixels when enlarged, smaller is smooth
            RenderOptions.SetBitmapScalingMode(CaptureImage, _zoom >= 1 ? BitmapScalingMode.NearestNeighbor : BitmapScalingMode.HighQuality);

            bool canPan = width > viewport.Width + 0.5 || height > viewport.Height + 0.5;
            Viewport.Cursor = canPan ? Cursors.SizeAll : null;
            LayoutOverview(canPan, viewport, width, height);
        }

        private void LayoutOverview(bool canPan, Size viewport, double width, double height)
        {
            // Not when it would cover most of a small window
            bool show = canPan && Viewport.ActualWidth >= OverviewSize * 2 && Viewport.ActualHeight >= OverviewSize * 1.5;
            Overview.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (!show)
            {
                return;
            }

            // In device independent pixels: the capture in the overview size
            _overviewScale = Math.Min(OverviewSize / CaptureWidth, OverviewSize / CaptureHeight);
            OverviewImage.Width = CaptureWidth * _overviewScale;
            OverviewImage.Height = CaptureHeight * _overviewScale;
            // The visible part, the overview scale is per pixel of the capture
            double frameScale = _overviewScale / _zoom;
            OverviewFrame.Width = Math.Min(viewport.Width, width) * frameScale;
            OverviewFrame.Height = Math.Min(viewport.Height, height) * frameScale;
            Canvas.SetLeft(OverviewFrame, _offsetX * frameScale);
            Canvas.SetTop(OverviewFrame, _offsetY * frameScale);
        }

        /// <summary>
        /// Zoom, the point of the capture under the anchor (in viewport pixels) stays where it is if possible
        /// </summary>
        private void ZoomTo(double zoom, Point? anchor = null)
        {
            zoom = Math.Max(MinimumZoom, Math.Min(MaximumZoom, zoom));
            if (Math.Abs(zoom - _zoom) < 0.0001)
            {
                return;
            }

            var viewport = ViewportPixels;
            var at = anchor ?? new Point(viewport.Width / 2, viewport.Height / 2);
            // The point of the capture (in capture pixels) under the anchor
            double captureX = (at.X + _offsetX - CaptureLeftPixels(viewport.Width)) / _zoom;
            double captureY = (at.Y + _offsetY - CaptureTopPixels(viewport.Height)) / _zoom;
            _zoom = zoom;
            _offsetX = captureX * zoom - at.X;
            _offsetY = captureY * zoom - at.Y;
            // The window keeps its size, the overview shows where the visible part is
            LayoutCapture();
        }

        /// <summary>
        /// Where the capture starts when it's centered (smaller than the viewport), else 0
        /// </summary>
        private double CaptureLeftPixels(double viewportWidth) => Math.Max(0, (viewportWidth - CaptureWidth * _zoom) / 2);

        private double CaptureTopPixels(double viewportHeight) => Math.Max(0, (viewportHeight - CaptureHeight * _zoom) / 2);

        /// <summary>
        /// The next zoom step, 1:1 is always one of the steps
        /// </summary>
        private void ZoomStepBy(bool zoomIn, Point? anchor = null)
        {
            double zoom = zoomIn ? _zoom * ZoomStep : _zoom / ZoomStep;
            if ((_zoom < 1 && zoom > 1) || (_zoom > 1 && zoom < 1))
            {
                zoom = 1;
            }

            ZoomTo(zoom, anchor);
        }

        private void ShowActualSize() => ZoomTo(1);

        /// <summary>
        /// Zoom so all of the capture is visible in the window
        /// </summary>
        private void FitToWindow()
        {
            var viewport = ViewportPixels;
            _zoom = Math.Max(MinimumZoom, Math.Min(MaximumZoom, Math.Min(viewport.Width / CaptureWidth, viewport.Height / CaptureHeight)));
            _offsetX = 0;
            _offsetY = 0;
            LayoutCapture();
        }

        /// <summary>
        /// Size the window to the zoomed capture, as far as the screen allows
        /// </summary>
        private void SizeWindowToCapture() => ApplyWindowSize(false);

        private void PanBy(double dx, double dy)
        {
            _offsetX += dx;
            _offsetY += dy;
            LayoutCapture();
        }

        private void Export()
        {
            AsyncCommand.Run(() => _viewModel.ExportAsync(ChooseDestination), "Export from the viewer");
        }

        /// <summary>
        /// The destination window of the recipes (without the preview, the viewer shows the capture), owned by the viewer.
        /// It also offers to forward the capture to a recipe.
        /// </summary>
        private (IDestination Destination, CaptureRecipe Recipe) ChooseDestination(IReadOnlyList<IDestination> destinations, string error)
        {
            var recipes = RecipeManager.Instance.GetAllRecipes().Where(r => r != null && r.IsEnabled).ToList();
            var window = new DynamicDestinationWindow(null, null, destinations, recipes, error)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            window.ShowDialog();
            if (window.OpenInEditorRequested)
            {
                return (DestinationHelper.GetDestination(EditorDestination.DESIGNATION) ?? window.SelectedDestination, null);
            }

            return (window.SelectedDestination, window.SelectedRecipeToForward);
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var modifiers = Keyboard.Modifiers;
            var viewport = ViewportPixels;
            bool handled = true;
            if (e.Key == Key.Apps || (e.Key == Key.System && e.SystemKey == Key.F10 && modifiers == ModifierKeys.Shift))
            {
                ShowMenu(false);
            }
            else if (modifiers != ModifierKeys.None && modifiers != ModifierKeys.Shift)
            {
                // Ctrl and Alt combinations belong to Windows and the menus
                handled = false;
            }
            else
            {
                switch (e.Key)
                {
                    case Key.Escape:
                        if (_viewModel.ShowHelp)
                        {
                            _viewModel.ShowHelp = false;
                        }
                        else
                        {
                            Close();
                        }
                        break;
                    case Key.H:
                    case Key.F1:
                        _viewModel.ShowHelp = !_viewModel.ShowHelp;
                        break;
                    case Key.Add:
                    case Key.OemPlus:
                        ZoomStepBy(true);
                        break;
                    case Key.Subtract:
                    case Key.OemMinus:
                        ZoomStepBy(false);
                        break;
                    case Key.D0:
                    case Key.NumPad0:
                        FitToWindow();
                        break;
                    case Key.D1:
                    case Key.NumPad1:
                        ShowActualSize();
                        break;
                    case Key.Left:
                        PanBy(-viewport.Width / 10, 0);
                        break;
                    case Key.Right:
                        PanBy(viewport.Width / 10, 0);
                        break;
                    case Key.Up:
                        PanBy(0, -viewport.Height / 10);
                        break;
                    case Key.Down:
                        PanBy(0, viewport.Height / 10);
                        break;
                    case Key.W:
                        SizeWindowToCapture();
                        break;
                    case Key.E:
                        Export();
                        break;
                    case Key.P:
                        _viewModel.AlwaysOnTop = !_viewModel.AlwaysOnTop;
                        break;
                    case Key.T:
                        _viewModel.ShowTitleBar = !_viewModel.ShowTitleBar;
                        break;
                    case Key.M:
                        _viewModel.ShowCursor = !_viewModel.ShowCursor;
                        break;
                    default:
                        handled = false;
                        break;
                }
            }

            e.Handled = handled;
        }

        /// <summary>
        /// The menu with all actions, the keys are shown next to them
        /// </summary>
        /// <param name="atMouse">At the mouse, else (keyboard) in the middle of the window</param>
        private void ShowMenu(bool atMouse)
        {
            var menu = ThemedMenu.CreateContextMenu();
            menu.Items.Add(CreateMenuItem(Texts.Core.ViewerExport, "E", Export));
            menu.Items.Add(ThemedMenu.CreateSeparator());
            menu.Items.Add(CreateMenuItem(Texts.Core.ViewerActualSize, "1", ShowActualSize));
            menu.Items.Add(CreateMenuItem(Texts.Core.ViewerFit, "0", FitToWindow));
            menu.Items.Add(CreateMenuItem(Texts.Core.ViewerWindowToCapture, "W", SizeWindowToCapture));
            menu.Items.Add(ThemedMenu.CreateSeparator());
            menu.Items.Add(CreateCheckItem(Texts.Core.ViewerAlwaysOnTop, "P", _viewModel.AlwaysOnTop, value => _viewModel.AlwaysOnTop = value));
            menu.Items.Add(CreateCheckItem(Texts.Core.ViewerTitleBar, "T", _viewModel.ShowTitleBar, value => _viewModel.ShowTitleBar = value));
            var cursorItem = CreateCheckItem(Texts.Core.ViewerMousePointer, "M", _viewModel.ShowCursor, value => _viewModel.ShowCursor = value);
            cursorItem.IsEnabled = _viewModel.HasCursor;
            menu.Items.Add(cursorItem);
            menu.Items.Add(ThemedMenu.CreateSeparator());
            menu.Items.Add(CreateMenuItem(Texts.Core.ViewerHelp, "H", () => _viewModel.ShowHelp = true));
            menu.Items.Add(CreateMenuItem(Texts.Core.ViewerCloseAll, null, CloseAll));
            menu.Items.Add(CreateMenuItem(Texts.Editor.Close, "Esc", Close));

            if (atMouse)
            {
                ThemedMenu.ShowAtCursor(menu);
                return;
            }

            menu.PlacementTarget = Viewport;
            menu.Placement = PlacementMode.Center;
            menu.IsOpen = true;
            menu.Focus();
        }

        private static MenuItem CreateMenuItem(string text, string key, Action onClick)
        {
            var item = ThemedMenu.CreateItem(text, null, onClick);
            item.InputGestureText = key ?? string.Empty;
            return item;
        }

        private static MenuItem CreateCheckItem(string text, string key, bool isChecked, Action<bool> onChanged)
        {
            var item = ThemedMenu.CreateCheckItem(text, isChecked, onChanged);
            item.InputGestureText = key;
            return item;
        }

        private static IReadOnlyList<HelpEntry> CreateHelpEntries()
        {
            var core = Texts.Core;
            return new List<HelpEntry>
            {
                new HelpEntry("1", core.ViewerKeyActualSize),
                new HelpEntry("0", core.ViewerKeyFit),
                new HelpEntry("W", core.ViewerKeyWindowToCapture),
                new HelpEntry("+  −", core.ViewerKeyZoom),
                new HelpEntry("← ↑ → ↓", core.ViewerKeyPan),
                new HelpEntry("Ctrl", core.ViewerKeyMove),
                new HelpEntry("E", core.ViewerKeyExport),
                new HelpEntry("P", core.ViewerKeyAlwaysOnTop),
                new HelpEntry("T", core.ViewerKeyTitleBar),
                new HelpEntry("M", core.ViewerKeyMousePointer),
                new HelpEntry("Shift+F10", core.ViewerKeyMenu),
                new HelpEntry("H  F1", core.ViewerKeyHelp),
                new HelpEntry("Esc", core.ViewerKeyClose)
            };
        }

        private void TitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void ViewportSizeChanged(object sender, SizeChangedEventArgs e) => LayoutCapture();

        private void ViewportMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Viewport.Focus();
            if (Viewport.Cursor == null || Keyboard.Modifiers == ModifierKeys.Control)
            {
                // All of the capture is visible, or with Ctrl: dragging moves the window, also without a title bar
                DragMove();
                return;
            }

            _panStart = e.GetPosition(Viewport);
            _panStartOffsetX = _offsetX;
            _panStartOffsetY = _offsetY;
            Viewport.CaptureMouse();
            e.Handled = true;
        }

        private void ViewportMouseMove(object sender, MouseEventArgs e)
        {
            if (_panStart is not { } start)
            {
                return;
            }

            var position = e.GetPosition(Viewport);
            var dpi = Dpi;
            _offsetX = _panStartOffsetX - (position.X - start.X) * dpi.DpiScaleX;
            _offsetY = _panStartOffsetY - (position.Y - start.Y) * dpi.DpiScaleY;
            LayoutCapture();
        }

        private void ViewportMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_panStart != null)
            {
                Viewport.ReleaseMouseCapture();
            }
        }

        private void ViewportLostMouseCapture(object sender, MouseEventArgs e) => _panStart = null;

        private void ViewportMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var position = e.GetPosition(Viewport);
            var dpi = Dpi;
            ZoomStepBy(e.Delta > 0, new Point(position.X * dpi.DpiScaleX, position.Y * dpi.DpiScaleY));
            e.Handled = true;
        }

        private void OverviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _overviewDragging = true;
            Overview.CaptureMouse();
            MoveToOverviewPoint(e.GetPosition(OverviewImage));
            e.Handled = true;
        }

        private void OverviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_overviewDragging)
            {
                MoveToOverviewPoint(e.GetPosition(OverviewImage));
            }
        }

        private void OverviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _overviewDragging = false;
            Overview.ReleaseMouseCapture();
            e.Handled = true;
        }

        /// <summary>
        /// Center the visible part on the point of the overview
        /// </summary>
        private void MoveToOverviewPoint(Point point)
        {
            if (_overviewScale <= 0)
            {
                return;
            }

            var viewport = ViewportPixels;
            double frameScale = _overviewScale / _zoom;
            _offsetX = point.X / frameScale - viewport.Width / 2;
            _offsetY = point.Y / frameScale - viewport.Height / 2;
            LayoutCapture();
        }

        private void CloseClick(object sender, RoutedEventArgs e) => Close();

        private void HelpClick(object sender, RoutedEventArgs e) => _viewModel.ShowHelp = !_viewModel.ShowHelp;

        private void AlwaysOnTopClick(object sender, RoutedEventArgs e) => _viewModel.AlwaysOnTop = !_viewModel.AlwaysOnTop;

        private void FitClick(object sender, RoutedEventArgs e) => FitToWindow();

        private void ActualSizeClick(object sender, RoutedEventArgs e) => ShowActualSize();

        private void ExportClick(object sender, RoutedEventArgs e) => Export();

        /// <summary>
        /// Resizing with the mouse keeps the aspect ratio of the capture: the zoom follows the size
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg != WmSizing || !IsLoaded)
            {
                return IntPtr.Zero;
            }

            try
            {
                var rect = Marshal.PtrToStructure<SizingRect>(lParam);
                var dpi = Dpi;
                double chromeWidth = (ActualWidth - Viewport.ActualWidth) * dpi.DpiScaleX;
                double chromeHeight = (ActualHeight - Viewport.ActualHeight) * dpi.DpiScaleY;
                // The visible part keeps the proportions of the capture, the zoom stays
                double scaleByWidth = (rect.Right - rect.Left - chromeWidth) / CaptureWidth;
                double scaleByHeight = (rect.Bottom - rect.Top - chromeHeight) / CaptureHeight;
                int edge = wParam.ToInt32();
                // WMSZ_LEFT 1, WMSZ_RIGHT 2, WMSZ_TOP 3, WMSZ_BOTTOM 6, the others are corners
                double scale = edge switch
                {
                    1 or 2 => scaleByWidth,
                    3 or 6 => scaleByHeight,
                    _ => Math.Max(scaleByWidth, scaleByHeight)
                };
                var workArea = Screen.FromHandle(hwnd).WorkingArea;
                double maximumScale = Math.Min((workArea.Width - chromeWidth) / CaptureWidth, (workArea.Height - chromeHeight) / CaptureHeight);
                double minimumScale = (double)MinimumPixels / Math.Min(CaptureWidth, CaptureHeight);
                scale = Math.Min(maximumScale, Math.Max(minimumScale, scale));

                int width = (int)Math.Round(CaptureWidth * scale + chromeWidth);
                int height = (int)Math.Round(CaptureHeight * scale + chromeHeight);
                // The edges which are dragged move, the others stay
                if (edge is 1 or 4 or 7)
                {
                    rect.Left = rect.Right - width;
                }
                else
                {
                    rect.Right = rect.Left + width;
                }

                if (edge is 3 or 4 or 5)
                {
                    rect.Top = rect.Bottom - height;
                }
                else
                {
                    rect.Bottom = rect.Top + height;
                }

                Marshal.StructureToPtr(rect, lParam, false);
                handled = true;
                return new IntPtr(1);
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't keep the aspect ratio while resizing the viewer", ex);
                return IntPtr.Zero;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SizingRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        /// <summary>
        /// A line of the help: the keys and what they do
        /// </summary>
        public sealed class HelpEntry
        {
            public HelpEntry(string keys, string description)
            {
                Keys = keys;
                Description = description;
            }

            public string Keys { get; }

            public string Description { get; }

            public override string ToString() => Keys + " " + Description;
        }
    }
}
