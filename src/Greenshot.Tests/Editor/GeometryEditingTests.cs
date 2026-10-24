using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Measurements;
using Xunit;

namespace Greenshot.Tests.Editor
{
    public class GeometryEditingTests
    {
        public GeometryEditingTests() => TestEnvironment.EnsureInitialized();

        private static double Value(DrawableContainer shape, string key) => MeasurementGeometry.GetProperties(shape).Single(p => p.Key == key).Pixels;
        private static void Edit(DrawableContainer shape, string key, double value, MeasurementState state = null)
        {
            Assert.True(MeasurementGeometry.TryEdit(shape, state ?? MeasurementState.Pixels, key, value, out string error), error);
        }

        [Fact]
        public void LineAndArrow_HaveLengthAndGlobalEndpoints()
        {
            using var surface = new Surface(new Bitmap(300, 200));
            foreach (var shape in new DrawableContainer[] { new LineContainer(surface), new ArrowContainer(surface) })
            {
                shape.Left = 80; shape.Top = 90; shape.Width = -30; shape.Height = -40;
                surface.AddElement(shape);
                Assert.Equal(50, Value(shape, "L1"));
                Assert.Equal(80, Value(shape, "X1"));
                Assert.Equal(50, Value(shape, "X2"));
                Edit(shape, "L1", 100);
                Assert.Equal(80, shape.Left); Assert.Equal(90, shape.Top);
                Assert.Equal(-60, shape.Width); Assert.Equal(-80, shape.Height);
                surface.Undo();
                Assert.Equal(-30, shape.Width); Assert.Equal(-40, shape.Height);
                surface.Redo();
                Assert.Equal(100, Value(shape, "L1"));
                Edit(shape, "X1", 70);
                Assert.Equal(20, Value(shape, "X2"));
                Edit(shape, "Y2", 15);
                Assert.Equal(90, Value(shape, "Y1")); Assert.Equal(15, Value(shape, "Y2"));
            }
        }

        [Fact]
        public void Rectangle_ReportsCornersAndEditsEdgesWithoutChangingDirection()
        {
            using var surface = new Surface(new Bitmap(200, 200));
            var shape = new RectangleContainer(surface) { Left = 60, Top = 80, Width = -40, Height = -60 };
            surface.AddElement(shape);
            Assert.Equal(20, Value(shape, "X1")); Assert.Equal(20, Value(shape, "Y1"));
            Assert.Equal(60, Value(shape, "X2")); Assert.Equal(80, Value(shape, "Y3"));
            Edit(shape, "L1", 50);
            Assert.Equal(20, Value(shape, "X1")); Assert.Equal(70, Value(shape, "X2"));
            Assert.Equal(-50, shape.Width);
            Edit(shape, "X4", 10);
            Assert.Equal(10, Value(shape, "X1")); Assert.Equal(10, Value(shape, "X4"));
            Assert.Equal(70, Value(shape, "X3"));
            Assert.False(MeasurementGeometry.TryEdit(shape, MeasurementState.Pixels, "X1", 90, out _));
            Assert.Equal(10, Value(shape, "X1"));
        }

        [Fact]
        public void Ellipse_DiameterKeepsCentreAndCentreEditsTranslateIt()
        {
            using var surface = new Surface(new Bitmap(300, 300));
            var ellipse = new EllipseContainer(surface) { Left = 100, Top = 100, Width = -40, Height = -20 };
            surface.AddElement(ellipse);
            Assert.Equal(80, Value(ellipse, "XC")); Assert.Equal(90, Value(ellipse, "YC"));
            Edit(ellipse, "D1", 60);
            Assert.Equal(80, Value(ellipse, "XC")); Assert.Equal(-60, ellipse.Width);
            Edit(ellipse, "YC", 150);
            Assert.Equal(150, Value(ellipse, "YC")); Assert.Equal(20, Value(ellipse, "D2"));
            surface.Undo();
            Assert.Equal(90, Value(ellipse, "YC"));
        }

        [Fact]
        public void ScaledEdits_ConvertBackToImagePixelsAndRejectInvalidInput()
        {
            using var surface = new Surface(new Bitmap(300, 300));
            var shape = new RectangleContainer(surface) { Left = 10, Top = 20, Width = 100, Height = 50 };
            surface.AddElement(shape);
            var state = MeasurementState.Calibrate(100, 10, MeasurementUnit.Millimetres);
            Edit(shape, "L1", 15, state);
            Assert.Equal(150, shape.Width);
            Edit(shape, "X1", -2, state);
            Assert.Equal(-20, shape.Bounds.Left);
            foreach (double invalid in new[] { double.NaN, double.PositiveInfinity, -10d, 0d, 1e30 })
                Assert.False(MeasurementGeometry.TryEdit(shape, state, "L1", invalid, out _));
            Assert.Equal(180, shape.Width);
            Assert.False(MeasurementGeometry.TryEdit(shape, state, "UNKNOWN", 1, out _));
        }

        [Fact]
        public void FractionalGeometry_DisplaysActualPixelResultAndIgnoresNoOpEdits()
        {
            using var surface = new Surface(new Bitmap(100, 100));
            var ellipse = new EllipseContainer(surface) { Left = 10, Top = 20, Width = 5, Height = 7 };
            surface.AddElement(ellipse);
            Assert.Equal(12.5, Value(ellipse, "XC"));
            Edit(ellipse, "XC", 12.5);
            Assert.Equal(10, ellipse.Left);
            Edit(ellipse, "D1", 8);
            Assert.InRange(Math.Abs(Value(ellipse, "XC") - 12.5), 0, 0.5);
            Edit(ellipse, "YC", -10.5);
            Assert.Equal(-10.5, Value(ellipse, "YC"));
        }

        [Theory]
        [InlineData(60, 4, 60, 0)]
        [InlineData(3, 60, 0, 60)]
        [InlineData(-60, 2, -60, 0)]
        [InlineData(1, -60, 0, -60)]
        [InlineData(40, 40, 40, 40)]
        public void ScaleSnapping_UsesExistingShiftAngleRules(int dx, int dy, int expectedX, int expectedY)
        {
            var start = new Point(100, 100);
            var pointer = new Point(start.X + dx, start.Y + dy);
            Assert.Equal(pointer, Surface.GetScaleEndpoint(start, pointer, Keys.None));
            Assert.Equal(new Point(start.X + expectedX, start.Y + expectedY), Surface.GetScaleEndpoint(start, pointer, Keys.Shift));
            // Ctrl must not relocate the first of the two explicitly picked points.
            Assert.Equal(pointer, Surface.GetScaleEndpoint(start, pointer, Keys.Control));
        }
    }
}
