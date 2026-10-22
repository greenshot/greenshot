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
using System.Linq;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Plugin;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// What was found on the screen (QR codes, text lines) and is outside the selection doesn't belong to the capture
    /// </summary>
    public class CaptureFeaturesCropTests
    {
        private static DetectedOcrLine Line(int x, int y, string text) => new DetectedOcrLine(new NativeRect(x, y, 40, 20), text, new List<OcrWordInfo>());

        private static List<string> Texts(CaptureDetails details) => details.Features.Select(f => f.Text).OrderBy(t => t).ToList();

        [Fact]
        public void CropFeatures_KeepsWhatIsInTheSelection_AtItsNewPlace()
        {
            var details = new CaptureDetails();
            details.Features.Add(Line(110, 110, "inside"));
            details.Features.Add(Line(10, 10, "outside"));
            details.Features.Add(Line(285, 110, "mostly outside"));

            details.CropFeatures(new NativeRect(100, 100, 200, 100));

            Assert.Equal(new[] { "inside" }, Texts(details));
            Assert.Equal(new NativePoint(10, 10), details.Features[0].Bounds.Location);
            Assert.Equal(new NativeRect(100, 100, 200, 100), details.CropArea);
        }

        [Fact]
        public void AddFeatures_FoundBeforeTheCrop_AreMovedAndFiltered()
        {
            var details = new CaptureDetails();
            var cropAreaWhenFound = details.CropArea;
            details.CropFeatures(new NativeRect(100, 100, 200, 100));
            details.CropFeatures(new NativeRect(10, 10, 100, 50));

            details.AddFeatures(new IDetectedFeature[] { Line(120, 120, "inside"), Line(10, 10, "outside") }, cropAreaWhenFound);

            Assert.Equal(new[] { "inside" }, Texts(details));
            Assert.Equal(new NativePoint(10, 10), details.Features[0].Bounds.Location);
            Assert.Equal(new NativeRect(110, 110, 100, 50), details.CropArea);
        }

        [Fact]
        public void AddFeatures_WithoutACrop_AddsEverything()
        {
            var details = new CaptureDetails();

            details.AddFeatures(new IDetectedFeature[] { Line(5000, 5000, "far"), Line(0, 0, "near") }, details.CropArea);

            Assert.Equal(new[] { "far", "near" }, Texts(details));
        }
    }
}
