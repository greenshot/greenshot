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
using System.Globalization;
using Newtonsoft.Json;
using Greenshot.IniFile;

namespace GreenshotLutimPlugin
{
    /// <summary>
    /// Description of LutimInfo.
    /// </summary>
    public class LutimInfo : IDisposable
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(LutimInfo));
        private static readonly LutimConfiguration Config = IniConfig.GetIniSection<LutimConfiguration>();

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
                    lutimInfo.Timestamp = epoch.AddSeconds(secondsSince).DateTime.ToLocalTime();
                }

                lutimInfo.Original = $"{Config.LutimApiUrl}/{data.shorter}.{data.ext}";
                lutimInfo.Page = $"{Config.LutimApiUrl}/gallery#{data.shorter}.{data.ext}";
            }
            catch (Exception e)
            {
                Log.ErrorFormat("Could not parse Lutim response due to error {0}, response was: {1}", e.Message, response);
            }
            return lutimInfo;
        }

        public static LutimInfo ParseFromData(LutimData data)
        {
            var lutimInfo = new LutimInfo();

            lutimInfo.Hash = data.Hash;
            lutimInfo.ImageType = data.ImageType;
            lutimInfo.Title = data.Filename;
            lutimInfo.DeleteHash = data.DeleteHash;
            lutimInfo.Original = $"{Config.LutimApiUrl}/{data.Hash}.{data.ImageType}";
            lutimInfo.Page = $"{Config.LutimApiUrl}/gallery#{data.Hash}.{data.ImageType}";

            DateTime.TryParseExact(data.Filename.Split('.')[0], "yyyyMMdd-HHmm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedData);
            lutimInfo.Timestamp = parsedData;
            
            return lutimInfo;
        }
    }
}

// ReSharper disable InconsistentNaming
public class LutimData
{
    public string Hash { get; set; }
    public string DeleteHash { get; set; }
    public string ImageType { get; set; }
    public string Filename { get; set; }
}

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
