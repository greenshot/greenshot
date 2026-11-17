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
    /// Show them on the UI thread (STA).
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
            var result = OpenBuilder(filter, title, initialDirectory).ShowDialog(OwnerOrActive(owner));
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
            var result = OpenBuilder(filter, title, initialDirectory).AllowMultipleSelection().ShowDialog(OwnerOrActive(HandleOf(owner)));
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
                .WithSuggestedFileName(fileName);
            var filters = ParseFilter(filter);
            foreach (var (name, pattern) in filters)
            {
                builder.AddFilter(name, pattern);
            }

            // With a default extension the dialog adds the extension of the chosen file type to a name without one
            string defaultExtension = filters.Select(f => DefaultExtensionOf(f.Pattern)).FirstOrDefault(extension => extension != null);
            if (defaultExtension != null)
            {
                builder.WithDefaultExtension(defaultExtension);
            }

            var result = builder.ShowDialog(OwnerOrActive(owner));
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
                .ShowDialog(OwnerOrActive(HandleOf(owner)));
            return result.WasCancelled ? null : result.SelectedPath;
        }

        /// <summary>
        /// The file types of a filter text: pairs of name and patterns, e.g. "Images (*.png;*.jpg)|*.png;*.jpg|All files (*.*)|*.*".
        /// A name without patterns is left out.
        /// </summary>
        /// <param name="filter">the filter text, null or empty for none</param>
        /// <returns>the name and patterns of each file type</returns>
        public static IReadOnlyList<(string Name, string Pattern)> ParseFilter(string filter)
        {
            var filters = new List<(string Name, string Pattern)>();
            if (string.IsNullOrWhiteSpace(filter))
            {
                return filters;
            }

            string[] parts = filter.Split('|');
            for (int i = 0; i + 1 < parts.Length; i += 2)
            {
                string name = parts[i].Trim();
                string pattern = parts[i + 1].Trim();
                if (pattern.Length == 0)
                {
                    continue;
                }

                filters.Add((name.Length == 0 ? pattern : name, pattern));
            }

            return filters;
        }

        /// <summary>
        /// The extension of the first pattern, without the dot: "gsrecipe.json" for "*.gsrecipe.json;*.json", null for "*.*"
        /// </summary>
        internal static string DefaultExtensionOf(string pattern)
        {
            string first = pattern?.Split(';').Select(p => p.Trim()).FirstOrDefault(p => p.Length > 0);
            if (first == null || !first.StartsWith("*.", StringComparison.Ordinal))
            {
                return null;
            }

            string extension = first.Substring(2);
            return extension.Length == 0 || extension.IndexOfAny(new[] { '*', '?' }) >= 0 ? null : extension;
        }

        private static FileOpenDialogBuilder OpenBuilder(string filter, string title, string initialDirectory)
        {
            var builder = new FileOpenDialogBuilder()
                .WithTitle(title)
                .WithInitialDirectory(initialDirectory);
            foreach (var (name, pattern) in ParseFilter(filter))
            {
                builder.AddFilter(name, pattern);
            }

            return builder;
        }

        private static IntPtr HandleOf(Window window) => window == null ? IntPtr.Zero : new WindowInteropHelper(window).Handle;

        /// <summary>
        /// Like the WPF and WinForms dialogs: without an owner the active window of this thread owns the dialog, so it stays in front of it
        /// </summary>
        private static IntPtr OwnerOrActive(IntPtr owner)
        {
            if (owner != IntPtr.Zero)
            {
                return owner;
            }

            // The windows of the application can only be read on its thread
            var application = Application.Current;
            if (application != null && application.Dispatcher.CheckAccess())
            {
                var active = application.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive);
                if (active != null)
                {
                    return HandleOf(active);
                }
            }

            return System.Windows.Forms.Form.ActiveForm?.Handle ?? IntPtr.Zero;
        }
    }
}
