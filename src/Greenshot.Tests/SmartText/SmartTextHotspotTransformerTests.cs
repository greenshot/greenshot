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

using System.Linq;
using Greenshot.Plugin.SmartText.Capturing;
using Greenshot.Plugin.SmartText.Detection;
using Xunit;

namespace Greenshot.Tests.SmartText
{
    /// <summary>
    /// A smart text item as numbered hotspot of the text tool in the capture window
    /// </summary>
    public class SmartTextHotspotTransformerTests
    {
        [Fact]
        public void WebAddress_IsANumberedHotspotOfTheTextTool_OpenFirst()
        {
            TestEnvironment.EnsureInitialized();
            var item = Assert.Single(BuiltInDetectorTests.Scan("See https://getgreenshot.org/faq/ here", BuiltInDetectors.Url));
            var transformer = new SmartTextHotspotTransformer();

            Assert.True(transformer.CanTransform(item));
            var hotspot = transformer.Transform(item);

            Assert.Equal("Text", hotspot.ToolId);
            Assert.True(hotspot.IsNumbered);
            Assert.Equal(item.Box.Bounds, hotspot.Bounds);
            Assert.Null(hotspot.Outline);
            Assert.Equal(item.Value, hotspot.ToolTipText);
            Assert.Equal(new[] { "Open", "Copy" }, hotspot.Actions.Select(action => action.Text));
        }

        [Fact]
        public void ItemWithoutAction_OnlyCopies()
        {
            TestEnvironment.EnsureInitialized();
            var item = Assert.Single(BuiltInDetectorTests.Scan("IBAN DE89 3704 0044 0532 0130 00", BuiltInDetectors.Iban));
            var hotspot = new SmartTextHotspotTransformer().Transform(item);

            Assert.Equal(new[] { "Copy" }, hotspot.Actions.Select(action => action.Text));
        }
    }
}
