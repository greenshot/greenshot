using System;
using Newtonsoft.Json;

namespace Greenshot.Addon.Box.Entities
{
    /// <summary>
    /// Box representation of a shared link
    /// </summary>
    public class BoxSharedLink
    {
        /// <summary>
        /// The Url of the shared link
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; private set; }

        /// <summary>
        /// The Url of the download
        /// </summary>
        [JsonProperty("download_url")]
        public string DownloadUrl { get; private set; }

        /// <summary>
        /// An easily readible url
        /// </summary>
        [JsonProperty("vanity_url")]
        public string VanityUrl { get; private set; }

        /// <summary>
        /// Whether or not a password is enabled
        /// </summary>
        [JsonProperty("is_password_enabled")]
        public bool IsPasswordEnabled { get; private set; }

        /// <summary>
        /// When the item's share link will expire
        /// </summary>
        [JsonProperty("unshared_at")]
        public DateTime? UnsharedAt { get; private set; }

        /// <summary>
        /// Number of downloads
        /// </summary>
        [JsonProperty("download_count")]
        public int DownloadCount { get; private set; }

        /// <summary>
        /// Number of previews 
        /// </summary>
        [JsonProperty("preview_count")]
        public int PreviewCount { get; private set; }
    }
}
