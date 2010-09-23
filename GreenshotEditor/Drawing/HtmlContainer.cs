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
using System.Text.RegularExpressions;
using System.Windows.Forms;

using Greenshot.Drawing.Fields;
using Greenshot.Helpers;

namespace Greenshot.Drawing {
	/// <summary>
	/// Description of BitmapContainer.
	/// </summary>
	[Serializable()] 
	public class HtmlContainer : DrawableContainer {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(HtmlContainer));
		private const string SCRIPT_PATTERN = "<script(.*?)</script>";
		private string html = null;
		private Bitmap bitmap = null;
		private Size lastSize = new Size(0,0);

		public HtmlContainer(Surface parent, string html) : this(parent) {
			Html = html;
		}

		public HtmlContainer(Surface parent) : base(parent) {
			Width = 100;
			Height = 100;
		}
		
		private void removeScripts() {
            Regex regExRemoveScript = new Regex(SCRIPT_PATTERN);
			html = regExRemoveScript.Replace(html, "");
		}

		public string Html {
			set {
				html = value;
				removeScripts();
				lastSize = new Size(0, 0);
				RedrawIfNeeded();
			}
			get { return html; }
		}

		/**
		 * Destructor
		 */
		~HtmlContainer() {
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
			if (html == null) {
				if (bitmap != null) {
					bitmap.Dispose();
				}
				bitmap = null;
				return;
			}
			if (bitmap == null || lastSize.Height != Height || lastSize.Width != Width) {
				bitmap = WebsiteImageGenerator.GetImageFromHTML(html);
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
