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
using Newtonsoft.Json;

namespace Greenshot.Addon.Tfs.Entities
{
    /// <summary>
    /// The workitem entity
    /// </summary>
    [JsonObject]
    public class WorkItem
    {
        /// <summary>
        /// ID of the workitem
        /// </summary>
        [JsonProperty("id")]
        public long Id { get; set; }

        /// <summary>
        /// The revision of the workitem
        /// </summary>
        [JsonProperty("rev")]
        public int Revision { get; set; }

        /// <summary>
        /// The URL of the workitem
        /// </summary>
        [JsonProperty("url")]
        public Uri Url { get; set; }

        /// <summary>
        /// The fields of the workitem
        /// </summary>
        [JsonProperty("fields")]
        public WorkItemFields Fields { get; set; }
    }
}
