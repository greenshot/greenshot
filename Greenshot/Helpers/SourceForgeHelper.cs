/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.HttpExtensions;
using Dapplo.Log.Facade;

namespace Greenshot.Helpers
{
	public class SourceforgeFile
	{
		public string File
		{
			get;
		}

		public DateTimeOffset Pubdate
		{
			get;
		}

		public string Link
		{
			get;
		}

		public string DirectLink
		{
			get;
		}

		public Version Version
		{
			get;
			set;
		}

		private string _language;

		public string Language
		{
			get
			{
				return _language;
			}
			set
			{
				_language = value;
			}
		}

		public bool IsExe
		{
			get
			{
				if (File != null)
				{
					return File.ToLower().EndsWith(".exe");
				}
				return false;
			}
		}

		public bool IsUnstable
		{
			get
			{
				if (File != null)
				{
					return File.ToLower().Contains("unstable");
				}
				return false;
			}
		}

		public bool IsReleaseCandidate
		{
			get
			{
				if (File != null)
				{
					return Regex.IsMatch(File.ToLower(), "rc[0-9]+");
				}
				return false;
			}
		}

		public SourceforgeFile(string file, DateTimeOffset pubdate, string link, string directLink)
		{
			File = file;
			Pubdate = pubdate;
			Link = link;
			DirectLink = directLink;
		}
	}

	/// <summary>
	/// Description of SourceForgeHelper.
	/// </summary>
	public class SourceForgeHelper
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly Uri Rssfeed = new Uri("http://getgreenshot.org/project-feed/");

		/// <summary>
		/// This is using the HTTP HEAD Method to check if the RSS Feed is modified after the supplied date
		/// </summary>
		/// <param name="updateTime">DateTime</param>
		/// <returns>true if the feed is newer</returns>
		public static async Task<bool> IsRssModifiedAfter(DateTimeOffset updateTime)
		{
			var lastModified = await Rssfeed.LastModifiedAsync().ConfigureAwait(false);
			return updateTime.CompareTo(lastModified) < 0;
		}

		/// <summary>
		/// Read the Greenshot RSS feed, so we can use this information to check for updates
		/// </summary>
		/// <returns>Dictionary&lt;string, Dictionary&lt;string, RssFile&gt;&gt; with files and their RssFile "description"</returns>
		public static async Task<IDictionary<string, IDictionary<string, SourceforgeFile>>> ReadRssAsync(CancellationToken token = default(CancellationToken))
		{
			var rssFiles = new Dictionary<string, IDictionary<string, SourceforgeFile>>();


			var feed = await Rssfeed.GetAsAsync<SyndicationFeed>(token: token).ConfigureAwait(false);

			if (feed == null)
			{
				return rssFiles;
			}
			foreach (var item in feed.Items)
			{
				var sfLink = item.Links[0].Uri.AbsoluteUri;
				var pubdate = item.PublishDate;
				try
				{
					var match = Regex.Match(Uri.UnescapeDataString(sfLink), @"^http.*sourceforge.*\/projects\/([^\/]+)\/files\/([^\/]+)\/([^\/]+)\/(.+)\/download$");
					if (match.Success)
					{
						string project = match.Groups[1].Value;
						string subdir = match.Groups[2].Value;
						string type = match.Groups[3].Value;
						string file = match.Groups[4].Value;
						// !!! Change this to the mirror !!!
						string mirror = "kent";
						string directLink = Uri.EscapeUriString("http://" + mirror + ".dl.sourceforge.net/project/" + project + "/" + subdir + "/" + type + "/" + file);
						IDictionary<string, SourceforgeFile> filesForType;
						if (rssFiles.ContainsKey(type))
						{
							filesForType = rssFiles[type];
						}
						else
						{
							filesForType = new Dictionary<string, SourceforgeFile>();
							rssFiles.Add(type, filesForType);
						}
						var rssFile = new SourceforgeFile(file, pubdate, sfLink, directLink);
						if (file.EndsWith(".exe") || file.EndsWith(".zip"))
						{
							string version = Regex.Replace(file, @".*[a-zA-Z_]\-", "");
							version = version.Replace(@"\-[a-zA-Z]+.*", "");
							version = Regex.Replace(version, @"\.exe$", "");
							version = Regex.Replace(version, @"\.zip$", "");
							version = Regex.Replace(version, @"RC[0-9]+", "");
							if (version.Trim().Length > 0)
							{
								version = version.Replace('-', '.');
								version = version.Replace(',', '.');
								version = Regex.Replace(version, @"^[a-zA-Z_]*\.", "");
								version = Regex.Replace(version, @"\.[a-zA-Z_]*$", "");

								Version fileVersion;
								if (!Version.TryParse(version, out fileVersion))
								{
									Log.Debug().WriteLine("Found invalid version {0} in file {1}", version, file);
								}
								rssFile.Version = fileVersion;
							}
						}
						else if (type.Equals("Translations"))
						{
							string culture = Regex.Replace(file, @"[a-zA-Z]+-(..-..)\.(xml|html)", "$1");
							try
							{
								//CultureInfo cultureInfo = new CultureInfo(culture);
								rssFile.Language = culture; //cultureInfo.NativeName;
							}
							catch (Exception)
							{
								Log.Warn().WriteLine("Can't read the native name of the culture {0}", culture);
							}
						}
						filesForType.Add(file, rssFile);
					}
				}
				catch (Exception ex)
				{
					Log.Warn().WriteLine("Couldn't read RSS entry for: {0}", item.Title);
					Log.Warn().WriteLine(ex);
				}
			}

			return rssFiles;
		}
	}
}