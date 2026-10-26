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

using Greenshot.Linux.Core;
using Greenshot.Linux.Core.DBus;
using Greenshot.Linux.Core.Output;
using Greenshot.Linux.Editor.Serialization;

namespace Greenshot.Linux.Destinations;

/// <summary>
/// The save dialog of the desktop (FileChooser portal)
/// </summary>
internal sealed class SaveDialog(FileChooserPortal? portal)
{
    private static readonly IReadOnlyList<(string Name, string Pattern)> ImageFilters =
    [
        ("PNG image", "*.png"),
        ("JPEG image", "*.jpg"),
        ("WebP image", "*.webp"),
    ];

    private static readonly (string Name, string Pattern) GreenshotFilter = ("Greenshot file (editable)", "*" + GreenshotFile.Extension);

    /// <summary>
    /// The filters to open images, including .greenshot files
    /// </summary>
    public static IReadOnlyList<(string Name, string Pattern)> OpenFilters =>
    [
        ("Images and Greenshot files", "*.png;*.jpg;*.jpeg;*.webp;*.bmp;*.gif;*" + GreenshotFile.Extension),
        GreenshotFilter,
    ];

    public FileChooserPortal? Portal => portal;

    public Task<string?> AskAsync(string suggestedPath) => AskAsync(suggestedPath, includeGreenshotFormat: false);

    /// <summary>
    /// Ask for the file, null when cancelled. Without a file dialog the suggested path is used.
    /// With includeGreenshotFormat the editable .greenshot format is offered too.
    /// </summary>
    public async Task<string?> AskAsync(string suggestedPath, bool includeGreenshotFormat)
    {
        if (portal == null)
        {
            Log.Warn("There is no FileChooser portal, saving to the default file instead of asking");
            return suggestedPath;
        }

        var extension = Path.GetExtension(suggestedPath).Equals(GreenshotFile.Extension, StringComparison.OrdinalIgnoreCase)
            ? GreenshotFile.Extension.TrimStart('.')
            : ImageFormats.Normalize(Path.GetExtension(suggestedPath));
        var filters = ImageFilters.ToList();
        if (includeGreenshotFormat)
        {
            filters.Add(GreenshotFilter);
        }

        filters = filters.OrderByDescending(f => f.Pattern == "*." + extension).ToList();
        var path = await portal.SaveFileAsync("Save the capture", suggestedPath, filters).ConfigureAwait(true);
        if (path != null && string.IsNullOrEmpty(Path.GetExtension(path)))
        {
            path += "." + extension;
        }

        return path;
    }
}
