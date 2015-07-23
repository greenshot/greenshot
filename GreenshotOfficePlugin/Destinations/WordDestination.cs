/*
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

using Greenshot.Plugin;
using GreenshotPlugin.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using GreenshotOfficePlugin.OfficeExport;
using System.Threading.Tasks;
using System.Threading;

namespace GreenshotOfficePlugin {
	/// <summary>
	/// Description of EmailDestination.
	/// </summary>
	public class WordDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(WordDestination));
		private const int ICON_APPLICATION = 0;
		private const int ICON_DOCUMENT = 1;
		private static string exePath = null;
		private string documentCaption = null;

		static WordDestination() {
			exePath = PluginUtils.GetExePath("WINWORD.EXE");
			if (exePath != null && !File.Exists(exePath)) {
				exePath = null;
			}
		}
		
		public WordDestination() {
			
		}

		public WordDestination(string wordCaption) {
			this.documentCaption = wordCaption;
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
			return from caption in WordExporter.GetWordDocuments()
				   orderby caption
				   select new WordDestination(caption);
		}

		public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails, CancellationToken token = default(CancellationToken)) {
			ExportInformation exportInformation = new ExportInformation(this.Designation, this.Description);
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

					var wordDestinations = from caption in WordExporter.GetWordDocuments()
									orderby caption
									select new WordDestination(caption);
					bool initialValue = false;
					List<IDestination> destinations = new List<IDestination>();
					foreach (var wordDestination in wordDestinations) {
						if (!initialValue) {
							initialValue = true;
							destinations.Add(new WordDestination());
						}
						destinations.Add(wordDestination);
					}

					if (destinations.Count > 0) {
						// Return the ExportInformation from the picker without processing, as this indirectly comes from us self
						return await ShowPickerMenuAsync(false, surface, captureDetails, destinations, token).ConfigureAwait(false);
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
