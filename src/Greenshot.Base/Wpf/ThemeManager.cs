/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Threading;
using System.Windows;
using System.Windows.Media;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Threading;
using log4net;
using Microsoft.Win32;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// Follows the Windows theme: light or dark (or the user's choice in the Theme setting), the accent color and high contrast.
    /// WPF windows bind to the brushes here, they change when Windows' settings change.
    /// </summary>
    public class ThemeManager : INotifyPropertyChanged
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ThemeManager));
        private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        // The names of all brush properties: they change together with the palette
        private static readonly string[] BrushPropertyNames =
        {
            nameof(BackgroundBrush), nameof(ForegroundBrush), nameof(MutedBrush), nameof(BorderBrush), nameof(ControlBorderBrush),
            nameof(GroupBoxBrush), nameof(ControlBackgroundBrush), nameof(TextBoxBackgroundBrush), nameof(ScrollBarTrackBrush),
            nameof(ScrollBarThumbBrush), nameof(ScrollBarThumbHoverBrush), nameof(ScrollBarThumbPressedBrush), nameof(ButtonBackgroundBrush),
            nameof(ButtonHoverBrush), nameof(ButtonPressedBrush), nameof(HighlightForegroundBrush), nameof(TabItemBackgroundBrush),
            nameof(TabItemSelectedBrush), nameof(TitleBarBrush), nameof(AccentBrush), nameof(AccentForegroundBrush), nameof(WarningBrush),
            nameof(ErrorBrush)
        };

        // Thread-safe: WPF windows run on several threads, and a second instance would silently lose the subscribers of the first
        private static readonly Lazy<ThemeManager> LazyInstance = new Lazy<ThemeManager>(() => new ThemeManager(), LazyThreadSafetyMode.ExecutionAndPublication);

        private readonly object _paletteLock = new object();
        private ThemePalette _palette = ThemePalette.Light;
        private ThemePalette _taskbarPalette = ThemePalette.Light;
        private UiTheme _theme = UiTheme.System;
        private volatile UiTheme _appliedTheme = UiTheme.System;
        private ICoreConfiguration _coreConfiguration;
        private WindowsAccent _accent;

        public static ThemeManager Instance => LazyInstance.Value;

        public event PropertyChangedEventHandler PropertyChanged;

        private ThemeManager()
        {
            ComboBoxHelper.Initialize();
            try
            {
                _accent = new WindowsAccent();
                _accent.Changed += OnSystemThemeChanged;
            }
            catch (Exception ex)
            {
                // No WinRT (e.g. tests on an unusual system): the default accent is used
                Log.Debug("The Windows accent color isn't available", ex);
            }

            // Not the title bars yet: they ask this (still unfinished) instance for the theme
            Refresh(false);
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
            WindowFrameTheme.Register();
        }

        /// <summary>
        /// The palette of Greenshot's windows
        /// </summary>
        public ThemePalette CurrentPalette
        {
            get
            {
                lock (_paletteLock)
                {
                    return _palette;
                }
            }
        }

        /// <summary>
        /// The palette of the tray menu: Windows draws the taskbar and its menus in the light or dark mode of the system,
        /// which can differ from the one of apps (the "Custom" mode in the Windows settings)
        /// </summary>
        public ThemePalette TaskbarPalette
        {
            get
            {
                lock (_paletteLock)
                {
                    return _taskbarPalette;
                }
            }
        }

        /// <summary>
        /// The theme the user chose: follow Windows, or always light or dark. Stored in the Theme setting.
        /// </summary>
        public UiTheme Theme
        {
            get => CoreConfiguration?.Theme ?? _theme;
            set
            {
                _theme = value;
                var coreConfiguration = CoreConfiguration;
                if (coreConfiguration != null && coreConfiguration.Theme != value)
                {
                    coreConfiguration.Theme = value;
                }

                Refresh();
            }
        }

        /// <summary>
        /// True when Greenshot's windows are dark (a dark theme or a dark high contrast theme).
        /// Setting it chooses the light or dark theme instead of following Windows.
        /// </summary>
        public bool IsDarkTheme
        {
            get => CurrentPalette.IsDark;
            set => Theme = value ? UiTheme.Dark : UiTheme.Light;
        }

        /// <summary>
        /// True when Windows uses a high contrast theme, the colors are those the user chose for it
        /// </summary>
        public bool IsHighContrast => CurrentPalette.IsHighContrast;

        /// <summary>
        /// True when Greenshot's windows use the title bar of Windows: when following the Windows theme, and always with high contrast.
        /// With the light or dark theme the windows draw their own title bar in the theme colors, see <see cref="ThemedTitleBar"/>.
        /// </summary>
        public bool UseSystemTitleBar => Theme == UiTheme.System || IsHighContrast;

        /// <summary>
        /// Switch between light and dark, this stops following the Windows theme (it can be chosen again in the settings)
        /// </summary>
        public void ToggleTheme()
        {
            IsDarkTheme = !IsDarkTheme;
        }

        public Brush BackgroundBrush => CurrentPalette.BackgroundBrush;

        public Brush ForegroundBrush => CurrentPalette.ForegroundBrush;

        public Brush MutedBrush => CurrentPalette.MutedBrush;

        public Brush BorderBrush => CurrentPalette.BorderBrush;

        public Brush ControlBorderBrush => CurrentPalette.ControlBorderBrush;

        public Brush GroupBoxBrush => CurrentPalette.GroupBoxBrush;

        public Brush ControlBackgroundBrush => CurrentPalette.ControlBackgroundBrush;

        public Brush TextBoxBackgroundBrush => CurrentPalette.TextBoxBackgroundBrush;

        public Brush ScrollBarTrackBrush => CurrentPalette.ScrollBarTrackBrush;

        public Brush ScrollBarThumbBrush => CurrentPalette.ScrollBarThumbBrush;

        public Brush ScrollBarThumbHoverBrush => CurrentPalette.ScrollBarThumbHoverBrush;

        public Brush ScrollBarThumbPressedBrush => CurrentPalette.ScrollBarThumbPressedBrush;

        public Brush ButtonBackgroundBrush => CurrentPalette.ButtonBackgroundBrush;

        public Brush ButtonHoverBrush => CurrentPalette.ButtonHoverBrush;

        public Brush ButtonPressedBrush => CurrentPalette.ButtonPressedBrush;

        /// <summary>
        /// The text color on <see cref="ButtonHoverBrush"/> and <see cref="ButtonPressedBrush"/>
        /// </summary>
        public Brush HighlightForegroundBrush => CurrentPalette.HighlightForegroundBrush;

        public Brush TabItemBackgroundBrush => CurrentPalette.TabItemBackgroundBrush;

        public Brush TabItemSelectedBrush => CurrentPalette.TabItemSelectedBrush;

        public Brush TitleBarBrush => CurrentPalette.TitleBarBrush;

        public Brush AccentBrush => CurrentPalette.AccentBrush;

        /// <summary>
        /// The text color on <see cref="AccentBrush"/>, e.g. for the default button
        /// </summary>
        public Brush AccentForegroundBrush => CurrentPalette.AccentForegroundBrush;

        public Brush WarningBrush => CurrentPalette.WarningBrush;

        public Brush ErrorBrush => CurrentPalette.ErrorBrush;

        /// <summary>
        /// The core configuration with the Theme setting, null as long as it isn't loaded (e.g. in tests)
        /// </summary>
        private ICoreConfiguration CoreConfiguration
        {
            get
            {
                if (_coreConfiguration != null)
                {
                    return _coreConfiguration;
                }

                try
                {
                    var coreConfiguration = IniConfigRegistry.GetSection<ICoreConfiguration>();
                    if (coreConfiguration != null && Interlocked.CompareExchange(ref _coreConfiguration, coreConfiguration, null) == null)
                    {
                        coreConfiguration.PropertyChanged += OnCoreConfigurationChanged;
                    }
                }
                catch
                {
                    // Configuration might not be registered yet
                }

                return _coreConfiguration;
            }
        }

        private void OnCoreConfigurationChanged(object sender, PropertyChangedEventArgs e)
        {
            // Changed by hand in greenshot.ini, or by the Theme setter (then it's applied already)
            if (e.PropertyName == nameof(ICoreConfiguration.Theme) && _appliedTheme != ((ICoreConfiguration)sender).Theme)
            {
                UiDispatcher.Current.InvokeAsync(() => Refresh()).FireAndLog("Apply the theme setting");
            }
        }

        /// <summary>
        /// Read the Windows theme again and update the palettes, windows, title bars and menus follow
        /// </summary>
        public void Refresh()
        {
            Refresh(true);
        }

        private void Refresh(bool updateWindowFrames)
        {
            bool appsDark;
            bool systemDark;
            var theme = Theme;
            _appliedTheme = theme;
            switch (theme)
            {
                case UiTheme.Light:
                    appsDark = systemDark = false;
                    break;
                case UiTheme.Dark:
                    appsDark = systemDark = true;
                    break;
                default:
                    appsDark = !ReadPersonalizeFlag("AppsUseLightTheme");
                    systemDark = !ReadPersonalizeFlag("SystemUsesLightTheme");
                    break;
            }

            ThemePalette palette;
            ThemePalette taskbarPalette;
            if (System.Windows.Forms.SystemInformation.HighContrast)
            {
                // High contrast wins over everything: the user needs these colors
                palette = taskbarPalette = ThemePalette.CreateHighContrast();
            }
            else
            {
                palette = CreatePalette(appsDark);
                taskbarPalette = systemDark == appsDark ? palette : CreatePalette(systemDark);
            }

            bool wasDark = false;
            bool wasHighContrast = false;
            lock (_paletteLock)
            {
                // Windows reports many changes which don't touch the colors (e.g. "General" for most settings): nothing to redraw
                if (palette.HasSameColors(_palette) && taskbarPalette.HasSameColors(_taskbarPalette))
                {
                    palette = null;
                }
                else
                {
                    wasDark = _palette.IsDark;
                    wasHighContrast = _palette.IsHighContrast;
                    _palette = palette;
                    _taskbarPalette = taskbarPalette;
                }
            }

            OnPropertyChanged(nameof(Theme));
            // Also when the colors stay the same: "Same as Windows" and "Dark" on a dark Windows only differ in the title bar
            OnPropertyChanged(nameof(UseSystemTitleBar));
            if (palette == null)
            {
                return;
            }

            if (wasDark != palette.IsDark)
            {
                OnPropertyChanged(nameof(IsDarkTheme));
            }

            if (wasHighContrast != palette.IsHighContrast)
            {
                OnPropertyChanged(nameof(IsHighContrast));
            }

            foreach (var name in BrushPropertyNames)
            {
                OnPropertyChanged(name);
            }

            OnPropertyChanged(nameof(TaskbarPalette));
            // The last one: listeners which redraw by hand (see WpfThemeHelper.ThemeChanged) see all new values
            OnPropertyChanged(nameof(CurrentPalette));
            if (updateWindowFrames)
            {
                WindowFrameTheme.UpdateAll();
            }
        }

        private ThemePalette CreatePalette(bool isDark)
        {
            var accent = _accent?.GetAccent(isDark) ?? (isDark ? ThemePalette.DefaultDarkAccent : ThemePalette.DefaultLightAccent);
            return ThemePalette.Create(isDark, accent);
        }

        /// <summary>
        /// A light/dark flag of the Windows personalization settings, true (light) when it can't be read
        /// </summary>
        private static bool ReadPersonalizeFlag(string name)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
                return !(key?.GetValue(name) is int intValue && intValue == 0);
            }
            catch
            {
                return true;
            }
        }

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            switch (e.Category)
            {
                case UserPreferenceCategory.General:
                case UserPreferenceCategory.Color:
                case UserPreferenceCategory.Accessibility:
                case UserPreferenceCategory.VisualStyle:
                    OnSystemThemeChanged();
                    break;
            }
        }

        private void OnSystemThemeChanged()
        {
            // Raised on a system events or WinRT thread
            UiDispatcher.Current.InvokeAsync(() => Refresh()).FireAndLog("Detect the system theme");
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ResourceDictionary GetThemeResources()
        {
            var dict = new ResourceDictionary();
            dict.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("/Greenshot.Base;component/Wpf/Styles/ScrollBarStyles.xaml", UriKind.Relative)
            });
            dict.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("/Greenshot.Base;component/Wpf/Styles/ListViewStyles.xaml", UriKind.Relative)
            });
            
            dict["ThemeBackgroundBrush"] = BackgroundBrush;
            dict["ThemeForegroundBrush"] = ForegroundBrush;
            dict["ThemeMutedBrush"] = MutedBrush;
            dict["ThemeBorderBrush"] = BorderBrush;
            dict["ThemeControlBorderBrush"] = ControlBorderBrush;
            dict["ThemeGroupBoxBrush"] = GroupBoxBrush;
            dict["ThemeControlBackgroundBrush"] = ControlBackgroundBrush;
            dict["ThemeTextBoxBackgroundBrush"] = TextBoxBackgroundBrush;
            dict["ThemeButtonBackgroundBrush"] = ButtonBackgroundBrush;
            dict["ThemeButtonHoverBrush"] = ButtonHoverBrush;
            dict["ThemeButtonPressedBrush"] = ButtonPressedBrush;
            dict["ThemeHighlightForegroundBrush"] = HighlightForegroundBrush;
            dict["ThemeTitleBarBrush"] = TitleBarBrush;
            dict["ThemeAccentBrush"] = AccentBrush;
            dict["ThemeAccentForegroundBrush"] = AccentForegroundBrush;
            dict["ThemeWarningBrush"] = WarningBrush;
            dict["ThemeErrorBrush"] = ErrorBrush;
            
            return dict;
        }
    }
}
