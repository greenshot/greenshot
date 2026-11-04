using System;
using Greenshot.Base.Interfaces.Drawing;
using Dapplo.Windows.Common.Extensions;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Effects;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Measurements;
using Xunit;

namespace Greenshot.Tests.Editor
{
    [Collection(TestCollections.InputState)]
    public class UpstreamIntegrationTests
    {
        public UpstreamIntegrationTests() => TestEnvironment.EnsureInitialized();

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void RecipeEffect_TransformsCalibrationAndDimensionGeometry(int operation)
        {
            using var surface = new Surface(new Bitmap(200, 200));
            var rectangle = new RectangleContainer(surface) { Left = 50, Top = 50, Width = 100, Height = 40 };
            surface.AddElement(rectangle, false);
            foreach (var edge in DimensionLayout.Edges(rectangle)) rectangle.SetDimensionVisible(edge.Index, true);
            surface.SetMeasurements(MeasurementState.Calibrate(100, 10, MeasurementUnit.Millimetres));
            var before = DimensionLayout.Labels(rectangle).Select(label => label.Text).OrderBy(text => text).ToArray();
            Assert.True(surface.ApplyEffect(operation == 2 ? (IEffect)new RotateEffect(90) : new ResizeEffect(400, operation == 0 ? 400 : 200, false)));
            if (operation == 1)
            {
                Assert.False(surface.Measurements.IsCalibrated);
                Assert.All(DimensionLayout.Labels(rectangle), label => Assert.EndsWith(" px", label.Text));
            }
            else
            {
                Assert.True(surface.Measurements.IsCalibrated);
                Assert.Equal(before, DimensionLayout.Labels(rectangle).Select(label => label.Text).OrderBy(text => text).ToArray());
                Assert.Equal(10, surface.Measurements.Convert(operation == 0 ? 200 : 100), 5);
            }
        }

        [Fact]
        public void InjectedModifiers_ConstrainPolygonDisableThroughSnapAndToggleBoxSelection()
        {
            Sta(() =>
            {
                Keys modifiers = Keys.None;
                InputState.ModifierKeysSource = () => modifiers;
                try
                {
                    using var surface = new Surface(new Bitmap(400, 400));
                    surface.DrawingMode = DrawingModes.Polygon;
                    Mouse(surface, "SurfaceMouseDown", 20, 20);
                    modifiers = Keys.Shift;
                    Mouse(surface, "SurfaceMouseDown", 100, 28);
                    modifiers = Keys.None;
                    Mouse(surface, "SurfaceMouseDown", 100, 130);
                    Mouse(surface, "SurfaceMouseDown", 20, 20);
                    var polygon = Assert.IsType<PolygonContainer>(Assert.Single(surface.Elements));
                    Assert.Equal(20, polygon.Vertices[1].Y);
                    surface.DeselectAllElements();
                    var target = new LineContainer(surface) { Left = 50, Top = 50, Width = 20, Height = 0 };
                    surface.AddElement(target, false);
                    surface.SetObjectSnapping(false, true);
                    Assert.Equal(new Point(100,100), surface.SnapGesture(new Point(102,98), Point.Empty, null, true));
                    modifiers = Keys.Control;
                    Assert.Equal(new Point(102,98), surface.SnapGesture(new Point(102,98), Point.Empty, null, true));
                    surface.SelectElement(target);
                    modifiers = Keys.Shift;
                    Mouse(surface, "SurfaceMouseDown", 5, 5);
                    Mouse(surface, "SurfaceMouseMove", 150, 160);
                    Mouse(surface, "SurfaceMouseUp", 150, 160);
                    Assert.True(polygon.Selected);
                    Assert.False(target.Selected);
                    Assert.False(surface.Capture);
                }
                finally { InputState.ModifierKeysSource = () => Keys.None; }
            });
        }

        [Fact]
        public void GroupEdgeSnap_HasOneUndoAndPreservesSignedAndAffineGeometry()
        {
            using var surface = new Surface(new Bitmap(400, 400));
            var rectangle = new RectangleContainer(surface) { Left = 100, Top = 80, Width = 60, Height = 30 };
            var line = new LineContainer(surface) { Left = 240, Top = 180, Width = -50, Height = -30 };
            surface.AddElement(rectangle, false); surface.SelectElement(rectangle);
            surface.RotateSelectedObjects(37);
            surface.AddElement(line, false); surface.SelectElement(line);
            Assert.True(surface.GroupSelectedElements());
            var originalRectangle = rectangle.ObjectTransformState; var originalLine = line.Bounds;
            ((DrawableContainerList)surface.SelectedElements).SnapAllToEdge(Direction.LEFT, surface);
            Assert.Equal(0, rectangle.Bounds.Left); Assert.Equal(0, line.Bounds.Left);
            Assert.Equal(-50, line.Width); Assert.Equal(-30, line.Height);
            surface.Undo();
            Assert.Equal(originalRectangle, rectangle.ObjectTransformState); Assert.Equal(originalLine, line.Bounds);
            Assert.Equal(rectangle.GroupId, line.GroupId); Assert.NotEqual(Guid.Empty, line.GroupId);
        }

        private static void Mouse(Surface surface, string method, int x, int y) =>
            typeof(Surface).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(surface,
                new object[] { surface, new MouseEventArgs(MouseButtons.Left, 1, x, y, 0) });
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
