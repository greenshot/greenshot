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
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Forms;
using Xunit;
namespace Greenshot.Tests.Editor
{
    [Collection(TestCollections.Clipboard)]
    public class RubberBandEditorSmokeTests
    {
        public RubberBandEditorSmokeTests() => TestEnvironment.EnsureInitialized();

        [Fact]
        public void EditorRendersFeatureAtConstrainedWidth()
        {
            Exception error = null;
            var thread = new Thread(() =>
            {
                try
                {
                    using var s = new Surface(new Bitmap(600, 360));
                    var first = new RectangleContainer(s) { Left = 50, Top = 50, Width = 100, Height = 80 };
                    var second = new RectangleContainer(s) { Left = 250, Top = 180, Width = 100, Height = 80 };
                    s.AddElement(first, false); s.AddElement(second, false);
                    s.DrawingMode = DrawingModes.None;
                    var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                    typeof(Surface).GetMethod("BeginSelectionBox", flags).Invoke(s, new object[] { new Point(20, 20), Keys.None });
                    typeof(Surface).GetMethod("MoveSelectionBox", flags).Invoke(s, new object[] { new Point(400, 300) });
                    using var editor = new ImageEditorForm(s, true, false);
                    editor.Size = new Size(850, 560);
                    editor.Show();
                    Application.DoEvents();
                    using var bitmap = new Bitmap(editor.Width, editor.Height);
                    editor.DrawToBitmap(bitmap, new Rectangle(Point.Empty, editor.Size));
                    var path = Environment.GetEnvironmentVariable("GREENSHOT_FEATURE_SMOKE");
                    if (!string.IsNullOrEmpty(path)) { Directory.CreateDirectory(Path.GetDirectoryName(path)); bitmap.Save(path); }
                    editor.Hide();
                }
                catch (Exception e) { error = e; }
            });
            thread.SetApartmentState(ApartmentState.STA); thread.Start(); thread.Join();
            if (error != null) throw error;
        }
    }
}