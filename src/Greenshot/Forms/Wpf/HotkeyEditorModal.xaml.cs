using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Dapplo.Windows.Input.Enums;
using Greenshot.Base.Core;
using Greenshot.Base.Wpf;

namespace Greenshot.Forms.Wpf
{
    public partial class HotkeyEditorModal : UserControl
    {
        private Action<string> _onSavedCallback;

        private HotkeyEditorViewModel ViewModel => DataContext as HotkeyEditorViewModel;

        public HotkeyEditorModal()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            IsVisibleChanged += OnIsVisibleChanged;
        }

        public void Open(string title, string hotkeyString, Action<string> onSaved)
        {
            ThemeManager.Instance.IsDarkTheme = Greenshot.UI.WpfThemeHelper.IsDarkMode;
            _onSavedCallback = onSaved;
            if (ViewModel == null)
            {
                var vm = new HotkeyEditorViewModel();
                DataContext = vm;
            }

            ViewModel.Saved -= OnViewModelSaved;
            ViewModel.Cancelled -= OnViewModelCancelled;
            ViewModel.Saved += OnViewModelSaved;
            ViewModel.Cancelled += OnViewModelCancelled;

            ViewModel.LoadFromSequenceString(title, hotkeyString);
            Visibility = Visibility.Visible;
            FocusCapture();
        }

        private void OnViewModelSaved(object sender, EventArgs e)
        {
            if (ViewModel != null)
            {
                var seq = ViewModel.BuildSequence();
                _onSavedCallback?.Invoke(seq.ToString());
            }
            Close();
        }

        private void OnViewModelCancelled(object sender, EventArgs e)
        {
            Close();
        }

        public void Close()
        {
            Visibility = Visibility.Collapsed;
            _onSavedCallback = null;
            HotkeyManager.IsPaused = false;
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                HotkeyManager.IsPaused = true;
                FocusCapture();
            }
            else
            {
                HotkeyManager.IsPaused = false;
            }
        }

        public void FocusCapture()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                KeyCaptureBorder.Focus();
                Keyboard.Focus(KeyCaptureBorder);
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is HotkeyEditorViewModel oldVm)
            {
                oldVm.PropertyChanged -= OnViewModelPropertyChanged;
            }
            if (e.NewValue is HotkeyEditorViewModel newVm)
            {
                newVm.PropertyChanged += OnViewModelPropertyChanged;
                UpdateLivePreview();
            }
        }

        private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateLivePreview();
        }

        private void UpdateLivePreview()
        {
            if (ViewModel == null) return;
            var seq = ViewModel.BuildSequence();
            LivePreviewControl.HotkeyString = seq.ToString();
        }

        private void KeyCaptureBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            KeyCaptureBorder.Focus();
            Keyboard.Focus(KeyCaptureBorder);
            e.Handled = true;
        }

        private void KeyCaptureBorder_GotFocus(object sender, RoutedEventArgs e)
        {
            HotkeyManager.IsPaused = true;
            if (ViewModel != null) ViewModel.IsCapturing = true;
            if (CaptureHintText != null)
            {
                CaptureHintText.Text = "🔴 Listening... Press keys on your keyboard";
            }
        }

        private void KeyCaptureBorder_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null) ViewModel.IsCapturing = false;
            if (CaptureHintText != null)
            {
                CaptureHintText.Text = "Click here to focus and press shortcut keys";
            }
        }

        private void HotkeyEditorModal_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Visibility != Visibility.Visible) return;

            if (e.OriginalSource is DependencyObject dep)
            {
                var cb = FindVisualParent<ComboBox>(dep);
                if (cb != null && cb.IsDropDownOpen)
                {
                    return;
                }

                var btn = FindVisualParent<Button>(dep);
                if (btn != null && (e.Key == Key.Enter || e.Key == Key.Space))
                {
                    return;
                }
            }

            HandleCapturedKeyDown(e);
        }

        private void KeyCaptureBorder_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            HandleCapturedKeyDown(e);
        }

        private void HandleCapturedKeyDown(KeyEventArgs e)
        {
            e.Handled = true;

            Key key = e.Key == Key.System ? e.SystemKey : e.Key;

            bool isLeftCtrl = Keyboard.IsKeyDown(Key.LeftCtrl);
            bool isRightCtrl = Keyboard.IsKeyDown(Key.RightCtrl);
            bool isLeftAlt = Keyboard.IsKeyDown(Key.LeftAlt);
            bool isRightAlt = Keyboard.IsKeyDown(Key.RightAlt);
            bool isLeftShift = Keyboard.IsKeyDown(Key.LeftShift);
            bool isRightShift = Keyboard.IsKeyDown(Key.RightShift);
            bool isLeftWin = Keyboard.IsKeyDown(Key.LWin);
            bool isRightWin = Keyboard.IsKeyDown(Key.RWin);

            bool ctrl = isLeftCtrl || isRightCtrl;
            bool alt = isLeftAlt || isRightAlt;
            bool shift = isLeftShift || isRightShift;
            bool win = isLeftWin || isRightWin;

            // Standalone modifier press: update active modifier toggles
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                ViewModel?.UpdateModifiersFromKeyboard(ctrl, alt, shift, win,
                    isLeftCtrl, isRightCtrl, isLeftAlt, isRightAlt, isLeftShift, isRightShift, isLeftWin, isRightWin);
                return;
            }

            int vkInt = KeyInterop.VirtualKeyFromKey(key);
            var vk = (VirtualKeyCode)vkInt;

            // Handle PrintScreen which sometimes maps specifically
            if (key == Key.Snapshot || key == Key.PrintScreen)
            {
                vk = VirtualKeyCode.Snapshot;
            }

            ViewModel?.SetCapturedKey(vk, ctrl, alt, shift, win,
                isLeftCtrl, isRightCtrl, isLeftAlt, isRightAlt, isLeftShift, isRightShift, isLeftWin, isRightWin);
        }

        private void HotkeyEditorModal_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            HandleCapturedKeyUp(e);
        }

        private void KeyCaptureBorder_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            HandleCapturedKeyUp(e);
        }

        private void HandleCapturedKeyUp(KeyEventArgs e)
        {
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key == Key.Snapshot || key == Key.PrintScreen)
            {
                e.Handled = true;
                bool isLeftCtrl = Keyboard.IsKeyDown(Key.LeftCtrl);
                bool isRightCtrl = Keyboard.IsKeyDown(Key.RightCtrl);
                bool isLeftAlt = Keyboard.IsKeyDown(Key.LeftAlt);
                bool isRightAlt = Keyboard.IsKeyDown(Key.RightAlt);
                bool isLeftShift = Keyboard.IsKeyDown(Key.LeftShift);
                bool isRightShift = Keyboard.IsKeyDown(Key.RightShift);
                bool isLeftWin = Keyboard.IsKeyDown(Key.LWin);
                bool isRightWin = Keyboard.IsKeyDown(Key.RWin);

                ViewModel?.SetCapturedKey(VirtualKeyCode.Snapshot,
                    isLeftCtrl || isRightCtrl,
                    isLeftAlt || isRightAlt,
                    isLeftShift || isRightShift,
                    isLeftWin || isRightWin,
                    isLeftCtrl, isRightCtrl, isLeftAlt, isRightAlt, isLeftShift, isRightShift, isLeftWin, isRightWin);
            }
        }

        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent) return parent;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }
    }
}
