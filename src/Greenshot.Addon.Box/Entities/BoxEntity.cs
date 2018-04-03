using Newtonsoft.Json;

namespace Greenshot.Addon.Box.Entities
{
    /// <summary>
    /// Represents the base class for most Box model objects
    /// </summary>
    public class BoxEntity
    {
        /// <summary>
        /// The item’s ID
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; protected set; }

        /// <summary>
        /// The type of the item
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; protected set; }
    }
}
