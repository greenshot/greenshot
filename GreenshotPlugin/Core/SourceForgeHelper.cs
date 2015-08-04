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

using log4net;
using System;
using System.Collections.Generic;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace GreenshotPlugin.Core {
	public class SourceforgeFile {
		private string _file;
		public string File {
			get {return _file;}
		}
		private DateTimeOffset _pubdate;
		public DateTimeOffset Pubdate {
			get {return _pubdate;}
		}
		private string _link;
		public string Link {
			get {return _link;}
		}
		private string _directLink;
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

		public SourceforgeFile(string file, DateTimeOffset pubdate, string link, string directLink) {
			this._file = file;
			this._pubdate = pubdate;
			this._link = link;
			this._directLink = directLink;
		}
	}
	/// <summary>
	/// Description of SourceForgeHelper.
	/// </summary>
	public class SourceForgeHelper {
		private static ILog LOG = LogManager.GetLogger(typeof(SourceForgeHelper));
		private static readonly Uri RSSFEED = new Uri("http://getgreenshot.org/project-feed/");

		/// <summary>
		/// This is using the HTTP HEAD Method to check if the RSS Feed is modified after the supplied date
		/// </summary>
		/// <param name="updateTime">DateTime</param>
		/// <returns>true if the feed is newer</returns>
		public static async Task<bool> isRSSModifiedAfter(DateTimeOffset updateTime) {
			DateTimeOffset lastModified = await RSSFEED.LastModifiedAsync().ConfigureAwait(false);
			return updateTime.CompareTo(lastModified) < 0;
		}

		/// <summary>
		/// Read the Greenshot RSS feed, so we can use this information to check for updates
		/// </summary>
		/// <returns>Dictionary<string, Dictionary<string, RssFile>> with files and their RssFile "description"</returns>
		public static async Task<IDictionary<string, IDictionary<string, SourceforgeFile>>> readRSS() {
			var rssFiles = new Dictionary<string, IDictionary<string, SourceforgeFile>>();
			var rssContent = await RSSFEED.GetAsync().ConfigureAwait(false);
			if (rssContent == null) {
				return rssFiles;
			}
			var stream = await rssContent.GetAsMemoryStreamAsync(false).ConfigureAwait(false);
			if (stream == null) {
				return rssFiles;
			}
			using (XmlReader reader = XmlReader.Create(stream)) {
				var feed = SyndicationFeed.Load(reader);

				foreach (var item in feed.Items) {
					var sfLink = item.Links[0].Uri.ToString();
					var pubdate = item.PublishDate;
					try {
						Match match = Regex.Match(Uri.UnescapeDataString(sfLink), @"^http.*sourceforge.*\/projects\/([^\/]+)\/files\/([^\/]+)\/([^\/]+)\/(.+)\/download$");
						if (match.Success) {
							string project = match.Groups[1].Value;
							string subdir = match.Groups[2].Value;
							string type = match.Groups[3].Value;
							string file = match.Groups[4].Value;
							// !!! Change this to the mirror !!!
							string mirror = "kent";
							string directLink = Uri.EscapeUriString("http://" + mirror + ".dl.sourceforge.net/project/" + project + "/" + subdir + "/" + type + "/" + file);
							IDictionary<string, SourceforgeFile> filesForType;
							if (rssFiles.ContainsKey(type)) {
								filesForType = rssFiles[type];
							} else {
								filesForType = new Dictionary<string, SourceforgeFile>();
								rssFiles.Add(type, filesForType);
							}
							SourceforgeFile rssFile = new SourceforgeFile(file, pubdate, sfLink, directLink);
							if (file.EndsWith(".exe") || file.EndsWith(".zip")) {
								string version = Regex.Replace(file, @".*[a-zA-Z_]\-", "");
								version = version.Replace(@"\-[a-zA-Z]+.*", "");
								version = Regex.Replace(version, @"\.exe$", "");
								version = Regex.Replace(version, @"\.zip$", "");
								version = Regex.Replace(version, @"RC[0-9]+", "");
								if (version.Trim().Length > 0) {
									version = version.Replace('-', '.');
									version = version.Replace(',', '.');
									version = Regex.Replace(version, @"^[a-zA-Z_]*\.", "");
									version = Regex.Replace(version, @"\.[a-zA-Z_]*$", "");

									Version fileVersion;
									if (!Version.TryParse(version, out fileVersion)) {
										LOG.DebugFormat("Found invalid version {0} in file {1}", version, file);
									}
									rssFile.Version = fileVersion;
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
						LOG.WarnFormat("Couldn't read RSS entry for: {0}", item.Title);
						LOG.Warn("Reason: ", ex);
					}
				}
			}

			return rssFiles;
			


		}
	}
}
