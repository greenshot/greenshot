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

using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.OwnedInstances;
using Caliburn.Micro;
using Dapplo.Addons;
using Dapplo.CaliburnMicro;
using Dapplo.HttpExtensions;
using Dapplo.Log;
using Greenshot.Addons.Core;
using Greenshot.Ui.Notifications.ViewModels;

namespace Greenshot.Components
{
    /// <summary>
    ///     This processes the information, if there are updates available.
    /// </summary>
    [Service(nameof(UpdateService), nameof(MainFormStartup))]
    public class UpdateService : IStartup, IShutdown, IVersionProvider
    {
        private static readonly LogSource Log = new LogSource();
        private static readonly Regex VersionRegex = new Regex(@"^.*[^-]-(?<version>[0-9\.]+)\-(?<type>(release|beta|rc[0-9]+))\.exe.*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Uri UpdateFeed = new Uri("https://getgreenshot.org/project-feed/");
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly ICoreConfiguration _coreConfiguration;
        private readonly IEventAggregator _eventAggregator;
        private readonly Func<Version, Owned<UpdateNotificationViewModel>> _updateNotificationViewModelFactory;

        /// <inheritdoc />
        public Version CurrentVersion { get; }

        /// <inheritdoc />
        public Version LatestVersion { get; private set; }

        /// <summary>
        /// The latest beta version
        /// </summary>
        public Version BetaVersion { get; private set; }

        /// <summary>
        /// The latest RC version
        /// </summary>
        public Version ReleaseCandidateVersion { get; private set; }

        /// <inheritdoc />
        public bool IsUpdateAvailable => LatestVersion > CurrentVersion;

        /// <summary>
        /// Constructor with dependencies
        /// </summary>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="eventAggregator">IEventAggregator</param>
        /// <param name="updateNotificationViewModelFactory">UpdateNotificationViewModel factory</param>
        public UpdateService(
            ICoreConfiguration coreConfiguration,
            IEventAggregator eventAggregator,
            Func<Version, Owned<UpdateNotificationViewModel>> updateNotificationViewModelFactory)
        {
            _coreConfiguration = coreConfiguration;
            _eventAggregator = eventAggregator;
            _updateNotificationViewModelFactory = updateNotificationViewModelFactory;
            var version = FileVersionInfo.GetVersionInfo(GetType().Assembly.Location);
            LatestVersion = CurrentVersion = new Version(version.FileMajorPart, version.FileMinorPart, version.FileBuildPart);
            if (_coreConfiguration != null)
            {
                _coreConfiguration.LastSaveWithVersion = CurrentVersion.ToString();
            }
        }

        /// <inheritdoc />
        public void Startup()
        {
            _ = BackgroundTask(() => TimeSpan.FromDays(_coreConfiguration.UpdateCheckInterval), UpdateCheck, _cancellationTokenSource.Token);
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
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
            // Initial delay, to make sure this doesn't happen at the startup
            await Task.Delay(20000, cancellationToken);
            Log.Info().WriteLine("Starting background task to check for updates");
            await Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var interval = intervalFactory();
                    var task = reoccurringTask;
                    // If the check is disabled, handle that here
                    if (TimeSpan.Zero == interval)
                    {
                        interval = TimeSpan.FromMinutes(10);
                        task = c => Task.FromResult(true);
                    }

                    try
                    {
                        await task(cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Log.Error().WriteLine(ex, "Error occured when trying to check for updates.");
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
                        Log.Error().WriteLine(ex, "Error occured await for the next update check.");
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
            Log.Info().WriteLine("Checking for updates from {0}", UpdateFeed);
            var updateFeed = await UpdateFeed.GetAsAsync<SyndicationFeed>(cancellationToken);
            if (updateFeed == null)
            {
                return;
            }

            if (_coreConfiguration != null)
            {
                _coreConfiguration.LastUpdateCheck = DateTime.Now;
            }

            ProcessFeed(updateFeed);
            
            if (IsUpdateAvailable)
            {
                ShowUpdate(LatestVersion);
            }
        }


        /// <summary>
        /// This takes care of creating the toast view model, publishing it, and disposing afterwards
        /// </summary>
        private void ShowUpdate(Version latestVersion)
        {
            // Create the ViewModel "part"
            var message = _updateNotificationViewModelFactory(latestVersion);
            // Prepare to dispose the view model parts automatically if it's finished
            void DisposeHandler(object sender, DeactivationEventArgs args)
            {
                message.Value.Deactivated -= DisposeHandler;
                message.Dispose();
            }

            message.Value.Deactivated += DisposeHandler;

            // Show the ViewModel as toast 
            _eventAggregator.PublishOnUIThread(message.Value);
        }

        /// <summary>
        /// Process the update feed to get the latest version
        /// </summary>
        /// <param name="updateFeed"></param>
        public void ProcessFeed(SyndicationFeed updateFeed)
        {
            var versions =
                from link in updateFeed.Items.SelectMany(i => i.Links)
                select VersionRegex.Match(link.Uri.AbsoluteUri) into match
                where match.Success
                group match by Regex.Replace(match.Groups["type"].Value, @"[\d-]", string.Empty) into groupedVersions
                select groupedVersions.OrderByDescending(m => new Version(m.Groups["version"].Value)).First();
         
            foreach (var versionMatch in versions)
            {
                var version = new Version(versionMatch.Groups["version"].Value);
                var type = versionMatch.Groups["type"].Value;
                if (string.IsNullOrEmpty(type))
                {
                    continue;
                }
                Log.Debug().WriteLine("Got {0} {1}", type, version);
                if ("release".Equals(type, StringComparison.OrdinalIgnoreCase))
                {
                    LatestVersion = version;
                }
                if ("beta".Equals(type, StringComparison.OrdinalIgnoreCase))
                {
                    BetaVersion = version;
                }
                
                if (type.StartsWith("rc", StringComparison.OrdinalIgnoreCase))
                {
                    ReleaseCandidateVersion = version;
                }
            }
        }
    }
}