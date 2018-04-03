using Newtonsoft.Json;

namespace Greenshot.Addon.Dropbox.Entities
{
    internal class ErrorTag
    {
        [JsonProperty(".tag")]
        public string Tag { get; set; }
    }
}