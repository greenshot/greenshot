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

namespace Greenshot.Document.Editing
{
    /// <summary>
    /// The shape of an element as an editor changes it: its area, its ends, its rotation. Changes give a new element with
    /// the same id, an element of an unknown type is never changed.
    /// </summary>
    public static class ElementShapes
    {
        /// <summary>
        /// The area of a box-shaped element before it is turned, null for lines, arrows and freehand
        /// </summary>
        public static RectD? AreaOf(Element element) => element switch
        {
            RectangleElement e => e.Area,
            EllipseElement e => e.Area,
            TextElement e => e.Area,
            SpeechBubbleElement e => e.Area,
            StepLabelElement e => e.Area,
            HighlightElement e => e.Area,
            ObfuscateElement e => e.Area,
            ImageElement e => e.Area,
            EmojiElement e => e.Area,
            CursorElement e => e.Area,
            CutMarkElement e => e.Area,
            FrameElement e => e.Area,
            PluginElement e => e.Area,
            _ => null
        };

        /// <summary>
        /// Elements which turn by their Rotation (R35)
        /// </summary>
        public static bool CanTurn(Element element) =>
            element is RectangleElement or EllipseElement or TextElement or SpeechBubbleElement or HighlightElement or ObfuscateElement or ImageElement or EmojiElement
                or PluginElement;

        /// <summary>
        /// The start and end of a line or arrow, null for other elements
        /// </summary>
        public static (PointD Start, PointD End)? EndsOf(Element element) => element switch
        {
            LineElement e => (e.Start, e.End),
            ArrowElement e => (e.Start, e.End),
            _ => null
        };

        /// <summary>
        /// A line or arrow with other ends, a hand-made change drops the anchors of the ends
        /// </summary>
        public static Element WithEnds(Element element, PointD start, PointD end) => element switch
        {
            LineElement e => e with { Start = start, End = end, StartAnchor = null, EndAnchor = null },
            ArrowElement e => e with { Start = start, End = end, StartAnchor = null, EndAnchor = null },
            _ => element
        };

        /// <summary>
        /// The element with another area: a box-shaped element gets it as its Area, lines, arrows and freehand are scaled
        /// from their Bounds into it. A speech bubble keeps its tail where it is.
        /// </summary>
        public static Element WithArea(Element element, RectD area) => element switch
        {
            RectangleElement e => e with { Area = area },
            EllipseElement e => e with { Area = area },
            TextElement e => e with { Area = area },
            SpeechBubbleElement e => e with { Area = area },
            StepLabelElement e => e with { Area = area },
            HighlightElement e => e with { Area = area },
            ObfuscateElement e => e with { Area = area },
            ImageElement e => e with { Area = area, Scale = null },
            EmojiElement e => e with { Area = area },
            CursorElement e => e with { Area = area },
            CutMarkElement e => e with { Area = area },
            FrameElement e => e with { Area = area },
            PluginElement e => e with { Area = area },
            LineElement e => e with { Start = Scale(e.Start, e.Bounds, area), End = Scale(e.End, e.Bounds, area), StartAnchor = null, EndAnchor = null },
            ArrowElement e => e with { Start = Scale(e.Start, e.Bounds, area), End = Scale(e.End, e.Bounds, area), StartAnchor = null, EndAnchor = null },
            FreehandElement e => e with { Points = e.Points.Select(point => Scale(point, e.Bounds, area)).ToList() },
            _ => element
        };

        /// <summary>
        /// The element turned to degrees clockwise, when it can turn
        /// </summary>
        public static Element WithRotation(Element element, double degrees) => CanTurn(element) ? element with { Rotation = Normalized(degrees) } : element;

        /// <summary>
        /// The element moved by hand: moved by dx, dy and without the anchor which would put it back
        /// </summary>
        public static Element MovedByHand(Element element, double dx, double dy) => Unanchored(Anchors.Move(element, dx, dy));

        /// <summary>
        /// The element without the anchors of its position, as after a change by hand; a speech bubble keeps the anchor of its tail
        /// </summary>
        public static Element Unanchored(Element element)
        {
            element = element.Anchor == null ? element : element with { Anchor = null };
            return element switch
            {
                LineElement { StartAnchor: not null } or LineElement { EndAnchor: not null } => ((LineElement)element) with { StartAnchor = null, EndAnchor = null },
                ArrowElement { StartAnchor: not null } or ArrowElement { EndAnchor: not null } => ((ArrowElement)element) with { StartAnchor = null, EndAnchor = null },
                _ => element
            };
        }

        /// <summary>
        /// Degrees from 0 up to 360
        /// </summary>
        public static double Normalized(double degrees) => (degrees % 360 + 360) % 360;

        private static PointD Scale(PointD point, RectD from, RectD to) =>
            new(to.X + (from.Width > 0 ? (point.X - from.X) * to.Width / from.Width : to.Width / 2),
                to.Y + (from.Height > 0 ? (point.Y - from.Y) * to.Height / from.Height : to.Height / 2));
    }
}
