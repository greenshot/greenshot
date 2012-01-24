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
using System.Text.RegularExpressions;
using System.Windows.Automation;

using GreenshotPlugin.Core;
using Confluence;

namespace GreenshotConfluencePlugin {
	/// <summary>
	/// Description of ConfluenceUtils.
	/// </summary>
	public class ConfluenceUtils {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ConfluenceUtils));

		public static List<Confluence.Page> GetCurrentPages() {
			List<Confluence.Page> pages = new List<Confluence.Page>();
			Regex pageIdRegex = new Regex(@"pageId=(\d+)");
			Regex spacePageRegex = new Regex(@"\/display\/([^\/]+)\/([^#]+)");
			foreach(string browserurl in  GetBrowserUrls()) {
				string url = null;
				try {
					url = Uri.UnescapeDataString(browserurl).Replace("+", " ");
				} catch {
					LOG.WarnFormat("Error processing URL: {0}", browserurl);
					continue;
				}
				MatchCollection pageIdMatch = pageIdRegex.Matches(url);
				if (pageIdMatch != null && pageIdMatch.Count > 0) {
					long pageId = long.Parse(pageIdMatch[0].Groups[1].Value);
					try {
						bool pageDouble = false;
						foreach(Confluence.Page page in pages) {
							if (page.id == pageId) {
								pageDouble = true;
								LOG.DebugFormat("Skipping double page with ID {0}", pageId);
								break;
							}
						}
						if (!pageDouble) {
							Confluence.Page page = ConfluencePlugin.ConfluenceConnector.getPage(pageId);
							LOG.DebugFormat("Adding page {0}", page.Title);
							pages.Add(page);
						}
						continue;
					} catch (Exception ex) {
						// Preventing security problems
						LOG.DebugFormat("Couldn't get page details for PageID {0}", pageId);
						LOG.Warn(ex);
					}
				}
				MatchCollection spacePageMatch = spacePageRegex.Matches(url);
				if (spacePageMatch != null && spacePageMatch.Count > 0) {
					if (spacePageMatch[0].Groups.Count >= 2) {
						string space = spacePageMatch[0].Groups[1].Value;
						string title = spacePageMatch[0].Groups[2].Value;
						if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(space)) {
							continue;
						}
						if (title.EndsWith("#")) {
							title = title.Substring(0, title.Length-1);
						}
						try {
							bool pageDouble = false;
							foreach(Confluence.Page page in pages) {
								if (page.Title.Equals(title)) {
									LOG.DebugFormat("Skipping double page with title {0}", title);
									pageDouble = true;
									break;
								}
							}
							if (!pageDouble) {
								Confluence.Page page = ConfluencePlugin.ConfluenceConnector.getPage(space, title);
								LOG.DebugFormat("Adding page {0}", page.Title);
								pages.Add(page);
								
							}
							continue;
						} catch (Exception ex) {
							// Preventing security problems
							LOG.DebugFormat("Couldn't get page details for space {0} / title {1}", space, title);
							LOG.Warn(ex);
						}
					}
				}
			}
			return pages;
		}		

		private static IEnumerable<string> GetBrowserUrls() {
			HashSet<string> urls = new HashSet<string>();

			// FireFox
			foreach(Process firefox in Process.GetProcessesByName("firefox")) {
				LOG.DebugFormat("Checking process {0}", firefox.Id);
				foreach(AutomationElement rootElement in AutomationElement.RootElement.FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.ProcessIdProperty, firefox.Id))) {
					Condition condCustomControl = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Custom);
					//AutomationElement rootElement = AutomationElement.FromHandle(firefox.MainWindowHandle);
					var collection = rootElement.FindAll(TreeScope.Children, condCustomControl);
					if (collection == null || collection.Count == 0) {
						continue;
					}
					AutomationElement firstCustomControl = collection[collection.Count - 1];
					if (firstCustomControl == null) {
						continue;
					}
					AutomationElement secondCustomControl = firstCustomControl.FindFirst(TreeScope.Children, condCustomControl);
					if (secondCustomControl == null) {
						continue;
					}
					foreach (AutomationElement thirdElement in secondCustomControl.FindAll(TreeScope.Children, condCustomControl)) {
						foreach (AutomationElement fourthElement in thirdElement.FindAll(TreeScope.Children, condCustomControl)) {
							Condition condDocument = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document);
							AutomationElement docElement = fourthElement.FindFirst(TreeScope.Children, condDocument);
							if (docElement != null) {
								foreach (AutomationPattern pattern in docElement.GetSupportedPatterns()) {
									if (docElement.GetCurrentPattern(pattern) is ValuePattern) {
										string url = (docElement.GetCurrentPattern(pattern) as ValuePattern).Current.Value.ToString();
										urls.Add(url);
									}
								}
							}
						}
					}
				}
			}

			foreach(string url in IEHelper.GetIEUrls()) {
				urls.Add(url);
			}

			return urls;
		}

	}
}
