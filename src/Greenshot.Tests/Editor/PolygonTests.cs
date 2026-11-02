using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.FileFormatHandlers;
using Greenshot.Editor.Helpers;
using Greenshot.Editor.Measurements;
using Xunit;

namespace Greenshot.Tests.Editor
{
    [Collection(TestCollections.Clipboard)]
    public class PolygonTests
    {
        public PolygonTests() => TestEnvironment.EnsureInitialized();
        private static PolygonContainer Triangle(Surface surface)
        {
            var polygon = new PolygonContainer(surface);
            polygon.SetVertices(new[] { new Point(10, 10), new Point(110, 10), new Point(10, 110) });
            surface.AddElement(polygon); return polygon;
        }

        [Fact]
        public void Geometry_TranslationResizeTransformAndUndoKeepIndexedVertices()
        {
            using var surface = new Surface(new Bitmap(300, 300));
            var polygon = Triangle(surface); var before = polygon.Vertices;
            polygon.MakeBoundsChangeUndoable(false);
            polygon.MoveBy(20, 30);
            polygon.ApplyBounds(new NativeRectFloat(30, 40, 200, 100));
            Assert.Equal(new Point(230, 40), polygon.Vertices[1]);
            surface.Undo(); Assert.Equal(before, polygon.Vertices);
            surface.Redo(); Assert.Equal(new Point(230, 40), polygon.Vertices[1]);
            using var rotate = new Matrix(0, 1, -1, 0, 300, 0);
            var expected = polygon.Vertices; rotate.TransformPoints(expected);
            polygon.Transform(rotate); Assert.Equal(expected, polygon.Vertices);
            Assert.True(polygon.ClickableAt(250, 50));
            Assert.False(polygon.ClickableAt(0, 0));
        }

        [Fact]
        public void Winding_UsesVisualAnticlockwiseNeighbourAndStableDegenerateFallback()
        {
            using var surface = new Surface(); var polygon = Triangle(surface);
            Assert.Equal(2, polygon.AnticlockwiseNeighbour(0));
            polygon.SetVertices(polygon.Vertices.AsEnumerable().Reverse().ToArray());
            Assert.Equal(1, polygon.AnticlockwiseNeighbour(0));
            polygon.SetVertices(new[] { new Point(0, 0), new Point(10, 0), new Point(20, 0) });
            Assert.Equal(2, polygon.AnticlockwiseNeighbour(0));
            polygon.SetVertices(new[] { Point.Empty, Point.Empty, Point.Empty });
            Assert.Equal(0, polygon.AnticlockwiseNeighbour(1));
        }

        [Fact]
        public void NumericVertexEdit_IsCalibratedUnsnappedAndAtomic()
        {
            using var surface = new Surface(new Bitmap(300, 300)); var polygon = Triangle(surface);
            polygon.SelectedVertex = 1;
            surface.SetObjectSnapping(true, true, 100);
            var units = MeasurementState.Calibrate(100, 10, MeasurementUnit.Centimetres);
            Assert.True(MeasurementGeometry.TryEdit(polygon, units, "VX", -2.5, out var error), error);
            Assert.Equal(new Point(-25, 10), polygon.Vertices[1]);
            surface.Undo(); Assert.Equal(new Point(110, 10), polygon.Vertices[1]);
            surface.Redo(); Assert.Equal(new Point(-25, 10), polygon.Vertices[1]);
            Assert.False(MeasurementGeometry.TryEdit(polygon, units, "Area", 5, out _));
            Assert.False(MeasurementGeometry.TryEdit(polygon, units, "VY", double.NaN, out _));
            Assert.Equal(4, MeasurementGeometry.GetProperties(polygon).Count);
        }

        [Fact]
        public void Targets_IncludeVerticesAndEveryClosedEdgeMidpoint()
        {
            using var surface = new Surface(); var polygon = Triangle(surface);
            var targets = ObjectSnapHelper.Targets(polygon).ToArray();
            Assert.Equal(6, targets.Length);
            Assert.Contains(new PointF(60, 60), targets);
            Assert.Contains(new PointF(10, 60), targets);
            Assert.Equal(5000, polygon.SignedArea);
            Assert.Equal(200 + Math.Sqrt(20000), MeasurementGeometry.GetProperties(polygon).Single(p => p.Key == "Perimeter").Pixels, 8);
        }

        [Fact]
        public void ProjectAndClipboard_RoundTripIndexedGeometry()
        {
            using var surface = new Surface(new Bitmap(300, 300)); var polygon = Triangle(surface);
            var handler = new GreenshotFileFormatHandler();
            using var stream = new MemoryStream();
            Assert.True(handler.TrySaveToStream((Bitmap)surface.Image, stream, ".greenshot", surface));
            stream.Position = 0;
            using var loaded = (Surface)handler.LoadSurface(stream);
            var restored = Assert.IsType<PolygonContainer>(Assert.Single(loaded.Elements));
            Assert.Equal(polygon.Vertices, restored.Vertices);
            Assert.Equal(8, restored.Adorners.Count);
            var list = new DrawableContainerList { polygon };
            var pasted = DrawableContainerClipboard.Deserialize(DrawableContainerClipboard.Serialize(list));
            Assert.Equal(polygon.Vertices, Assert.IsType<PolygonContainer>(Assert.Single(pasted)).Vertices);
        }

        [Theory]
        [InlineData("Escape")]
        [InlineData("Right")]
        [InlineData("Tool")]
        [InlineData("Document")]
        public void CreationCancellation_LeavesNoObjectOrUndo(string cancel)
        {
            Sta(() =>
            {
                using var surface = new Surface(new Bitmap(300, 300)); surface.DrawingMode = DrawingModes.Polygon;
                Click(surface, 10, 10); Click(surface, 110, 10); Click(surface, 10, 110);
                Assert.Empty(surface.Elements);
                if (cancel == "Escape") Assert.True(surface.ProcessCmdKey(Keys.Escape));
                if (cancel == "Right") Mouse(surface, "SurfaceMouseDown", 20, 20, MouseButtons.Right);
                if (cancel == "Tool") surface.DrawingMode = DrawingModes.Line;
                if (cancel == "Document") surface.CancelPolygonInteraction();
                Assert.Empty(surface.Elements);
                Assert.Empty((System.Collections.Generic.List<Point>)typeof(Surface).GetField("_polygonPoints", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(surface));
            });
        }

        [Fact]
        public void ShiftAndSnapping_ConstrainCreationAndVisualNeighbourBeforeTargets()
        {
            Sta(() =>
            {
                using var surface = new Surface(new Bitmap(300, 300)); var polygon = Triangle(surface);
                surface.SelectElement(polygon);
                // Clockwise storage: visual anticlockwise neighbour of vertex 1 is vertex 3.
                Mouse(surface, "SurfaceMouseDown", 10, 10);
                surface.MovePolygonVertex(new Point(60, 108), Keys.Shift);
                Assert.Equal(new Point(60, 110), polygon.Vertices[0]);
                surface.CancelPolygonInteraction();
                var anchor = new Point(10, 10);
                surface.AddElement(new LineContainer(surface) { Left = 112, Top = 13 });
                surface.SetObjectSnapping(true, true, 8);
                Assert.Equal(new Point(110, 10), surface.PolygonEndpoint(new Point(110, 13), anchor, polygon, Keys.Shift));
                surface.AddElement(new LineContainer(surface) { Left = 114, Top = 10 });
                Assert.Equal(new Point(114, 10), surface.PolygonEndpoint(new Point(110, 13), anchor, polygon, Keys.Shift));
            });
        }

        [Fact]
        public void ZoomedVertexHitAndPreviewExport_UseImageCoordinatesWithoutOverlays()
        {
            Sta(() =>
            {
                using var surface = new Surface(new Bitmap(300, 300));
                surface.ZoomFactor = new Greenshot.Base.Core.Fraction(2, 1);
                surface.DrawingMode = DrawingModes.Polygon;
                Click(surface, 20, 20); Click(surface, 220, 20); Click(surface, 20, 220);
                using (var preview = (Bitmap)surface.GetImageForExport()) Assert.Equal(0, preview.GetPixel(60, 10).A);
                Click(surface, 20, 20);
                var polygon = Assert.IsType<PolygonContainer>(Assert.Single(surface.Elements));
                Assert.Equal(new Point(110, 10), polygon.Vertices[1]);
                Mouse(surface, "SurfaceMouseDown", 220, 20); Mouse(surface, "SurfaceMouseMove", 260, 60); Mouse(surface, "SurfaceMouseUp", 260, 60);
                Assert.Equal(new Point(130, 30), polygon.Vertices[1]);
                using var export = (Bitmap)surface.GetImageForExport();
                Assert.NotEqual(Color.DeepSkyBlue.ToArgb(), export.GetPixel(130, 30).ToArgb());
            });
        }

        [Fact]
        public void CreationCloseAndVertexDrag_AreEachOneUndoAndEscapeRestoresDrag()
        {
            Sta(() =>
            {
                using var surface = new Surface(new Bitmap(300, 300)); surface.DrawingMode = DrawingModes.Polygon;
                Click(surface, 10, 10); Click(surface, 110, 10); Click(surface, 10, 110); Click(surface, 10, 10);
                var polygon = Assert.IsType<PolygonContainer>(Assert.Single(surface.Elements));
                var before = polygon.Vertices;
                Mouse(surface, "SurfaceMouseDown", 110, 10);
                Mouse(surface, "SurfaceMouseMove", 130, 30);
                Mouse(surface, "SurfaceMouseMove", 150, 50);
                Mouse(surface, "SurfaceMouseUp", 150, 50);
                Assert.Equal(new Point(150, 50), polygon.Vertices[1]);
                surface.Undo(); Assert.Equal(before, polygon.Vertices);
                surface.Redo(); Assert.Equal(new Point(150, 50), polygon.Vertices[1]);
                Mouse(surface, "SurfaceMouseDown", 150, 50); Mouse(surface, "SurfaceMouseMove", 180, 70);
                Assert.True(surface.ProcessCmdKey(Keys.Escape));
                Assert.Equal(new Point(150, 50), polygon.Vertices[1]);
                surface.Undo(); Assert.Equal(before, polygon.Vertices);
                surface.Undo(); Assert.Empty(surface.Elements);
            });
        }

        private static void Click(Surface s, int x, int y) { Mouse(s, "SurfaceMouseDown", x, y); Mouse(s, "SurfaceMouseUp", x, y); }
        private static void Mouse(Surface surface, string method, int x, int y, MouseButtons button = MouseButtons.Left) =>
            typeof(Surface).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(surface, new object[] { surface, new MouseEventArgs(button, 1, x, y, 0) });
        private static void Sta(Action action)
        {
            Exception error = null;
            var thread = new Thread(() => { try { action(); } catch (Exception e) { error = e; } });
            thread.SetApartmentState(ApartmentState.STA); thread.Start();
            Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "UI test timed out");
            if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
        }
    }
}
