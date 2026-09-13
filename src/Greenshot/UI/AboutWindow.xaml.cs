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
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Greenshot.Base.Core;
using Greenshot.Configuration;
using log4net;
using Lang = Greenshot.Base.Core.Language;
using Path = System.Windows.Shapes.Path;

namespace Greenshot.UI
{
    /// <summary>
    /// Modern WPF About window with custom WindowChrome, dark/light mode support, runtime translation updates, and XAML animations matching g.svg specification.
    /// </summary>
    public partial class AboutWindow : Window, INotifyPropertyChanged
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AboutWindow));
        private readonly Random _rand = new();
        private readonly DispatcherTimer _highlightTimer;
        private Storyboard _startupStoryboard;
        private Storyboard _waveStoryboard;
        private readonly Path[] _allDots;
        private readonly SolidColorBrush[] _allDotBrushes;
        private readonly ScaleTransform[] _allDotScales;

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

            AppVersionTitle = $"Greenshot {version}";
            BitnessText = $"{OsInfo.Bits}-bit";

            WebsiteUrl = $"https://getgreenshot.org/?version={versionWithBuild}";
            BugsUrl = $"https://getgreenshot.org/tickets/?version={versionWithBuild}";
            DonationsUrl = $"https://getgreenshot.org/support/?version={versionWithBuild}";

            // Populate all localized strings from language files
            InitializeLanguage();

            _allDots = new[]
            {
                Dot0, Dot1, Dot2, Dot3, Dot4, Dot5, Dot6,
                Dot7, Dot8, Dot9, Dot10, Dot11, Dot12, Dot13,
                Dot14, Dot15, Dot16, Dot17, Dot18, Dot19, Dot20
            };

            _allDotBrushes = new[]
            {
                Dot0Brush, Dot1Brush, Dot2Brush, Dot3Brush, Dot4Brush, Dot5Brush, Dot6Brush,
                Dot7Brush, Dot8Brush, Dot9Brush, Dot10Brush, Dot11Brush, Dot12Brush, Dot13Brush,
                Dot14Brush, Dot15Brush, Dot16Brush, Dot17Brush, Dot18Brush, Dot19Brush, Dot20Brush
            };

            _allDotScales = new[]
            {
                Dot0Scale, Dot1Scale, Dot2Scale, Dot3Scale, Dot4Scale, Dot5Scale, Dot6Scale,
                Dot7Scale, Dot8Scale, Dot9Scale, Dot10Scale, Dot11Scale, Dot12Scale, Dot13Scale,
                Dot14Scale, Dot15Scale, Dot16Scale, Dot17Scale, Dot18Scale, Dot19Scale, Dot20Scale
            };

            // Setup highlight timer for periodic animations
            _highlightTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4.0)
            };
            _highlightTimer.Tick += OnHighlightTimerTick;

            WpfThemeHelper.ThemeChanged += OnThemeChanged;
            Lang.LanguageChanged += OnLanguageChanged;

            Loaded += OnWindowLoaded;
            Unloaded += OnWindowUnloaded;
            KeyDown += OnWindowKeyDown;
        }

        /// <summary>
        /// Populates localized resources from the active language configuration.
        /// </summary>
        protected virtual void InitializeLanguage()
        {
            string title = Lang.GetString("about_title");
            WindowTitleText = string.IsNullOrWhiteSpace(title) || title.StartsWith("###") ? "About Greenshot" : title;
            WindowTitleSubtitle = $" - {WindowTitleText}";

            LicenseCopyrightText = Lang.GetString("about_license");
            HostLabelText = Lang.GetString("about_host");
            BugsLabelText = Lang.GetString("about_bugs");
            DonationsLabelText = Lang.GetString("about_donations");
            IconsLabelText = Lang.GetString("about_icons");

            string translation = Lang.GetString("about_translation");
            TranslationCreditsText = string.IsNullOrWhiteSpace(translation) || translation.StartsWith("###") ? null : translation;

            string close = Lang.GetString("bugreport_cancel");
            CloseButtonText = string.IsNullOrWhiteSpace(close) || close.StartsWith("###") ? "Close" : close;
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
            OnPropertyChanged(nameof(CloseButtonText));
            OnPropertyChanged(nameof(CloseButtonToolTip));
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (Dispatcher.CheckAccess())
            {
                InitializeLanguage();
            }
            else
            {
                Dispatcher.Invoke(InitializeLanguage);
            }
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            ApplyImmersiveDarkMode();
            System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(this);

            _startupStoryboard = TryFindResource("StartupStoryboard") as Storyboard;
            _waveStoryboard = TryFindResource("WaveStoryboard") as Storyboard;

            if (_startupStoryboard != null)
            {
                _startupStoryboard.Completed += (s, args) =>
                {
                    _highlightTimer.Interval = TimeSpan.FromMilliseconds(2500 + _rand.Next(3000));
                    _highlightTimer.Start();
                };
                _startupStoryboard.Begin(this);
            }
            else
            {
                _highlightTimer.Start();
            }
        }

        private void OnWindowUnloaded(object sender, RoutedEventArgs e)
        {
            WpfThemeHelper.ThemeChanged -= OnThemeChanged;
            Lang.LanguageChanged -= OnLanguageChanged;
            _highlightTimer.Stop();
        }

        private void OnHighlightTimerTick(object sender, EventArgs e)
        {
            // Reset timer with next randomized interval (between 3.5s and 7s)
            _highlightTimer.Interval = TimeSpan.FromMilliseconds(3500 + _rand.Next(3500));

            // Randomly choose between full flow wave or sparkle pulses on a random subset of bubbles
            int animationMode = _rand.Next(3);
            if (animationMode == 0 && _waveStoryboard != null)
            {
                _waveStoryboard.Begin(this);
            }
            else
            {
                HighlightRandomBubbles();
            }
        }

        private void HighlightRandomBubbles()
        {
            int count = _rand.Next(2, 6);
            for (int i = 0; i < count; i++)
            {
                int index = _rand.Next(_allDots.Length);
                var brush = _allDotBrushes[index];
                var scale = _allDotScales[index];
                if (brush == null || scale == null) continue;

                var colorAnim = new ColorAnimationUsingKeyFrames
                {
                    BeginTime = TimeSpan.FromMilliseconds(i * 120)
                };
                colorAnim.KeyFrames.Add(new SplineColorKeyFrame(System.Windows.Media.Color.FromRgb(138, 255, 0), KeyTime.FromTimeSpan(TimeSpan.Zero)));
                colorAnim.KeyFrames.Add(new SplineColorKeyFrame(Colors.White, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.2))));
                colorAnim.KeyFrames.Add(new SplineColorKeyFrame(System.Windows.Media.Color.FromRgb(138, 255, 0), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.45))));

                var scaleAnim = new DoubleAnimationUsingKeyFrames
                {
                    BeginTime = TimeSpan.FromMilliseconds(i * 120)
                };
                scaleAnim.KeyFrames.Add(new SplineDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
                scaleAnim.KeyFrames.Add(new SplineDoubleKeyFrame(1.35, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.2))));
                scaleAnim.KeyFrames.Add(new SplineDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.45))));

                brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            }
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

        private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void OnThemeToggleClicked(object sender, RoutedEventArgs e)
        {
            WpfThemeHelper.ToggleTheme();
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
                MessageBox.Show(this, Lang.GetFormattedString(LangKey.error_openlink, e.Uri.AbsoluteUri),
                    Lang.GetString(LangKey.error), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnEnvironmentInfoClicked(object sender, RoutedEventArgs e)
        {
            ShowEnvironmentInfo();
        }

        private void OnViewLogClicked(object sender, RoutedEventArgs e)
        {
            OpenLogFile();
        }

        private void OnViewSettingsClicked(object sender, RoutedEventArgs e)
        {
            OpenConfigFile();
        }

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;
                case Key.E:
                    ShowEnvironmentInfo();
                    e.Handled = true;
                    break;
                case Key.L:
                    OpenLogFile();
                    e.Handled = true;
                    break;
                case Key.I:
                    OpenConfigFile();
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
                // Silently ignore if DWM call is unsupported
            }
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

