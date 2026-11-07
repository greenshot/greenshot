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
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Forms;
using Xunit;

namespace Greenshot.Tests.Editor
{
    public class RubberBandEditorSmokeTests
    {
        public RubberBandEditorSmokeTests() => TestEnvironment.EnsureInitialized();

        [Fact]
        public void EditorRendersFeatureAtConstrainedWidth()
        {
            Exception failure = null;
            var uiThread = new Thread(() =>
            {
                try
                {
                    using var surface = new Surface(new Bitmap(600, 360));
                    var firstRectangle = new RectangleContainer(surface) { Left = 50, Top = 50, Width = 100, Height = 80 };
                    var secondRectangle = new RectangleContainer(surface) { Left = 250, Top = 180, Width = 100, Height = 80 };
                    surface.AddElement(firstRectangle, false);
                    surface.AddElement(secondRectangle, false);
                    surface.DrawingMode = DrawingModes.None;
                    var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
                    typeof(Surface).GetMethod("BeginSelectionBox", bindingFlags).Invoke(surface, new object[] { new Point(20, 20), Keys.None });
                    typeof(Surface).GetMethod("MoveSelectionBox", bindingFlags).Invoke(surface, new object[] { new Point(400, 300) });
                    using var editor = new ImageEditorForm(surface, true, false);
                    editor.Size = new Size(850, 560);
                    editor.Show();
                    Application.DoEvents();
                    using var bitmap = new Bitmap(editor.Width, editor.Height);
                    editor.DrawToBitmap(bitmap, new Rectangle(Point.Empty, editor.Size));
                    var previewPath = Environment.GetEnvironmentVariable("GREENSHOT_FEATURE_SMOKE");
                    if (!string.IsNullOrEmpty(previewPath))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(previewPath));
                        bitmap.Save(previewPath);
                    }

                    editor.Hide();
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
            });
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();
            Assert.True(uiThread.Join(TimeSpan.FromSeconds(30)), "Editor rendering timed out.");
            if (failure != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }
    }
}
