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

namespace Greenshot.Base.Interfaces
{
    /// <summary>
    /// Outcome of an export.
    /// </summary>
    public enum ExportStatus
    {
        Succeeded,

        /// <summary>
        /// The user said no (closed a dialog). Cancellation is not a result, it throws an OperationCanceledException (rule R7).
        /// </summary>
        Declined,

        Failed
    }

    /// <summary>
    /// The result of <see cref="IDestination.ExportAsync"/>. The caller turns it into surface state and notifications.
    /// </summary>
    public sealed class ExportResult
    {
        public ExportResult(ExportStatus status, string filePath = null, Uri uri = null, string error = null, string target = null, bool clearsModified = true, bool keepsCapture = false,
            Exception exception = null)
        {
            Exception = exception;
            Status = status;
            FilePath = filePath;
            Uri = uri;
            Error = error;
            Target = target;
            ClearsModified = clearsModified;
            KeepsCapture = keepsCapture;
        }

        /// <summary>
        /// True when the destination took over the capture (the editor shows the surface): the flow must not dispose it.
        /// </summary>
        public bool KeepsCapture { get; }

        /// <summary>
        /// The same result for another target (e.g. the destination picked in the picker).
        /// </summary>
        public ExportResult WithTarget(string target) => new ExportResult(Status, FilePath, Uri, Error, target, ClearsModified, KeepsCapture, Exception) { ExportedBy = ExportedBy };

        /// <summary>
        /// The same result, exported by another destination than the one which returns it (e.g. the one picked in the picker).
        /// </summary>
        public ExportResult WithExportedBy(IDestination destination) =>
            new ExportResult(Status, FilePath, Uri, Error, Target, ClearsModified, KeepsCapture, Exception) { ExportedBy = destination };

        /// <summary>
        /// The destination which really exported the capture when it isn't the one which returns the result (e.g. the picker), null otherwise.
        /// The notification shows its icon.
        /// </summary>
        public IDestination ExportedBy { get; private set; }

        /// <summary>
        /// The exception which made the export fail, if any (for the log and the error report).
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// True (default) when a successful export counts as "saved": the modified state of the capture is cleared.
        /// The editor destination only opens the capture, it doesn't save it.
        /// </summary>
        public bool ClearsModified { get; }

        public ExportStatus Status { get; }

        /// <summary>
        /// The file the capture was written to, if any.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// The link to the uploaded capture, if any.
        /// </summary>
        public Uri Uri { get; }

        /// <summary>
        /// Why the export failed.
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// Name of what received the capture when it isn't the destination itself (e.g. the app chosen in the share dialog).
        /// </summary>
        public string Target { get; }

        public bool IsSucceeded => Status == ExportStatus.Succeeded;

        public static ExportResult Declined { get; } = new ExportResult(ExportStatus.Declined);

        public static ExportResult Succeeded(string filePath = null, Uri uri = null, string target = null, bool clearsModified = true, bool keepsCapture = false) =>
            new ExportResult(ExportStatus.Succeeded, filePath, uri, target: target, clearsModified: clearsModified, keepsCapture: keepsCapture);

        public static ExportResult Failed(string error, Exception exception = null) => new ExportResult(ExportStatus.Failed, error: error, exception: exception);

        public override string ToString() => $"{Status}{(FilePath != null ? " " + FilePath : "")}{(Uri != null ? " " + Uri : "")}{(Error != null ? ": " + Error : "")}";
    }
}
