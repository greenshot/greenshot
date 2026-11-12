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
using System.Reflection;
using System.Windows.Forms;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Xunit;

namespace Greenshot.Tests.Editor
{
    /// <summary>
    /// The paint buffer is only kept while it's needed, the remembered drawing bounds follow every change of the element
    /// </summary>
    public class EditorPaintTests
    {
        public EditorPaintTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static Bitmap CreateImage(int width, int height)
        {
            var image = new Bitmap(width, height);
            // The buffer takes the resolution of the image, which is 0 under Mono
            image.SetResolution(96, 96);
            return image;
        }

        private static void Paint(Surface surface)
        {
            using var target = new Bitmap(surface.Width, surface.Height);
            using var graphics = Graphics.FromImage(target);
            var paint = typeof(Surface).GetMethod("SurfacePaint", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(paint);
            paint.Invoke(surface, new object[] { surface, new PaintEventArgs(graphics, new Rectangle(0, 0, surface.Width, surface.Height)) });
        }

        [Fact]
        public void PaintBuffer_IsReleasedWhenTheZoomGoesBackTo100Percent()
        {
            using var surface = new Surface(CreateImage(200, 100));

            surface.ZoomFactor = new Fraction(2, 1);
            Paint(surface);
            Assert.True(surface.HasPaintBuffer);

            surface.ZoomFactor = Fraction.Identity;
            Paint(surface);
            Assert.False(surface.HasPaintBuffer);
        }

        [Fact]
        public void PaintBuffer_IsKeptWhileAnElementHasAFilter()
        {
            using var surface = new Surface(CreateImage(200, 100));
            var highlight = new HighlightContainer(surface)
            {
                Left = 10,
                Top = 10,
                Width = 50,
                Height = 30
            };
            surface.AddElement(highlight, false, false);

            surface.ZoomFactor = new Fraction(2, 1);
            Paint(surface);
            surface.ZoomFactor = Fraction.Identity;
            Paint(surface);

            Assert.True(surface.HasPaintBuffer);
        }

        [Fact]
        public void ArrowDrawingBounds_FollowMovesAndTheLineThickness()
        {
            using var surface = new Surface(new Bitmap(400, 400));
            var arrow = new ArrowContainer(surface)
            {
                Left = 10,
                Top = 10,
                Width = 100,
                Height = 50
            };
            surface.AddElement(arrow, false, false);

            var before = arrow.DrawingBounds;
            Assert.Equal(before, arrow.DrawingBounds);

            arrow.Left = 60;
            var moved = arrow.DrawingBounds;
            Assert.Equal(before.X + 50, moved.X);

            arrow.SetFieldValue(FieldType.LINE_THICKNESS, 20);
            Assert.True(arrow.DrawingBounds.Width > moved.Width);
        }

        [Fact]
        public void SpeechBubbleDrawingBounds_FollowTheTail()
        {
            using var surface = new Surface(new Bitmap(400, 400));
            var bubble = new SpeechbubbleContainer(surface)
            {
                Left = 100,
                Top = 100,
                Width = 80,
                Height = 40
            };
            surface.AddElement(bubble, false, false);
            bubble.SetTailLocation(new NativePoint(150, 160));
            var before = bubble.DrawingBounds;

            bubble.SetTailLocation(new NativePoint(350, 350));

            var after = bubble.DrawingBounds;
            Assert.True(after.Right > before.Right);
            Assert.True(after.Bottom > before.Bottom);
        }

        [Theory]
        [InlineData(400, 390)]
        [InlineData(395, 20)]
        [InlineData(5, 387)]
        public void SpeechBubbleTail_EndsExactlyOnTheTarget(int targetX, int targetY)
        {
            using var surface = new Surface(new Bitmap(400, 400));
            var bubble = new SpeechbubbleContainer(surface)
            {
                Left = 20,
                Top = 30,
                Width = 81,
                Height = 41
            };
            surface.AddElement(bubble, false, false);

            // Moving the bubble a pixel at a time must not move the tip away from the target (it jumped with whole degrees)
            for (int left = 20; left < 40; left++)
            {
                bubble.Left = left;
                bubble.SetTailLocation(new NativePoint(targetX, targetY));
                using var tail = bubble.CreateTail();
                var points = tail.PathPoints;
                Assert.Contains(points, point => System.Math.Abs(point.X - targetX) < 0.01 && System.Math.Abs(point.Y - targetY) < 0.01);
            }
        }
    }
}
