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

using Dapplo.Config.Ini;
using GreenshotPlugin.Configuration;
using Dapplo.Windows.Native;

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GreenshotPlugin.Interfaces.Forms;
using GreenshotPlugin.Interfaces.Plugin;

namespace GreenshotPlugin.Core
{
	/// <summary>
	/// Description of PluginUtils.
	/// </summary>
	public static class PluginUtils
	{
		private static readonly Serilog.ILogger LOG = Serilog.Log.Logger.ForContext(typeof(PluginUtils));
		private static readonly ICoreConfiguration Conf = IniConfig.Current.Get<ICoreConfiguration>();
		private static readonly IDictionary<string, Bitmap> ExeIconCache = new Dictionary<string, Bitmap>();

		static PluginUtils()
		{
			Conf.PropertyChanged += OnIconSizeChanged;
		}

		/// <summary>
		/// Simple global property to get the Greenshot host
		/// </summary>
		public static IGreenshotHost Host
		{
			get;
			set;
		}

		/// <summary>
		/// Clear icon cache
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void OnIconSizeChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != "IconSize")
			{
				return;
			}
			var cachedImages = new List<Image>();
			lock (ExeIconCache)
			{
				cachedImages.AddRange(ExeIconCache.Keys.Select(key => ExeIconCache[key]));
				ExeIconCache.Clear();
			}
			foreach (var cachedImage in cachedImages)
			{
				cachedImage?.Dispose();
			}
		}

		/// <summary>
		/// Get the path of an executable
		/// </summary>
		/// <param name="exeName">e.g. cmd.exe</param>
		/// <returns>Path to file</returns>
		public static string GetExePath(string exeName)
		{
			using (var key = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{exeName}", false))
			{
				if (key != null)
				{
					// "" is the default key, which should point to the requested location
					return (string) key.GetValue("");
				}
			}
			foreach (string pathEntry in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
			{
				try
				{
					string path = pathEntry.Trim();
					if (!String.IsNullOrEmpty(path) && File.Exists(path = Path.Combine(path, exeName)))
					{
						return Path.GetFullPath(path);
					}
				}
				catch (Exception)
				{
					LOG.Warning("Problem with path entry '{0}'.", pathEntry);
				}
			}
			return null;
		}

		/// <summary>
		/// Get icon for executable, from the cache
		/// </summary>
		/// <param name="path">path to the exe or dll</param>
		/// <param name="index">index of the icon</param>
		/// <returns>Bitmap with the icon or null if something happended</returns>
		public static Bitmap GetCachedExeIcon(string path, int index)
		{
			string cacheKey = $"{path}:{index}";
			Bitmap returnValue;
			lock (ExeIconCache)
			{
				if (!ExeIconCache.TryGetValue(cacheKey, out returnValue))
				{
					returnValue = GetExeIcon(path, index);
					if (returnValue != null)
					{
						ExeIconCache.Add(cacheKey, returnValue);
					}
				}
			}
			return returnValue;
		}

		/// <summary>
		/// Get icon for executable
		/// </summary>
		/// <param name="path">path to the exe or dll</param>
		/// <param name="index">index of the icon</param>
		/// <returns>Bitmap with the icon or null if something happended</returns>
		private static Bitmap GetExeIcon(string path, int index)
		{
			if (!File.Exists(path))
			{
				return null;
			}
			try
			{
				using (Icon appIcon = ImageHelper.ExtractAssociatedIcon(path, index, CoreConfigurationChecker.UseLargeIcons(Conf.IconSize)))
				{
					if (appIcon != null)
					{
						return appIcon.ToBitmap();
					}
				}
				using (Icon appIcon = Shell32.GetFileIcon(path, CoreConfigurationChecker.UseLargeIcons(Conf.IconSize) ? Shell32.IconSize.Large : Shell32.IconSize.Small, false))
				{
					if (appIcon != null)
					{
						return appIcon.ToBitmap();
					}
				}
			}
			catch (Exception exIcon)
			{
				LOG.Error("error retrieving icon: ", exIcon);
			}
			return null;
		}

		/// <summary>
		/// Helper method to add a MenuItem to the File MenuItem of an ImageEditor
		/// </summary>
		/// <param name="imageEditor"></param>
		/// <param name="image">Image to display in the menu</param>
		/// <param name="text">Text to display in the menu</param>
		/// <param name="tag">The TAG value</param>
		/// <param name="shortcutKeys">Keys which can be used as shortcut</param>
		/// <param name="handler">The onclick handler</param>
		public static void AddToFileMenu(IImageEditor imageEditor, Image image, string text, object tag, Keys? shortcutKeys, EventHandler handler)
		{
			var item = new ToolStripMenuItem
			{
				Image = image, Text = text, Tag = tag
			};
			if (shortcutKeys.HasValue)
			{
				item.ShortcutKeys = shortcutKeys.Value;
			}
			item.Click += handler;
			AddToFileMenu(imageEditor, item);
		}

		/// <summary>
		/// Helper method to add a MenuItem to the File MenuItem of an ImageEditor
		/// </summary>
		/// <param name="imageEditor"></param>
		/// <param name="item"></param>
		public static void AddToFileMenu(IImageEditor imageEditor, ToolStripMenuItem item)
		{
			ToolStripMenuItem toolStripMenuItem = imageEditor.GetFileMenuItem();
			bool added = false;
			for (int i = 0; i < toolStripMenuItem.DropDownItems.Count; i++)
			{
				if (toolStripMenuItem.DropDownItems[i].GetType() == typeof (ToolStripSeparator))
				{
					toolStripMenuItem.DropDownItems.Insert(i, item);
					added = true;
					break;
				}
			}
			if (!added)
			{
				toolStripMenuItem.DropDownItems.Add(item);
			}
		}

		/// <summary>
		/// Helper method to add a MenuItem to the Plugin MenuItem of an ImageEditor
		/// </summary>
		/// <param name="imageEditor"></param>
		/// <param name="item"></param>
		public static void AddToPluginMenu(IImageEditor imageEditor, ToolStripMenuItem item)
		{
			ToolStripMenuItem toolStripMenuItem = imageEditor.GetPluginMenuItem();
			bool added = false;
			for (int i = 0; i < toolStripMenuItem.DropDownItems.Count; i++)
			{
				if (toolStripMenuItem.DropDownItems[i].GetType() == typeof (ToolStripSeparator))
				{
					toolStripMenuItem.DropDownItems.Insert(i, item);
					added = true;
					break;
				}
			}
			if (!added)
			{
				toolStripMenuItem.DropDownItems.Add(item);
			}
		}

		/// <summary>
		/// Helper method to add a plugin MenuItem to the Greenshot context menu
		/// </summary>
		/// <param name="host"></param>
		/// <param name="item"></param>
		public static void AddToContextMenu(IGreenshotHost host, ToolStripMenuItem item)
		{
			// Here we can hang ourselves to the main context menu!
			var contextMenu = host.MainMenu;
			bool addedItem = false;

			lock (contextMenu)
			{
				// Try to find a separator, so we insert ourselves after it 
				for (int i = 0; i < contextMenu.Items.Count; i++)
				{
					if (contextMenu.Items[i].GetType() != typeof (ToolStripSeparator))
					{
						continue;
					}
					// Check if we need to add a new separator, which is done if the first found has a Tag with the value "PluginsAreAddedBefore"
					if ("PluginsAreAddedBefore".Equals(contextMenu.Items[i].Tag))
					{
						var separator = new ToolStripSeparator
						{
							Tag = "PluginsAreAddedAfter", Size = new Size(305, 6)
						};
						contextMenu.Items.Insert(i, separator);
					}
					else if (!"PluginsAreAddedAfter".Equals(contextMenu.Items[i].Tag))
					{
						continue;
					}
					contextMenu.Items.Insert(i + 1, item);
					addedItem = true;
					break;
				}
				// If we didn't insert the item, we just add it...
				if (!addedItem)
				{
					contextMenu.Items.Add(item);
				}
			}
		}
	}
}