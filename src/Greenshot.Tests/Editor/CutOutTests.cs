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
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using Dapplo.Windows.Common.Structs;
using Greenshot.Editor.Drawing;
using Greenshot.Base.Core;
using Greenshot.Base.Effects;
using Greenshot.Editor.Drawing.Fields;
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

        private static TornEdgeEffect CreateEdgeSettings() => new TornEdgeEffect
        {
            ToothHeight = 12,
            HorizontalToothRange = 20,
            VerticalToothRange = 20
        };

        private static Surface CreateRedSurface()
        {
            var source = new Bitmap(100, 100);
            using (var graphics = Graphics.FromImage(source))
            {
                graphics.Clear(Color.Red);
            }

            return new Surface(source);
        }

        [Fact]
        public void ApplyCutOut_TornEdges_AddsAFullWidthCutMark_AndKeepsTheGap()
        {
            using var surface = CreateRedSurface();
            var below = AddRectangle(surface, 5, 70);

            Assert.True(surface.ApplyCutOut(new NativeRect(0, 30, 100, 30), CropContainer.CropModes.Horizontal, CutMarkStyle.Torn, CreateEdgeSettings()));

            // 12 rows of the strip are kept for the gap
            Assert.Equal(new Size(100, 82), surface.Image.Size);
            Assert.Equal(52, below.Top);
            var cutMark = Assert.Single(surface.Elements.OfType<CutMarkContainer>());
            Assert.True(cutMark.IsHorizontal);
            Assert.Equal(0, cutMark.Left);
            Assert.Equal(100, cutMark.Width);
            // The band has a tooth on both sides of the gap
            Assert.Equal(18, cutMark.Top);
            Assert.Equal(36, cutMark.Height);
            Assert.Equal(CutMarkStyle.Torn, cutMark.GetFieldValue(FieldType.CUT_MARK_STYLE));

            surface.Undo();

            Assert.Equal(new Size(100, 100), surface.Image.Size);
            Assert.Equal(70, below.Top);
            Assert.Empty(surface.Elements.OfType<CutMarkContainer>());

            surface.Redo();

            Assert.Equal(new Size(100, 82), surface.Image.Size);
            Assert.Contains(cutMark, surface.Elements);
        }

        [Fact]
        public void ApplyCutOut_Vertical_CutMarkSpansTheFullHeight()
        {
            using var surface = CreateRedSurface();

            Assert.True(surface.ApplyCutOut(new NativeRect(30, 0, 30, 100), CropContainer.CropModes.Vertical, CutMarkStyle.ZigZag, CreateEdgeSettings()));

            var cutMark = Assert.Single(surface.Elements.OfType<CutMarkContainer>());
            Assert.False(cutMark.IsHorizontal);
            Assert.Equal(0, cutMark.Top);
            Assert.Equal(100, cutMark.Height);
            Assert.Equal(18, cutMark.Left);
        }

        [Fact]
        public void CutMark_MovesOnlyAcrossTheCut()
        {
            using var surface = CreateRedSurface();
            surface.ApplyCutOut(new NativeRect(0, 30, 100, 30), CropContainer.CropModes.Horizontal, CutMarkStyle.Torn, CreateEdgeSettings());
            var cutMark = surface.Elements.OfType<CutMarkContainer>().Single();

            cutMark.MoveBy(10, 5);

            Assert.Equal(0, cutMark.Left);
            Assert.Equal(100, cutMark.Width);
            Assert.Equal(23, cutMark.Top);
        }

        [Fact]
        public void CutMark_DraggedWithTheSelection_StaysFullWidth()
        {
            using var surface = CreateRedSurface();
            surface.ApplyCutOut(new NativeRect(0, 30, 100, 30), CropContainer.CropModes.Horizontal, CutMarkStyle.Torn, CreateEdgeSettings());
            var cutMark = surface.Elements.OfType<CutMarkContainer>().Single();
            var selection = new DrawableContainerList(surface.ID) { cutMark };

            selection.MoveBy(15, -4);

            Assert.Equal(0, cutMark.Left);
            Assert.Equal(100, cutMark.Width);
            Assert.Equal(14, cutMark.Top);
        }

        [Fact]
        public void CutMark_BiggerTeeth_KeepTheGap()
        {
            using var surface = CreateRedSurface();
            surface.ApplyCutOut(new NativeRect(0, 30, 100, 30), CropContainer.CropModes.Horizontal, CutMarkStyle.Torn, CreateEdgeSettings());
            var cutMark = surface.Elements.OfType<CutMarkContainer>().Single();

            cutMark.SetFieldValue(FieldType.TOOTH_HEIGHT, 20);

            // 8 more on both sides, the gap stays 12
            Assert.Equal(10, cutMark.Top);
            Assert.Equal(52, cutMark.Height);
        }

        [Fact]
        public void CutMark_KeepsItsEdge_UntilReseeded()
        {
            using var surface = CreateRedSurface();
            surface.ApplyCutOut(new NativeRect(0, 30, 100, 30), CropContainer.CropModes.Horizontal, CutMarkStyle.Torn, CreateEdgeSettings());
            var cutMark = surface.Elements.OfType<CutMarkContainer>().Single();
            int seed = cutMark.Seed;

            Assert.Equal(seed, cutMark.GetSettings().Seed);

            cutMark.NewSeed();

            Assert.NotEqual(seed, cutMark.Seed);
        }

        [Fact]
        public void CutMark_SurvivesSaveAndLoad()
        {
            using var surface = CreateRedSurface();
            surface.ApplyCutOut(new NativeRect(0, 30, 100, 30), CropContainer.CropModes.Horizontal, CutMarkStyle.Wave, CreateEdgeSettings());
            var cutMark = surface.Elements.OfType<CutMarkContainer>().Single();

            using var stream = new MemoryStream();
            surface.SaveElementsToStream(stream);
            stream.Position = 0;
            using var loadedSurface = new Surface(new Bitmap(100, 82));
            loadedSurface.LoadElementsFromStream(stream);

            var loaded = Assert.Single(loadedSurface.Elements.OfType<CutMarkContainer>());
            Assert.Equal(cutMark.Seed, loaded.Seed);
            Assert.Equal(cutMark.Top, loaded.Top);
            Assert.Equal(cutMark.Height, loaded.Height);
            Assert.True(loaded.IsHorizontal);
            Assert.Equal(CutMarkStyle.Wave, loaded.GetFieldValue(FieldType.CUT_MARK_STYLE));
        }

        [Fact]
        public void Export_WithTransparentGap_HasTransparentPixels()
        {
            using var surface = CreateRedSurface();
            surface.ApplyCutOut(new NativeRect(0, 30, 100, 30), CropContainer.CropModes.Horizontal, CutMarkStyle.Torn, CreateEdgeSettings());
            var cutMark = surface.Elements.OfType<CutMarkContainer>().Single();
            cutMark.SetFieldValue(FieldType.SHADOW, false);

            using var exported = (Bitmap)surface.GetImageForExport();

            Assert.Equal(255, exported.GetPixel(50, 2).A);
            Assert.Equal(255, exported.GetPixel(50, 79).A);
            // The middle of the gap
            Assert.Equal(0, exported.GetPixel(50, 36).A);
        }

        [Fact]
        public void ApplyCutOut_AtTheEdge_IsSeamless()
        {
            using var surface = new Surface(new Bitmap(100, 100));

            Assert.True(surface.ApplyCutOut(new NativeRect(0, 0, 100, 30), CropContainer.CropModes.Horizontal, CutMarkStyle.Torn, CreateEdgeSettings()));

            Assert.Equal(new Size(100, 70), surface.Image.Size);
            Assert.Empty(surface.Elements.OfType<CutMarkContainer>());
        }

        [Fact]
        public void CutOut_JoinsSeamlessly()
        {
            using var source = new Bitmap(80, 60);

            using var vertical = CutOutHelper.CutOut(source, 20, 30, false);
            using var horizontal = CutOutHelper.CutOut(source, 20, 30, true);

            Assert.Equal(new Size(50, 60), vertical.Size);
            Assert.Equal(new Size(80, 30), horizontal.Size);
        }

        [Fact]
        public void TornEdgeEffect_SameSeed_GivesTheSameEdges()
        {
            using var source = new Bitmap(120, 80);
            using (var graphics = Graphics.FromImage(source))
            {
                graphics.Clear(Color.Red);
            }

            var effect = new TornEdgeEffect { GenerateShadow = false };
            using var first = (Bitmap)effect.Apply(source, new Matrix());
            using var second = (Bitmap)effect.Apply(source, new Matrix());

            for (int x = 0; x < first.Width; x++)
            {
                Assert.Equal(first.GetPixel(x, 5).A, second.GetPixel(x, 5).A);
            }
        }

        [Fact]
        public void TornEdgeEffect_BackgroundColor_IsStoredInTheSettings()
        {
            var effect = new TornEdgeEffect { BackgroundColor = Color.FromArgb(255, 10, 20, 30) };
            var converter = new EffectConverter();

            string stored = (string)converter.ConvertTo(null, CultureInfo.InvariantCulture, effect, typeof(string));
            var loaded = (TornEdgeEffect)converter.ConvertFrom(null, CultureInfo.InvariantCulture, stored);

            Assert.Equal(effect.BackgroundColor.ToArgb(), loaded.BackgroundColor.ToArgb());
        }

        [Fact]
        public void TornEdgeEffect_OnlyTransparentNeedsAlpha()
        {
            using var source = new Bitmap(120, 80, PixelFormat.Format24bppRgb);
            using (var graphics = Graphics.FromImage(source))
            {
                graphics.Clear(Color.Red);
            }

            using var transparent = new TornEdgeEffect().Apply(source, new Matrix());
            using var white = new TornEdgeEffect { BackgroundColor = Color.White }.Apply(source, new Matrix());

            Assert.Equal(PixelFormat.Format32bppArgb, transparent.PixelFormat);
            Assert.Equal(PixelFormat.Format24bppRgb, white.PixelFormat);
        }

        [Fact]
        public void TornEdges_AreAnElement_WhichCanBeUndone()
        {
            using var surface = CreateRedSurface();

            var tornEdges = surface.AddTornEdges(CreateEdgeSettings());

            Assert.Contains(tornEdges, surface.Elements);
            Assert.Same(tornEdges, surface.AddTornEdges(CreateEdgeSettings()));
            Assert.Single(surface.Elements.OfType<TornEdgeContainer>());
            Assert.Equal(new Size(100, 100), surface.Image.Size);

            surface.Undo();

            Assert.Empty(surface.Elements.OfType<TornEdgeContainer>());
        }

        [Fact]
        public void TornEdges_CoverTheImage_AndOnlyTheEdgesAreClickable()
        {
            using var surface = CreateRedSurface();
            var tornEdges = surface.AddTornEdges(CreateEdgeSettings());

            tornEdges.MoveBy(10, 10);

            Assert.Equal(0, tornEdges.Left);
            Assert.Equal(0, tornEdges.Top);
            Assert.True(tornEdges.ClickableAt(2, 50));
            Assert.True(tornEdges.ClickableAt(50, 97));
            Assert.False(tornEdges.ClickableAt(50, 50));
        }

        [Fact]
        public void TornEdges_Export_IsTransparentOutsideTheEdges()
        {
            using var surface = CreateRedSurface();
            var tornEdges = surface.AddTornEdges(CreateEdgeSettings());
            tornEdges.SetFieldValue(FieldType.SHADOW, false);

            using var exported = (Bitmap)surface.GetImageForExport();

            Assert.Equal(new Size(100, 100), exported.Size);
            Assert.Equal(0, exported.GetPixel(0, 0).A);
            Assert.Equal(255, exported.GetPixel(50, 50).A);
        }

        [Fact]
        public void TornEdges_SurviveSaveAndLoad()
        {
            using var surface = CreateRedSurface();
            var tornEdges = surface.AddTornEdges(CreateEdgeSettings());

            using var stream = new MemoryStream();
            surface.SaveElementsToStream(stream);
            stream.Position = 0;
            using var loadedSurface = new Surface(new Bitmap(100, 100));
            loadedSurface.LoadElementsFromStream(stream);

            var loaded = Assert.Single(loadedSurface.Elements.OfType<TornEdgeContainer>());
            Assert.Equal(tornEdges.Seed, loaded.Seed);
            Assert.Equal(12, loaded.ToothHeight);
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
