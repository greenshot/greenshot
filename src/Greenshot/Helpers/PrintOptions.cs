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

namespace Greenshot.Helpers
{
    /// <summary>
    /// Explicit options for print output. Null properties fall back to ICoreConfiguration defaults.
    /// </summary>
    public class PrintOptions
    {
        public bool? AllowRotate { get; set; }
        public bool? AllowEnlarge { get; set; }
        public bool? AllowShrink { get; set; }
        public bool? Center { get; set; }
        public bool? Inverted { get; set; }
        public bool? Grayscale { get; set; }
        public bool? Monochrome { get; set; }
        public byte? MonochromeThreshold { get; set; }
        public bool? Footer { get; set; }
        public string FooterPattern { get; set; }
        public bool? PromptOptions { get; set; }
    }
}
