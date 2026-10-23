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
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Dapplo.Windows.Common.Extensions;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Adorners;
using Greenshot.Editor.Drawing.Fields;
using Xunit;

namespace Greenshot.Tests.Editor
{
    public class PolylineTests
    {
        public PolylineTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static Surface CreateSurface() => new Surface(new Bitmap(400, 300))
        {
            DrawingMode = DrawingModes.Polyline,
            Modified = false
        };

        private static void Mouse(Surface surface, string eventName, int x, int y, MouseButtons button = MouseButtons.Left, int clicks = 1)
        {
            // Raise the real control events, including the surface's image-coordinate conversion.
            typeof(Control).GetMethod(eventName, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(surface, new object[] { new MouseEventArgs(button, clicks, x, y, 0) });
        }

        private static void Click(Surface surface, int x, int y, MouseButtons button = MouseButtons.Left, int clicks = 1)
        {
            Mouse(surface, "OnMouseDown", x, y, button, clicks);
            Mouse(surface, "OnMouseUp", x, y, button, clicks);
        }

        private static PolylineContainer DrawPolyline(Surface surface)
        {
            Click(surface, 20, 20);
            Click(surface, 100, 20);
            Click(surface, 100, 100);
            Assert.True(surface.ProcessCanvasKey(Keys.Enter));
            return Assert.IsType<PolylineContainer>(Assert.Single(surface.Elements));
        }

        private static PolylineContainer DrawClosedPolyline(Surface surface)
        {
            Click(surface, 20, 20);
            Click(surface, 100, 20);
            Click(surface, 100, 100);
            Click(surface, 24, 23);
            return Assert.IsType<PolylineContainer>(Assert.Single(surface.Elements));
        }

        private static Bitmap Render(PolylineContainer polyline, RenderMode renderMode)
        {
            var bitmap = new Bitmap(400, 300);
            using var graphics = Graphics.FromImage(bitmap);
            polyline.Draw(graphics, renderMode);
            return bitmap;
        }

        [Fact]
        public void ClicksAndEnter_CreateOneObject_AndUndoRedoRestoreAllSegments()
        {
            using var surface = CreateSurface();
            var polyline = DrawPolyline(surface);

            Assert.Equal(new[] { new Point(20, 20), new Point(100, 20), new Point(100, 100) }, polyline.Points);
            Assert.Same(polyline, Assert.Single(surface.SelectedElements));
            Assert.Equal(EditStatus.IDLE, polyline.Status);
            surface.Undo();
            Assert.Empty(surface.Elements);
            Assert.False(surface.CanUndo);
            surface.Redo();
            Assert.Same(polyline, Assert.Single(surface.Elements));
            Assert.Equal(3, polyline.Points.Length);
        }

        [Theory]
        [InlineData(MouseButtons.Left, 2)]
        [InlineData(MouseButtons.Right, 1)]
        public void DoubleClickOrRightClick_FinishesWithoutAnExtraVertex(MouseButtons button, int clicks)
        {
            using var surface = CreateSurface();
            Click(surface, 20, 20);
            Click(surface, 100, 20);
            Click(surface, 100, 100);
            Click(surface, 101, 101, button, clicks);
            Mouse(surface, "OnMouseDoubleClick", 101, 101, button, clicks);

            var polyline = Assert.IsType<PolylineContainer>(Assert.Single(surface.SelectedElements));
            Assert.Equal(new Point(100, 100), polyline.Points[2]);
            Assert.Equal(3, polyline.Points.Length);

            // A fresh click starts a different polyline.
            Click(surface, 200, 200);
            Click(surface, 250, 220);
            surface.ProcessCanvasKey(Keys.Enter);
            Assert.Equal(2, surface.Elements.Count);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EscapeOrSingleVertexFinish_DiscardsDraftWithoutUndo(bool cancel)
        {
            using var surface = CreateSurface();
            Click(surface, 20, 20);
            if (cancel)
            {
                Click(surface, 100, 20);
            }

            Assert.True(surface.ProcessCanvasKey(cancel ? Keys.Escape : Keys.Enter));
            Assert.Empty(surface.Elements);
            Assert.Empty(surface.SelectedElements);
            Assert.False(surface.CanUndo);
            DrawPolyline(surface);
        }

        [Fact]
        public void CancelDraft_PreservesEarlierUndoHistory()
        {
            using var surface = CreateSurface();
            var first = DrawPolyline(surface);
            Click(surface, 200, 200);
            Click(surface, 250, 220);
            surface.ProcessCmdKey(Keys.Escape);

            Assert.Same(first, Assert.Single(surface.Elements));
            Assert.True(surface.Modified);
            surface.Undo();
            Assert.Empty(surface.Elements);
            Assert.False(surface.CanUndo);
        }

        [Fact]
        public void StylingThenCancellingDraft_LeavesNoUndoEntries()
        {
            using var surface = CreateSurface();
            Click(surface, 20, 20);
            Click(surface, 100, 20);
            surface.FieldAggregator.GetField(FieldType.LINE_THICKNESS).Value = 7;
            surface.ProcessCanvasKey(Keys.Escape);

            Assert.Empty(surface.Elements);
            Assert.False(surface.CanUndo);
        }

        [Fact]
        public void FirstVertex_EnablesUndo_AndUndoDiscardsTheDraft()
        {
            using var surface = CreateSurface();
            bool undoEnabled = false;
            surface.MovingElementChanged += (_, _) => undoEnabled = surface.CanUndo;
            Click(surface, 20, 20);

            Assert.True(surface.CanUndo);
            Assert.True(undoEnabled);
            surface.Undo();
            Assert.Empty(surface.Elements);
            Assert.False(surface.CanUndo);
        }

        [Fact]
        public void CancellingDraft_PreservesExistingRedoHistory()
        {
            using var surface = CreateSurface();
            var original = DrawPolyline(surface);
            surface.Undo();
            Click(surface, 200, 200);
            Click(surface, 250, 220);
            surface.Undo();

            Assert.Empty(surface.Elements);
            Assert.False(surface.CanUndo);
            Assert.True(surface.CanRedo);
            surface.Redo();
            Assert.Same(original, Assert.Single(surface.Elements));
        }

        [Theory]
        [InlineData(Keys.Enter, true)]
        [InlineData(Keys.Escape, true)]
        [InlineData(Keys.Enter, false)]
        [InlineData(Keys.Escape, false)]
        public void FinishOrCancel_WhileShiftIsStillHeld(Keys key, bool canvasKey)
        {
            using var surface = CreateSurface();
            Click(surface, 20, 20);
            Click(surface, 100, 20);

            Assert.True(canvasKey ? surface.ProcessCanvasKey(key | Keys.Shift) : surface.ProcessCmdKey(key | Keys.Shift));
            if (key == Keys.Enter)
            {
                Assert.Equal(EditStatus.IDLE, Assert.Single(surface.Elements).Status);
                surface.Undo();
            }

            Assert.Empty(surface.Elements);
            Assert.False(surface.CanUndo);
        }

        [Fact]
        public void ClearBackgroundWhileDrawing_CommitsBeforeTheBackgroundUndoStep()
        {
            using var surface = CreateSurface();
            Image originalImage = surface.Image;
            Click(surface, 20, 20);
            Click(surface, 100, 20);
            surface.Clear(Color.White);

            Assert.Equal(EditStatus.IDLE, Assert.Single(surface.Elements).Status);
            surface.Undo();
            Assert.Same(originalImage, surface.Image);
            Assert.Single(surface.Elements);
            surface.Undo();
            Assert.Empty(surface.Elements);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AddOtherElementsWhileDrawing_CommitsBeforeTheInsertionUndoStep(bool batch)
        {
            using var surface = CreateSurface();
            Click(surface, 20, 20);
            Click(surface, 100, 20);
            var rectangle = new RectangleContainer(surface) { Left = 200, Top = 100, Width = 50, Height = 50 };
            if (batch)
            {
                surface.AddElements(new DrawableContainerList { rectangle });
            }
            else
            {
                surface.AddElement(rectangle);
            }

            Assert.Equal(EditStatus.IDLE, surface.Elements[0].Status);
            surface.Undo();
            Assert.IsType<PolylineContainer>(Assert.Single(surface.Elements));
            surface.Undo();
            Assert.Empty(surface.Elements);
        }

        [Fact]
        public void LoadElementsWhileDrawing_CommitsDraftAndSelectsOnlyImportedElements()
        {
            using var source = CreateSurface();
            source.AddElement(new RectangleContainer(source) { Left = 200, Top = 100, Width = 50, Height = 50 });
            using var stream = new MemoryStream();
            source.SaveElementsToStream(stream);
            stream.Position = 0;
            using var surface = CreateSurface();
            Click(surface, 20, 20);
            Click(surface, 100, 20);
            surface.LoadElementsFromStream(stream);

            Assert.IsType<RectangleContainer>(Assert.Single(surface.SelectedElements));
            Assert.Equal(EditStatus.IDLE, surface.Elements[0].Status);
            surface.Undo();
            Assert.IsType<PolylineContainer>(Assert.Single(surface.Elements));
            surface.Undo();
            Assert.Empty(surface.Elements);
        }

        [Fact]
        public void ExportWhileDrawing_HasThePlacedSegmentsWithoutThePreview_AndDrawingContinues()
        {
            using var surface = CreateSurface();
            surface.FieldAggregator.GetField(FieldType.SHADOW).Value = false;
            Click(surface, 20, 20);
            Click(surface, 100, 20);
            Mouse(surface, "OnMouseMove", 100, 100, MouseButtons.None);
            using (var exported = (Bitmap)surface.GetImageForExport())
            {
                Assert.True(exported.GetPixel(60, 20).A > 0);
                Assert.Equal(0, exported.GetPixel(100, 60).A);
            }

            Click(surface, 100, 100);
            surface.ProcessCanvasKey(Keys.Enter);
            var polyline = Assert.IsType<PolylineContainer>(Assert.Single(surface.Elements));
            Assert.Equal(3, polyline.Points.Length);
            surface.Undo();
            Assert.Empty(surface.Elements);
        }

        [Fact]
        public void SelectAllAndDeleteWhileDrawing_DoNotLeaveAnActiveDraft()
        {
            using var surface = CreateSurface();
            Click(surface, 20, 20);
            Click(surface, 100, 20);
            surface.SelectAllElements();
            surface.RemoveSelectedElements();

            Assert.Empty(surface.Elements);
            DrawPolyline(surface);
        }

        [Fact]
        public void SwitchingTools_CommitsPlacedVerticesAndRemovesPreview()
        {
            using var surface = CreateSurface();
            Click(surface, 20, 20);
            Click(surface, 100, 20);
            Mouse(surface, "OnMouseMove", 300, 250, MouseButtons.None);
            surface.DrawingMode = DrawingModes.Rect;

            var polyline = Assert.IsType<PolylineContainer>(Assert.Single(surface.Elements));
            Assert.Equal(2, polyline.Points.Length);
            Assert.False(polyline.DrawingBounds.Contains(300, 250));
            surface.Undo();
            Assert.Empty(surface.Elements);
        }

        [Fact]
        public void TinyHorizontalSegment_IsNotExpandedOrDuplicated()
        {
            using var surface = CreateSurface();
            Click(surface, 20, 20);
            Click(surface, 20, 20);
            Click(surface, 22, 20);
            surface.ProcessCmdKey(Keys.Enter);

            var polyline = Assert.IsType<PolylineContainer>(Assert.Single(surface.Elements));
            Assert.Equal(new[] { new Point(20, 20), new Point(22, 20) }, polyline.Points);
            Assert.True(polyline.ClickableAt(21, 20));
            Assert.Equal(2, polyline.Width);
            Assert.Equal(0, polyline.Height);
        }

        [Fact]
        public void ZoomedClicks_UseImageCoordinates()
        {
            using var surface = CreateSurface();
            surface.ZoomFactor = new Fraction(2, 1);
            Click(surface, 40, 40);
            Click(surface, 200, 40);
            surface.ProcessCanvasKey(Keys.Enter);

            var polyline = Assert.IsType<PolylineContainer>(Assert.Single(surface.Elements));
            Assert.Equal(new[] { new Point(20, 20), new Point(100, 20) }, polyline.Points);
        }

        [Fact]
        public void Rendering_IsStraightAndOpen_AndPreviewIsOnlyShownWhileDrawing()
        {
            using var surface = CreateSurface();
            surface.FieldAggregator.GetField(FieldType.SHADOW).Value = false;
            Click(surface, 20, 20);
            Click(surface, 100, 20);
            Click(surface, 100, 100);
            Mouse(surface, "OnMouseMove", 200, 100, MouseButtons.None);
            var polyline = Assert.IsType<PolylineContainer>(Assert.Single(surface.Elements));

            using (var edit = Render(polyline, RenderMode.EDIT))
            {
                Assert.True(edit.GetPixel(150, 100).A > 0);
            }

            using (var export = Render(polyline, RenderMode.EXPORT))
            {
                Assert.True(export.GetPixel(60, 20).A > 0);
                Assert.True(export.GetPixel(100, 60).A > 0);
                Assert.Equal(0, export.GetPixel(60, 60).A); // No closing diagonal or fill.
                Assert.Equal(0, export.GetPixel(150, 100).A); // No uncommitted preview.
            }

            surface.ProcessCanvasKey(Keys.Enter);
            using (var finished = Render(polyline, RenderMode.EDIT))
            {
                Assert.Equal(0, finished.GetPixel(150, 100).A);
            }

            Assert.True(polyline.ClickableAt(60, 20));
            Assert.True(polyline.ClickableAt(100, 60));
            Assert.False(polyline.ClickableAt(60, 60));
        }

        [Fact]
        public void MoveResizeAndUndo_PreserveTheVertices()
        {
            using var surface = CreateSurface();
            var polyline = DrawPolyline(surface);
            var original = polyline.Points;
            polyline.MakeBoundsChangeUndoable(false);
            polyline.Left = 40;
            polyline.Top = 50;
            polyline.Width = 160;
            polyline.Height = 40;

            Assert.Equal(new[] { new Point(40, 50), new Point(200, 50), new Point(200, 90) }, polyline.Points);
            surface.Undo();
            Assert.Equal(original, polyline.Points);
        }

        [Fact]
        public void Handles_AreAtEveryVertex_AndFollowBoundsChanges()
        {
            using var surface = CreateSurface();
            var polyline = DrawPolyline(surface);

            Assert.Equal(polyline.Points, polyline.Adorners.Select(adorner => (Point)Assert.IsType<PolylineVertexAdorner>(adorner).Location).ToArray());
            polyline.MoveBy(30, 40);
            polyline.Width *= 2;
            Assert.Equal(polyline.Points, polyline.Adorners.Select(adorner => (Point)Assert.IsType<PolylineVertexAdorner>(adorner).Location).ToArray());
        }

        [Fact]
        public void DragVertex_OnlyMovesThatVertex_AndIsOneUndoableEdit()
        {
            using var surface = CreateSurface();
            var polyline = DrawPolyline(surface);
            surface.Modified = false;
            Mouse(surface, "OnMouseDown", 100, 20);
            Mouse(surface, "OnMouseMove", 120, 30);
            Mouse(surface, "OnMouseMove", 140, 50);
            Mouse(surface, "OnMouseUp", 140, 50);

            var edited = new[] { new Point(20, 20), new Point(140, 50), new Point(100, 100) };
            Assert.Equal(edited, polyline.Points);
            Assert.True(surface.Modified);
            Assert.All(polyline.Adorners, adorner => Assert.False(adorner.IsActive));
            surface.Undo();
            Assert.Equal(new[] { new Point(20, 20), new Point(100, 20), new Point(100, 100) }, polyline.Points);
            surface.Redo();
            Assert.Equal(edited, polyline.Points);
            surface.Undo();
            surface.Undo();
            Assert.Empty(surface.Elements);
        }

        [Fact]
        public void DragAcrossAnotherVertex_KeepsTheOriginalHandleActive()
        {
            using var surface = CreateSurface();
            var polyline = DrawPolyline(surface);
            Mouse(surface, "OnMouseDown", 100, 100);
            Mouse(surface, "OnMouseMove", 20, 20);
            Mouse(surface, "OnMouseMove", 40, 70);
            Mouse(surface, "OnMouseUp", 40, 70);

            Assert.Equal(new[] { new Point(20, 20), new Point(100, 20), new Point(40, 70) }, polyline.Points);
            Assert.All(polyline.Adorners, adorner => Assert.False(adorner.IsActive));
        }

        [Fact]
        public void VertexDragAtZoom_UsesImageCoordinatesAndReleasePosition()
        {
            using var surface = CreateSurface();
            var polyline = DrawPolyline(surface);
            surface.ZoomFactor = new Fraction(2, 1);
            Mouse(surface, "OnMouseDown", 200, 40);
            Mouse(surface, "OnMouseUp", 280, 100);

            Assert.Equal(new[] { new Point(20, 20), new Point(140, 50), new Point(100, 100) }, polyline.Points);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(2, 2)]
        [InlineData(-3, -3)]
        public void ClickingAVertexWithoutMoving_DoesNotAddUndoOrModification(int offsetX, int offsetY)
        {
            using var surface = CreateSurface();
            DrawPolyline(surface);
            surface.Modified = false;
            Click(surface, 100 + offsetX, 20 + offsetY);

            Assert.False(surface.Modified);
            surface.Undo();
            Assert.Empty(surface.Elements);
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(1, 1)]
        [InlineData(2, 1)]
        public void VertexHitArea_MatchesTheDisplayedHandleAtEveryZoom(uint numerator, uint denominator)
        {
            using var surface = CreateSurface();
            var polyline = DrawPolyline(surface);
            surface.ZoomFactor = new Fraction(numerator, denominator);
            var adorner = Assert.IsType<PolylineVertexAdorner>(polyline.Adorners[1]);
            var displayLocation = surface.ToSurfaceCoordinates(adorner.Location);

            Assert.True(adorner.HitTest(surface.ToImageCoordinates(displayLocation.Offset(4, 0))));
            Assert.False(adorner.HitTest(surface.ToImageCoordinates(displayLocation.Offset(10, 0))));
        }

        [Fact]
        public void ClickingNearTheStart_ClosesAndFinishesAsOneUndoableObject()
        {
            using var surface = CreateSurface();
            var polyline = DrawClosedPolyline(surface);

            Assert.Same(polyline, Assert.Single(surface.SelectedElements));
            var closed = new[] { new Point(20, 20), new Point(100, 20), new Point(100, 100) };
            Assert.Equal(closed, polyline.Points);
            Assert.True(polyline.IsClosed);
            Assert.Equal(EditStatus.IDLE, polyline.Status);
            surface.Undo();
            Assert.Empty(surface.Elements);
            Assert.False(surface.CanUndo);
            surface.Redo();
            Assert.Equal(closed, Assert.IsType<PolylineContainer>(Assert.Single(surface.Elements)).Points);
        }

        [Fact]
        public void ClosedPolyline_IsJoinedAtTheStart_AndDraggingTheStartMovesBothSegments()
        {
            using var surface = CreateSurface();
            var polyline = DrawClosedPolyline(surface);

            Assert.Equal(3, polyline.Adorners.Count);
            Assert.True(polyline.ClickableAt(60, 60));
            using (var export = Render(polyline, RenderMode.EXPORT))
            {
                Assert.True(export.GetPixel(60, 60).A > 0); // The closing segment.
            }

            Mouse(surface, "OnMouseDown", 20, 20);
            Mouse(surface, "OnMouseMove", 40, 60);
            Mouse(surface, "OnMouseUp", 40, 60);
            Assert.Equal(new[] { new Point(40, 60), new Point(100, 20), new Point(100, 100) }, polyline.Points);
            Assert.True(polyline.IsClosed);
            Assert.True(polyline.ClickableAt(70, 40));
            Assert.True(polyline.ClickableAt(70, 80));
            Assert.False(polyline.ClickableAt(60, 20));
            surface.Undo();
            Assert.Equal(new[] { new Point(20, 20), new Point(100, 20), new Point(100, 100) }, polyline.Points);
        }

        [Fact]
        public void DoubleClickOnTheStart_ClosesWithoutStartingAnotherPolyline()
        {
            using var surface = CreateSurface();
            Click(surface, 20, 20);
            Click(surface, 100, 20);
            Click(surface, 100, 100);
            Click(surface, 22, 22);
            Click(surface, 22, 22, clicks: 2);
            Mouse(surface, "OnMouseDoubleClick", 22, 22, clicks: 2);

            var polyline = Assert.IsType<PolylineContainer>(Assert.Single(surface.Elements));
            Assert.True(polyline.IsClosed);
            Assert.Equal(3, polyline.Points.Length);
            Mouse(surface, "OnMouseMove", 200, 200, MouseButtons.None);
            Assert.False(polyline.DrawingBounds.Contains(200, 200));
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 1)]
        [InlineData(1, 2)]
        public void ClosingSnapDistance_IsConstantOnScreen(uint numerator, uint denominator)
        {
            using var surface = CreateSurface();
            surface.ZoomFactor = new Fraction(numerator, denominator);
            using var polyline = new PolylineContainer(surface);
            polyline.AddPoint(new Point(100, 100), snapToAxes: false);
            polyline.AddPoint(new Point(200, 100), snapToAxes: false);
            polyline.AddPoint(new Point(200, 200), snapToAxes: false);

            // A click farther from the start adds a corner.
            int farOffset = (int)(40 / (double)surface.ZoomFactor);
            polyline.AddPoint(new Point(100 + farOffset, 100), snapToAxes: false);
            Assert.False(polyline.IsClosed);
            Assert.Equal(4, polyline.Points.Length);

            int nearOffset = (int)(4 / (double)surface.ZoomFactor);
            polyline.AddPoint(new Point(100 + nearOffset, 100), snapToAxes: false);
            Assert.True(polyline.IsClosed);
            Assert.Equal(4, polyline.Points.Length);
        }

        [Fact]
        public void ClosingPreviewAndClick_SnapToTheSameStartEvenWithShift()
        {
            using var surface = CreateSurface();
            using var polyline = new PolylineContainer(surface) { Status = EditStatus.DRAWING };
            polyline.SetFieldValue(FieldType.SHADOW, false);
            polyline.SetFieldValue(FieldType.LINE_THICKNESS, 2);
            polyline.AddPoint(new Point(20, 20), snapToAxes: false);
            polyline.AddPoint(new Point(100, 20), snapToAxes: false);
            polyline.AddPoint(new Point(100, 100), snapToAxes: false);
            polyline.UpdatePreview(new Point(24, 23), snapToAxes: true);
            using var bitmap = new Bitmap(150, 150);
            using var graphics = Graphics.FromImage(bitmap);
            polyline.Draw(graphics, RenderMode.EDIT);
            Assert.True(bitmap.GetPixel(60, 60).A > 0);

            graphics.Clear(Color.Transparent);
            polyline.Draw(graphics, RenderMode.EXPORT);
            Assert.Equal(0, bitmap.GetPixel(60, 60).A);
            polyline.AddPoint(new Point(24, 23), snapToAxes: true);
            Assert.True(polyline.IsClosed);
            Assert.Equal(3, polyline.Points.Length);
            polyline.Draw(graphics, RenderMode.EXPORT);
            Assert.True(bitmap.GetPixel(60, 60).A > 0);
        }

        [Fact]
        public void ClosingSnap_RequiresAtLeastThreePlacedVertices()
        {
            using var surface = CreateSurface();
            Click(surface, 20, 20);
            Click(surface, 22, 21);
            Assert.Empty(surface.SelectedElements);
            surface.ProcessCanvasKey(Keys.Enter);
            var polyline = Assert.IsType<PolylineContainer>(Assert.Single(surface.Elements));
            Assert.Equal(new[] { new Point(20, 20), new Point(22, 21) }, polyline.Points);
            Assert.False(polyline.IsClosed);
        }

        [Fact]
        public void ClosedPolylineSaveAndLoad_StaysClosed()
        {
            using var surface = CreateSurface();
            DrawClosedPolyline(surface);
            using var stream = new MemoryStream();
            surface.SaveElementsToStream(stream);
            stream.Position = 0;
            using var loaded = CreateSurface();
            loaded.LoadElementsFromStream(stream);
            var polyline = Assert.IsType<PolylineContainer>(Assert.Single(loaded.Elements));
            Assert.True(polyline.IsClosed);
            Assert.Equal(3, polyline.Points.Length);
            Assert.True(polyline.ClickableAt(60, 60));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-80)]
        [InlineData(37)]
        public void UndoVertexEditAfterResize_RestoresOriginalGeometryExactly(int width)
        {
            using var surface = CreateSurface();
            var polyline = DrawPolyline(surface);
            var original = polyline.Points;
            polyline.MakeBoundsChangeUndoable(false);
            polyline.Width = width;
            Point last = polyline.Points[2];
            Mouse(surface, "OnMouseDown", last.X, last.Y);
            Mouse(surface, "OnMouseMove", 150, 120);
            Mouse(surface, "OnMouseUp", 150, 120);

            surface.Undo(); // Vertex edit.
            Assert.Equal(width, polyline.Width);
            surface.Undo(); // Resize, including recovery of a collapsed dimension.
            Assert.Equal(original, polyline.Points);
            surface.Redo();
            surface.Redo();
            Assert.Equal(new Point(150, 120), polyline.Points[2]);
        }

        [Theory]
        [InlineData(100, 35, 100, 20)]
        [InlineData(35, 100, 20, 100)]
        [InlineData(-80, 35, -80, 20)]
        [InlineData(35, -80, 20, -80)]
        public void ShiftDrawing_SnapsToHorizontalOrVertical(int x, int y, int expectedX, int expectedY)
        {
            using var surface = CreateSurface();
            using var polyline = new PolylineContainer(surface);
            polyline.AddPoint(new Point(20, 20), snapToAxes: true);
            polyline.AddPoint(new Point(x, y), snapToAxes: true);

            Assert.Equal(new[] { new Point(20, 20), new Point(expectedX, expectedY) }, polyline.Points);
            polyline.AddPoint(new Point(150, 170), snapToAxes: false);
            Assert.Equal(new Point(150, 170), polyline.Points[2]);
        }

        [Fact]
        public void ShiftPreview_MatchesCommittedSegment_AndCanReturnToFreeDrawing()
        {
            using var surface = CreateSurface();
            using var polyline = new PolylineContainer(surface) { Status = EditStatus.DRAWING };
            polyline.SetFieldValue(FieldType.SHADOW, false);
            polyline.SetFieldValue(FieldType.LINE_THICKNESS, 2);
            polyline.AddPoint(new Point(20, 20), snapToAxes: false);
            polyline.UpdatePreview(new Point(100, 40), snapToAxes: true);
            using var bitmap = new Bitmap(200, 100);
            using var graphics = Graphics.FromImage(bitmap);
            polyline.Draw(graphics, RenderMode.EDIT);

            Assert.True(bitmap.GetPixel(60, 20).A > 0);
            Assert.Equal(0, bitmap.GetPixel(60, 30).A);
            graphics.Clear(Color.Transparent);
            polyline.UpdatePreview(new Point(100, 40), snapToAxes: false);
            polyline.Draw(graphics, RenderMode.EDIT);
            Assert.True(bitmap.GetPixel(60, 30).A > 0);
            Assert.Equal(0, bitmap.GetPixel(60, 20).A);

            polyline.AddPoint(new Point(100, 40), snapToAxes: true);
            Assert.Equal(new Point(100, 20), polyline.Points[1]);
            // The next segment snaps from the last committed vertex.
            polyline.AddPoint(new Point(90, 80), snapToAxes: true);
            Assert.Equal(new Point(100, 80), polyline.Points[2]);
        }

        [Theory]
        [InlineData(0, 35, 35, 35, 20)]
        [InlineData(0, 90, 80, 100, 80)]
        [InlineData(1, 120, 40, 120, 20)]
        [InlineData(1, 30, 120, 20, 120)]
        [InlineData(2, 150, 30, 150, 20)]
        [InlineData(2, 110, 120, 100, 120)]
        public void ShiftVertexEdit_UsesTheAdjacentVertex(int index, int x, int y, int expectedX, int expectedY)
        {
            using var surface = CreateSurface();
            var polyline = DrawPolyline(surface);
            var point = new Point(x, y);

            Assert.Equal(new Point(expectedX, expectedY), polyline.ConstrainVertex(index, point, snapToAxes: true));
            Assert.Equal(point, polyline.ConstrainVertex(index, point, snapToAxes: false));
        }

        [Fact]
        public void ShiftVertexEdit_OfTheClosedStart_UsesTheLastVertex()
        {
            using var surface = CreateSurface();
            var polyline = DrawClosedPolyline(surface);

            Assert.Equal(new Point(100, 30), polyline.ConstrainVertex(0, new Point(90, 30), snapToAxes: true));
        }

        [Fact]
        public void Rotate_TransformsEveryVertex()
        {
            using var surface = CreateSurface();
            var polyline = DrawPolyline(surface);
            using var matrix = new Matrix(0, 1, -1, 0, 300, 0);
            polyline.Transform(matrix);

            Assert.Equal(new[] { new Point(280, 20), new Point(280, 100), new Point(200, 100) }, polyline.Points);
            Assert.True(polyline.ClickableAt(280, 60));
        }

        [Fact]
        public void SavedElementsAndClipboard_RoundTripAsEditablePolylines()
        {
            using var surface = CreateSurface();
            var original = DrawPolyline(surface);
            using var stream = new MemoryStream();
            surface.SaveElementsToStream(stream);
            stream.Position = 0;
            using var loaded = CreateSurface();
            loaded.LoadElementsFromStream(stream);

            var restored = Assert.IsType<PolylineContainer>(Assert.Single(loaded.Elements));
            Assert.Equal(original.Points, restored.Points);
            Assert.Equal(restored.Points, restored.Adorners.Select(adorner => (Point)Assert.IsType<PolylineVertexAdorner>(adorner).Location).ToArray());
            Assert.True(restored.ClickableAt(100, 60));
            loaded.DrawingMode = DrawingModes.None;
            loaded.SelectElement(restored);
            Mouse(loaded, "OnMouseDown", 100, 20);
            Mouse(loaded, "OnMouseUp", 150, 40);
            Assert.Equal(new Point(150, 40), restored.Points[1]);

            var copied = DrawableContainerClipboard.Deserialize(DrawableContainerClipboard.Serialize(surface.Elements));
            using var pasted = Assert.IsType<PolylineContainer>(Assert.Single(copied));
            pasted.MoveBy(10, 15);
            Assert.Equal(new Point(30, 35), pasted.Points[0]);
            Assert.Equal(new Point(20, 20), original.Points[0]);
        }
    }
}
