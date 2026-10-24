using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Controls;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.FileFormatHandlers;
using Greenshot.Editor.Measurements;
using Greenshot.Editor.Forms;
using Xunit;

namespace Greenshot.Tests.Editor
{
    [Collection(TestCollections.Clipboard)]
    public class MeasurementTests
    {
        public MeasurementTests() => TestEnvironment.EnsureInitialized();

        [Fact]
        public void Dimensions_UseSignedGeometryWithoutStrokeOrShadow()
        {
            using var surface = new Surface();
            var line = new LineContainer(surface) { Width = -3, Height = 4 };
            Assert.Equal(("L1", 5d), Assert.Single(MeasurementCalculator.GetDimensions(line)));
            var rectangle = new RectangleContainer(surface) { Width = -30, Height = -40 };
            Assert.Equal(new[] { ("L1", 30d), ("L2", 40d) }, MeasurementCalculator.GetDimensions(rectangle));
            var ellipse = new EllipseContainer(surface) { Width = 25, Height = 25 };
            Assert.Equal(new[] { ("D1", 25d), ("D2", 25d) }, MeasurementCalculator.GetDimensions(ellipse));
            Assert.Equal(("L1", 5d), Assert.Single(MeasurementCalculator.GetDimensions(new ArrowContainer(surface) { Width = 3, Height = 4 })));
            Assert.Empty(MeasurementCalculator.GetDimensions(null));
            Assert.True(MeasurementCalculator.LineLength(int.MaxValue, int.MaxValue) > int.MaxValue);
        }

        [Theory]
        [InlineData(MeasurementUnit.Millimetres, 304.8)]
        [InlineData(MeasurementUnit.Centimetres, 30.48)]
        [InlineData(MeasurementUnit.Metres, 0.3048)]
        [InlineData(MeasurementUnit.Inches, 12)]
        [InlineData(MeasurementUnit.Feet, 1)]
        [InlineData(MeasurementUnit.Pixels, 100)]
        public void Calibration_ConvertsUnits(MeasurementUnit unit, double expected)
        {
            var state = MeasurementState.Calibrate(100, 1, MeasurementUnit.Feet);
            Assert.Equal(expected, state.WithUnit(unit).Convert(100), 8);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        public void Calibration_RejectsInvalidDistances(double distance)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MeasurementState.Calibrate(100, distance, MeasurementUnit.Metres));
            Assert.Throws<ArgumentOutOfRangeException>(() => MeasurementState.Calibrate(distance, 10, MeasurementUnit.Metres));
        }

        [Fact]
        public void Calibration_IsPerDocumentAndUndoable()
        {
            using var first = new Surface(new Bitmap(100, 100));
            using var second = new Surface();
            var state = MeasurementState.Calibrate(50, 10, MeasurementUnit.Centimetres);
            first.SetMeasurements(state);
            Assert.False(second.Measurements.IsCalibrated);
            Assert.True(first.Modified);
            first.Undo();
            Assert.False(first.Measurements.IsCalibrated);
            first.Redo();
            Assert.Same(state, first.Measurements);
            using var clone = (Surface)first.Clone();
            Assert.Equal(state.MetresPerPixel, clone.Measurements.MetresPerPixel);
            first.SetMeasurements(state.WithUnit(MeasurementUnit.Pixels));
            Assert.True(first.Measurements.IsCalibrated);
            first.SetMeasurements(MeasurementState.Pixels);
            first.Undo();
            Assert.True(first.Measurements.IsCalibrated);
        }

        [Fact]
        public void Transform_PreservesPhysicalLengthAndRejectsAnisotropy()
        {
            var state = MeasurementState.Calibrate(100, 10, MeasurementUnit.Millimetres);
            using var matrix = new Matrix();
            matrix.Translate(20, 40);
            matrix.Rotate(90);
            Assert.Equal(state.MetresPerPixel, state.Transform(matrix).MetresPerPixel, 10);
            matrix.Scale(2, 2);
            Assert.Equal(10, state.Transform(matrix).Convert(200), 5);
            matrix.Scale(2, 1);
            Assert.False(state.Transform(matrix).IsCalibrated);
        }

        [Fact]
        public void GeometryChanges_NotifyForAdornerKeyboardAndUndoPaths()
        {
            using var surface = new Surface();
            var shape = new RectangleContainer(surface) { Width = 20, Height = 30 };
            surface.AddElement(shape);
            surface.SelectElement(shape);
            int notifications = 0;
            shape.GeometryChanged += (s, e) => notifications++;
            surface.ProcessCmdKey(Keys.Control | Keys.Right);
            Assert.True(notifications > 0);
            int width = shape.Width;
            surface.Undo();
            Assert.NotEqual(width, shape.Width);
            Assert.Equal(20, MeasurementCalculator.GetDimensions(shape)[0].Pixels);
            Assert.True(notifications > 1);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Project_RoundTripsLegacyAndCalibratedFiles(bool calibrated)
        {
            using var surface = new Surface(new Bitmap(80, 60));
            surface.AddElement(new RectangleContainer(surface) { Left = 2, Top = 3, Width = 30, Height = 20 });
            if (calibrated) surface.SetMeasurements(MeasurementState.Calibrate(50, 10, MeasurementUnit.Centimetres));
            var handler = new GreenshotFileFormatHandler();
            using var stream = new MemoryStream();
            Assert.True(handler.TrySaveToStream((Bitmap)surface.Image, stream, ".greenshot", surface));
            byte[] bytes = stream.ToArray();
            // A legacy reader still locates and reads the original serialized element list.
            using (var legacy = new MemoryStream(bytes))
            {
                legacy.Position = legacy.Length - 22;
                using var reader = new BinaryReader(legacy, Encoding.UTF8, true);
                long payload = reader.ReadInt64();
                legacy.Position = legacy.Length - 22 - payload;
                using var oldSurface = new Surface();
                oldSurface.LoadElementsFromStream(legacy);
                Assert.Equal(30, Assert.Single(oldSurface.Elements).Width);
            }
            using var input = new MemoryStream(bytes);
            using var loaded = (Surface)handler.LoadSurface(input);
            Assert.Equal(80, loaded.Image.Width);
            Assert.Equal(calibrated, loaded.Measurements.IsCalibrated);
            Assert.Equal(surface.Measurements.MetresPerPixel, loaded.Measurements.MetresPerPixel);
            Assert.Equal(30, Assert.Single(loaded.Elements).Width);
        }

        [Fact]
        public void MalformedMetadata_FallsBackToPixelsAndRestoresStreamPosition()
        {
            using var stream = new MemoryStream();
            MeasurementMetadata.Write(stream, MeasurementState.Calibrate(100, 1, MeasurementUnit.Metres));
            stream.Position = 12;
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            writer.Write(double.NaN);
            stream.Position = 3;
            Assert.False(MeasurementMetadata.Read(stream, 0, stream.Length).IsCalibrated);
            Assert.Equal(3, stream.Position);
            Assert.False(MeasurementMetadata.Read(stream, 0, 10).IsCalibrated);
        }

        [Fact]
        public void ScalePreview_DoesNotBecomeAnObjectAndEscapeCancels()
        {
            RunOnSta(() =>
            {
            using var surface = new Surface(new Bitmap(100, 100));
            surface.DrawingMode = DrawingModes.Scale;
            double measured = 0;
            surface.ScaleLineCompleted += (s, e) => measured = e.PixelLength;
            Mouse(surface, "SurfaceMouseDown", 10, 10);
            Mouse(surface, "SurfaceMouseUp", 10, 10);
            Assert.Equal(0, measured);
            Mouse(surface, "SurfaceMouseMove", 40, 50);
            Assert.Empty(surface.Elements);
            using (var exported = surface.GetImageForExport()) Assert.Equal(100, exported.Width);
            Mouse(surface, "SurfaceMouseDown", 40, 50);
            Mouse(surface, "SurfaceMouseUp", 40, 50);
            Assert.Equal(50, measured);
            Assert.Empty(surface.Elements);
            surface.DrawingMode = DrawingModes.Scale;
            Mouse(surface, "SurfaceMouseDown", 0, 0);
            Assert.True(surface.ProcessCmdKey(Keys.Escape));
            Assert.Equal(DrawingModes.None, surface.DrawingMode);
            Assert.False(surface.Measurements.IsCalibrated);
            });
        }

        private static void RunOnSta(Action action)
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException, true);
                    action();
                }
                catch (Exception ex) { failure = ex; }
            }) { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "UI test timed out");
            if (failure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }

        private static void Mouse(Surface surface, string method, int x, int y) =>
            typeof(Surface).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(surface, new object[] { surface, new MouseEventArgs(MouseButtons.Left, 1, x, y, 0) });

        [Fact]
        public void PropertyGrid_CommitsScaledEditsAndProtectsTypingFromCanvasShortcuts()
        {
            RunOnSta(() =>
            {
                using var surface = new Surface(new Bitmap(640, 400));
                using var editor = new ImageEditorForm(surface, true, false);
                editor.Size = new Size(1050, 700);
                var panel = (MeasurementPanel)typeof(ImageEditorForm).GetField("_measurementPanel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(editor);
                panel.SetExpanded(true);
                editor.Show();
                Application.DoEvents();
                var shape = new ArrowContainer(surface) { Left = 100, Top = 100, Width = 30, Height = 40 };
                surface.AddElement(shape);
                surface.SelectElement(shape);
                surface.SetMeasurements(MeasurementState.Calibrate(100, 10, MeasurementUnit.Millimetres));
                var grid = panel.Controls.OfType<FlowLayoutPanel>().Single().Controls.OfType<DataGridView>().Single();
                Assert.Equal(5, grid.Rows.Count);
                grid.Focus();
                grid.CurrentCell = grid.Rows[0].Cells[1];
                Assert.True(grid.BeginEdit(true));
                Assert.True((bool)typeof(ImageEditorForm).GetProperty("IsTyping", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(editor));
                Assert.False((bool)typeof(ImageEditorForm).GetProperty("CanvasHasKeys", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(editor));
                ((TextBox)grid.EditingControl).Text = "10";
                Assert.True(grid.EndEdit());
                Assert.Equal(60, shape.Width); Assert.Equal(80, shape.Height);
                Assert.Equal("10", grid.Rows[0].Cells[1].Value);
                surface.Undo();
                Assert.Equal(30, shape.Width); Assert.Equal(40, shape.Height);
                Assert.Equal("5", grid.Rows[0].Cells[1].Value);
                grid.CurrentCell = grid.Rows[0].Cells[1];
                Assert.True(grid.BeginEdit(true));
                ((TextBox)grid.EditingControl).Text = "-1";
                grid.EndEdit();
                Assert.Equal(30, shape.Width);
                grid.CancelEdit();
                Assert.Equal("5", grid.Rows[0].Cells[1].Value);

                string preview = Environment.GetEnvironmentVariable("GREENSHOTPLUS_PREVIEW");
                if (!string.IsNullOrEmpty(preview))
                {
                    editor.Update();
                    using var bitmap = new Bitmap(editor.Width, editor.Height);
                    editor.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                    bitmap.Save(preview, System.Drawing.Imaging.ImageFormat.Png);
                }
            });
        }

        [Fact]
        public void ResizeImage_UpdatesCalibrationAndUndoRestoresIt()
        {
            using var surface = new Surface(new Bitmap(100, 100));
            surface.SetMeasurements(MeasurementState.Calibrate(100, 10, MeasurementUnit.Millimetres));
            var apply = typeof(Surface).GetMethod("ApplyEffectResult", BindingFlags.NonPublic | BindingFlags.Instance,
                null, new[] { typeof(Image), typeof(Matrix) }, null);
            var uniform = new Matrix();
            uniform.Scale(2, 2);
            apply.Invoke(surface, new object[] { new Bitmap(200, 200), uniform });
            Assert.Equal(10, surface.Measurements.Convert(200), 8);
            var nonuniform = new Matrix();
            nonuniform.Scale(2, 1);
            apply.Invoke(surface, new object[] { new Bitmap(400, 200), nonuniform });
            Assert.False(surface.Measurements.IsCalibrated);
            surface.Undo();
            Assert.Equal(10, surface.Measurements.Convert(200), 8);
            surface.Redo();
            Assert.False(surface.Measurements.IsCalibrated);
        }

        [Fact]
        public void EditorPanel_UpdatesAndClearsWithoutReplacingCanvas()
        {
            RunOnSta(() =>
            {
                using var surface = new Surface(new Bitmap(640, 400));
                using var editor = new ImageEditorForm(surface, true, false);
                editor.Size = new Size(1000, 650);
                var field = typeof(ImageEditorForm).GetField("_measurementPanel", BindingFlags.NonPublic | BindingFlags.Instance);
                var panel = (MeasurementPanel)field.GetValue(editor);
                Assert.True(panel.Width < 60);
                panel.SetExpanded(true);
                editor.PerformLayout();
                Assert.True(panel.Width >= 185);
                var shape = new RectangleContainer(surface) { Width = 30, Height = 40 };
                surface.AddElement(shape);
                surface.SelectElement(shape);
                var grid = panel.Controls.OfType<FlowLayoutPanel>().Single().Controls.OfType<DataGridView>().Single();
                Assert.Equal("30", grid.Rows[0].Cells[1].Value);
                Assert.Equal(10, grid.Rows.Count);
                shape.Width = 55;
                Assert.Equal("55", grid.Rows[0].Cells[1].Value);
                surface.SetMeasurements(MeasurementState.Calibrate(100, 10, MeasurementUnit.Millimetres));
                Assert.Contains("mm", grid.Columns[1].HeaderText);
                Assert.Equal(5.5d.ToString(System.Globalization.CultureInfo.CurrentCulture), grid.Rows[0].Cells[1].Value);
                var second = new EllipseContainer(surface) { Width = 10, Height = 20 };
                surface.AddElement(second);
                surface.SelectElement(second);
                Assert.Empty(grid.Rows.Cast<DataGridViewRow>());
                surface.DeselectAllElements();
                Assert.Empty(grid.Rows.Cast<DataGridViewRow>());
                // Optional local visual review, requested explicitly by the test runner.
                string preview = Environment.GetEnvironmentVariable("GREENSHOTPLUS_PREVIEW");
                if (!string.IsNullOrEmpty(preview))
                {
                    editor.Show();
                    Application.DoEvents();
                    surface.SelectElement(shape);
                    editor.Update();
                    using var bitmap = new Bitmap(editor.Width, editor.Height);
                    editor.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                    bitmap.Save(preview, System.Drawing.Imaging.ImageFormat.Png);
                }
            });
        }

        [Fact]
        public void Palette_UsesExistingFieldsAndDeduplicatesRecents()
        {
            var config = new EditorConfigurationImpl();
            using var toolbar = new ToolStrip();
            using var palette = new QuickColorPalette(toolbar, config);
            using var surface = new Surface();
            var rectangle = new RectangleContainer(surface) { Width = 10, Height = 10 };
            surface.AddElement(rectangle);
            surface.SelectElement(rectangle);
            palette.ColorSelected += color => surface.FieldAggregator.GetField(palette.Target).Value = color;
            palette.Refresh(true, true);
            var swatch = toolbar.Items.OfType<ToolStripButton>().First(b => b.Tag is Color c && c == Color.Blue);
            var before = rectangle.GetFieldValueAsColor(FieldType.LINE_COLOR);
            swatch.PerformClick();
            Assert.Equal(Color.Blue, rectangle.GetFieldValueAsColor(FieldType.LINE_COLOR));
            surface.Undo();
            Assert.Equal(before, rectangle.GetFieldValueAsColor(FieldType.LINE_COLOR));
            palette.Remember(Color.Red);
            palette.Remember(Color.Blue);
            palette.Remember(Color.FromArgb(Color.Red.ToArgb()));
            Assert.Equal(2, config.RecentColors.Count);
            Assert.Equal(Color.Red.ToArgb(), config.RecentColors[0].ToArgb());
        }
    }
}
