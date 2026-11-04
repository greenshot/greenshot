using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Adorners;
using Xunit;

namespace Greenshot.Tests.Editor
{
    [Collection(TestCollections.InputState)]
    public class SnappingGestureTests
    {
        public SnappingGestureTests() => TestEnvironment.EnsureInitialized();
        [Theory]
        [InlineData(false, 1)]
        [InlineData(true, 1)]
        [InlineData(false, 2)]
        [InlineData(true, 2)]
        public void ExistingHandleWithCreationTool_SnapsGeometryWithoutMovingActualPointerAndCleansUp(bool rotated, int zoom)
        {
            DimensionFormatTests.Sta(() =>
            {
                var originalPointer = Cursor.Position; var modifiers = InputState.ModifierKeysSource;
                try
                {
                    InputState.ModifierKeysSource = () => Keys.None;
                    using var surface = new Surface(new Bitmap(350, 260));
                    using var form = new Form { ClientSize = new Size(750, 550), StartPosition = FormStartPosition.Manual, Location = new Point(30, 30) };
                    form.Controls.Add(surface); form.Show(); Application.DoEvents();
                    surface.ZoomFactor = new Fraction((uint)zoom, 1);
                    var line = new LineContainer(surface) { Left = 60, Top = 60, Width = 80, Height = 60 };
                    surface.AddElement(line, false); surface.SelectElement(line);
                    if (rotated) surface.RotateSelectedObjects(37);
                    surface.DrawingMode = DrawingModes.Rect; surface.SelectElement(line);
                    surface.AddElement(new LineContainer(surface) { Left = 230, Top = 180, Width = 40, Height = 0 }, false);
                    surface.SetObjectSnapping(true, true, 8);
                    var start = Point.Round(line.ModelToImage(new PointF(line.Left + line.Width, line.Top + line.Height)));
                    Mouse(surface, "SurfaceMouseDown", start, zoom);
                    Assert.False(surface.CanAttractDrawingPointer);
                    var pointer = surface.PointToScreen(new Point(232 * zoom, 178 * zoom)); Cursor.Position = pointer;
                    Mouse(surface, "SurfaceMouseMove", new Point(232, 178), zoom);
                    Assert.Equal(pointer, Cursor.Position);
                    var end = line.ModelToImage(new PointF(line.Left + line.Width, line.Top + line.Height));
                    Assert.InRange(Math.Abs(end.X - 230), 0, 2); Assert.InRange(Math.Abs(end.Y - 180), 0, 2);
                    Mouse(surface, "SurfaceMouseUp", new Point(232, 178), zoom);
                    Assert.True(surface.CanAttractDrawingPointer); Assert.False(surface.Capture);
                    Assert.All(line.Adorners, adorner => Assert.False(adorner.IsActive));
                    Mouse(surface, "SurfaceMouseDown", Point.Round(end), zoom);
                    surface.Capture = false;
                    Assert.True(surface.CanAttractDrawingPointer); Assert.All(line.Adorners, adorner => Assert.False(adorner.IsActive));
                }
                finally { InputState.ModifierKeysSource = modifiers; Cursor.Position = originalPointer; }
            });
        }
        [Fact]
        public void PolygonCreationAttractsButVertexAndBoxEditingNeverAttractAndCancellationClearsState()
        {
            DimensionFormatTests.Sta(() =>
            {
                using var surface = new Surface(new Bitmap(400, 300));
                surface.DrawingMode = DrawingModes.Polygon; Assert.True(surface.CanAttractDrawingPointer);
                Mouse(surface, "SurfaceMouseDown", new Point(50,50), 1);
                Mouse(surface, "SurfaceMouseDown", new Point(150,50), 1);
                Mouse(surface, "SurfaceMouseDown", new Point(150,150), 1);
                Mouse(surface, "SurfaceMouseDown", new Point(50,50), 1);
                var polygon = Assert.IsType<PolygonContainer>(Assert.Single(surface.Elements));
                Mouse(surface, "SurfaceMouseDown", new Point(150,50), 1);
                Assert.False(surface.CanAttractDrawingPointer);
                Mouse(surface, "SurfaceMouseMove", new Point(180,70), 1);
                surface.ProcessCmdKey(Keys.Escape); Assert.Equal(new Point(150,50), polygon.Vertices[1]); Assert.False(surface.Capture);
                surface.BeginSelectionBox(Point.Empty, Keys.None); Assert.False(surface.CanAttractDrawingPointer);
                surface.CancelSelectionBox(); surface.DrawingMode = DrawingModes.Rect;
                Assert.True(surface.CanAttractDrawingPointer);
            });
        }
        [Fact]
        public void FirstClickCreationStillAttractsActualPointer()
        {
            DimensionFormatTests.Sta(() =>
            {
                var original = Cursor.Position;
                try
                {
                    using var surface = new Surface(new Bitmap(350,260)); using var form = new Form { ClientSize = new Size(400,300) };
                    form.Controls.Add(surface); form.Show(); Application.DoEvents();
                    surface.AddElement(new LineContainer(surface) { Left=200,Top=180,Width=40,Height=0 }, false);
                    surface.DrawingMode = DrawingModes.Rect; surface.SetObjectSnapping(true,false,8);
                    Cursor.Position = surface.PointToScreen(new Point(202,182));
                    Mouse(surface,"SurfaceMouseMove",new Point(202,182),1);
                    Assert.Equal(surface.PointToScreen(new Point(200,180)),Cursor.Position);
                }
                finally { Cursor.Position = original; }
            });
        }
        private static void Mouse(Surface surface, string method, Point point, int zoom) =>
            typeof(Surface).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(surface,
                new object[] { surface, new MouseEventArgs(MouseButtons.Left, 1, point.X * zoom, point.Y * zoom, 0) });
    }
}
