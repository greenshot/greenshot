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
using System.Collections.Generic;
using System.Linq;
using Greenshot.Document;

namespace Greenshot.Editor.Standalone.Rendering
{
    /// <summary>
    /// Finds where two documents draw different pixels, so an editor only draws that part again. The document model is
    /// immutable: an element which is the same object in both documents draws the same.
    /// </summary>
    public static class DocumentDiff
    {
        // The shadow of an element (offset and blur) and the soft edge of its lines reach this far out of its visual bounds
        private const double Margin = 10;

        /// <summary>
        /// The area in which the documents differ, in the coordinates of the elements. An area without size when they draw the
        /// same, null when all of the canvas has to be drawn (the canvas, a layer or the order of the elements changed).
        /// Pass it through <see cref="DocumentRenderer.Affected"/> before drawing.
        /// </summary>
        public static RectD? Changed(GreenshotDocument before, GreenshotDocument after)
        {
            if (before == null || after == null)
            {
                return null;
            }

            if (ReferenceEquals(before, after))
            {
                return default(RectD);
            }

            // Everything but the layers: the canvas, the resources, the styles
            if (!(before with { Layers = after.Layers }).Equals(after) || before.Layers.Count != after.Layers.Count || before.CanvasArea() != after.CanvasArea())
            {
                return null;
            }

            RectD? changed = null;
            for (int i = 0; i < after.Layers.Count; i++)
            {
                Layer old = before.Layers[i], current = after.Layers[i];
                if (ReferenceEquals(old, current) || ReferenceEquals(old.Elements, current.Elements) && old.Equals(current))
                {
                    continue;
                }

                if (!(old with { Elements = current.Elements }).Equals(current))
                {
                    // Shown, hidden, another opacity
                    return null;
                }

                if (!current.Visible)
                {
                    continue;
                }

                var oldById = old.Elements.ToDictionary(element => element.Id);
                var currentIds = new HashSet<Guid>(current.Elements.Select(element => element.Id));
                // The elements both have must be in the same order, else one moved to the front or back
                if (!old.Elements.Where(element => currentIds.Contains(element.Id)).Select(element => element.Id)
                        .SequenceEqual(current.Elements.Where(element => oldById.ContainsKey(element.Id)).Select(element => element.Id)))
                {
                    return null;
                }

                foreach (var element in current.Elements)
                {
                    if (!oldById.TryGetValue(element.Id, out var known))
                    {
                        changed = Union(changed, DrawnArea(after, element));
                    }
                    else if (!ReferenceEquals(known, element))
                    {
                        changed = Union(Union(changed, DrawnArea(before, known)), DrawnArea(after, element));
                    }
                }

                foreach (var element in old.Elements.Where(element => !currentIds.Contains(element.Id)))
                {
                    changed = Union(changed, DrawnArea(before, element));
                }
            }

            return changed ?? default(RectD);
        }

        /// <summary>
        /// Where the element, its frame and its shadow change pixels
        /// </summary>
        public static RectD DrawnArea(GreenshotDocument document, Element element)
        {
            var bounds = document.VisualBounds(element);
            if (document.FrameOf(element) is FrameElement frame)
            {
                bounds = RectD.Union(bounds, document.VisualBounds(frame));
            }

            return bounds.Inflate(Margin);
        }

        private static RectD Union(RectD? known, RectD area) => known is RectD rect ? RectD.Union(rect, area) : area;
    }
}
