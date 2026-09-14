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

using System.Drawing;
using Greenshot.Base.Core;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    public class PayloadAndSurfaceCloningTests
    {
        public PayloadAndSurfaceCloningTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void Surface_Clone_CreatesIndependentDeepCopy()
        {
            using var bmp = new Bitmap(200, 200);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Blue);
            }

            var capture = new Capture((Image)bmp.Clone());
            capture.CaptureDetails.Title = "Original Window";
            var payload = new CapturePayload(capture);

            var surface = payload.EnsureSurface();
            surface.AddTextContainer("Sample Text", 10, 10, new FontFamily("Arial"), 12f, false, true, false, 0, Color.Red, Color.Transparent);

            Assert.Single(surface.Elements);

            using var clonedPayload = payload.Clone();
            Assert.NotNull(clonedPayload.Surface);
            Assert.False(ReferenceEquals(payload.Surface, clonedPayload.Surface));
            Assert.Single(clonedPayload.Surface.Elements);
            Assert.False(ReferenceEquals(payload.Surface.Image, clonedPayload.Surface.Image));

            // Mutate clone
            clonedPayload.Surface.AddTextContainer("Second Text on Clone", 30, 30, new FontFamily("Arial"), 12f, false, true, false, 0, Color.Green, Color.Transparent);

            Assert.Single(payload.Surface.Elements);
            Assert.Equal(2, clonedPayload.Surface.Elements.Count);
        }

        [Fact]
        public void CaptureFlowContext_CreateBranchContext_ProvidesIsolatedState()
        {
            var recipe = new CaptureRecipe("branch_iso", "Branch Isolation");
            using var bmp = new Bitmap(100, 100);
            var capture = new Capture((Image)bmp.Clone());

            using var parentContext = new CaptureFlowContext(recipe)
            {
                Payload = new CapturePayload(capture)
            };
            parentContext.Properties["CommonKey"] = "OriginalValue";
            parentContext.Payload.EnsureSurface();

            using var branchContext = parentContext.CreateBranchContext();

            Assert.False(ReferenceEquals(parentContext, branchContext));
            Assert.False(ReferenceEquals(parentContext.Payload, branchContext.Payload));
            Assert.False(ReferenceEquals(parentContext.Payload.Surface, branchContext.Payload.Surface));
            Assert.Equal("OriginalValue", branchContext.Properties["CommonKey"]);

            // Mutate branch
            branchContext.Properties["BranchSpecific"] = "Branch123";
            branchContext.Properties["CommonKey"] = "MutatedValue";

            Assert.False(parentContext.Properties.ContainsKey("BranchSpecific"));
            Assert.Equal("OriginalValue", parentContext.Properties["CommonKey"]);
            Assert.Equal("MutatedValue", branchContext.Properties["CommonKey"]);
        }
    }
}
