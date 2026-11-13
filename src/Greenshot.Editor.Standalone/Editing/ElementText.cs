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
using Greenshot.Document.Editing;

namespace Greenshot.Editor.Standalone.Editing
{
    /// <summary>
    /// The text the user types into an element: the text of a text box or speech bubble, the label of a shape or line
    /// </summary>
    public static class ElementText
    {
        public static bool HasText(Element element) =>
            element is TextElement or SpeechBubbleElement or RectangleElement or EllipseElement or LineElement or ArrowElement or ImageElement;

        public static string TextOf(Element element) => element switch
        {
            TextElement e => e.Text,
            SpeechBubbleElement e => e.Text,
            _ => GreenshotDocument.LabelOf(element)
        };

        /// <summary>
        /// The element with the text, an empty label is removed
        /// </summary>
        public static Element WithText(Element element, string text)
        {
            bool empty = string.IsNullOrEmpty(text);
            return element switch
            {
                TextElement e => e with { Text = text ?? string.Empty },
                SpeechBubbleElement e => e with { Text = text ?? string.Empty },
                RectangleElement e => e with { Label = empty ? null : (e.Label ?? new BoxLabel(text)) with { Text = text } },
                EllipseElement e => e with { Label = empty ? null : (e.Label ?? new BoxLabel(text)) with { Text = text } },
                ImageElement e => e with { Label = empty ? null : (e.Label ?? new BoxLabel(text)) with { Text = text } },
                LineElement e => e with { Label = empty ? null : (e.Label ?? new LineLabel(text)) with { Text = text } },
                ArrowElement e => e with { Label = empty ? null : (e.Label ?? new LineLabel(text)) with { Text = text } },
                _ => element
            };
        }

        /// <summary>
        /// Where the text is typed: the box of the element, for a label of a line a box around its middle
        /// </summary>
        public static (RectD Area, double Rotation) EditAreaOf(GreenshotDocument document, Element element)
        {
            switch (element)
            {
                case TextElement e:
                    return (e.Area, e.Rotation);
                case SpeechBubbleElement e:
                    return (e.Area, e.Rotation);
                case RectangleElement or EllipseElement or ImageElement:
                    var area = ElementShapes.AreaOf(element) ?? element.Bounds;
                    return (area, element.Rotation);
                default:
                    var size = document.StyleOf(element).Text?.Size ?? 11;
                    var bounds = element.Bounds;
                    var center = bounds.Center;
                    double width = System.Math.Max(120, size * 12), height = size * 2.4;
                    return (new RectD(center.X - width / 2, center.Y - height / 2, width, height), 0);
            }
        }
    }
}
