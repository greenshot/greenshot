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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

using Greenshot.Configuration;
using GreenshotPlugin.Core;
using Greenshot.Plugin;
using Greenshot.Helpers;
using Greenshot.Interop.Office;
using Greenshot.IniFile;

namespace Greenshot.Destinations {
	/// <summary>
	/// Description of EmailDestination.
	/// </summary>
	public class WordDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(WordDestination));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		private static string exePath = null;
		private static Image applicationIcon = null;
		private static Image documentIcon = null;
		private string documentCaption = null;

		static WordDestination() {
			exePath = GetExePath("WINWORD.EXE");
			if (exePath != null && File.Exists(exePath)) {
				applicationIcon = GetExeIcon(exePath, 0);
				documentIcon = GetExeIcon(exePath, 1);
			} else {
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
				return exePath != null;
			}
		}

		public override Image DisplayIcon {
			get {
				if (!string.IsNullOrEmpty(documentCaption)) {
					return documentIcon;
				}
				return applicationIcon;
			}
		}

		public override IEnumerable<IDestination> DynamicDestinations() {
			foreach (string wordCaption in WordExporter.GetWordDocuments()) {
				yield return new WordDestination(wordCaption);
			}
		}

		public override bool ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			string tmpFile = captureDetails.Filename;
			if (tmpFile == null || surface.Modified) {
				using (Image image = surface.GetImageForExport()) {
					tmpFile = ImageOutput.SaveNamedTmpFile(image, captureDetails, conf.OutputFileFormat, conf.OutputFileJpegQuality, conf.OutputFileReduceColors);
				}
			}
			if (documentCaption != null) {
				WordExporter.InsertIntoExistingDocument(documentCaption, tmpFile);
			} else {
				if (!manuallyInitiated) {
					List<string> documents = WordExporter.GetWordDocuments();
					if (documents != null && documents.Count > 0) {
						List<IDestination> destinations = new List<IDestination>();
						destinations.Add(new WordDestination());
						foreach (string document in documents) {
							destinations.Add(new WordDestination(document));
						}
						ContextMenuStrip menu = PickerDestination.CreatePickerMenu(false, surface, captureDetails, destinations);
						PickerDestination.ShowMenuAtCursor(menu);
						return false;
					}
				}
				WordExporter.InsertIntoNewDocument(tmpFile);
			}
			surface.SendMessageEvent(this, SurfaceMessageTyp.Info, Language.GetFormattedString(LangKey.exported_to, Description));
			surface.Modified = false;

			return true;
		}
	}
}
