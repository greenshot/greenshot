using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Drawing.Filters;
using Xunit;

namespace Greenshot.Tests.Editor
{
    [Collection(TestCollections.Clipboard)]
    public class GroupTests
    {
        public GroupTests() => TestEnvironment.EnsureInitialized();
        private static RectangleContainer Box(Surface surface, int x)
        {
            var box = new RectangleContainer(surface) { Left = x, Top = 20, Width = 30, Height = 30 };
            box.SetFieldValue(FieldType.SHADOW, false);
            surface.AddElement(box, false); return box;
        }
        private static void Select(Surface surface, params DrawableContainer[] members)
        { surface.DeselectAllElements(); surface.SelectElements(new DrawableContainerList(members)); }

        [Fact]
        public void GroupUngroupUndo_PreservesGeometryStylesAndInterleavedOrder()
        {
            using var surface = new Surface(new Bitmap(300, 300));
            var first = Box(surface, 20); var between = Box(surface, 60); var last = Box(surface, 100);
            last.SetFieldValue(FieldType.FILL_COLOR, Color.Aquamarine);
            var order = surface.Elements.ToArray(); var bounds = last.Bounds;
            Select(surface, last, first);
            Assert.True(surface.GroupSelectedElements());
            var id = first.GroupId;
            Assert.NotEqual(Guid.Empty, id); Assert.Equal(id, last.GroupId); Assert.Equal(Guid.Empty, between.GroupId);
            Assert.Equal(order, surface.Elements);
            surface.Undo(); Assert.Equal(Guid.Empty, first.GroupId); Assert.Equal(Guid.Empty, last.GroupId);
            surface.Redo(); Assert.Equal(id, first.GroupId); Assert.Equal(id, last.GroupId);
            Assert.True(surface.UngroupSelectedElements());
            Assert.Equal(Guid.Empty, first.GroupId); Assert.Equal(bounds, last.Bounds);
            Assert.Equal(Color.Aquamarine, last.GetFieldValueAsColor(FieldType.FILL_COLOR));
            Assert.Equal(order, surface.Elements);
            surface.Undo(); Assert.Equal(id, last.GroupId);
            surface.DeselectAllElements(); surface.SelectElement(first);
            Assert.Equal(new[] { first, last }, surface.SelectedElements);
            surface.DeselectElement(last); Assert.Empty(surface.SelectedElements);
        }

        [Fact]
        public void GroupFlattening_UndoRestoresPreviousMemberships()
        {
            using var surface = new Surface(); var a = Box(surface, 10); var b = Box(surface, 50); var c = Box(surface, 90); var d = Box(surface, 130);
            Select(surface, a, b); surface.GroupSelectedElements(); var firstId = a.GroupId;
            Select(surface, c, d); surface.GroupSelectedElements(); var secondId = c.GroupId;
            Select(surface, a, c); Assert.Equal(4, surface.SelectedElements.Count);
            surface.GroupSelectedElements(); Assert.Single(surface.Elements.OfType<DrawableContainer>().Select(e => e.GroupId).Distinct());
            surface.Undo(); Assert.Equal(firstId, a.GroupId); Assert.Equal(firstId, b.GroupId); Assert.Equal(secondId, c.GroupId); Assert.Equal(secondId, d.GroupId);
        }

        [Fact]
        public void DeleteAndMoveGroup_AreAtomicAndRestoreOriginalPaintOrder()
        {
            using var surface = new Surface(new Bitmap(300, 300)); var a = Box(surface, 10); var other = Box(surface, 50); var b = Box(surface, 90);
            var order = surface.Elements.ToArray(); Select(surface, a, b); surface.GroupSelectedElements();
            surface.DeselectAllElements(); surface.SelectElement(b);
            surface.ProcessCmdKey(Keys.Right); Assert.Equal(11, a.Left); Assert.Equal(91, b.Left); Assert.Equal(50, other.Left);
            surface.Undo(); Assert.Equal(10, a.Left); Assert.Equal(90, b.Left);
            surface.RemoveSelectedElements(); Assert.Same(other, Assert.Single(surface.Elements));
            surface.Undo(); Assert.Equal(order, surface.Elements); Assert.Equal(2, surface.SelectedElements.Count);
            surface.Redo(); Assert.Same(other, Assert.Single(surface.Elements));
        }

        [Fact]
        public void ClipboardAndDuplicate_CreateIndependentGroupsAndPreserveRelativePlacement()
        {
            using var surface = new Surface(new Bitmap(300, 300)); var a = Box(surface, 10); var b = Box(surface, 90);
            Select(surface, a, b); surface.GroupSelectedElements(); var original = a.GroupId;
            // A context selection containing one member must still serialize both.
            var bytes = DrawableContainerClipboard.Serialize(new DrawableContainerList(surface.ID) { b });
            var first = DrawableContainerClipboard.Deserialize(bytes); var second = DrawableContainerClipboard.Deserialize(bytes);
            Assert.Equal(2, first.Count); Assert.Equal(2, second.Count);
            Assert.Equal(10, first[0].Left); Assert.Equal(90, first[1].Left);
            var firstId = ((DrawableContainer)first[0]).GroupId; var secondId = ((DrawableContainer)second[0]).GroupId;
            Assert.NotEqual(original, firstId); Assert.NotEqual(firstId, secondId);
            Assert.Equal(firstId, ((DrawableContainer)first[1]).GroupId); Assert.NotSame(a, first[0]);
            using var target = new Surface(new Bitmap(300, 300)); target.AddElements(first); target.DeselectAllElements(); target.SelectElement(first[1]);
            Assert.Equal(2, target.SelectedElements.Count);
            target.ProcessCmdKey(Keys.Right); Assert.Equal(10, a.Left); Assert.Equal(11, first[0].Left); Assert.Equal(10, second[0].Left);
            target.Undo(); Assert.Equal(10, first[0].Left);
            target.Undo(); Assert.Empty(target.Elements);
            surface.DuplicateSelectedElements();
            Assert.Equal(4, surface.Elements.Count); Assert.NotEqual(original, ((DrawableContainer)surface.SelectedElements[0]).GroupId);
            Assert.Equal(2, surface.SelectedElements.Count);
        }

        [Fact]
        public void ClipboardPasteBetweenSurfaces_RepeatedPasteNeverLinksGroups()
        {
            Sta(() =>
            {
                using var source = new Surface(new Bitmap(300, 300)); var a = Box(source, 20); var b = Box(source, 100);
                Select(source, a, b); source.GroupSelectedElements(); source.CopySelectedElements();
                using var target = new Surface(new Bitmap(300, 300)); target.PasteElementFromClipboard();
                Assert.Equal(2, target.Elements.Count); var firstId = ((DrawableContainer)target.Elements[0]).GroupId;
                Assert.NotEqual(a.GroupId, firstId); Assert.Equal(80, target.Elements[1].Left - target.Elements[0].Left);
                target.PasteElementFromClipboard(); Assert.Equal(4, target.Elements.Count);
                var secondId = ((DrawableContainer)target.Elements[2]).GroupId;
                Assert.NotEqual(firstId, secondId); Assert.Equal(2, target.SelectedElements.Count);
                target.Undo(); Assert.Equal(2, target.Elements.Count);
                target.DeselectAllElements(); target.SelectElement(target.Elements[0]); Assert.Equal(2, target.SelectedElements.Count);
            });
        }

        [Fact]
        public void ProjectRoundTrip_RetainsMembershipAndIndependentSelectionAfterUngroup()
        {
            using var surface = new Surface(new Bitmap(300, 300)); var a = Box(surface, 10); var b = Box(surface, 90);
            Select(surface, a, b); surface.GroupSelectedElements();
            using var stream = new MemoryStream(); surface.SaveElementsToStream(stream); stream.Position = 0;
            using var loaded = new Surface(new Bitmap(300, 300)); loaded.LoadElementsFromStream(stream);
            Assert.Equal(a.GroupId, ((DrawableContainer)loaded.Elements[0]).GroupId);
            loaded.SelectElement(loaded.Elements[0]); Assert.Equal(2, loaded.SelectedElements.Count);
            loaded.UngroupSelectedElements(); loaded.DeselectAllElements(); loaded.SelectElement(loaded.Elements[0]);
            Assert.Single(loaded.SelectedElements);
        }

        [Fact]
        public void GroupedInvertedFilters_KeepIdenticalRasterAndTopLevelRenderingOrder()
        {
            using var image = new Bitmap(80, 60); using (var g = Graphics.FromImage(image)) g.Clear(Color.OrangeRed);
            using var surface = new Surface((Bitmap)image.Clone());
            var a = Box(surface, 5); a.Width = 10; a.Height = 10; a.Add(new GrayscaleFilter(a) { Invert = true });
            var other = Box(surface, 25); other.SetFieldValue(FieldType.FILL_COLOR, Color.Lime);
            var b = Box(surface, 45); b.Width = 10; b.Height = 10; b.Add(new GrayscaleFilter(b) { Invert = true });
            using var before = (Bitmap)surface.GetImageForExport();
            Select(surface, a, b); surface.GroupSelectedElements();
            using var grouped = (Bitmap)surface.GetImageForExport();
            for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) Assert.Equal(before.GetPixel(x, y), grouped.GetPixel(x, y));
            Assert.Equal(new[] { a, other, b }, surface.Elements);
        }

        [Fact]
        public void DragUnselectedGroup_MovesEveryMemberAndPolygonCannotEditThroughGroup()
        {
            Sta(() =>
            {
                using var surface = new Surface(new Bitmap(300, 300)); var a = Box(surface, 20);
                var polygon = new PolygonContainer(surface); polygon.SetVertices(new[] { new Point(100, 100), new Point(150, 100), new Point(100, 150) }); surface.AddElement(polygon, false);
                Select(surface, a, polygon); surface.GroupSelectedElements(); surface.DeselectAllElements();
                surface.SetObjectSnapping(true, true, 100);
                var before = polygon.Vertices;
                Mouse(surface, "SurfaceMouseDown", 20, 30); Mouse(surface, "SurfaceMouseMove", 30, 45); Mouse(surface, "SurfaceMouseUp", 30, 45);
                Assert.Equal(30, a.Left); Assert.Equal(new Point(110, 115), polygon.Vertices[0]); Assert.Equal(2, surface.SelectedElements.Count);
                surface.Undo(); Assert.Equal(20, a.Left); Assert.Equal(before, polygon.Vertices);
                Mouse(surface, "SurfaceMouseDown", 150, 100); Mouse(surface, "SurfaceMouseMove", 160, 110); Mouse(surface, "SurfaceMouseUp", 160, 110);
                Assert.Equal(30, a.Left); Assert.Equal(new Point(110, 110), polygon.Vertices[0]); Assert.Equal(new Point(160, 110), polygon.Vertices[1]);
            });
        }

        [Fact]
        public void GroupShortcutsAndTabTraversal_SelectGroupAsOneUnit()
        {
            using var surface = new Surface(new Bitmap(300, 300)); var a = Box(surface, 10); var b = Box(surface, 50); var c = Box(surface, 90);
            Select(surface, a, b); Assert.True(surface.ProcessCmdKey(Keys.Control | Keys.Alt | Keys.G));
            Assert.False(surface.ProcessCmdKey(Keys.Control | Keys.G)); // Existing Grayscale shortcut belongs to the form.
            surface.DeselectAllElements(); Assert.True(surface.SelectNextElement(true)); Assert.Equal(2, surface.SelectedElements.Count);
            Assert.True(surface.SelectNextElement(true)); Assert.Same(c, Assert.Single(surface.SelectedElements));
            surface.SelectElement(a); Assert.True(surface.ProcessCmdKey(Keys.Control | Keys.Shift | Keys.G)); Assert.Equal(Guid.Empty, a.GroupId);
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
