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
using GreenshotPlugin.UnmanagedHelpers;

namespace Greenshot.Destinations {
	/// <summary>
	/// The PickerDestination shows a context menu with all possible destinations, so the user can "pick" one
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
		
		/// <summary>
		/// This method will create and show the destination picker menu
		/// </summary>
		/// <param name="addDynamics">Boolean if the dynamic values also need to be added</param>
		/// <param name="surface">The surface which can be exported</param>
		/// <param name="captureDetails">Details for the surface</param>
		/// <param name="destinations">The list of destinations to show</param>
		/// <returns></returns>
		public static void ShowPickerMenu(bool addDynamics, ISurface surface, ICaptureDetails captureDetails, IEnumerable<IDestination> destinations) {
			ContextMenuStrip menu = new ContextMenuStrip();
			menu.Closing += delegate(object source, ToolStripDropDownClosingEventArgs eventArgs) {
				LOG.DebugFormat("Close reason: {0}", eventArgs.CloseReason);
				switch (eventArgs.CloseReason) {
					case ToolStripDropDownCloseReason.ItemClicked:
					case ToolStripDropDownCloseReason.CloseCalled:
						break;
					case ToolStripDropDownCloseReason.Keyboard:
						// Dispose as the close is clicked
						surface.Dispose();
						surface = null;
						break;
					default:
						eventArgs.Cancel = true;
						break;
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
						bool isEditor = EditorDestination.DESIGNATION.Equals(clickedDestination.Designation);
						// Make sure the menu is invisible, don't close it
						menu.Hide();

						// Export
						ExportInformation exportInformation = clickedDestination.ExportCapture(true, surface, captureDetails);
						if (exportInformation != null && exportInformation.ExportMade) {
							LOG.InfoFormat("Export to {0} success, closing menu", exportInformation.DestinationDescription);
							// close menu if the destination wasn't the editor
							menu.Close();

							// Cleanup surface, only if the destination wasn't the editor
							if (!isEditor) {
								surface.Dispose();
								surface = null;
							}
						} else {
							LOG.Info("Export cancelled or failed, showing menu again");
							// This prevents the problem that the context menu shows in the task-bar
							ShowMenuAtCursor(menu);
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
				surface = null;
			};
			menu.Items.Add(closeItem);

			ShowMenuAtCursor(menu);
		}

		/// <summary>
		/// This method will show the supplied context menu at the mouse cursor, also makes sure it has focus and it's not visible in the taskbar.
		/// </summary>
		/// <param name="menu"></param>
		private static void ShowMenuAtCursor(ContextMenuStrip menu) {
			// find a suitable location
			Point location = Cursor.Position;
			Rectangle menuRectangle = new Rectangle(location, menu.Size);

			menuRectangle.Intersect(WindowCapture.GetScreenBounds());
			if (menuRectangle.Height < menu.Height) {
				location.Offset(-40, -(menuRectangle.Height - menu.Height));
			} else {
				location.Offset(-40, -10);
			}
			// This prevents the problem that the context menu shows in the task-bar
			User32.SetForegroundWindow(MainForm.instance.notifyIcon.ContextMenuStrip.Handle);
			menu.Show(location);
			menu.Focus();

			// Wait for the menu to close, so we can dispose it.
			while (true) {
				if (menu.Visible) {
					Application.DoEvents();
					System.Threading.Thread.Sleep(100);
				} else {
					menu.Dispose();
					break;
				}
			}
		}

		/// <summary>
		/// Export the capture with the destination picker
		/// </summary>
		/// <param name="manuallyInitiated">Did the user select this destination?</param>
		/// <param name="surface">Surface to export</param>
		/// <param name="captureDetails">Details of the capture</param>
		/// <returns>true if export was made</returns>
		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			ExportInformation exportInformation = new ExportInformation(this.Designation, this.Description);
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

			ShowPickerMenu(true, surface, captureDetails, destinations);
			exportInformation.ExportMade = true;
			// No Processing! :)
			return exportInformation;
		}
	}
}
