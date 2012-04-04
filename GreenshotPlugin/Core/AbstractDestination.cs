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

namespace GreenshotPlugin.Core {
	/// <summary>
	/// Description of AbstractDestination.
	/// </summary>
	public abstract class AbstractDestination : IDestination	{
		private const string PATH_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\";
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(AbstractDestination));

		public static string GetExePath(string exeName) {
			using (RegistryKey key = Registry.LocalMachine.OpenSubKey(PATH_KEY + exeName, false)) {
				if (key != null) {
					// "" is the default key, which should point to the outlook location
					return (string)key.GetValue("");
				}
			}
			return null;
		}

		/// <summary>
		/// Internaly used to create an icon
		/// </summary>
		/// <param name="path">path to the exe or dll</param>
		/// <param name="index">index of the icon</param>
		/// <returns>Bitmap with the icon or null if something happended</returns>
		public static Bitmap GetExeIcon(string path, int index) {
			if (!File.Exists(path)) {
				return null;
			}
			try {
				using (Icon appIcon = ImageHelper.ExtractAssociatedIcon(path, index, false)) {
					if (appIcon != null) {
						return appIcon.ToBitmap();
					}
				}
			} catch (Exception exIcon) {
				LOG.Error("error retrieving icon: ", exIcon);
			}
			return null;
		}
		
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
						subDestinations.AddRange(DynamicDestinations());
						if (subDestinations.Count > 0) {
							subDestinations.Sort();
							ToolStripMenuItem destinationMenuItem = new ToolStripMenuItem(Description);
							destinationMenuItem.Tag = this;
							destinationMenuItem.Image = DisplayIcon;
							destinationMenuItem.Click += destinationClickHandler;
							basisMenuItem.DropDownItems.Add(destinationMenuItem);
							foreach(IDestination subDestination in subDestinations) {
								destinationMenuItem = new ToolStripMenuItem(subDestination.Description);
								destinationMenuItem.Tag = subDestination;
								destinationMenuItem.Image = subDestination.DisplayIcon;
								destinationMenuItem.Click += destinationClickHandler;
								basisMenuItem.DropDownItems.Add(destinationMenuItem);
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

		public virtual bool isActive {
			get {
				return true;
			}
		}

		public abstract bool ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails);
		
		public override string ToString() {
			return Description;
		}

	}
}
