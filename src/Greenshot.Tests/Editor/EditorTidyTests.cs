using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Forms;
using Xunit;

namespace Greenshot.Tests.Editor
{
    [Collection(TestCollections.Clipboard)]
    public class EditorTidyTests
    {
        public EditorTidyTests() => TestEnvironment.EnsureInitialized();
        private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(owner);
        private static int Undos(Surface s) => Field<ICollection>(s, "_undoStack").Count;
        private static RectangleContainer Box(Surface s, int x, int y = 30, int size = 20)
        {
            var box = new RectangleContainer(s) { Left = x, Top = y, Width = size, Height = size };
            box.SetFieldValue(FieldType.SHADOW, false); box.SetFieldValue(FieldType.FILL_COLOR, Color.Blue);
            s.AddElement(box, false); return box;
        }
        private static void Select(Surface s, params DrawableContainer[] objects)
        { s.DeselectAllElements(); s.SelectElements(new DrawableContainerList(objects)); }

        [Theory]
        [InlineData(DrawingModes.Line)]
        [InlineData(DrawingModes.Arrow)]
        [InlineData(DrawingModes.Rect)]
        public void HoverAfterRepeatedDrawing_UsesModeAndZoomWithoutMovingHeadlessPointer(DrawingModes mode)
        {
            Sta(() =>
            {
                using var s = new Surface(new Bitmap(300, 300)); Box(s, 100, 100, 40);
                s.SetObjectSnapping(true, false, 8); s.ZoomFactor = new Fraction(2, 1); s.DrawingMode = mode;
                var cursor = Cursor.Position;
                for (int i = 0; i < 2; i++)
                {
                    int x = 20 + i * 100;
                    Mouse(s, "SurfaceMouseDown", x, 20); Mouse(s, "SurfaceMouseMove", x + 40, 40); Mouse(s, "SurfaceMouseUp", x + 40, 40);
                    Mouse(s, "SurfaceMouseMove", 206, 200);
                    Assert.Equal(new PointF(100, 100), Field<PointF?>(s, "_snapTarget"));
                    Assert.Equal(new Point(100, 100), s.PreviewDrawingStart(new Point(103, 100)));
                }
                Assert.Equal(cursor, Cursor.Position);
                s.DrawingMode = DrawingModes.None; Assert.Null(Field<PointF?>(s, "_snapTarget"));
            });
        }

        [Fact]
        public void PolygonHover_UsesSharedPointerPreviewAtZoomAndClearsOnToolChange()
        {
            Sta(() =>
            {
                using var s = new Surface(new Bitmap(300, 300)); Box(s, 100, 100, 40);
                s.SetObjectSnapping(true, true); s.ZoomFactor = new Fraction(2, 1); s.DrawingMode = DrawingModes.Polygon;
                var cursor = Cursor.Position; Mouse(s, "SurfaceMouseMove", 206, 200);
                Assert.Equal(new Point(100, 100), Field<Point>(s, "_polygonPointer"));
                Assert.Equal(new PointF(100, 100), Field<PointF?>(s, "_snapTarget")); Assert.Equal(cursor, Cursor.Position);
                s.DrawingMode = DrawingModes.None; Assert.Null(Field<PointF?>(s, "_snapTarget"));
            });
        }

        [Fact]
        public void ToolbarActions_UseNativeSizeImagesAccessibilityAndEnablement()
        {
            Sta(() =>
            {
                using var s = new Surface(new Bitmap(300, 300)); using var editor = new ImageEditorForm(s, true, false);
                var standard = Field<ToolStripButton>(editor, "btnRect");
                foreach (var name in new[] { "_polygonButton", "_groupButton", "_ungroupButton", "_rotateObjectButton" })
                {
                    var button = Field<ToolStripButton>(editor, name);
                    Assert.Equal(ToolStripItemDisplayStyle.Image, button.DisplayStyle); Assert.Equal(new Size(16, 16), button.Image.Size);
                    Assert.False(string.IsNullOrWhiteSpace(button.AccessibleName)); Assert.False(string.IsNullOrWhiteSpace(button.ToolTipText));
                    Assert.Equal(standard.ImageScaling, button.ImageScaling);
                }
                Assert.False(Field<ToolStripButton>(editor, "_groupButton").Enabled);
                Assert.False(Field<ToolStripButton>(editor, "_rotateObjectButton").Enabled);
                s.DrawingMode = DrawingModes.Polygon; Assert.True(Field<ToolStripButton>(editor, "_polygonButton").Checked);
                Assert.Contains("drag a box", Field<ToolStripButton>(editor, "btnCursor").ToolTipText);
            });
        }

        [Fact]
        public void AbsoluteSingleRotation_RetainsAngleAndRepeatedApplyHasNoUndoOrModification()
        {
            using var s = new Surface(new Bitmap(300, 300)); var box = Box(s, 40); Select(s, box);
            Assert.True(s.SetSelectedRotation(36)); Assert.True(s.TryGetSelectedRotation(out double angle)); Assert.Equal(36, angle, 8);
            int history = Undos(s); var transform = box.ObjectTransformState; s.Modified = false;
            Assert.True(s.SetSelectedRotation(36)); Assert.True(s.SetSelectedRotation(396)); Assert.Equal(history, Undos(s)); Assert.False(s.Modified); Assert.Equal(transform, box.ObjectTransformState);
            s.Undo(); Assert.False(box.HasObjectTransform); s.Redo(); Assert.True(s.TryGetSelectedRotation(out angle)); Assert.Equal(36, angle, 8);
            s.SetSelectedRotation(-30); s.TryGetSelectedRotation(out angle); Assert.Equal(330, angle, 8);
        }

        [Fact]
        public void AbsoluteGroupRotation_PreservesRelativeAnglesAndMembershipUndoReference()
        {
            using var s = new Surface(new Bitmap(300, 300)); var a = Box(s, 30); var b = Box(s, 100);
            Select(s, a); s.SetSelectedRotation(20); Select(s, b); s.SetSelectedRotation(350);
            Select(s, a, b); Assert.False(s.CanSetSelectedRotation); Assert.False(s.SetSelectedRotation(36));
            s.GroupSelectedElements(); s.TryGetSelectedRotation(out double initial); Assert.Equal(0, initial);
            Assert.True(s.SetSelectedRotation(36)); Assert.Equal(56, a.ObjectAngle, 8); Assert.Equal(26, b.ObjectAngle, 8);
            int history = Undos(s); s.SetSelectedRotation(36); Assert.Equal(history, Undos(s));
            s.Undo(); s.TryGetSelectedRotation(out initial); Assert.Equal(0, initial); Assert.Equal(20, a.ObjectAngle, 8);
            s.Redo(); s.TryGetSelectedRotation(out initial); Assert.Equal(36, initial);
            s.UngroupSelectedElements(); Select(s, a); s.TryGetSelectedRotation(out initial); Assert.Equal(56, initial, 8);
            s.Undo(); s.TryGetSelectedRotation(out initial); Assert.Equal(36, initial);
            var extra = Box(s, 180); s.SelectElement(extra); s.GroupSelectedElements(); s.TryGetSelectedRotation(out initial); Assert.Equal(0, initial);
            s.Undo(); Select(s, a); s.TryGetSelectedRotation(out initial); Assert.Equal(36, initial);
        }

        [Fact]
        public void GroupAbsoluteReference_RoundTripsClipboardProjectAndCanvasOrientation()
        {
            using var s = new Surface(new Bitmap(300, 300)); var a = Box(s, 30); var b = Box(s, 100); Select(s, a, b); s.GroupSelectedElements(); s.SetSelectedRotation(36);
            var copies = DrawableContainerClipboard.Deserialize(DrawableContainerClipboard.Serialize(s.SelectedElements));
            using var pasted = new Surface(new Bitmap(300, 300)); pasted.AddElements(copies); pasted.TryGetSelectedRotation(out double angle); Assert.Equal(36, angle);
            int history = Undos(pasted); pasted.SetSelectedRotation(36); Assert.Equal(history, Undos(pasted)); Assert.NotEqual(a.GroupId, ((DrawableContainer)copies[0]).GroupId);
            using var data = new MemoryStream(); s.SaveElementsToStream(data); data.Position = 0;
            using var loaded = new Surface(new Bitmap(300, 300)); loaded.LoadElementsFromStream(data); loaded.TryGetSelectedRotation(out angle); Assert.Equal(36, angle);
            var apply = typeof(Surface).GetMethod("ApplyEffectResult", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(Image), typeof(Matrix), typeof(Greenshot.Base.Effects.IEffect) }, null);
            using var turn = new Matrix(0, 1, -1, 0, 300, 0); apply.Invoke(s, new object[] { new Bitmap(300, 300), turn , null }); s.TryGetSelectedRotation(out angle); Assert.Equal(126, angle, 5);
            s.Undo(); s.TryGetSelectedRotation(out angle); Assert.Equal(36, angle, 5);
            using var stretch = new Matrix(2, 0, 0, 1, 0, 0); apply.Invoke(s, new object[] { new Bitmap(600, 300), stretch , null }); s.TryGetSelectedRotation(out angle);
            Assert.Equal(Math.Atan2(Math.Sin(Math.PI / 5), 2 * Math.Cos(Math.PI / 5)) * 180 / Math.PI, angle, 5);
            s.Undo(); s.TryGetSelectedRotation(out angle); Assert.Equal(36, angle, 5);
            using var flip = new Matrix(-1, 0, 0, 1, 300, 0); apply.Invoke(s, new object[] { new Bitmap(300, 300), flip , null }); s.TryGetSelectedRotation(out angle); Assert.Equal(144, angle, 5);
            s.Undo(); s.TryGetSelectedRotation(out angle); Assert.Equal(36, angle, 5);
        }

        [Fact]
        public void ReverseZoomedBoxSelection_ReplacesSelectionAndNeverExportsOrAddsUndo()
        {
            Sta(() =>
            {
                using var s = new Surface(new Bitmap(300, 300)); var a = Box(s, 30); var b = Box(s, 100); Select(s, b); s.ZoomFactor = new Fraction(2, 1);
                int history = Undos(s); s.Modified = false;
                Mouse(s, "SurfaceMouseDown", 140, 140); Mouse(s, "SurfaceMouseMove", 20, 20);
                Assert.Same(a, Assert.Single(s.SelectedElements));
                using (var export = (Bitmap)s.GetImageForExport()) Assert.Equal(0, export.GetPixel(10, 10).A);
                Mouse(s, "SurfaceMouseUp", 20, 20);
                Assert.False(s.Capture); Assert.Equal(history, Undos(s)); Assert.False(s.Modified);
            });
        }

        [Fact]
        public void ShiftBoxSelection_TogglesEntireEnclosedGroupsAgainstInitialSelection()
        {
            Sta(() =>
            {
                using var s = new Surface(new Bitmap(300, 300)); var a = Box(s, 30); var b = Box(s, 100); var c = Box(s, 180);
                Select(s, a, b); s.GroupSelectedElements(); s.DeselectAllElements();
                s.BeginSelectionBox(Point.Empty, Keys.None); Mouse(s, "SurfaceMouseMove", 80, 80); Mouse(s, "SurfaceMouseUp", 80, 80); Assert.Empty(s.SelectedElements);
                s.BeginSelectionBox(Point.Empty, Keys.None); Mouse(s, "SurfaceMouseMove", 140, 80); Mouse(s, "SurfaceMouseUp", 140, 80); Assert.Equal(2, s.SelectedElements.Count);
                s.SelectElement(c); s.BeginSelectionBox(Point.Empty, Keys.Shift);
                Mouse(s, "SurfaceMouseMove", 140, 80); Mouse(s, "SurfaceMouseMove", 145, 85); Assert.Same(c, Assert.Single(s.SelectedElements));
                Mouse(s, "SurfaceMouseUp", 145, 85); Assert.Same(c, Assert.Single(s.SelectedElements));
            });
        }

        [Theory]
        [InlineData("Escape")]
        [InlineData("Tool")]
        [InlineData("Document")]
        [InlineData("Capture")]
        public void BoxCancellation_ReleasesCaptureAndRestoresSelectionWhereAppropriate(string method)
        {
            Sta(() =>
            {
                using var s = new Surface(new Bitmap(300, 300)); var a = Box(s, 30); var b = Box(s, 100); Select(s, b); int history = Undos(s);
                s.BeginSelectionBox(Point.Empty, Keys.None); Mouse(s, "SurfaceMouseMove", 80, 80); Assert.Same(a, Assert.Single(s.SelectedElements));
                if (method == "Escape") Assert.True(s.ProcessCanvasKey(Keys.Escape));
                if (method == "Document") Assert.True(s.CancelSelectionBox());
                if (method == "Capture") s.Capture = false;
                if (method == "Tool") s.DrawingMode = DrawingModes.Line;
                if (method != "Tool") Assert.Same(b, Assert.Single(s.SelectedElements));
                Assert.False(s.Capture); Assert.Null(Field<Point?>(s, "_selectionBoxStart")); Assert.Equal(history, Undos(s));
            });
        }

        [Fact]
        public void SelectionThresholdAndObjectDrag_PreserveExistingPriorities()
        {
            Sta(() =>
            {
                using var s = new Surface(new Bitmap(300, 300)); var box = Box(s, 30); Select(s, box); s.ZoomFactor = new Fraction(2, 1);
                s.BeginSelectionBox(Point.Empty, Keys.Shift); Mouse(s, "SurfaceMouseMove", 2, 2); Assert.False(Field<bool>(s, "_selectionBoxActive"));
                Mouse(s, "SurfaceMouseUp", 2, 2); Assert.Same(box, Assert.Single(s.SelectedElements));
                Mouse(s, "SurfaceMouseDown", 80, 80); Mouse(s, "SurfaceMouseMove", 100, 100); Mouse(s, "SurfaceMouseUp", 100, 100);
                Assert.Equal(40, box.Left); Assert.Equal(40, box.Top); Assert.Null(Field<Point?>(s, "_selectionBoxStart"));
            });
        }

        private static void Mouse(Surface s, string method, int x, int y) => typeof(Surface).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(s, new object[] { s, new MouseEventArgs(MouseButtons.Left, 1, x, y, 0) });
        private static void Sta(Action action)
        {
            Exception error = null; var thread = new Thread(() => { try { action(); } catch (Exception e) { error = e; } });
            thread.SetApartmentState(ApartmentState.STA); thread.Start(); Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "UI test timed out");
            if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
        }
    }
}
