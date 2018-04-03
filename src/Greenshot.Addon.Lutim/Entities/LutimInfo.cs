using System;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Greenshot.Addon.Lutim.Entities
{
    public class LutimInfo
    {
        [JsonProperty("real_short")]
        public string RealShort { get; set; }

        [JsonProperty("short")]
        public string Short { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("thumb")]
        public string ThumbBase64 { get; set; }

        [JsonIgnore]
        public Image Thumbnail
        {
            get
            {
                using (var memoryStream = new MemoryStream(Convert.FromBase64String(ThumbBase64)))
                {
                    return Image.FromStream(memoryStream);
                }
            }
        }

        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("created_at"), JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("del_at_view")]
        public bool DelAtView { get; set; }

        [JsonProperty("ext")]
        public string Ext { get; set; }

        [JsonProperty("limit")]
        public long Limit { get; set; }
    }
}