//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using Dapplo.Ini;
using Dapplo.Log;
using Dapplo.Windows.Native;
using Greenshot.Core.Configuration;
using Greenshot.Core.Gfx;

#endregion

namespace Greenshot.Core
{
	/// <summary>
	///     Helper code for icons
	/// </summary>
	public static class IconHelper
	{
		private static readonly LogSource Log = new LogSource();

		private static readonly IDictionary<string, Bitmap> ExeIconCache = new Dictionary<string, Bitmap>();
		private static readonly IUiConfiguration UiConfiguration = IniConfig.Current.GetSubSection<IUiConfiguration>();

		static IconHelper()
		{
			UiConfiguration.PropertyChanged += OnIconSizeChanged;
			OnIconSizeChanged(UiConfiguration, new PropertyChangedEventArgs(nameof(IUiConfiguration.IconSize)));
		}

		public static bool UseLargeIcons { get; set; }

		/// <summary>
		///     D:\Projects\greenshot-develop\Greenshot.Core\IconHelper.cs
		///     Get icon for executable, from the cache
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
		///     Get icon for executable
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
				using (Icon appIcon = ImageHelper.ExtractAssociatedIcon(path, index, UseLargeIcons))
				{
					if (appIcon != null)
					{
						return appIcon.ToBitmap();
					}
				}
				using (Icon appIcon = Shell32.GetFileIcon(path, UseLargeIcons ? Shell32.IconSize.Large : Shell32.IconSize.Small, false))
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

		/// <summary>
		///     Clear icon cache
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void OnIconSizeChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof(IUiConfiguration.IconSize))
			{
				return;
			}
			UseLargeIcons = (UiConfiguration.IconSize.Width >= 32) || (UiConfiguration.IconSize.Height >= 32);
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
	}
}