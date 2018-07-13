#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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

#endregion

#region Usings

using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Addons;
using Dapplo.CaliburnMicro;
using Dapplo.HttpExtensions;
using Dapplo.Log;
using Dapplo.Utils;
using Greenshot.Addons;
using Greenshot.Addons.Core;
using Greenshot.Forms;

#endregion

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
		private const string StableDownloadLink = "https://getgreenshot.org/downloads/";
		private static readonly Uri UpdateFeed = new Uri("http://getgreenshot.org/project-feed/");
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly ICoreConfiguration _coreConfiguration;
	    private readonly IGreenshotLanguage _greenshotLanguage;

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
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        public UpdateService(
            ICoreConfiguration coreConfiguration,
            IGreenshotLanguage greenshotLanguage)
	    {
	        _coreConfiguration = coreConfiguration;
	        _greenshotLanguage = greenshotLanguage;
	        LatestVersion = CurrentVersion = GetType().Assembly.GetName().Version;
	        _coreConfiguration.LastSaveWithVersion = CurrentVersion.ToString();
	    }

	    /// <inheritdoc />
	    public void Startup()
	    {
	        var ignore = BackgroundTask(() => TimeSpan.FromDays(_coreConfiguration.UpdateCheckInterval), UpdateCheck, _cancellationTokenSource.Token);
        }

	    /// <inheritdoc />
	    public void Shutdown()
	    {
	        _cancellationTokenSource.Cancel();
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
            Log.Info().WriteLine("Starting background task");
	        await Task.Run(async () =>
	        {
	            while (true)
	            {
	                var interval = intervalFactory();
	                var task = reoccurringTask;
	                if (TimeSpan.Zero == interval)
	                {
	                    interval = TimeSpan.FromMinutes(10);
	                    task = c => Task.FromResult(true);
	                }
                    try
	                {
	                    await Task.WhenAll(Task.Delay(interval), task(cancellationToken)).ConfigureAwait(false);
                    }
	                catch (Exception ex)
	                {
                        Log.Error().WriteLine(ex, "Error occured when trying to check for updates.");
	                }
                    if (cancellationToken.IsCancellationRequested)
	                {
	                    break;
                    }
	            }
	        }, cancellationToken).ConfigureAwait(false);
	        Log.Info().WriteLine("Finished background task");
        }

        /// <summary>
        /// Do the actual update check
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task</returns>
	    private async Task UpdateCheck(CancellationToken cancellationToken = default)
	    {
	        Log.Info().WriteLine("Checking for updates");
	        var updateFeed = await UpdateFeed.GetAsAsync<SyndicationFeed>(cancellationToken);
	        if (updateFeed == null)
	        {
	            return;
	        }
            _coreConfiguration.LastUpdateCheck = DateTime.Now;

            ProcessFeed(updateFeed);
	        
	        if (IsUpdateAvailable)
	        {
	            await UiContext.RunOn(() =>
	            {
	                // TODO: Show update more nicely...
	                MainForm.Instance.NotifyIcon.BalloonTipClicked += HandleBalloonTipClick;
	                MainForm.Instance.NotifyIcon.BalloonTipClosed += CleanupBalloonTipClick;
	                MainForm.Instance.NotifyIcon.ShowBalloonTip(10000, "Greenshot", string.Format(_greenshotLanguage.UpdateFound, LatestVersion), ToolTipIcon.Info);
                }, cancellationToken).ConfigureAwait(false);
            }
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
                Regex rcReg = new Regex(@"^rc\d*$",RegexOptions.IgnoreCase);
	            if (rcReg.IsMatch(type))
                {
	                ReleaseCandidateVersion = version;
	            }
            }
        }

        /// <summary>
        /// Remove the event handlers
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">EventArgs</param>
		private void CleanupBalloonTipClick(object sender, EventArgs e)
		{
			MainForm.Instance.NotifyIcon.BalloonTipClicked -= HandleBalloonTipClick;
			MainForm.Instance.NotifyIcon.BalloonTipClosed -= CleanupBalloonTipClick;
		}

        /// <summary>
        /// Handle the click on a balloon
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">EventArgs</param>
		private void HandleBalloonTipClick(object sender, EventArgs e)
		{
			try
			{
				// "Direct" download link
				// Process.Start(latestGreenshot.Link);
				// Go to getgreenshot.org
				Process.Start(StableDownloadLink);
			}
			catch (Exception)
			{
				MessageBox.Show(string.Format(_greenshotLanguage.ErrorOpenlink, StableDownloadLink), _greenshotLanguage.Error);
			}
			finally
			{
				CleanupBalloonTipClick(sender, e);
			}
		}
	}
}