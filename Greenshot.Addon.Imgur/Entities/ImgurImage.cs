using System.Windows.Media;
using Newtonsoft.Json;

namespace Greenshot.Addon.Imgur.Entities
{
    /// <summary>
    /// Information on the imgur image
    /// </summary>
    public class ImgurImage
    {
        [JsonIgnore]
        public ImageSource Image { get; set; }

        [JsonProperty("data")]
        public ImgurData Data { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("status")]
        public long Status { get; set; }
    }
}
