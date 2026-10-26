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

namespace Greenshot.Linux.Core.Destinations;

/// <summary>
/// The result of an export, the ExportInformation of Greenshot.Base
/// </summary>
public sealed record ExportResult(bool Success, string? FilePath = null, string? Message = null, bool Cancelled = false)
{
    public static ExportResult Done(string? filePath = null, string? message = null) => new(true, filePath, message);

    public static ExportResult Failed(string message) => new(false, Message: message);

    public static ExportResult Aborted => new(false, Cancelled: true);
}

/// <summary>
/// Where a capture goes: a file, the clipboard, the editor, ... The designations are the ones of the Windows version,
/// so OutputDestinations in greenshot.ini means the same.
/// </summary>
public interface IDestination
{
    string Designation { get; }

    string DisplayName { get; }

    /// <summary>
    /// The order in the picker, like Priority in Greenshot.Base
    /// </summary>
    int Priority { get; }

    bool IsAvailable { get; }

    Task<ExportResult> ExportAsync(Capture capture, CancellationToken cancellationToken = default);
}

public static class WellKnownDestinations
{
    public const string Picker = "Picker";
    public const string FileDefault = "FileDefault";
    public const string FileWithDialog = "FileWithDialog";
    public const string Clipboard = "Clipboard";
    public const string Editor = "Editor";
}
