#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Dapplo.Log;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Native;
using GreenshotPlugin.Gfx;
using GreenshotPlugin.IniFile;
using GreenshotPlugin.Interfaces;
using Dapplo.Log;

#endregion

namespace GreenshotPlugin.Core
{
	/// <summary>
	///     Description of AbstractDestination.
	/// </summary>
	public abstract class AbstractDestination : IDestination
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();

		public virtual int CompareTo(object obj)
		{
			var other = obj as IDestination;
			if (other == null)
			{
				return 1;
			}
			if (Priority == other.Priority)
			{
				return string.Compare(Description, other.Description, StringComparison.Ordinal);
			}
			return Priority - other.Priority;
		}

		public abstract string Designation { get; }

		public abstract string Description { get; }

		public virtual int Priority => 10;

		public virtual Image DisplayIcon { get; set; }

		public virtual Image GetDisplayIcon(double dpi)
		{
			return DisplayIcon;
		}

		public virtual bool HasDisplayIcon => true;


		public virtual Keys EditorShortcutKeys => Keys.None;

		public virtual IEnumerable<IDestination> DynamicDestinations()
		{
			yield break;
		}

		public void Dispose()
		{
			Dispose(true);
		}

		public virtual bool IsDynamic => false;

		public virtual bool UseDynamicsOnly => false;

		public virtual bool IsLinkable => false;

		public virtual bool IsActive
		{
			get
			{
				if (CoreConfig.ExcludeDestinations != null && CoreConfig.ExcludeDestinations.Contains(Designation))
				{
					return false;
				}
				return true;
			}
		}

		public abstract ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails);

		/// <summary>
		///     Return a menu item
		/// </summary>
		/// <param name="menu"></param>
		/// <param name="addDynamics"></param>
		/// <param name="destinationClickHandler"></param>
		/// <returns>ToolStripMenuItem</returns>
		public virtual ToolStripMenuItem GetMenuItem(bool addDynamics, ContextMenuStrip menu, EventHandler destinationClickHandler, BitmapScaleHandler<IDestination> scaleHandler)
		{
			var basisMenuItem = new ToolStripMenuItem(Description)
			{
				Tag = this,
				Text = Description
			};

			scaleHandler.AddTarget(basisMenuItem, this);

			AddTagEvents(basisMenuItem, menu, Description);
			basisMenuItem.Click -= destinationClickHandler;
			basisMenuItem.Click += destinationClickHandler;

			if (IsDynamic && addDynamics)
			{
				basisMenuItem.DropDownOpening += delegate
				{
					if (basisMenuItem.DropDownItems.Count == 0)
					{
						var subDestinations = new List<IDestination>();
						// Fixing Bug #3536968 by catching the COMException (every exception) and not displaying the "subDestinations"
						try
						{
							subDestinations.AddRange(DynamicDestinations());
						}
						catch (Exception ex)
						{
							Log.Error().WriteLine("Skipping {0}, due to the following error: {1}", Description, ex.Message);
						}
						if (subDestinations.Count > 0)
						{
							if (UseDynamicsOnly && subDestinations.Count == 1)
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
									var destinationMenuItem = new ToolStripMenuItem(subDestination.Description)
									{
										Tag = subDestination,
									};
									scaleHandler.AddTarget(destinationMenuItem, subDestination);

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

		protected virtual void Dispose(bool disposing)
		{
			//if (disposing) {}
		}

		/// <summary>
		///     A small helper method to perform some default destination actions, like inform the surface of the export
		/// </summary>
		/// <param name="exportInformation"></param>
		/// <param name="surface"></param>
		public void ProcessExport(ExportInformation exportInformation, ISurface surface)
		{
			if (exportInformation != null && exportInformation.ExportMade)
			{
				if (!string.IsNullOrEmpty(exportInformation.Uri))
				{
					surface.UploadUrl = exportInformation.Uri;
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
			else if (!string.IsNullOrEmpty(exportInformation?.ErrorMessage))
			{
				surface.SendMessageEvent(this, SurfaceMessageTyp.Error,
					Language.GetFormattedString("exported_to_error", exportInformation.DestinationDescription) + " " + exportInformation.ErrorMessage);
			}
		}

		public override string ToString()
		{
			return Description;
		}

		/// <summary>
		///     Helper method to add events which set the tag, this way we can see why there might be a close.
		/// </summary>
		/// <param name="menuItem">Item to add the events to</param>
		/// <param name="menu">Menu to set the tag</param>
		/// <param name="tagValue">Value for the tag</param>
		private void AddTagEvents(ToolStripMenuItem menuItem, ContextMenuStrip menu, string tagValue)
		{
			if (menuItem != null && menu != null)
			{
				menuItem.MouseDown += (sender, args) =>
				{
					Log.Debug().WriteLine("Setting tag to '{0}'", tagValue);
					menu.Tag = tagValue;
				};
				menuItem.MouseUp += (sender, args) =>
				{
					Log.Debug().WriteLine("Deleting tag");
					menu.Tag = null;
				};
			}
		}

		/// <summary>
		///     This method will create and show the destination picker menu
		/// </summary>
		/// <param name="addDynamics">Boolean if the dynamic values also need to be added</param>
		/// <param name="surface">The surface which can be exported</param>
		/// <param name="captureDetails">Details for the surface</param>
		/// <param name="destinations">The list of destinations to show</param>
		/// <returns></returns>
		public ExportInformation ShowPickerMenu(bool addDynamics, ISurface surface, ICaptureDetails captureDetails, IEnumerable<IDestination> destinations)
		{
			var menu = new ContextMenuStrip
			{
				Tag = null,
				TopLevel = true
			};
			var dpiHandler = menu.HandleDpiChanges();
			var bitmapScaleHandler = BitmapScaleHandler.Create<IDestination>(
				dpiHandler,
				(destination, dpi) => (Bitmap)destination.GetDisplayIcon(dpi),
				(bitmap, d) => (Bitmap)bitmap.ScaleIconForDisplaying(d));

			dpiHandler.OnDpiChanged.Subscribe(dpi =>
			{
				var width = DpiHandler.ScaleWithDpi(CoreConfig.IconSize.Width, dpi);
				var size = new Size(width, width);
				menu.ImageScalingSize = size;
			});

			// Generate an empty ExportInformation object, for when nothing was selected.
			var exportInformation = new ExportInformation(Designation, Language.GetString("settings_destination_picker"));
			menu.Closing += (source, eventArgs) =>
			{
				Log.Debug().WriteLine("Close reason: {0}", eventArgs.CloseReason);
				switch (eventArgs.CloseReason)
				{
					case ToolStripDropDownCloseReason.AppFocusChange:
						if (menu.Tag == null)
						{
							// Do not allow the close if the tag is not set, this means the user clicked somewhere else.
							eventArgs.Cancel = true;
						}
						else
						{
							Log.Debug().WriteLine("Letting the menu 'close' as the tag is set to '{0}'", menu.Tag);
						}
						break;
					case ToolStripDropDownCloseReason.ItemClicked:
					case ToolStripDropDownCloseReason.CloseCalled:
						// The ContextMenuStrip can be "closed" for these reasons.
						break;
					case ToolStripDropDownCloseReason.Keyboard:
						// Dispose as the close is clicked
						if (!captureDetails.HasDestination("Editor"))
						{
							surface.Dispose();
							surface = null;
						}
						break;
					default:
						eventArgs.Cancel = true;
						break;
				}
			};
			menu.MouseEnter += (sender, args) =>
			{
				// in case the menu has been unfocused, focus again so that dropdown menus will still open on mouseenter
				if (!menu.ContainsFocus)
				{
					menu.Focus();
				}
			};
			foreach (var destination in destinations)
			{
				// Fix foreach loop variable for the delegate
				var item = destination.GetMenuItem(addDynamics, menu,
					delegate(object sender, EventArgs e)
					{
						var toolStripMenuItem = sender as ToolStripMenuItem;
						var clickedDestination = (IDestination) toolStripMenuItem?.Tag;
						if (clickedDestination == null)
						{
							return;
						}
						menu.Tag = clickedDestination.Designation;
						// Export
						exportInformation = clickedDestination.ExportCapture(true, surface, captureDetails);
						if (exportInformation != null && exportInformation.ExportMade)
						{
							Log.Info().WriteLine("Export to {0} success, closing menu", exportInformation.DestinationDescription);
							// close menu if the destination wasn't the editor
							menu.Close();

							// Cleanup surface, only if there is no editor in the destinations and we didn't export to the editor
							if (!captureDetails.HasDestination("Editor") && !"Editor".Equals(clickedDestination.Designation))
							{
								surface.Dispose();
								surface = null;
							}
						}
						else
						{
							Log.Info().WriteLine("Export cancelled or failed, showing menu again");

							// Make sure a click besides the menu don't close it.
							menu.Tag = null;

							// This prevents the problem that the context menu shows in the task-bar
							ShowMenuAtCursor(menu);
						}
					} , bitmapScaleHandler
				);
				if (item != null)
				{
					menu.Items.Add(item);
				}
			}
			// Close
			menu.Items.Add(new ToolStripSeparator());
			var closeItem = new ToolStripMenuItem(Language.GetString("editor_close"))
			{
				Image = GreenshotResources.getImage("Close.Image")
			};
			closeItem.Click += delegate
			{
				// This menu entry is the close itself, we can dispose the surface
				menu.Close();
				if (!captureDetails.HasDestination("Editor"))
				{
					surface.Dispose();
					surface = null;
				}
			};
			menu.Items.Add(closeItem);

			ShowMenuAtCursor(menu);
			return exportInformation;
		}

		/// <summary>
		///     This method will show the supplied context menu at the mouse cursor, also makes sure it has focus and it's not
		///     visible in the taskbar.
		/// </summary>
		/// <param name="menu"></param>
		private static void ShowMenuAtCursor(ContextMenuStrip menu)
		{
			// find a suitable location
			var location = Cursor.Position;
			var menuRectangle = new Rectangle(location, menu.Size);

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
			while (true)
			{
				if (menu.Visible)
				{
					Application.DoEvents();
					Thread.Sleep(100);
				}
				else
				{
					menu.Dispose();
					break;
				}
			}
		}
	}
}