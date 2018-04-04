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
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("datetime"), JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTime Datetime { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("animated")]
        public bool Animated { get; set; }

        [JsonProperty("width")]
        public long Width { get; set; }

        [JsonProperty("height")]
        public long Height { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

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