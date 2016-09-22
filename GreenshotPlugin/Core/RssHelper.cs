/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using log4net;

namespace GreenshotPlugin.Core {
	public class RssFile {
		public string File { get; }
		private readonly DateTime _pubdate;
		public DateTime Pubdate => _pubdate;

		public string Link { get; }
		public Version Version { get; set; }

		public string Language { get; set; }

		public bool IsExe {
			get {
				if (File != null) {
					return File.ToLower().EndsWith(".exe");
				}
				return false;
			}
		}

		public bool IsUnstable {
			get {
				if (File != null) {
					return File.ToLower().Contains("unstable");
				}
				return false;
			}
		}

		public bool IsReleaseCandidate {
			get {
				if (File != null) {
					return Regex.IsMatch(File.ToLower(), "rc[0-9]+");
				}
				return false;
			}
		}

		public RssFile(string file, string pubdate, string link) {
			File = file;
			if (!DateTime.TryParse(pubdate, out _pubdate))
			{
				DateTime.TryParseExact(pubdate.Replace(" UT", ""), "ddd, dd MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out _pubdate);
			}
			Link = link;
		}
	}

	/// <summary>
	/// Description of RssHelper.
	/// </summary>
	public class RssHelper {
		private static readonly ILog Log = LogManager.GetLogger(typeof(RssHelper));
		private const string Rssfeed = "http://getgreenshot.org/project-feed/";

		/// <summary>
		/// This is using the HTTP HEAD Method to check if the RSS Feed is modified after the supplied date
		/// </summary>
		/// <param name="updateTime">DateTime</param>
		/// <returns>true if the feed is newer</returns>
		public static bool IsRssModifiedAfter(DateTime updateTime) {
			DateTime lastModified = NetworkHelper.GetLastModified(new Uri(Rssfeed));
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
		/// <returns>List with RssFile(s)</returns>
		public static IList<RssFile> ReadRss() {
			XmlDocument rssDoc = new XmlDocument();
			try {
				HttpWebRequest webRequest = NetworkHelper.CreateWebRequest(Rssfeed);
				XmlTextReader rssReader = new XmlTextReader(webRequest.GetResponse().GetResponseStream());
	
				// Load the XML content into a XmlDocument
				rssDoc.Load(rssReader);
			} catch (Exception wE) {
				Log.WarnFormat("Problem reading RSS from {0}", Rssfeed);
				Log.Warn(wE.Message);
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
				Log.Debug("No RSS Feed!");
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
				Log.Debug("No channel in RSS feed!");
				return null;
			}

			IList<RssFile> rssFiles = new List<RssFile>();

			// Loop for the <title>, <link>, <description> and all the other tags
			for (int i = 0; i < nodeChannel.ChildNodes.Count; i++) {
				// If it is the item tag, then it has children tags which we will add as items to the ListView

				if (nodeChannel.ChildNodes[i].Name != "item")
				{
					continue;
				}
				XmlNode nodeItem = nodeChannel.ChildNodes[i];
				string link = nodeItem["link"]?.InnerText;
				string pubdate = nodeItem["pubDate"]?.InnerText;
				try {
					if (link == null)
					{
						continue;
					}
					Match match = Regex.Match(Uri.UnescapeDataString(link), @"^.*\/(Greenshot.+)\/download$");
					if (!match.Success)
					{
						continue;
					}
					string file = match.Groups[1].Value;

					RssFile rssFile = new RssFile(file, pubdate, link);
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
								Log.DebugFormat("Found invalid version {0} in file {1}", version, file);
							}
						}
						rssFiles.Add(rssFile);
					}
				} catch (Exception ex) {
					Log.WarnFormat("Couldn't read RSS entry for: {0}", nodeChannel["title"]?.InnerText);
					Log.Warn("Reason: ", ex);
				}
			}
			
			return rssFiles;
		}
	}
}
