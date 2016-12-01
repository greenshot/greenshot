//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using Dapplo.Confluence.Entities;
using Dapplo.Log;
using Greenshot.CaptureCore;
using Greenshot.CaptureCore.CaptureSources;
using Greenshot.Core;

#endregion

namespace Greenshot.Addon.Confluence
{
	/// <summary>
	///     Confluence utils are to support the confluence plugin with retrieving the current confluence pages.
	/// </summary>
	public static class ConfluenceUtils
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly Regex PageIdRegex = new Regex(@"pageId=(\d+)", RegexOptions.Compiled);
		private static readonly Regex DisplayRegex = new Regex(@"\/display\/([^\/]+)\/([^#]+)", RegexOptions.Compiled);
		private static readonly Regex ViewPageRegex = new Regex(@"pages\/viewpage.action\?title=(.+)&spaceKey=(.+)", RegexOptions.Compiled);

		private static IEnumerable<string> GetBrowserUrls()
		{
			HashSet<string> urls = new HashSet<string>();

			// FireFox
			foreach (WindowDetails window in WindowDetails.GetAllWindows("MozillaWindowClass"))
			{
				if (window.Text.Length == 0)
				{
					continue;
				}
				AutomationElement currentElement = AutomationElement.FromHandle(window.Handle);
				Condition conditionCustom = new AndCondition(new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Custom), new PropertyCondition(AutomationElement.IsOffscreenProperty, false));
				for (int i = 5; (i > 0) && (currentElement != null); i--)
				{
					currentElement = currentElement.FindFirst(TreeScope.Children, conditionCustom);
				}
				if (currentElement == null)
				{
					continue;
				}

				Condition conditionDocument = new AndCondition(new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document), new PropertyCondition(AutomationElement.IsOffscreenProperty, false));
				AutomationElement docElement = currentElement.FindFirst(TreeScope.Children, conditionDocument);
				if (docElement == null)
				{
					continue;
				}
				foreach (AutomationPattern pattern in docElement.GetSupportedPatterns())
				{
					if (pattern.ProgrammaticName != "ValuePatternIdentifiers.Pattern")
					{
						continue;
					}
					var valuePattern = docElement.GetCurrentPattern(pattern) as ValuePattern;
					string url = valuePattern?.Current.Value;
					if (!string.IsNullOrEmpty(url))
					{
						urls.Add(url);
						break;
					}
				}
			}

			foreach (string url in InternetExplorerCaptureSource.GetIeUrls())
			{
				urls.Add(url);
			}

			return urls;
		}

		public static async Task<IList<Content>> GetCurrentPages(CancellationToken token = default(CancellationToken))
		{
			IList<Content> pages = new List<Content>();
			var browserUrls = await Task.Factory.StartNew(GetBrowserUrls, token);
			foreach (string browserurl in browserUrls)
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
				var pageIdMatch = PageIdRegex.Matches(url);
				if (pageIdMatch.Count > 0)
				{
					string contentId = pageIdMatch[0].Groups[1].Value;
					try
					{
						bool pageDouble = false;
						foreach (var page in pages)
						{
							if (page.Id == contentId)
							{
								pageDouble = true;
								Log.Debug().WriteLine("Skipping double page with ID {0}", contentId);
								break;
							}
						}
						if (!pageDouble)
						{
							var page = await ConfluencePlugin.ConfluenceAPI.GetContentAsync(contentId, token).ConfigureAwait(false);
							Log.Debug().WriteLine("Adding page {0}", page.Title);
							pages.Add(page);
						}
						continue;
					}
					catch (Exception ex)
					{
						// Preventing security problems
						Log.Warn().WriteLine(ex, "Couldn't get page details for PageID {0}", contentId);
					}
				}
				MatchCollection spacePageMatch = DisplayRegex.Matches(url);
				if (spacePageMatch.Count > 0)
				{
					if (spacePageMatch[0].Groups.Count >= 2)
					{
						string space = spacePageMatch[0].Groups[1].Value;
						string title = spacePageMatch[0].Groups[2].Value;
						if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(space))
						{
							continue;
						}
						try
						{
							bool pageDouble = false;
							foreach (var page in pages)
							{
								if (page.Title == title)
								{
									Log.Debug().WriteLine("Skipping double page with title {0}", title);
									pageDouble = true;
									break;
								}
							}
							if (!pageDouble)
							{
								var contentList = await ConfluencePlugin.ConfluenceAPI.GetContentByTitleAsync(space, title, 0, 20, token).ConfigureAwait(false);
								if ((contentList != null) && (contentList.Results.Count > 0))
								{
									var content = contentList.Results.First();
									Log.Debug().WriteLine("Adding page {0}", content.Title);
									pages.Add(content);
								}
							}
							continue;
						}
						catch (Exception ex)
						{
							// Preventing security problems
							Log.Warn().WriteLine(ex, "Couldn't get page details for space {0} / title {1}", space, title);
						}
					}
				}
				var viewPageMatch = ViewPageRegex.Matches(url);
				if (viewPageMatch.Count > 0)
				{
					if (viewPageMatch[0].Groups.Count >= 2)
					{
						string title = viewPageMatch[0].Groups[1].Value;
						string space = viewPageMatch[0].Groups[2].Value;
						if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(space))
						{
							continue;
						}
						try
						{
							bool pageDouble = false;
							foreach (var page in pages)
							{
								if (page.Title == title)
								{
									Log.Debug().WriteLine("Skipping double page with title {0}", title);
									pageDouble = true;
									break;
								}
							}
							if (!pageDouble)
							{
								var contentList = await ConfluencePlugin.ConfluenceAPI.GetContentByTitleAsync(space, title, 0, 20, token).ConfigureAwait(false);
								if ((contentList != null) && (contentList.Results.Count > 0))
								{
									var content = contentList.Results.First();
									Log.Debug().WriteLine("Adding page {0}", content.Title);
									pages.Add(content);
								}
							}
						}
						catch (Exception ex)
						{
							// Preventing security problems
							Log.Warn().WriteLine(ex, "Couldn't get page details for space {0} / title {1}", space, title);
						}
					}
				}
			}
			return pages;
		}
	}
}