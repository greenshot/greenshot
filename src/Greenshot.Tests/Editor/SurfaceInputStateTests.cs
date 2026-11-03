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

using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Xunit;

namespace Greenshot.Tests.Editor
{
    /// <summary>
    /// The editor reads Shift and the mouse buttons through InputState, the tests set them instead of the desktop
    /// </summary>
    [Collection(TestCollections.InputState)]
    public class SurfaceInputStateTests
    {
        public SurfaceInputStateTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void Enter_WithShift_DrawsASquare()
        {
            InputState.ModifierKeysSource = () => Keys.Shift;
            try
            {
                using var surface = new Surface(new Bitmap(400, 300));
                surface.DrawingMode = DrawingModes.Rect;

                Assert.True(surface.ProcessCanvasKey(Keys.Enter));

                var rectangle = Assert.Single(surface.Elements.OfType<RectangleContainer>());
                Assert.Equal(rectangle.Bounds.Width, rectangle.Bounds.Height);
            }
            finally
            {
                InputState.ModifierKeysSource = () => Keys.None;
            }
        }

        [Fact]
        public void CanvasKeys_AreIgnoredWhileAMouseButtonIsPressed()
        {
            InputState.MouseButtonsSource = () => MouseButtons.Left;
            try
            {
                using var surface = new Surface(new Bitmap(400, 300));
                surface.DrawingMode = DrawingModes.Rect;

                Assert.False(surface.ProcessCanvasKey(Keys.Enter));
                Assert.Empty(surface.Elements);
            }
            finally
            {
                InputState.MouseButtonsSource = () => MouseButtons.None;
            }
        }
    }
}
