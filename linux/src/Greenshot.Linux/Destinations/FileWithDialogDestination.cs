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

using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Core.Destinations;
using Greenshot.Linux.Core.Languages;

namespace Greenshot.Linux.Destinations;

/// <summary>
/// "Save as": asks for the file with the dialog of the desktop, then saves like FileDefault
/// </summary>
internal sealed class FileWithDialogDestination(CoreSettings settings, SaveDialog dialog) : IDestination
{
    public string Designation => WellKnownDestinations.FileWithDialog;

    public string DisplayName => Texts.WithoutAccessKey(Texts.Editor("saveas"));

    public int Priority => 1;

    public bool IsAvailable => true;

    public async Task<ExportResult> ExportAsync(Capture capture, CancellationToken cancellationToken = default)
    {
        var suggested = FileDestination.GetDefaultPath(settings, capture.Details);
        var lastFolder = Path.GetDirectoryName(settings.OutputFileAsFullpath);
        if (!string.IsNullOrEmpty(lastFolder) && Directory.Exists(lastFolder))
        {
            suggested = Path.Combine(lastFolder, Path.GetFileName(suggested));
        }

        var path = await dialog.AskAsync(suggested).ConfigureAwait(true);
        if (path == null)
        {
            return ExportResult.Aborted;
        }

        // The folder chosen last is the one offered next time, like the SaveAs of Windows
        settings.OutputFileAsFullpath = path;
        return await FileDestination.SaveAsync(settings, capture, path, cancellationToken).ConfigureAwait(true);
    }
}
