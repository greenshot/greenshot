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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Greenshot.Document;
using Xunit;

namespace Greenshot.Tests.Document
{
    public class AnchorsTests
    {
        private static GreenshotDocument WithCapture(RectD captureArea, params Element[] elements) =>
            new(Guid.NewGuid(), new Canvas(1000, 1000), new List<Layer>
            {
                new(Guid.NewGuid(), "Capture", LayerKind.Capture, new List<Element> { new ImageElement(Guid.NewGuid(), captureArea, Guid.NewGuid()) }),
                new(Guid.NewGuid(), "Annotations", LayerKind.Annotations, elements)
            }, new Dictionary<Guid, Resource>());

        private static T Placed<T>(GreenshotDocument document, Guid id) where T : Element =>
            Assert.IsType<T>(Anchors.Apply(document).Layers[1].Elements.Single(element => element.Id == id));

        [Fact]
        public void Apply_PlacesABoxRelativeToTheCapture()
        {
            // 10 px inside the right edge, centred vertically
            var label = new RectangleElement(Guid.NewGuid(), new RectD(0, 0, 100, 50))
            {
                Anchor = new Anchor { X = new AxisAnchor(AnchorLine.End, AnchorLine.End, -10), Y = new AxisAnchor(AnchorLine.Center, AnchorLine.Center) }
            };

            Assert.Equal(new RectD(690, 275, 100, 50), Placed<RectangleElement>(WithCapture(new RectD(0, 0, 800, 600), label), label.Id).Area);
            // The canvas grew by a border of 20 px: the capture moved, the label follows
            Assert.Equal(new RectD(710, 295, 100, 50), Placed<RectangleElement>(WithCapture(new RectD(20, 20, 800, 600), label), label.Id).Area);
        }

        [Fact]
        public void Apply_PlacesTheTargetFirst_AndAnAxisWithoutAnchorStays()
        {
            var box = new RectangleElement(Guid.NewGuid(), new RectD(0, 0, 100, 50)) { Anchor = new Anchor { X = new AxisAnchor(AnchorLine.Start, AnchorLine.Start, 10) } };
            // A caption 8 px below the box, listed before it
            var caption = new TextElement(Guid.NewGuid(), new RectD(500, 0, 100, 20), "Caption")
            {
                Anchor = new Anchor { Target = box.Id, Y = new AxisAnchor(AnchorLine.End, AnchorLine.Start, 8) }
            };
            var document = WithCapture(new RectD(30, 40, 800, 600), caption, box);

            Assert.Equal(new RectD(40, 0, 100, 50), Placed<RectangleElement>(document, box.Id).Area);
            Assert.Equal(new RectD(500, 58, 100, 20), Placed<TextElement>(document, caption.Id).Area);
        }

        [Fact]
        public void Apply_PlacesEachEndOfAnArrow()
        {
            var from = new RectangleElement(Guid.NewGuid(), new RectD(100, 100, 50, 50));
            var to = new EllipseElement(Guid.NewGuid(), new RectD(400, 300, 40, 40));
            var arrow = new ArrowElement(Guid.NewGuid(), new PointD(0, 0), new PointD(1, 1))
            {
                StartAnchor = new Anchor { Target = from.Id, X = new AxisAnchor(AnchorLine.End), Y = new AxisAnchor(AnchorLine.Center) },
                EndAnchor = new Anchor { Target = to.Id, X = new AxisAnchor(AnchorLine.Start, Offset: -5), Y = new AxisAnchor(AnchorLine.Center) }
            };

            var placed = Placed<ArrowElement>(WithCapture(new RectD(0, 0, 800, 600), from, to, arrow), arrow.Id);

            Assert.Equal(new PointD(150, 125), placed.Start);
            Assert.Equal(new PointD(395, 320), placed.End);
        }

        [Fact]
        public void Apply_MovesTheTailWithTheBubble_UnlessTheTailHasItsOwnAnchor()
        {
            var target = new RectangleElement(Guid.NewGuid(), new RectD(300, 300, 20, 20));
            var bubble = new SpeechBubbleElement(Guid.NewGuid(), new RectD(0, 0, 100, 40), "Hi", new PointD(50, 80))
            {
                Anchor = new Anchor { X = new AxisAnchor(AnchorLine.Start, Offset: 10), Y = new AxisAnchor(AnchorLine.Start, Offset: 10) }
            };
            var document = WithCapture(new RectD(0, 0, 800, 600), target, bubble);

            var moved = Placed<SpeechBubbleElement>(document, bubble.Id);
            Assert.Equal(new RectD(10, 10, 100, 40), moved.Area);
            Assert.Equal(new PointD(60, 90), moved.Tail);
            Assert.Equal(new RectD(10, 10, 100, 80), moved.Bounds);

            var pointing = bubble with { TailAnchor = new Anchor { Target = target.Id, X = new AxisAnchor(AnchorLine.Center), Y = new AxisAnchor(AnchorLine.Start) } };
            Assert.Equal(new PointD(310, 300), Placed<SpeechBubbleElement>(WithCapture(new RectD(0, 0, 800, 600), target, pointing), bubble.Id).Tail);
        }

        [Fact]
        public void Apply_LeavesAPartWithAMissingTargetWhereItIs()
        {
            var box = new RectangleElement(Guid.NewGuid(), new RectD(5, 6, 7, 8)) { Anchor = new Anchor { Target = Guid.NewGuid(), X = new AxisAnchor(AnchorLine.End) } };

            Assert.Equal(box, Placed<RectangleElement>(WithCapture(new RectD(0, 0, 800, 600), box), box.Id));
        }

        [Fact]
        public void Loops_AreRefusedWhenSet_AndWhenWritten()
        {
            var first = new RectangleElement(Guid.NewGuid(), new RectD(0, 0, 10, 10));
            var second = new RectangleElement(Guid.NewGuid(), new RectD(0, 0, 10, 10)) { Anchor = new Anchor { Target = first.Id, X = new AxisAnchor(AnchorLine.End) } };
            var third = new RectangleElement(Guid.NewGuid(), new RectD(0, 0, 10, 10)) { Anchor = new Anchor { Target = second.Id, X = new AxisAnchor(AnchorLine.End) } };
            var document = WithCapture(new RectD(0, 0, 800, 600), first, second, third);

            Assert.True(Anchors.MakesLoop(document, first.Id, third.Id));
            Assert.True(Anchors.MakesLoop(document, first.Id, first.Id));
            Assert.False(Anchors.MakesLoop(document, third.Id, first.Id));
            Assert.False(Anchors.MakesLoop(document, first.Id, null));

            var looped = WithCapture(new RectD(0, 0, 800, 600), first with { Anchor = new Anchor { Target = third.Id, X = new AxisAnchor(AnchorLine.End) } }, second, third);
            Assert.Throws<InvalidDataException>(() => Anchors.Apply(looped));
            Assert.Throws<InvalidDataException>(() => GreenshotPackage.Write(looped, new MemoryStream(), _ => "png"));
        }
    }
}
