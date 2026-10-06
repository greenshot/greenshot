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
    /// Provides the color roles for a light, dark or high contrast WPF theme.
    /// The brushes are created once and frozen, so they can be shared between threads and windows.
    /// </summary>
    public sealed class ThemePalette
    {
        /// <summary>
        /// The accent of the light theme when Windows doesn't tell us the user's accent color: the one of the default Windows blue
        /// </summary>
        public static readonly Color DefaultLightAccent = Color.FromRgb(0, 90, 158);

        /// <summary>
        /// The accent of the dark theme when Windows doesn't tell us the user's accent color: the one of the default Windows blue
        /// </summary>
        public static readonly Color DefaultDarkAccent = Color.FromRgb(153, 235, 255);

        public static ThemePalette Light { get; } = Create(false, DefaultLightAccent);
        public static ThemePalette Dark { get; } = Create(true, DefaultDarkAccent);

        private ThemePalette()
        {
        }

        /// <summary>
        /// The light or dark palette with the given accent color
        /// </summary>
        public static ThemePalette Create(bool isDarkTheme, Color accent)
        {
            return new ThemePalette
            {
                IsDark = isDarkTheme,
                AccentBrush = Brush(accent),
                AccentForegroundBrush = Brush(ReadableForeground(accent)),
                BackgroundBrush = Brush(isDarkTheme, 32, 32, 32, 245, 245, 245),
                BadgeBackground = Brush(isDarkTheme, 58, 58, 60, 235, 235, 237),
                BorderBrush = Brush(isDarkTheme, 70, 70, 70, 200, 200, 200),
                ButtonBackgroundBrush = Brush(isDarkTheme, 55, 55, 55, 230, 230, 230),
                ButtonHoverBrush = Brush(isDarkTheme, 70, 70, 70, 210, 210, 210),
                ButtonPressedBrush = Brush(isDarkTheme, 45, 45, 45, 190, 190, 190),
                CardBackground = Brush(isDarkTheme, 44, 44, 46, 255, 255, 255),
                CardBorder = Brush(isDarkTheme, 58, 58, 60, 229, 229, 234),
                ControlBackgroundBrush = Brush(isDarkTheme, 36, 36, 36, 250, 250, 250),
                ControlBorderBrush = Brush(isDarkTheme, 70, 70, 70, 180, 180, 180),
                ScrollBarTrackBrush = Brush(isDarkTheme, 44, 44, 44, 232, 232, 232),
                ScrollBarThumbBrush = Brush(isDarkTheme, 75, 75, 75, 190, 190, 190),
                ScrollBarThumbHoverBrush = Brush(isDarkTheme, 90, 90, 90, 160, 160, 160),
                ScrollBarThumbPressedBrush = Brush(isDarkTheme, 120, 120, 120, 135, 135, 135),
                ErrorBackground = Brush(isDarkTheme, 59, 24, 24, 253, 237, 237),
                ErrorBorder = Brush(isDarkTheme, 127, 42, 42, 245, 194, 199),
                ErrorBrush = Brush(isDarkTheme, 255, 107, 107, 132, 32, 41),
                ForegroundBrush = Brush(isDarkTheme, 230, 230, 230, 40, 40, 40),
                GroupBoxBrush = Brush(isDarkTheme, 42, 42, 42, 255, 255, 255),
                MutedBrush = Brush(isDarkTheme, 175, 175, 175, 100, 100, 100),
                TabItemBackgroundBrush = Brush(isDarkTheme, 50, 50, 50, 220, 220, 220),
                TextBoxBackgroundBrush = Brush(isDarkTheme, 24, 24, 24, 255, 255, 255),
                TitleBarBrush = Brush(isDarkTheme, 24, 24, 24, 240, 240, 240),
                WarningBackground = Brush(isDarkTheme, 56, 36, 5, 255, 243, 205),
                WarningBorder = Brush(isDarkTheme, 102, 66, 10, 255, 230, 156),
                WarningBrush = Brush(isDarkTheme, 255, 186, 66, 102, 77, 3)
            }.WithDerivedRoles(null);
        }

        /// <summary>
        /// The palette of a Windows high contrast theme: every role uses the system colors the user picked,
        /// so text and borders keep the contrast the user needs. Hover and selection use the highlight colors.
        /// </summary>
        public static ThemePalette CreateHighContrast()
        {
            // The GDI system colors are read from Windows right now, WPF's SystemColors may still have the old ones while the theme changes
            var windowColor = SystemColor(System.Drawing.SystemColors.Window);
            var window = Brush(windowColor);
            var windowText = Brush(SystemColor(System.Drawing.SystemColors.WindowText));
            var buttonFace = Brush(SystemColor(System.Drawing.SystemColors.Control));
            var highlight = Brush(SystemColor(System.Drawing.SystemColors.Highlight));
            var highlightText = Brush(SystemColor(System.Drawing.SystemColors.HighlightText));
            return new ThemePalette
            {
                IsDark = Luminance(windowColor) < 0.5,
                IsHighContrast = true,
                AccentBrush = highlight,
                AccentForegroundBrush = highlightText,
                BackgroundBrush = window,
                BadgeBackground = buttonFace,
                BorderBrush = windowText,
                ButtonBackgroundBrush = buttonFace,
                ButtonHoverBrush = highlight,
                ButtonPressedBrush = highlight,
                CardBackground = window,
                CardBorder = windowText,
                ControlBackgroundBrush = window,
                ControlBorderBrush = windowText,
                ScrollBarTrackBrush = buttonFace,
                ScrollBarThumbBrush = windowText,
                ScrollBarThumbHoverBrush = highlight,
                ScrollBarThumbPressedBrush = highlight,
                // Colors can't carry meaning in high contrast, warnings and errors are shown in the normal text colors
                ErrorBackground = window,
                ErrorBorder = windowText,
                ErrorBrush = windowText,
                ForegroundBrush = windowText,
                GroupBoxBrush = window,
                MutedBrush = Brush(SystemColor(System.Drawing.SystemColors.GrayText)),
                TabItemBackgroundBrush = buttonFace,
                TextBoxBackgroundBrush = window,
                TitleBarBrush = window,
                WarningBackground = window,
                WarningBorder = windowText,
                WarningBrush = windowText
            }.WithDerivedRoles(highlightText);
        }

        private ThemePalette WithDerivedRoles(SolidColorBrush highlightForeground)
        {
            HighlightForegroundBrush = highlightForeground ?? ForegroundBrush;
            return this;
        }

        /// <summary>
        /// True for a dark palette (also a dark high contrast theme)
        /// </summary>
        public bool IsDark { get; private set; }

        /// <summary>
        /// True when this palette uses the colors of a Windows high contrast theme
        /// </summary>
        public bool IsHighContrast { get; private set; }

        public SolidColorBrush Accent => AccentBrush;

        public SolidColorBrush AccentBrush { get; private set; }

        /// <summary>
        /// The text color on an accent background (e.g. the default button): black or white, whichever reads better
        /// </summary>
        public SolidColorBrush AccentForegroundBrush { get; private set; }

        public SolidColorBrush BackgroundBrush { get; private set; }

        public SolidColorBrush BadgeBackground { get; private set; }

        public SolidColorBrush BorderBrush { get; private set; }

        public SolidColorBrush ButtonBackgroundBrush { get; private set; }

        public SolidColorBrush ButtonHoverBrush { get; private set; }

        public SolidColorBrush ButtonPressedBrush { get; private set; }

        public SolidColorBrush CardBackground { get; private set; }

        public SolidColorBrush CardBorder { get; private set; }

        public SolidColorBrush ControlBackgroundBrush { get; private set; }

        public SolidColorBrush ControlBorderBrush { get; private set; }

        public SolidColorBrush ScrollBarTrackBrush { get; private set; }

        public SolidColorBrush ScrollBarThumbBrush { get; private set; }

        public SolidColorBrush ScrollBarThumbHoverBrush { get; private set; }

        public SolidColorBrush ScrollBarThumbPressedBrush { get; private set; }

        public SolidColorBrush ErrorBackground { get; private set; }

        public SolidColorBrush ErrorBorder { get; private set; }

        public SolidColorBrush ErrorBrush { get; private set; }

        public SolidColorBrush ErrorText => ErrorBrush;

        public SolidColorBrush ForegroundBrush { get; private set; }

        public SolidColorBrush GroupBoxBrush { get; private set; }

        /// <summary>
        /// The text color on a hover or pressed background (<see cref="ButtonHoverBrush"/>, <see cref="ButtonPressedBrush"/>):
        /// the normal text color, in high contrast the highlight text color
        /// </summary>
        public SolidColorBrush HighlightForegroundBrush { get; private set; }

        public SolidColorBrush MutedBrush { get; private set; }

        public SolidColorBrush TabItemBackgroundBrush { get; private set; }

        public SolidColorBrush TabItemSelectedBrush => GroupBoxBrush;

        public SolidColorBrush TextBoxBackgroundBrush { get; private set; }

        public SolidColorBrush TextPrimary => ForegroundBrush;

        public SolidColorBrush TextSecondary => MutedBrush;

        public SolidColorBrush TitleBarBrush { get; private set; }

        public SolidColorBrush WarningBackground { get; private set; }

        public SolidColorBrush WarningBorder { get; private set; }

        public SolidColorBrush WarningBrush { get; private set; }

        public SolidColorBrush WarningText => WarningBrush;

        public SolidColorBrush WindowBackground => BackgroundBrush;

        /// <summary>
        /// True when the other palette looks the same: switching to it changes nothing
        /// </summary>
        public bool HasSameColors(ThemePalette other)
        {
            if (other == null || other.IsDark != IsDark || other.IsHighContrast != IsHighContrast)
            {
                return false;
            }

            // The light and dark palettes only differ in the accent, high contrast palettes in the system colors behind these roles
            return other.AccentBrush.Color == AccentBrush.Color
                   && other.AccentForegroundBrush.Color == AccentForegroundBrush.Color
                   && other.BackgroundBrush.Color == BackgroundBrush.Color
                   && other.ForegroundBrush.Color == ForegroundBrush.Color
                   && other.MutedBrush.Color == MutedBrush.Color
                   && other.ButtonBackgroundBrush.Color == ButtonBackgroundBrush.Color
                   && other.HighlightForegroundBrush.Color == HighlightForegroundBrush.Color;
        }

        /// <summary>
        /// Black or white, whichever has more contrast on the background
        /// </summary>
        public static Color ReadableForeground(Color background)
        {
            // Above this luminance black text has the higher contrast ratio, below it white text
            return Luminance(background) > 0.179 ? Colors.Black : Colors.White;
        }

        /// <summary>
        /// The relative luminance (0 black .. 1 white) of a color, as defined by WCAG
        /// </summary>
        public static double Luminance(Color color)
        {
            static double Channel(byte value)
            {
                double c = value / 255.0;
                return c <= 0.03928 ? c / 12.92 : System.Math.Pow((c + 0.055) / 1.055, 2.4);
            }

            return 0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);
        }

        private static SolidColorBrush Brush(bool isDarkTheme, byte darkR, byte darkG, byte darkB, byte lightR, byte lightG, byte lightB)
        {
            return isDarkTheme ? Brush(Color.FromRgb(darkR, darkG, darkB)) : Brush(Color.FromRgb(lightR, lightG, lightB));
        }

        private static Color SystemColor(System.Drawing.Color color)
        {
            return Color.FromRgb(color.R, color.G, color.B);
        }

        private static SolidColorBrush Brush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }
}
