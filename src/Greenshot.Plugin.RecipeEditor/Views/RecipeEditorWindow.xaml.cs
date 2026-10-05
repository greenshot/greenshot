using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Greenshot.Base.Recipes;
using Greenshot.Base.Wpf;
using Greenshot.Base.Wpf.Views;
using Greenshot.Plugin.RecipeEditor.ViewModels;

namespace Greenshot.Plugin.RecipeEditor.Views
{
    /// <summary>
    /// Interaction logic for RecipeEditorWindow.xaml
    /// </summary>
    public partial class RecipeEditorWindow : Window
    {
        private Point _rightClickDownPos;
        private bool _isRightClickDown;

        public RecipeEditorViewModel ViewModel { get; }

        private readonly DispatcherTimer _unsavedStateTimer;

        public RecipeEditorWindow(IRecipeManager recipeManager = null)
        {
            InitializeComponent();

            InitializeCanvasGestures();

            ViewModel = new RecipeEditorViewModel(recipeManager);
            DataContext = ViewModel;

            // Steps and triggers change their configuration directly: compare with the saved recipe regularly
            _unsavedStateTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(700) };
            _unsavedStateTimer.Tick += (s, e) => ViewModel.RefreshUnsavedState();
            _unsavedStateTimer.Start();
            Closing += OnWindowClosing;
            Closed += (s, e) =>
            {
                _unsavedStateTimer.Stop();
                ViewModel.Detach();
            };

            WpfThemeHelper.ThemeChanged += ApplyImmersiveDarkMode;
            Loaded += (s, e) =>
            {
                ApplyImmersiveDarkMode();
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
                // Mouse wheel as in other Windows apps: scroll up/down, Shift scrolls sideways, Ctrl zooms
                gestures.Editor.PanWithMouseWheel = true;
                gestures.Editor.PanVerticalModifierKey = ModifierKeys.None;
                gestures.Editor.PanHorizontalModifierKey = ModifierKeys.Shift;
                gestures.Editor.ZoomModifierKey = ModifierKeys.Control;
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
                                    // After this mouse up: the canvas may still hold the mouse (right drag pans), the menu wouldn't get its clicks
                                    var menu = fe.ContextMenu;
                                    e.Handled = true;
                                    _ = Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        Mouse.Capture(null);
                                        menu.PlacementTarget = fe;
                                        menu.IsOpen = true;
                                    }), System.Windows.Threading.DispatcherPriority.Input);
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

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            // Save, discard or keep editing the unsaved changes
            if (!ViewModel.ConfirmDiscardChanges())
            {
                e.Cancel = true;
            }
        }

        private void HotkeyDisplayControl_EditRequested(object sender, EventArgs e)
        {
            if (sender is Greenshot.Base.Wpf.Views.HotkeyDisplayView displayControl)
            {
                HotkeyModal.Open(displayControl.HeaderText, displayControl.HotkeyString, newHotkey =>
                {
                    displayControl.HotkeyString = newHotkey;
                });
            }
        }

        private void ApplyImmersiveDarkMode()
        {
            // The title bar in the colors of the theme, it follows theme changes from now on
            WindowFrameTheme.Attach(this);
        }
    }
}
