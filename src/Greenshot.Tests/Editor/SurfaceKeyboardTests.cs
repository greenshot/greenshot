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
using System.Threading;
using System.Windows.Forms;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Xunit;

namespace Greenshot.Tests.Editor
{
    /// <summary>
    /// The canvas works without a mouse: Enter draws, Tab selects, Ctrl+arrows resize, and a screen reader sees the elements
    /// </summary>
    public class SurfaceKeyboardTests
    {
        public SurfaceKeyboardTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static RectangleContainer AddRectangle(Surface surface, int left, int top, int width, int height)
        {
            var rectangle = new RectangleContainer(surface)
            {
                Left = left,
                Top = top,
                Width = width,
                Height = height
            };
            surface.AddElement(rectangle, false, false);
            return rectangle;
        }

        [Fact]
        public void Enter_DrawsARectangleInTheMiddleOfTheView()
        {
            using var surface = new Surface(new Bitmap(400, 300));
            surface.DrawingMode = DrawingModes.Rect;

            Assert.True(surface.ProcessCanvasKey(Keys.Enter));

            var rectangle = Assert.Single(surface.Elements.OfType<RectangleContainer>());
            Assert.Equal(new NativeRect(120, 110, 160, 80), rectangle.Bounds);
            Assert.True(rectangle.Selected);
            Assert.Same(rectangle, Assert.Single(surface.SelectedElements));

            // Undo removes it like an element drawn with the mouse
            Assert.True(surface.CanUndo);
            surface.Undo();
            Assert.Empty(surface.Elements);
        }

        [Fact]
        public void Enter_WithoutATool_DrawsNothing()
        {
            using var surface = new Surface(new Bitmap(400, 300));

            Assert.False(surface.ProcessCanvasKey(Keys.Enter));
            Assert.Empty(surface.Elements);
        }

        [Fact]
        public void Tab_CyclesTheSelectionInZOrder_AndLeavesTheCanvasAfterTheLast()
        {
            using var surface = new Surface(new Bitmap(400, 300));
            var first = AddRectangle(surface, 10, 10, 20, 20);
            var second = AddRectangle(surface, 50, 10, 20, 20);
            var third = AddRectangle(surface, 90, 10, 20, 20);

            Assert.True(surface.ProcessCanvasKey(Keys.Tab));
            Assert.Same(first, Assert.Single(surface.SelectedElements));
            Assert.True(surface.ProcessCanvasKey(Keys.Tab));
            Assert.Same(second, Assert.Single(surface.SelectedElements));
            Assert.True(surface.ProcessCanvasKey(Keys.Shift | Keys.Tab));
            Assert.Same(first, Assert.Single(surface.SelectedElements));
            Assert.False(surface.ProcessCanvasKey(Keys.Shift | Keys.Tab));
            Assert.Empty(surface.SelectedElements);

            // From nothing selected, Shift+Tab starts at the top
            Assert.True(surface.ProcessCanvasKey(Keys.Shift | Keys.Tab));
            Assert.Same(third, Assert.Single(surface.SelectedElements));
            // After the last one Tab moves on: the key isn't handled and nothing is selected
            Assert.False(surface.ProcessCanvasKey(Keys.Tab));
            Assert.Empty(surface.SelectedElements);
        }

        [Fact]
        public void Escape_ClearsTheSelection()
        {
            using var surface = new Surface(new Bitmap(400, 300));
            var rectangle = AddRectangle(surface, 10, 10, 20, 20);
            surface.SelectElement(rectangle);

            Assert.True(surface.ProcessCanvasKey(Keys.Escape));
            Assert.Empty(surface.SelectedElements);
            Assert.False(rectangle.Selected);
        }

        [Fact]
        public void CtrlArrows_ResizeTheSelection_AndOneUndoRestoresIt()
        {
            using var surface = new Surface(new Bitmap(400, 300));
            var rectangle = AddRectangle(surface, 10, 20, 100, 50);
            surface.SelectElement(rectangle);

            Assert.True(surface.ProcessCmdKey(Keys.Control | Keys.Right));
            Assert.True(surface.ProcessCmdKey(Keys.Control | Keys.Shift | Keys.Down));
            Assert.True(surface.ProcessCmdKey(Keys.Control | Keys.Up));

            Assert.Equal(new NativeRect(10, 20, 101, 59), rectangle.Bounds);

            surface.Undo();
            Assert.Equal(new NativeRect(10, 20, 100, 50), rectangle.Bounds);
        }

        [Fact]
        public void CtrlArrows_NeverMakeAnElementVanish()
        {
            using var surface = new Surface(new Bitmap(400, 300));
            var rectangle = AddRectangle(surface, 10, 20, 5, 5);
            surface.SelectElement(rectangle);

            surface.ProcessCmdKey(Keys.Control | Keys.Shift | Keys.Left);

            Assert.Equal(1, rectangle.Width);
        }

        [Fact]
        public void AccessibleObject_HasAChildPerElement_WithNameDescriptionAndState()
        {
            RunOnSta(() =>
            {
                using var surface = new Surface(new Bitmap(400, 300));
                var rectangle = AddRectangle(surface, 10, 20, 120, 40);
                var text = new TextContainer(surface)
                {
                    Left = 200,
                    Top = 100,
                    Width = 80,
                    Height = 30
                };
                surface.AddElement(text, false, false);
                text.Text = "Hello";
                surface.SelectElement(rectangle);

                var canvas = surface.AccessibilityObject;
                Assert.Equal(2, canvas.GetChildCount());

                var rectangleChild = canvas.GetChild(0);
                Assert.Equal("Rectangle", rectangleChild.Name);
                Assert.Equal("120 × 40 at 10, 20", rectangleChild.Description);
                Assert.True((rectangleChild.State & AccessibleStates.Selected) != 0);
                Assert.Same(canvas, rectangleChild.Parent);
                // The same object every time, a screen reader keeps track of it
                Assert.Same(rectangleChild, canvas.GetChild(0));

                var textChild = canvas.GetChild(1);
                Assert.Equal("Text: Hello", textChild.Name);
                Assert.True((textChild.State & AccessibleStates.Selected) == 0);
                Assert.Same(textChild, rectangleChild.Navigate(AccessibleNavigation.Next));
            });
        }

        private static void RunOnSta(Action action)
        {
            Exception exception = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (exception != null)
            {
                throw new Xunit.Sdk.XunitException($"Failed on the STA thread: {exception}");
            }
        }
    }
}
