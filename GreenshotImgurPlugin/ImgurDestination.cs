/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;

using Greenshot.Plugin;
using GreenshotPlugin.Core;
using Greenshot.IniFile;

namespace GreenshotImgurPlugin  {
	/// <summary>
	/// Description of ImgurDestination.
	/// </summary>
	public class ImgurDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ImgurDestination));
		private static ImgurConfiguration config = IniConfig.GetIniSection<ImgurConfiguration>();
		private ILanguage lang = Language.GetInstance();
		private ImgurPlugin plugin = null;
		private ILanguage coreLanguage;

		public ImgurDestination(ImgurPlugin plugin) {
			this.plugin = plugin;
			this.coreLanguage = plugin.CoreLanguage;
		}
		
		public override string Designation {
			get {
				return "Imgur";
			}
		}

		public override string Description {
			get {
				return lang.GetString(LangKey.upload_menu_item);
			}
		}

		public override Image DisplayIcon {
			get {
				ComponentResourceManager resources = new ComponentResourceManager(typeof(ImgurPlugin));
				return (Image)resources.GetObject("Imgur");
			}
		}

		public override bool ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			using (Image image = surface.GetImageForExport()) {
				bool uploaded = plugin.Upload(captureDetails, image);
				if (uploaded) {
					surface.SendMessageEvent(this, SurfaceMessageTyp.Info, coreLanguage.GetFormattedString("exported_to", Description));
					surface.Modified = false;
				}
				return uploaded;
			}
		}
	}
}
