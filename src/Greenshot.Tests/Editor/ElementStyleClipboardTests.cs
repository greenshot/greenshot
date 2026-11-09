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

using System.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Xunit;

namespace Greenshot.Tests.Editor
{
    /// <summary>
    /// Copy and apply style and size of elements. The copied style is static: the tests of this class run one after the other.
    /// </summary>
    public class ElementStyleClipboardTests
    {
        public ElementStyleClipboardTests()
        {
            TestEnvironment.EnsureInitialized();
            ElementStyleClipboard.Clear();
        }

        [Fact]
        public void ApplyStyle_GivesEachElementTheFieldsItHas_AsOneUndoStep()
        {
            using var surface = new Surface(new Bitmap(200, 100));
            var source = new RectangleContainer(surface);
            source.SetFieldValue(FieldType.LINE_COLOR, Color.Red);
            source.SetFieldValue(FieldType.LINE_THICKNESS, 7);
            source.SetFieldValue(FieldType.FILL_COLOR, Color.Blue);
            var ellipse = new EllipseContainer(surface);
            ellipse.SetFieldValue(FieldType.LINE_COLOR, Color.Green);
            ellipse.SetFieldValue(FieldType.FILL_COLOR, Color.Yellow);
            var line = new LineContainer(surface);
            line.SetFieldValue(FieldType.LINE_THICKNESS, 1);
            var targets = new DrawableContainerList { ellipse, line };

            Assert.False(ElementStyleClipboard.CanApplyStyle(targets));
            Assert.True(ElementStyleClipboard.CopyStyle(source));
            Assert.True(ElementStyleClipboard.CanApplyStyle(targets));
            Assert.True(ElementStyleClipboard.ApplyStyle(surface, targets));

            Assert.Equal(Color.Red, ellipse.GetFieldValue(FieldType.LINE_COLOR));
            Assert.Equal(Color.Blue, ellipse.GetFieldValue(FieldType.FILL_COLOR));
            Assert.Equal(7, line.GetFieldValue(FieldType.LINE_THICKNESS));
            // A line has no fill
            Assert.False(line.HasField(FieldType.FILL_COLOR));
            Assert.True(surface.Modified);

            surface.Undo();
            Assert.Equal(Color.Green, ellipse.GetFieldValue(FieldType.LINE_COLOR));
            Assert.Equal(Color.Yellow, ellipse.GetFieldValue(FieldType.FILL_COLOR));
            Assert.Equal(1, line.GetFieldValue(FieldType.LINE_THICKNESS));

            surface.Redo();
            Assert.Equal(Color.Red, ellipse.GetFieldValue(FieldType.LINE_COLOR));
            Assert.Equal(7, line.GetFieldValue(FieldType.LINE_THICKNESS));
        }

        [Fact]
        public void ApplySize_KeepsTheCornerAndTheDirectionOfALine_AsOneUndoStep()
        {
            using var surface = new Surface(new Bitmap(200, 100));
            var source = new RectangleContainer(surface) { Left = 1, Top = 2, Width = 30, Height = 40 };
            var rectangle = new RectangleContainer(surface) { Left = 50, Top = 60, Width = 10, Height = 10 };
            var line = new LineContainer(surface) { Left = 100, Top = 50, Width = -20, Height = 5 };
            var targets = new DrawableContainerList { rectangle, line };

            Assert.False(ElementStyleClipboard.CanApplySize(targets));
            Assert.True(ElementStyleClipboard.CopySize(source));
            Assert.True(ElementStyleClipboard.ApplySize(surface, targets));

            Assert.Equal((50, 60, 30, 40), (rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height));
            Assert.Equal((100, 50, -30, 40), (line.Left, line.Top, line.Width, line.Height));

            surface.Undo();
            Assert.Equal((10, 10), (rectangle.Width, rectangle.Height));
            Assert.Equal((-20, 5), (line.Width, line.Height));
        }

        [Fact]
        public void Copy_NeedsExactlyOneElement()
        {
            using var surface = new Surface(new Bitmap(200, 100));
            var one = new RectangleContainer(surface);
            var two = new EllipseContainer(surface);

            Assert.True(ElementStyleClipboard.CanCopy(new DrawableContainerList { one }));
            Assert.False(ElementStyleClipboard.CanCopy(new DrawableContainerList { one, two }));
            Assert.False(ElementStyleClipboard.CanCopy(new DrawableContainerList()));
            Assert.False(ElementStyleClipboard.CanCopy(null));
        }

        [Fact]
        public void ApplyStyle_WithTheSameValues_ChangesNothing()
        {
            using var surface = new Surface(new Bitmap(200, 100));
            var source = new RectangleContainer(surface);
            ElementStyleClipboard.CopyStyle(source);

            Assert.False(ElementStyleClipboard.ApplyStyle(surface, new DrawableContainerList { source }));
            Assert.False(surface.CanUndo);
        }
    }
}
