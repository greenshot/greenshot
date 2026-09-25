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
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Greenshot.Base.Core;
using Greenshot.Base.Wpf;
using log4net;

namespace Greenshot.UI.SelfService
{
    public partial class SelfServiceWindow : Window
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SelfServiceWindow));
        private static SelfServiceWindow _currentInstance;

        public SelfServiceViewModel ViewModel => DataContext as SelfServiceViewModel;

        public SelfServiceWindow(string initialSection = null)
        {
            InitializeComponent();

            try
            {
                var iconUri = new Uri("pack://application:,,,/Greenshot;component/icons/applicationIcon/icon.ico", UriKind.RelativeOrAbsolute);
                Icon = BitmapFrame.Create(iconUri);
            }
            catch
            {
                // Ignore under test runners where Pack URI container is not initialized
            }

            var vm = new SelfServiceViewModel(initialSection);
            DataContext = vm;
            vm.PropertyChanged += OnViewModelPropertyChanged;

            UpdateVisiblePanel();

            WpfThemeHelper.ThemeChanged += OnThemeChanged;
            Loaded += OnWindowLoaded;
            Closed += OnWindowClosed;
            KeyDown += OnWindowKeyDown;
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelfServiceViewModel.SelectedSection))
            {
                UpdateVisiblePanel();
            }
        }

        private void UpdateVisiblePanel()
        {
            string sectionId = ViewModel?.CurrentSectionId ?? "system";

            SystemInfoPanel.Visibility = sectionId == "system" ? Visibility.Visible : Visibility.Collapsed;
            FileInfoPanel.Visibility = sectionId == "files" ? Visibility.Visible : Visibility.Collapsed;
            ClipboardPanel.Visibility = sectionId == "clipboard" ? Visibility.Visible : Visibility.Collapsed;
            HotkeysPanel.Visibility = sectionId == "hotkeys" ? Visibility.Visible : Visibility.Collapsed;

            UpdateStatusText(null);
        }

        private void UpdateStatusText(string customMessage)
        {
            if (FooterStatusText == null) return;

            if (!string.IsNullOrEmpty(customMessage))
            {
                FooterStatusText.Text = customMessage;
                return;
            }

            string sectionId = ViewModel?.CurrentSectionId;
            switch (sectionId)
            {
                case "system":
                    FooterStatusText.Text = ViewModel?.SystemInfoSection.StatusMessage ?? "System information ready";
                    break;
                case "files":
                    FooterStatusText.Text = ViewModel?.FileInfoSection.StatusMessage ?? "File locations ready";
                    break;
                case "clipboard":
                    FooterStatusText.Text = ViewModel?.ClipboardSection.StatusHeader ?? "Clipboard diagnostics ready";
                    break;
                case "hotkeys":
                    FooterStatusText.Text = ViewModel?.HotkeySection.StatusMessage ?? "Hotkey diagnostics ready";
                    break;
                default:
                    FooterStatusText.Text = "Ready";
                    break;
            }
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            ApplyImmersiveDarkMode();
            System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(this);
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            WpfThemeHelper.ThemeChanged -= OnThemeChanged;
            ViewModel?.Cleanup();
            if (_currentInstance == this)
            {
                _currentInstance = null;
            }
        }

        private void OnThemeChanged()
        {
            ApplyImmersiveDarkMode();
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
                    ToggleMaximize();
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

        private void OnMaximizeRestoreClicked(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void ToggleMaximize()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void OnThemeToggleClicked(object sender, RoutedEventArgs e)
        {
            ViewModel?.ToggleTheme();
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;
                case Key.F5:
                    ViewModel?.SelectedSection?.Refresh();
                    UpdateStatusText("Refreshed!");
                    e.Handled = true;
                    break;
                case Key.T:
                    ViewModel?.ToggleTheme();
                    e.Handled = true;
                    break;
                case Key.D1:
                case Key.E:
                    ViewModel?.SelectSection("system");
                    e.Handled = true;
                    break;
                case Key.D2:
                case Key.F:
                case Key.L:
                case Key.I:
                    ViewModel?.SelectSection("files");
                    e.Handled = true;
                    break;
                case Key.D3:
                case Key.C:
                    ViewModel?.SelectSection("clipboard");
                    e.Handled = true;
                    break;
                case Key.D4:
                case Key.H:
                    ViewModel?.SelectSection("hotkeys");
                    e.Handled = true;
                    break;
            }
        }

        // Section 1 actions
        private void OnRefreshCurrentSectionClicked(object sender, RoutedEventArgs e)
        {
            ViewModel?.SelectedSection?.Refresh();
            UpdateStatusText("Refreshed!");
        }

        private void OnCopySystemInfoClicked(object sender, RoutedEventArgs e)
        {
            ViewModel?.SystemInfoSection.CopyReportToClipboard();
            UpdateStatusText("System information copied to clipboard!");
        }

        // Section 2 actions
        private void OnOpenLogInExplorerClicked(object sender, RoutedEventArgs e)
        {
            ViewModel?.FileInfoSection.OpenLogInExplorer();
            UpdateStatusText(ViewModel?.FileInfoSection.StatusMessage);
        }

        private void OnCopyLogPathClicked(object sender, RoutedEventArgs e)
        {
            ViewModel?.FileInfoSection.CopyLogPath();
            UpdateStatusText(ViewModel?.FileInfoSection.StatusMessage);
        }

        private void OnOpenConfigInExplorerClicked(object sender, RoutedEventArgs e)
        {
            ViewModel?.FileInfoSection.OpenConfigInExplorer();
            UpdateStatusText(ViewModel?.FileInfoSection.StatusMessage);
        }

        private void OnCopyConfigPathClicked(object sender, RoutedEventArgs e)
        {
            ViewModel?.FileInfoSection.CopyConfigPath();
            UpdateStatusText(ViewModel?.FileInfoSection.StatusMessage);
        }

        private void OnOpenLogViewerClicked(object sender, RoutedEventArgs e)
        {
            ViewModel?.FileInfoSection.OpenLogViewer(this);
            UpdateStatusText(ViewModel?.FileInfoSection.StatusMessage);
        }

        // Section 3 actions
        private void OnCheckClipboardNowClicked(object sender, RoutedEventArgs e)
        {
            ViewModel?.ClipboardSection.CheckClipboardStatus(logToMonitor: true);
            UpdateStatusText(ViewModel?.ClipboardSection.StatusHeader);
        }

        private void OnToggleMonitoringClicked(object sender, RoutedEventArgs e)
        {
            ViewModel?.ClipboardSection.ToggleMonitoring();
            UpdateStatusText(ViewModel?.ClipboardSection.IsMonitoring == true ? "Clipboard loop monitor active" : "Monitor stopped");
        }

        private void OnClearClipboardLogClicked(object sender, RoutedEventArgs e)
        {
            ViewModel?.ClipboardSection.ClearLog();
            UpdateStatusText("Monitor log cleared");
        }

        // Section 4 actions
        private void OnReRegisterHotkeysClicked(object sender, RoutedEventArgs e)
        {
            ViewModel?.HotkeySection.ReRegisterHotkeys();
            UpdateStatusText(ViewModel?.HotkeySection.StatusMessage);
        }

        private void OnDisableOneDriveHotkeyClicked(object sender, RoutedEventArgs e)
        {
            ViewModel?.HotkeySection.DisableOneDriveHotkey();
            UpdateStatusText(ViewModel?.HotkeySection.StatusMessage);
        }

        private void OnDisableSnippingToolTakeoverClicked(object sender, RoutedEventArgs e)
        {
            ViewModel?.HotkeySection.DisableSnippingToolTakeover();
            UpdateStatusText(ViewModel?.HotkeySection.StatusMessage);
        }

        private void OnOpenWindowsKeyboardSettingsClicked(object sender, RoutedEventArgs e)
        {
            ViewModel?.HotkeySection.OpenWindowsKeyboardSettings();
            UpdateStatusText("Opened Windows Keyboard Settings");
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
                // Ignore if unsupported by OS
            }
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        /// <summary>
        /// Displays the Self-Service window safely, activating an existing instance if one is open.
        /// </summary>
        public static void ShowSelfService(Window owner = null, string initialSection = null)
        {
            void Display()
            {
                try
                {
                    if (_currentInstance != null && _currentInstance.IsLoaded)
                    {
                        if (!string.IsNullOrEmpty(initialSection))
                        {
                            _currentInstance.ViewModel?.SelectSection(initialSection);
                        }
                        if (_currentInstance.WindowState == WindowState.Minimized)
                        {
                            _currentInstance.WindowState = WindowState.Normal;
                        }
                        _currentInstance.Activate();
                        _currentInstance.Focus();
                        return;
                    }

                    var window = new SelfServiceWindow(initialSection);
                    if (owner != null && owner.IsLoaded && owner.IsVisible)
                    {
                        window.Owner = owner;
                    }
                    _currentInstance = window;
                    window.Show();
                }
                catch (Exception ex)
                {
                    Log.Error("Error opening SelfServiceWindow", ex);
                    MessageBox.Show(
                        $"Could not open Greenshot Self-Service:\n{ex.Message}",
                        "Greenshot - Self-Service",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }

            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                Display();
            }
            else
            {
                var staThread = new Thread(Display)
                {
                    Name = "GreenshotSelfServiceSTAThread",
                    IsBackground = true
                };
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();
            }
        }
    }
}
