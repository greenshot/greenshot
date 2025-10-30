// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
//
// For more information see: https://getgreenshot.org/
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
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.JsonNet;
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Configuration;
using Greenshot.Helpers.Entities;
using log4net;

namespace Greenshot.Helpers
{
    /// <summary>
    ///     This processes the information, if there are updates available.
    /// </summary>
    public class UpdateService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(UpdateService));
        private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();
        private static readonly Uri UpdateFeed = new Uri("https://getgreenshot.org/update-feed.json");
        private static readonly Uri Downloads = new Uri("https://getgreenshot.org/downloads");
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Provides the current version
        /// </summary>
        public Version CurrentVersion { get; }

        /// <summary>
        /// Provides the latest known version
        /// </summary>
        public Version LatestReleaseVersion { get; private set; }

        /// <summary>
        /// The latest beta version
        /// </summary>
        public Version LatestBetaVersion { get; private set; }

        /// <summary>
        /// Checks if there is an release update available
        /// </summary>
        public bool IsUpdateAvailable => LatestReleaseVersion > CurrentVersion;

        /// <summary>
        /// Checks if there is an beta update available
        /// </summary>
        public bool IsBetaUpdateAvailable => LatestBetaVersion > CurrentVersion;

        /// <summary>
        /// Keep track of when the update was shown, so it won't be every few minutes
        /// </summary>
        public DateTimeOffset LastUpdateShown = DateTimeOffset.MinValue;

        /// <summary>
        /// Constructor with dependencies
        /// </summary>
        public UpdateService()
        {
            JsonNetJsonSerializer.RegisterGlobally();
            var version = FileVersionInfo.GetVersionInfo(GetType().Assembly.Location);
            LatestReleaseVersion = CurrentVersion = new Version(version.FileMajorPart, version.FileMinorPart, version.FileBuildPart);
            CoreConfig.LastSaveWithVersion = CurrentVersion.ToString();
        }

        /// <summary>
        /// Start the background task which checks for updates
        /// </summary>
        public void Startup()
        {
            _ = BackgroundTask(() => TimeSpan.FromDays(CoreConfig.UpdateCheckInterval), UpdateCheck, _cancellationTokenSource.Token);
        }

        /// <summary>
        /// This runs a periodic task in the background
        /// </summary>
        /// <param name="intervalFactory">Func which returns a TimeSpan</param>
        /// <param name="reoccurringTask">Func which returns a task</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task</returns>
        private async Task BackgroundTask(Func<TimeSpan> intervalFactory, Func<CancellationToken, Task> reoccurringTask, CancellationToken cancellationToken = default)
        {
            // Initial delay, to make sure this doesn't happen at the startup
            await Task.Delay(20000, cancellationToken);
            Log.Info("Starting background task to check for updates");
            await Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var interval = intervalFactory();
                    var task = reoccurringTask;

                    // If the check is disabled, handle that here
                    var checkIsDisabled = TimeSpan.Zero == interval;
                    var nextCheckIsInTheFuture = CoreConfig.LastUpdateCheck.Add(interval) > DateTime.Now;

                    if (checkIsDisabled || nextCheckIsInTheFuture)
                    {
                        // Just wait for 10 minutes, maybe the configuration will change
                        interval = TimeSpan.FromMinutes(10);
                        task = c => Task.FromResult(true);
                    }

                    try
                    {
                        await task(cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error occurred when trying to check for updates.", ex);
                    }

                    try
                    {
                        await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        // Ignore, this always happens
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error occurred await for the next update check.", ex);
                    }
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Do the actual update check
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task</returns>
        private async Task UpdateCheck(CancellationToken cancellationToken = default)
        {
            Log.InfoFormat("Checking for updates from {0}", UpdateFeed);
            var updateFeed = await UpdateFeed.GetAsAsync<UpdateFeed>(cancellationToken);
            if (updateFeed == null)
            {
                return;
            }

            CoreConfig.LastUpdateCheck = DateTime.Now;
            IniConfig.Save();

            ProcessFeed(updateFeed);

            // Only show if the update was shown >24 hours ago.
            if (DateTimeOffset.Now.AddDays(-1) > LastUpdateShown)
            {
                if (IsBetaUpdateAvailable)
                {
                    LastUpdateShown = DateTimeOffset.Now;
                    ShowUpdate(LatestBetaVersion);
                }
                else if (IsUpdateAvailable)
                {
                    LastUpdateShown = DateTimeOffset.Now;
                    ShowUpdate(LatestReleaseVersion);
                }
            }
        }


        /// <summary>
        /// This takes care of creating the toast view model, publishing it, and disposing afterwards
        /// </summary>
        /// <param name="newVersion">Version</param>
        private void ShowUpdate(Version newVersion)
        {
            var notificationService = SimpleServiceProvider.Current.GetInstance<INotificationService>();
            var message = Language.GetFormattedString(LangKey.update_found, newVersion.ToString());
            notificationService.ShowInfoMessage(message, TimeSpan.FromHours(1), () => Process.Start(Downloads.AbsoluteUri));
        }

        /// <summary>
        /// Process the update feed to get the latest version
        /// </summary>
        /// <param name="updateFeed"></param>
        private void ProcessFeed(UpdateFeed updateFeed)
        {
            var latestReleaseString = Regex.Replace(updateFeed.CurrentReleaseVersion, "[a-zA-Z\\-]*", "");
            if (Version.TryParse(latestReleaseString, out var latestReleaseVersion))
            {
                LatestReleaseVersion = latestReleaseVersion;
            }

            var latestBetaString = Regex.Replace(updateFeed.CurrentBetaVersion, "[a-zA-Z\\-]*", "");
            if (Version.TryParse(latestBetaString, out var latestBetaVersion))
            {
                LatestBetaVersion = latestBetaVersion;
            }
        }
    }
}