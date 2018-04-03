using System;
using Newtonsoft.Json;

namespace Greenshot.Addon.Box.Entities
{
    /// <summary>
    /// Box representation of an item in box
    /// </summary>
    public class BoxItem : BoxEntity
    {
        /// <summary>
        /// A unique ID for use with the /events endpoint
        /// </summary>
        [JsonProperty("sequence_id")]
        public string SequenceId { get; private set; }

        /// <summary>
        /// A unique string identifying the version of this item
        /// </summary>
        [JsonProperty("etag")]
        public string ETag { get; private set; }

        /// <summary>
        /// The name of the item
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; private set; }

        /// <summary>
        /// The description of the item
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; private set; }

        /// <summary>
        /// The folder size in bytes
        /// </summary>
        [JsonProperty("size")]
        public long? Size { get; private set; }
        
        /// <summary>
        /// The time the item was created
        /// </summary>
        [JsonProperty("created_at")]
        public DateTime? CreatedAt { get; private set; }

        /// <summary>
        /// The time the item or its contents were last modified
        /// </summary>
        [JsonProperty("modified_at")]
        public DateTime? ModifiedAt { get; private set; }

        /// <summary>
        /// Whether this item is deleted or not
        /// </summary>
        [JsonProperty("item_status")]
        public string ItemStatus { get; private set; }

        /// <summary>
        /// The shared link for this item
        /// </summary>
        [JsonProperty("shared_link")]
        public BoxSharedLink SharedLink { get; private set; }

        /// <summary>
        /// The tag for this item
        /// </summary>
        [JsonProperty("tags")]
        public string[] Tags { get; private set; }
    }
}
