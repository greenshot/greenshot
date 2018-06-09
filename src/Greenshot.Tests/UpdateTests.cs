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
            var updateService = new UpdateService(null, null);
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
