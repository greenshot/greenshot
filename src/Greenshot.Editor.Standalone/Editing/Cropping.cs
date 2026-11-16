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
using System;
using System.Linq;
using Greenshot.Document;
using Greenshot.Document.Editing;

namespace Greenshot.Editor.Standalone.Editing
{
    /// <summary>
    /// Crops a document like the user expects it of an editor: what lies completely outside of the new canvas is gone
    /// </summary>
    public static class Cropping
    {
        /// <summary>
        /// The canvas cut to the area. The elements which draw nothing inside of it are removed (an undo brings them back):
        /// they would be invisible, but still there to select, and still in the saved file. The capture stays as it is,
        /// only its part inside the canvas is drawn.
        /// </summary>
        public static GreenshotDocument Crop(GreenshotDocument document, RectD area)
        {
            var cropped = DocumentEdits.Crop(document, area);
            var canvas = cropped.CanvasArea();
            var outside = cropped.Layers.Where(layer => layer.Kind != LayerKind.Capture)
                .SelectMany(layer => layer.Elements)
                .Where(element => element is not FrameElement && !Overlaps(cropped.VisualBounds(element).Turned(element.Rotation), canvas))
                .Select(element => element.Id)
                .ToHashSet();
            return DocumentEdits.Remove(cropped, outside);
        }

        private static bool Overlaps(RectD a, RectD b) => a.X < b.Right && a.Right > b.X && a.Y < b.Bottom && a.Bottom > b.Y;
    }
}
