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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Greenshot.Addon.Imgur.Entities
{
    /// <summary>
    /// Contains the information on the image
    /// </summary>
    public class ImgurData
    {
#pragma warning disable 1591
        /// <summary>
        /// Id of the image
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Title of the image
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Description of the image
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Date created
        /// </summary>
        [JsonProperty("datetime"), JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTime Datetime { get; set; }

        /// <summary>
        /// Type of image
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Specifies if the image is animated
        /// </summary>
        [JsonProperty("animated")]
        public bool Animated { get; set; }

        /// <summary>
        /// The width of the image
        /// </summary>
        [JsonProperty("width")]
        public long Width { get; set; }

        /// <summary>
        /// The height of the image
        /// </summary>
        [JsonProperty("height")]
        public long Height { get; set; }

        /// <summary>
        /// The size of the image
        /// </summary>
        [JsonProperty("size")]
        public long Size { get; set; }

        /// <summary>
        /// How many times is this image viewed
        /// </summary>
        [JsonProperty("views")]
        public long Views { get; set; }

        [JsonProperty("bandwidth")]
        public long Bandwidth { get; set; }

        [JsonProperty("favorite")]
        public bool Favorite { get; set; }

        [JsonProperty("section")]
        public string Section { get; set; }

        [JsonProperty("account_url")]
        public string AccountUrl { get; set; }

        [JsonProperty("account_id")]
        public long AccountId { get; set; }

        [JsonProperty("is_ad")]
        public bool IsAd { get; set; }

        [JsonProperty("in_most_viral")]
        public bool InMostViral { get; set; }

        [JsonProperty("has_sound")]
        public bool HasSound { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        [JsonProperty("ad_type")]
        public long AdType { get; set; }

        [JsonProperty("ad_url")]
        public string AdUrl { get; set; }

        [JsonProperty("in_gallery")]
        public bool InGallery { get; set; }

        [JsonProperty("deletehash")]
        public string Deletehash { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("link")]
        public Uri Link { get; set; }

        /// <summary>
        /// Link to the page
        /// </summary>
        [JsonIgnore]
        public Uri LinkPage => Id == null ? null : new Uri($"http://i.imgur.com/{Id}");

        /// <summary>
        /// Link to a small thumbnail
        /// </summary>
        [JsonIgnore]
        public Uri LinkThumbnail => Id == null ? null : new Uri($"http://i.imgur.com/{Id}s.png");
    }
}