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

namespace Greenshot.UI
{
    /// <summary>
    /// Forwarding facade to <see cref="Greenshot.Base.Wpf.WpfThemeHelper"/>.
    /// </summary>
    public static class WpfThemeHelper
    {
        public static event Action ThemeChanged
        {
            add => Greenshot.Base.Wpf.WpfThemeHelper.ThemeChanged += value;
            remove => Greenshot.Base.Wpf.WpfThemeHelper.ThemeChanged -= value;
        }

        public static bool IsDarkMode
        {
            get => Greenshot.Base.Wpf.WpfThemeHelper.IsDarkMode;
            set => Greenshot.Base.Wpf.WpfThemeHelper.IsDarkMode = value;
        }

        public static void ToggleTheme() => Greenshot.Base.Wpf.WpfThemeHelper.ToggleTheme();

        public static SolidColorBrush WindowBackground => Greenshot.Base.Wpf.WpfThemeHelper.WindowBackground;
        public static SolidColorBrush CardBackground => Greenshot.Base.Wpf.WpfThemeHelper.CardBackground;
        public static SolidColorBrush CardBorder => Greenshot.Base.Wpf.WpfThemeHelper.CardBorder;
        public static SolidColorBrush TextPrimary => Greenshot.Base.Wpf.WpfThemeHelper.TextPrimary;
        public static SolidColorBrush TextSecondary => Greenshot.Base.Wpf.WpfThemeHelper.TextSecondary;
        public static SolidColorBrush Accent => Greenshot.Base.Wpf.WpfThemeHelper.Accent;
        public static SolidColorBrush WarningBackground => Greenshot.Base.Wpf.WpfThemeHelper.WarningBackground;
        public static SolidColorBrush WarningBorder => Greenshot.Base.Wpf.WpfThemeHelper.WarningBorder;
        public static SolidColorBrush WarningText => Greenshot.Base.Wpf.WpfThemeHelper.WarningText;
        public static SolidColorBrush ErrorBackground => Greenshot.Base.Wpf.WpfThemeHelper.ErrorBackground;
        public static SolidColorBrush ErrorBorder => Greenshot.Base.Wpf.WpfThemeHelper.ErrorBorder;
        public static SolidColorBrush ErrorText => Greenshot.Base.Wpf.WpfThemeHelper.ErrorText;
        public static SolidColorBrush BadgeBackground => Greenshot.Base.Wpf.WpfThemeHelper.BadgeBackground;
    }
}
