using Greenshot.Configuration;
using Greenshot.Forms;
using Greenshot.IniFile;
using GreenshotPlugin.Core;
using log4net;
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Greenshot.Experimental {
	/// <summary>
	/// Description of RssFeedHelper.
	/// </summary>
	public static class UpdateHelper {
		private static ILog LOG = LogManager.GetLogger(typeof(UpdateHelper));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		private const string STABLE_DOWNLOAD_LINK = "http://getgreenshot.org/downloads/";
		private const string VERSION_HISTORY_LINK = "http://getgreenshot.org/version-history/";
		private static AsyncLock _asyncLock = new AsyncLock();
		private static SourceforgeFile latestGreenshot;
		private static SourceforgeFile currentGreenshot;
		private static string downloadLink = STABLE_DOWNLOAD_LINK;

		/// <summary>
		/// Is an update check needed?
		/// </summary>
		/// <returns>bool true if yes</returns>
		public static async Task<bool> IsUpdateCheckNeeded() {
			using (await _asyncLock.LockAsync().ConfigureAwait(false)) {
				if (conf.UpdateCheckInterval == 0) {
					return false;
				}
				if (conf.LastUpdateCheck != default(DateTimeOffset)) {
					var checkTime = conf.LastUpdateCheck;
					checkTime = checkTime.AddDays(conf.UpdateCheckInterval);
					if (DateTimeOffset.Now.CompareTo(checkTime) < 0) {
						LOG.DebugFormat("No need to check RSS feed for updates, feed check will be after {0}", checkTime);
						return false;
					}
					LOG.DebugFormat("Update check is due, last check was {0} check needs to be made after {1} (which is one {2} later)", conf.LastUpdateCheck, checkTime, conf.UpdateCheckInterval);
					if (!await SourceForgeHelper.isRSSModifiedAfter(conf.LastUpdateCheck).ConfigureAwait(false)) {
						LOG.DebugFormat("RSS feed has not been updated since after {0}", conf.LastUpdateCheck);
						return false;
					}
				}
			}
			return true;
		}

		/// <summary>
		/// Read the RSS feed to see if there is a Greenshot update
		/// </summary>
		public static async Task CheckAndAskForUpdate() {
			using (await _asyncLock.LockAsync().ConfigureAwait(false)) {
				Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
				// Test like this:
				// currentVersion = new Version("0.8.1.1198");
	
				try {
					latestGreenshot = null;
					ProcessRSSInfo(currentVersion);
					if (latestGreenshot != null) {
						MainForm.Instance.BeginInvoke( new Action(() => {
							MainForm.Instance.NotifyIcon.BalloonTipClicked += HandleBalloonTipClick;
							MainForm.Instance.NotifyIcon.BalloonTipClosed += CleanupBalloonTipClick;
							MainForm.Instance.NotifyIcon.ShowBalloonTip(10000, "Greenshot", Language.GetFormattedString(LangKey.update_found, "'" + latestGreenshot.File + "'"), ToolTipIcon.Info);
						}));
					}
					conf.LastUpdateCheck = DateTimeOffset.Now;
				} catch (Exception e) {
					LOG.Error("An error occured while checking for updates, the error will be ignored: ", e);
				}
			}
		}

		private static void CleanupBalloonTipClick(object sender, EventArgs e) {
			MainForm.Instance.NotifyIcon.BalloonTipClicked -= HandleBalloonTipClick;
			MainForm.Instance.NotifyIcon.BalloonTipClosed -= CleanupBalloonTipClick;
		}
		
		private static void HandleBalloonTipClick(object sender, EventArgs e) {
			try {
				if (latestGreenshot != null) {
					// "Direct" download link
					// Process.Start(latestGreenshot.Link);
					// Go to getgreenshot.org
					Process.Start(downloadLink);
				}
			} catch (Exception) {
				MessageBox.Show(Language.GetFormattedString(LangKey.error_openlink, downloadLink), Language.GetString(LangKey.error));
			} finally {
				CleanupBalloonTipClick(sender, e);
			}
		}

		private static async void ProcessRSSInfo(Version currentVersion) {
			// Reset latest Greenshot
			var rssFiles = await SourceForgeHelper.readRSS().ConfigureAwait(false);

			if (rssFiles == null) {
				return;
			}

			// Retrieve the current and latest greenshot
			foreach(string fileType in rssFiles.Keys) {
				foreach(string file in rssFiles[fileType].Keys) {
					SourceforgeFile rssFile = rssFiles[fileType][file];
					if (fileType.StartsWith("Greenshot")) {
						// check for exe
						if (!rssFile.isExe) {
							continue;
						}

						// do we have a version?
						if (rssFile.Version == null) {
							LOG.DebugFormat("Skipping unversioned exe {0} which is published at {1} : {2}", file, rssFile.Pubdate.ToLocalTime(), rssFile.Link);
							continue;
						}

						// if the file is unstable, we will skip it when:
						// the current version is a release or release candidate AND check unstable is turned off.
						if (rssFile.isUnstable) {
							// Skip if we shouldn't check unstables
							if ((conf.BuildState == BuildStates.RELEASE) && !conf.CheckForUnstable) {
								continue;
							}
						}

						// if the file is a release candidate, we will skip it when:
						// the current version is a release AND check unstable is turned off.
						if (rssFile.isReleaseCandidate) {
							if (conf.BuildState == BuildStates.RELEASE && !conf.CheckForUnstable) {
								continue;
							}
						}

						// Compare versions
						int versionCompare = rssFile.Version.CompareTo(currentVersion);
						if (versionCompare > 0) {
							LOG.DebugFormat("Found newer Greenshot '{0}' with version {1} published at {2} : {3}", file, rssFile.Version, rssFile.Pubdate.ToLocalTime(), rssFile.Link);
							if (latestGreenshot == null || rssFile.Version.CompareTo(latestGreenshot.Version) > 0) {
								latestGreenshot = rssFile;
								if (rssFile.isReleaseCandidate || rssFile.isUnstable) {
									downloadLink = VERSION_HISTORY_LINK;
								} else {
									downloadLink = STABLE_DOWNLOAD_LINK;
								}
							}
						} else if (versionCompare < 0) {
							LOG.DebugFormat("Skipping older greenshot with version {0}", rssFile.Version);
						} else if (versionCompare == 0) {
							currentGreenshot = rssFile;
							LOG.DebugFormat("Found current version as exe {0} with version {1} published at {2} : {3}", file, rssFile.Version, rssFile.Pubdate.ToLocalTime(), rssFile.Link);
						}
					}
				}
			}
		}
	}
}
