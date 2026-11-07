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
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Xunit;
namespace Greenshot.Tests.Editor
{
    public class PolygonFillTests
    {
        public PolygonFillTests() => TestEnvironment.EnsureInitialized();

        [Fact]
        public void CompletionRequiresThreeVerticesAndClosesFilledInterior()
        {
            using var s = new Surface(new Bitmap(200, 200));
            var polygon = new PolygonContainer(s);
            polygon.AddPoint(new Point(20, 20), false);
            polygon.AddPoint(new Point(120, 20), false);
            Assert.False(polygon.InitContent());
            polygon.AddPoint(new Point(120, 120), false);
            Assert.True(polygon.InitContent()); Assert.True(polygon.IsClosed);
            polygon.SetFieldValue(FieldType.FILL_COLOR, Color.Blue);
            polygon.SetFieldValue(FieldType.SHADOW, false);
            Assert.True(polygon.ClickableAt(90, 40));
            using var bitmap = new Bitmap(200, 200);
            using (var g = Graphics.FromImage(bitmap)) polygon.Draw(g, RenderMode.EXPORT);
            Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(90, 40).ToArgb());
            polygon.SetFieldValue(FieldType.FILL_COLOR, Color.Transparent);
            Assert.False(polygon.ClickableAt(90, 40));
        }
        [Fact]
        public void SavedPolygonRetainsFillAndEditableVertices()
        {
            using var s = new Surface(new Bitmap(200, 200));
            var polygon = new PolygonContainer(s);
            polygon.AddPoint(new Point(20, 20), false);
            polygon.AddPoint(new Point(120, 20), false);
            polygon.AddPoint(new Point(120, 120), false);
            polygon.InitContent();
            polygon.Status = EditStatus.IDLE;
            polygon.SetFieldValue(FieldType.FILL_COLOR, Color.Blue);
            s.AddElement(polygon, false);
            using var stream = new System.IO.MemoryStream();
            s.SaveElementsToStream(stream); stream.Position = 0;
            using var loaded = new Surface(new Bitmap(200, 200));
            loaded.LoadElementsFromStream(stream);
            var restored = Assert.IsType<PolygonContainer>(Assert.Single(loaded.Elements));
            Assert.True(restored.IsClosed);
            Assert.Equal(Color.Blue.ToArgb(), restored.GetFieldValueAsColor(FieldType.FILL_COLOR).ToArgb());
            var original = restored.Points;
            var undo = restored.CreateVertexChangeMemento();
            restored.MoveVertex(1, new Point(90, 30));
            undo.Restore();
            Assert.Equal(original, restored.Points);
        }
        [Fact]
        public void PolygonModeUsesSharedClickLifecycleAndOneCreationUndo()
        {
            using var s = new Surface(new Bitmap(200, 200)) { DrawingMode = Greenshot.Base.Interfaces.DrawingModes.Polygon };
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            foreach (var point in new[] { new Point(20, 20), new Point(120, 20), new Point(120, 120) })
            {
                foreach (var name in new[] { "OnMouseDown", "OnMouseUp" })
                {
                    typeof(System.Windows.Forms.Control).GetMethod(name, flags).Invoke(s,
                        new object[] { new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.Left, 1, point.X, point.Y, 0) });
                }
            }
            Assert.True(s.ProcessCanvasKey(System.Windows.Forms.Keys.Enter));
            var polygon = Assert.IsType<PolygonContainer>(Assert.Single(s.Elements));
            Assert.True(polygon.IsClosed); Assert.Equal(3, polygon.Points.Length);
            s.Undo(); Assert.Empty(s.Elements);
            s.Redo(); Assert.IsType<PolygonContainer>(Assert.Single(s.Elements));
        }
    }
}