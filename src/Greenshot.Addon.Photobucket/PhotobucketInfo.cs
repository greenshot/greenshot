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
using System.Xml;
using Dapplo.Log;

namespace Greenshot.Addon.Photobucket
{
	/// <summary>
	///     Information about a photobucket upload
	/// </summary>
	public class PhotobucketInfo
	{
		private static readonly LogSource Log = new LogSource();

		/// <summary>
		/// Link to the original file
		/// </summary>
		public string Original { get; set; }

		/// <summary>
		/// Link to the page
		/// </summary>
		public string Page { get; set; }

		/// <summary>
		/// Link to the thumbnail
		/// </summary>
		public string Thumbnail { get; set; }

		/// <summary>
		///     Parse the upload response
		/// </summary>
		/// <param name="response">XML</param>
		/// <returns>PhotobucketInfo object</returns>
		public static PhotobucketInfo FromUploadResponse(string response)
		{
			Log.Debug().WriteLine(response);
			var photobucketInfo = new PhotobucketInfo();
			try
			{
				var doc = new XmlDocument();
				doc.LoadXml(response);
				var nodes = doc.GetElementsByTagName("url");
				if (nodes.Count > 0)
				{
					photobucketInfo.Original = nodes.Item(0)?.InnerText;
				}
				nodes = doc.GetElementsByTagName("browseurl");
				if (nodes.Count > 0)
				{
					photobucketInfo.Page = nodes.Item(0)?.InnerText;
				}
				nodes = doc.GetElementsByTagName("thumb");
				if (nodes.Count > 0)
				{
					photobucketInfo.Thumbnail = nodes.Item(0)?.InnerText;
				}
			}
			catch (Exception e)
			{
				Log.Error().WriteLine("Could not parse Photobucket response due to error {0}, response was: {1}", e.Message, response);
			}
			return photobucketInfo;
		}
	}
}