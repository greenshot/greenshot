/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
using System.Windows.Forms;

using Greenshot.Configuration;
using GreenshotPlugin.Core;
using Greenshot.IniFile;
using log4net;

namespace Greenshot.Experimental {
	/// <summary>
	/// Description of RssFeedHelper.
	/// </summary>
	public static class UpdateHelper {
		private static readonly ILog LOG = LogManager.GetLogger(typeof(UpdateHelper));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		private const string STABLE_DOWNLOAD_LINK = "http://getgreenshot.org/downloads/";
		private const string VERSION_HISTORY_LINK = "http://getgreenshot.org/version-history/";
		private static readonly object LockObject = new object();
		private static RssFile _latestGreenshot;
		private static string _downloadLink = STABLE_DOWNLOAD_LINK;

		/// <summary>
		/// Is an update check needed?
		/// </summary>
		/// <returns>bool true if yes</returns>
		public static bool IsUpdateCheckNeeded() {
			lock (LockObject) {
				if (conf.UpdateCheckInterval == 0) {
					return false;
				}
				if (conf.LastUpdateCheck != null) {
					DateTime checkTime = conf.LastUpdateCheck;
					checkTime = checkTime.AddDays(conf.UpdateCheckInterval);
					if (DateTime.Now.CompareTo(checkTime) < 0) {
						LOG.DebugFormat("No need to check RSS feed for updates, feed check will be after {0}", checkTime);
						return false;
					}
					LOG.DebugFormat("Update check is due, last check was {0} check needs to be made after {1} (which is one {2} later)", conf.LastUpdateCheck, checkTime, conf.UpdateCheckInterval);
					if (!RssHelper.IsRSSModifiedAfter(conf.LastUpdateCheck)) {
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
		public static void CheckAndAskForUpdate() {
			lock (LockObject) {
				Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
				// Test like this:
				// currentVersion = new Version("0.8.1.1198");
	
				try {
					_latestGreenshot = null;
					ProcessRSSInfo(currentVersion);
					if (_latestGreenshot != null) {
						MainForm.Instance.NotifyIcon.BalloonTipClicked += HandleBalloonTipClick;
						MainForm.Instance.NotifyIcon.BalloonTipClosed += CleanupBalloonTipClick;
						MainForm.Instance.NotifyIcon.ShowBalloonTip(10000, "Greenshot", Language.GetFormattedString(LangKey.update_found, "'" + _latestGreenshot.File + "'"), ToolTipIcon.Info);
					}
					conf.LastUpdateCheck = DateTime.Now;
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
				if (_latestGreenshot != null) {
					// "Direct" download link
					// Process.Start(latestGreenshot.Link);
					// Go to getgreenshot.org
					Process.Start(_downloadLink);
				}
			} catch (Exception) {
				MessageBox.Show(Language.GetFormattedString(LangKey.error_openlink, _downloadLink), Language.GetString(LangKey.error));
			} finally {
				CleanupBalloonTipClick(sender, e);
			}
		}

		private static void ProcessRSSInfo(Version currentVersion) {
			// Reset latest Greenshot
			IList<RssFile> rssFiles = RssHelper.readRSS();

			if (rssFiles == null) {
				return;
			}

			// Retrieve the current and latest greenshot
			foreach(RssFile rssFile in rssFiles) {
				if (rssFile.File.StartsWith("Greenshot")) {
					// check for exe
					if (!rssFile.isExe) {
						continue;
					}

					// do we have a version?
					if (rssFile.Version == null) {
						LOG.DebugFormat("Skipping unversioned exe {0} which is published at {1} : {2}", rssFile.File, rssFile.Pubdate.ToLocalTime(), rssFile.Link);
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
						LOG.DebugFormat("Found newer Greenshot '{0}' with version {1} published at {2} : {3}", rssFile.File, rssFile.Version, rssFile.Pubdate.ToLocalTime(), rssFile.Link);
						if (_latestGreenshot == null || rssFile.Version.CompareTo(_latestGreenshot.Version) > 0) {
							_latestGreenshot = rssFile;
							if (rssFile.isReleaseCandidate || rssFile.isUnstable) {
								_downloadLink = VERSION_HISTORY_LINK;
							} else {
								_downloadLink = STABLE_DOWNLOAD_LINK;
							}
						}
					} else if (versionCompare < 0) {
						LOG.DebugFormat("Skipping older greenshot with version {0}", rssFile.Version);
					} else if (versionCompare == 0) {
						LOG.DebugFormat("Found current version as exe {0} with version {1} published at {2} : {3}", rssFile.File, rssFile.Version, rssFile.Pubdate.ToLocalTime(), rssFile.Link);
					}
				}
			}
		}
	}
}
