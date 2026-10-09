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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Export;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Threading;
using Dapplo.Windows.Com;
using Microsoft.Win32;

namespace Greenshot.Plugin.Office.Destinations
{
    /// <summary>
    /// What the Office destinations share: the COM calls run on an STA worker per Office application (with the OLE message filter),
    /// so a hanging Outlook doesn't block Word, the capture is handed over as a file.
    /// </summary>
    public abstract class OfficeDestinationBase : DestinationBase
    {
        private static readonly Regex ImageFileRegex = new Regex(@".*(\.png|\.gif|\.jpg|\.jpeg|\.tiff|\.bmp)$", RegexOptions.Compiled);

        /// <summary>
        /// The STA thread for the COM calls of this Office application
        /// </summary>
        protected IStaWorker Office => StaWorkers.Get(Designation);

        /// <summary>
        /// The executable of the COM server registered for the ProgID, e.g. "Word.Application", null when it's not registered.
        /// This is the registration COM activation uses, so it works for every kind of Office installation (MSI or Click-to-Run).
        /// </summary>
        internal static string GetComServerPath(string progId)
        {
            var classId = Ole32Api.ClassIdFromProgId(progId);
            if (!classId.HasValue)
            {
                return null;
            }

            // A 32-bit Office registers its classes in the 32-bit view
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                using var classesRoot = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, view);
                using var localServer = classesRoot.OpenSubKey($@"CLSID\{classId.Value:B}\LocalServer32", false);
                // e.g. "C:\...\WINWORD.EXE" /Automation
                string command = (localServer?.GetValue(null) as string)?.Trim();
                if (string.IsNullOrEmpty(command))
                {
                    continue;
                }

                // The path is quoted, or ends at ".exe"
                int exeEnd = command.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
                string path = command.StartsWith("\"") ? command.Split('"')[1] : exeEnd >= 0 ? command.Substring(0, exeEnd + 4) : command;
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        /// <summary>
        /// The icon of an Office application (or of its documents)
        /// </summary>
        protected static string IconKeyFor(string exePath, int index) => DestinationIcons.Exe(exePath, index);

        /// <summary>
        /// Run the COM code on the STA worker of this Office application
        /// </summary>
        protected Task<T> RunOnOfficeAsync<T>(System.Func<T> comCall, CancellationToken cancellationToken) => Office.RunAsync(comCall, cancellationToken);

        /// <summary>
        /// A file with the capture: the file it was loaded from when it's unchanged, else a new temporary file.
        /// </summary>
        /// <returns>the path and if the file was created (and can be deleted after the export)</returns>
        protected static async Task<(string Path, bool Created)> GetImageFileAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            string imageFile = request.Metadata?.Filename;
            if (imageFile != null && !request.Source.IsModified && ImageFileRegex.IsMatch(imageFile))
            {
                return (imageFile, false);
            }

            imageFile = await ExportFiles.SaveNamedTmpFileAsync(request.Source, request.Metadata, new SurfaceOutputSettings().PreventGreenshotFormat(), cancellationToken).ConfigureAwait(false);
            return (imageFile, true);
        }

        /// <summary>
        /// The size of the capture (as it's exported)
        /// </summary>
        protected static async Task<Size> GetImageSizeAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            using var lease = await request.Source.RenderAsync(new SurfaceOutputSettings().PreventGreenshotFormat(), cancellationToken).ConfigureAwait(false);
            return lease.Image?.Size ?? Size.Empty;
        }

        /// <summary>
        /// Let the user choose between a new document and the open ones, then export to the choice.
        /// </summary>
        protected static async Task<ExportResult> PickAndExportAsync(ExportRequest request, IReadOnlyList<IDestination> choices, CancellationToken cancellationToken)
        {
            var picked = await request.Ui.PickDestinationAsync(choices, request.Metadata, cancellationToken).ConfigureAwait(false);
            if (picked == null)
            {
                return ExportResult.Declined;
            }

            // The user picked it: export as manually initiated, else "new document" would show the picker again
            var pickedRequest = new ExportRequest(request.Source, request.Metadata, true, request.Ui, request.Progress);
            var result = await picked.ExportAsync(pickedRequest, cancellationToken).ConfigureAwait(false);
            return result.WithTarget(picked.Descriptor?.DisplayName);
        }

        /// <summary>
        /// The dynamic destinations, created from the names the COM call returned
        /// </summary>
        protected async ValueTask<IReadOnlyList<IDestination>> GetDynamicDestinationsAsync(System.Func<IEnumerable<string>> getNames, System.Func<string, IDestination> create, CancellationToken cancellationToken)
        {
            // Materialize on the STA thread, the COM enumeration must not leave it
            var names = await RunOnOfficeAsync(() => getNames().ToList(), cancellationToken).ConfigureAwait(false);
            return names.Select(create).ToList();
        }
    }
}
