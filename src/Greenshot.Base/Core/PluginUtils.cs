/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Windows.Forms;
using Dapplo.Windows.Icons;
using Dapplo.Ini;
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
        private static ICoreConfiguration _coreConfig;
        private static ICoreConfiguration CoreConfig
        {
            get
            {
                if (_coreConfig != null) return _coreConfig;
                try
                {
                    _coreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();
                    if (_coreConfig != null)
                    {
                        _coreConfig.PropertyChanged += OnIconSizeChanged;
                    }
                }
                catch
                {
                    // Configuration might not be registered yet (e.g. unit tests)
                }
                return _coreConfig;
            }
        }
        private static readonly IDictionary<string, Image> ExeIconCache = new Dictionary<string, Image>();
        private const string PathKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\";

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

            // For index 0, check if this path points to a Windows App (e.g. AppExecutionAlias or in WindowsApps)
            if (index == 0)
            {
                try
                {
                    var appLogo = WindowsAppHelper.GetAppLogo(path);
                    if (appLogo is Bitmap bitmap)
                    {
                        return bitmap;
                    }
                    if (appLogo != null)
                    {
                        return new Bitmap(appLogo);
                    }
                }
                catch (Exception exApp)
                {
                    Log.Debug("Error checking Windows App logo for: " + path, exApp);
                }
            }

            try
            {
                var iconSize = CoreConfig?.IconSize;
                bool isLarge = iconSize.HasValue && (iconSize.Value.Width >= 32 || iconSize.Value.Height >= 32);
                var appIcon = IconHelper.ExtractAssociatedIcon<Bitmap>(path, index, isLarge);
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

            // Fallback: use the Windows shell-associated icon (handles exes with no embedded icon, e.g. Windows curl.exe)
            try
            {
                using var shellIcon = Icon.ExtractAssociatedIcon(path);
                if (shellIcon != null)
                {
                    Log.DebugFormat("Loaded shell icon for {0}", path);
                    return shellIcon.ToBitmap();
                }
            }
            catch (Exception exShell)
            {
                Log.Warn("error retrieving shell icon: ", exShell);
            }

            // Fallback: check WindowsAppHelper if not already checked or if index > 0
            try
            {
                var appLogo = WindowsAppHelper.GetAppLogo(path);
                if (appLogo is Bitmap bitmap)
                {
                    return bitmap;
                }
                if (appLogo != null)
                {
                    return new Bitmap(appLogo);
                }
            }
            catch
            {
                // Ignore
            }

            return null;
        }

        /// <summary>
        /// Gets the localized text for a plugin quicklink context menu item (e.g. "Configure {0}").
        /// </summary>
        /// <param name="pluginDisplayName">Display name of the plugin.</param>
        /// <returns>Formatted quicklink text.</returns>
        public static string GetQuicklinkText(string pluginDisplayName)
        {
            string format = Language.GetString("contextmenu_configure_plugin");
            if (string.IsNullOrEmpty(format) || format.StartsWith("string ###"))
            {
                format = "Configure {0}";
            }
            return string.Format(format, pluginDisplayName);
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

            item.VisibleChanged += (s, e) => UpdatePluginSeparatorsVisibility(contextMenu);
            UpdatePluginSeparatorsVisibility(contextMenu);
        }

        /// <summary>
        /// Update visibility of the plugin separator based on whether any plugin items are currently visible.
        /// If no plugin items are visible, hides the top separator so two separators don't appear next to each other.
        /// </summary>
        public static void UpdatePluginSeparatorsVisibility(ContextMenuStrip contextMenu = null)
        {
            contextMenu ??= SimpleServiceProvider.Current.GetInstance<ContextMenuStrip>(isOptional: true);
            if (contextMenu == null) return;

            ToolStripSeparator afterSeparator = null;
            bool hasVisiblePluginItems = false;
            bool trackingPlugins = false;

            for (int i = 0; i < contextMenu.Items.Count; i++)
            {
                var current = contextMenu.Items[i];
                if ("PluginsAreAddedAfter".Equals(current.Tag))
                {
                    afterSeparator = current as ToolStripSeparator;
                    trackingPlugins = true;
                    continue;
                }

                if ("PluginsAreAddedBefore".Equals(current.Tag))
                {
                    break;
                }

                if (trackingPlugins && current.Available)
                {
                    hasVisiblePluginItems = true;
                    break;
                }
            }

            if (afterSeparator != null)
            {
                afterSeparator.Available = hasVisiblePluginItems;
            }
        }
    }
}