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
using System.Reflection;

using Microsoft.Win32;

using Greenshot.Plugin;
using Greenshot.IniFile;

namespace GreenshotPlugin.Core {
	/// <summary>
	/// Description of AbstractDestination.
	/// </summary>
	public abstract class AbstractDestination : IDestination {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(AbstractDestination));
		private static CoreConfiguration configuration = IniConfig.GetIniSection<CoreConfiguration>();
		
		public virtual int CompareTo(object obj) {
			IDestination other = obj as IDestination;
			if (other == null) {
				return 1;
			}
			if (Priority == other.Priority) {
				return Description.CompareTo(other.Description);
			}
			return Priority - other.Priority;
		}

		public abstract string Designation {
			get;
		}

		public abstract string Description {
			get;
		}

		public virtual int Priority {
			get {
				return 10;
			}
		}
		
		public virtual Image DisplayIcon {
			get {
				return null;
			}
		}

		public virtual Keys EditorShortcutKeys {
			get {
				return Keys.None;
			}
		}

		/// <summary>
		/// Return a menu item
		/// </summary>
		/// <param name="destinationClickHandler"></param>
		/// <returns>ToolStripMenuItem</returns>
		public virtual ToolStripMenuItem GetMenuItem(bool addDynamics, EventHandler destinationClickHandler) {
			ToolStripMenuItem basisMenuItem;
			basisMenuItem = new ToolStripMenuItem(Description);
			basisMenuItem.Image = DisplayIcon;
			basisMenuItem.Tag = this;
			basisMenuItem.Text = Description;
			if (isDynamic && addDynamics) {
				basisMenuItem.DropDownOpening += delegate (object source, EventArgs eventArgs) {
					if (basisMenuItem.DropDownItems.Count == 0) {
						List<IDestination> subDestinations = new List<IDestination>();
						// Fixing Bug #3536968 by catching the COMException (every exception) and not displaying the "subDestinations"
						try {
							subDestinations.AddRange(DynamicDestinations());
						} catch(Exception ex) {
							LOG.ErrorFormat("Skipping {0}, due to the following error: {1}", Description, ex.Message);
						}
						if (subDestinations.Count > 0) {
							subDestinations.Sort();

							ToolStripMenuItem destinationMenuItem = null;
							if (!useDynamicsOnly) {
								destinationMenuItem = new ToolStripMenuItem(Description);
								destinationMenuItem.Tag = this;
								destinationMenuItem.Image = DisplayIcon;
								destinationMenuItem.Click += destinationClickHandler;
								basisMenuItem.DropDownItems.Add(destinationMenuItem);
							}
							if (useDynamicsOnly && subDestinations.Count == 1) {
								basisMenuItem.Tag = subDestinations[0];
								basisMenuItem.Text = subDestinations[0].Description;
								basisMenuItem.Click -= destinationClickHandler;
								basisMenuItem.Click += destinationClickHandler;
							} else {
								foreach (IDestination subDestination in subDestinations) {
									destinationMenuItem = new ToolStripMenuItem(subDestination.Description);
									destinationMenuItem.Tag = subDestination;
									destinationMenuItem.Image = subDestination.DisplayIcon;
									destinationMenuItem.Click += destinationClickHandler;
									basisMenuItem.DropDownItems.Add(destinationMenuItem);
								}
							}
						} else {
							// Setting base "click" only if there are no sub-destinations

							// Make sure any previous handler is removed, otherwise it would be added multiple times!
							basisMenuItem.Click -= destinationClickHandler;
							basisMenuItem.Click += destinationClickHandler;
						}
					}
				};
			} else {
				basisMenuItem.Click += destinationClickHandler;
			}

			return basisMenuItem;
		}
		
		public virtual IEnumerable<IDestination> DynamicDestinations() {
			yield break;
		}
		
		public virtual void Dispose() {
		}

		public virtual bool isDynamic {
			get {
				return false;
			}
		}

		public virtual bool useDynamicsOnly {
			get {
				return false;
			}
		}

		public virtual bool isLinkable {
			get {
				return false;
			}
		}

		public virtual bool isActive {
			get {
				if (configuration.ExcludeDestinations != null && configuration.ExcludeDestinations.Contains(Designation)) {
					return false;
				}
				return true;
			}
		}

		public abstract ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails);

		/// <summary>
		/// A small helper method to perform some default destination actions, like inform the surface of the export
		/// </summary>
		/// <param name="exportInformation"></param>
		/// <param name="surface"></param>
		public void ProcessExport(ExportInformation exportInformation, ISurface surface) {
			if (exportInformation != null && exportInformation.ExportMade) {
				if (!string.IsNullOrEmpty(exportInformation.Uri)) {
					surface.UploadURL = exportInformation.Uri;
					surface.SendMessageEvent(this, SurfaceMessageTyp.UploadedUri, Language.GetFormattedString("exported_to", exportInformation.DestinationDescription));
				} else if (!string.IsNullOrEmpty(exportInformation.Filepath)) {
					surface.LastSaveFullPath = exportInformation.Filepath;
					surface.SendMessageEvent(this, SurfaceMessageTyp.FileSaved, Language.GetFormattedString("exported_to", exportInformation.DestinationDescription));
				} else {
					surface.SendMessageEvent(this, SurfaceMessageTyp.Info, Language.GetFormattedString("exported_to", exportInformation.DestinationDescription));
				}
				surface.Modified = false;
			} else if (exportInformation != null && !string.IsNullOrEmpty(exportInformation.ErrorMessage)) {
				surface.SendMessageEvent(this, SurfaceMessageTyp.Error, Language.GetFormattedString("exported_to_error", exportInformation.DestinationDescription) + " " + exportInformation.ErrorMessage);
			}
		}

		public override string ToString() {
			return Description;
		}

	}
}
