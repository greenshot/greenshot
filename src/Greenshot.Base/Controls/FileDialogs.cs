/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using Dapplo.Windows.Dialogs;

namespace Greenshot.Base.Controls
{
    /// <summary>
    /// The open, save and folder dialogs of Greenshot: the Windows Common Item Dialog of Dapplo.Windows.Dialogs (long paths, no WinForms or WPF dialog).
    /// The file types are given as filter text like "Recipes (*.gsrecipe.json)|*.gsrecipe.json|All files (*.*)|*.*", as the language packs have them.
    /// Without an owner the active window of the thread owns the dialog. Show them on the UI thread (STA).
    /// </summary>
    public static class FileDialogs
    {
        /// <summary>
        /// Let the user pick a file to open
        /// </summary>
        /// <param name="owner">the window handle of the owner, IntPtr.Zero for the active window</param>
        /// <param name="filter">the file types, null or empty for all files</param>
        /// <param name="title">the title of the dialog, null for the Windows default</param>
        /// <param name="initialDirectory">the folder to start in, null for the last one</param>
        /// <returns>the path, null when the user cancelled</returns>
        public static string PickFileToOpen(IntPtr owner, string filter, string title = null, string initialDirectory = null)
        {
            var result = OpenBuilder(filter, title, initialDirectory).ShowDialog(owner);
            return result.WasCancelled ? null : result.SelectedPath;
        }

        /// <inheritdoc cref="PickFileToOpen(IntPtr, string, string, string)"/>
        /// <param name="owner">the owner window, null for the active window</param>
        public static string PickFileToOpen(Window owner, string filter, string title = null, string initialDirectory = null) =>
            PickFileToOpen(HandleOf(owner), filter, title, initialDirectory);

        /// <summary>
        /// Let the user pick one or more files to open
        /// </summary>
        /// <param name="owner">the owner window, null for the active window</param>
        /// <param name="filter">the file types, null or empty for all files</param>
        /// <param name="title">the title of the dialog, null for the Windows default</param>
        /// <param name="initialDirectory">the folder to start in, null for the last one</param>
        /// <returns>the paths, empty when the user cancelled</returns>
        public static IReadOnlyList<string> PickFilesToOpen(Window owner, string filter, string title = null, string initialDirectory = null)
        {
            var result = OpenBuilder(filter, title, initialDirectory).AllowMultipleSelection().ShowDialog(HandleOf(owner));
            return result.WasCancelled ? Array.Empty<string>() : result.SelectedPaths ?? Array.Empty<string>();
        }

        /// <summary>
        /// Let the user pick where to save a file. Windows asks before a file is overwritten, and adds the extension of the chosen file type to a name without one.
        /// </summary>
        /// <param name="owner">the window handle of the owner, IntPtr.Zero for the active window</param>
        /// <param name="filter">the file types, null or empty for all files</param>
        /// <param name="fileName">the proposed file name, null for none</param>
        /// <param name="title">the title of the dialog, null for the Windows default</param>
        /// <param name="initialDirectory">the folder to start in, null for the last one</param>
        /// <returns>the path, null when the user cancelled</returns>
        public static string PickFileToSave(IntPtr owner, string filter, string fileName = null, string title = null, string initialDirectory = null)
        {
            var builder = new FileSaveDialogBuilder()
                .WithTitle(title)
                .WithInitialDirectory(initialDirectory)
                .WithSuggestedFileName(fileName)
                .AddFilters(filter);

            // With a default extension the dialog adds the extension of the chosen file type to a name without one
            string defaultExtension = DefaultExtensionOf(filter);
            if (defaultExtension != null)
            {
                builder.WithDefaultExtension(defaultExtension);
            }

            var result = builder.ShowDialog(owner);
            return result.WasCancelled ? null : result.SelectedPath;
        }

        /// <inheritdoc cref="PickFileToSave(IntPtr, string, string, string, string)"/>
        /// <param name="owner">the owner window, null for the active window</param>
        public static string PickFileToSave(Window owner, string filter, string fileName = null, string title = null, string initialDirectory = null) =>
            PickFileToSave(HandleOf(owner), filter, fileName, title, initialDirectory);

        /// <summary>
        /// Let the user pick a folder
        /// </summary>
        /// <param name="owner">the owner window, null for the active window</param>
        /// <param name="title">the title of the dialog, null for the Windows default</param>
        /// <param name="initialDirectory">the folder to start in, null or a missing folder for the last one</param>
        /// <returns>the folder, null when the user cancelled</returns>
        public static string PickFolder(Window owner, string title = null, string initialDirectory = null)
        {
            var result = new FolderPickerBuilder()
                .WithTitle(title)
                .WithInitialDirectory(initialDirectory)
                .ShowDialog(HandleOf(owner));
            return result.WasCancelled ? null : result.SelectedPath;
        }

        /// <summary>
        /// The extension of the first pattern of the first file type that has one, without the dot:
        /// "gsrecipe.json" for "Recipes|*.gsrecipe.json;*.json|All files|*.*", null when there is none (e.g. only "*.*")
        /// </summary>
        /// <param name="filter">the filter text, null or empty for none</param>
        internal static string DefaultExtensionOf(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return null;
            }

            string[] parts = filter.Split('|');
            for (int i = 1; i < parts.Length; i += 2)
            {
                string first = parts[i].Split(';').Select(pattern => pattern.Trim()).FirstOrDefault(pattern => pattern.Length > 0);
                if (first == null || !first.StartsWith("*.", StringComparison.Ordinal))
                {
                    continue;
                }

                string extension = first.Substring(2);
                if (extension.Length > 0 && extension.IndexOfAny(new[] { '*', '?' }) < 0)
                {
                    return extension;
                }
            }

            return null;
        }

        private static FileOpenDialogBuilder OpenBuilder(string filter, string title, string initialDirectory) =>
            new FileOpenDialogBuilder()
                .WithTitle(title)
                .WithInitialDirectory(initialDirectory)
                .AddFilters(filter);

        private static IntPtr HandleOf(Window window) => window == null ? IntPtr.Zero : new WindowInteropHelper(window).Handle;
    }
}
