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

using System.Text;
using Tmds.DBus.Protocol;

namespace Greenshot.Linux.Core.DBus;

/// <summary>
/// The file dialog of the desktop through the FileChooser portal, so "Save as" looks like every other application
/// </summary>
public sealed class FileChooserPortal(DesktopPortal portal)
{
    private const string FileChooserInterface = "org.freedesktop.portal.FileChooser";

    public static async Task<bool> IsAvailableAsync(DesktopPortal portal) =>
        await portal.GetVersionAsync(FileChooserInterface).ConfigureAwait(false) != null;

    /// <summary>
    /// Ask where to save, null when the user cancels
    /// </summary>
    /// <param name="filters">name and glob pattern, e.g. ("PNG image", "*.png")</param>
    public async Task<string?> SaveFileAsync(string title, string suggestedPath, IReadOnlyList<(string Name, string Pattern)> filters,
        CancellationToken cancellationToken = default)
    {
        var folder = Path.GetDirectoryName(suggestedPath);
        var response = await portal.RequestAsync(FileChooserInterface, "SaveFile", "ssa{sv}", (ref MessageWriter writer, string token) =>
        {
            writer.WriteString(string.Empty);
            writer.WriteString(title);

            // Written entry by entry: current_folder and the filters are not simple values for WriteOptions
            var options = writer.WriteDictionaryStart();
            WriteKey(ref writer, "handle_token");
            writer.WriteVariantString(token);
            WriteKey(ref writer, "current_name");
            writer.WriteVariantString(Path.GetFileName(suggestedPath));
            if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            {
                // A byte array with a trailing zero, the path doesn't need to be valid UTF-8
                WriteKey(ref writer, "current_folder");
                writer.WriteSignature("ay");
                writer.WriteArray(Encoding.UTF8.GetBytes(folder + "\0"));
            }

            if (filters.Count > 0)
            {
                WriteKey(ref writer, "filters");
                writer.WriteSignature("a(sa(us))");
                var array = writer.WriteArrayStart(DBusType.Struct);
                foreach (var filter in filters)
                {
                    WriteFilter(ref writer, filter);
                }

                writer.WriteArrayEnd(array);

                WriteKey(ref writer, "current_filter");
                writer.WriteSignature("(sa(us))");
                WriteFilter(ref writer, filters[0]);
            }

            writer.WriteDictionaryEnd(options);
        }, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccess || !response.Results.TryGetValue("uris", out var uris) || uris.Count == 0)
        {
            return null;
        }

        return new Uri(uris.GetItem(0).GetString()).LocalPath;
    }

    /// <summary>
    /// Ask for a file to open, null when the user cancels
    /// </summary>
    /// <param name="directory">Choose a folder instead of a file</param>
    public async Task<string?> OpenFileAsync(string title, IReadOnlyList<(string Name, string Pattern)> filters,
        CancellationToken cancellationToken = default, bool directory = false)
    {
        var response = await portal.RequestAsync(FileChooserInterface, "OpenFile", "ssa{sv}", (ref MessageWriter writer, string token) =>
        {
            writer.WriteString(string.Empty);
            writer.WriteString(title);
            var options = writer.WriteDictionaryStart();
            WriteKey(ref writer, "handle_token");
            writer.WriteVariantString(token);
            if (directory)
            {
                WriteKey(ref writer, "directory");
                writer.WriteVariantBool(true);
            }

            if (filters.Count > 0 && !directory)
            {
                WriteKey(ref writer, "filters");
                writer.WriteSignature("a(sa(us))");
                var array = writer.WriteArrayStart(DBusType.Struct);
                foreach (var filter in filters)
                {
                    WriteFilter(ref writer, filter);
                }

                writer.WriteArrayEnd(array);
            }

            writer.WriteDictionaryEnd(options);
        }, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccess || !response.Results.TryGetValue("uris", out var uris) || uris.Count == 0)
        {
            return null;
        }

        return new Uri(uris.GetItem(0).GetString()).LocalPath;
    }

    public Task<string?> OpenFolderAsync(string title, CancellationToken cancellationToken = default) =>
        OpenFileAsync(title, [], cancellationToken, directory: true);

    /// <summary>
    /// Start a dictionary entry; the caller writes the variant signature and value
    /// </summary>
    private static void WriteKey(ref MessageWriter writer, string key)
    {
        writer.WriteDictionaryEntryStart();
        writer.WriteString(key);
    }

    private static void WriteFilter(ref MessageWriter writer, (string Name, string Pattern) filter)
    {
        writer.WriteStructureStart();
        writer.WriteString(filter.Name);
        var patterns = writer.WriteArrayStart(DBusType.Struct);
        // One filter can have more patterns, separated by ;
        foreach (var pattern in filter.Pattern.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            writer.WriteStructureStart();
            // 0 is a glob pattern, 1 would be a MIME type
            writer.WriteUInt32(0);
            writer.WriteString(pattern);
        }

        writer.WriteArrayEnd(patterns);
    }
}
