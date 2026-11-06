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
using System.Threading;
using System.Windows.Forms;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Xunit;
namespace Greenshot.Tests.Editor
{
    public class RubberBandSelectionTests
    {
        public RubberBandSelectionTests() => TestEnvironment.EnsureInitialized();

        private static void Sta(Action action)
        {
            Exception error = null;
            var thread = new Thread(() => { try { action(); } catch (Exception e) { error = e; } });
            thread.SetApartmentState(ApartmentState.STA); thread.Start(); thread.Join();
            if (error != null) throw error;
        }

        private static void Mouse(Surface s, string name, int x, int y)
            => typeof(Control).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(s, new object[] { new MouseEventArgs(MouseButtons.Left, 1, x, y, 0) });

        private static RectangleContainer Box(Surface s, int x)
        {
            var box = new RectangleContainer(s) { Left = x, Top = 40, Width = 20, Height = 20 };
            box.SetFieldValue(FieldType.SHADOW, false); s.AddElement(box, false); return box;
        }

        [Fact]
        public void DragSelectsEnclosedObjectsAndEscapeRestoresInitialSelection() => Sta(() =>
        {
            using var s = new Surface(new Bitmap(300, 200));
            var first = Box(s, 40); var second = Box(s, 160);
            s.DrawingMode = DrawingModes.None;
            s.DeselectAllElements(); s.SelectElement(second);
            Mouse(s, "OnMouseDown", 10, 10); Mouse(s, "OnMouseMove", 100, 100);
            Assert.True(first.Selected); Assert.False(second.Selected);
            Assert.True(s.ProcessCmdKey(Keys.Escape));
            Assert.False(first.Selected); Assert.True(second.Selected);
        });

        [Fact]
        public void PartialIntersectionDoesNotSelectAndReleaseFinishesGesture() => Sta(() =>
        {
            using var s = new Surface(new Bitmap(300, 200));
            var first = Box(s, 40); var second = Box(s, 160);
            s.DrawingMode = DrawingModes.None; s.DeselectAllElements();
            Mouse(s, "OnMouseDown", 10, 10); Mouse(s, "OnMouseMove", 170, 100); Mouse(s, "OnMouseUp", 170, 100);
            Assert.True(first.Selected); Assert.False(second.Selected);
            Assert.False((bool)typeof(Surface).GetField("_selectionBoxActive", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(s));
        });
        [Fact]
        public void ShiftToggleAndLostCaptureRestorePriorSelectionAtZoom() => Sta(() =>
        {
            using var s = new Surface(new Bitmap(300, 200));
            var first = Box(s, 40); var second = Box(s, 160);
            s.DrawingMode = DrawingModes.None;
            s.DeselectAllElements(); s.SelectElement(first); s.SelectElement(second);
            s.ZoomFactor = new Greenshot.Base.Core.Fraction(2, 1);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(Surface).GetMethod("BeginSelectionBox", flags).Invoke(s, new object[] { new Point(10, 10), Keys.Shift });
            Mouse(s, "OnMouseMove", 200, 200);
            Assert.False(first.Selected); Assert.True(second.Selected);
            s.Capture = false;
            Assert.True(first.Selected); Assert.True(second.Selected);
        });
    }
}