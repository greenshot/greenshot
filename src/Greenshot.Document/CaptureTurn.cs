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

namespace Greenshot.Document
{
    /// <summary>
    /// Turns the capture in quarter turns as one change of the document (R48 - R53): the capture keeps its pixels and gets
    /// a rotation, every element goes with it so it stays on the same spot of the capture.
    /// </summary>
    public static class CaptureTurn
    {
        /// <summary>
        /// The document with the capture turned clockwise by degrees, a multiple of 90. With keepTextUpright, text, speech
        /// bubbles, emojis and the labels of rectangles and ellipses move with the capture but stay readable.
        /// </summary>
        public static GreenshotDocument Apply(GreenshotDocument document, int degrees, bool keepTextUpright = false)
        {
            if (degrees % 90 != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(degrees), degrees, "The capture turns in multiples of 90 degrees");
            }

            int quarters = (degrees / 90 % 4 + 4) % 4;
            if (quarters == 0)
            {
                return document;
            }

            // The page keeps its top left corner: a fixed canvas, otherwise the capture
            var capture = document.Layers.Where(layer => layer.Kind == LayerKind.Capture).SelectMany(layer => layer.Elements).OfType<ImageElement>().FirstOrDefault();
            var page = document.Canvas.Fit == CanvasFit.Fixed || capture == null ? document.CanvasArea() : capture.Bounds;
            var turnedPage = page.Turned(quarters * 90);
            var turn = new Turn(quarters, page.Center, page.X - turnedPage.X, page.Y - turnedPage.Y, keepTextUpright);
            var canvas = document.Canvas;
            return Anchors.Apply(document with
            {
                Canvas = quarters % 2 == 1 && canvas.Fit == CanvasFit.Fixed ? canvas with { Width = canvas.Height, Height = canvas.Width } : canvas,
                Layers = document.Layers.Select(layer => layer with { Elements = layer.Elements.Select(turn.Element).ToList() }).ToList()
            });
        }

        private sealed class Turn
        {
            private readonly int _quarters;
            private readonly PointD _center;
            private readonly double _dx, _dy;
            private readonly bool _upright;

            public Turn(int quarters, PointD center, double dx, double dy, bool upright)
            {
                _quarters = quarters;
                _center = center;
                _dx = dx;
                _dy = dy;
                _upright = upright;
            }

            private bool Odd => _quarters % 2 == 1;

            public Element Element(Element element) => Anchored(element switch
            {
                RectangleElement e => e.Label != null && !_upright ? e with { Area = Moved(e.Area), Rotation = Rotation(e.Rotation) } : e with { Area = Swapped(e.Area), Label = Placed(e.Label) },
                EllipseElement e => e.Label != null && !_upright ? e with { Area = Moved(e.Area), Rotation = Rotation(e.Rotation) } : e with { Area = Swapped(e.Area), Label = Placed(e.Label) },
                LineElement e => e with { Start = Point(e.Start), End = Point(e.End) },
                ArrowElement e => e with { Start = Point(e.Start), End = Point(e.End) },
                FreehandElement e => e with { Points = e.Points.Select(Point).ToList() },
                TextElement e => e with { Area = Moved(e.Area), Rotation = _upright ? e.Rotation : Rotation(e.Rotation) },
                SpeechBubbleElement e => e with { Area = Moved(e.Area), Rotation = _upright ? e.Rotation : Rotation(e.Rotation), Tail = Point(e.Tail) },
                EmojiElement e => e with { Area = Moved(e.Area), Rotation = _upright ? e.Rotation : Rotation(e.Rotation) },
                ImageElement e => e with { Area = Moved(e.Area), Rotation = Rotation(e.Rotation) },
                PluginElement e => e with { Area = Moved(e.Area), Rotation = Rotation(e.Rotation) },
                HighlightElement e => e with { Area = Swapped(e.Area) },
                ObfuscateElement e => e with { Area = Swapped(e.Area) },
                StepLabelElement e => e with { Area = Moved(e.Area) },
                CursorElement e => e with { Area = Moved(e.Area) },
                CutMarkElement e => e with { Area = Swapped(e.Area), Horizontal = Odd ? !e.Horizontal : e.Horizontal },
                FrameElement e => e with { Area = Swapped(e.Area), Sides = Sides(e.Sides) },
                _ => element
            });

            private PointD Point(PointD point)
            {
                var turned = point.Turned(_quarters * 90, _center);
                return new PointD(turned.X + _dx, turned.Y + _dy);
            }

            // The same size around the turned centre, for an element which turns by its rotation or stays upright
            private RectD Moved(RectD area)
            {
                var center = Point(area.Center);
                return area with { X = center.X - area.Width / 2, Y = center.Y - area.Height / 2 };
            }

            // A box turned by quarters is the box with its sides swapped, the rotation stays
            private RectD Swapped(RectD area)
            {
                var center = Point(area.Center);
                double width = Odd ? area.Height : area.Width, height = Odd ? area.Width : area.Height;
                return new RectD(center.X - width / 2, center.Y - height / 2, width, height);
            }

            private double Rotation(double rotation) => ((rotation + _quarters * 90) % 360 + 360) % 360;

            // A label which stays readable keeps its side of the box: below becomes left after a quarter turn
            private BoxLabel Placed(BoxLabel label)
            {
                for (int i = 0; label != null && i < _quarters; i++)
                {
                    label = label with { X = Mirror(label.Y), Y = label.X };
                }

                return label;
            }

            private FrameSides Sides(FrameSides sides)
            {
                for (int i = 0; sides != null && i < _quarters; i++)
                {
                    sides = new FrameSides { Top = sides.Left, Right = sides.Top, Bottom = sides.Right, Left = sides.Bottom };
                }

                return sides;
            }

            private Element Anchored(Element element)
            {
                element = element with { Anchor = Turned(element.Anchor) };
                return element switch
                {
                    LineElement e => e with { StartAnchor = Turned(e.StartAnchor), EndAnchor = Turned(e.EndAnchor) },
                    ArrowElement e => e with { StartAnchor = Turned(e.StartAnchor), EndAnchor = Turned(e.EndAnchor) },
                    SpeechBubbleElement e => e with { TailAnchor = Turned(e.TailAnchor) },
                    _ => element
                };
            }

            // The new x axis is the old y axis mirrored, the new y axis the old x axis
            private Anchor Turned(Anchor anchor)
            {
                for (int i = 0; anchor != null && i < _quarters; i++)
                {
                    anchor = anchor with { X = Mirror(anchor.Y), Y = anchor.X };
                }

                return anchor;
            }

            private static AxisAnchor Mirror(AxisAnchor axis) => axis == null ? null : new AxisAnchor(Mirror(axis.Target), Mirror(axis.Self), -axis.Offset);

            private static AnchorLine Mirror(AnchorLine line) => line switch
            {
                AnchorLine.Start => AnchorLine.End,
                AnchorLine.End => AnchorLine.Start,
                _ => line
            };

            private static LabelPlace Mirror(LabelPlace place) => place switch
            {
                LabelPlace.Before => LabelPlace.After,
                LabelPlace.Start => LabelPlace.End,
                LabelPlace.End => LabelPlace.Start,
                LabelPlace.After => LabelPlace.Before,
                _ => place
            };
        }
    }
}
