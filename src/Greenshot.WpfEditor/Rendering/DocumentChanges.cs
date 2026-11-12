/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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

using Greenshot.Document;

namespace Greenshot.WpfEditor.Rendering
{
    /// <summary>
    /// Where elements draw, for the bitmaps of the composition
    /// </summary>
    public static class DocumentChanges
    {
        /// <summary>
        /// Where the element (and its frame) changes pixels: its visual bounds with the margin the renderer gives them too, for the
        /// soft edges and the editor's shadow
        /// </summary>
        public static RectD DrawnArea(GreenshotDocument document, Element element)
        {
            var bounds = document.VisualBounds(element);
            if (document.FrameOf(element) is FrameElement frame)
            {
                bounds = RectD.Union(bounds, document.VisualBounds(frame));
            }

            return bounds.Inflate(8);
        }
    }
}
