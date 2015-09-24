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
using GreenshotPlugin.Extensions;
using log4net;
using System.Threading.Tasks;
using Dapplo.Config.Ini;
using GreenshotPlugin.Configuration;

namespace GreenshotPlugin.Core
{
	/// <summary>
	/// Description of AbstractDestination.
	/// </summary>
	public abstract class AbstractDestination : IDestination
	{
		private static readonly ILog LOG = LogManager.GetLogger(typeof(AbstractDestination));
		private static readonly ICoreConfiguration configuration = IniConfig.Current.Get<ICoreConfiguration>();

		public virtual int CompareTo(object obj)
		{
			var other = obj as IDestination;
			if (other == null)
			{
				return 1;
			}
			if (Priority == other.Priority)
			{
				if (Description != null)
				{
					return Description.CompareTo(other.Description);
				} else
				{
					return -1;
				}
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

		/// <summary>
		/// Async add the dropdown items
		/// </summary>
		/// <param name="baseItem"></param>
		/// <param name="destinationClickHandler"></param>
		/// <param name="token"></param>
		/// <returns>Task</returns>
		public virtual async Task AddDynamicDestinations(ToolStripMenuItem baseItem, EventHandler destinationClickHandler, CancellationToken token = default(CancellationToken)) {
			await Task.Factory.StartNew(() => {
				foreach (var destination in DynamicDestinations()) {
					var menuItem = CreateFor(destination);
					menuItem.Click += destinationClickHandler;
					baseItem.DropDownItems.Add(menuItem);
				}
			}, token, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
		}

		public virtual bool IsDynamic
		{
			get
			{
				return false;
			}
		}

		public virtual bool IsActive
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
		/// The async ExportCaptureAsync should be overriden, here you will need to make your export
		/// </summary>
		/// <param name="manuallyInitiated"></param>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <param name="token"></param>
		/// <returns>Task with ExportInformation</returns>
		public abstract Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails, CancellationToken token = default(CancellationToken));

		/// <summary>
		/// A small helper method to perform some default destination actions, like inform the surface of the export
		/// </summary>
		/// <param name="exportInformation"></param>
		/// <param name="surface"></param>
		public void ProcessExport(ExportInformation exportInformation, ISurface surface)
		{
			if (exportInformation != null && exportInformation.ExportMade)
			{
				if (exportInformation.ExportedToUri != null)
				{
					surface.UploadUri = exportInformation.ExportedToUri;
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
			var exportInformation = new ExportInformation {
				DestinationDesignation = Designation,
				DestinationDescription = Description
			};
			string usedDestination = null;
			var canExitSemaphore = new SemaphoreSlim(0, 1);
			bool exit = false;

			do
			{
				var taskCompletionSource = new TaskCompletionSource<object>();
				// Create menu, and show it
				using (var menu = new ContextMenuStrip())
				{
					// Make sure that when the menu is closed, the taskCompletionSource is cancelled
					menu.Closed += (sender, args) => taskCompletionSource.TrySetResult(null);

					menu.ImageScalingSize = configuration.IconSize;

					// In some cases the closing event needs to be ignored.
					menu.Closing += (source, eventArgs) => {
						LOG.DebugFormat("Menu closing event with reason {0}", eventArgs.CloseReason);
						switch (eventArgs.CloseReason)
						{
							case ToolStripDropDownCloseReason.Keyboard:
								// User used Alt+F4 or ESC, this should be like calling close.
								exit = true;
								canExitSemaphore.Release();
								eventArgs.Cancel = true;
								taskCompletionSource.TrySetResult(null);
								break;
							case ToolStripDropDownCloseReason.AppFocusChange:
								// User clicked outside of the menu, just ignore this
								eventArgs.Cancel = true;
								break;
						}
					};

					// Make sure that the menu has focus if the mouse is over it.
					// This makes is possible that dropdown menus will still open on mouseenter
					menu.MouseEnter += (source, eventArgs) => {
						if (!menu.ContainsFocus)
						{
							menu.Focus();
						}
					};

					// Create entries for the destinations
					foreach (var destination in destinations)
					{
						// Fix foreach loop variable for the delegate
						var item = destination.CreateMenuItem(addDynamics, async (sender, e) => {
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
								menu.Close();

								LOG.DebugFormat("Destination {0} was clicked", clickedDestination.Description);
								exportInformation = await clickedDestination.ExportCaptureAsync(true, surface, captureDetails);
								if (exportInformation != null && exportInformation.ExportMade) {
									LOG.InfoFormat("Export to {0} success, closing menu", exportInformation.DestinationDescription);
									usedDestination = clickedDestination.Designation;
									exit = true;
								} else {
									// Export didn't work, but as we didn't set exit=true the menu will be shown again.
									LOG.Info("Export cancelled or failed, showing menu again");
								}
							} finally {
								canExitSemaphore.Release();
							}
						});

						if (item != null)
						{
							menu.Items.Add(item);
						}
					}
					// Add separator, enabled = false so we can't click it.
					menu.Items.Add(new ToolStripSeparator {
						Enabled = false
					});
					//  Add Close item
					var closeItem = new ToolStripMenuItem
					{
						Text = Language.GetString("editor_close"),
						Image = GreenshotResources.GetImage("Close.Image")
					};
					closeItem.Click += (source, eventArgs) => {
						LOG.Debug("Close clicked");
						exit = true;
						canExitSemaphore.Release();
					};
					menu.Items.Add(closeItem);

					// Show the context menu we just created at the cursor
					menu.ShowAtCursor();

					// wait on the closing event
					await taskCompletionSource.Task;
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
		/// Return a menu item, can override
		/// </summary>
		/// <param name="destinationClickHandler"></param>
		/// <returns>ToolStripMenuItem</returns>
		public virtual ToolStripMenuItem CreateMenuItem(bool addDynamics, EventHandler destinationClickHandler)
		{
			var basisMenuItem = CreateFor(this);
			basisMenuItem.Click -= destinationClickHandler;
			basisMenuItem.Click += destinationClickHandler;

			if (IsDynamic && addDynamics)
			{
				Task.Factory.StartNew(async () => {
					await AddDynamicDestinations(basisMenuItem, destinationClickHandler).ContinueWith((t) => {
						if (t.Exception != null) {
							LOG.ErrorFormat("Skipping {0}, due to the following error: {1}", Description, t.Exception.Message);
						}
						basisMenuItem.Invalidate();
					});
				}, default(CancellationToken), TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
			}
			return basisMenuItem;
		}

		/// <summary>
		/// Helper to consistantly create Menu items the same way
		/// </summary>
		/// <param name="destination"></param>
		/// <returns>ToolStripMenuItem</returns>
		protected ToolStripMenuItem CreateFor(IDestination destination) {
			return new ToolStripMenuItem {
				Text = destination.Description,
				Tag = destination,
				Image = destination.DisplayIcon
			};
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
				}

				disposedValue = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
		}
		#endregion
	}
}
