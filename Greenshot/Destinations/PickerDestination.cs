/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
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
using System.IO;
using System.Windows.Forms;

using Greenshot.Configuration;
using GreenshotPlugin.Core;
using Greenshot.Plugin;
using Greenshot.Helpers;
using Greenshot.Forms;
using IniFile;

namespace Greenshot.Destinations {
	/// <summary>
	/// Description of PickerDestination.
	/// </summary>
	public class PickerDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PickerDestination));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		public const string DESIGNATION = "Picker";
		private ILanguage lang = Language.GetInstance();

		public override string Designation {
			get {
				return DESIGNATION;
			}
		}

		public override string Description {
			get {
				return lang.GetString(LangKey.settings_destination_picker);
			}
		}

		public override int Priority {
			get {
				return 1;
			}
		}
		
		public override bool isActive {
			get {
				return conf.isExperimentalFeatureEnabled("Picker");
			}
		}

		public override bool ExportCapture(ISurface surface, ICaptureDetails captureDetails) {
			ContextMenuStrip menu = new ContextMenuStrip();
			menu.Closed += delegate(object source, ToolStripDropDownClosedEventArgs eventArgs) {
				// Dispose surface when no item was clicked, else the dispose should be made there!
				if (eventArgs.CloseReason != ToolStripDropDownCloseReason.ItemClicked) {
					LOG.DebugFormat("Disposing as no item was clicked, reason: {0}", eventArgs.CloseReason);
					surface.Dispose();
				}
			};

			foreach(IDestination destination in DestinationHelper.GetAllDestinations()) {
				if ("Picker".Equals(destination.Designation)) {
					continue;
				}
				if (!destination.isActive) {
					continue;
				}
				// Fix foreach loop variable for the delegate
				ToolStripMenuItem item = destination.GetMenuItem(delegate(object sender, EventArgs e) {
					ToolStripMenuItem toolStripMenuItem = sender as ToolStripMenuItem;
					if (toolStripMenuItem == null) {
						return;
					}
					IDestination clickedDestination = (IDestination)toolStripMenuItem.Tag;
					if (clickedDestination == null) {
						return;
					}
					bool result = clickedDestination.ExportCapture(surface, captureDetails);
					// TODO: Find some better way to detect that we exported to the editor
					if (!EditorDestination.DESIGNATION.Equals(clickedDestination.Designation) || result == false) {
						LOG.DebugFormat("Disposing as Destination was {0} and result {1}", clickedDestination.Description, result);
						// Cleanup surface
						surface.Dispose();
					}
					// Make sure the menu is closed
					menu.Close();
				});
				if (item != null) {
					menu.Items.Add(item);
				}
			}
			menu.Items.Add(new ToolStripSeparator());
			ToolStripMenuItem closeItem = new ToolStripMenuItem(lang.GetString(LangKey.editor_close));
			closeItem.Image = ((System.Drawing.Image)(new System.ComponentModel.ComponentResourceManager(typeof(ImageEditorForm)).GetObject("closeToolStripMenuItem.Image")));
			closeItem.Click += delegate {
				// This menu entry is the close itself, we can dispose the surface
				menu.Close();
				surface.Dispose();
			};
			menu.Items.Add(closeItem);
			
			// find a suitable location
			Point location = Cursor.Position;
			Rectangle menuRectangle = new Rectangle(location, menu.Size);
			menuRectangle.Intersect(WindowCapture.GetScreenBounds());
			if (menuRectangle.Height < menu.Height) {
				location.Offset(-40, -(menuRectangle.Height - menu.Height));
			} else {
				location.Offset(-40, -10);
			}
			menu.Show(location);
			menu.Focus();
			return true;
		}
	}
}
