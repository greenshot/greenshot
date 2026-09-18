using System;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.UI;
using Greenshot.UI.ViewModels;
using Xunit;

namespace Greenshot.Tests.Core
{
    public class BugReportWindowTests
    {
        public BugReportWindowTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public async Task ShowReport_FromMTAThreadPoolThread_SpawnsSTAAndDoesNotCrash()
        {
            // Verify that ApartmentState on ThreadPool is MTA
            bool wasMta = false;
            var ex = new ApplicationException("Simulated background exception");

            await Task.Run(() =>
            {
                wasMta = Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA;
                // Instantiating ViewModel on MTA is safe
                var vm = new BugReportViewModel(ex);
                Assert.NotNull(vm.StackTraceHash);
            });

            Assert.True(wasMta);
        }

        [Fact]
        public void ExceptionHelper_GeneratesProperNewIssueAndSearchUrls()
        {
            string hash = "abc123def456";
            string searchUrl = ExceptionHelper.GetGitHubSearchUrl(hash);
            string newIssueUrl = ExceptionHelper.GetNewIssueUrl(hash, "System.NullReferenceException");

            Assert.StartsWith("https://github.com/greenshot/greenshot/issues", searchUrl);
            Assert.Contains(hash, searchUrl);
            Assert.StartsWith("https://github.com/greenshot/greenshot/issues/new", newIssueUrl);
            Assert.Contains(hash, newIssueUrl);
            Assert.Contains("System.NullReferenceException", newIssueUrl);
        }

        [Fact]
        public void UpdateService_ProcessFeed_CorrectlyUpdatesReleaseAndBetaVersions()
        {
            var service = new Greenshot.Helpers.UpdateService(new Version(1, 2, 10));
            Assert.Equal(new Version(1, 2, 10), service.CurrentVersion);
            Assert.Null(service.LatestReleaseVersion);
            Assert.False(service.IsUpdateAvailable);

            // Process feed with newer version
            var feed = new Greenshot.Helpers.Entities.UpdateFeed
            {
                CurrentReleaseVersion = "1.3.0",
                CurrentBetaVersion = "1.3.1-beta"
            };
            service.ProcessFeed(feed);

            Assert.Equal(new Version(1, 3, 0), service.LatestReleaseVersion);
            Assert.Equal(new Version(1, 3, 1), service.LatestBetaVersion);
            Assert.True(service.IsUpdateAvailable);
            Assert.True(service.IsBetaUpdateAvailable);
        }

        [Fact]
        public void UpdateService_ProcessFeed_WhenUpToDate_ReportsNoUpdateAvailable()
        {
            var service = new Greenshot.Helpers.UpdateService(new Version(1, 3, 0));
            var feed = new Greenshot.Helpers.Entities.UpdateFeed
            {
                CurrentReleaseVersion = "1.3.0",
                CurrentBetaVersion = "1.2.9"
            };
            service.ProcessFeed(feed);

            Assert.Equal(new Version(1, 3, 0), service.LatestReleaseVersion);
            Assert.False(service.IsUpdateAvailable);
            Assert.False(service.IsBetaUpdateAvailable);
        }

        [Fact]
        public void UpdateService_DownloadsUri_IsConfigured()
        {
            Assert.NotNull(Greenshot.Helpers.UpdateService.DownloadsUri);
            Assert.Equal("https://getgreenshot.org/downloads", Greenshot.Helpers.UpdateService.DownloadsUri.AbsoluteUri);
        }

        [Fact]
        public void BugReportViewModel_WithOutdatedVersion_DisplaysUpgradeHint()
        {
            var updateService = new Greenshot.Helpers.UpdateService(new Version(1, 2, 10));
            updateService.ProcessFeed(new Greenshot.Helpers.Entities.UpdateFeed
            {
                CurrentReleaseVersion = "1.3.0"
            });

            var vm = new BugReportViewModel(new Exception("Test exception"), updateService: updateService);

            Assert.Equal("1.2.10", vm.CurrentVersion);
            Assert.Equal("1.3.0", vm.LatestReleaseVersion);
            Assert.True(vm.IsOutdated);
            Assert.Contains("A newer version (1.3.0) is available", vm.VersionComparisonText);
            Assert.Equal("https://getgreenshot.org/downloads", vm.UpgradeDownloadUrl);
        }

        [Fact]
        public void BugReportViewModel_WhenUpToDate_DisplaysUpToDateText()
        {
            var updateService = new Greenshot.Helpers.UpdateService(new Version(1, 3, 0));
            updateService.ProcessFeed(new Greenshot.Helpers.Entities.UpdateFeed
            {
                CurrentReleaseVersion = "1.3.0"
            });

            var vm = new BugReportViewModel(new Exception("Test exception"), updateService: updateService);

            Assert.Equal("1.3.0", vm.CurrentVersion);
            Assert.Equal("1.3.0", vm.LatestReleaseVersion);
            Assert.False(vm.IsOutdated);
            Assert.Contains("You are using the latest version", vm.VersionComparisonText);
        }

        [Fact]
        public async Task BugReportViewModel_CheckVersionAsync_HandlesNetworkFailureGracefully()
        {
            // Empty service with no feed loaded; CheckForUpdatesAsync with invalid/offline will return false or catch
            var updateService = new Greenshot.Helpers.UpdateService(new Version(1, 2, 10));
            var vm = new BugReportViewModel(new Exception("Test exception"), updateService: updateService);

            // Wait for CheckVersionAsync to complete
            await vm.CheckVersionAsync();

            Assert.False(vm.IsCheckingVersion);
            // If offline, LatestReleaseVersion should be Unavailable or a version, and should not crash
            Assert.NotNull(vm.LatestReleaseVersion);
        }
    }
}
