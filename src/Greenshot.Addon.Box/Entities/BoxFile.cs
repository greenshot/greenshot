using System;
using Newtonsoft.Json;

namespace Greenshot.Addon.Box.Entities
{
    /// <summary>
    /// Box representation of a file
    /// </summary>
    public class BoxFile : BoxItem
    {
        /// <summary>
        /// The sha1 hash of this file
        /// </summary>
        [JsonProperty(PropertyName = "sha1")]
        public string Sha1 { get; private set; }

        /// <summary>
        /// When this file was last moved to the trash
        /// </summary>
        [JsonProperty(PropertyName = "trashed_at")]
        public DateTime? TrashedAt { get; private set; }

        /// <summary>
        /// When this file will be permanently deleted
        /// </summary>
        [JsonProperty(PropertyName = "purged_at")]
        public DateTime? PurgedAt { get; private set; }

        /// <summary>
        /// When the content of this file was created
        /// For more information about content times <see>http://developers.box.com/content-times/</see>
        /// </summary>
        [JsonProperty(PropertyName = "content_created_at")]
        public DateTime? ContentCreatedAt { get; private set; }

        /// <summary>
        /// When the content of this file was last modified
        /// For more information about content times <see>http://developers.box.com/content-times/</see>
        /// </summary>
        [JsonProperty(PropertyName = "content_modified_at")]
        public DateTime? ContentModifiedAt { get; private set; }

        /// <summary>
        /// The version of the file
        /// </summary>
        [JsonProperty(PropertyName = "version_number")]
        public string VersionNumber { get; private set; }

        /// <summary>
        /// Indicates the suffix, when available, on the file.
        /// </summary>
        [JsonProperty(PropertyName = "extension")]
        public string Extension { get; private set; }

        /// <summary>
        /// The number of comments on a file
        /// </summary>
        [JsonProperty(PropertyName = "comment_count")]
        public int CommentCount { get; private set; }
    }
}
