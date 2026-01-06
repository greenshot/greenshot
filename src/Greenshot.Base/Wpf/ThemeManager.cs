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

        private ThemeManager()
        {
            DetectSystemTheme();
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            private set
            {
                if (_isDarkTheme != value)
                {
                    _isDarkTheme = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDarkTheme)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BackgroundBrush)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ForegroundBrush)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BorderBrush)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GroupBoxBrush)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TextBoxBackgroundBrush)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ButtonBackgroundBrush)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ButtonHoverBrush)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ButtonPressedBrush)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TabItemBackgroundBrush)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TabItemSelectedBrush)));
                }
            }
        }

        public Brush BackgroundBrush => _isDarkTheme 
            ? new SolidColorBrush(Color.FromRgb(32, 32, 32)) 
            : new SolidColorBrush(Color.FromRgb(245, 245, 245));

        public Brush ForegroundBrush => _isDarkTheme 
            ? new SolidColorBrush(Color.FromRgb(240, 240, 240)) 
            : new SolidColorBrush(Color.FromRgb(30, 30, 30));

        public Brush BorderBrush => _isDarkTheme 
            ? new SolidColorBrush(Color.FromRgb(70, 70, 70)) 
            : new SolidColorBrush(Color.FromRgb(200, 200, 200));

        public Brush GroupBoxBrush => _isDarkTheme 
            ? new SolidColorBrush(Color.FromRgb(42, 42, 42)) 
            : new SolidColorBrush(Colors.White);

        public Brush TextBoxBackgroundBrush => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(24, 24, 24))
            : new SolidColorBrush(Colors.White);

        public Brush ButtonBackgroundBrush => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(55, 55, 55))
            : new SolidColorBrush(Color.FromRgb(230, 230, 230));

        public Brush ButtonHoverBrush => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(70, 70, 70))
            : new SolidColorBrush(Color.FromRgb(210, 210, 210));

        public Brush ButtonPressedBrush => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(45, 45, 45))
            : new SolidColorBrush(Color.FromRgb(190, 190, 190));

        public Brush TabItemBackgroundBrush => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(50, 50, 50))
            : new SolidColorBrush(Color.FromRgb(220, 220, 220));

        public Brush TabItemSelectedBrush => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(42, 42, 42))
            : new SolidColorBrush(Colors.White);

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
            dict["ThemeBorderBrush"] = BorderBrush;
            dict["ThemeGroupBoxBrush"] = GroupBoxBrush;
            
            return dict;
        }
    }
}
