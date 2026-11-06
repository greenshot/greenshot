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
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Forms;
using Xunit;
namespace Greenshot.Tests.Editor
{
    [Collection(TestCollections.Clipboard)]
    public class QuickColorTests
    {
        public QuickColorTests() => TestEnvironment.EnsureInitialized();

        [Fact]
        public void RecentsDeduplicateAndToolbarKeepsFourSwatches()
        {
            Exception error = null;
            var thread = new Thread(() =>
            {
                IEditorConfiguration config = null;
                System.Collections.Generic.List<Color> original = null;
                try
                {
                    config = Dapplo.Ini.IniConfigRegistry.GetSection<IEditorConfiguration>();
                    original = config.RecentColors;
                    config.RecentColors = null;
                    using var strip = new ToolStrip();
                    var type = typeof(ImageEditorForm).Assembly.GetType("Greenshot.Editor.Controls.QuickColorPalette");
                    using var palette = (IDisposable)Activator.CreateInstance(type, strip, config);
                    var remember = type.GetMethod("Remember");
                    remember.Invoke(palette, new object[] { Color.Red });
                    remember.Invoke(palette, new object[] { Color.Blue });
                    remember.Invoke(palette, new object[] { Color.Red });
                    Assert.Equal(2, config.RecentColors.Count);
                    Assert.Equal(Color.Red.ToArgb(), config.RecentColors[0].ToArgb());
                    Assert.Equal(4, strip.Items.OfType<ToolStripButton>().Count());
                    Assert.Single(strip.Items.OfType<ToolStripDropDownButton>());
                }
                catch (Exception e) { error = e; }
                finally { if (config != null) config.RecentColors = original; }
            });
            thread.SetApartmentState(ApartmentState.STA); thread.Start(); thread.Join();
            if (error != null) throw error;
        }
        [Fact]
        public void ExistingShortcutsAndMouseColoursKeepUndoAndProjectColours()
        {
            Exception error = null;
            var thread = new Thread(() =>
            {
                try
                {
                    using var surface = new Greenshot.Editor.Drawing.Surface(new Bitmap(300, 200));
                    var rectangle = new Greenshot.Editor.Drawing.RectangleContainer(surface) { Left = 30, Top = 30, Width = 100, Height = 100 };
                    rectangle.SetFieldValue(Greenshot.Editor.Drawing.Fields.FieldType.LINE_COLOR, Color.Black);
                    rectangle.SetFieldValue(Greenshot.Editor.Drawing.Fields.FieldType.FILL_COLOR, Color.White);
                    surface.AddElement(rectangle, false);
                    using var editor = new ImageEditorForm(surface, true, false);
                    surface.SelectElement(rectangle);
                    Assert.True(surface.ProcessCmdKey(Keys.Control | Keys.D1));
                    Assert.Equal(Color.Red.ToArgb(), rectangle.GetFieldValueAsColor(Greenshot.Editor.Drawing.Fields.FieldType.FILL_COLOR).ToArgb());
                    Assert.True(surface.ProcessCmdKey(Keys.Control | Keys.Shift | Keys.D3));
                    Assert.Equal(Color.Blue.ToArgb(), rectangle.GetFieldValueAsColor(Greenshot.Editor.Drawing.Fields.FieldType.LINE_COLOR).ToArgb());

                    var palette = typeof(ImageEditorForm).GetField("_quickColors", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(editor);
                    var items = (System.Collections.Generic.IEnumerable<ToolStripItem>)palette.GetType().GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(palette);
                    items.OfType<ToolStripButton>().First(button => button.Tag is Color c && c.ToArgb() == Color.Black.ToArgb()).PerformClick();
                    Assert.Equal(Color.Black.ToArgb(), rectangle.GetFieldValueAsColor(Greenshot.Editor.Drawing.Fields.FieldType.LINE_COLOR).ToArgb());
                    surface.Undo();
                    Assert.Equal(Color.Blue.ToArgb(), rectangle.GetFieldValueAsColor(Greenshot.Editor.Drawing.Fields.FieldType.LINE_COLOR).ToArgb());
                    surface.Redo();
                    Assert.Equal(Color.Black.ToArgb(), rectangle.GetFieldValueAsColor(Greenshot.Editor.Drawing.Fields.FieldType.LINE_COLOR).ToArgb());

                    using var stream = new System.IO.MemoryStream();
                    surface.SaveElementsToStream(stream); stream.Position = 0;
                    using var loaded = new Greenshot.Editor.Drawing.Surface(new Bitmap(300, 200));
                    loaded.LoadElementsFromStream(stream);
                    var restored = Assert.IsType<Greenshot.Editor.Drawing.RectangleContainer>(Assert.Single(loaded.Elements));
                    Assert.Equal(Color.Black.ToArgb(), restored.GetFieldValueAsColor(Greenshot.Editor.Drawing.Fields.FieldType.LINE_COLOR).ToArgb());
                    Assert.Equal(Color.Red.ToArgb(), restored.GetFieldValueAsColor(Greenshot.Editor.Drawing.Fields.FieldType.FILL_COLOR).ToArgb());
                }
                catch (Exception e) { error = e; }
            });
            thread.SetApartmentState(ApartmentState.STA); thread.Start(); thread.Join();
            if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
        }
    }
}