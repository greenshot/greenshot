/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using Dapplo.Config.Ini;
using Dapplo.Windows.Native;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Interfaces.Plugin;
using Microsoft.Win32;
using Dapplo.Log;

namespace Greenshot.Addon.Core
{
	/// <summary>
	/// Description of PluginUtils.
	/// </summary>
	public static class PluginUtils
	{
		private static readonly LogSource Log = new LogSource();
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
				Log.Error().WriteLine(exIcon, "error retrieving icon: ");
			}
			return null;
		}

	}
}