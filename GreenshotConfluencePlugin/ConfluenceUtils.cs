#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Confluence;
using GreenshotPlugin.Core;
using log4net;

#endregion

namespace GreenshotConfluencePlugin
{
	/// <summary>
	///     Description of ConfluenceUtils.
	/// </summary>
	public class ConfluenceUtils
	{
		private static readonly ILog LOG = LogManager.GetLogger(typeof(ConfluenceUtils));

		public static List<Page> GetCurrentPages()
		{
			var pages = new List<Page>();
			var pageIdRegex = new Regex(@"pageId=(\d+)");
			var spacePageRegex = new Regex(@"\/display\/([^\/]+)\/([^#]+)");
			foreach (var browserurl in IEHelper.GetIeUrls().Distinct())
			{
				string url;
				try
				{
					url = Uri.UnescapeDataString(browserurl).Replace("+", " ");
				}
				catch
				{
					LOG.WarnFormat("Error processing URL: {0}", browserurl);
					continue;
				}
				var pageIdMatch = pageIdRegex.Matches(url);
				if (pageIdMatch != null && pageIdMatch.Count > 0)
				{
					var pageId = long.Parse(pageIdMatch[0].Groups[1].Value);
					try
					{
						var pageDouble = false;
						foreach (var page in pages)
						{
							if (page.Id == pageId)
							{
								pageDouble = true;
								LOG.DebugFormat("Skipping double page with ID {0}", pageId);
								break;
							}
						}
						if (!pageDouble)
						{
							var page = ConfluencePlugin.ConfluenceConnector.GetPage(pageId);
							LOG.DebugFormat("Adding page {0}", page.Title);
							pages.Add(page);
						}
						continue;
					}
					catch (Exception ex)
					{
						// Preventing security problems
						LOG.DebugFormat("Couldn't get page details for PageID {0}", pageId);
						LOG.Warn(ex);
					}
				}
				var spacePageMatch = spacePageRegex.Matches(url);
				if (spacePageMatch != null && spacePageMatch.Count > 0)
				{
					if (spacePageMatch[0].Groups.Count >= 2)
					{
						var space = spacePageMatch[0].Groups[1].Value;
						var title = spacePageMatch[0].Groups[2].Value;
						if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(space))
						{
							continue;
						}
						if (title.EndsWith("#"))
						{
							title = title.Substring(0, title.Length - 1);
						}
						try
						{
							var pageDouble = false;
							foreach (var page in pages)
							{
								if (page.Title.Equals(title))
								{
									LOG.DebugFormat("Skipping double page with title {0}", title);
									pageDouble = true;
									break;
								}
							}
							if (!pageDouble)
							{
								var page = ConfluencePlugin.ConfluenceConnector.GetPage(space, title);
								LOG.DebugFormat("Adding page {0}", page.Title);
								pages.Add(page);
							}
						}
						catch (Exception ex)
						{
							// Preventing security problems
							LOG.DebugFormat("Couldn't get page details for space {0} / title {1}", space, title);
							LOG.Warn(ex);
						}
					}
				}
			}
			return pages;
		}
	}
}