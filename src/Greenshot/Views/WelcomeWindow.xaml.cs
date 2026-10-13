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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;
using Greenshot.Base.Threading;
using Greenshot.Base.Wpf;
using Greenshot.SelfService.ViewModels;
using log4net;
using Microsoft.Win32;

namespace Greenshot.Views
{
    /// <summary>
    /// The welcome window: Greenshot says thank you at its first start, explains the basics with the user's own hotkeys,
    /// points out what keeps it from working as expected and shows a tip.
    /// </summary>
    public partial class WelcomeWindow : Window, INotifyPropertyChanged
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WelcomeWindow));
        private static WelcomeWindow _currentInstance;
        private readonly HotkeySectionViewModel _hotkeyChecks = new();
        private readonly Random _rand = new();
        private int _tipIndex;

        public event PropertyChangedEventHandler PropertyChanged;

        // Theme brushes bound to XAML
        public SolidColorBrush WindowBackgroundBrush => WpfThemeHelper.WindowBackground;
        public SolidColorBrush CardBackgroundBrush => WpfThemeHelper.CardBackground;
        public SolidColorBrush CardBorderBrush => WpfThemeHelper.CardBorder;
        public SolidColorBrush TextPrimaryBrush => WpfThemeHelper.TextPrimary;
        public SolidColorBrush TextSecondaryBrush => WpfThemeHelper.TextSecondary;
        public SolidColorBrush AccentBrush => WpfThemeHelper.Accent;
        public SolidColorBrush BadgeBackgroundBrush => WpfThemeHelper.BadgeBackground;
        public SolidColorBrush WarningTextBrush => WpfThemeHelper.WarningText;

        /// <summary>
        /// The capture hotkeys of the user with what they do
        /// </summary>
        public IReadOnlyList<WelcomeBasic> Basics { get; private set; }

        public string CurrentTip { get; private set; }

        public bool IsTrayIconHidden { get; private set; }

        public Visibility TrayIconHiddenVisibility => IsTrayIconHidden ? Visibility.Visible : Visibility.Collapsed;
        public Visibility SnippingToolVisibility => _hotkeyChecks.IsSnippingToolHijackEnabled ? Visibility.Visible : Visibility.Collapsed;
        public Visibility OneDriveVisibility => _hotkeyChecks.IsOneDriveBlocking ? Visibility.Visible : Visibility.Collapsed;

        public Visibility SetupOkVisibility =>
            IsTrayIconHidden || _hotkeyChecks.IsSnippingToolHijackEnabled || _hotkeyChecks.IsOneDriveBlocking ? Visibility.Collapsed : Visibility.Visible;

        private static ICoreConfiguration CoreConfiguration => IniConfigHelper.EnsureSection<ICoreConfiguration>(() => new CoreConfigurationImpl());

        public WelcomeWindow()
        {
            InitializeComponent();
            DataContext = this;

            _tipIndex = _rand.Next(Tips().Length);
            InitializeLanguage();

            WpfThemeHelper.ThemeChanged += OnThemeChanged;
            Texts.Config.LanguageChanged += OnLanguageChanged;
            Unloaded += OnWindowUnloaded;
        }

        /// <summary>
        /// Show the welcome window, or bring it to the front when it's open already
        /// </summary>
        public static void ShowWelcome()
        {
            void Display()
            {
                try
                {
                    if (_currentInstance != null && _currentInstance.IsLoaded)
                    {
                        _currentInstance.Activate();
                        return;
                    }

                    _currentInstance = new WelcomeWindow();
                    _currentInstance.Show();
                    _currentInstance.Activate();
                }
                catch (Exception ex)
                {
                    Log.Error("Error opening the welcome window", ex);
                }
            }

            // All windows live on the one UI thread
            UiDispatcher.Current.RunOnUiAsync(Display).FireAndLog("Show the welcome window", Log);
        }

        /// <summary>
        /// The tips of the language file, a new one only needs a key there and a property in IWelcomeLanguage
        /// </summary>
        private static string[] Tips()
        {
            var texts = Texts.Welcome;
            return new[]
            {
                texts.Tip1, texts.Tip2, texts.Tip3, texts.Tip4, texts.Tip5,
                texts.Tip6, texts.Tip7, texts.Tip8, texts.Tip9, texts.Tip10
            }.Where(tip => !string.IsNullOrWhiteSpace(tip)).ToArray();
        }

        private void InitializeLanguage()
        {
            var texts = Texts.Welcome;
            var config = CoreConfiguration;
            Basics = new[]
                {
                    new WelcomeBasic(config.RegionHotkey, texts.BasicsRegion),
                    new WelcomeBasic(config.WindowHotkey, texts.BasicsWindow),
                    new WelcomeBasic(config.FullscreenHotkey, texts.BasicsFullscreen),
                    new WelcomeBasic(config.LastregionHotkey, texts.BasicsLastregion)
                }
                .Where(basic => basic.Keys != null)
                .ToList();
            ShowTip(_tipIndex);
            OnPropertyChanged(nameof(Basics));
        }

        private void ShowTip(int index)
        {
            var tips = Tips();
            _tipIndex = tips.Length == 0 ? 0 : index % tips.Length;
            CurrentTip = tips.Length == 0 ? string.Empty : tips[_tipIndex];
            OnPropertyChanged(nameof(CurrentTip));
        }

        /// <summary>
        /// Check the setup again, every time the window is activated: the user may come back from the Windows settings
        /// </summary>
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            RefreshSetup();
        }

        private void RefreshSetup()
        {
            try
            {
                IsTrayIconHidden = IsHiddenInOverflow(Registry.CurrentUser, System.Windows.Forms.Application.ExecutablePath);
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't check if the notification area icon is shown", ex);
                IsTrayIconHidden = false;
            }

            _hotkeyChecks.CheckSnippingToolStatus();
            _hotkeyChecks.CheckOneDriveStatus();
            OnPropertyChanged(nameof(IsTrayIconHidden));
            OnPropertyChanged(nameof(TrayIconHiddenVisibility));
            OnPropertyChanged(nameof(SnippingToolVisibility));
            OnPropertyChanged(nameof(OneDriveVisibility));
            OnPropertyChanged(nameof(SetupOkVisibility));
        }

        /// <summary>
        /// Windows 11 remembers per notification icon if it's shown next to the clock (IsPromoted 1) or hidden in the overflow,
        /// in HKCU\Control Panel\NotifyIconSettings. A new icon is hidden until the user shows it. Only read here, the choice is the user's.
        /// Windows 10 has no such key, there it's never reported as hidden.
        /// </summary>
        /// <param name="currentUser">HKEY_CURRENT_USER</param>
        /// <param name="executablePath">The path of Greenshot.exe</param>
        /// <returns>True when Greenshot has an icon there and none of its icons is shown</returns>
        internal static bool IsHiddenInOverflow(RegistryKey currentUser, string executablePath)
        {
            using var notifyIconSettings = currentUser.OpenSubKey(@"Control Panel\NotifyIconSettings");
            if (notifyIconSettings == null)
            {
                return false;
            }

            bool found = false;
            foreach (var iconKeyName in notifyIconSettings.GetSubKeyNames())
            {
                using var iconKey = notifyIconSettings.OpenSubKey(iconKeyName);
                if (iconKey?.GetValue("ExecutablePath") is not string registeredPath || !IsSameExecutable(registeredPath, executablePath))
                {
                    continue;
                }

                // An older version may have left an icon with another id, one shown icon is enough
                if (iconKey.GetValue("IsPromoted") is int isPromoted && isPromoted == 1)
                {
                    return false;
                }
                found = true;
            }

            return found;
        }

        /// <summary>
        /// The path of an executable in a known folder starts with the GUID of the folder,
        /// e.g. {6D809377-6AF0-444B-8957-A3773F02200E}\Greenshot\Greenshot.exe for C:\Program Files\Greenshot\Greenshot.exe
        /// </summary>
        internal static bool IsSameExecutable(string registeredPath, string executablePath)
        {
            if (string.IsNullOrEmpty(registeredPath) || string.IsNullOrEmpty(executablePath))
            {
                return false;
            }

            int knownFolderEnd = registeredPath.StartsWith("{", StringComparison.Ordinal) ? registeredPath.IndexOf('}') : -1;
            if (knownFolderEnd > 0)
            {
                return executablePath.EndsWith(registeredPath.Substring(knownFolderEnd + 1), StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(registeredPath, executablePath, StringComparison.OrdinalIgnoreCase);
        }

        private void OnOpenTaskbarSettingsClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("ms-settings:taskbar") { UseShellExecute = true })?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't open the taskbar settings", ex);
            }
        }

        private void OnFixSnippingToolClicked(object sender, RoutedEventArgs e)
        {
            _hotkeyChecks.DisableSnippingToolTakeover();
            RefreshSetup();
        }

        private void OnFixOneDriveClicked(object sender, RoutedEventArgs e)
        {
            _hotkeyChecks.DisableOneDriveHotkey();
            RefreshSetup();
        }

        private void OnNextTipClicked(object sender, RoutedEventArgs e)
        {
            ShowTip(_tipIndex + 1);
        }

        private void OnSettingsClicked(object sender, RoutedEventArgs e)
        {
            SimpleServiceProvider.Current.GetInstance<IGreenshotShell>()?.ShowSetting();
        }

        private void OnStartClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            // The language can change on another thread
            UiDispatcher.Current.RunOnUiAsync(InitializeLanguage).FireAndLog("Initialize the language of the welcome window");
        }

        private void OnThemeChanged()
        {
            OnPropertyChanged(nameof(WindowBackgroundBrush));
            OnPropertyChanged(nameof(CardBackgroundBrush));
            OnPropertyChanged(nameof(CardBorderBrush));
            OnPropertyChanged(nameof(TextPrimaryBrush));
            OnPropertyChanged(nameof(TextSecondaryBrush));
            OnPropertyChanged(nameof(AccentBrush));
            OnPropertyChanged(nameof(BadgeBackgroundBrush));
            OnPropertyChanged(nameof(WarningTextBrush));
        }

        private void OnWindowUnloaded(object sender, RoutedEventArgs e)
        {
            WpfThemeHelper.ThemeChanged -= OnThemeChanged;
            Texts.Config.LanguageChanged -= OnLanguageChanged;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // The title bar in the colors of the theme, it follows theme changes from now on
            WindowFrameTheme.Attach(this);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// A hotkey of the user and what it does, for the basics of the welcome window
    /// </summary>
    public sealed class WelcomeBasic
    {
        public WelcomeBasic(string hotkey, string description)
        {
            string keys = HotkeyManager.GetLocalizedHotkeyStringFromString(hotkey);
            Keys = keys == "None" ? null : keys;
            Description = description;
        }

        /// <summary>
        /// The keys to press, null when there is no hotkey for it
        /// </summary>
        public string Keys { get; }

        public string Description { get; }
    }
}
