// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using Dapplo.Ini;
using Greenshot.Base.Interfaces;
using Greenshot.Configuration;
using Greenshot.Helpers.Entities;
using log4net;

namespace Greenshot.Helpers
{
    /// <summary>
    ///     This processes the information, if there are updates available.
    /// </summary>
    public class UpdateService : IDisposable
    {
        private bool _disposed;
        private readonly object _lifecycleLock = new object();
        private static readonly ILog Log = LogManager.GetLogger(typeof(UpdateService));
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();
        private static readonly Uri UpdateFeed = new Uri("https://getgreenshot.org/update-feed.json");
        private static readonly Uri Downloads = new Uri("https://getgreenshot.org/downloads");
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// URI pointing to the Greenshot downloads webpage
        /// </summary>
        public static Uri DownloadsUri => Downloads;

        /// <summary>
        /// Instance property for downloads URI
        /// </summary>
        public Uri DownloadsUrl => Downloads;

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
        public bool IsUpdateAvailable => LatestReleaseVersion != null && LatestReleaseVersion > CurrentVersion;

        /// <summary>
        /// Checks if there is an beta update available
        /// </summary>
        public bool IsBetaUpdateAvailable => LatestBetaVersion != null && LatestBetaVersion > CurrentVersion;

        /// <summary>
        /// Indicates whether the background update check task is currently running.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Keep track of when the update was shown, so it won't be every few minutes
        /// </summary>
        public DateTimeOffset LastUpdateShown = DateTimeOffset.MinValue;

        /// <summary>
        /// Constructor with dependencies
        /// </summary>
        public UpdateService() : this(null)
        {
        }

        /// <summary>
        /// Constructor allowing explicit CurrentVersion (useful for unit testing and dependency injection)
        /// </summary>
        /// <param name="currentVersion">Current version override</param>
        public UpdateService(Version currentVersion)
        {
            JsonNetJsonSerializer.RegisterGlobally();

            if (currentVersion != null)
            {
                CurrentVersion = currentVersion;
            }
            else
            {
                try
                {
                    var location = GetType().Assembly.Location;
                    if (!string.IsNullOrEmpty(location))
                    {
                        var version = FileVersionInfo.GetVersionInfo(location);
                        CurrentVersion = new Version(version.FileMajorPart, version.FileMinorPart, version.FileBuildPart);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn("Could not determine current version from assembly file info", ex);
                }

                if (CurrentVersion == null)
                {
                    var ver = GetType().Assembly.GetName().Version;
                    CurrentVersion = ver != null ? new Version(ver.Major, ver.Minor, Math.Max(0, ver.Build)) : new Version(1, 0, 0);
                }
            }
        }

        /// <summary>
        /// Start the background task which checks for updates
        /// </summary>
        public void Startup()
        {
            lock (_lifecycleLock)
            {
                if (IsRunning)
                {
                    return;
                }

                if (_disposed)
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                    _disposed = false;
                }

                IsRunning = true;
                var interval = CoreConfig?.UpdateCheckInterval ?? 14;
                _ = BackgroundTask(() => TimeSpan.FromDays(interval), ct => CheckForUpdatesAsync(true, ct), _cancellationTokenSource.Token);
            }
        }

        /// <summary>
        /// Cancels the background task and releases resources.
        /// </summary>
        public void Dispose()
        {
            lock (_lifecycleLock)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
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
            try
            {
                // Initial delay, to make sure this doesn't happen at the startup
                await Task.Delay(20000, cancellationToken).ConfigureAwait(false);
                Log.Info("Starting background task to check for updates");
                await Task.Run(async () =>
                {
                    try
                    {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            var interval = intervalFactory();
                            var task = reoccurringTask;

                            // If the check is disabled, handle that here
                            var checkIsDisabled = TimeSpan.Zero == interval;
                            var nextCheckIsInTheFuture = CoreConfig != null && CoreConfig.LastUpdateCheck.Add(interval) > DateTime.Now;

                            // If we have an invalid interval
                            if (interval.TotalSeconds < 0)
                            {
                                // Just wait for longer time, maybe the configuration will change
                                interval = TimeSpan.FromDays(1);
                            }

                            if (checkIsDisabled || nextCheckIsInTheFuture)
                            {
                                // Just wait for 30 minutes, maybe the configuration will change
                                interval = TimeSpan.FromMinutes(30);
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
                                // Use duration to get an absolute time and can't be negative.
                                await Task.Delay(interval.Duration(), cancellationToken).ConfigureAwait(false);
                            }
                            catch (TaskCanceledException)
                            {
                                // Ignore, this always happens
                            }
                            catch (Exception ex)
                            {
                                Log.Error("Error occurred await for the next background interval check.", ex);
                                // Safety pause, to avoid a potential tight loop if something is really wrong with the configuration or the update feed.
                                await Task.Delay(TimeSpan.FromDays(1), cancellationToken).ConfigureAwait(false);
                            }
                        }
                    }
                    finally
                    {
                        Log.Info("Stopping background task to check for updates");
                    }
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            finally
            {
                lock (_lifecycleLock)
                {
                    IsRunning = false;
                }
            }
        }

        /// <summary>
        /// Check for updates asynchronously from the Greenshot update feed.
        /// </summary>
        /// <param name="showNotification">Whether to show a toast notification if an update is found.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the update check completed successfully, false if an error occurred (e.g. offline).</returns>
        public async Task<bool> CheckForUpdatesAsync(bool showNotification = false, CancellationToken cancellationToken = default)
        {
            Log.InfoFormat("Checking for updates from {0}", UpdateFeed);

            try
            {
                if (CoreConfig != null)
                {
                    CoreConfig.LastUpdateCheck = DateTime.Now;
                }

                var updateFeed = await UpdateFeed.GetAsAsync<UpdateFeed>(cancellationToken).ConfigureAwait(false);
                if (updateFeed == null)
                {
                    return false;
                }

                ProcessFeed(updateFeed);
            }
            catch (Exception ex)
            {
                Log.Warn("Error occurred when checking for updates.", ex);
                return false;
            }

            if (showNotification && DateTimeOffset.Now.AddDays(-1) > LastUpdateShown)
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

            return true;
        }

        /// <summary>
        /// This takes care of creating the toast view model, publishing it, and disposing afterwards
        /// </summary>
        /// <param name="newVersion">Version</param>
        private void ShowUpdate(Version newVersion)
        {
            try
            {
                var notificationService = SimpleServiceProvider.Current?.GetInstance<INotificationService>(isOptional: true);
                if (notificationService == null) return;

                var message = Language.GetFormattedString(LangKey.update_found, newVersion.ToString());
                notificationService.ShowInfoMessage(message, TimeSpan.FromHours(1), () =>
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(Downloads.AbsoluteUri) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Failed to launch download URL: {Downloads.AbsoluteUri}", ex);
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Warn("Could not display update notification.", ex);
            }
        }

        /// <summary>
        /// Process the update feed to get the latest version
        /// </summary>
        /// <param name="updateFeed">Update feed entity</param>
        public void ProcessFeed(UpdateFeed updateFeed)
        {
            if (updateFeed == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(updateFeed.CurrentReleaseVersion))
            {
                var latestReleaseString = Regex.Replace(updateFeed.CurrentReleaseVersion, "[a-zA-Z\\-]*", "");
                if (Version.TryParse(latestReleaseString, out var latestReleaseVersion))
                {
                    LatestReleaseVersion = latestReleaseVersion;
                }
            }

            if (!string.IsNullOrEmpty(updateFeed.CurrentBetaVersion))
            {
                var latestBetaString = Regex.Replace(updateFeed.CurrentBetaVersion, "[a-zA-Z\\-]*", "");
                if (Version.TryParse(latestBetaString, out var latestBetaVersion))
                {
                    LatestBetaVersion = latestBetaVersion;
                }
            }
        }
    }
}