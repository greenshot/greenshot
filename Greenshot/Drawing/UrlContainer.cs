/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
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
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Forms;

using Greenshot.Configuration;
using Greenshot.Drawing.Fields;
using Greenshot.Helpers;

namespace Greenshot.Drawing {
	/// <summary>
	/// Description of BitmapContainer.
	/// </summary>
	[Serializable()] 
	public class UrlContainer : DrawableContainer {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(UrlContainer));

		protected Bitmap bitmap = null;
		protected string url = null;
		private Size lastSize = new Size(0, 0);

		public UrlContainer(Surface parent, string url) : this(parent) {
			Url = url;
		}

		public UrlContainer(Surface parent) : base(parent) {
			Width = 100;
			Height = 100;
		}

		public string Url {
			set {
				url = value;
				lastSize = new Size(0, 0);
				RedrawIfNeeded();
			}
			get { return url; }
		}

		/**
		 * Destructor
		 */
		~UrlContainer() {
			Dispose(false);
		}

		/**
		 * The public accessible Dispose
		 * Will call the GarbageCollector to SuppressFinalize, preventing being cleaned twice
		 */
		public new void Dispose() {
			Dispose(true);
			base.Dispose();
			GC.SuppressFinalize(this);
		}

		// The bulk of the clean-up code is implemented in Dispose(bool)

		/**
		 * This Dispose is called from the Dispose and the Destructor.
		 * When disposing==true all non-managed resources should be freed too!
		 */
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (bitmap != null) {
					bitmap.Dispose();
				}
			}
			bitmap = null;
		}

		protected void RedrawIfNeeded() {
			if (url == null) {
				if (bitmap != null) {
					bitmap.Dispose();
				}
				bitmap = null;
				return;
			}
			if (bitmap == null || lastSize.Height != Height || lastSize.Width != Width) {
				bitmap = WebsiteImageGenerator.GetImageFromURL(url);
				if (bitmap != null) {
					lastSize = new Size(bitmap.Width, bitmap.Height);
				}
			}
		}
		
		public override void Draw(Graphics g, RenderMode rm) {
			RedrawIfNeeded();
			if (bitmap != null) {
				g.DrawImage(bitmap, Bounds);
			}
		}
	}
}
