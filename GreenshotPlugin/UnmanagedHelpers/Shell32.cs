﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;

namespace GreenshotPlugin.UnmanagedHelpers {
	/// <summary>
	/// Description of Shell32.
	/// </summary>
	public static class Shell32 {
		[DllImport("shell32", CharSet = CharSet.Unicode)]
		public static extern int ExtractIconEx(string sFile, int iIndex, out IntPtr piLargeVersion, out IntPtr piSmallVersion, int amountIcons);
		[DllImport("shell32", CharSet = CharSet.Unicode)]
		internal static extern IntPtr ExtractAssociatedIcon(HandleRef hInst, StringBuilder iconPath, ref int index);
		[DllImport("shell32", CharSet = CharSet.Unicode)]
		private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
		private struct SHFILEINFO {
			public readonly IntPtr hIcon;
			public readonly int iIcon;
			public readonly uint dwAttributes;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public readonly string szDisplayName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
			public readonly string szTypeName;
		};

        // Browsing for directory.

		private const uint SHGFI_ICON = 0x000000100;     // get icon
		private const uint SHGFI_LINKOVERLAY = 0x000008000;     // put a link overlay on icon
		private const uint SHGFI_LARGEICON = 0x000000000;     // get large icon
		private const uint SHGFI_SMALLICON = 0x000000001;     // get small icon
		private const uint SHGFI_OPENICON = 0x000000002;     // get open icon
		private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;     // use passed dwFileAttribute

		private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
		private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

        /// <summary>
		/// Options to specify the size of icons to return.
		/// </summary>
		public enum IconSize {
			/// <summary>
			/// Specify large icon - 32 pixels by 32 pixels.
			/// </summary>
			Large = 0,
			/// <summary>
			/// Specify small icon - 16 pixels by 16 pixels.
			/// </summary>
			Small = 1
		}

		/// <summary>
		/// Options to specify whether folders should be in the open or closed state.
		/// </summary>
		public enum FolderType {
			/// <summary>
			/// Specify open folder.
			/// </summary>
			Open = 0,
			/// <summary>
			/// Specify closed folder.
			/// </summary>
			Closed = 1
		}

		/// <summary>
		/// Returns an icon for a given file extension - indicated by the name parameter.
		/// See: http://msdn.microsoft.com/en-us/library/windows/desktop/bb762179(v=vs.85).aspx
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <param name="size">Large or small</param>
		/// <param name="linkOverlay">Whether to include the link icon</param>
		/// <returns>System.Drawing.Icon</returns>
		public static Icon GetFileIcon(string filename, IconSize size, bool linkOverlay) {
			SHFILEINFO shfi = new SHFILEINFO();
			// SHGFI_USEFILEATTRIBUTES makes it simulate, just gets the icon for the extension
			uint flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES;

			if (linkOverlay) {
				flags += SHGFI_LINKOVERLAY;
			}

			// Check the size specified for return.
			if (IconSize.Small == size) {
				flags += SHGFI_SMALLICON;
			} else {
				flags += SHGFI_LARGEICON;
			}

			SHGetFileInfo(Path.GetFileName(filename), FILE_ATTRIBUTE_NORMAL, ref shfi, (uint)Marshal.SizeOf(shfi), flags);

			// Only return an icon if we really got one
			if (shfi.hIcon != IntPtr.Zero) {
				// Copy (clone) the returned icon to a new object, thus allowing us to clean-up properly
				Icon icon = (Icon)Icon.FromHandle(shfi.hIcon).Clone();
				// Cleanup
				User32.DestroyIcon(shfi.hIcon);
				return icon;
			}
			return null;
		}

		/// <summary>
		/// Used to access system folder icons.
		/// </summary>
		/// <param name="size">Specify large or small icons.</param>
		/// <param name="folderType">Specify open or closed FolderType.</param>
		/// <returns>System.Drawing.Icon</returns>
		public static Icon GetFolderIcon(IconSize size, FolderType folderType) {
			// Need to add size check, although errors generated at present!
			uint flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES;

			if (FolderType.Open == folderType) {
				flags += SHGFI_OPENICON;
			}

			if (IconSize.Small == size) {
				flags += SHGFI_SMALLICON;
			} else {
				flags += SHGFI_LARGEICON;
			}

			// Get the folder icon
			SHFILEINFO shfi = new SHFILEINFO();
			SHGetFileInfo(null, FILE_ATTRIBUTE_DIRECTORY, ref shfi, (uint)Marshal.SizeOf(shfi), flags);

			//Icon.FromHandle(shfi.hIcon);	// Load the icon from an HICON handle
			// Now clone the icon, so that it can be successfully stored in an ImageList
			Icon icon = (Icon)Icon.FromHandle(shfi.hIcon).Clone();

			// Cleanup
			User32.DestroyIcon(shfi.hIcon);
			return icon;
		}

		/// <summary>
		/// Returns an icon representation of an image contained in the specified file.
		/// This function is identical to System.Drawing.Icon.ExtractAssociatedIcon, xcept this version works.
		/// See: http://stackoverflow.com/questions/1842226/how-to-get-the-associated-icon-from-a-network-share-file
		/// </summary>
		/// <param name="filePath">The path to the file that contains an image.</param>
		/// <returns>The System.Drawing.Icon representation of the image contained in the specified file.</returns>
		public static Icon ExtractAssociatedIcon(string filePath) {
			int index = 0;

			Uri uri;
			if (filePath == null) {
				throw new ArgumentException("Null is not valid for filePath", nameof(filePath));
			}
			try {
				uri = new Uri(filePath);
			} catch (UriFormatException) {
				filePath = Path.GetFullPath(filePath);
				uri = new Uri(filePath);
			}

			if (uri.IsFile) {
				if (File.Exists(filePath)) {
					StringBuilder iconPath = new StringBuilder(1024);
					iconPath.Append(filePath);

					IntPtr handle = ExtractAssociatedIcon(new HandleRef(null, IntPtr.Zero), iconPath, ref index);
					if (handle != IntPtr.Zero) {
						return Icon.FromHandle(handle);
					}
				}
			}
			return null;
		}
	}
}
