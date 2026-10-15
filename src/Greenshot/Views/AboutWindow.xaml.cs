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
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Greenshot.Base.Core;
using Greenshot.Base.Wpf;
using Greenshot.Helpers;
using Greenshot.SelfService.Views;
using log4net;
using Greenshot.Base.Threading;
using Greenshot.Base.Languages;

namespace Greenshot.Views
{
    /// <summary>
    /// Modern WPF About window with the themed title bar, dark/light mode support, runtime translation updates, and XAML animations matching g.svg specification.
    /// </summary>
    public partial class AboutWindow : Window, INotifyPropertyChanged
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AboutWindow));

        public event PropertyChangedEventHandler PropertyChanged;

        // Theme brushes bound to XAML
        public SolidColorBrush WindowBackgroundBrush => WpfThemeHelper.WindowBackground;
        public SolidColorBrush CardBackgroundBrush => WpfThemeHelper.CardBackground;
        public SolidColorBrush CardBorderBrush => WpfThemeHelper.CardBorder;
        public SolidColorBrush TextPrimaryBrush => WpfThemeHelper.TextPrimary;
        public SolidColorBrush TextSecondaryBrush => WpfThemeHelper.TextSecondary;
        public SolidColorBrush AccentBrush => WpfThemeHelper.Accent;
        public SolidColorBrush BadgeBackgroundBrush => WpfThemeHelper.BadgeBackground;

        public string ThemeToggleIcon => WpfThemeHelper.IsDarkMode ? "☀️" : "🌙";
        public string ThemeToggleToolTip => WpfThemeHelper.IsDarkMode ? "Switch to Light Mode" : "Switch to Dark Mode";

        // Window G Icon Source
        public ImageSource WindowIconSource { get; }

        // Version & Metadata
        public string AppVersionTitle { get; }
        public string BitnessText { get; }
        public Visibility PortableBadgeVisibility => GreenshotEnvironment.IsPortable ? Visibility.Visible : Visibility.Collapsed;
        public Visibility BetaTesterBadgeVisibility => CoreConfiguration != null && CoreConfiguration.IsBetaTester ? Visibility.Visible : Visibility.Collapsed;

        // URLs & Display strings
        public string WebsiteUrl { get; }
        public string WebsiteDisplayUrl { get; } = "https://getgreenshot.org";
        public string BugsUrl { get; }
        public string DonationsUrl { get; }

        // Localized strings
        public string WindowTitleText { get; private set; }
        public string WindowTitleSubtitle { get; private set; }
        public string LicenseCopyrightText { get; private set; }
        public string HostLabelText { get; private set; }
        public string BugsLabelText { get; private set; }
        public string DonationsLabelText { get; private set; }
        public string IconsLabelText { get; private set; }
        public string TranslationCreditsText { get; private set; }
        public Visibility TranslationCreditsVisibility => string.IsNullOrWhiteSpace(TranslationCreditsText) ? Visibility.Collapsed : Visibility.Visible;
        public string SelfServiceButtonText { get; private set; }
        public string SelfServiceButtonToolTip { get; private set; }
        public string CloseButtonText { get; private set; }
        public string CloseButtonToolTip { get; private set; }

        private static ICoreConfiguration CoreConfiguration => IniConfigHelper.EnsureSection<ICoreConfiguration>(() => new CoreConfigurationImpl());

        public AboutWindow()
        {
            InitializeComponent();

            // Load window "G" icon
            try
            {
                using var gIcon = GreenshotResources.GetGreenshotIcon();
                if (gIcon != null)
                {
                    WindowIconSource = Imaging.CreateBitmapSourceFromHIcon(
                        gIcon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    Icon = WindowIconSource;
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Could not load Greenshot window icon", ex);
            }

            DataContext = this;

            var version = EnvironmentInfo.GetGreenshotVersion();
            var versionWithBuild = EnvironmentInfo.GetGreenshotVersion(true);

            AppVersionTitle = $"{GreenshotEdition.ProductName} {version}";
            BitnessText = $"{OsInfo.Bits}-bit";

            WebsiteUrl = $"https://getgreenshot.org/?version={versionWithBuild}";
            BugsUrl = $"https://getgreenshot.org/tickets/?version={versionWithBuild}";
            DonationsUrl = $"https://getgreenshot.org/support/?version={versionWithBuild}";

            // Populate all localized strings from language files
            InitializeLanguage();

            WpfThemeHelper.ThemeChanged += OnThemeChanged;
            Texts.Config.LanguageChanged += OnLanguageChanged;

            Loaded += OnWindowLoaded;
            Unloaded += OnWindowUnloaded;
            KeyDown += OnWindowKeyDown;
        }

        /// <summary>
        /// Populates localized resources from the active language configuration.
        /// </summary>
        protected virtual void InitializeLanguage()
        {
            WindowTitleText = Texts.Core.AboutTitle;
            WindowTitleSubtitle = $" - {WindowTitleText}";

            LicenseCopyrightText = Texts.Core.AboutLicense;
            HostLabelText = Texts.Core.AboutHost;
            BugsLabelText = Texts.Core.AboutBugs;
            DonationsLabelText = Texts.Core.AboutDonations;
            IconsLabelText = Texts.Core.AboutIcons;

            TranslationCreditsText = Texts.Core.AboutTranslation;
            SelfServiceButtonText = Texts.SelfService.ButtonText;
            SelfServiceButtonToolTip = Texts.SelfService.Tooltip;

            CloseButtonText = Texts.Core.BugreportCancel;
            CloseButtonToolTip = $"{CloseButtonText} (Esc)";

            OnPropertyChanged(nameof(WindowTitleText));
            OnPropertyChanged(nameof(WindowTitleSubtitle));
            OnPropertyChanged(nameof(LicenseCopyrightText));
            OnPropertyChanged(nameof(HostLabelText));
            OnPropertyChanged(nameof(BugsLabelText));
            OnPropertyChanged(nameof(DonationsLabelText));
            OnPropertyChanged(nameof(IconsLabelText));
            OnPropertyChanged(nameof(TranslationCreditsText));
            OnPropertyChanged(nameof(TranslationCreditsVisibility));
            OnPropertyChanged(nameof(SelfServiceButtonText));
            OnPropertyChanged(nameof(SelfServiceButtonToolTip));
            OnPropertyChanged(nameof(CloseButtonText));
            OnPropertyChanged(nameof(CloseButtonToolTip));
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            // The language can change on another thread
            UiDispatcher.Current.RunOnUiAsync(InitializeLanguage).FireAndLog("Initialize the language of the about window");
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            ApplyImmersiveDarkMode();
        }

        private void OnWindowUnloaded(object sender, RoutedEventArgs e)
        {
            WpfThemeHelper.ThemeChanged -= OnThemeChanged;
            Texts.Config.LanguageChanged -= OnLanguageChanged;
        }

        private void OnThemeChanged()
        {
            ApplyImmersiveDarkMode();
            NotifyAllPropertiesChanged();
        }

        private void NotifyAllPropertiesChanged()
        {
            OnPropertyChanged(nameof(WindowBackgroundBrush));
            OnPropertyChanged(nameof(CardBackgroundBrush));
            OnPropertyChanged(nameof(CardBorderBrush));
            OnPropertyChanged(nameof(TextPrimaryBrush));
            OnPropertyChanged(nameof(TextSecondaryBrush));
            OnPropertyChanged(nameof(AccentBrush));
            OnPropertyChanged(nameof(BadgeBackgroundBrush));
            OnPropertyChanged(nameof(ThemeToggleIcon));
            OnPropertyChanged(nameof(ThemeToggleToolTip));
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ApplyImmersiveDarkMode();
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnHyperlinkRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                Log.Error($"Error opening link '{e.Uri}'", ex);
                MessageBox.Show(this, string.Format(Texts.Core.ErrorOpenlink, e.Uri.AbsoluteUri),
                    Texts.Core.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnOpenSelfServiceClicked(object sender, RoutedEventArgs e)
        {
            OpenSelfService();
        }

        private void OpenSelfService(string sectionId = null)
        {
            SelfServiceWindow.ShowSelfService(this, sectionId);
        }

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;
                case Key.S:
                    OpenSelfService();
                    e.Handled = true;
                    break;
                case Key.E:
                    OpenSelfService("system");
                    e.Handled = true;
                    break;
                case Key.L:
                case Key.I:
                    OpenSelfService("files");
                    e.Handled = true;
                    break;
                case Key.T:
                    WpfThemeHelper.ToggleTheme();
                    e.Handled = true;
                    break;
            }
        }

        private void ShowEnvironmentInfo()
        {
            try
            {
                string info = EnvironmentInfo.EnvironmentToString(true);
                MessageBox.Show(this, info, "Greenshot - System Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error("Error displaying environment info", ex);
            }
        }

        private void OpenLogFile()
        {
            try
            {
                if (File.Exists(GreenshotMain.LogFileLocation))
                {
                    using (Process.Start(new ProcessStartInfo(GreenshotMain.LogFileLocation) { UseShellExecute = true }))
                    {
                    }
                }
                else
                {
                    MessageBox.Show(this, "Greenshot cannot find the log file, expected location: " + GreenshotMain.LogFileLocation,
                        "Log File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error opening log file", ex);
                MessageBox.Show(this, "Could not open greenshot.log: " + ex.Message,
                    "Error Opening Log", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenConfigFile()
        {
            try
            {
                if (File.Exists(GreenshotEnvironment.ConfigLocation))
                {
                    using (Process.Start(new ProcessStartInfo(GreenshotEnvironment.ConfigLocation) { UseShellExecute = true }))
                    {
                    }
                }
                else
                {
                    MessageBox.Show(this, "Greenshot cannot find greenshot.ini, expected location: " + GreenshotEnvironment.ConfigLocation,
                        "Settings File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error opening greenshot.ini", ex);
                MessageBox.Show(this, "Could not open greenshot.ini: " + ex.Message,
                    "Error Opening Settings", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyImmersiveDarkMode()
        {
            // The title bar in the colors of the theme, it follows theme changes from now on
            WindowFrameTheme.Attach(this);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

