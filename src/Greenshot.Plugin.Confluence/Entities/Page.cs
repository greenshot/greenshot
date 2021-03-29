/*
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

using GreenshotConfluencePlugin.confluence;

namespace Greenshot.Plugin.Confluence.Entities
{
    public class Page
    {
        public Page(RemotePage page)
        {
            Id = page.id;
            Title = page.title;
            SpaceKey = page.space;
            Url = page.url;
            Content = page.content;
        }

        public Page(RemoteSearchResult searchResult, string space)
        {
            Id = searchResult.id;
            Title = searchResult.title;
            SpaceKey = space;
            Url = searchResult.url;
            Content = searchResult.excerpt;
        }

        public Page(RemotePageSummary pageSummary)
        {
            Id = pageSummary.id;
            Title = pageSummary.title;
            SpaceKey = pageSummary.space;
            Url = pageSummary.url;
        }

        public long Id { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string Content { get; set; }
        public string SpaceKey { get; set; }
    }
}