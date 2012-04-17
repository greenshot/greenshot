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
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;

using Greenshot.Configuration;
using GreenshotPlugin.Core;
using Greenshot.Plugin;
using Greenshot.Helpers;
using Greenshot.IniFile;

namespace Greenshot.Destinations {
	/// <summary>
	/// Description of EditorDestination.
	/// </summary>
	public class EditorDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(EditorDestination));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		public const string DESIGNATION = "Editor";
		private IImageEditor editor = null;
		private static Image greenshotIcon = GreenshotPlugin.Core.GreenshotResources.getGreenshotIcon().ToBitmap();

		public EditorDestination() {
		}
		
		public EditorDestination(IImageEditor editor) {
			this.editor = editor;
		}

		public override string Designation {
			get {
				return DESIGNATION;
			}
		}

		public override string Description {
			get {
				if (editor == null) {
					return Language.GetString(LangKey.settings_destination_editor);
				} else {
					return Language.GetString(LangKey.settings_destination_editor) + " - " + editor.CaptureDetails.Title;
				}
			}
		}

		public override int Priority {
			get {
				return 1;
			}
		}

		public override bool isDynamic {
			get {
				return true;
			}
		}

		public override Image DisplayIcon {
			get {
				return greenshotIcon;
			}
		}

		public override IEnumerable<IDestination> DynamicDestinations() {
			foreach (IImageEditor editor in ImageEditorForm.Editors) {
				yield return new EditorDestination(editor);
			}
		}

		public override bool ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			if (editor == null) {
				// Make sure we collect the garbage before opening the screenshot
				GC.Collect();
				GC.WaitForPendingFinalizers();
				
				try {
					ImageEditorForm editorForm = new ImageEditorForm(surface, !surface.Modified); // Output made??
	
					if (!string.IsNullOrEmpty(captureDetails.Filename)) {
						editorForm.SetImagePath(captureDetails.Filename);
					}
					editorForm.Show();
					editorForm.Activate();
					LOG.Debug("Finished opening Editor");
					return true;
				} catch (Exception e) {
					LOG.Error(e);
				}
			} else {
				using (Bitmap image = (Bitmap)surface.GetImageForExport()) {
					editor.Surface.AddBitmapContainer(image, 10, 10);
				}
				surface.SendMessageEvent(this, SurfaceMessageTyp.Info, Language.GetFormattedString(LangKey.exported_to, Description));
			}
			return false;
		}
	}
}
