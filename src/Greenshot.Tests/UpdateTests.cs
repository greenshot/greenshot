// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#if !NETCOREAPP3_0
using System;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using Dapplo.HttpExtensions;
using Greenshot.Components;
using Xunit;

namespace Greenshot.Tests
{
    public class UpdateTests
    {
        [Fact]
        public async Task UpdateTest_GetFeed()
        {
            var updateFeed = await new Uri("http://getgreenshot.org/project-feed/").GetAsAsync<SyndicationFeed>();
            Assert.True(updateFeed.Links.Count >0);
        }

        [Fact]
        public void UpdateTest()
        {
            var updateService = new UpdateService(null, null, null);
            using (var reader = XmlReader.Create(@"TestFiles\project-feed.xml"))
            {
                var feed = SyndicationFeed.Load(reader);
                updateService.ProcessFeed(feed);
                Assert.Equal(new Version("1.2.10.6"), updateService.LatestVersion);
                Assert.Equal(new Version("1.3.0.0"), updateService.BetaVersion);
                Assert.Equal(new Version("1.2.20.99"), updateService.ReleaseCandidateVersion);
            }
        }
    }
}
#endif