using System;
using Dapplo.Windows.Common.Extensions;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Greenshot.Editor.Controls;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.FileFormatHandlers;
using Greenshot.Editor.Measurements;
using Xunit;

namespace Greenshot.Tests.Editor
{
    [Collection(TestCollections.Clipboard)]
    public class DimensionFormatTests
    {
        public DimensionFormatTests() => TestEnvironment.EnsureInitialized();
        [Theory]
        [InlineData(1.2, DimensionFormatMode.DecimalPlaces, 3, "1.200")]
        [InlineData(1.7, DimensionFormatMode.DecimalPlaces, 0, "2")]
        [InlineData(0.00000001, DimensionFormatMode.DecimalPlaces, 10, "0.0000000100")]
        [InlineData(0.00000000001, DimensionFormatMode.DecimalPlaces, 4, "0.0000")]
        [InlineData(1.2, DimensionFormatMode.SignificantFigures, 4, "1.200")]
        [InlineData(9.999, DimensionFormatMode.SignificantFigures, 3, "10.0")]
        [InlineData(99.99, DimensionFormatMode.SignificantFigures, 3, "100")]
        [InlineData(0.009999, DimensionFormatMode.SignificantFigures, 3, "0.0100")]
        [InlineData(0, DimensionFormatMode.SignificantFigures, 3, "0.00")]
        [InlineData(-1.2, DimensionFormatMode.SignificantFigures, 3, "-1.20")]
        [InlineData(1e-100, DimensionFormatMode.SignificantFigures, 3, "1.00E-100")]
        public void ExplicitPrecision_RoundsAndRetainsZeros(double value, DimensionFormatMode mode, int digits, string expected)
        {
            var previous = CultureInfo.CurrentCulture;
            try { CultureInfo.CurrentCulture = CultureInfo.InvariantCulture; Assert.Equal(expected, new DimensionFormat(mode, digits).Format(value)); }
            finally { CultureInfo.CurrentCulture = previous; }
        }
        [Fact]
        public void Formatting_IsCultureAwareBoundedAndValidatesCounts()
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                Assert.Equal("1,200", new DimensionFormat(DimensionFormatMode.DecimalPlaces, 3).Format(1.2));
                Assert.Equal("0,0100", new DimensionFormat(DimensionFormatMode.SignificantFigures, 3).Format(.009999));
                foreach (double value in new[] { double.Epsilon, double.MaxValue, -double.MaxValue, double.NaN, double.PositiveInfinity })
                foreach (var mode in new[] { DimensionFormatMode.Automatic, DimensionFormatMode.DecimalPlaces, DimensionFormatMode.SignificantFigures })
                    Assert.InRange(new DimensionFormat(mode, mode == DimensionFormatMode.Automatic ? 0 : 10).Format(value).Length, 1, 40);
                Assert.Equal("Out of range", DimensionFormat.Automatic.Format(double.NaN));
                Assert.Throws<ArgumentOutOfRangeException>(() => new DimensionFormat(DimensionFormatMode.DecimalPlaces, 11));
                Assert.Throws<ArgumentOutOfRangeException>(() => new DimensionFormat(DimensionFormatMode.SignificantFigures, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => new DimensionFormat((DimensionFormatMode)99, 0));
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }
        private static LineContainer Line(Surface surface, int y = 100)
        {
            var line = new LineContainer(surface) { Left = 80, Top = y, Width = 100, Height = 0 };
            line.SetFieldValue(FieldType.SHADOW, false); surface.AddElement(line, false); line.SetDimensionVisible(0, true); return line;
        }
        [Fact]
        public void DefaultsOverridesResetAndApplyAll_AreIndependentAndUndoTogetherIncludingHiddenEdges()
        {
            using var surface = new Surface(new Bitmap(500, 400)); var first = Line(surface); var hidden = Line(surface, 200);
            var decimals = new DimensionFormat(DimensionFormatMode.DecimalPlaces, 2);
            surface.SetDimensionStyle(surface.DimensionStyle.WithFormat(decimals));
            Assert.Equal("100.00 px", Assert.Single(DimensionLayout.Labels(first)).Text);
            first.SetDimensionFormat(0, new DimensionFormat(DimensionFormatMode.SignificantFigures, 4));
            Assert.Equal("100.0 px", Assert.Single(DimensionLayout.Labels(first)).Text);
            first.SetDimensionFormat(0, null); Assert.Equal("100.00 px", Assert.Single(DimensionLayout.Labels(first)).Text);
            surface.Undo(); Assert.Equal("100.0 px", Assert.Single(DimensionLayout.Labels(first)).Text);
            hidden.SetDimensionFormat(0, DimensionFormat.Automatic); hidden.SetDimensionVisible(0, false);
            var previous = surface.DimensionStyle;
            Assert.True(surface.ApplyDimensionFormatToAll(DimensionFormat.Automatic));
            Assert.Null(first.GetDimensionFormat(0)); Assert.Null(hidden.GetDimensionFormat(0));
            Assert.Equal("100 px", Assert.Single(DimensionLayout.Labels(first)).Text);
            surface.Undo(); Assert.Equal(previous, surface.DimensionStyle);
            Assert.Equal(4, first.GetDimensionFormat(0).Digits); Assert.Equal(DimensionFormat.Automatic, hidden.GetDimensionFormat(0));
            surface.Redo(); Assert.Null(hidden.GetDimensionFormat(0)); Assert.Null(first.GetDimensionFormat(0));
            Assert.Equal(100, first.Width); Assert.Equal(DimensionFormat.Automatic, surface.DimensionStyle.Format);
        }
        [Theory]
        [InlineData("roundtrip")]
        [InlineData("legacy")]
        [InlineData("marker")]
        [InlineData("version")]
        [InlineData("format")]
        [InlineData("truncated")]
        public void MetadataExtension_PreservesBaseStyleAndCalibration(string kind)
        {
            var format = new DimensionFormat(DimensionFormatMode.DecimalPlaces, 3);
            var style = new DimensionStyle("Arial", 13, FontStyle.Bold, Color.Navy, format, Color.LightYellow);
            using var stream = new MemoryStream();
            if (kind == "legacy")
            {
                using var body = new MemoryStream(); using var data = new BinaryWriter(body, Encoding.UTF8, true);
                data.Write(1); var family = Encoding.UTF8.GetBytes(style.Family); data.Write((ushort)family.Length); data.Write(family);
                data.Write(style.Size); data.Write((int)style.FontStyle); data.Write(style.Color.ToArgb());
                body.WriteTo(stream); using var footer = new BinaryWriter(stream, Encoding.UTF8, true); footer.Write((int)body.Length); footer.Write(Encoding.ASCII.GetBytes("GSPDIM01"));
            }
            else
            {
                DimensionMetadata.Write(stream, style);
                long extension = 4 + 2 + Encoding.UTF8.GetByteCount(style.Family) + 12;
                if (kind == "marker") { stream.Position = extension; stream.WriteByte(0); }
                if (kind == "version" || kind == "format")
                { stream.Position = extension + (kind == "version" ? 8 : 12); new BinaryWriter(stream, Encoding.UTF8, true).Write(int.MaxValue); }
                if (kind == "truncated")
                {
                    // Remove the extension's last bytes but retain the valid version-1 base and footer.
                    stream.SetLength(extension + 3); stream.Position = stream.Length;
                    using var footer = new BinaryWriter(stream, Encoding.UTF8, true); footer.Write((int)extension + 3); footer.Write(Encoding.ASCII.GetBytes("GSPDIM01"));
                }
            }
            stream.Position = stream.Length; var calibration = MeasurementState.Calibrate(100, 2, MeasurementUnit.Metres);
            MeasurementMetadata.Write(stream, calibration, true); stream.Position = 2;
            var restored = DimensionMetadata.Read(stream, 0, stream.Length);
            Assert.Equal(2, stream.Position); Assert.Equal(style.Family, restored.Family); Assert.Equal(style.Size, restored.Size); Assert.Equal(style.Color.ToArgb(), restored.Color.ToArgb());
            Assert.Equal(kind == "roundtrip" ? format : DimensionFormat.Automatic, restored.Format);
            Assert.Equal(kind == "roundtrip" ? Color.LightYellow.ToArgb() : Color.Transparent.ToArgb(), restored.BackgroundColor.ToArgb());
            Assert.Equal(calibration.MetresPerPixel, MeasurementMetadata.Read(stream, 0, stream.Length).MetresPerPixel);
        }
        [Fact]
        public void ProjectClipboardDuplicate_PreserveOverridesAndUseDestinationDefaultAndUnits()
        {
            using var source = new Surface(new Bitmap(400, 300)); var first = Line(source); var second = Line(source, 180);
            first.SetDimensionFormat(0, new DimensionFormat(DimensionFormatMode.DecimalPlaces, 3));
            source.SetDimensionStyle(new DimensionStyle("Arial", 12, FontStyle.Bold, Color.Navy, new DimensionFormat(DimensionFormatMode.SignificantFigures, 4), Color.LightYellow));
            using var bytes = new MemoryStream(); var handler = new GreenshotFileFormatHandler();
            Assert.True(handler.TrySaveToStream((Bitmap)source.Image, bytes, ".greenshot", source)); bytes.Position = 0;
            using var loaded = (Surface)handler.LoadSurface(bytes);
            Assert.Equal(source.DimensionStyle, loaded.DimensionStyle); Assert.Equal(3, ((DrawableContainer)loaded.Elements[0]).GetDimensionFormat(0).Digits);
            using var target = new Surface(new Bitmap(400, 300));
            target.SetMeasurements(MeasurementState.Calibrate(100, 2, MeasurementUnit.Metres));
            target.SetDimensionStyle(target.DimensionStyle.WithFormat(new DimensionFormat(DimensionFormatMode.DecimalPlaces, 1)));
            var copies = DrawableContainerClipboard.Deserialize(DrawableContainerClipboard.Serialize(new DrawableContainerList { first, second }));
            target.AddElements(copies);
            Assert.Equal("2.000 m", Assert.Single(DimensionLayout.Labels((DrawableContainer)copies[0])).Text);
            Assert.Equal("2.0 m", Assert.Single(DimensionLayout.Labels((DrawableContainer)copies[1])).Text);
            target.DeselectAllElements(); target.SelectElement(copies[0]); target.DuplicateSelectedElements();
            var duplicate = Assert.IsType<LineContainer>(target.Elements.Last()); Assert.Equal(3, duplicate.GetDimensionFormat(0).Digits);
            duplicate.SetDimensionFormat(0, null); Assert.Equal(3, ((DrawableContainer)copies[0]).GetDimensionFormat(0).Digits);
        }
        [Fact]
        public void Background_RotatesWithLabelStaysInsideBoundsAndSupportsUndo()
        {
            using var surface = new Surface(new Bitmap(500, 400)); var line = Line(surface);
            surface.SelectElement(line); surface.RotateSelectedObjects(37);
            surface.SetDimensionStyle(surface.DimensionStyle.WithBackground(Color.Magenta));
            var label = Assert.Single(DimensionLayout.Labels(line));
            using var rendered = (Bitmap)surface.GetImageForExport(); int fill = 0;
            for (int y = 0; y < rendered.Height; y++) for (int x = 0; x < rendered.Width; x++)
                if (rendered.GetPixel(x,y).ToArgb() == Color.Magenta.ToArgb()) { fill++; Assert.True(label.Bounds.Contains(x,y)); Assert.True(line.DrawingBounds.Contains(x,y)); }
            Assert.True(fill > 50); surface.Undo(); Assert.Equal(0, surface.DimensionStyle.BackgroundColor.A);
            surface.Redo(); Assert.Equal(Color.Magenta, surface.DimensionStyle.BackgroundColor);
        }
        [Fact]
        public void BackgroundOverrides_AreIndependentUndoableAndPersistWithRememberedColour()
        {
            using var surface = new Surface(new Bitmap(500, 400)); var first = Line(surface); var second = Line(surface, 200);
            var transparent = surface.DimensionStyle.WithBackground(Color.LightYellow).WithBackgroundTransparency(true);
            surface.SetDimensionStyle(transparent);
            Assert.Equal(Color.LightYellow.ToArgb(), transparent.WithBackgroundTransparency(false).BackgroundColor.ToArgb());
            first.SetDimensionBackground(0, Color.Magenta);
            second.SetDimensionBackground(0, Color.Cyan);
            first.SetDimensionBackground(0, Color.FromArgb(0, first.GetDimensionBackground(0).Value));
            surface.Undo(); Assert.Equal(Color.Magenta.ToArgb(), first.GetDimensionBackground(0).Value.ToArgb());
            surface.Redo(); Assert.Equal(0, first.GetDimensionBackground(0).Value.A);
            Assert.Equal(Color.Magenta.ToArgb(), Color.FromArgb(255, first.GetDimensionBackground(0).Value).ToArgb());
            Assert.Equal(Color.Cyan.ToArgb(), second.GetDimensionBackground(0).Value.ToArgb());
            using (var rendered = (Bitmap)surface.GetImageForExport())
            {
                var label = Assert.Single(DimensionLayout.Labels(second)); int cyan = 0;
                for (int y = label.Bounds.Top; y < label.Bounds.Bottom; y++)
                for (int x = label.Bounds.Left; x < label.Bounds.Right; x++)
                    if (rendered.GetPixel(x, y).ToArgb() == Color.Cyan.ToArgb()) cyan++;
                Assert.True(cyan > 50);
            }
            var rectangle = new RectangleContainer(surface) { Left = 250, Top = 150, Width = 100, Height = 100 };
            surface.AddElement(rectangle, false); rectangle.SetDimensionBackground(0, Color.Red); rectangle.SetDimensionBackground(1, Color.Blue);
            Assert.Equal(Color.Red.ToArgb(), rectangle.GetDimensionBackground(0).Value.ToArgb());
            Assert.Equal(Color.Blue.ToArgb(), rectangle.GetDimensionBackground(1).Value.ToArgb());
            using var bytes = new MemoryStream(); var handler = new GreenshotFileFormatHandler();
            Assert.True(handler.TrySaveToStream((Bitmap)surface.Image, bytes, ".greenshot", surface)); bytes.Position = 0;
            using var loaded = (Surface)handler.LoadSurface(bytes);
            Assert.Equal(Color.LightYellow.ToArgb(), loaded.DimensionStyle.WithBackgroundTransparency(false).BackgroundColor.ToArgb());
            Assert.Equal(first.GetDimensionBackground(0).Value.ToArgb(), ((DrawableContainer)loaded.Elements[0]).GetDimensionBackground(0).Value.ToArgb());
            var copies = DrawableContainerClipboard.Deserialize(DrawableContainerClipboard.Serialize(new DrawableContainerList { first, rectangle }));
            Assert.Equal(first.GetDimensionBackground(0).Value.ToArgb(), ((DrawableContainer)copies[0]).GetDimensionBackground(0).Value.ToArgb());
            Assert.Equal(Color.Blue.ToArgb(), ((DrawableContainer)copies[1]).GetDimensionBackground(1).Value.ToArgb());
            ((DrawableContainer)copies[1]).SetDimensionBackground(1, Color.Green);
            Assert.Equal(Color.Blue.ToArgb(), rectangle.GetDimensionBackground(1).Value.ToArgb());
            first.SetDimensionBackground(0, null); Assert.Null(first.GetDimensionBackground(0));
            surface.Undo(); Assert.Equal(0, first.GetDimensionBackground(0).Value.A);
        }

        [Fact]
        public void Panel_PrecisionGridInheritanceBackgroundAndVisualSmoke()
        {
            Sta(() =>
            {
                using var surface = new Surface(new Bitmap(650, 500)); using (var g = Graphics.FromImage(surface.Image)) g.Clear(Color.WhiteSmoke);
                using var editor = new Greenshot.Editor.Forms.ImageEditorForm(surface, true, false); editor.Size = new Size(1100, 950);
                var line = Line(surface, 230); surface.SelectElement(line); surface.RotateSelectedObjects(31);
                var panel = (MeasurementPanel)typeof(Greenshot.Editor.Forms.ImageEditorForm).GetField("_measurementPanel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(editor);
                panel.SetExpanded(true); editor.Show(); Application.DoEvents();
                var mode = Field<ComboBox>(panel, "_documentFormat"); var digits = Field<NumericUpDown>(panel, "_documentDigits");
                mode.SelectedIndex = 1; digits.Value = 3;
                var edges = Field<DataGridView>(panel, "_dimensionEdges");
                Assert.Equal(Assert.Single(DimensionLayout.Labels(line)).Text, edges.Rows[0].Cells[2].Value);
                Field<ComboBox>(panel, "_edgeFormat").SelectedIndex = 3; Field<NumericUpDown>(panel, "_edgeDigits").Value = 5;
                Assert.Equal(5, line.GetDimensionFormat(0).Digits);
                Field<ComboBox>(panel, "_edgeFormat").SelectedIndex = 0; Assert.Null(line.GetDimensionFormat(0));
                Field<CheckBox>(panel, "_dimensionTransparent").Checked = false; Assert.Equal(Color.White.ToArgb(), line.GetDimensionBackground(0).Value.ToArgb());
                line.SetDimensionBackground(0, Color.LightYellow);
                Field<CheckBox>(panel, "_dimensionTransparent").Checked = true;
                Field<CheckBox>(panel, "_dimensionTransparent").Checked = false;
                Assert.Equal(Color.LightYellow.ToArgb(), line.GetDimensionBackground(0).Value.ToArgb());
                Field<CheckBox>(panel, "_inheritDimensionBackground").Checked = true; Assert.Null(line.GetDimensionBackground(0));
                surface.SetDimensionStyle(surface.DimensionStyle.WithBackground(Color.LightYellow));
                Assert.Equal(3, surface.DimensionStyle.Format.Digits);
                using var bitmap = new Bitmap(panel.Width, panel.Height); panel.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                bitmap.Save(Path.Combine(Path.GetTempPath(), "GreenshotPlus-v080-measurement-panel.png"), ImageFormat.Png);
                using var rendered = surface.GetImageForExport(); rendered.Save(Path.Combine(Path.GetTempPath(), "GreenshotPlus-v080-rotated-dimension.png"), ImageFormat.Png);
                var body = Field<FlowLayoutPanel>(panel, "_body");
                foreach (FlowLayoutPanel section in body.Controls)
                {
                    var header = Assert.IsType<Button>(section.Controls[0]); var content = section.Controls[1];
                    int height = section.Height; Assert.True(content.Visible);
                    header.PerformClick(); Application.DoEvents(); Assert.False(content.Visible); Assert.True(section.Height < height);
                    header.PerformClick(); Application.DoEvents(); Assert.True(content.Visible);
                }
            });
        }
        private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
        internal static void Sta(Action action)
        {
            Exception error = null; var thread = new Thread(() => { try { action(); } catch (Exception e) { error = e; } });
            thread.SetApartmentState(ApartmentState.STA); thread.Start(); Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "UI test timed out");
            if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
        }
    }
}
