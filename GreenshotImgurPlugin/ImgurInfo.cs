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
using System.Drawing;
using System.Xml;

namespace GreenshotImgurPlugin
{
	/// <summary>
	/// Description of ImgurInfo.
	/// </summary>
	public class ImgurInfo : IDisposable {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ImgurInfo));
		private string hash;
		public string Hash {
			get {return hash;}
			set {hash = value;}
		}

		private string deleteHash;
		public string DeleteHash {
			get {return deleteHash;}
			set {
				deleteHash = value;
				deletePage = "http://imgur.com/delete/" + value;
			}
		}

		private string title;
		public string Title {
			get {return title;}
			set {title = value;}
		}

		private string imageType;
		public string ImageType {
			get {return imageType;}
			set {imageType = value;}
		}

		private DateTime timestamp;
		public DateTime Timestamp {
			get {return timestamp;}
			set {timestamp = value;}
		}

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

		private string smallSquare;
		public string SmallSquare {
			get {return smallSquare;}
			set {smallSquare = value;}
		}

		private string largeThumbnail;
		public string LargeThumbnail {
			get {return largeThumbnail;}
			set {largeThumbnail = value;}
		}

		private string deletePage;
		public string DeletePage {
			get {return deletePage;}
			set {deletePage = value;}
		}
		private Image image;
		public Image Image {
			get {return image;}
			set {
				if (image != null) {
					image.Dispose();
				}
				image = value;
			}
		}

		public ImgurInfo() {
		}

		/// <summary>
		/// The public accessible Dispose
		/// Will call the GarbageCollector to SuppressFinalize, preventing being cleaned twice
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// This Dispose is called from the Dispose and the Destructor.
		/// When disposing==true all non-managed resources should be freed too!
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (image != null) {
					image.Dispose();
				}
			}
			image = null;
		}
		public static ImgurInfo ParseResponse(string response) {
			LOG.Debug(response);
			ImgurInfo imgurInfo = new ImgurInfo();
			try {
				XmlDocument doc = new XmlDocument();
				doc.LoadXml(response);
				XmlNodeList nodes = doc.GetElementsByTagName("hash");
				if(nodes.Count > 0) {
					imgurInfo.Hash = nodes.Item(0).InnerText;
				}
				nodes = doc.GetElementsByTagName("deletehash");
				if(nodes.Count > 0) {
					imgurInfo.DeleteHash = nodes.Item(0).InnerText;
				}
				nodes = doc.GetElementsByTagName("type");
				if(nodes.Count > 0) {
					imgurInfo.ImageType = nodes.Item(0).InnerText;
				}
				nodes = doc.GetElementsByTagName("title");
				if(nodes.Count > 0) {
					imgurInfo.Title = nodes.Item(0).InnerText;
				}
				nodes = doc.GetElementsByTagName("datetime");
				if(nodes.Count > 0) {
					imgurInfo.Timestamp =  DateTime.Parse(nodes.Item(0).InnerText);
				}
				nodes = doc.GetElementsByTagName("original");
				if(nodes.Count > 0) {
					imgurInfo.Original = nodes.Item(0).InnerText;
				}
				nodes = doc.GetElementsByTagName("imgur_page");
				if(nodes.Count > 0) {
					imgurInfo.Page = nodes.Item(0).InnerText;
				}
				nodes = doc.GetElementsByTagName("small_square");
				if(nodes.Count > 0) {
					imgurInfo.SmallSquare = nodes.Item(0).InnerText;
				}
				nodes = doc.GetElementsByTagName("large_thumbnail");
				if(nodes.Count > 0) {
					imgurInfo.LargeThumbnail = nodes.Item(0).InnerText;
				}
			} catch(Exception e) {
				LOG.ErrorFormat("Could not parse Imgur response due to error {0}, response was: {1}", e.Message, response);
			}
			return imgurInfo;
		}
	}
}
