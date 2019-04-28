// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Dapplo.Log;
using Dapplo.Windows.Icons;
using Dapplo.Windows.Icons.Enums;
using Greenshot.Addons.Interfaces.Forms;
using Greenshot.Gfx;
using Microsoft.Win32;

namespace Greenshot.Addons.Core
{
	/// <summary>
	///     Description of PluginUtils.
	/// </summary>
	public static class PluginUtils
	{
		private const string PathKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\";
		private static readonly LogSource Log = new LogSource();
	    private static bool _isHooked = false;
		private static readonly Dictionary<string, IBitmapWithNativeSupport> ExeIconCache = new Dictionary<string, IBitmapWithNativeSupport>();
        private static ICoreConfiguration coreConfiguration;

        /// <summary>
        /// Set from DI via AddonsModule
        /// </summary>
        internal static ICoreConfiguration CoreConfiguration {
            get
            {
                return coreConfiguration;
            }

            set
            {
                coreConfiguration = value;
                coreConfiguration.PropertyChanged += OnIconSizeChanged;
            }
        }

		/// <summary>
		///     Clear icon cache
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void OnIconSizeChanged(object sender, PropertyChangedEventArgs e)
		{
		    if (e.PropertyName != "IconSize")
		    {
		        return;
		    }

		    var cachedImages = new List<IBitmapWithNativeSupport>();
		    lock (ExeIconCache)
		    {
		        foreach (var key in ExeIconCache.Keys)
		        {
		            cachedImages.Add(ExeIconCache[key]);
		        }
		        ExeIconCache.Clear();
		    }
		    foreach (var cachedImage in cachedImages)
		    {
		        // TODO: Fix System.ArgumentException on other places due to the disposing of the images.
		        //cachedImage?.Dispose();
		    }
		}

		/// <summary>
		///     Get the path of an executable
		/// </summary>
		/// <param name="exeName">e.g. cmd.exe</param>
		/// <returns>Path to file</returns>
		public static string GetExePath(string exeName)
		{
			using (var key = Registry.LocalMachine.OpenSubKey(PathKey + exeName, false))
			{
				if (key != null)
				{
					// "" is the default key, which should point to the requested location
					return (string) key.GetValue("");
				}
			}
			foreach (var pathEntry in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
			{
				try
				{
					var path = pathEntry.Trim();
					if (!string.IsNullOrEmpty(path) && File.Exists(path = Path.Combine(path, exeName)))
					{
						return Path.GetFullPath(path);
					}
				}
				catch (Exception)
				{
					Log.Warn().WriteLine("Problem with path entry '{0}'.", pathEntry);
				}
			}
			return null;
		}

		/// <summary>
		///     Get icon from resource files, from the cache.
		///     Examples can be found here: https://diymediahome.org/windows-icons-reference-list-with-details-locations-images/
		/// </summary>
		/// <param name="path">path to the exe or dll</param>
		/// <param name="index">index of the icon</param>
		/// <param name="useLargeIcon">true to use the large icon</param>
		/// <returns>Bitmap with the icon or null if something happended. you are responsible for copying this icon</returns>
		public static IBitmapWithNativeSupport GetCachedExeIcon(string path, int index, bool useLargeIcon = true)
		{
			string cacheKey = $"{path}:{index}";
            IBitmapWithNativeSupport returnValue;
			lock (ExeIconCache)
			{
                if (ExeIconCache.TryGetValue(cacheKey, out returnValue))
				{
					return returnValue;
				}
				lock (ExeIconCache)
				{
					if (ExeIconCache.TryGetValue(cacheKey, out returnValue))
					{
						return returnValue;
					}
					returnValue = GetExeIcon(path, index, useLargeIcon);
					if (returnValue != null)
					{
						ExeIconCache.Add(cacheKey, returnValue);
					}
				}
			}
			return returnValue;
		}

		/// <summary>
		///     Get icon for executable
		/// </summary>
		/// <param name="path">path to the exe or dll</param>
		/// <param name="index">index of the icon</param>
		/// <param name="useLargeIcon">true to use the large icon, if available</param>
		/// <returns>Bitmap with the icon or null if something happended</returns>
		private static IBitmapWithNativeSupport GetExeIcon(string path, int index, bool useLargeIcon = true)
		{
			if (!File.Exists(path))
			{
				return null;
			}
			try
			{
			    var appIcon = IconHelper.ExtractAssociatedIcon<Bitmap>(path, index, useLargeIcon);
				if (appIcon != null)
				{
					return BitmapWrapper.FromBitmap(appIcon);
				}
			    return BitmapWrapper.FromBitmap(Shell32.GetFileExtensionIcon<Bitmap>(path, useLargeIcon ? IconSize.Large : IconSize.Small, false));
			}
			catch (Exception exIcon)
			{
				Log.Error().WriteLine(exIcon, "error retrieving icon: ");
			}
			return null;
		}

		/// <summary>
		///     Helper method to add a MenuItem to the File MenuItem of an ImageEditor
		/// </summary>
		/// <param name="imageEditor"></param>
		/// <param name="item"></param>
		public static void AddToFileMenu(IImageEditor imageEditor, ToolStripMenuItem item)
		{
			var toolStripMenuItem = imageEditor.GetFileMenuItem();
			var added = false;
			for (var i = 0; i < toolStripMenuItem.DropDownItems.Count; i++)
			{
			    if (toolStripMenuItem.DropDownItems[i].GetType() != typeof(ToolStripSeparator))
			    {
			        continue;
			    }

			    toolStripMenuItem.DropDownItems.Insert(i, item);
			    added = true;
			    break;
			}
			if (!added)
			{
				toolStripMenuItem.DropDownItems.Add(item);
			}
		}

	}
}