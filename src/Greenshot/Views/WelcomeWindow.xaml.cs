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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;
using Greenshot.Base.Threading;
using Greenshot.Base.Wpf;
using Greenshot.SelfService.ViewModels;
using Greenshot.ViewModels;
using log4net;

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
        private readonly Random _rand = new();
        private int _tipIndex;
        // The steps of the wizard, in their order; a new step is a panel in the XAML and an entry here
        private readonly FrameworkElement[] _steps;
        private int _currentStep;

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
        public IReadOnlyList<WelcomeHotkeyViewModel> Basics { get; private set; }

        public string CurrentTip { get; private set; }

        /// <summary>
        /// The checks of the "Your setup" step, the same as in the Self-Service window
        /// </summary>
        public SetupSectionViewModel Setup { get; } = new();

        /// <summary>
        /// One dot per step, the current one in the accent color
        /// </summary>
        public IReadOnlyList<SolidColorBrush> StepDots =>
            _steps.Select((_, index) => index == _currentStep ? AccentBrush : BadgeBackgroundBrush).ToList();

        public Visibility BackVisibility => _currentStep > 0 ? Visibility.Visible : Visibility.Hidden;

        public string NextText => _currentStep == _steps.Length - 1 ? Texts.Welcome.ButtonStart : Texts.Welcome.ButtonNext;

        private static ICoreConfiguration CoreConfiguration => IniConfigHelper.EnsureSection<ICoreConfiguration>(() => new CoreConfigurationImpl());

        public WelcomeWindow()
        {
            InitializeComponent();
            _steps = new FrameworkElement[] { StepWelcome, StepBasics, StepSetup, StepDone };
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
        /// The tips of the language file, a new one needs a key there, a property in IWelcomeLanguage and an entry here
        /// </summary>
        private static string[] Tips()
        {
            var texts = Texts.Welcome;
            var tips = new List<string>
            {
                texts.Tip1, texts.Tip2, texts.Tip3, texts.Tip4, texts.Tip5,
                texts.Tip6, texts.Tip7, texts.Tip8, texts.Tip9, texts.Tip10
            };
            // Only where a notification offers its capture again: not in Greenshot Light, the portable version or without toasts
            if (SimpleServiceProvider.Current.GetInstance<INotificationService>(isOptional: true)?.ShowsCaptures == true)
            {
                tips.Add(texts.TipNotification);
            }

            return tips.Where(tip => !string.IsNullOrWhiteSpace(tip)).ToArray();
        }

        private void InitializeLanguage()
        {
            var texts = Texts.Welcome;
            var config = CoreConfiguration;
            Basics = new[]
                {
                    new WelcomeHotkeyViewModel(config.RegionHotkey, texts.BasicsRegion),
                    new WelcomeHotkeyViewModel(config.WindowHotkey, texts.BasicsWindow),
                    new WelcomeHotkeyViewModel(config.FullscreenHotkey, texts.BasicsFullscreen),
                    new WelcomeHotkeyViewModel(config.LastregionHotkey, texts.BasicsLastregion)
                }
                .Where(basic => basic.Keys != null)
                .ToList();
            ShowTip(_tipIndex);
            OnPropertyChanged(nameof(Basics));
            OnPropertyChanged(nameof(NextText));
        }

        private void ShowStep(int step)
        {
            _currentStep = Math.Max(0, Math.Min(step, _steps.Length - 1));
            for (int index = 0; index < _steps.Length; index++)
            {
                // Hidden keeps the size of the window the same for every step
                _steps[index].Visibility = index == _currentStep ? Visibility.Visible : Visibility.Hidden;
            }

            OnPropertyChanged(nameof(StepDots));
            OnPropertyChanged(nameof(BackVisibility));
            OnPropertyChanged(nameof(NextText));
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
            Setup.Refresh();
        }

        private void OnShowTrayIconClicked(object sender, RoutedEventArgs e)
        {
            Setup.ShowTrayIcon();
        }

        private void OnFixSnippingToolClicked(object sender, RoutedEventArgs e)
        {
            Setup.FixSnippingTool();
        }

        private void OnFixOneDriveClicked(object sender, RoutedEventArgs e)
        {
            Setup.FixOneDrive();
        }

        private void OnNextTipClicked(object sender, RoutedEventArgs e)
        {
            ShowTip(_tipIndex + 1);
        }

        private void OnSettingsClicked(object sender, RoutedEventArgs e)
        {
            SimpleServiceProvider.Current.GetInstance<IGreenshotShell>()?.ShowSetting();
        }

        private void OnBackClicked(object sender, RoutedEventArgs e)
        {
            ShowStep(_currentStep - 1);
        }

        private void OnNextClicked(object sender, RoutedEventArgs e)
        {
            if (_currentStep == _steps.Length - 1)
            {
                Close();
                return;
            }

            ShowStep(_currentStep + 1);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape)
            {
                Close();
            }
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
            OnPropertyChanged(nameof(StepDots));
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
}
