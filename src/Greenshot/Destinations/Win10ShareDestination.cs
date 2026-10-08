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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.Export;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Core.FileFormat;

namespace Greenshot.Destinations
{
    /// <summary>
    /// What to share with the Windows share dialog, the view (ShareHostWindow) returns the name of the app, null when nothing was shared.
    /// </summary>
    public sealed class ShareRequest : IDialogViewModel<string>
    {
        public ShareRequest(string filePath, string title)
        {
            FilePath = filePath;
            Title = title;
        }

        /// <summary>
        /// The capture, saved as PNG
        /// </summary>
        public string FilePath { get; }

        public string Title { get; }
    }

    /// <summary>
    /// This uses the Windows Share dialog to make the capture available to apps.
    /// </summary>
    public class Win10ShareDestination : DestinationBase
    {
        public override string Designation { get; } = "Windows10Share";

        /// <summary>
        /// Icon for the App-share, the icon was found via: https://help4windows.com/windows_8_shell32_dll.shtml
        /// </summary>
        public override DestinationDescriptor Descriptor { get; } =
            new DestinationDescriptor("Windows share", 3, DestinationIcons.Exe(FilenameHelper.FillCmdVariables(@"%windir%\system32\shell32.dll"), 238));

        /// <summary>
        /// Share the screenshot with a windows app
        /// </summary>
        public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            SharingFiles.CleanupOldShareFiles();
            // A unique name, an app which still has an older share open keeps its file
            string filePath = Path.Combine(Path.GetTempPath(), $"greenshot_share_{Guid.NewGuid()}.png");
            await ExportFiles.SaveAsync(request.Source, filePath, false, new SurfaceOutputSettings(WellKnownFileFormats.Png), cancellationToken).ConfigureAwait(false);
            string appName = await request.Ui.ShowDialogAsync(new ShareRequest(filePath, request.Metadata?.Title), cancellationToken).ConfigureAwait(false);
            return appName == null ? ExportResult.Declined : ExportResult.Succeeded(target: appName);
        }
    }

    /// <summary>
    /// The temporary files for the share dialog.
    /// </summary>
    public static class SharingFiles
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(SharingFiles));

        /// <summary>
        /// Try to clean up the files of previous shares, an app which still has one open keeps it (the delete fails).
        /// </summary>
        public static void CleanupOldShareFiles()
        {
            try
            {
                foreach (string file in Directory.GetFiles(Path.GetTempPath(), "greenshot_share_*.png"))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (IOException)
                    {
                        // Still in use
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Still in use
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug("Couldn't clean up old share files", ex);
            }
        }
    }
}
