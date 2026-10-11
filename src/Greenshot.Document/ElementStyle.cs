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

namespace Greenshot.Document
{
    /// <summary>
    /// How lines are drawn.
    /// </summary>
    public sealed record Stroke(Rgba Color, double Width);

    /// <summary>
    /// How an area is filled.
    /// </summary>
    public sealed record Fill(Rgba Color);

    /// <summary>
    /// The drop shadow, the renderer decides how it looks.
    /// </summary>
    public sealed record Shadow
    {
        public static readonly Shadow Default = new();
    }

    public sealed record FontSpec(string Family, double Size, bool Bold = false, bool Italic = false);

    public enum TextAlignment
    {
        Near,
        Center,
        Far
    }

    public sealed record TextStyle(FontSpec Font, Rgba Color, TextAlignment Horizontal = TextAlignment.Center, TextAlignment Vertical = TextAlignment.Center);

    /// <summary>
    /// The style of an element, a part which is null (or a stroke with width 0) isn't drawn.
    /// </summary>
    public sealed record ElementStyle(Stroke Stroke = null, Fill Fill = null, Shadow Shadow = null, TextStyle Text = null)
    {
        public static readonly ElementStyle None = new();
    }
}
