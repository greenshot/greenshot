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
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.UnmanagedHelpers;
using log4net;
using System.Threading.Tasks;

namespace GreenshotPlugin.Core
{
	/// <summary>
	/// Description of AbstractDestination.
	/// </summary>
	public abstract class AbstractDestination : IDestination
	{
		private static readonly ILog LOG = LogManager.GetLogger(typeof(AbstractDestination));
		private static CoreConfiguration configuration = IniConfig.GetIniSection<CoreConfiguration>();

		public virtual int CompareTo(object obj)
		{
			IDestination other = obj as IDestination;
			if (other == null)
			{
				return 1;
			}
			if (Priority == other.Priority)
			{
				return Description.CompareTo(other.Description);
			}
			return Priority - other.Priority;
		}

		public abstract string Designation
		{
			get;
		}

		public abstract string Description
		{
			get;
		}

		public virtual int Priority
		{
			get
			{
				return 10;
			}
		}

		public virtual Image DisplayIcon
		{
			get
			{
				return null;
			}
		}

		public virtual Keys EditorShortcutKeys
		{
			get
			{
				return Keys.None;
			}
		}

		public virtual IEnumerable<IDestination> DynamicDestinations()
		{
			yield break;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			//if (disposing) {}
		}

		public virtual bool isDynamic
		{
			get
			{
				return false;
			}
		}

		public virtual bool useDynamicsOnly
		{
			get
			{
				return false;
			}
		}

		public virtual bool isLinkable
		{
			get
			{
				return false;
			}
		}

		public virtual bool isActive
		{
			get
			{
				if (configuration.ExcludeDestinations != null && configuration.ExcludeDestinations.Contains(Designation))
				{
					return false;
				}
				return true;
			}
		}

		/// <summary>
		/// Default ExportCapture, which calls the ExportCaptureAsync synchronously
		/// Should be able to delete this when all code has been made async
		/// </summary>
		/// <param name="manuallyInitiated"></param>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <returns>ExportInformation</returns>
		public virtual ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportTask = ExportCaptureAsync(manuallyInitiated, surface, captureDetails);
			return exportTask.GetAwaiter().GetResult();
		}

		/// <summary>
		/// Wrapper around synchronous code, to make it async, should be overridden
		/// </summary>
		/// <param name="manuallyInitiated"></param>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public virtual async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails, CancellationToken token = default(CancellationToken))
		{
			TaskScheduler scheduler = TaskScheduler.FromCurrentSynchronizationContext();
			var task = Task.Factory.StartNew(() => ExportCapture(manuallyInitiated, surface, captureDetails), token, TaskCreationOptions.None, scheduler);
			return await task;
		}

		/// <summary>
		/// A small helper method to perform some default destination actions, like inform the surface of the export
		/// </summary>
		/// <param name="exportInformation"></param>
		/// <param name="surface"></param>
		public void ProcessExport(ExportInformation exportInformation, ISurface surface)
		{
			if (exportInformation != null && exportInformation.ExportMade)
			{
				if (!string.IsNullOrEmpty(exportInformation.Uri))
				{
					surface.UploadURL = exportInformation.Uri;
					surface.SendMessageEvent(this, SurfaceMessageTyp.UploadedUri, Language.GetFormattedString("exported_to", exportInformation.DestinationDescription));
				}
				else if (!string.IsNullOrEmpty(exportInformation.Filepath))
				{
					surface.LastSaveFullPath = exportInformation.Filepath;
					surface.SendMessageEvent(this, SurfaceMessageTyp.FileSaved, Language.GetFormattedString("exported_to", exportInformation.DestinationDescription));
				}
				else
				{
					surface.SendMessageEvent(this, SurfaceMessageTyp.Info, Language.GetFormattedString("exported_to", exportInformation.DestinationDescription));
				}
				surface.Modified = false;
			}
			else if (exportInformation != null && !string.IsNullOrEmpty(exportInformation.ErrorMessage))
			{
				surface.SendMessageEvent(this, SurfaceMessageTyp.Error, Language.GetFormattedString("exported_to_error", exportInformation.DestinationDescription) + " " + exportInformation.ErrorMessage);
			}
		}

		public override string ToString()
		{
			return Description;
		}

		/// <summary>
		/// Helper method to add events which set the tag, this way we can see why there might be a close.
		/// </summary>
		/// <param name="menuItem">Item to add the events to</param>
		/// <param name="menu">Menu to set the tag</param>
		/// <param name="tagValue">Value for the tag</param>
		private void AddTagEvents(ToolStripMenuItem menuItem, ContextMenuStrip menu, string tagValue)
		{
			if (menuItem != null && menu != null)
			{
				menuItem.MouseDown += delegate (object source, MouseEventArgs eventArgs) {
					LOG.DebugFormat("Setting tag to '{0}'", tagValue);
					menu.Tag = tagValue;
				};
				menuItem.MouseUp += delegate (object source, MouseEventArgs eventArgs) {
					LOG.Debug("Deleting tag");
					menu.Tag = null;
				};
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
		public async Task<ExportInformation> ShowPickerMenuAsync(bool addDynamics, ISurface surface, ICaptureDetails captureDetails, IEnumerable<IDestination> destinations, CancellationToken token = default(CancellationToken))
		{
			// Generate an empty ExportInformation object, for when nothing was selected.
			var exportInformation = new ExportInformation(Designation, Language.GetString("settings_destination_picker"));
			bool success = false;
			bool exit = false;
			string usedDestination = null;
			do {
				// Create menu, and show it
				using (var menu = new ContextMenuStrip()) {
					menu.ImageScalingSize = configuration.IconSize;
					menu.Tag = null;
					menu.Closing += (source, eventArgs) => {
						LOG.DebugFormat("Close reason: {0}", eventArgs.CloseReason);
						switch (eventArgs.CloseReason) {
							case ToolStripDropDownCloseReason.AppFocusChange:
								if (menu.Tag == null) {
									// Do not allow the close if the tag is not set, this means the user clicked somewhere else.
									eventArgs.Cancel = true;
								} else {
									LOG.DebugFormat("Letting the menu 'close' as the tag is set to '{0}'", menu.Tag);
								}
								break;
							case ToolStripDropDownCloseReason.ItemClicked:
							case ToolStripDropDownCloseReason.CloseCalled:
								// The ContextMenuStrip can be "closed" for these reasons.
								break;
							case ToolStripDropDownCloseReason.Keyboard:
								exit = true;
								break;
							default:
								eventArgs.Cancel = true;
								break;
						}
					};
					menu.MouseEnter +=(source, eventArgs) => {
						// in case the menu has been unfocused, focus again so that dropdown menus will still open on mouseenter
						if (!menu.ContainsFocus) {
							menu.Focus();
						}
					};
					foreach (var destination in destinations) {
						// Fix foreach loop variable for the delegate
						var item = destination.GetMenuItem(addDynamics, menu, async (sender, e) => {
							var toolStripMenuItem = sender as ToolStripMenuItem;
							if (toolStripMenuItem == null) {
								return;
							}
							var clickedDestination = (IDestination)toolStripMenuItem.Tag;
							if (clickedDestination == null) {
								return;
							}
							menu.Tag = clickedDestination.Designation;
							// Export
							exportInformation = await clickedDestination.ExportCaptureAsync(true, surface, captureDetails);
							if (exportInformation != null && exportInformation.ExportMade) {
								LOG.InfoFormat("Export to {0} success, closing menu", exportInformation.DestinationDescription);
								usedDestination = clickedDestination.Designation;
								success = true;
								menu.Close();
							} else {
								LOG.Info("Export cancelled or failed, showing menu again");
								// Make sure a click besides the menu don't close it.
								menu.Tag = null;
							}
						}
						);
						if (item != null) {
							menu.Items.Add(item);
						}
					}
					//  Add Close item
					menu.Items.Add(new ToolStripSeparator());
					var closeItem = new ToolStripMenuItem(Language.GetString("editor_close"));
					closeItem.Image = GreenshotResources.getImage("Close.Image");
					closeItem.Click += (source, eventArgs) => {
						exit = true;
						menu.Close();
					};
					menu.Items.Add(closeItem);

					await ShowMenuAtCursorAsync(menu, token);
				}
				// Check if it worked, or if exit was clicked
			} while (!exit && !success);

			// Dispose as the close is clicked
			if (!"Editor".Equals(usedDestination)) {
				surface.Dispose();
				surface = null;
			}

			return exportInformation;
		}

		/// <summary>
		/// This method will show the supplied context menu at the mouse cursor, also makes sure it has focus and it's not visible in the taskbar.
		/// </summary>
		/// <param name="menu"></param>
		private static async Task ShowMenuAtCursorAsync(ContextMenuStrip menu, CancellationToken token = default(CancellationToken))
		{
			// find a suitable location
			Point location = Cursor.Position;
			Rectangle menuRectangle = new Rectangle(location, menu.Size);

			menuRectangle.Intersect(WindowCapture.GetScreenBounds());
			if (menuRectangle.Height < menu.Height)
			{
				location.Offset(-40, -(menuRectangle.Height - menu.Height));
			}
			else
			{
				location.Offset(-40, -10);
			}

			// This prevents the problem that the context menu shows in the task-bar
			User32.SetForegroundWindow(PluginUtils.Host.NotifyIcon.ContextMenuStrip.Handle);
			menu.Show(location);
			menu.Focus();

			// Wait for the menu to close, so we can dispose it.
			while (!token.IsCancellationRequested && menu.Visible)
			{
				await Task.Delay(400);
			}
		}

		/// <summary>
		/// Return a menu item
		/// </summary>
		/// <param name="destinationClickHandler"></param>
		/// <returns>ToolStripMenuItem</returns>
		public virtual ToolStripMenuItem GetMenuItem(bool addDynamics, ContextMenuStrip menu, EventHandler destinationClickHandler)
		{
			var basisMenuItem = new ToolStripMenuItem
			{
				Image = DisplayIcon,
				Tag = this,
				Text = Description,
			};
			AddTagEvents(basisMenuItem, menu, Description);
			basisMenuItem.Click -= destinationClickHandler;
			basisMenuItem.Click += destinationClickHandler;

			if (isDynamic && addDynamics)
			{
				basisMenuItem.DropDownOpening += delegate (object source, EventArgs eventArgs) {
					if (basisMenuItem.DropDownItems.Count == 0)
					{
						List<IDestination> subDestinations = new List<IDestination>();
						// Fixing Bug #3536968 by catching the COMException (every exception) and not displaying the "subDestinations"
						try
						{
							subDestinations.AddRange(DynamicDestinations());
						}
						catch (Exception ex)
						{
							LOG.ErrorFormat("Skipping {0}, due to the following error: {1}", Description, ex.Message);
						}
						if (subDestinations.Count > 0)
						{
							ToolStripMenuItem destinationMenuItem = null;

							if (useDynamicsOnly && subDestinations.Count == 1)
							{
								basisMenuItem.Tag = subDestinations[0];
								basisMenuItem.Text = subDestinations[0].Description;
								basisMenuItem.Click -= destinationClickHandler;
								basisMenuItem.Click += destinationClickHandler;
							}
							else
							{
								foreach (IDestination subDestination in subDestinations)
								{
									destinationMenuItem = new ToolStripMenuItem(subDestination.Description);
									destinationMenuItem.Tag = subDestination;
									destinationMenuItem.Image = subDestination.DisplayIcon;
									destinationMenuItem.Click += destinationClickHandler;
									AddTagEvents(destinationMenuItem, menu, subDestination.Description);
									basisMenuItem.DropDownItems.Add(destinationMenuItem);
								}
							}
						}
					}
				};
			}

			return basisMenuItem;
		}
	}
}
