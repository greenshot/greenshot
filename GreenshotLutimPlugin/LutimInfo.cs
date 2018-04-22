/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Drawing;
using System.Xml;
using Newtonsoft.Json;

namespace GreenshotLutimPlugin
{
    /// <summary>
    /// Description of LutimInfo.
    /// </summary>
    public class LutimInfo : IDisposable
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(LutimInfo));

        public string Hash
        {
            get;
            set;
        }

        private string _deleteHash;
        public string DeleteHash
        {
            get { return _deleteHash; }
            set
            {
                _deleteHash = value;
                DeletePage = "https://imgur.com/delete/" + value;
            }
        }

        public string Title
        {
            get;
            set;
        }

        public string ImageType
        {
            get;
            set;
        }

        public DateTime Timestamp
        {
            get;
            set;
        }

        public string Original
        {
            get;
            set;
        }

        public string Page
        {
            get;
            set;
        }

        public string SmallSquare
        {
            get;
            set;
        }

        public string LargeThumbnail
        {
            get;
            set;
        }

        public string DeletePage
        {
            get;
            set;
        }


        private Image _image;
        public Image Image
        {
            get { return _image; }
            set
            {
                _image?.Dispose();
                _image = value;
            }
        }

        /// <summary>
        /// The public accessible Dispose
        /// Will call the GarbageCollector to SuppressFinalize, preventing being cleaned twice
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// This Dispose is called from the Dispose and the Destructor.
        /// When disposing==true all non-managed resources should be freed too!
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _image?.Dispose();
            }
            _image = null;
        }

        public static LutimInfo ParseResponse(string response)
        {
            Log.Debug(response);

            LutimInfo lutimInfo = new LutimInfo();
            try
            {
                var data = JsonConvert.DeserializeObject<JsonData>(response).msg;
                lutimInfo.Hash = data.shorter;
                lutimInfo.ImageType = data.ext;
                lutimInfo.Title = data.filename;
                lutimInfo.DeleteHash = data.token;

                double secondsSince;
                if (double.TryParse(data.created_at, out secondsSince))
                {
                    var epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
                    lutimInfo.Timestamp = epoch.AddSeconds(secondsSince).DateTime;
                }

                lutimInfo.Original = $"https://framapic.org/{data.shorter}.{data.ext}";
                lutimInfo.Page = $"https://framapic.org/gallery#{data.shorter}.{data.ext}";

                //XmlNodeList nodes = doc.GetElementsByTagName("id");
                //if (nodes.Count > 0)
                //{
                //    lutimInfo.Hash = nodes.Item(0)?.InnerText;
                //}
                //nodes = doc.GetElementsByTagName("hash");
                //if (nodes.Count > 0)
                //{
                //    lutimInfo.Hash = nodes.Item(0)?.InnerText;
                //}
                //nodes = doc.GetElementsByTagName("deletehash");
                //if (nodes.Count > 0)
                //{
                //    lutimInfo.DeleteHash = nodes.Item(0)?.InnerText;
                //}
                //nodes = doc.GetElementsByTagName("type");
                //if (nodes.Count > 0)
                //{
                //    lutimInfo.ImageType = nodes.Item(0)?.InnerText;
                //}
                //nodes = doc.GetElementsByTagName("title");
                //if (nodes.Count > 0)
                //{
                //    lutimInfo.Title = nodes.Item(0)?.InnerText;
                //}
                //nodes = doc.GetElementsByTagName("datetime");
                //if (nodes.Count > 0)
                //{
                //    // Version 3 has seconds since Epoch
                //    double secondsSince;
                //    if (double.TryParse(nodes.Item(0)?.InnerText, out secondsSince))
                //    {
                //        var epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
                //        lutimInfo.Timestamp = epoch.AddSeconds(secondsSince).DateTime;
                //    }
                //}
                //nodes = doc.GetElementsByTagName("original");
                //if (nodes.Count > 0)
                //{
                //    lutimInfo.Original = nodes.Item(0)?.InnerText.Replace("http:", "https:");
                //}
                //// Version 3 API only has Link
                //nodes = doc.GetElementsByTagName("link");
                //if (nodes.Count > 0)
                //{
                //    lutimInfo.Original = nodes.Item(0)?.InnerText.Replace("http:", "https:");
                //}
                //nodes = doc.GetElementsByTagName("Lutim_page");
                //if (nodes.Count > 0)
                //{
                //    lutimInfo.Page = nodes.Item(0)?.InnerText.Replace("http:", "https:");
                //}
                //else
                //{
                //    // Version 3 doesn't have a page link in the response
                //    lutimInfo.Page = $"https://Lutim.com/{lutimInfo.Hash}";
                //}
                //nodes = doc.GetElementsByTagName("small_square");
                //lutimInfo.SmallSquare = nodes.Count > 0 ? nodes.Item(0)?.InnerText : $"http://i.Lutim.com/{lutimInfo.Hash}s.png";
            }
            catch (Exception e)
            {
                Log.ErrorFormat("Could not parse Lutim response due to error {0}, response was: {1}", e.Message, response);
            }
            return lutimInfo;
        }
    }
}

// ReSharper disable InconsistentNaming
public class JsonData
{
    public JsonMessage msg { get; set; }
    public bool success { get; set; }
}

public class JsonMessage
{
    public bool del_at_view { get; set; }
    public string thumb { get; set; }

    [JsonProperty(PropertyName = "short")]
    public string shorter { get; set; }
    public string real_short { get; set; }
    public string created_at { get; set; }
    public string filename { get; set; }
    public string limit { get; set; }
    public string token { get; set; }
    public string ext { get; set; }
}
