/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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


using System.Windows;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// The colors of the panels next to the canvas of the editor. They follow the editor theme setting, which can differ from
    /// Greenshot's theme (the shared styles follow Greenshot's theme). Views of panels and sections use these keys as
    /// DynamicResource, the editor fills them for its theme and changes them when the theme changes.
    /// </summary>
    public static class EditorPanelResources
    {
        public const string Background = "Greenshot.EditorPanel.Background";
        public const string Foreground = "Greenshot.EditorPanel.Foreground";
        public const string Muted = "Greenshot.EditorPanel.Muted";
        public const string Border = "Greenshot.EditorPanel.Border";
        public const string SectionBackground = "Greenshot.EditorPanel.SectionBackground";
        public const string ControlBackground = "Greenshot.EditorPanel.ControlBackground";
        public const string ControlBorder = "Greenshot.EditorPanel.ControlBorder";
        public const string Hover = "Greenshot.EditorPanel.Hover";
        public const string Accent = "Greenshot.EditorPanel.Accent";

        /// <summary>
        /// Put the colors of the palette into the resources of the panel area
        /// </summary>
        public static void Apply(ResourceDictionary resources, ThemePalette palette)
        {
            if (resources == null || palette == null)
            {
                return;
            }

            resources[Background] = palette.BackgroundBrush;
            resources[Foreground] = palette.ForegroundBrush;
            resources[Muted] = palette.MutedBrush;
            resources[Border] = palette.BorderBrush;
            resources[SectionBackground] = palette.CardBackground;
            resources[ControlBackground] = palette.TextBoxBackgroundBrush;
            resources[ControlBorder] = palette.ControlBorderBrush;
            resources[Hover] = palette.ButtonHoverBrush;
            resources[Accent] = palette.AccentBrush;
        }
    }
}
