/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
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
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

using Greenshot.Configuration;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;

namespace Greenshot.Experimental {
	public class SourceforgeFile {
		private string file;
		public string File {
			get {return file;}
		}
		private DateTime pubdate;
		public DateTime Pubdate {
			get {return pubdate;}
		}
		private string link;
		public string Link {
			get {return link;}
		}
		private string directLink;
		public string DirectLink {
			get {return directLink;}
		}
		private Version version;
		public Version Version {
			get {return version;}
			set {
				version = value;
			}
		}
		private string language;
		public string Language {
			get {return language;}
			set {language = value;}
		}
		
		public SourceforgeFile(string file, string pubdate, string link, string directLink) {
			this.file = file;
			this.pubdate = DateTime.Parse(pubdate);
			this.link = link;
			this.directLink = directLink;
		}
	}

	/// <summary>
	/// Description of RssFeedHelper.
	/// </summary>
	public class UpdateHelper {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(UpdateHelper));
		private const String RSSFEED = "https://sourceforge.net/api/file/index/project-id/191585/mtime/desc/rss";
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		private static Dictionary<string, string> mirrors = new Dictionary<string, string>();
		private static object lockObject = new object();
		private static SourceforgeFile latestGreenshot;
		private static SourceforgeFile currentGreenshot;
		//private static List<RssFile> languageFiles;

		static UpdateHelper() {
			// See: http://sourceforge.net/apps/trac/sourceforge/wiki/Mirrors
			mirrors.Add("aarnet", "Brisbane, Australia");
			mirrors.Add("cdnetworks-kr-1", "Seoul, Korea, Republic of");
			mirrors.Add("cdnetworks-kr-2", "Seoul, Korea, Republic of");
			mirrors.Add("cdnetworks-us-1", "San Jose, CA");
			mirrors.Add("cdnetworks-us-2", "San Jose, CA");
			mirrors.Add("citylan", "Moscow, Russian Federation");
		}
		
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
						ILanguage lang = Language.GetInstance();
						DialogResult result = MessageBox.Show(lang.GetFormattedString(LangKey.update_found, latestGreenshot.Version), "Greenshot", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly, false);
						if (result == DialogResult.OK) {
							Process.Start(latestGreenshot.Link);
						}
					}
					conf.LastUpdateCheck = DateTime.Now;
					IniConfig.Save();
				} catch (Exception e) {
					LOG.Error("An error occured while checking for updates, the error will be ignored: ", e);
				}
			}
		}

		private static void ProcessRSSInfo(Version currentVersion) {
			// Reset latest Greenshot
			latestGreenshot = null;
			Dictionary<string, Dictionary<string, SourceforgeFile>> rssFiles = readRSS();

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

		/// <summary>
		/// Read the Greenshot RSS feed, so we can use this information to check for updates
		/// </summary>
		/// <returns>Dictionary<string, Dictionary<string, RssFile>> with files and their RssFile "description"</returns>
		private static Dictionary<string, Dictionary<string, SourceforgeFile>> readRSS() {
			HttpWebRequest webRequest = (HttpWebRequest)GreenshotPlugin.Core.NetworkHelper.CreatedWebRequest(RSSFEED);
			XmlTextReader rssReader = new XmlTextReader(webRequest.GetResponse().GetResponseStream());
			XmlDocument rssDoc = new XmlDocument();

			// Load the XML content into a XmlDocument
			rssDoc.Load(rssReader);
			
			// Loop for the <rss> tag
			XmlNode nodeRss = null;
			for (int i = 0; i < rssDoc.ChildNodes.Count; i++) {
				// If it is the rss tag
				if (rssDoc.ChildNodes[i].Name == "rss") {
					// <rss> tag found
					nodeRss = rssDoc.ChildNodes[i];
				}
			}

			if (nodeRss == null) {
				LOG.Debug("No RSS Feed!");
				return null;
			}

			// Loop for the <channel> tag
			XmlNode nodeChannel = null;
			for (int i = 0; i < nodeRss.ChildNodes.Count; i++) {
				// If it is the channel tag
				if (nodeRss.ChildNodes[i].Name == "channel") {
					// <channel> tag found
					nodeChannel = nodeRss.ChildNodes[i];
				}
			}
			
			if (nodeChannel == null) {
				LOG.Debug("No channel in RSS feed!");
				return null;
			}

			Dictionary<string, Dictionary<string, SourceforgeFile>> rssFiles = new Dictionary<string, Dictionary<string, SourceforgeFile>>();

			// Loop for the <title>, <link>, <description> and all the other tags
			for (int i = 0; i < nodeChannel.ChildNodes.Count; i++) {
				// If it is the item tag, then it has children tags which we will add as items to the ListView

				if (nodeChannel.ChildNodes[i].Name == "item") {
					XmlNode nodeItem = nodeChannel.ChildNodes[i];
					string sfLink = nodeItem["link"].InnerText;
					string pubdate = nodeItem["pubDate"].InnerText;
					try {
						Match match= Regex.Match(Uri.UnescapeDataString(sfLink), @"^http.*sourceforge.*\/projects\/([^\/]+)\/files\/([^\/]+)\/([^\/]+)\/(.+)\/download$");
						if (match.Success) {
							string project = match.Groups[1].Value;
							string subdir = match.Groups[2].Value;
							string type = match.Groups[3].Value;
							string file = match.Groups[4].Value;
							// !!! Change this to the mirror !!!
							string mirror = "kent";
							string directLink = Uri.EscapeUriString("http://"+mirror+".dl.sourceforge.net/project/"+project+"/"+subdir+"/"+type+"/"+file);
							Dictionary<string, SourceforgeFile> filesForType;
							if (rssFiles.ContainsKey(type)) {
								filesForType = rssFiles[type];
							} else {
								filesForType = new Dictionary<string, SourceforgeFile>();
								rssFiles.Add(type, filesForType);
							}
							SourceforgeFile rssFile = new SourceforgeFile(file, pubdate, sfLink, directLink);
							if (file.EndsWith(".exe") ||file.EndsWith(".zip")) {
								string version = Regex.Replace(file, ".*[a-zA-Z]-", "");
								version = Regex.Replace(version, ".exe$", "");
								version = Regex.Replace(version, ".zip$", "");
								if (version.Trim().Length > 0) {
									version = version.Replace('-','.');
									version = version.Replace(',','.');
									try {
										rssFile.Version = new Version(version);
									} catch (Exception) {
										LOG.DebugFormat("Found invalid version {0} in file {1}", version, file);
									}
								}
						    }
							if (type.Equals("Translations")) {
								string culture = Regex.Replace(file, @"[a-zA-Z]+-(..-..)\.(xml|html)", "$1");
								CultureInfo cultureInfo = new CultureInfo(culture);
								rssFile.Language = cultureInfo.NativeName;
							}
							filesForType.Add(file, rssFile);
						}
					} catch (Exception ex) {
						LOG.WarnFormat("Couldn't read RSS entry for: {0}", nodeChannel["title"].InnerText);
						LOG.Warn("Reason: ", ex);
					}
				}
			}
			
			return rssFiles;
		}
	}
}
