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
using System.Windows.Media;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// Detects Windows system theme (Dark / Light mode) and exposes color brushes for modern WPF UI.
    /// </summary>
    public static class WpfThemeHelper
    {
        public static event Action ThemeChanged;

        static WpfThemeHelper()
        {
            ThemeManager.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ThemeManager.IsDarkTheme))
                {
                    ThemeChanged?.Invoke();
                }
            };
        }

        /// <summary>
        /// Returns true if Windows system apps or the selected theme is set to Dark Mode.
        /// </summary>
        public static bool IsDarkMode
        {
            get => ThemeManager.Instance.IsDarkTheme;
            set => ThemeManager.Instance.IsDarkTheme = value;
        }

        public static void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
        }

        public static SolidColorBrush WindowBackground => ThemeManager.Instance.CurrentPalette.WindowBackground;

        public static SolidColorBrush CardBackground => ThemeManager.Instance.CurrentPalette.CardBackground;

        public static SolidColorBrush CardBorder => ThemeManager.Instance.CurrentPalette.CardBorder;

        public static SolidColorBrush TextPrimary => ThemeManager.Instance.CurrentPalette.TextPrimary;

        public static SolidColorBrush TextSecondary => ThemeManager.Instance.CurrentPalette.TextSecondary;

        public static SolidColorBrush Accent => ThemeManager.Instance.CurrentPalette.Accent;

        public static SolidColorBrush WarningBackground => ThemeManager.Instance.CurrentPalette.WarningBackground;

        public static SolidColorBrush WarningBorder => ThemeManager.Instance.CurrentPalette.WarningBorder;

        public static SolidColorBrush WarningText => ThemeManager.Instance.CurrentPalette.WarningText;

        public static SolidColorBrush ErrorBackground => ThemeManager.Instance.CurrentPalette.ErrorBackground;

        public static SolidColorBrush ErrorBorder => ThemeManager.Instance.CurrentPalette.ErrorBorder;

        public static SolidColorBrush ErrorText => ThemeManager.Instance.CurrentPalette.ErrorText;

        public static SolidColorBrush BadgeBackground => ThemeManager.Instance.CurrentPalette.BadgeBackground;
    }
}
