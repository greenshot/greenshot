/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using Dapplo.Log;
using GreenshotPlugin.Core;

namespace Greenshot.Addon.Lutim
{
    /// <summary>
    /// Description of LutimInfo.
    /// </summary>
    public class LutimInfo : IDisposable
    {
		private static readonly LogSource Log = new LogSource();

		public DateTime CreatedAt { get; set; }

        public bool DelAtView { get; set; }

        public string Ext { get; set; }

        public string Filename { get; set; }

        public TimeSpan Limit { get; set; }

        public string RealShort { get; set; }

        public string Short { get; set; }

        private Image _thumb;

        public Image Thumb
        {
            get { return _thumb; }
            set
            {
                _thumb?.Dispose();
                _thumb = value;
            }
        }
        public string Token { get; set; }

        public Uri LutimBaseUri { get; set; }

        public Uri Uri
        {
            get { return new Uri(LutimBaseUri, Short); }
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
                _thumb?.Dispose();
            }
            _thumb = null;
        }
        public static LutimInfo ParseResponse(string lutimBaseUri, string response)
        {
            Log.Debug().WriteLine(response);

            LutimInfo lutimInfo = new LutimInfo();
            try
            {
                var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var r = JSONHelper.JsonDecode(response);
                if ((bool)r[@"success"])
                {
                    var msg = (IDictionary<string, object>)r[@"msg"];
                    lutimInfo.CreatedAt = epoch.AddSeconds((double)msg[@"created_at"]);
                    lutimInfo.DelAtView = (bool)msg[@"del_at_view"];
                    lutimInfo.Ext = (string)msg[@"ext"];
                    lutimInfo.Filename = (string)msg[@"filename"];
                    object oLimit = msg[@"limit"];
                    if (oLimit is double)
                        lutimInfo.Limit = TimeSpan.FromDays((double)oLimit);
                    else
                        lutimInfo.Limit = TimeSpan.FromDays(double.Parse((string)oLimit));
                    lutimInfo.RealShort = (string)msg[@"real_short"];
                    lutimInfo.Short = (string)msg[@"short"];
                    lutimInfo.Token = (string)msg[@"token"];
                    lutimInfo.LutimBaseUri = new Uri(lutimBaseUri);

                    using (var ms = new MemoryStream(Convert.FromBase64String(((string)msg[@"thumb"]).Split(',')[1])))
                        lutimInfo.Thumb = Image.FromStream(ms);
                }
            }
            catch (Exception e)
            {
                Log.Error().WriteLine("Could not parse Lutim response due to error {0}, response was: {1}", e.Message, response);
            }
            return lutimInfo;
        }

        public string ToIniString()
        {
            var sb = new StringBuilder();
            sb.Append(CreatedAt.ToBinary());
            sb.Append(';');
            sb.Append(DelAtView);
            sb.Append(';');
            sb.Append(Ext);
            sb.Append(';');
            sb.Append(Filename);
            sb.Append(';');
            sb.Append(Limit.TotalDays);
            sb.Append(';');
            sb.Append(RealShort);
            sb.Append(';');
            sb.Append(Token);
            sb.Append(';');
            sb.Append(LutimBaseUri);
            sb.Append(';');
            using (var ms = new MemoryStream())
            {
                Thumb.Save(ms, ImageFormat.Png);
                ms.Seek(0, SeekOrigin.Begin);
                var bytes = ms.ToArray();
                sb.Append(Convert.ToBase64String(bytes));
            }
            return sb.ToString();
        }

        public static LutimInfo FromIniString(string key, string iniString)
        {
            var values = iniString.Split(';');
            if (values.Length != 9)
                throw new ArgumentException("Invalid format", nameof(iniString));

            var lutimInfo = new LutimInfo
            {
                Short = key,
                CreatedAt = DateTime.FromBinary(long.Parse(values[0])),
                DelAtView = bool.Parse(values[1]),
                Ext = values[2],
                Filename = values[3],
                Limit = TimeSpan.FromDays(double.Parse(values[4])),
                RealShort = values[5],
                Token = values[6],
                LutimBaseUri = new Uri(values[7])
            };

            using (var ms = new MemoryStream(Convert.FromBase64String(values[8])))
                lutimInfo.Thumb = Image.FromStream(ms);

            return lutimInfo;
        }
    }
}
