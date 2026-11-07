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
using System.Drawing;
using System.Reflection;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Xunit;
namespace Greenshot.Tests.Editor
{
    [Collection(TestCollections.InputState)]
    public class RubberBandSelectionTests
    {
        public RubberBandSelectionTests() => TestEnvironment.EnsureInitialized();

        private static void Sta(Action action)
        {
            Exception failure = null;
            var uiThread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
            });
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();
            Assert.True(uiThread.Join(TimeSpan.FromSeconds(30)), "Surface interaction timed out.");
            if (failure != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        private static void Mouse(Surface s, string name, int x, int y)
            => typeof(Control).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(s, new object[] { new MouseEventArgs(MouseButtons.Left, 1, x, y, 0) });

        private static RectangleContainer Box(Surface s, int x)
        {
            var box = new RectangleContainer(s) { Left = x, Top = 40, Width = 20, Height = 20 };
            box.SetFieldValue(FieldType.SHADOW, false);
            box.SetFieldValue(FieldType.FILL_COLOR, Color.Blue);
            s.AddElement(box, false);
            return box;
        }

        [Fact]
        public void DragSelectsEnclosedObjectsAndEscapeRestoresInitialSelection() => Sta(() =>
        {
            using var s = new Surface(new Bitmap(300, 200));
            var first = Box(s, 40);
            var second = Box(s, 160);
            s.DrawingMode = DrawingModes.None;
            s.DeselectAllElements();
            s.SelectElement(second);
            Mouse(s, "OnMouseDown", 10, 10);
            Mouse(s, "OnMouseMove", 100, 100);
            Assert.True(first.Selected);
            Assert.False(second.Selected);
            Assert.True(s.ProcessCmdKey(Keys.Escape));
            Assert.False(first.Selected);
            Assert.True(second.Selected);
        });

        [Fact]
        public void PartialIntersectionDoesNotSelectAndReleaseFinishesGesture() => Sta(() =>
        {
            using var s = new Surface(new Bitmap(300, 200));
            var first = Box(s, 40);
            var second = Box(s, 160);
            s.DrawingMode = DrawingModes.None;
            s.DeselectAllElements();
            Mouse(s, "OnMouseDown", 10, 10);
            Mouse(s, "OnMouseMove", 170, 100);
            Mouse(s, "OnMouseUp", 170, 100);
            Assert.True(first.Selected);
            Assert.False(second.Selected);
            Assert.False((bool)typeof(Surface).GetField("_selectionBoxActive", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(s));
        });
        [Fact]
        public void ShiftToggleAndLostCaptureRestorePriorSelectionAtZoom() => Sta(() =>
        {
            using var s = new Surface(new Bitmap(300, 200));
            var first = Box(s, 40);
            var second = Box(s, 160);
            s.DrawingMode = DrawingModes.None;
            s.DeselectAllElements();
            s.SelectElement(first);
            s.SelectElement(second);
            s.ZoomFactor = new Greenshot.Base.Core.Fraction(2, 1);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(Surface).GetMethod("BeginSelectionBox", flags).Invoke(s, new object[] { new Point(10, 10), Keys.Shift });
            Mouse(s, "OnMouseMove", 200, 200);
            Assert.False(first.Selected);
            Assert.True(second.Selected);
            s.Capture = false;
            Assert.True(first.Selected);
            Assert.True(second.Selected);
        });
        [Fact]
        public void SubThresholdReleaseUsesExistingObjectClickSelection() => Sta(() =>
        {
            using var s = new Surface(new Bitmap(300, 200));
            var first = Box(s, 40);
            var second = Box(s, 160);
            s.DrawingMode = DrawingModes.None;
            s.DeselectAllElements();
            s.SelectElement(second);
            int edge = System.Linq.Enumerable.Range(0, 60).First(x => first.ClickableAt(x, 50));
            Assert.False(first.ClickableAt(edge - 1, 50));
            Mouse(s, "OnMouseDown", edge - 1, 50);
            Mouse(s, "OnMouseUp", edge + 1, 50);
            Assert.True(first.Selected);
            Assert.False(second.Selected);
        });

        [Fact]
        public void ExistingClicksAndShiftClicksKeepUpstreamSelectionRules() => Sta(() =>
        {
            Keys modifiers = Keys.None;
            Greenshot.Base.Core.InputState.ModifierKeysSource = () => modifiers;
            try
            {
                using var s = new Surface(new Bitmap(300, 200));
                var first = Box(s, 40);
                var second = Box(s, 160);
                first.Width = first.Height = second.Width = second.Height = 80;
                s.DrawingMode = DrawingModes.None;
                s.DeselectAllElements();
                Mouse(s, "OnMouseDown", 80, 80);
                Mouse(s, "OnMouseUp", 80, 80);
                Assert.True(first.Selected, "initial click selects first");
                Assert.False(second.Selected);
                modifiers = Keys.Shift;
                Mouse(s, "OnMouseDown", 200, 80);
                Mouse(s, "OnMouseUp", 200, 80);
                Assert.True(first.Selected);
                Assert.True(second.Selected);
                Mouse(s, "OnMouseDown", 10, 10);
                Mouse(s, "OnMouseUp", 10, 10);
                Assert.True(first.Selected);
                Assert.True(second.Selected);
                Mouse(s, "OnMouseDown", 80, 80);
                Mouse(s, "OnMouseUp", 80, 80);
                Assert.False(first.Selected);
                Assert.True(second.Selected);
                modifiers = Keys.None;
                Mouse(s, "OnMouseDown", 10, 10);
                Mouse(s, "OnMouseUp", 10, 10);
                Assert.False(first.Selected);
                Assert.False(second.Selected);
            }
            finally
            {
                Greenshot.Base.Core.InputState.ModifierKeysSource = () => Keys.None;
            }
        });

        [Theory]
        [InlineData(Keys.None)]
        [InlineData(Keys.Control)]
        public void ExistingObjectDragAndUndoKeepUpstreamBehavior(Keys modifiers) => Sta(() =>
        {
            Greenshot.Base.Core.InputState.ModifierKeysSource = () => modifiers;
            try
            {
                using var s = new Surface(new Bitmap(400, 200));
                var first = Box(s, 40);
                var second = Box(s, 160);
                first.Width = 120;
                first.Height = 100;
                s.DrawingMode = DrawingModes.None;
                s.DeselectAllElements();
                s.SelectElement(first);
                s.SelectElement(second);
                Assert.Same(first, s.Elements.ClickableElementAt(100, 90));
                Mouse(s, "OnMouseDown", 100, 90);
                Mouse(s, "OnMouseMove", 130, 110);
                Mouse(s, "OnMouseUp", 130, 110);
                Assert.Equal(70, first.Left);
                Assert.Equal(190, second.Left);
                Assert.Equal(60, first.Top);
                s.Undo();
                Assert.Equal(40, first.Left);
                Assert.Equal(160, second.Left);
                Assert.Equal(40, first.Top);
            }
            finally
            {
                Greenshot.Base.Core.InputState.ModifierKeysSource = () => Keys.None;
            }
        });

        [Fact]
        public void DrawingToolEmptyCanvasDragStillCreatesItsShape() => Sta(() =>
        {
            using var s = new Surface(new Bitmap(300, 200)) { DrawingMode = DrawingModes.Rect };
            Mouse(s, "OnMouseDown", 10, 10);
            Mouse(s, "OnMouseMove", 100, 110);
            Mouse(s, "OnMouseUp", 100, 110);
            var rectangle = Assert.IsType<RectangleContainer>(Assert.Single(s.Elements));
            Assert.Equal(10, rectangle.Left);
            Assert.Equal(10, rectangle.Top);
            Assert.Equal(90, rectangle.Width);
            Assert.Equal(100, rectangle.Height);
            s.Undo();
            Assert.Empty(s.Elements);
        });
        private static T GetSurfaceField<T>(Surface surface, string name)
        {
            return (T)typeof(Surface).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(surface);
        }

        private static object InvokeSurface(Surface surface, string methodName, params object[] arguments)
        {
            return typeof(Surface).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(surface, arguments);
        }

        [Fact]
        public void PointerMouseDownRemovesAnUnconfirmedCropBeforeStartingSelection() => Sta(() =>
        {
            using var surface = new Surface(new Bitmap(400, 300));
            surface.DrawingMode = DrawingModes.Crop;
            Mouse(surface, "OnMouseDown", 20, 20);
            Mouse(surface, "OnMouseMove", 120, 110);
            Mouse(surface, "OnMouseUp", 120, 110);
            var crop = GetSurfaceField<CropContainer>(surface, "_cropContainer");
            Assert.NotNull(crop);
            Assert.Contains(crop, surface.Elements);

            surface.DrawingMode = DrawingModes.None;
            Assert.Same(crop, GetSurfaceField<CropContainer>(surface, "_cropContainer"));
            Mouse(surface, "OnMouseDown", 250, 200);

            Assert.Null(GetSurfaceField<CropContainer>(surface, "_cropContainer"));
            Assert.DoesNotContain(crop, surface.Elements);
            Assert.NotNull(GetSurfaceField<Point?>(surface, "_selectionBoxStart"));
            Mouse(surface, "OnMouseUp", 250, 200);
        });

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CaptureLossWithoutASelectionBoxPreservesTheCurrentGesture(bool drawShape) => Sta(() =>
        {
            using var surface = new Surface(new Bitmap(400, 300));
            RectangleContainer draggedElement = null;
            if (drawShape)
            {
                surface.DrawingMode = DrawingModes.Rect;
                Mouse(surface, "OnMouseDown", 10, 10);
                Mouse(surface, "OnMouseMove", 50, 60);
            }
            else
            {
                draggedElement = Box(surface, 40);
                draggedElement.Width = 120;
                draggedElement.Height = 100;
                surface.DrawingMode = DrawingModes.None;
                surface.SelectElement(draggedElement);
                Mouse(surface, "OnMouseDown", 100, 90);
            }

            var drawingElement = GetSurfaceField<Greenshot.Base.Interfaces.Drawing.IDrawableContainer>(surface, "_drawingElement");
            var mouseDownElement = GetSurfaceField<Greenshot.Base.Interfaces.Drawing.IDrawableContainer>(surface, "_mouseDownElement");
            Assert.True(GetSurfaceField<bool>(surface, "_mouseDown"));
            surface.Capture = true;
            surface.Capture = false;

            Assert.True(GetSurfaceField<bool>(surface, "_mouseDown"));
            Assert.Same(drawingElement, GetSurfaceField<Greenshot.Base.Interfaces.Drawing.IDrawableContainer>(surface, "_drawingElement"));
            Assert.Same(mouseDownElement, GetSurfaceField<Greenshot.Base.Interfaces.Drawing.IDrawableContainer>(surface, "_mouseDownElement"));
            if (drawShape)
            {
                Mouse(surface, "OnMouseMove", 100, 110);
                Mouse(surface, "OnMouseUp", 100, 110);
                var rectangle = Assert.IsType<RectangleContainer>(Assert.Single(surface.Elements));
                Assert.Equal(90, rectangle.Width);
                Assert.Equal(100, rectangle.Height);
            }
            else
            {
                Assert.Equal(Greenshot.Base.Interfaces.Drawing.EditStatus.MOVING, draggedElement.Status);
                Mouse(surface, "OnMouseMove", 130, 110);
                Mouse(surface, "OnMouseUp", 130, 110);
                Assert.Equal(70, draggedElement.Left);
                Assert.Equal(60, draggedElement.Top);
            }
        });

        [Fact]
        public void SelectionDeltaKeepsRetainedObjectsSelectedAndNotifiesOncePerChange() => Sta(() =>
        {
            using var surface = new Surface(new Bitmap(500, 300));
            var retainedElement = Box(surface, 40);
            var addedElement = Box(surface, 160);
            surface.DrawingMode = DrawingModes.None;
            surface.SelectElement(retainedElement);
            using var editor = new Greenshot.Editor.Forms.ImageEditorForm(surface, true, false);
            _ = surface.Handle;
            var invalidatedAreas = new System.Collections.Generic.List<Rectangle>();
            surface.Invalidated += (sender, eventArgs) => invalidatedAreas.Add(eventArgs.InvalidRect);
            int retainedSelectionChanges = 0;
            int selectionNotifications = 0;
            retainedElement.PropertyChanged += (sender, eventArgs) =>
            {
                if (eventArgs.PropertyName == "Selected")
                {
                    retainedSelectionChanges++;
                }
            };
            surface.MovingElementChanged += (sender, eventArgs) => selectionNotifications++;

            InvokeSurface(surface, "BeginSelectionBox", new Point(10, 10), Keys.None);
            InvokeSurface(surface, "MoveSelectionBox", new Point(100, 100));
            Assert.Equal(0, selectionNotifications);
            InvokeSurface(surface, "MoveSelectionBox", new Point(220, 100));
            Assert.True(addedElement.Selected);
            Assert.Equal(1, selectionNotifications);
            InvokeSurface(surface, "MoveSelectionBox", new Point(230, 100));
            Assert.Equal(1, selectionNotifications);
            InvokeSurface(surface, "MoveSelectionBox", new Point(100, 100));
            Assert.False(addedElement.Selected);
            Assert.Equal(2, selectionNotifications);
            Assert.Equal(0, retainedSelectionChanges);
            Assert.True(retainedElement.Selected);
            Assert.Single(surface.SelectedElements);
            surface.ProcessCmdKey(Keys.Escape);
            Assert.Equal(2, selectionNotifications);
            Assert.Equal(0, retainedSelectionChanges);
            Assert.NotEmpty(invalidatedAreas);
            Assert.DoesNotContain(surface.ClientRectangle, invalidatedAreas);
        });

        [Theory]
        [InlineData(1u, 2u, 192, 8, 8, 34, 29)]
        [InlineData(3u, 2u, 144, 28, 28, 94, 79)]
        [InlineData(2u, 1u, 96, 38, 38, 124, 104)]
        public void BoxInvalidationCoversPaintAtZoomAndDpiWithoutInvalidatingTheSurface(
            uint zoomNumerator, uint zoomDenominator, int dpi, int left, int top, int width, int height) => Sta(() =>
        {
            using var viewport = new Panel { Size = new Size(300, 250) };
            using var surface = new Surface(new Bitmap(1000, 800));
            viewport.Controls.Add(surface);
            surface.ZoomFactor = new Greenshot.Base.Core.Fraction(zoomNumerator, zoomDenominator);
            surface.AdjustToDpi(dpi);
            surface.Location = new Point(-100, -50);
            _ = surface.Handle;
            var invalidatedAreas = new System.Collections.Generic.List<Rectangle>();
            surface.Invalidated += (sender, eventArgs) => invalidatedAreas.Add(eventArgs.InvalidRect);
            InvokeSurface(surface, "BeginSelectionBox", new Point(20, 20), Keys.None);
            invalidatedAreas.Clear();

            InvokeSurface(surface, "MoveSelectionBox", new Point(80, 70));

            var firstPaintBounds = new Rectangle(left, top, width, height);
            Assert.Equal(firstPaintBounds, Assert.Single(invalidatedAreas));
            using var painting = new Bitmap(surface.Width, surface.Height);
            using (var graphics = Graphics.FromImage(painting))
            {
                InvokeSurface(surface, "PaintSelectionBox", graphics);
            }
            for (int imageY = 0; imageY < 150; imageY++)
            {
                for (int imageX = 0; imageX < 200; imageX++)
                {
                    if (painting.GetPixel(imageX, imageY).A != 0)
                    {
                        Assert.True(firstPaintBounds.Contains(imageX, imageY));
                    }
                }
            }

            invalidatedAreas.Clear();
            InvokeSurface(surface, "MoveSelectionBox", new Point(90, 80));
            Assert.Equal(2, invalidatedAreas.Count);
            Assert.Equal(firstPaintBounds, invalidatedAreas[0]);
            Rectangle finalPaintBounds = invalidatedAreas[1];
            Assert.NotEqual(surface.ClientRectangle, finalPaintBounds);
            invalidatedAreas.Clear();
            surface.ProcessCmdKey(Keys.Escape);
            Assert.Equal(finalPaintBounds, Assert.Single(invalidatedAreas));
        });
    }
}
