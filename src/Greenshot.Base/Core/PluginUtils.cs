/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Dapplo.Windows.Icons;
using Greenshot.Base.IniFile;
using log4net;
using Microsoft.Win32;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Description of PluginUtils.
    /// </summary>
    public static class PluginUtils
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PluginUtils));
        private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();
        private static readonly IDictionary<string, Image> ExeIconCache = new Dictionary<string, Image>();
        private const string PathKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\";
        
        static PluginUtils()
        {
            CoreConfig.PropertyChanged += OnIconSizeChanged;
        }

        /// <summary>
        /// Clear icon cache
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnIconSizeChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "IconSize") return;
            var cachedImages = new List<Image>();
            lock (ExeIconCache)
            {
                foreach (string key in ExeIconCache.Keys)
                {
                    cachedImages.Add(ExeIconCache[key]);
                }

                ExeIconCache.Clear();
            }

            foreach (Image cachedImage in cachedImages)
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
            using (var key = Registry.LocalMachine.OpenSubKey(PathKey + exeName, false))
            {
                if (key != null)
                {
                    // "" is the default key, which should point to the requested location
                    return (string)key.GetValue(string.Empty);
                }
            }

            foreach (string pathEntry in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(';'))
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
                    Log.WarnFormat("Problem with path entry '{0}'.", pathEntry);
                }
            }

            return null;
        }

        /// <summary>
        /// Get icon from resource files, from the cache.
        /// Examples can be found here: https://diymediahome.org/windows-icons-reference-list-with-details-locations-images/
        /// </summary>
        /// <param name="path">path to the exe or dll</param>
        /// <param name="index">index of the icon</param>
        /// <returns>Bitmap with the icon or null if something happened</returns>
        public static Image GetCachedExeIcon(string path, int index)
        {
            string cacheKey = $"{path}:{index}";
            Image returnValue;
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
        /// <returns>Bitmap with the icon or null if something happened</returns>
        private static Bitmap GetExeIcon(string path, int index)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                var appIcon = IconHelper.ExtractAssociatedIcon<Bitmap>(path, index, CoreConfig.UseLargeIcons);
                if (appIcon != null)
                {
                    Log.DebugFormat("Loaded icon for {0}, with dimensions {1}x{2}", path, appIcon.Width, appIcon.Height);
                    return appIcon;
                }
            }
            catch (Exception exIcon)
            {
                Log.Error("error retrieving icon: ", exIcon);
            }

            return null;
        }

        /// <summary>
        /// Helper method to add a plugin MenuItem to the Greenshot context menu
        /// </summary>
        /// <param name="item">ToolStripMenuItem</param>
        public static void AddToContextMenu(ToolStripMenuItem item)
        {
            // Here we can hang ourselves to the main context menu!
            var contextMenu = SimpleServiceProvider.Current.GetInstance<ContextMenuStrip>();
            bool addedItem = false;

            // Try to find a separator, so we insert ourselves after it 
            for (int i = 0; i < contextMenu.Items.Count; i++)
            {
                if (contextMenu.Items[i].GetType() != typeof(ToolStripSeparator)) continue;
                // Check if we need to add a new separator, which is done if the first found has a Tag with the value "PluginsAreAddedBefore"
                if ("PluginsAreAddedBefore".Equals(contextMenu.Items[i].Tag))
                {
                    var separator = new ToolStripSeparator
                    {
                        Tag = "PluginsAreAddedAfter",
                        Size = new Size(305, 6)
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