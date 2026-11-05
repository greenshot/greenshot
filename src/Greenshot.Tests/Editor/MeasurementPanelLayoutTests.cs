using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Greenshot.Editor.Controls;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Forms;
using Xunit;

namespace Greenshot.Tests.Editor
{
    [Collection(TestCollections.Clipboard)]
    public class MeasurementPanelLayoutTests
    {
        public MeasurementPanelLayoutTests() => TestEnvironment.EnsureInitialized();

        [Fact]
        public void Panel_SelectionAndWidthLayout()
        {
            DimensionFormatTests.Sta(() =>
            {
                using var surface = new Surface(new Bitmap(700, 500));
                using var editor = new ImageEditorForm(surface, true, false) { Size = new Size(1100, 850) };
                var panel = Field<MeasurementPanel>(editor, "_measurementPanel");
                panel.SetExpanded(true); editor.Show(); Application.DoEvents();
                Capture(panel, "after-unselected-default");
                var rectangle = new RectangleContainer(surface) { Left = 100, Top = 100, Width = 200, Height = 100 };
                surface.AddElement(rectangle); surface.SelectElement(rectangle); Application.DoEvents();
                Capture(panel, "after-rectangle-default");
                var properties = Field<DataGridView>(panel, "_properties");
                var edges = Field<DataGridView>(panel, "_dimensionEdges");
                Assert.True(properties.AllowUserToResizeColumns && properties.AllowUserToResizeRows);
                Assert.True(edges.AllowUserToResizeColumns && edges.AllowUserToResizeRows);
                properties.Rows[0].Height = 40; edges.Rows[0].Height = 40;
                panel.RefreshDimensionLabels(surface); Assert.Equal(40, edges.Rows[0].Height);
                int narrowGridWidth = 0;
                foreach (int width in new[] { 280, 520 })
                {
                    panel.SetPanelWidth(width); Application.DoEvents();
                    Assert.Equal(width, panel.Width);
                    AssertResponsive(panel);
                    Assert.True(properties.Width > panel.Width - 75);
                    Assert.True(edges.Width > panel.Width - 75);
                    Assert.DoesNotContain(properties.Controls.OfType<HScrollBar>(), scrollbar => scrollbar.Visible);
                    Assert.DoesNotContain(edges.Controls.OfType<HScrollBar>(), scrollbar => scrollbar.Visible);
                    Assert.All(properties.Rows.Cast<DataGridViewRow>(), row => Assert.True(row.Height >= 25));
                    if (width == 280) narrowGridWidth = properties.Width;
                    else Assert.True(properties.Width > narrowGridWidth + 200);
                    Capture(panel, "after-rectangle-" + width);
                    surface.DeselectAllElements(); Application.DoEvents();
                    Assert.False(Field<Control>(panel, "_geometryCategory").Visible);
                    Assert.False(Field<Control>(panel, "_edgesCategory").Visible);
                    Assert.False(Field<Control>(panel, "_edgePrecisionRow").Visible);
                    AssertResponsive(panel); Capture(panel, "after-unselected-" + width);
                    surface.SelectElement(rectangle); Application.DoEvents();
                }
                int remembered = panel.Width;
                panel.SetExpanded(false); panel.SetExpanded(true); Application.DoEvents(); Assert.Equal(remembered, panel.Width);
                var grip = Field<Panel>(panel, "_resizeGrip");
                panel.SetPanelWidth(340);
                Mouse(grip, "OnMouseDown", 3, MouseButtons.Left);
                Mouse(grip, "OnMouseMove", -77, MouseButtons.Left);
                Mouse(grip, "OnMouseUp", 3, MouseButtons.Left);
                Assert.Equal(420, panel.Width); Assert.False(grip.Capture);
                panel.SetPanelWidth(0); Assert.Equal(280, panel.Width);
                panel.SetPanelWidth(int.MaxValue); Assert.True(panel.Width <= 640);
                editor.Size = new Size(700, 650); Application.DoEvents();
                Assert.True(panel.Width <= panel.Parent.ClientSize.Width - 220);
                AssertResponsive(panel);
                var polygon = new PolygonContainer(surface);
                polygon.SetVertices(new[] { new Point(50, 50), new Point(220, 80), new Point(190, 230), new Point(70, 190) });
                surface.AddElement(polygon); surface.DeselectAllElements(); surface.SelectElement(polygon); Application.DoEvents();
                Assert.Equal(4, properties.Rows.Count); Assert.Equal(4, edges.Rows.Count);
                AssertResponsive(panel); Capture(panel, "after-polygon-small-window");
                var geometry = Field<Control>(panel, "_geometryCategory");
                var header = (Button)geometry.Controls[0];
                header.PerformClick(); Application.DoEvents();
                Assert.False(geometry.Controls[1].Visible);
                int collapsedHeight = geometry.Height;
                panel.SetPanelWidth(300); Application.DoEvents();
                Assert.False(geometry.Controls[1].Visible);
                header.PerformClick(); Application.DoEvents(); Assert.True(geometry.Height > collapsedHeight);
                AssertResponsive(panel); Capture(panel, "after-polygon-expanded");
                editor.Size = new Size(400, 650); Application.DoEvents();
                Assert.False(panel.IsExpanded); Assert.True(panel.Width < 60);
                editor.Size = new Size(1100, 850); Application.DoEvents();
                panel.SetExpanded(true); Application.DoEvents(); Assert.Equal(300, panel.Width);
            });
        }

        private static void Mouse(Control control, string method, int x, MouseButtons button) =>
            typeof(Control).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(control, new object[] { new MouseEventArgs(button, 1, x, 15, 0) });

        private static void AssertResponsive(MeasurementPanel panel)
        {
            var body = Field<Panel>(panel, "_body");
            Assert.False(body.HorizontalScroll.Visible);
            void Check(Control parent)
            {
                var visible = parent.Controls.Cast<Control>().Where(c => c.Visible).ToArray();
                if (parent is TableLayoutPanel)
                {
                    for (int i = 0; i < visible.Length; i++)
                    for (int j = i + 1; j < visible.Length; j++)
                        Assert.False(visible[i].Bounds.IntersectsWith(visible[j].Bounds), visible[i].Text + " overlaps " + visible[j].Text);
                    foreach (var child in visible)
                        Assert.True(child.Right <= parent.ClientSize.Width, child.Text + " clips at the right edge");
                }
                foreach (var button in visible.OfType<Button>())
                {
                    Assert.InRange(button.Height, 23, 45);
                    int textWidth = TextRenderer.MeasureText(button.Text, button.Font, Size.Empty,
                        TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;
                    Assert.True(button.ClientSize.Width >= textWidth + 8, button.Text + " is clipped");
                }
                foreach (var child in visible) Check(child);
            }
            Check(panel);
        }

        private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
        private static void Capture(Control control, string name)
        {
            string folder = Path.Combine(Path.GetTempPath(), "GreenshotPlus-measurement-layout"); Directory.CreateDirectory(folder);
            using var image = new Bitmap(control.Width, control.Height);
            control.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
            image.Save(Path.Combine(folder, name + ".png"), ImageFormat.Png);
        }
    }
}
