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

using GreenshotConfluencePlugin.Model;
using GreenshotPlugin.Core;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Automation;
using log4net;

namespace GreenshotConfluencePlugin
{
	/// <summary>
	/// Confluence utils are to support the confluence plugin with retrieving the current confluence pages.
	/// </summary>
	public class ConfluenceUtils
	{
		private static readonly ILog LOG = LogManager.GetLogger(typeof (ConfluenceUtils));
		private static readonly Regex pageIdRegex = new Regex(@"pageId=(\d+)", RegexOptions.Compiled);
		private static readonly Regex displayRegex = new Regex(@"\/display\/([^\/]+)\/([^#]+)", RegexOptions.Compiled);
		private static readonly Regex viewPageRegex = new Regex(@"pages\/viewpage.action\?title=(.+)&spaceKey=(.+)", RegexOptions.Compiled);

		public static async Task<IList<Content>> GetCurrentPages()
		{
			IList<Content> pages = new List<Content>();
			foreach (string browserurl in GetBrowserUrls())
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
				MatchCollection pageIdMatch = pageIdRegex.Matches(url);
				if (pageIdMatch.Count > 0)
				{
					long contentId;
					if (!long.TryParse(pageIdMatch[0].Groups[1].Value, out contentId))
					{
						continue;
					}
					try
					{
						bool pageDouble = false;
						foreach (var page in pages)
						{
							if (page.Id == contentId)
							{
								pageDouble = true;
								LOG.DebugFormat("Skipping double page with ID {0}", contentId);
								break;
							}
						}
						if (!pageDouble)
						{
							var page = await ConfluencePlugin.ConfluenceAPI.GetContentAsync(contentId).ConfigureAwait(false);
							LOG.DebugFormat("Adding page {0}", page.Title);
							pages.Add(page);
						}
						continue;
					}
					catch (Exception ex)
					{
						// Preventing security problems
						LOG.DebugFormat("Couldn't get page details for PageID {0}", contentId);
						LOG.Warn(ex);
					}
				}
				MatchCollection spacePageMatch = displayRegex.Matches(url);
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
									LOG.DebugFormat("Skipping double page with title {0}", title);
									pageDouble = true;
									break;
								}
							}
							if (!pageDouble)
							{
								var content = await ConfluencePlugin.ConfluenceAPI.SearchPageAsync(space, title).ConfigureAwait(false);
								LOG.DebugFormat("Adding page {0}", content.Title);
								pages.Add(content);
							}
							continue;
						}
						catch (Exception ex)
						{
							// Preventing security problems
							LOG.DebugFormat("Couldn't get page details for space {0} / title {1}", space, title);
							LOG.Warn(ex);
						}
					}
				}
				MatchCollection viewPageMatch = viewPageRegex.Matches(url);
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
									LOG.DebugFormat("Skipping double page with title {0}", title);
									pageDouble = true;
									break;
								}
							}
							if (!pageDouble)
							{
								var content = await ConfluencePlugin.ConfluenceAPI.SearchPageAsync(space, title).ConfigureAwait(false);
								if (content != null)
								{
									LOG.DebugFormat("Adding page {0}", content.Title);
									pages.Add(content);
								}
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
				for (int i = 5; i > 0 && currentElement != null; i--)
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
					if (valuePattern != null)
					{
						string url = valuePattern.Current.Value;
						if (!string.IsNullOrEmpty(url))
						{
							urls.Add(url);
							break;
						}
					}
				}
			}

			foreach (string url in IEHelper.GetIEUrls())
			{
				urls.Add(url);
			}

			return urls;
		}
	}
}