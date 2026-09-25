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

using System.Windows.Media;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// Provides the color roles for a light or dark WPF theme.
    /// </summary>
    public sealed class ThemePalette
    {
        public static ThemePalette Light { get; } = new ThemePalette(false);
        public static ThemePalette Dark { get; } = new ThemePalette(true);

        private readonly bool _isDarkTheme;

        private ThemePalette(bool isDarkTheme)
        {
            _isDarkTheme = isDarkTheme;
        }

        public SolidColorBrush Accent => AccentBrush;

        public SolidColorBrush AccentBrush => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(10, 132, 255))
            : new SolidColorBrush(Color.FromRgb(0, 122, 255));

        public SolidColorBrush BackgroundBrush => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(32, 32, 32))
            : new SolidColorBrush(Color.FromRgb(245, 245, 245));

        public SolidColorBrush BadgeBackground => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(58, 58, 60))
            : new SolidColorBrush(Color.FromRgb(235, 235, 237));

        public SolidColorBrush BorderBrush => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(70, 70, 70))
            : new SolidColorBrush(Color.FromRgb(200, 200, 200));

        public SolidColorBrush ButtonBackgroundBrush => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(55, 55, 55))
            : new SolidColorBrush(Color.FromRgb(230, 230, 230));

        public SolidColorBrush ButtonHoverBrush => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(70, 70, 70))
            : new SolidColorBrush(Color.FromRgb(210, 210, 210));

        public SolidColorBrush ButtonPressedBrush => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(45, 45, 45))
            : new SolidColorBrush(Color.FromRgb(190, 190, 190));

        public SolidColorBrush CardBackground => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(44, 44, 46))
            : new SolidColorBrush(Color.FromRgb(255, 255, 255));

        public SolidColorBrush CardBorder => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(58, 58, 60))
            : new SolidColorBrush(Color.FromRgb(229, 229, 234));

        public SolidColorBrush ControlBackgroundBrush => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(36, 36, 36))
            : new SolidColorBrush(Color.FromRgb(250, 250, 250));

        public SolidColorBrush ControlBorderBrush => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(70, 70, 70))
            : new SolidColorBrush(Color.FromRgb(180, 180, 180));

        public SolidColorBrush ErrorBackground => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(59, 24, 24))
            : new SolidColorBrush(Color.FromRgb(253, 237, 237));

        public SolidColorBrush ErrorBorder => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(127, 42, 42))
            : new SolidColorBrush(Color.FromRgb(245, 194, 199));

        public SolidColorBrush ErrorBrush => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(255, 107, 107))
            : new SolidColorBrush(Color.FromRgb(132, 32, 41));

        public SolidColorBrush ErrorText => ErrorBrush;

        public SolidColorBrush ForegroundBrush => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(230, 230, 230))
            : new SolidColorBrush(Color.FromRgb(40, 40, 40));

        public SolidColorBrush GroupBoxBrush => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(42, 42, 42))
            : new SolidColorBrush(Color.FromRgb(255, 255, 255));

        public SolidColorBrush MutedBrush => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(175, 175, 175))
            : new SolidColorBrush(Color.FromRgb(100, 100, 100));

        public SolidColorBrush TabItemBackgroundBrush => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(50, 50, 50))
            : new SolidColorBrush(Color.FromRgb(220, 220, 220));

        public SolidColorBrush TabItemSelectedBrush => GroupBoxBrush;

        public SolidColorBrush TextBoxBackgroundBrush => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(24, 24, 24))
            : new SolidColorBrush(Color.FromRgb(255, 255, 255));

        public SolidColorBrush TextPrimary => ForegroundBrush;

        public SolidColorBrush TextSecondary => MutedBrush;

        public SolidColorBrush TitleBarBrush => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(24, 24, 24))
            : new SolidColorBrush(Color.FromRgb(240, 240, 240));

        public SolidColorBrush WarningBackground => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(56, 36, 5))
            : new SolidColorBrush(Color.FromRgb(255, 243, 205));

        public SolidColorBrush WarningBorder => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(102, 66, 10))
            : new SolidColorBrush(Color.FromRgb(255, 230, 156));

        public SolidColorBrush WarningBrush => _isDarkTheme
            ? new SolidColorBrush(Color.FromRgb(255, 186, 66))
            : new SolidColorBrush(Color.FromRgb(102, 77, 3));

        public SolidColorBrush WarningText => WarningBrush;

        public SolidColorBrush WindowBackground => BackgroundBrush;
    }
}
