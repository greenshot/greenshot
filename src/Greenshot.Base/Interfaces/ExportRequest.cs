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
    /// Everything a destination gets for an export (roadmap section 5.1).
    /// </summary>
    public sealed class ExportRequest
    {
        public ExportRequest(IExportSource source, ICaptureDetails metadata, bool manuallyInitiated, IUserInteraction ui, IProgress<ProgressInfo> progress = null)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Metadata = metadata;
            ManuallyInitiated = manuallyInitiated;
            Ui = ui ?? throw new ArgumentNullException(nameof(ui));
            Progress = progress;
        }

        /// <summary>
        /// The capture to export.
        /// </summary>
        public IExportSource Source { get; }

        /// <summary>
        /// Details of the capture (title, time, filename). The CaptureMetadata of the imaging roadmap, on net48 the capture details.
        /// </summary>
        public ICaptureDetails Metadata { get; }

        /// <summary>
        /// True when the user picked the destination (menu, picker), false when it runs as part of a flow.
        /// </summary>
        public bool ManuallyInitiated { get; }

        /// <summary>
        /// Dialogs, progress and notifications.
        /// </summary>
        public IUserInteraction Ui { get; }

        public IProgress<ProgressInfo> Progress { get; }
    }
}
