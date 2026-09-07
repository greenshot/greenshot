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
using System.Windows.Media;
using Microsoft.Win32;

namespace Greenshot.UI
{
    /// <summary>
    /// Detects Windows system theme (Dark / Light mode) and exposes color brushes for modern WPF UI.
    /// </summary>
    public static class WpfThemeHelper
    {
        private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        /// <summary>
        /// Returns true if Windows system apps are set to Dark Mode.
        /// </summary>
        public static bool IsDarkMode
        {
            get
            {
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey))
                    {
                        var val = key?.GetValue("AppsUseLightTheme");
                        if (val is int intVal)
                        {
                            return intVal == 0;
                        }
                    }
                }
                catch
                {
                    // Fall back to light mode on error
                }
                return false;
            }
        }

        public static SolidColorBrush WindowBackground => IsDarkMode
            ? new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20))
            : new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF7));

        public static SolidColorBrush CardBackground => IsDarkMode
            ? new SolidColorBrush(Color.FromRgb(0x2C, 0x2C, 0x2E))
            : new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));

        public static SolidColorBrush CardBorder => IsDarkMode
            ? new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3C))
            : new SolidColorBrush(Color.FromRgb(0xE5, 0xE5, 0xEA));

        public static SolidColorBrush TextPrimary => IsDarkMode
            ? new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF))
            : new SolidColorBrush(Color.FromRgb(0x1C, 0x1C, 0x1E));

        public static SolidColorBrush TextSecondary => IsDarkMode
            ? new SolidColorBrush(Color.FromRgb(0x98, 0x98, 0x9D))
            : new SolidColorBrush(Color.FromRgb(0x6C, 0x6C, 0x70));

        public static SolidColorBrush Accent => IsDarkMode
            ? new SolidColorBrush(Color.FromRgb(0x0A, 0x84, 0xFF))
            : new SolidColorBrush(Color.FromRgb(0x00, 0x7A, 0xFF));

        public static SolidColorBrush WarningBackground => IsDarkMode
            ? new SolidColorBrush(Color.FromRgb(0x38, 0x24, 0x05))
            : new SolidColorBrush(Color.FromRgb(0xFF, 0xF3, 0xCD));

        public static SolidColorBrush WarningBorder => IsDarkMode
            ? new SolidColorBrush(Color.FromRgb(0x66, 0x42, 0x0A))
            : new SolidColorBrush(Color.FromRgb(0xFF, 0xE6, 0x9C));

        public static SolidColorBrush WarningText => IsDarkMode
            ? new SolidColorBrush(Color.FromRgb(0xFF, 0xBA, 0x42))
            : new SolidColorBrush(Color.FromRgb(0x66, 0x4D, 0x03));

        public static SolidColorBrush BadgeBackground => IsDarkMode
            ? new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3C))
            : new SolidColorBrush(Color.FromRgb(0xEB, 0xEB, 0xED));
    }
}
