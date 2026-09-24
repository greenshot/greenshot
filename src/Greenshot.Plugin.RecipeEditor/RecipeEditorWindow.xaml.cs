using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Greenshot.Base.Recipes;
using Greenshot.Base.Wpf;
using Greenshot.Plugin.RecipeEditor.ViewModels;

namespace Greenshot.Plugin.RecipeEditor
{
    /// <summary>
    /// Interaction logic for RecipeEditorWindow.xaml
    /// </summary>
    public partial class RecipeEditorWindow : Window
    {
        private Point _rightClickDownPos;
        private bool _isRightClickDown;

        public RecipeEditorViewModel ViewModel { get; }

        public RecipeEditorWindow(IRecipeManager recipeManager = null)
        {
            InitializeComponent();

            InitializeCanvasGestures();

            ViewModel = new RecipeEditorViewModel(recipeManager);
            DataContext = ViewModel;

            WpfThemeHelper.ThemeChanged += ApplyImmersiveDarkMode;
            Loaded += (s, e) =>
            {
                ApplyImmersiveDarkMode();
                System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(this);
                try
                {
                    if (Greenshot.Editor.Controls.Emoji.EmojiData.Data?.Groups == null || Greenshot.Editor.Controls.Emoji.EmojiData.Data.Groups.Count == 0)
                    {
                        Greenshot.Editor.Controls.Emoji.EmojiData.Load();
                    }
                }
                catch
                {
                    // Ignore if emojis.xml is not present
                }
            };
        }

        private void InitializeCanvasGestures()
        {
            // Configure Nodify gestures: Support RightClick drag, MiddleClick drag, and Space+LeftClick drag to pan the canvas
            try
            {
                var gestures = new Nodify.Interactivity.EditorGestures();
                gestures.Editor.Pan.Value = new Nodify.Interactivity.AnyGesture(
                    new Nodify.Interactivity.MouseGesture(MouseAction.RightClick),
                    new Nodify.Interactivity.MouseGesture(MouseAction.MiddleClick),
                    new Nodify.Interactivity.MouseGesture(MouseAction.LeftClick, Key.Space)
                );
                EditorCanvas.InputGestures = gestures;
            }
            catch
            {
                // Silently fallback to defaults if gesture mapping cannot be initialized
            }

            // Track right-click down position to distinguish between dragging (panning) and clicking (context menu)
            EditorCanvas.PreviewMouseRightButtonDown += (s, e) =>
            {
                _rightClickDownPos = e.GetPosition(EditorCanvas);
                _isRightClickDown = true;
            };

            // Clear selection when clicking directly on empty canvas background
            EditorCanvas.PreviewMouseLeftButtonDown += (s, e) =>
            {
                var hit = VisualTreeHelper.HitTest(EditorCanvas, e.GetPosition(EditorCanvas));
                if (hit?.VisualHit != null)
                {
                    DependencyObject elem = hit.VisualHit;
                    bool isNodeOrConnection = false;
                    while (elem != null && elem != EditorCanvas)
                    {
                        if (elem is FrameworkElement fe && (fe.DataContext is StepNodeViewModel || fe.DataContext is StepConnectionViewModel || fe.DataContext is StepPortViewModel))
                        {
                            isNodeOrConnection = true;
                            break;
                        }
                        elem = VisualTreeHelper.GetParent(elem);
                    }
                    if (!isNodeOrConnection)
                    {
                        ViewModel.SelectedNode = null;
                        ViewModel.SelectedConnection = null;
                    }
                }
            };

            EditorCanvas.PreviewMouseRightButtonUp += (s, e) =>
            {
                if (_isRightClickDown)
                {
                    _isRightClickDown = false;
                    var pos = e.GetPosition(EditorCanvas);
                    var delta = (pos - _rightClickDownPos).Length;

                    // If clicked without significant dragging (< 4 pixels), open context menu of clicked element
                    if (delta < 4.0)
                    {
                        var hitResult = VisualTreeHelper.HitTest(EditorCanvas, pos);
                        if (hitResult?.VisualHit != null)
                        {
                            DependencyObject element = hitResult.VisualHit;
                            while (element != null && element != EditorCanvas)
                            {
                                if (element is FrameworkElement fe && fe.ContextMenu != null)
                                {
                                    fe.ContextMenu.PlacementTarget = fe;
                                    fe.ContextMenu.IsOpen = true;
                                    e.Handled = true;
                                    break;
                                }
                                element = VisualTreeHelper.GetParent(element);
                            }
                        }
                    }
                }
            };
        }

        private void OnConnectionPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is StepConnectionViewModel connVm)
            {
                ViewModel.SelectedConnection = connVm;
                ViewModel.SelectedNode = null;
                fe.Focus();
                e.Handled = true;
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ApplyImmersiveDarkMode();
        }

        private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                }
                else
                {
                    DragMove();
                }
            }
        }

        private void OnMinimizeClicked(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnMaximizeClicked(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void OnCloseTitleBarClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HotkeyDisplayControl_EditRequested(object sender, EventArgs e)
        {
            if (sender is Greenshot.UI.Controls.HotkeyDisplayControl displayControl)
            {
                HotkeyModal.Open(displayControl.HeaderText, displayControl.HotkeyString, newHotkey =>
                {
                    displayControl.HotkeyString = newHotkey;
                });
            }
        }

        private void ApplyImmersiveDarkMode()
        {
            try
            {
                var helper = new WindowInteropHelper(this);
                if (helper.Handle != IntPtr.Zero)
                {
                    int useImmersiveDarkMode = WpfThemeHelper.IsDarkMode ? 1 : 0;
                    int hr = DwmSetWindowAttribute(helper.Handle, 20, ref useImmersiveDarkMode, sizeof(int));
                    if (hr != 0)
                    {
                        DwmSetWindowAttribute(helper.Handle, 19, ref useImmersiveDarkMode, sizeof(int));
                    }
                }
            }
            catch
            {
                // Silently ignore if DWM call is unsupported on older OS
            }
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    }
}
