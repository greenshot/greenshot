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
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using log4net;

namespace GreenshotPlugin.Core {
	public class SourceforgeFile {
		private readonly string _file;
		public string File {
			get {return _file;}
		}
		private readonly DateTime _pubdate;
		public DateTime Pubdate {
			get {return _pubdate;}
		}
		private readonly string _link;
		public string Link {
			get {return _link;}
		}
		private readonly string _directLink;
		public string DirectLink {
			get {return _directLink;}
		}
		private Version _version;
		public Version Version {
			get {return _version;}
			set {
				_version = value;
			}
		}
		private string _language;
		public string Language {
			get {return _language;}
			set {_language = value;}
		}

		public bool isExe {
			get {
				if (_file != null) {
					return _file.ToLower().EndsWith(".exe");
				}
				return false;
			}
		}

		public bool isUnstable {
			get {
				if (_file != null) {
					return _file.ToLower().Contains("unstable");
				}
				return false;
			}
		}

		public bool isReleaseCandidate {
			get {
				if (_file != null) {
					return Regex.IsMatch(_file.ToLower(), "rc[0-9]+");
				}
				return false;
			}
		}

		public SourceforgeFile(string file, string pubdate, string link, string directLink) {
			_file = file;
			DateTime.TryParse(pubdate, out _pubdate);
			_link = link;
			_directLink = directLink;
		}
	}
	/// <summary>
	/// Description of SourceForgeHelper.
	/// </summary>
	public class SourceForgeHelper {
		private static readonly ILog LOG = LogManager.GetLogger(typeof(SourceForgeHelper));
		private const string RSSFEED = "http://getgreenshot.org/project-feed/";

		/// <summary>
		/// This is using the HTTP HEAD Method to check if the RSS Feed is modified after the supplied date
		/// </summary>
		/// <param name="updateTime">DateTime</param>
		/// <returns>true if the feed is newer</returns>
		public static bool IsRSSModifiedAfter(DateTime updateTime) {
			DateTime lastModified = NetworkHelper.GetLastModified(new Uri(RSSFEED));
			if (lastModified == DateTime.MinValue)
			{
				// Time could not be read, just take now and add one hour to it.
				// This assist BUG-1850
				lastModified = DateTime.Now.AddHours(1);
			}
			return updateTime.CompareTo(lastModified) < 0;
		}

		/// <summary>
		/// Read the Greenshot RSS feed, so we can use this information to check for updates
		/// </summary>
		/// <returns>Dictionary<string, Dictionary<string, RssFile>> with files and their RssFile "description"</returns>
		public static Dictionary<string, Dictionary<string, SourceforgeFile>> readRSS() {
			XmlDocument rssDoc = new XmlDocument();
			try {
				HttpWebRequest webRequest = NetworkHelper.CreateWebRequest(RSSFEED);
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
