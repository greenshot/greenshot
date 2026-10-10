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

using System.Drawing;
using Greenshot.Base.IniFile;

namespace Greenshot.Plugin.CaptionBar
{
    /// <summary>
    /// Configuration for the CaptionBar plugin
    /// </summary>
    [IniSection("CaptionBar", Description = "Greenshot CaptionBar Plugin configuration")]
    public class CaptionBarConfiguration : IniSection
    {
        [IniProperty("Enabled", Description = "Enable caption bar", DefaultValue = "False")]
        public bool Enabled { get; set; }

        [IniProperty("BarHeight", Description = "Height of the caption bar in pixels", DefaultValue = "30")]
        public int BarHeight { get; set; }

        [IniProperty("BackgroundColor", Description = "Background color of the caption bar", DefaultValue = "64,64,64")]
        public Color BackgroundColor { get; set; }

        [IniProperty("TextColor", Description = "Text color of the caption bar", DefaultValue = "255,255,255")]
        public Color TextColor { get; set; }

        [IniProperty("CustomText", Description = "Custom text to display on the right", DefaultValue = "")]
        public string CustomText { get; set; }

        [IniProperty("ShowTimestamp", Description = "Show timestamp on the left", DefaultValue = "True")]
        public bool ShowTimestamp { get; set; }

        [IniProperty("TimestampFormat", Description = "Format for the timestamp", DefaultValue = "M/d/yyyy h:mm:ss tt")]
        public string TimestampFormat { get; set; }

        [IniProperty("FontName", Description = "Font name for the caption bar text", DefaultValue = "Segoe UI")]
        public string FontName { get; set; }

        [IniProperty("FontSize", Description = "Font size for the caption bar text", DefaultValue = "9")]
        public float FontSize { get; set; }

        [IniProperty("TimestampAlignment", Description = "Timestamp text alignment: Near=Left, Center=Center, Far=Right", DefaultValue = "Near")]
        public StringAlignment TimestampAlignment { get; set; }

        [IniProperty("CustomTextAlignment", Description = "Custom text alignment: Near=Left, Center=Center, Far=Right", DefaultValue = "Far")]
        public StringAlignment CustomTextAlignment { get; set; }

        [IniProperty("EnableTextWrapping", Description = "Enable automatic text wrapping", DefaultValue = "True")]
        public bool EnableTextWrapping { get; set; }

        [IniProperty("MaxLines", Description = "Maximum lines when wrapping (1-4)", DefaultValue = "2")]
        public int MaxLines { get; set; }

        [IniProperty("MinFontSizePercent", Description = "Minimum font size as percentage (20-100)", DefaultValue = "50")]
        public int MinFontSizePercent { get; set; }

        [IniProperty("TextPadding", Description = "Horizontal padding in pixels", DefaultValue = "10")]
        public int TextPadding { get; set; }
    }
}
