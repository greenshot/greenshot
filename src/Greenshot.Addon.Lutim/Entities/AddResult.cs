using Newtonsoft.Json;

namespace Greenshot.Addon.Lutim.Entities
{
    public class AddResult
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("msg")]
        public LutimInfo LutimInfo { get; set; }
    }
}
