/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Greenshot.Base.Core;

namespace Greenshot.Helpers
{
    public static class ThemeManager
    {
        public static bool IsDarkModeEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        object value = key.GetValue("AppsUseLightTheme");
                        if (value is int i)
                        {
                            return i == 0;
                        }
                    }
                }
            }
            catch
            {
                // Fallback to light mode
            }
            return false;
        }

        public static void ApplyTheme(Window window)
        {
            bool isDark = IsDarkModeEnabled();
            
            // Add application icon
            try
            {
                using (var icon = GreenshotResources.GetGreenshotIcon())
                {
                    if (icon != null)
                    {
                        window.Icon = Imaging.CreateBitmapSourceFromHIcon(
                            icon.Handle,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                    }
                }
            }
            catch
            {
                // Ignore icon loading errors
            }

            if (isDark)
            {
                window.Resources["WindowBackground"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                window.Resources["CardBackground"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
                window.Resources["PrimaryText"] = Brushes.White;
                window.Resources["SecondaryText"] = new SolidColorBrush(Color.FromRgb(180, 180, 180));
                window.Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(60, 60, 60));
                window.Resources["InputBackground"] = new SolidColorBrush(Color.FromRgb(40, 40, 40));
                window.Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0, 120, 215));
                window.Resources["ButtonBackground"] = new SolidColorBrush(Color.FromRgb(60, 60, 60));
                window.Resources["ButtonForeground"] = Brushes.White;
                window.Resources["WarningBackground"] = new SolidColorBrush(Color.FromRgb(60, 50, 0));
                window.Resources["WarningForeground"] = Brushes.White;
                window.Resources["WarningBorder"] = new SolidColorBrush(Color.FromRgb(150, 120, 0));
                window.Resources["TitleBarBackground"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
                window.Resources["TitleBarButtonHover"] = new SolidColorBrush(Color.FromRgb(60, 60, 60));
                window.Resources["TitleBarCloseHover"] = new SolidColorBrush(Color.FromRgb(196, 43, 28));
            }
            else
            {
                window.Resources["WindowBackground"] = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                window.Resources["CardBackground"] = Brushes.White;
                window.Resources["PrimaryText"] = new SolidColorBrush(Color.FromRgb(33, 33, 33));
                window.Resources["SecondaryText"] = new SolidColorBrush(Color.FromRgb(117, 117, 117));
                window.Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                window.Resources["InputBackground"] = new SolidColorBrush(Color.FromRgb(250, 250, 250));
                window.Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(211, 47, 47)); // Keeping the red-ish accent for light mode
                window.Resources["ButtonBackground"] = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                window.Resources["ButtonForeground"] = Brushes.Black;
                window.Resources["WarningBackground"] = new SolidColorBrush(Color.FromRgb(255, 249, 196));
                window.Resources["WarningForeground"] = new SolidColorBrush(Color.FromRgb(51, 51, 51));
                window.Resources["WarningBorder"] = new SolidColorBrush(Color.FromRgb(251, 192, 45));
                window.Resources["TitleBarBackground"] = Brushes.White;
                window.Resources["TitleBarButtonHover"] = new SolidColorBrush(Color.FromRgb(235, 235, 235));
                window.Resources["TitleBarCloseHover"] = new SolidColorBrush(Color.FromRgb(232, 17, 35));
            }
        }
    }
}
