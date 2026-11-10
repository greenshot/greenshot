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
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Panels;
using Greenshot.Editor.Panels.Effects;
using Xunit;

namespace Greenshot.Tests.Editor
{
    /// <summary>
    /// The Effects panel: presets and settings, the preview, and Apply as one undo step. Only the logic, no WPF.
    /// A Surface is a WinForms control, it puts a WindowsFormsSynchronizationContext on the test thread which nothing pumps:
    /// what awaits the thread pool starts there (Task.Run, like SurfaceHdrTests), or it never comes back.
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

            return new Surface(bitmap);
        }

        [Fact]
        public async Task Border_ShowsInThePreview_AndApplyIsOneUndoStep()
        {
            using var surface = CreateSurface();
            using var viewModel = CreateViewModel(surface);
            viewModel.Show(EffectKind.Border);
            viewModel.ApplyPreset(viewModel.Presets[2]);
            Assert.Equal(6, viewModel.BorderWidth);

            using (var preview = await Task.Run(() => surface.CreateEffectPreviewAsync(viewModel.CreateEffect(), false)))
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
        public async Task Grayscale_LeavesTheElementsInThePreview()
        {
            using var surface = CreateSurface();
            var rectangle = new RectangleContainer(surface) { Left = 10, Top = 10, Width = 40, Height = 40 };
            rectangle.SetFieldValue(Greenshot.Editor.Drawing.Fields.FieldType.FILL_COLOR, Color.Red);
            rectangle.SetFieldValue(Greenshot.Editor.Drawing.Fields.FieldType.SHADOW, false);
            surface.AddElement(rectangle, false, false);
            using var viewModel = CreateViewModel(surface);
            viewModel.Show(EffectKind.Grayscale);
            Assert.True(viewModel.HasNoSettings);

            using var preview = await Task.Run(() => surface.CreateEffectPreviewAsync(viewModel.CreateEffect(), true));
            // Like the effect: the element stays red, the capture is gray
            var inside = preview.GetPixel(30, 30);
            Assert.True(inside.R > 200 && inside.G < 50, inside.ToString());
            var outside = preview.GetPixel(150, 80);
            Assert.Equal(outside.R, outside.G);
        }

        [Fact]
        public async Task TornEdges_AreAnElementWithTheEdgesOfThePreview_AndCanBeChanged()
        {
            using var surface = CreateSurface();
            using var viewModel = CreateViewModel(surface);
            viewModel.Show(EffectKind.TornEdges);
            viewModel.TornShadow = false;
            viewModel.ApplyPreset(viewModel.Presets.First(preset => preset.Name == Greenshot.Base.Languages.Texts.Editor.EffectsPresetBottom));
            Assert.False(viewModel.ChangesTornEdges);
            int seed = ((TornEdgeEffect)viewModel.CreateEffect()).Seed;

            // Torn edges are added without the thread pool, like the elements are
            await viewModel.ApplyAsync();
            var tornEdges = surface.Elements.OfType<TornEdgeContainer>().Single();
            Assert.Equal(seed, tornEdges.Seed);
            Assert.Equal(new[] { false, false, true, false }, tornEdges.Edges);
            Assert.True(viewModel.ChangesTornEdges);

            // Torn edges which are there are changed, not added again
            viewModel.TornTop = true;
            await viewModel.ApplyAsync();
            Assert.Single(surface.Elements.OfType<TornEdgeContainer>());
            Assert.Equal(new[] { true, false, true, false }, tornEdges.Edges);

            surface.Undo();
            Assert.Equal(new[] { false, false, true, false }, tornEdges.Edges);
        }

        [Fact]
        public async Task TornEdgesPreview_HasTheRoomForTheShadow()
        {
            using var surface = CreateSurface();
            var settings = new TornEdgeEffect { GenerateShadow = true, ShadowSize = 7, ShadowOffset = new Dapplo.Windows.Common.Structs.NativePoint(-1, -1) };
            int margin = TornEdgeContainer.GetShadowMargin(settings);

            using var preview = await Task.Run(() => surface.CreateTornEdgesPreviewAsync(settings));

            Assert.Equal(new Size(200 + 2 * margin, 100 + 2 * margin), preview.Size);
            Assert.Equal(new Size(200, 100), surface.Image.Size);
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
            using var surface = new Surface(new Bitmap(20, 20, System.Drawing.Imaging.PixelFormat.Format24bppRgb));
            using var viewModel = CreateViewModel(surface);
            viewModel.Show(EffectKind.RemoveTransparency);
            Assert.False(viewModel.CanApply);
            Assert.True(viewModel.StatusIsError);
        }
    }
}
