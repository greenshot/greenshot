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
using Greenshot.Document.Editing;
using Xunit;

namespace Greenshot.Tests.Document
{
    public class DocumentEditsTests
    {
        private static readonly Resource Capture = new(Guid.NewGuid(), "image/png", new byte[] { 1 }) { Width = 200, Height = 100 };

        private static GreenshotDocument NewDocument() => DocumentEdits.FromImage(Capture, 200, 100);

        [Fact]
        public void Add_PutsElementsOnTheAnnotationsLayer()
        {
            var rectangle = new RectangleElement(Guid.NewGuid(), new RectD(10, 10, 20, 20));
            var document = DocumentEdits.Add(NewDocument(), new[] { rectangle });

            Assert.Same(rectangle, document.Layers.Single(layer => layer.Kind == LayerKind.Annotations).Elements.Single());
            Assert.Same(rectangle, DocumentEdits.Find(document, rectangle.Id));
        }

        [Fact]
        public void Remove_AlsoRemovesTheFrameOfTheElementAndEmptySets()
        {
            var rectangle = new RectangleElement(Guid.NewGuid(), new RectD(10, 10, 20, 20));
            var frame = new FrameElement(Guid.NewGuid(), rectangle.Area, 1) { Target = rectangle.Id };
            var document = DocumentEdits.Add(NewDocument(), new Element[] { rectangle, frame }) with
            {
                Groups = new List<Group> { new(Guid.NewGuid(), GroupTypes.Set, new Guid?[] { rectangle.Id }) }
            };

            var removed = DocumentEdits.Remove(document, new[] { rectangle.Id });

            Assert.Null(DocumentEdits.Find(removed, rectangle.Id));
            Assert.Null(DocumentEdits.Find(removed, frame.Id));
            Assert.Empty(removed.Groups);
        }

        [Fact]
        public void Arrange_MovesInTheDrawingOrder()
        {
            var first = new RectangleElement(Guid.NewGuid(), new RectD(0, 0, 10, 10));
            var second = new EllipseElement(Guid.NewGuid(), new RectD(0, 0, 10, 10));
            var third = new TextElement(Guid.NewGuid(), new RectD(0, 0, 10, 10), "x");
            var document = DocumentEdits.Add(NewDocument(), new Element[] { first, second, third });

            var front = DocumentEdits.Arrange(document, new[] { first.Id }, Arrangement.ToFront);
            var back = DocumentEdits.Arrange(document, new[] { third.Id }, Arrangement.ToBack);

            Assert.Equal(new[] { second.Id, third.Id, first.Id }, Annotations(front));
            Assert.Equal(new[] { third.Id, first.Id, second.Id }, Annotations(back));
        }

        [Fact]
        public void Crop_IsAFixedCanvasWithEverythingMoved()
        {
            var rectangle = new RectangleElement(Guid.NewGuid(), new RectD(50, 40, 20, 20));
            var document = DocumentEdits.Add(NewDocument(), new[] { rectangle });

            var cropped = DocumentEdits.Crop(document, new RectD(40, 30, 100, 50));

            Assert.Equal(new RectD(0, 0, 100, 50), cropped.CanvasArea());
            Assert.Equal(new RectD(10, 10, 20, 20), ((RectangleElement)DocumentEdits.Find(cropped, rectangle.Id)).Area);
            // Nothing is cut away: the capture is only moved
            Assert.Equal(new RectD(-40, -30, 200, 100), DocumentEdits.CaptureOf(cropped).Area);
        }

        [Fact]
        public void Scale_ScalesAreasAndPoints()
        {
            var arrow = new ArrowElement(Guid.NewGuid(), new PointD(10, 10), new PointD(30, 20));
            var document = DocumentEdits.Add(NewDocument(), new[] { arrow });

            var scaled = DocumentEdits.Scale(document, 2, 0.5);

            var scaledArrow = (ArrowElement)DocumentEdits.Find(scaled, arrow.Id);
            Assert.Equal(new PointD(20, 5), scaledArrow.Start);
            Assert.Equal(new PointD(60, 10), scaledArrow.End);
            Assert.Equal(new RectD(0, 0, 400, 50), DocumentEdits.CaptureOf(scaled).Area);
        }

        [Fact]
        public void HitTesting_FindsTheTopmostElementAndIgnoresTheLockedCapture()
        {
            var below = new RectangleElement(Guid.NewGuid(), new RectD(0, 0, 50, 50)) { Style = new Style { Fill = FillStyle.Of(new Rgba(0, 0, 255, 255)) } };
            var above = new EllipseElement(Guid.NewGuid(), new RectD(10, 10, 30, 30)) { Style = new Style { Fill = FillStyle.Of(new Rgba(0, 255, 0, 255)) } };
            var document = DocumentEdits.Add(NewDocument(), new Element[] { below, above });

            Assert.Same(above, HitTesting.ElementAt(document, new PointD(25, 25), 1));
            Assert.Same(below, HitTesting.ElementAt(document, new PointD(3, 3), 1));
            Assert.Null(HitTesting.ElementAt(document, new PointD(150, 80), 1));
        }

        [Fact]
        public void HitTesting_AnUnfilledRectangleIsOnlyHitOnItsLine()
        {
            var rectangle = new RectangleElement(Guid.NewGuid(), new RectD(0, 0, 50, 50));
            var document = DocumentEdits.Add(NewDocument(), new[] { rectangle });

            Assert.Null(HitTesting.ElementAt(document, new PointD(25, 25), 2));
            Assert.Same(rectangle, HitTesting.ElementAt(document, new PointD(50, 25), 2));
        }

        [Fact]
        public void ElementsIn_WithTouchingAlsoGivesElementsHalfInside()
        {
            var inside = new RectangleElement(Guid.NewGuid(), new RectD(10, 10, 10, 10));
            var half = new RectangleElement(Guid.NewGuid(), new RectD(25, 10, 20, 10));
            var document = DocumentEdits.Add(NewDocument(), new Element[] { inside, half });
            var area = new RectD(0, 0, 30, 30);

            Assert.Equal(new[] { inside.Id }, HitTesting.ElementsIn(document, area, false).Select(element => element.Id));
            Assert.Equal(new[] { inside.Id, half.Id }, HitTesting.ElementsIn(document, area, true).Select(element => element.Id).OrderBy(id => id == half.Id));
        }

        private static IEnumerable<Guid> Annotations(GreenshotDocument document) =>
            document.Layers.Single(layer => layer.Kind == LayerKind.Annotations).Elements.Select(element => element.Id);
    }
}
