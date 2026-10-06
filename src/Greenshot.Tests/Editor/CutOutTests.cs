/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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
using Dapplo.Windows.Common.Structs;
using Greenshot.Editor.Drawing;
using Greenshot.Base.Effects;
using Greenshot.Editor.Helpers;
using Xunit;

namespace Greenshot.Tests.Editor
{
    /// <summary>
    /// Crop out horizontally / vertically: only the elements after the cut move (#485)
    /// </summary>
    public class CutOutTests
    {
        public CutOutTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Theory]
        [InlineData(10, 10, Surface.CutOutPlacement.Before)]
        [InlineData(20, 10, Surface.CutOutPlacement.Before)]
        [InlineData(40, 10, Surface.CutOutPlacement.Inside)]
        [InlineData(30, 30, Surface.CutOutPlacement.Inside)]
        [InlineData(60, 10, Surface.CutOutPlacement.After)]
        [InlineData(25, 10, Surface.CutOutPlacement.Across)]
        [InlineData(55, 10, Surface.CutOutPlacement.Across)]
        [InlineData(20, 50, Surface.CutOutPlacement.Across)]
        public void GetCutOutPlacement_Horizontal(int top, int height, Surface.CutOutPlacement expected)
        {
            // Rows 30 up to 60 are cut out
            Assert.Equal(expected, Surface.GetCutOutPlacement(new NativeRect(5, top, 10, height), 30, 30, true));
        }

        [Fact]
        public void GetCutOutPlacement_Vertical_UsesColumns()
        {
            Assert.Equal(Surface.CutOutPlacement.After, Surface.GetCutOutPlacement(new NativeRect(70, 0, 10, 100), 30, 30, false));
            Assert.Equal(Surface.CutOutPlacement.Inside, Surface.GetCutOutPlacement(new NativeRect(35, 0, 10, 100), 30, 30, false));
        }

        [Fact]
        public void GetCutOutPlacement_NegativeSize_IsNormalized()
        {
            // A line drawn upwards has a negative height
            Assert.Equal(Surface.CutOutPlacement.Inside, Surface.GetCutOutPlacement(new NativeRect(5, 50, 10, -10), 30, 30, true));
        }

        private static RectangleContainer AddRectangle(Surface surface, int left, int top)
        {
            var rectangle = new RectangleContainer(surface)
            {
                Left = left,
                Top = top,
                Width = 10,
                Height = 10
            };
            surface.AddElement(rectangle, false, false);
            return rectangle;
        }

        [Fact]
        public void ApplyCutOut_Horizontal_MovesOnlyElementsBelow_AndUndoRestores()
        {
            using var surface = new Surface(new Bitmap(100, 100));
            var above = AddRectangle(surface, 5, 10);
            var inside = AddRectangle(surface, 5, 40);
            var across = AddRectangle(surface, 5, 25);
            var below = AddRectangle(surface, 5, 70);

            Assert.True(surface.ApplyCutOut(new NativeRect(0, 30, 100, 30), CropContainer.CropModes.Horizontal));

            Assert.Equal(new Size(100, 70), surface.Image.Size);
            Assert.Equal(10, above.Top);
            Assert.Equal(25, across.Top);
            Assert.Equal(40, below.Top);
            Assert.DoesNotContain(inside, surface.Elements);

            surface.Undo();

            Assert.Equal(new Size(100, 100), surface.Image.Size);
            Assert.Equal(10, above.Top);
            Assert.Equal(25, across.Top);
            Assert.Equal(70, below.Top);
            Assert.Contains(inside, surface.Elements);

            surface.Redo();

            Assert.Equal(new Size(100, 70), surface.Image.Size);
            Assert.Equal(40, below.Top);
            Assert.DoesNotContain(inside, surface.Elements);
        }

        [Fact]
        public void ApplyCutOut_Vertical_MovesOnlyElementsRight()
        {
            using var surface = new Surface(new Bitmap(100, 100));
            var left = AddRectangle(surface, 10, 5);
            var right = AddRectangle(surface, 70, 5);

            Assert.True(surface.ApplyCutOut(new NativeRect(30, 0, 30, 100), CropContainer.CropModes.Vertical));

            Assert.Equal(new Size(70, 100), surface.Image.Size);
            Assert.Equal(10, left.Left);
            Assert.Equal(40, right.Left);
        }

        [Fact]
        public void ApplyCutOut_TornEdges_LeavesATransparentGap_AndMovesByCutSizeMinusGap()
        {
            using var source = new Bitmap(100, 100);
            using (var graphics = Graphics.FromImage(source))
            {
                graphics.Clear(Color.Red);
            }

            using var surface = new Surface((Image)source.Clone());
            var below = AddRectangle(surface, 5, 70);
            var settings = new TornEdgeEffect();
            int gap = CutOutHelper.GetGap(CutMarkStyle.Torn, settings);

            Assert.True(surface.ApplyCutOut(new NativeRect(0, 30, 100, 30), CropContainer.CropModes.Horizontal, CutMarkStyle.Torn, settings));

            Assert.Equal(new Size(100, 70 + gap), surface.Image.Size);
            Assert.Equal(70 - 30 + gap, below.Top);
            var image = (Bitmap)surface.Image;
            Assert.Equal(255, image.GetPixel(50, 2).A);
            Assert.Equal(255, image.GetPixel(50, 70 + gap - 2).A);
            // The middle of the gap only has the shadow
            Assert.True(image.GetPixel(50, 30 + gap / 2).A < 255);

            surface.Undo();

            Assert.Equal(new Size(100, 100), surface.Image.Size);
            Assert.Equal(70, below.Top);
        }

        [Fact]
        public void ApplyCutOut_AtTheEdge_IsSeamless()
        {
            using var surface = new Surface(new Bitmap(100, 100));

            Assert.True(surface.ApplyCutOut(new NativeRect(0, 0, 100, 30), CropContainer.CropModes.Horizontal, CutMarkStyle.Torn, new TornEdgeEffect()));

            Assert.Equal(new Size(100, 70), surface.Image.Size);
        }

        [Theory]
        [InlineData(CutMarkStyle.Line)]
        [InlineData(CutMarkStyle.ZigZag)]
        [InlineData(CutMarkStyle.Wave)]
        [InlineData(CutMarkStyle.Torn)]
        public void CutOut_EveryStyle_HasTheExpectedSize(CutMarkStyle style)
        {
            using var source = new Bitmap(80, 60);
            var settings = new TornEdgeEffect();

            using var vertical = CutOutHelper.CutOut(source, 20, 30, false, style, settings);

            Assert.Equal(new Size(50 + CutOutHelper.GetGap(style, settings), 60), vertical.Size);
        }

        [Theory]
        [InlineData(CutMarkStyle.Line)]
        [InlineData(CutMarkStyle.ZigZag)]
        [InlineData(CutMarkStyle.Wave)]
        [InlineData(CutMarkStyle.Torn)]
        public void CreateEdge_StaysWithinTheToothHeight(CutMarkStyle style)
        {
            var edge = CutOutHelper.CreateEdge(style, 200, 12, 20, new Random(7));

            Assert.All(edge, p => Assert.InRange(p.Y, 0, 12));
            Assert.Equal(0, edge.First().X);
            Assert.Equal(200, edge.Last().X);
        }
    }
}
