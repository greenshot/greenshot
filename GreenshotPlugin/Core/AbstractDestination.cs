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
using Greenshot.Plugin;
using GreenshotPlugin.UnmanagedHelpers;
using log4net;
using System.Threading.Tasks;
using Dapplo.Config.Ini;

namespace GreenshotPlugin.Core
{
	/// <summary>
	/// Description of AbstractDestination.
	/// </summary>
	public abstract class AbstractDestination : IDestination
	{
		private static readonly ILog LOG = LogManager.GetLogger(typeof(AbstractDestination));
		private static CoreConfiguration configuration = IniConfig.Get("Greenshot","greenshot").Get<CoreConfiguration>();

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
			string usedDestination = null;
			var canExitSemaphore = new SemaphoreSlim(0, 1);
			bool exit = false;

			do
			{
				// Create menu, and show it
				using (var menu = new ContextMenuStrip())
				{
					menu.ImageScalingSize = configuration.IconSize;
					menu.Closing += (source, eventArgs) => {
						LOG.DebugFormat("Menu closing event with reason {0}", eventArgs.CloseReason);
						switch (eventArgs.CloseReason)
						{
							case ToolStripDropDownCloseReason.Keyboard:
								exit = true;
								canExitSemaphore.Release();
								break;
							case ToolStripDropDownCloseReason.AppFocusChange:
								eventArgs.Cancel = true;
								break;
						}
					};
					menu.MouseEnter += (source, eventArgs) => {
						// in case the menu has been unfocused, focus again so that dropdown menus will still open on mouseenter
						if (!menu.ContainsFocus)
						{
							menu.Focus();
						}
					};
					foreach (var destination in destinations)
					{
						// Fix foreach loop variable for the delegate
						var item = destination.GetMenuItem(addDynamics, menu, async (sender, e) => {
							var toolStripMenuItem = sender as ToolStripMenuItem;
							if (toolStripMenuItem == null)
							{
								return;
							}
							var clickedDestination = (IDestination)toolStripMenuItem.Tag;

							// try to export
							try {
								if (clickedDestination == null) {
									return;
								}
								LOG.DebugFormat("Destination {0} was clicked", clickedDestination.Description);
								exportInformation = await clickedDestination.ExportCaptureAsync(true, surface, captureDetails);
								if (exportInformation != null && exportInformation.ExportMade) {
									LOG.InfoFormat("Export to {0} success, closing menu", exportInformation.DestinationDescription);
									usedDestination = clickedDestination.Designation;
									exit = true;
									menu.Close();
								} else {
									LOG.Info("Export cancelled or failed, showing menu again");
								}
							} finally {
								canExitSemaphore.Release();
							}
						}
						);
						if (item != null)
						{
							menu.Items.Add(item);
						}
					}
					//  Add Close item
					menu.Items.Add(new ToolStripSeparator());
					var closeItem = new ToolStripMenuItem
					{
						Text = Language.GetString("editor_close"),
						Image = GreenshotResources.getImage("Close.Image")
					};
					closeItem.Click += (source, eventArgs) => {
						LOG.Debug("Close clicked");
						exit = true;
						canExitSemaphore.Release();
					};
					menu.Items.Add(closeItem);

					menu.ShowAtCursor();
					await menu.WaitForClosedAsync(token);
					// Await the can exit semaphore
					await canExitSemaphore.WaitAsync(token);
				}
			} while (!exit);

			// Dispose as the close is clicked, but only if we didn't export to the editor.
			if (!"Editor".Equals(usedDestination))
			{
				surface.Dispose();
				surface = null;
			}

			return exportInformation;
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
			basisMenuItem.Click -= destinationClickHandler;
			basisMenuItem.Click += destinationClickHandler;

			if (isDynamic && addDynamics)
			{
				basisMenuItem.DropDownOpening += (source, eventArgs) => {
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
								foreach (var subDestination in subDestinations)
								{
									destinationMenuItem = new ToolStripMenuItem(subDestination.Description);
									destinationMenuItem.Tag = subDestination;
									destinationMenuItem.Image = subDestination.DisplayIcon;
									destinationMenuItem.Click += destinationClickHandler;
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
