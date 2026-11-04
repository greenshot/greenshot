using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EditorToolbar_FirstReleaseRetainsAnchorAndPreviewThenCompletesPolygon(bool moveWhileHeld)
        {
            Sta(() =>
            {
                using var surface = new Surface(new Bitmap(400, 400));
                using var editor = new Greenshot.Editor.Forms.ImageEditorForm(surface, true, false);
                editor.Show(); Application.DoEvents();
                var button = (ToolStripButton)typeof(Greenshot.Editor.Forms.ImageEditorForm).GetField("_polygonButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(editor);
                button.PerformClick();
                int downs = 0, ups = 0;
                int captureLosses = 0;
                surface.MouseDown += (s, e) => downs++;
                surface.MouseUp += (s, e) => ups++;
                surface.MouseCaptureChanged += (s, e) => { if (!surface.Capture) captureLosses++; };
                NativeMouse(surface, 0x201, 50, 50, true);
                Assert.True(downs == 1, "Native mouse-down dispatch count: " + downs);
                if (moveWhileHeld) NativeMouse(surface, 0x200, 80, 65, true);
                NativeMouse(surface, 0x202, moveWhileHeld ? 80 : 50, moveWhileHeld ? 65 : 50);
                Assert.True(ups == 1, "Native mouse-up dispatch count: " + ups);
                Assert.True(captureLosses > 0, "The regression must exercise the native release of mouse capture.");
                Assert.Equal(DrawingModes.Polygon, surface.DrawingMode);
                var anchored = (System.Collections.Generic.List<Point>)typeof(Surface).GetField("_polygonPoints", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(surface);
                Assert.Equal(new[] { new Point(50, 50) }, anchored);
                NativeMouse(surface, 0x200, 200, 50);
                using (var preview = new Bitmap(surface.Width, surface.Height))
                {
                    surface.DrawToBitmap(preview, new Rectangle(Point.Empty, preview.Size));
                    int blue = 0;
                    for (int y = 40; y <= 60; y++) for (int x = 40; x <= 210; x++)
                        if (preview.GetPixel(x, y).ToArgb() == Color.DeepSkyBlue.ToArgb()) blue++;
                    Assert.True(blue > 20, "The anchored first segment must remain visible after release.");
                }
                foreach (var point in new[] { new Point(200, 50), new Point(50, 200), new Point(50, 50) })
                { NativeMouse(surface, 0x201, point.X, point.Y, true); NativeMouse(surface, 0x202, point.X, point.Y); }
                Application.DoEvents();
                var polygon = Assert.IsType<PolygonContainer>(Assert.Single(surface.Elements));
                Assert.Equal(new[] { new Point(50, 50), new Point(200, 50), new Point(50, 200) }, polygon.Vertices);
                Assert.True(polygon.Selected); Assert.Equal(DrawingModes.None, surface.DrawingMode);
            });
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);
        private static void NativeMouse(Surface surface, int message, int x, int y, bool pressed = false)
        {
            SendMessage(surface.Handle, message, new IntPtr(pressed ? 1 : 0), new IntPtr((y << 16) | (x & 0xffff)));
            Application.DoEvents();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void NativeVertexDrag_NormalReleaseCommitsAndCaptureLossRollsBack(bool interruptCapture)
        {
            Sta(() =>
            {
                using var surface = new Surface(new Bitmap(400, 400));
                using var editor = new Greenshot.Editor.Forms.ImageEditorForm(surface, true, false);
                editor.Show(); Application.DoEvents();
                var polygon = Triangle(surface); surface.SelectElement(polygon);
                var before = polygon.Vertices; int history = UndoCount(surface); surface.Modified = false;
                NativeMouse(surface, 0x201, 110, 10, true);
                NativeMouse(surface, 0x200, 140, 40, true);
                Assert.NotEqual(before, polygon.Vertices);
                if (interruptCapture) { surface.Capture = false; Application.DoEvents(); }
                NativeMouse(surface, 0x202, 140, 40);
                Assert.False(surface.Capture);
                if (interruptCapture)
                {
                    Assert.Equal(before, polygon.Vertices); Assert.Equal(history, UndoCount(surface)); Assert.False(surface.Modified);
                }
                else
                {
                    Assert.Equal(new Point(140, 40), polygon.Vertices[1]); Assert.Equal(history + 1, UndoCount(surface));
                    surface.Undo(); Assert.Equal(before, polygon.Vertices);
                    surface.Redo(); Assert.Equal(new Point(140, 40), polygon.Vertices[1]);
                }
            });
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

        [Theory]
        [InlineData(1, 1, false)]
        [InlineData(2, 1, false)]
        [InlineData(1, 2, false)]
        [InlineData(1, 1, true)]
        [InlineData(2, 1, true)]
        [InlineData(1, 2, true)]
        public void OffsetVertexClickAndJitter_SelectWithoutGeometryUndoOrModification(int numerator, int denominator, bool rotated)
        {
            Sta(() =>
            {
                using var surface = new Surface(new Bitmap(400, 400)); var polygon = Triangle(surface);
                surface.SelectElement(polygon); if (rotated) surface.RotateSelectedObjects(37);
                var vertex = polygon.ImageVertices[1];
                surface.AddElement(new LineContainer(surface) { Left=(int)vertex.X+4, Top=(int)vertex.Y+2, Width=30 });
                surface.SetObjectSnapping(true,true,8);
                surface.ZoomFactor = new Greenshot.Base.Core.Fraction((uint)numerator,(uint)denominator);
                double zoom=(double)numerator/denominator;
                int x=(int)Math.Round(vertex.X*zoom)+2, y=(int)Math.Round(vertex.Y*zoom);
                var before=polygon.ModelVertices; var affine=polygon.ObjectTransformState;
                int history=UndoCount(surface); surface.Modified=false;
                foreach(int jitter in new[]{0,1})
                {
                    polygon.SelectedVertex=0;
                    Mouse(surface,"SurfaceMouseDown",x,y);
                    if(jitter!=0) Mouse(surface,"SurfaceMouseMove",x+jitter,y+jitter);
                    Mouse(surface,"SurfaceMouseUp",x+jitter,y+jitter);
                    Assert.Equal(1,polygon.SelectedVertex); Assert.Equal(before,polygon.ModelVertices);
                    Assert.Equal(affine,polygon.ObjectTransformState); Assert.Equal(history,UndoCount(surface));
                    Assert.False(surface.Modified); Assert.False(surface.Capture);
                }
            });
        }

        [Fact]
        public void OffsetVertexDrag_PreservesGrabOffsetCommitsOnceAndReturnToStartIsNoOp()
        {
            Sta(() =>
            {
                using var surface=new Surface(new Bitmap(400,400)); var polygon=Triangle(surface); surface.SelectElement(polygon);
                var before=polygon.ModelVertices; int history=UndoCount(surface); surface.Modified=false;
                Mouse(surface,"SurfaceMouseDown",113,10); Mouse(surface,"SurfaceMouseMove",133,25); Mouse(surface,"SurfaceMouseUp",133,25);
                Assert.Equal(new Point(130,25),polygon.Vertices[1]); Assert.Equal(history+1,UndoCount(surface)); Assert.True(surface.Modified);
                surface.Undo(); Assert.Equal(before,polygon.ModelVertices); surface.Redo(); Assert.Equal(new Point(130,25),polygon.Vertices[1]);
                var after=polygon.ModelVertices; history=UndoCount(surface); surface.Modified=false;
                Mouse(surface,"SurfaceMouseDown",133,25); Mouse(surface,"SurfaceMouseMove",153,45); Mouse(surface,"SurfaceMouseUp",133,25);
                Assert.Equal(after,polygon.ModelVertices); Assert.Equal(history,UndoCount(surface)); Assert.False(surface.Modified);
                Assert.False(surface.Capture);
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void OffsetRotatedVertexDrag_CancellationRestoresGeometryAndHistory(bool changeTool)
        {
            Sta(() =>
            {
                using var surface=new Surface(new Bitmap(400,400)); var polygon=Triangle(surface); surface.SelectElement(polygon);
                surface.RotateSelectedObjects(31); surface.ZoomFactor=new Greenshot.Base.Core.Fraction(2,1);
                var vertex=polygon.ImageVertices[1]; int x=(int)Math.Round(vertex.X*2)+2, y=(int)Math.Round(vertex.Y*2);
                var before=polygon.ModelVertices; var affine=polygon.ObjectTransformState; int history=UndoCount(surface); surface.Modified=false;
                Mouse(surface,"SurfaceMouseDown",x,y); Mouse(surface,"SurfaceMouseMove",x+40,y+30);
                Assert.NotEqual(before,polygon.ModelVertices);
                var moved=polygon.ImageVertices[1]; Assert.InRange(Math.Abs(moved.X-vertex.X-20),0,1); Assert.InRange(Math.Abs(moved.Y-vertex.Y-15),0,1);
                if(changeTool)surface.DrawingMode=DrawingModes.Rect; else Assert.True(surface.ProcessCmdKey(Keys.Escape));
                Assert.Equal(before,polygon.ModelVertices); Assert.Equal(affine,polygon.ObjectTransformState);
                Assert.Equal(history,UndoCount(surface)); Assert.False(surface.Modified); Assert.False(surface.Capture);
            });
        }

        private static int UndoCount(Surface surface) => ((ICollection)typeof(Surface).GetField("_undoStack",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(surface)).Count;
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
