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
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// Manages theme switching between dark and light modes
    /// </summary>
    public class ThemeManager : INotifyPropertyChanged
    {
        private static ThemeManager _instance;
        private bool _isDarkTheme;

        public static ThemeManager Instance => _instance ??= new ThemeManager();

        public event PropertyChangedEventHandler PropertyChanged;

        public ThemePalette CurrentPalette => _isDarkTheme ? ThemePalette.Dark : ThemePalette.Light;

        private ThemeManager()
        {
            ComboBoxHelper.Initialize();
            DetectSystemTheme();
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (_isDarkTheme != value)
                {
                    _isDarkTheme = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDarkTheme)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentPalette)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BackgroundBrush)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ForegroundBrush)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MutedBrush)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BorderBrush)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ControlBorderBrush)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GroupBoxBrush)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ControlBackgroundBrush)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TextBoxBackgroundBrush)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ButtonBackgroundBrush)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ButtonHoverBrush)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ButtonPressedBrush)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TabItemBackgroundBrush)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TabItemSelectedBrush)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TitleBarBrush)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AccentBrush)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WarningBrush)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ErrorBrush)));
                }
            }
        }

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

        public Brush ButtonBackgroundBrush => CurrentPalette.ButtonBackgroundBrush;

        public Brush ButtonHoverBrush => CurrentPalette.ButtonHoverBrush;

        public Brush ButtonPressedBrush => CurrentPalette.ButtonPressedBrush;

        public Brush TabItemBackgroundBrush => CurrentPalette.TabItemBackgroundBrush;

        public Brush TabItemSelectedBrush => CurrentPalette.TabItemSelectedBrush;

        public Brush TitleBarBrush => CurrentPalette.TitleBarBrush;

        public Brush AccentBrush => CurrentPalette.AccentBrush;

        public Brush WarningBrush => CurrentPalette.WarningBrush;

        public Brush ErrorBrush => CurrentPalette.ErrorBrush;

        private void DetectSystemTheme()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    var value = key?.GetValue("AppsUseLightTheme");
                    IsDarkTheme = value is int intValue && intValue == 0;
                }
            }
            catch
            {
                // Default to light theme if we can't detect
                IsDarkTheme = false;
            }
        }

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                Application.Current?.Dispatcher.Invoke(() => DetectSystemTheme());
            }
        }

        public ResourceDictionary GetThemeResources()
        {
            var dict = new ResourceDictionary();
            
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
            dict["ThemeTitleBarBrush"] = TitleBarBrush;
            dict["ThemeAccentBrush"] = AccentBrush;
            dict["ThemeWarningBrush"] = WarningBrush;
            dict["ThemeErrorBrush"] = ErrorBrush;
            
            return dict;
        }
    }
}
