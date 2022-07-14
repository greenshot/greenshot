﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Automation;
using Greenshot.Base.Core;
using Greenshot.Plugin.Confluence.Entities;

namespace Greenshot.Plugin.Confluence
{
    /// <summary>
    /// Description of ConfluenceUtils.
    /// </summary>
    public static class ConfluenceUtils
    {
        private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ConfluenceUtils));

        public static List<Page> GetCurrentPages()
        {
            List<Page> pages = new();
            Regex pageIdRegex = new(@"pageId=(\d+)");
            Regex spacePageRegex = new(@"\/display\/([^\/]+)\/([^#]+)");
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
                if (pageIdMatch?.Count > 0)
                {
                    long pageId = long.Parse(pageIdMatch[0].Groups[1].Value);
                    try
                    {
                        bool pageDouble = false;
                        foreach (Page page in pages)
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
                            Page page = ConfluencePlugin.ConfluenceConnector.GetPage(pageId);
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

                MatchCollection spacePageMatch = spacePageRegex.Matches(url);
                if (spacePageMatch?.Count > 0 && spacePageMatch[0].Groups.Count >= 2)
                {
                    string space = spacePageMatch[0].Groups[1].Value;
                    string title = spacePageMatch[0].Groups[2].Value;
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
                        bool pageDouble = false;
                        foreach (var _ in from Page page in pages
                                          where page.Title.Equals(title)
                                          select new { })
                        {
                            LOG.DebugFormat("Skipping double page with title {0}", title);
                            pageDouble = true;
                            break;
                        }

                        if (!pageDouble)
                        {
                            Page page = ConfluencePlugin.ConfluenceConnector.GetPage(space, title);
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

            return pages;
        }

        private static IEnumerable<string> GetBrowserUrls()
        {
            HashSet<string> urls = new();

            // FireFox
            foreach (WindowDetails window in WindowDetails.GetAllWindows("MozillaWindowClass"))
            {
                if (window.Text.Length == 0)
                {
                    continue;
                }

                AutomationElement currentElement = AutomationElement.FromHandle(window.Handle);
                Condition conditionCustom = new AndCondition(new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Custom),
                    new PropertyCondition(AutomationElement.IsOffscreenProperty, false));
                for (int i = 5; i > 0 && currentElement != null; i--)
                {
                    currentElement = currentElement.FindFirst(TreeScope.Children, conditionCustom);
                }

                if (currentElement == null)
                {
                    continue;
                }

                Condition conditionDocument = new AndCondition(new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document),
                    new PropertyCondition(AutomationElement.IsOffscreenProperty, false));
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

                    string url = (docElement.GetCurrentPattern(pattern) as ValuePattern).Current.Value;
                    if (!string.IsNullOrEmpty(url))
                    {
                        urls.Add(url);
                        break;
                    }
                }
            }

            foreach (string url in IEHelper.GetIEUrls().Distinct())
            {
                urls.Add(url);
            }

            return urls;
        }
    }
}