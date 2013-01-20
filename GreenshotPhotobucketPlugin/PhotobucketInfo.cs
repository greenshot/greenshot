/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
using System.Xml;

namespace GreenshotPhotobucketPlugin
{
	/// <summary>
	/// Description of PhotobucketInfo.
	/// </summary>
	public class PhotobucketInfo {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PhotobucketInfo));

		private string original;
		public string Original {
			get {return original;}
			set {original = value;}
		}

		private string page;
		public string Page {
			get {return page;}
			set {page = value;}
		}

		private string thumbnail;
		public string Thumbnail {
			get {return thumbnail;}
			set {thumbnail = value;}
		}

		public PhotobucketInfo() {
		}

		/// <summary>
		/// Parse the upload response
		/// </summary>
		/// <param name="response">XML</param>
		/// <returns>PhotobucketInfo object</returns>
		public static PhotobucketInfo FromUploadResponse(string response) {
			LOG.Debug(response);
			PhotobucketInfo PhotobucketInfo = new PhotobucketInfo();
			try {
				XmlDocument doc = new XmlDocument();
				doc.LoadXml(response);
				XmlNodeList nodes;
				nodes = doc.GetElementsByTagName("url");
				if(nodes.Count > 0) {
					PhotobucketInfo.Original = nodes.Item(0).InnerText;
				}
				nodes = doc.GetElementsByTagName("browseurl");
				if(nodes.Count > 0) {
					PhotobucketInfo.Page = nodes.Item(0).InnerText;
				}
				nodes = doc.GetElementsByTagName("thumb");
				if(nodes.Count > 0) {
					PhotobucketInfo.Thumbnail = nodes.Item(0).InnerText;
				}
			} catch(Exception e) {
				LOG.ErrorFormat("Could not parse Photobucket response due to error {0}, response was: {1}", e.Message, response);
			}
			return PhotobucketInfo;
		}
	}
}
