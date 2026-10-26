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
    /// <summary>
    /// A link the notification opens when it is clicked, e.g. the URL of a QR code
    /// </summary>
    public string? Link { get; init; }

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
    /// <summary>
    /// Save directly, "FileDefault" in older settings
    /// </summary>
    public const string FileDefault = "FileNoDialog";

    /// <summary>
    /// Save as, "FileWithDialog" in older settings
    /// </summary>
    public const string FileWithDialog = "FileDialog";
    public const string Clipboard = "Clipboard";
    public const string Editor = "Editor";
    public const string Printer = "Printer";
    public const string Email = "EMail";
    public const string Ocr = "OCR";

    /// <summary>
    /// The OCR destination of the Windows version, so a greenshot.ini of Windows works
    /// </summary>
    public const string WindowsOcr = "Windows10OCR";
    public const string QrCode = "ZxingQrDestination";

    /// <summary>
    /// The current name of a destination: older settings and the Windows version used other names for some
    /// </summary>
    public static string Normalize(string designation) => designation.Trim() switch
    {
        var d when d.Equals("FileDefault", StringComparison.OrdinalIgnoreCase) => FileDefault,
        var d when d.Equals("FileWithDialog", StringComparison.OrdinalIgnoreCase) => FileWithDialog,
        var d when d.Equals(WindowsOcr, StringComparison.OrdinalIgnoreCase) => Ocr,
        var d => d,
    };
}
