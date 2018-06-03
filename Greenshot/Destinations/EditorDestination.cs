﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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
using System;
using System.Collections.Generic;
using System.Drawing;
using Greenshot.Configuration;
using GreenshotPlugin.Core;
using Greenshot.Plugin;
using Greenshot.IniFile;
using log4net;

namespace Greenshot.Destinations {
	/// <summary>
	/// Description of EditorDestination.
	/// </summary>
	public class EditorDestination : AbstractDestination {
		private static readonly ILog LOG = LogManager.GetLogger(typeof(EditorDestination));
		private static readonly EditorConfiguration editorConfiguration = IniConfig.GetIniSection<EditorConfiguration>();
		public const string DESIGNATION = "Editor";
		private readonly IImageEditor editor;
		private static readonly Image greenshotIcon = GreenshotResources.getGreenshotIcon().ToBitmap();

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
				}
				return Language.GetString(LangKey.settings_destination_editor) + " - " + editor.CaptureDetails.Title;
			}
		}

		public override int Priority {
			get {
				return 1;
			}
		}

		public override bool IsDynamic {
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
			foreach (IImageEditor someEditor in ImageEditorForm.Editors) {
				yield return new EditorDestination(someEditor);
			}
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
            base.SetDefaults(surface);
            ExportInformation exportInformation = new ExportInformation(Designation, Description);
			// Make sure we collect the garbage before opening the screenshot
			GC.Collect();
			GC.WaitForPendingFinalizers();

			bool modified = surface.Modified;
			if (editor == null) {
				if (editorConfiguration.ReuseEditor) {
					foreach(IImageEditor openedEditor in ImageEditorForm.Editors) {
						if (!openedEditor.Surface.Modified) {
							openedEditor.Surface = surface;
							exportInformation.ExportMade = true;
							break;
						}
					}
				}
				if (!exportInformation.ExportMade) {
					try {
						ImageEditorForm editorForm = new ImageEditorForm(surface, !surface.Modified); // Output made??

						if (!string.IsNullOrEmpty(captureDetails.Filename)) {
							editorForm.SetImagePath(captureDetails.Filename);
						}
						editorForm.Show();
						editorForm.Activate();
						LOG.Debug("Finished opening Editor");
						exportInformation.ExportMade = true;
					} catch (Exception e) {
						LOG.Error(e);
						exportInformation.ErrorMessage = e.Message;
					}
				}
			} else {
				try {
					using (Image image = surface.GetImageForExport()) {
						editor.Surface.AddImageContainer(image, 10, 10);
					}
					exportInformation.ExportMade = true;
				} catch (Exception e) {
					LOG.Error(e);
					exportInformation.ErrorMessage = e.Message;
				}
			}
			ProcessExport(exportInformation, surface);
			// Workaround for the modified flag when using the editor.
			surface.Modified = modified;
			return exportInformation;
		}
	}
}
