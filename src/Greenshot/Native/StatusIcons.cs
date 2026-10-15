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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Greenshot.Base.Interfaces;
using log4net;

namespace Greenshot.Native
{
    /// <summary>
    /// The error and warning icons of Windows as png files, a toast only shows image files: the big image of a toast with an error or a
    /// warning, like the icon of a balloon of the tray icon. Not in Greenshot Light, which has no toasts.
    /// </summary>
    internal static class StatusIcons
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(StatusIcons));

        private const uint SiidWarning = 78;
        private const uint SiidError = 80;
        private const uint ShgsiIconLocation = 0;
        private const int Size = 96;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct StockIconInfo
        {
            public uint cbSize;
            public IntPtr hIcon;
            public int iSysImageIndex;
            public int iIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szPath;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHGetStockIconInfo(uint siid, uint uFlags, ref StockIconInfo psii);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHDefExtractIcon(string pszIconFile, int iIndex, uint uFlags, out IntPtr phiconLarge, out IntPtr phiconSmall, uint nIconSize);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        /// <summary>
        /// The file names in %LocalAppData%\Greenshot, see cleanup.iss
        /// </summary>
        internal const string ErrorFileName = "notification-error.png";
        internal const string WarningFileName = "notification-warning.png";

        /// <summary>
        /// Writes the icon of the kind once
        /// </summary>
        /// <returns>The png file, null for Info or when it couldn't be written</returns>
        public static string Write(string directory, NotificationKind kind)
        {
            if (kind == NotificationKind.Info)
            {
                return null;
            }
            var path = Path.Combine(directory, kind == NotificationKind.Error ? ErrorFileName : WarningFileName);
            if (File.Exists(path))
            {
                return path;
            }
            try
            {
                var info = new StockIconInfo { cbSize = (uint)Marshal.SizeOf(typeof(StockIconInfo)) };
                if (SHGetStockIconInfo(kind == NotificationKind.Error ? SiidError : SiidWarning, ShgsiIconLocation, ref info) != 0)
                {
                    return null;
                }
                // The large icon in the size, the small one isn't needed
                if (SHDefExtractIcon(info.szPath, info.iIcon, 0, out var large, out var small, Size | (16u << 16)) != 0 || large == IntPtr.Zero)
                {
                    return null;
                }
                try
                {
                    Directory.CreateDirectory(directory);
                    using var icon = Icon.FromHandle(large);
                    using var bitmap = icon.ToBitmap();
                    bitmap.Save(path, ImageFormat.Png);
                    return path;
                }
                finally
                {
                    DestroyIcon(large);
                    if (small != IntPtr.Zero)
                    {
                        DestroyIcon(small);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"Couldn't write the {kind} icon for the notifications.", ex);
                return null;
            }
        }
    }
}
