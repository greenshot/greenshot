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
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;

namespace GreenshotPlugin.Core {
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

		public bool isExe {
			get {
				if (file != null) {
					return file.ToLower().EndsWith(".exe");
				}
				return false;
			}
		}

		public bool isUnstable {
			get {
				if (file != null) {
					return file.ToLower().Contains("unstable");
				}
				return false;
			}
		}

		public bool isReleaseCandidate {
			get {
				if (file != null) {
					return Regex.IsMatch(file.ToLower(), "rc[0-9]+");
				}
				return false;
			}
		}

		public SourceforgeFile(string file, string pubdate, string link, string directLink) {
			this.file = file;
			this.pubdate = DateTime.Parse(pubdate);
			this.link = link;
			this.directLink = directLink;
		}
	}
	/// <summary>
	/// Description of SourceForgeHelper.
	/// </summary>
	public class SourceForgeHelper {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(SourceForgeHelper));
		private const String RSSFEED = "http://getgreenshot.org/project-feed/";

		/// <summary>
		/// Read the Greenshot RSS feed, so we can use this information to check for updates
		/// </summary>
		/// <returns>Dictionary<string, Dictionary<string, RssFile>> with files and their RssFile "description"</returns>
		public static Dictionary<string, Dictionary<string, SourceforgeFile>> readRSS() {
			HttpWebRequest webRequest;
			XmlDocument rssDoc = new XmlDocument();
			try {
				webRequest = (HttpWebRequest)NetworkHelper.CreateWebRequest(RSSFEED);
				XmlTextReader rssReader = new XmlTextReader(webRequest.GetResponse().GetResponseStream());
	
				// Load the XML content into a XmlDocument
				rssDoc.Load(rssReader);
			} catch (Exception wE) {
				LOG.WarnFormat("Problem reading RSS from {0}", RSSFEED);
				LOG.Warn(wE.Message);
				return null;
			}
			
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
								string version = Regex.Replace(file, @".*[a-zA-Z_]\-", "");
								version = version.Replace(@"\-[a-zA-Z]+.*","");
								version = Regex.Replace(version, @"\.exe$", "");
								version = Regex.Replace(version, @"\.zip$", "");
								version = Regex.Replace(version, @"RC[0-9]+", "");
								if (version.Trim().Length > 0) {
									version = version.Replace('-','.');
									version = version.Replace(',','.');
									version = Regex.Replace(version, @"^[a-zA-Z_]*\.", "");
									version = Regex.Replace(version, @"\.[a-zA-Z_]*$", "");

									try {
										rssFile.Version = new Version(version);
									} catch (Exception) {
										LOG.DebugFormat("Found invalid version {0} in file {1}", version, file);
									}
								}
							} else if (type.Equals("Translations")) {
								string culture = Regex.Replace(file, @"[a-zA-Z]+-(..-..)\.(xml|html)", "$1");
								try {
									//CultureInfo cultureInfo = new CultureInfo(culture);
									rssFile.Language = culture;//cultureInfo.NativeName;
								} catch (Exception) {
									LOG.WarnFormat("Can't read the native name of the culture {0}", culture);
								}
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
