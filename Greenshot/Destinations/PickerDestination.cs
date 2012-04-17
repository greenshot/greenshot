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
using System.IO;
using System.Windows.Forms;

using Greenshot.Configuration;
using GreenshotPlugin.Core;
using Greenshot.Plugin;
using Greenshot.Helpers;
using Greenshot.Forms;
using Greenshot.IniFile;

namespace Greenshot.Destinations {
	/// <summary>
	/// Description of PickerDestination.
	/// </summary>
	public class PickerDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PickerDestination));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		public const string DESIGNATION = "Picker";

		public override string Designation {
			get {
				return DESIGNATION;
			}
		}

		public override string Description {
			get {
				return Language.GetString(LangKey.settings_destination_picker);
			}
		}

		public override int Priority {
			get {
				return 1;
			}
		}
		
		public override bool isActive {
			get {
				return true;
			}
		}

		public static ContextMenuStrip CreatePickerMenu(bool addDynamics, ISurface surface, ICaptureDetails captureDetails, IEnumerable<IDestination> destinations) {
			ContextMenuStrip menu = new ContextMenuStrip();
			menu.Closing += delegate(object source, ToolStripDropDownClosingEventArgs eventArgs) {
				LOG.DebugFormat("Close reason: {0}", eventArgs.CloseReason);
				if (eventArgs.CloseReason != ToolStripDropDownCloseReason.ItemClicked && eventArgs.CloseReason != ToolStripDropDownCloseReason.CloseCalled) {
					eventArgs.Cancel = true;
				}
			};
			foreach (IDestination destination in destinations) {
				// Fix foreach loop variable for the delegate
				ToolStripMenuItem item = destination.GetMenuItem(addDynamics,
					delegate(object sender, EventArgs e) {
						ToolStripMenuItem toolStripMenuItem = sender as ToolStripMenuItem;
						if (toolStripMenuItem == null) {
							return;
						}
						IDestination clickedDestination = (IDestination)toolStripMenuItem.Tag;
						if (clickedDestination == null) {
							return;
						}
						// Make sure the menu is closed
						menu.Close();
						bool result = clickedDestination.ExportCapture(true, surface, captureDetails);
						// TODO: Find some better way to detect that we exported to the editor
						if (!EditorDestination.DESIGNATION.Equals(clickedDestination.Designation) || result == false) {
							LOG.DebugFormat("Disposing as Destination was {0} and result {1}", clickedDestination.Description, result);
							// Cleanup surface
							surface.Dispose();
						}
					}
				);
				if (item != null) {
					menu.Items.Add(item);
				}
			}
			// Close
			menu.Items.Add(new ToolStripSeparator());
			ToolStripMenuItem closeItem = new ToolStripMenuItem(Language.GetString(LangKey.editor_close));
			closeItem.Image = ((System.Drawing.Image)(new System.ComponentModel.ComponentResourceManager(typeof(ImageEditorForm)).GetObject("closeToolStripMenuItem.Image")));
			closeItem.Click += delegate {
				// This menu entry is the close itself, we can dispose the surface
				menu.Close();
				// Dispose as the close is clicked
				surface.Dispose();
			};
			menu.Items.Add(closeItem);

			return menu;
		}

		public static void ShowMenuAtCursor(ContextMenuStrip menu) {
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
		}

		public override bool ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			List<IDestination> destinations = new List<IDestination>();
			foreach(IDestination destination in DestinationHelper.GetAllDestinations()) {
				if ("Picker".Equals(destination.Designation)) {
					continue;
				}
				if (!destination.isActive) {
					continue;
				}
				destinations.Add(destination);
			}

			ContextMenuStrip menu = CreatePickerMenu(true, surface, captureDetails, destinations);
			ShowMenuAtCursor(menu);
			return true;
		}
	}
}
