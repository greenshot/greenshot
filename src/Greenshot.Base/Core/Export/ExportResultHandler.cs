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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;

namespace Greenshot.Base.Core.Export
{
    /// <summary>
    /// Turns an export result into surface state (upload link, last saved path, modified flag) and the surface message,
    /// which the editor shows in its status bar. The notification of a flow is made from its export log instead
    /// (see Recipes.Pipeline.ExportLog), the text is the same (<see cref="GetMessage"/>). Runs on the UI thread,
    /// through the export source (what AbstractDestination.ProcessExport did inside every destination).
    /// </summary>
    public static class ExportResultHandler
    {
        public static Task ApplyAsync(IDestination destination, ExportResult result, IExportSource source, CancellationToken cancellationToken = default)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (result == null || source == null || result.Status == ExportStatus.Declined)
            {
                return Task.CompletedTask;
            }

            // The message is about the destination which really exported (e.g. the one picked in the picker)
            destination = result.ExportedBy ?? destination;
            string target = result.Target ?? destination.Descriptor?.DisplayName ?? destination.Designation;
            return source.UseSurfaceAsync(surface =>
            {
                Apply(destination, result, surface, target);
                return true;
            }, cancellationToken);
        }

        /// <summary>
        /// What an export tells the user, e.g. "Exported to: Clipboard": in the status bar of the editor and in the notification
        /// </summary>
        /// <returns>null for nothing to tell: declined, or failed without an error</returns>
        public static string GetMessage(ExportResult result, string target)
        {
            if (result == null)
            {
                return null;
            }
            if (result.IsSucceeded)
            {
                return string.Format(Texts.Core.ExportedTo, target);
            }
            if (result.Status == ExportStatus.Failed && !string.IsNullOrEmpty(result.Error))
            {
                return string.Format(Texts.Core.ExportedToError, target) + " " + result.Error;
            }
            return null;
        }

        private static void Apply(IDestination destination, ExportResult result, ISurface surface, string target)
        {
            if (surface == null)
            {
                return;
            }

            string message = GetMessage(result, target);
            if (result.IsSucceeded)
            {
                SurfaceMessageTyp messageType = SurfaceMessageTyp.Info;
                if (result.Uri != null)
                {
                    surface.UploadUrl = result.Uri.AbsoluteUri;
                    messageType = SurfaceMessageTyp.UploadedUri;
                }
                else if (!string.IsNullOrEmpty(result.FilePath))
                {
                    surface.LastSaveFullPath = result.FilePath;
                    messageType = SurfaceMessageTyp.FileSaved;
                }
                surface.SendMessageEvent(destination, messageType, message);

                if (result.ClearsModified)
                {
                    surface.Modified = false;
                }
            }
            else if (message != null)
            {
                surface.SendMessageEvent(destination, SurfaceMessageTyp.Error, message);
            }
        }
    }
}
