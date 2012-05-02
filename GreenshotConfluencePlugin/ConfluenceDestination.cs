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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;

using Confluence;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using Greenshot.IniFile;

namespace GreenshotConfluencePlugin {
	/// <summary>
	/// Description of JiraDestination.
	/// </summary>
	public class ConfluenceDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ConfluenceDestination));
		private static ConfluenceConfiguration config = IniConfig.GetIniSection<ConfluenceConfiguration>();
		private static Image confluenceIcon = null;
		private Confluence.Page page;
		
		static ConfluenceDestination() {
			Uri confluenceIconUri = new Uri("/GreenshotConfluencePlugin;component/Images/Confluence.ico", UriKind.Relative);
			using (Stream iconStream = Application.GetResourceStream(confluenceIconUri).Stream) {
				using (Image tmpImage = Image.FromStream(iconStream)) {
					confluenceIcon = ImageHelper.Clone(tmpImage);
				}
			}
		}
		
		public ConfluenceDestination() {
			
		}
		public ConfluenceDestination(Confluence.Page page) {
			this.page = page;
		}
		
		public override string Designation {
			get {
				return "Confluence";
			}
		}

		public override string Description {
			get {
				if (page == null) {
					return Language.GetString("confluence", LangKey.upload_menu_item);
				} else {
					return Language.GetString("confluence", LangKey.upload_menu_item) + ": \"" + page.Title + "\"";
				}
			}
		}

		public override bool isDynamic {
			get {
				return true;
			}
		}
		
		public override bool isActive {
			get {
				return !string.IsNullOrEmpty(config.Url);
			}
		}

		public override Image DisplayIcon {
			get {
				return confluenceIcon;
			}
		}
		
		public override IEnumerable<IDestination> DynamicDestinations() {
			if (ConfluencePlugin.ConfluenceConnectorNoLogin == null || !ConfluencePlugin.ConfluenceConnectorNoLogin.isLoggedIn) {
				yield break;
			}
			List<Confluence.Page> currentPages = ConfluenceUtils.GetCurrentPages();
			if (currentPages == null || currentPages.Count == 0) {
				yield break;
			}
			List<IDestination> dynamicDestinations = new List<IDestination>();
			foreach(Confluence.Page currentPage in currentPages) {
				yield return new ConfluenceDestination(currentPage);
			}
		}

		public override bool ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			// force password check to take place before the pages load
			if (!ConfluencePlugin.ConfluenceConnector.isLoggedIn) {
				return false;
			}

			Page selectedPage = page;
			bool openPage = (page == null) && config.OpenPageAfterUpload;
			string filename = ConfluencePlugin.Host.GetFilename(config.UploadFormat, captureDetails);
			if (selectedPage == null) {
				ConfluenceUpload confluenceUpload = new ConfluenceUpload(filename);
				Nullable<bool> dialogResult = confluenceUpload.ShowDialog();
				if (dialogResult.HasValue && dialogResult.Value) {
					selectedPage = confluenceUpload.SelectedPage;
					if (confluenceUpload.isOpenPageSelected) {
						openPage = false;
					}
					filename = confluenceUpload.Filename;
				} else {
					return false;
				}
			}
			if (selectedPage != null) {
				using (Image image = surface.GetImageForExport()) {
					string errorMessage;
					bool uploaded = upload(image, selectedPage, filename, out errorMessage);
					if (uploaded) {
						if (openPage) {
							try {
								Process.Start(page.Url);
							} catch { }
						}
						surface.UploadURL = page.Url;
						surface.SendMessageEvent(this, SurfaceMessageTyp.UploadedUrl, Language.GetFormattedString("exported_to", Description));
						surface.Modified = false;
						return true;
					} else {
						surface.SendMessageEvent(this, SurfaceMessageTyp.Error, Language.GetString("confluence", LangKey.upload_failure) + " " + errorMessage);
					}
				}
			}
			
			return false;
		}
		
		private bool upload(Image image, Page page, string filename, out string errorMessage) {
			OutputSettings outputSettings = new OutputSettings(config.UploadFormat, config.UploadJpegQuality, config.UploadReduceColors);
			byte[] buffer;
			using (MemoryStream stream = new MemoryStream()) {
				ConfluencePlugin.Host.SaveToStream(image, stream, outputSettings);
				// COPY buffer to array
				buffer = stream.ToArray();
			}
			errorMessage = null;
			try {
				ConfluencePlugin.ConfluenceConnector.addAttachment(page.id, "image/" + config.UploadFormat.ToString().ToLower(),  null, filename, buffer);
				LOG.Debug("Uploaded to Confluence.");
				if (config.CopyWikiMarkupForImageToClipboard) {
					int retryCount = 2;
					while (retryCount >= 0) {
						try {
							Clipboard.SetText("!" + filename + "!");
							break;
						} catch (Exception ee) {
							if (retryCount == 0) {
								LOG.Error(ee);
							} else {
								Thread.Sleep(100);
							}
						} finally {
							--retryCount;
						}
					}
				}
				return true;
			} catch(Exception e) {
				errorMessage = e.Message;
			}
			return false;
		}
	}
}
