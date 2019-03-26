// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapplo.Confluence;
using Dapplo.Confluence.Entities;
using Dapplo.Confluence.Query;
using Dapplo.Log;
using Greenshot.Addon.InternetExplorer;

namespace Greenshot.Addon.Confluence
{
	/// <summary>
	///     Description of ConfluenceUtils.
	/// </summary>
	public class ConfluenceUtils
	{
		private static readonly LogSource Log = new LogSource();

		public static async Task<List<Content>> GetCurrentPages(IConfluenceClient confluenceConnector)
		{
			var pages = new List<Content>();
			var pageIdRegex = new Regex(@"pageId=(\d+)");
			var spacePageRegex = new Regex(@"\/display\/([^\/]+)\/([^#]+)");
			foreach (var browserurl in InternetExplorerHelper.GetIEUrls().Distinct())
			{
				string url;
				try
				{
					url = Uri.UnescapeDataString(browserurl).Replace("+", " ");
				}
				catch
				{
					Log.Warn().WriteLine("Error processing URL: {0}", browserurl);
					continue;
				}
				var pageIdMatch = pageIdRegex.Matches(url);
				if (pageIdMatch.Count > 0)
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
								Log.Debug().WriteLine("Skipping double page with ID {0}", pageId);
								break;
							}
						}
						if (!pageDouble)
						{
							var page = await confluenceConnector.Content.GetAsync(pageId).ConfigureAwait(false);
							Log.Debug().WriteLine("Adding page {0}", page.Title);
							pages.Add(page);
						}
						continue;
					}
					catch (Exception ex)
					{
						// Preventing security problems
						Log.Debug().WriteLine("Couldn't get page details for PageID {0}", pageId);
						Log.Warn().WriteLine(ex);
					}
				}
				var spacePageMatch = spacePageRegex.Matches(url);
			    if (spacePageMatch.Count <= 0|| spacePageMatch[0].Groups.Count < 2)
			    {
			        continue;
			    }

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
			            if (!page.Title.Equals(title))
			            {
			                continue;
			            }

			            Log.Debug().WriteLine("Skipping double page with title {0}", title);
			            pageDouble = true;
			            break;
			        }
			        if (!pageDouble)
			        {
			            var foundContents = await confluenceConnector.Content.SearchAsync(Where.And(Where.Space.Is(space), Where.Title.Is(title)));
			            var page = foundContents.FirstOrDefault();
			            if (page == null)
			            {
                            continue;
			            }

                        Log.Debug().WriteLine("Adding page {0}", page.Title);
			            pages.Add(page);
			        }
			    }
			    catch (Exception ex)
			    {
			        // Preventing security problems
			        Log.Debug().WriteLine("Couldn't get page details for space {0} / title {1}", space, title);
			        Log.Warn().WriteLine(ex);
			    }
			}
			return pages;
		}
	}
}