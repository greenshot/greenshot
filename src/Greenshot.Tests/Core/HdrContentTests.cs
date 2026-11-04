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

using System.Collections.Generic;
using System.Drawing;
using Greenshot.Base.Core;
using Greenshot.Base.Hdr;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Recipes.Steps;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// A capture on an HDR display has HDR content only with something an 8-bit sRGB file can't show: a highlight or a color outside sRGB.
    /// Without it (a normal window) it counts as a capture without HDR content: no HDR file, no badge, payload.hdr is None.
    /// </summary>
    public class HdrContentTests
    {
        public HdrContentTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        /// <summary>
        /// An HDR image of 4x2 pixels at SDR white (240 nits is 3.0), with the pixel at 1,1 as given
        /// </summary>
        private static HdrImage CreateHdrImage(HalfVector4 pixel)
        {
            var hdrImage = new HdrImage(4, 2, 240, 400);
            hdrImage.Pixels.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < 2; y++)
                {
                    accessor.GetRowSpan(y).Fill(new HalfVector4(3, 3, 3, 1));
                }
            });
            hdrImage.Pixels[1, 1] = pixel;
            return hdrImage;
        }

        [Fact]
        public void SdrWhite_IsNoHdrContent()
        {
            using var hdrImage = CreateHdrImage(new HalfVector4(0.5f, 0.2f, 0, 1));

            Assert.False(hdrImage.HasHdrContent);
        }

        [Fact]
        public void Highlight_IsHdrContent()
        {
            using var hdrImage = CreateHdrImage(new HalfVector4(5, 3, 3, 1));

            Assert.True(hdrImage.HasHdrContent);
        }

        [Fact]
        public void ColorOutsideSrgb_IsHdrContent_AlsoBelowSdrWhite()
        {
            // A saturated green of P3: a negative red in scRGB
            using var hdrImage = CreateHdrImage(new HalfVector4(-0.2f, 0.8f, -0.05f, 1));

            Assert.True(hdrImage.HasHdrContent);
        }

        [Fact]
        public void TransparentHighlight_IsNoHdrContent()
        {
            using var hdrImage = CreateHdrImage(new HalfVector4(5, 5, 5, 0));

            Assert.False(hdrImage.HasHdrContent);
        }

        [Fact]
        public void HighlightOnOneOfTheHdrDisplays_IsHdrContent()
        {
            using var left = CreateHdrImage(new HalfVector4(1, 1, 1, 1));
            using var right = CreateHdrImage(new HalfVector4(9, 9, 9, 1));
            using var sdr = new System.Drawing.Bitmap(12, 2, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using var both = HdrImage.Combine(sdr, new[] { (left, new Dapplo.Windows.Common.Structs.NativePoint(0, 0)), (right, new Dapplo.Windows.Common.Structs.NativePoint(8, 0)) });
            using var leftOnly = both.Crop(new Dapplo.Windows.Common.Structs.NativeRect(0, 0, 6, 2));

            Assert.True(both.HasHdrContent);
            Assert.False(leftOnly.HasHdrContent);
        }

        [Fact]
        public void PayloadHdr_OfAnHdrCaptureWithoutHighlights_IsNone()
        {
            var capture = new Capture(new Bitmap(4, 2));
            var hdrImage = CreateHdrImage(new HalfVector4(1, 1, 1, 1));
            hdrImage.BelongTo(capture.Image);
            capture.HdrImage = hdrImage;

            Assert.Equal(HdrOutput.StateNone, HdrOutput.StateOf(new CapturePayload(capture)));
        }

        [Theory]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        [InlineData(false, false, false)]
        public void NoHdrNote_OnlyWhenAskedFor_AndWithoutHdrContent(bool noHdrNote, bool withHighlight, bool noted)
        {
            var capture = new Capture(new Bitmap(4, 2));
            var hdrImage = CreateHdrImage(withHighlight ? new HalfVector4(5, 5, 5, 1) : new HalfVector4(1, 1, 1, 1));
            hdrImage.BelongTo(capture.Image);
            capture.HdrImage = hdrImage;
            var step = new NotificationStep(new RecipeNodeConfig("notify", WellKnownStepTypes.Notification)
            {
                Parameters = new Dictionary<string, object> { [NotificationStep.NoHdrNoteParameter] = noHdrNote }
            });
            using var context = new CaptureFlowContext(new CaptureRecipe("note_test", "Note Test")) { Payload = new CapturePayload(capture) };

            Assert.Equal(noted ? Texts.Core.NotificationNoHdr : null, step.GetNote(context));
        }
    }
}
