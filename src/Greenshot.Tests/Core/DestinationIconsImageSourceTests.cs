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

using System.Threading.Tasks;
using Dapplo.Windows.Shell32.Enums;
using Greenshot.Base.Core;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// The built in icons are converted to a WPF image source once, the picker gets the same frozen source every time it opens
    /// </summary>
    public class DestinationIconsImageSourceTests
    {
        public DestinationIconsImageSourceTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public async Task ResourceIcon_IsTheSameSourceEachTime()
        {
            string key = DestinationIcons.Resource("Close.Image");

            var first = await DestinationIcons.GetImageSourceAsync(key);
            var second = await DestinationIcons.GetImageSourceAsync(key);

            Assert.NotNull(first);
            Assert.True(first.IsFrozen);
            Assert.Same(first, second);
        }

        [Fact]
        public async Task StockIcon_IsTheSameSourceEachTime()
        {
            string key = DestinationIcons.Stock(StockIconId.Find);

            var first = await DestinationIcons.GetImageSourceAsync(key);
            var second = await DestinationIcons.GetImageSourceAsync(key);

            Assert.NotNull(first);
            Assert.Same(first, second);
        }

        [Fact]
        public async Task UnknownKey_IsNull()
        {
            Assert.Null(await DestinationIcons.GetImageSourceAsync("nobody-provides-this:icon"));
            Assert.Null(await DestinationIcons.GetImageSourceAsync(null));
        }
    }
}
