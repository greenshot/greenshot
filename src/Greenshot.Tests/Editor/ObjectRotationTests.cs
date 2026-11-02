using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Text;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Adorners;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Drawing.Filters;
using Greenshot.Editor.Helpers;
using Greenshot.Editor.Measurements;
using Xunit;

namespace Greenshot.Tests.Editor
{
    [Collection(TestCollections.Clipboard)]
    public class ObjectRotationTests
    {
        public ObjectRotationTests() => TestEnvironment.EnsureInitialized();
        private static RectangleContainer Box(Surface s, int x = 60, int y = 90)
        {
            var shape = new RectangleContainer(s) { Left = x, Top = y, Width = 80, Height = 20 };
            shape.SetFieldValue(FieldType.SHADOW, false); shape.SetFieldValue(FieldType.LINE_THICKNESS, 0); shape.SetFieldValue(FieldType.FILL_COLOR, Color.Blue);
            s.AddElement(shape, false); s.SelectElement(shape); return shape;
        }
        private static void Near(PointF expected, PointF actual, double tolerance = .001)
        { Assert.InRange(Math.Abs(expected.X - actual.X), 0, tolerance); Assert.InRange(Math.Abs(expected.Y - actual.Y), 0, tolerance); }

        [Fact]
        public void ArbitraryRotation_RenderBoundsPublicHitTestingAndRawDrawingContract()
        {
            using var s = new Surface(new Bitmap(220, 220)); var box = Box(s);
            Assert.True(s.RotateSelectedObjects(45)); Assert.Equal(45, box.ObjectAngle, 6);
            Assert.True(box.Bounds.Width > 70 && box.Bounds.Width < 74);
            Assert.True(box.ClickableAt(85, 85)); Assert.False(box.ClickableAt(130, 75));
            Assert.Same(box, s.Elements.ClickableElementAt(85, 85));
            using var export = (Bitmap)s.GetImageForExport(); Assert.Equal(Color.Blue.ToArgb(), export.GetPixel(100, 100).ToArgb());
            for (int y = 0; y < export.Height; y++) for (int x = 0; x < export.Width; x++)
                if (export.GetPixel(x, y).A > 0) Assert.True(box.DrawingBounds.Contains(x, y));
            using var raw = new Bitmap(220, 220); using (var g = Graphics.FromImage(raw)) box.Draw(g, RenderMode.EXPORT);
            Assert.Equal(Color.Blue.ToArgb(), raw.GetPixel(65, 95).ToArgb());
            using var content = new Bitmap(220, 220); using (var g = Graphics.FromImage(content))
            {
                box.DrawContent(g, content, RenderMode.EXPORT, new NativeRect(0, 0, 220, 220));
                Assert.True(g.Transform.IsIdentity);
            }
            Assert.Equal(0, content.GetPixel(65, 95).A); Assert.Equal(export.GetPixel(100, 100), content.GetPixel(100, 100));
        }

        [Fact]
        public void RepeatedRotations_RetainOriginalModelAndAvoidResamplingOrSmallAngleLoss()
        {
            using var s = new Surface(new Bitmap(220, 220)); var box = Box(s); var model = box.ModelBounds;
            var before = box.ModelToImage(new PointF(60, 90));
            for (int i = 0; i < 360; i++) Assert.True(s.RotateSelectedObjects(1));
            Near(before, box.ModelToImage(new PointF(60, 90)), .0001); Assert.Equal(model, box.ModelBounds);
            s.RotateSelectedObjects(.000001); Assert.InRange(box.ObjectAngle, .0000009, .0000011);
        }

        [Fact]
        public void MovementAfterRotation_UsesImageDeltaAndUndoRestoresAffineState()
        {
            using var s = new Surface(new Bitmap(220, 220)); var box = Box(s); s.RotateSelectedObjects(37);
            var model = box.ModelBounds; var corner = box.ModelToImage(new PointF(60, 90));
            box.MakeBoundsChangeUndoable(false); box.MoveBy(-15, 23);
            Near(new PointF(corner.X - 15, corner.Y + 23), box.ModelToImage(new PointF(60, 90)));
            Assert.Equal(model, box.ModelBounds); s.Undo(); Near(corner, box.ModelToImage(new PointF(60, 90)));
            s.Redo(); Near(new PointF(corner.X - 15, corner.Y + 23), box.ModelToImage(new PointF(60, 90)));
            box.SnapToEdge(Direction.LEFT, new Size(220, 220)); Assert.Equal(0, box.Bounds.Left); Assert.Equal(model, box.ModelBounds);
            s.Undo(); Near(new PointF(corner.X - 15, corner.Y + 23), box.ModelToImage(new PointF(60, 90)));
        }

        [Fact]
        public void RotatedResizeHandle_MapsImagePointerBackToLocalAxesAndUndo()
        {
            Sta(() =>
            {
                using var s = new Surface(new Bitmap(300, 300)); var box = Box(s); s.RotateSelectedObjects(30);
                var anchor = box.ModelToImage(new PointF(box.Left, box.Top));
                var handle = new ResizeAdorner(box, Positions.BottomRight);
                var start = Point.Round(box.ModelToImage(new PointF(140, 110)));
                var end = Point.Round(box.ModelToImage(new PointF(160, 130)));
                Assert.True(handle.HitTest(start));
                handle.MouseDown(s, Mouse(start)); handle.MouseMove(s, Mouse(end)); handle.MouseUp(s, Mouse(end));
                Assert.InRange(box.Width, 99, 101); Assert.InRange(box.Height, 39, 41); Near(anchor, box.ModelToImage(new PointF(box.Left, box.Top)));
                s.Undo(); Assert.Equal(80, box.Width); Assert.Equal(20, box.Height); Assert.Equal(30, box.ObjectAngle, 6);
            });
        }

        [Fact]
        public void RotatedLineAndPolygonMeasurements_UseActualImageCoordinatesAndTargets()
        {
            using var s = new Surface(new Bitmap(300, 300));
            var line = new LineContainer(s) { Left = 30, Top = 50, Width = 100, Height = 0 }; s.AddElement(line, false); s.SelectElement(line); s.RotateSelectedObjects(90);
            var properties = MeasurementGeometry.GetProperties(line);
            Assert.Equal(80, properties.Single(p => p.Key == "X1").Pixels, 5); Assert.Equal(0, properties.Single(p => p.Key == "Y1").Pixels, 5);
            Assert.Contains(ObjectSnapHelper.Targets(line), p => Math.Abs(p.X - 80) < .001 && Math.Abs(p.Y - 100) < .001);
            Assert.True(MeasurementGeometry.TryEdit(line, MeasurementState.Pixels, "X2", 90, out _));
            Assert.Equal(90, MeasurementGeometry.GetProperties(line).Single(p => p.Key == "X2").Pixels, 5);
            s.Undo(); Assert.Equal(80, MeasurementGeometry.GetProperties(line).Single(p => p.Key == "X2").Pixels, 5);
            s.DeselectAllElements(); var polygon = new PolygonContainer(s); polygon.SetVertices(new[] { new Point(20, 20), new Point(100, 20), new Point(20, 100) }); s.AddElement(polygon, false); s.SelectElement(polygon); s.RotateSelectedObjects(33);
            var before = polygon.ImageVertices; polygon.SelectedVertex = 1;
            Assert.Equal(before[1].X, MeasurementGeometry.GetProperties(polygon).Single(p => p.Key == "VX").Pixels);
            Assert.True(MeasurementGeometry.TryEdit(polygon, MeasurementState.Pixels, "VX", before[1].X + 20, out _));
            Assert.InRange(Math.Abs(polygon.ImageVertices[1].X - before[1].X - 20), 0, 1);
            Near(before[0], polygon.ImageVertices[0]); s.Undo(); Near(before[1], polygon.ImageVertices[1]);
        }

        [Fact]
        public void MixedGroupRotation_UsesSharedStablePivotAndOneUndo()
        {
            using var s = new Surface(new Bitmap(400, 400)); var box = Box(s, 30, 40);
            var text = new TextContainer(s) { Left = 160, Top = 40, Width = 90, Height = 35, Text = "Rotated text" }; s.AddElement(text, false);
            using var pixels = new Bitmap(20, 10); using (var g = Graphics.FromImage(pixels)) g.Clear(Color.Lime);
            var image = new ImageContainer(s) { Left = 60, Top = 160, Width = 40, Height = 20, Image = pixels }; s.AddElement(image, false);
            s.SelectElements(new DrawableContainerList { text, image }); s.GroupSelectedElements(); var id = box.GroupId; var order = s.Elements.ToArray();
            var members = s.Elements.Cast<DrawableContainer>().ToArray(); var centres = members.Select(m => m.ImageCentre).ToArray();
            double x = centres.Average(p => p.X), y = centres.Average(p => p.Y), angle = 31 * Math.PI / 180;
            Assert.True(s.RotateSelectedObjects(31));
            for (int i = 0; i < members.Length; i++)
            {
                double dx = centres[i].X - x, dy = centres[i].Y - y;
                Assert.Equal(x + dx * Math.Cos(angle) - dy * Math.Sin(angle), members[i].ImageCentre.X, 8);
                Assert.Equal(y + dx * Math.Sin(angle) + dy * Math.Cos(angle), members[i].ImageCentre.Y, 8);
                Assert.Equal(id, members[i].GroupId);
            }
            using var rendered = (Bitmap)s.GetImageForExport();
            Assert.Contains(Enumerable.Range(0, rendered.Width * rendered.Height).Select(i => rendered.GetPixel(i % rendered.Width, i / rendered.Width)), c => c.G > 200 && c.R < 80);
            Assert.Equal(order, s.Elements); Assert.Equal(20, image.Image.Width); Assert.Equal(10, image.Image.Height);
            s.Undo(); Assert.All(members, m => Assert.False(m.HasObjectTransform)); s.Redo(); Assert.All(members, m => Assert.Equal(31, m.ObjectAngle, 6));
        }

        [Fact]
        public void RotationClipboardAndProject_RetainTransformAndFreshGroupIdentity()
        {
            using var s = new Surface(new Bitmap(300, 300)); var a = Box(s); var b = Box(s, 20, 30); s.GroupSelectedElements(); s.RotateSelectedObjects(23.5);
            var point = a.ModelToImage(new PointF(a.Left, a.Top));
            var copied = DrawableContainerClipboard.Deserialize(DrawableContainerClipboard.Serialize(s.SelectedElements));
            var copy = Assert.IsType<RectangleContainer>(copied[0]); Near(point, copy.ModelToImage(new PointF(copy.Left, copy.Top))); Assert.NotEqual(a.GroupId, copy.GroupId);
            using var bytes = new MemoryStream(); s.SaveElementsToStream(bytes); bytes.Position = 0;
            using var loaded = new Surface(new Bitmap(300, 300)); loaded.LoadElementsFromStream(bytes);
            var restored = Assert.IsType<RectangleContainer>(loaded.Elements[0]); Near(point, restored.ModelToImage(new PointF(restored.Left, restored.Top))); Assert.Equal(a.GroupId, restored.GroupId);
            Assert.Equal(23.5, restored.ObjectAngle, 6);
        }

        [Fact]
        public void WholeCanvasRotationAndNonuniformResize_ComposeAndUndoWithoutChangingImageContent()
        {
            using var s = new Surface(new Bitmap(300, 300)); using var pixels = new Bitmap(20, 10);
            var image = new ImageContainer(s) { Left = 40, Top = 60, Width = 80, Height = 40, Image = pixels }; s.AddElement(image, false); s.SelectElement(image); s.RotateSelectedObjects(27);
            var before = image.ModelToImage(new PointF(40, 60)); var bitmap = image.Image;
            var apply = typeof(Surface).GetMethod("ApplyEffectResult", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(Image), typeof(Matrix) }, null);
            using var rotation = new Matrix(0, 1, -1, 0, 300, 0);
            apply.Invoke(s, new object[] { new Bitmap(300, 300), rotation }); Near(new PointF(300 - before.Y, before.X), image.ModelToImage(new PointF(40, 60)));
            Assert.Same(bitmap, image.Image); s.Undo(); Near(before, image.ModelToImage(new PointF(40, 60))); Assert.Equal(27, image.ObjectAngle, 5);
            using var resize = new Matrix(2, 0, 0, 1, 0, 0);
            apply.Invoke(s, new object[] { new Bitmap(600, 300), resize }); Near(new PointF(before.X * 2, before.Y), image.ModelToImage(new PointF(40, 60)));
            s.Undo(); Near(before, image.ModelToImage(new PointF(40, 60)));
        }

        [Fact]
        public void SvgRotation_KeepsVectorSourceAndRendersRotatedContentAfterClipboard()
        {
            using var s = new Surface(new Bitmap(220, 220));
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg' width='20' height='10'><rect width='20' height='10' fill='#00ff00'/></svg>"));
            var svg = new SvgContainer(stream, s) { Left = 60, Top = 90, Width = 80, Height = 20 }; s.AddElement(svg, false); s.SelectElement(svg);
            var source = svg.SvgContent; s.RotateSelectedObjects(45);
            using var image = (Bitmap)s.GetImageForExport(); Assert.True(image.GetPixel(100, 100).G > 200);
            Assert.Equal(source, svg.SvgContent); Assert.False(svg.ClickableAt(130, 75));
            var copy = Assert.IsType<SvgContainer>(Assert.Single(DrawableContainerClipboard.Deserialize(DrawableContainerClipboard.Serialize(s.SelectedElements))));
            Assert.Equal(source, copy.SvgContent); Assert.Equal(45, copy.ObjectAngle, 6);
        }

        [Fact]
        public void UnsupportedEffects_DisableWholeSelectionWithoutPartialRotation()
        {
            using var s = new Surface(new Bitmap(300, 300)); var a = Box(s); var b = Box(s, 20, 30); b.Add(new GrayscaleFilter(b) { Invert = true });
            Assert.False(s.CanRotateObjects); Assert.False(s.RotateSelectedObjects(45)); Assert.False(a.HasObjectTransform); Assert.False(b.HasObjectTransform);
            s.GroupSelectedElements(); Assert.False(s.CanRotateObjects); Assert.False(s.RotateSelectedObjects(45));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RotatedTextModalCommits_EachUndoSeparatelyIncludingAfterDeserialization(bool reload)
        {
            using var original = new Surface(new Bitmap(300, 300));
            var text = new TextContainer(original) { Left = 50, Top = 50, Width = 120, Height = 40 };
            text.ChangeText("Original", false); original.AddElement(text, false); original.SelectElement(text); original.RotateSelectedObjects(31);
            using var restored = new Surface(new Bitmap(300, 300));
            var surface = original;
            if (reload)
            {
                using var stream = new MemoryStream(); original.SaveElementsToStream(stream); stream.Position = 0;
                restored.LoadElementsFromStream(stream); surface = restored; text = Assert.IsType<TextContainer>(Assert.Single(restored.Elements));
            }
            var transform = text.ObjectTransformState;
            surface.Modified = false;
            Assert.False(text.CommitRotatedText("Original")); Assert.False(surface.Modified);
            Assert.True(text.CommitRotatedText("First confirmation")); Assert.True(surface.Modified);
            Assert.True(text.CommitRotatedText("Second confirmation"));
            Assert.False(text.CommitRotatedText("Second confirmation"));
            surface.Undo(); Assert.Equal("First confirmation", text.Text);
            surface.Undo(); Assert.Equal("Original", text.Text);
            surface.Redo(); Assert.Equal("First confirmation", text.Text);
            surface.Redo(); Assert.Equal("Second confirmation", text.Text);
            Assert.Equal(transform, text.ObjectTransformState); Assert.Equal(31, text.ObjectAngle, 6);
        }

        private static MouseEventArgs Mouse(Point p) => new MouseEventArgs(MouseButtons.Left, 1, p.X, p.Y, 0);
        private static void Sta(Action action)
        {
            Exception error = null; var thread = new Thread(() => { try { action(); } catch (Exception e) { error = e; } });
            thread.SetApartmentState(ApartmentState.STA); thread.Start(); Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "UI test timed out");
            if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
        }
    }
}
