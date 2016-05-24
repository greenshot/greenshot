/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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

using Greenshot.Interop.Office;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;

namespace GreenshotOfficePlugin {
	/// <summary>
	/// Description of EmailDestination.
	/// </summary>
	public class WordDestination : AbstractDestination {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(WordDestination));
		private const int ICON_APPLICATION = 0;
		private const int ICON_DOCUMENT = 1;
		private static readonly string exePath = null;
		private readonly string documentCaption = null;

		static WordDestination() {
			exePath = PluginUtils.GetExePath("WINWORD.EXE");
			if (exePath != null && !File.Exists(exePath)) {
				exePath = null;
			}
		}
		
		public WordDestination() {
			
		}

		public WordDestination(string wordCaption) {
			documentCaption = wordCaption;
		}

		public override string Designation {
			get {
				return "Word";
			}
		}

		public override string Description {
			get {
				if (documentCaption == null) {
					return "Microsoft Word";
				} else {
					return documentCaption;
				}
			}
		}

		public override int Priority {
			get {
				return 4;
			}
		}
		
		public override bool isDynamic {
			get {
				return true;
			}
		}

		public override bool isActive {
			get {
				return base.isActive && exePath != null;
			}
		}

		public override Image DisplayIcon {
			get {
				if (!string.IsNullOrEmpty(documentCaption)) {
					return PluginUtils.GetCachedExeIcon(exePath, ICON_DOCUMENT);
				}
				return PluginUtils.GetCachedExeIcon(exePath, ICON_APPLICATION);
			}
		}

		public override IEnumerable<IDestination> DynamicDestinations() {
			foreach (string wordCaption in WordExporter.GetWordDocuments()) {
				yield return new WordDestination(wordCaption);
			}
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			ExportInformation exportInformation = new ExportInformation(Designation, Description);
			string tmpFile = captureDetails.Filename;
			if (tmpFile == null || surface.Modified || !Regex.IsMatch(tmpFile, @".*(\.png|\.gif|\.jpg|\.jpeg|\.tiff|\.bmp)$")) {
				tmpFile = ImageOutput.SaveNamedTmpFile(surface, captureDetails, new SurfaceOutputSettings().PreventGreenshotFormat());
			}
			if (documentCaption != null) {
				try {
					WordExporter.InsertIntoExistingDocument(documentCaption, tmpFile);
					exportInformation.ExportMade = true;
				} catch (Exception) {
					try {
						WordExporter.InsertIntoExistingDocument(documentCaption, tmpFile);
						exportInformation.ExportMade = true;
					} catch (Exception ex) {
						LOG.Error(ex);
						// TODO: Change to general logic in ProcessExport
						surface.SendMessageEvent(this, SurfaceMessageTyp.Error, Language.GetFormattedString("destination_exportfailed", Description));
					}
				}
			} else {
				if (!manuallyInitiated) {
					List<string> documents = WordExporter.GetWordDocuments();
					if (documents != null && documents.Count > 0) {
						List<IDestination> destinations = new List<IDestination>();
						destinations.Add(new WordDestination());
						foreach (string document in documents) {
							destinations.Add(new WordDestination(document));
						}
						// Return the ExportInformation from the picker without processing, as this indirectly comes from us self
						return ShowPickerMenu(false, surface, captureDetails, destinations);
					}
				}
				try {
					WordExporter.InsertIntoNewDocument(tmpFile, null, null);
					exportInformation.ExportMade = true;
				} catch(Exception) {
					// Retry once, just in case
					try {
						WordExporter.InsertIntoNewDocument(tmpFile, null, null);
						exportInformation.ExportMade = true;
					} catch (Exception ex) {
						LOG.Error(ex);
						// TODO: Change to general logic in ProcessExport
						surface.SendMessageEvent(this, SurfaceMessageTyp.Error, Language.GetFormattedString("destination_exportfailed", Description));
					}
				}
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}
