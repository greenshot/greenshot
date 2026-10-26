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
using Greenshot.Linux.Core.Naming;
using Greenshot.Linux.Core.Output;

namespace Greenshot.Linux.Core.Destinations;

/// <summary>
/// Saves the capture in the configured folder with the filename pattern, without asking (FileDefault)
/// </summary>
public sealed class FileDestination(CoreSettings settings, Func<string, Task<string?>>? askPath = null) : IDestination
{
    public string Designation => WellKnownDestinations.FileDefault;

    public string DisplayName => "Save";

    public int Priority => 0;

    public bool IsAvailable => true;

    public async Task<ExportResult> ExportAsync(Capture capture, CancellationToken cancellationToken = default)
    {
        var path = GetDefaultPath(settings, capture.Details);

        // Like Windows: without OutputFileAllowOverwrite an existing file brings up the save-as dialog
        if (File.Exists(path) && !settings.OutputFileAllowOverwrite)
        {
            if (askPath == null)
            {
                path = MakeUnique(path);
            }
            else
            {
                var chosen = await askPath(path).ConfigureAwait(false);
                if (chosen == null)
                {
                    return ExportResult.Aborted;
                }

                path = chosen;
            }
        }

        return await SaveAsync(settings, capture, path, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// The folder of OutputFilePath (variables like ${YYYY} are filled) and the name of OutputFileFilenamePattern
    /// </summary>
    public static string GetDefaultPath(CoreSettings settings, CaptureDetails details)
    {
        var pattern = FilenamePattern.ForSettings(settings);
        var folder = pattern.Fill(settings.OutputFilePath, details, filenameSafe: false);
        if (folder.StartsWith("~/", StringComparison.Ordinal))
        {
            folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), folder[2..]);
        }

        var filename = pattern.GetFilename(settings.OutputFileFilenamePattern, ImageFormats.Normalize(settings.OutputFileFormat), details);
        return Path.Combine(folder, filename);
    }

    /// <summary>
    /// Write the capture, the format follows the extension of the path
    /// </summary>
    public static async Task<ExportResult> SaveAsync(CoreSettings settings, Capture capture, string path, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = ImageFormats.Encode(capture.Image, Path.GetExtension(path) is { Length: > 1 } ext ? ext : settings.OutputFileFormat,
                settings.OutputFileJpegQuality);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, data, cancellationToken).ConfigureAwait(false);
            Log.Info($"Saved the capture to {path}");

            if (settings.OutputFileCopyPathToClipboard && SystemClipboard.IsAvailable)
            {
                await SystemClipboard.SetTextAsync(path).ConfigureAwait(false);
            }

            return ExportResult.Done(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Log.Error($"Saving to {path} failed", ex);
            return ExportResult.Failed($"Could not save {path}: {ex.Message}");
        }
    }

    internal static string MakeUnique(string path)
    {
        var folder = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        for (int i = 1; ; i++)
        {
            var candidate = Path.Combine(folder, $"{name} ({i}){extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }
    }
}
