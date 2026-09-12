using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Greenshot.Recipes;
using Greenshot.UI.RecipeEditor.ViewModels;

namespace Greenshot.UI.RecipeEditor
{
    /// <summary>
    /// Interaction logic for RecipeEditorWindow.xaml
    /// </summary>
    public partial class RecipeEditorWindow : Window
    {
        private Point _rightClickDownPos;
        private bool _isRightClickDown;

        public RecipeEditorViewModel ViewModel { get; }

        public RecipeEditorWindow(RecipeManager recipeManager = null)
        {
            InitializeComponent();

            InitializeCanvasGestures();

            ViewModel = new RecipeEditorViewModel(recipeManager);
            DataContext = ViewModel;
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

        private void ApplyImmersiveDarkMode()
        {
            try
            {
                var helper = new WindowInteropHelper(this);
                if (helper.Handle != IntPtr.Zero && WpfThemeHelper.IsDarkMode)
                {
                    int useImmersiveDarkMode = 1;
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
