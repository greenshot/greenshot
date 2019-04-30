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
using System.Drawing;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Greenshot.Addon.Lutim.Entities
{
    /// <summary>
    /// This is the entity which Lutim returns
    /// </summary>
    public class LutimInfo
    {
#pragma warning disable 1591
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