/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
		private static object lockObject = new object();
		private static SourceforgeFile latestGreenshot;
		private static SourceforgeFile currentGreenshot;
		private const string DOWNLOAD_LINK = "http://getgreenshot.org/downloads/";

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
				// Version currentVersion = new Version("0.8.1.1198");
	
				try {
					UpdateHelper.ProcessRSSInfo(currentVersion);
					if (latestGreenshot != null) {
						MainForm.instance.notifyIcon.BalloonTipClicked += HandleBalloonTipClick;
						MainForm.instance.notifyIcon.BalloonTipClosed += CleanupBalloonTipClick;
						MainForm.instance.notifyIcon.ShowBalloonTip(10000, "Greenshot", Language.GetFormattedString(LangKey.update_found, latestGreenshot.Version), ToolTipIcon.Info);
					}
					conf.LastUpdateCheck = DateTime.Now;
					IniConfig.Save();
				} catch (Exception e) {
					LOG.Error("An error occured while checking for updates, the error will be ignored: ", e);
				}
			}
		}

		private static void CleanupBalloonTipClick(object sender, EventArgs e) {
			MainForm.instance.notifyIcon.BalloonTipClicked -= HandleBalloonTipClick;
			MainForm.instance.notifyIcon.BalloonTipClosed -= CleanupBalloonTipClick;
		}
		
		private static void HandleBalloonTipClick(object sender, EventArgs e) {
			try {
				if (latestGreenshot != null) {
					// "Direct" download link
					// Process.Start(latestGreenshot.Link);
					// Go to getgreenshot.org
					Process.Start(DOWNLOAD_LINK);
				}
			} catch (Exception) {
				MessageBox.Show(Language.GetFormattedString(LangKey.error_openlink, latestGreenshot.Link), Language.GetString(LangKey.error));
			} finally {
				MainForm.instance.notifyIcon.BalloonTipClicked -= HandleBalloonTipClick;
				MainForm.instance.notifyIcon.BalloonTipClosed -= CleanupBalloonTipClick;
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
						if (rssFile.File == null || !rssFile.File.EndsWith(".exe")) {
							continue;
						}
						// Check if non stable
						if (!conf.CheckUnstable && rssFile.File.ToLower().Contains("unstable")) {
							continue;
						}
						if (rssFile.Version == null) {
							LOG.DebugFormat("Skipping unversioned exe {0} with published at {1} : {2}", file, rssFile.Pubdate.ToLocalTime(), rssFile.Link);
							continue;
						}
						int versionCompare = rssFile.Version.CompareTo(currentVersion);
						if (versionCompare > 0) {
							LOG.DebugFormat("Found newer version as exe {0} with version {1} published at {2} : {3}", file, rssFile.Version, rssFile.Pubdate.ToLocalTime(), rssFile.Link);
							if (latestGreenshot == null || rssFile.Version.CompareTo(latestGreenshot.Version) > 0) {
								latestGreenshot = rssFile;
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
