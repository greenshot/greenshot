/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Config.Ini;
using Dapplo.Config.Language;
using Dapplo.Config.Support;
using Greenshot.Forms;
using GreenshotPlugin.Configuration;
using GreenshotPlugin.Core;
using GreenshotPlugin.Extensions;


namespace Greenshot.Helpers
{
	/// <summary>
	/// Description of RssFeedHelper.
	/// </summary>
	public static class UpdateHelper
	{
		private static readonly Serilog.ILogger Log = Serilog.Log.Logger.ForContext(typeof(UpdateHelper));
		private static readonly ICoreConfiguration conf = IniConfig.Current.Get<ICoreConfiguration>();
		private static readonly IGreenshotLanguage language = LanguageLoader.Current.Get<IGreenshotLanguage>();
		private const string StableDownloadLink = "http://getgreenshot.org/downloads/";
		private const string VersionHistoryLink = "http://getgreenshot.org/version-history/";
		private static readonly AsyncLock _asyncLock = new AsyncLock();
		private static SourceforgeFile _latestGreenshot;
		private static SourceforgeFile _currentGreenshot;
		private static string _downloadLink = StableDownloadLink;

		/// <summary>
		/// Is an update check needed?
		/// </summary>
		/// <returns>bool true if yes</returns>
		public static async Task<bool> IsUpdateCheckNeeded()
		{
			using (await _asyncLock.LockAsync().ConfigureAwait(false))
			{
				if (conf.UpdateCheckInterval == 0)
				{
					return false;
				}
				if (conf.LastUpdateCheck != default(DateTimeOffset))
				{
					var checkTime = conf.LastUpdateCheck;
					checkTime = checkTime.AddDays(conf.UpdateCheckInterval);
					if (DateTimeOffset.Now.CompareTo(checkTime) < 0)
					{
						Log.Debug("No need to check RSS feed for updates, feed check will be after {0}", checkTime);
						return false;
					}
					Log.Debug("Update check is due, last check was {0} check needs to be made after {1} (which is one {2} later)", conf.LastUpdateCheck, checkTime, conf.UpdateCheckInterval);
					if (!await SourceForgeHelper.IsRssModifiedAfter(conf.LastUpdateCheck).ConfigureAwait(false))
					{
						Log.Debug("RSS feed has not been updated since after {0}", conf.LastUpdateCheck);
						return false;
					}
				}
			}
			return true;
		}

		/// <summary>
		/// Read the RSS feed to see if there is a Greenshot update
		/// </summary>
		public static async Task CheckAndAskForUpdate()
		{
			using (await _asyncLock.LockAsync().ConfigureAwait(false))
			{
				Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
				// Test like this:
				// currentVersion = new Version("0.8.1.1198");

				try
				{
					_latestGreenshot = null;
					await ProcessRssInfoAsync(currentVersion).ConfigureAwait(false);
					if (_latestGreenshot != null)
					{
						MainForm.Instance.AsyncInvoke(() =>
						{
							MainForm.Instance.NotifyIcon.BalloonTipClicked += HandleBalloonTipClick;
							MainForm.Instance.NotifyIcon.BalloonTipClosed += CleanupBalloonTipClick;
							MainForm.Instance.NotifyIcon.ShowBalloonTip(10000, "Greenshot", string.Format(language.UpdateFound, "'" + _latestGreenshot.File + "'"), ToolTipIcon.Info);
						});
					}
					conf.LastUpdateCheck = DateTimeOffset.Now;
				}
				catch (Exception e)
				{
					Log.Error("An error occured while checking for updates, the error will be ignored: ", e);
				}
			}
		}

		private static void CleanupBalloonTipClick(object sender, EventArgs e)
		{
			MainForm.Instance.NotifyIcon.BalloonTipClicked -= HandleBalloonTipClick;
			MainForm.Instance.NotifyIcon.BalloonTipClosed -= CleanupBalloonTipClick;
		}

		private static void HandleBalloonTipClick(object sender, EventArgs e)
		{
			try
			{
				if (_latestGreenshot != null)
				{
					// "Direct" download link
					// Process.Start(latestGreenshot.Link);
					// Go to getgreenshot.org
					Process.Start(_downloadLink);
				}
			}
			catch (Exception)
			{
				MessageBox.Show(string.Format(language.ErrorOpenlink, _downloadLink), language.Error);
			}
			finally
			{
				CleanupBalloonTipClick(sender, e);
			}
		}

		private static async Task ProcessRssInfoAsync(Version currentVersion)
		{
			// Reset latest Greenshot
			var rssFiles = await SourceForgeHelper.ReadRss().ConfigureAwait(false);

			if (rssFiles == null)
			{
				return;
			}

			// Retrieve the current and latest greenshot
			foreach (string fileType in rssFiles.Keys)
			{
				foreach (string file in rssFiles[fileType].Keys)
				{
					SourceforgeFile rssFile = rssFiles[fileType][file];
					if (fileType.StartsWith("Greenshot"))
					{
						// check for exe
						if (!rssFile.IsExe)
						{
							continue;
						}

						// do we have a version?
						if (rssFile.Version == null)
						{
							Log.Debug("Skipping unversioned exe {0} which is published at {1} : {2}", file, rssFile.Pubdate.ToLocalTime(), rssFile.Link);
							continue;
						}

						// if the file is unstable, we will skip it when:
						// the current version is a release or release candidate AND check unstable is turned off.
						if (rssFile.IsUnstable)
						{
							// Skip if we shouldn't check unstables
							if ((conf.BuildState == BuildStates.RELEASE) && !conf.CheckForUnstable)
							{
								continue;
							}
						}

						// if the file is a release candidate, we will skip it when:
						// the current version is a release AND check unstable is turned off.
						if (rssFile.IsReleaseCandidate)
						{
							if (conf.BuildState == BuildStates.RELEASE && !conf.CheckForUnstable)
							{
								continue;
							}
						}

						// Compare versions
						int versionCompare = rssFile.Version.CompareTo(currentVersion);
						if (versionCompare > 0)
						{
							Log.Debug("Found newer Greenshot '{0}' with version {1} published at {2} : {3}", file, rssFile.Version, rssFile.Pubdate.ToLocalTime(), rssFile.Link);
							if (_latestGreenshot == null || rssFile.Version.CompareTo(_latestGreenshot.Version) > 0)
							{
								_latestGreenshot = rssFile;
								if (rssFile.IsReleaseCandidate || rssFile.IsUnstable)
								{
									_downloadLink = VersionHistoryLink;
								}
								else
								{
									_downloadLink = StableDownloadLink;
								}
							}
						}
						else if (versionCompare < 0)
						{
							Log.Debug("Skipping older greenshot with version {0}", rssFile.Version);
						}
						else if (versionCompare == 0)
						{
							_currentGreenshot = rssFile;
							Log.Debug("Found current version as exe {0} with version {1} published at {2} : {3}", file, rssFile.Version, rssFile.Pubdate.ToLocalTime(), rssFile.Link);
						}
					}
				}
			}
		}
	}
}