/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using Greenshot.Configuration;
using GreenshotPlugin.Core;
using Greenshot.IniFile;

namespace Greenshot.Experimental {
	/// <summary>
	/// Description of RssFeedHelper.
	/// </summary>
	public static class UpdateHelper {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(UpdateHelper));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		private const string STABLE_DOWNLOAD_LINK = "http://getgreenshot.org/downloads/";
		private const string VERSION_HISTORY_LINK = "http://getgreenshot.org/version-history/";
		private static object lockObject = new object();
		private static SourceforgeFile latestGreenshot;
		private static SourceforgeFile currentGreenshot;
		private static string downloadLink = STABLE_DOWNLOAD_LINK;

		/// <summary>
		/// Is an update check needed?
		/// </summary>
		/// <returns>bool true if yes</returns>
		public static bool IsUpdateCheckNeeded() {
			lock (lockObject) {
				if (conf.UpdateCheckInterval == 0) {
					return false;
				}
				if (conf.LastUpdateCheck != null) {
					DateTime checkTime = conf.LastUpdateCheck;
					checkTime = checkTime.AddDays(conf.UpdateCheckInterval);
					if (DateTime.Now.CompareTo(checkTime) < 0) {
						LOG.DebugFormat("No need to check RSS feed for updates, feed check will be after {0}", checkTime);
						return false;
					} else {
						LOG.DebugFormat("Update check is due, last check was {0} check needs to be made after {1} (which is one {2} later)", conf.LastUpdateCheck, checkTime, conf.UpdateCheckInterval);
					}
				}
			}
			return true;
		}

		/// <summary>
		/// Read the RSS feed to see if there is a Greenshot update
		/// </summary>
		public static void CheckAndAskForUpdate() {
			lock (lockObject) {
				Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
				// Test like this:
				// currentVersion = new Version("0.8.1.1198");
	
				try {
					latestGreenshot = null;
					UpdateHelper.ProcessRSSInfo(currentVersion);
					if (latestGreenshot != null) {
						MainForm.Instance.NotifyIcon.BalloonTipClicked += HandleBalloonTipClick;
						MainForm.Instance.NotifyIcon.BalloonTipClosed += CleanupBalloonTipClick;
						MainForm.Instance.NotifyIcon.ShowBalloonTip(10000, "Greenshot", Language.GetFormattedString(LangKey.update_found, "'" + latestGreenshot.File + "'"), ToolTipIcon.Info);
					}
					conf.LastUpdateCheck = DateTime.Now;
					IniConfig.Save();
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
				MessageBox.Show(Language.GetFormattedString(LangKey.error_openlink, latestGreenshot.Link), Language.GetString(LangKey.error));
			} finally {
				MainForm.Instance.NotifyIcon.BalloonTipClicked -= HandleBalloonTipClick;
				MainForm.Instance.NotifyIcon.BalloonTipClosed -= CleanupBalloonTipClick;
			}
		}

		private static void ProcessRSSInfo(Version currentVersion) {
			// Reset latest Greenshot
			Dictionary<string, Dictionary<string, SourceforgeFile>> rssFiles = SourceForgeHelper.readRSS();

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
							if ((conf.BuildState == BuildStates.RELEASE || conf.BuildState == BuildStates.RELEASE_CANDIDATE) && !conf.CheckForUnstable) {
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

//			// check for language file updates
//			// Directory to store the language files
//			string languageFilePath = Path.GetDirectoryName(Language.GetInstance().GetHelpFilePath());
//			LOG.DebugFormat("Language file path: {0}", languageFilePath);
//			foreach(string fileType in rssFiles.Keys) {
//				foreach(string file in rssFiles[fileType].Keys) {
//					RssFile rssFile = rssFiles[fileType][file];
//					if (fileType.Equals("Translations")) {
//						LOG.DebugFormat("Found translation {0} with language {1} published at {2} : {3}", file, rssFile.Language, rssFile.Pubdate.ToLocalTime(), rssFile.Link);
//						string languageFile = Path.Combine(languageFilePath, file);
//						if (!File.Exists(languageFile)) {
//							LOG.DebugFormat("Found not installed language: {0}", rssFile.Language);
//							// Example to download language files
//							//string languageFileContent = GreenshotPlugin.Core.NetworkHelper.DownloadFileAsString(new Uri(rssFile.Link), Encoding.UTF8);
//							//TextWriter writer = new StreamWriter(languageFile, false, Encoding.UTF8);
//							//LOG.InfoFormat("Writing {0}", languageFile);
//							//writer.Write(languageFileContent);
//							//writer.Close();
//						}
//					}
//				}
//			}
		}
	}
}
