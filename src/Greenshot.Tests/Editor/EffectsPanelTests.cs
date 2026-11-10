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
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Effects;
using Greenshot.Base.Threading;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Panels;
using Greenshot.Editor.Panels.Effects;
using Greenshot.Editor.Panels.Sections;
using Greenshot.Editor.Panels.ViewModels;
using Xunit;

namespace Greenshot.Tests.Editor
{
    /// <summary>
    /// The Effects panel: presets and settings, the preview, and Apply as one undo step; and the Torn edges section of the
    /// Selection panel. Only the logic, no WPF.
    /// </summary>
    public class EffectsPanelTests
    {
        private readonly IEditorConfiguration _configuration;

        public EffectsPanelTests()
        {
            TestEnvironment.EnsureInitialized();
            _configuration = IniConfigRegistry.GetSection<IEditorConfiguration>();
        }

        private EffectsPanelViewModel CreateViewModel(Surface surface)
        {
            var context = new EditorPanelContext(null, (_, _) => { }, null);
            context.SetSurface(surface);
            return new EffectsPanelViewModel(context, _configuration);
        }

        private static Surface CreateSurface(int width = 200, int height = 100)
        {
            var bitmap = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.SteelBlue);
            }

            // A Surface is a WinForms control: made on the test thread it would put a WinForms context there, which nothing pumps,
            // and the awaited previews would never come back
            return WinFormsContextGuard.CreateWithoutContext(() => new Surface(bitmap));
        }

        [Fact]
        public async Task Border_ShowsInThePreview_AndApplyIsOneUndoStep()
        {
            using var surface = CreateSurface();
            using var viewModel = CreateViewModel(surface);
            viewModel.Show(EffectKind.Border);
            viewModel.ApplyPreset(viewModel.Presets[2]);
            Assert.Equal(6, viewModel.BorderWidth);

            using (var preview = await surface.CreateEffectPreviewAsync(viewModel.CreateEffect(), false))
            {
                Assert.Equal(new Size(212, 112), preview.Size);
            }

            // The preview doesn't change the capture
            Assert.Equal(new Size(200, 100), surface.Image.Size);
            Assert.False(surface.CanUndo);

            await Task.Run(() => viewModel.ApplyAsync());
            Assert.Equal(new Size(212, 112), surface.Image.Size);
            Assert.True(surface.CanUndo);
            surface.Undo();
            Assert.Equal(new Size(200, 100), surface.Image.Size);
            Assert.False(surface.CanUndo);
        }

        [Fact]
        public async Task PreviewWithElementsOnTop_LeavesTheElementsUnchanged()
        {
            using var surface = CreateSurface();
            var rectangle = new RectangleContainer(surface) { Left = 10, Top = 10, Width = 40, Height = 40 };
            rectangle.SetFieldValue(FieldType.FILL_COLOR, Color.Red);
            rectangle.SetFieldValue(FieldType.SHADOW, false);
            surface.AddElement(rectangle, false, false);

            using var preview = await surface.CreateEffectPreviewAsync(new GrayscaleEffect(), true);
            // Like the effect: the element stays red, the capture is gray
            var inside = preview.GetPixel(30, 30);
            Assert.True(inside.R > 200 && inside.G < 50, inside.ToString());
            var outside = preview.GetPixel(150, 80);
            Assert.Equal(outside.R, outside.G);
        }

        [Fact]
        public void Panel_HasOnlyTheEffectsWithSettings()
        {
            using var surface = CreateSurface();
            using var viewModel = CreateViewModel(surface);
            Assert.Equal(new[] { EffectKind.Border, EffectKind.DropShadow, EffectKind.RemoveTransparency, EffectKind.Resize },
                viewModel.Effects.Select(entry => entry.Kind).ToArray());
        }

        [Fact]
        public void TornEdgesSection_ChangesTheSidesAndTheShadow_AsUndoSteps()
        {
            using var surface = CreateSurface();
            // Without a shadow there is no room made for it, which keeps the capture as it is
            var tornEdges = surface.AddTornEdges(new TornEdgeEffect { GenerateShadow = false, Edges = new[] { false, false, true, false } });
            var context = new EditorPanelContext(null, (_, _) => { }, null);
            context.SetSurface(surface);
            var section = new TornEdgesSection(null);
            Assert.True(section.AppliesTo(context));
            using var viewModel = (TornEdgesSectionViewModel)section.CreateViewModel(context);
            Assert.True(viewModel.TornBottom);
            Assert.False(viewModel.TornTop);
            Assert.False(viewModel.HasShadow);
            // The shadow on or off is a property of the element
            tornEdges.SetFieldValue(FieldType.SHADOW, true);
            Assert.True(viewModel.HasShadow);

            viewModel.TornTop = true;
            Assert.Equal(new[] { true, false, true, false }, tornEdges.Edges);

            // Dragging a slider is one undo step
            int size = viewModel.ShadowSize;
            viewModel.ShadowSize = size + 1;
            viewModel.ShadowSize = size + 2;
            viewModel.ShadowSize = size + 3;
            Assert.Equal(size + 3, tornEdges.GetSettings().ShadowSize);

            int seed = tornEdges.Seed;
            viewModel.NewEdges();
            Assert.NotEqual(seed, tornEdges.Seed);

            surface.Undo();
            Assert.Equal(seed, tornEdges.Seed);
            surface.Undo();
            // All of the drag at once, the side stays
            Assert.Equal(size, viewModel.ShadowSize);
            Assert.True(viewModel.TornTop);
            surface.Undo();
            Assert.False(viewModel.TornTop);
            Assert.Single(surface.Elements.OfType<TornEdgeContainer>());
        }

        [Fact]
        public void Resize_KeepsTheAspectRatio_AndChecksTheLimit()
        {
            using var surface = CreateSurface();
            using var viewModel = CreateViewModel(surface);
            viewModel.Show(EffectKind.Resize);
            Assert.Equal("200", viewModel.ResizeWidth);
            Assert.False(viewModel.CanApply);

            viewModel.ResizeWidth = "400";
            Assert.Equal("200", viewModel.ResizeHeight);
            Assert.True(viewModel.CanApply);

            viewModel.ResizeWidth = "30001";
            Assert.False(viewModel.CanApply);
            Assert.True(viewModel.StatusIsError);

            viewModel.KeepAspectRatio = false;
            viewModel.ResizeWidth = "30000";
            viewModel.ResizeHeight = "50";
            Assert.True(viewModel.CanApply);

            viewModel.ApplyPreset(viewModel.Presets.First(preset => preset.Name.StartsWith("50")));
            Assert.Equal("100", viewModel.ResizeWidth);
            Assert.Equal("50", viewModel.ResizeHeight);
        }

        [Fact]
        public void RemoveTransparency_NeedsTransparency()
        {
            using var surface = WinFormsContextGuard.CreateWithoutContext(() => new Surface(new Bitmap(20, 20, System.Drawing.Imaging.PixelFormat.Format24bppRgb)));
            using var viewModel = CreateViewModel(surface);
            viewModel.Show(EffectKind.RemoveTransparency);
            Assert.False(viewModel.CanApply);
            Assert.True(viewModel.StatusIsError);
        }
    }
}
