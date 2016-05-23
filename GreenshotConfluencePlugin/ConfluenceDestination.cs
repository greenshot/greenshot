﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;

namespace GreenshotConfluencePlugin {
	/// <summary>
	/// Description of ConfluenceDestination.
	/// </summary>
	public class ConfluenceDestination : AbstractDestination {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ConfluenceDestination));
		private static readonly ConfluenceConfiguration config = IniConfig.GetIniSection<ConfluenceConfiguration>();
		private static readonly CoreConfiguration coreConfig = IniConfig.GetIniSection<CoreConfiguration>();
		private static readonly Image confluenceIcon = null;
		private readonly Confluence.Page page;
		public static bool IsInitialized {
			get;
			private set;
		}
		static ConfluenceDestination() {
			IsInitialized = false;
			try {
				Uri confluenceIconUri = new Uri("/GreenshotConfluencePlugin;component/Images/Confluence.ico", UriKind.Relative);
				using (Stream iconStream = Application.GetResourceStream(confluenceIconUri).Stream) {
					using (Image tmpImage = Image.FromStream(iconStream)) {
						confluenceIcon = ImageHelper.Clone(tmpImage);
					}
				}
				IsInitialized = true;
			} catch (Exception ex) {
				LOG.ErrorFormat("Problem in the confluence static initializer: {0}", ex.Message);
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
				return base.isActive && !string.IsNullOrEmpty(config.Url);
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
			foreach(Confluence.Page currentPage in currentPages) {
				yield return new ConfluenceDestination(currentPage);
			}
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			ExportInformation exportInformation = new ExportInformation(this.Designation, this.Description);
			// force password check to take place before the pages load
			if (!ConfluencePlugin.ConfluenceConnector.isLoggedIn) {
				return exportInformation;
			}

			Page selectedPage = page;
			bool openPage = (page == null) && config.OpenPageAfterUpload;
			string filename = FilenameHelper.GetFilenameWithoutExtensionFromPattern(coreConfig.OutputFileFilenamePattern, captureDetails);
			if (selectedPage == null) {
				ConfluenceUpload confluenceUpload = new ConfluenceUpload(filename);
				Nullable<bool> dialogResult = confluenceUpload.ShowDialog();
				if (dialogResult.HasValue && dialogResult.Value) {
					selectedPage = confluenceUpload.SelectedPage;
					if (confluenceUpload.isOpenPageSelected) {
						openPage = false;
					}
					filename = confluenceUpload.Filename;
				}
			}
			string extension = "." + config.UploadFormat;
			if (!filename.ToLower().EndsWith(extension)) {
				filename = filename + extension;
			}
			if (selectedPage != null) {
				string errorMessage;
				bool uploaded = upload(surface, selectedPage, filename, out errorMessage);
				if (uploaded) {
					if (openPage) {
						try {
							Process.Start(selectedPage.Url);
						} catch { }
					}
					exportInformation.ExportMade = true;
					exportInformation.Uri = selectedPage.Url;
				} else {
					exportInformation.ErrorMessage = errorMessage;
				}
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}

		private bool upload(ISurface surfaceToUpload, Page page, string filename, out string errorMessage) {
			SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(config.UploadFormat, config.UploadJpegQuality, config.UploadReduceColors);
			errorMessage = null;
			try {
				new PleaseWaitForm().ShowAndWait(Description, Language.GetString("confluence", LangKey.communication_wait),
					delegate() {
						ConfluencePlugin.ConfluenceConnector.addAttachment(page.id, "image/" + config.UploadFormat.ToString().ToLower(), null, filename, new SurfaceContainer(surfaceToUpload, outputSettings, filename));
					}
				);
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
