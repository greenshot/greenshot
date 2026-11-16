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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Document;

namespace Greenshot.Editor.Standalone.Ipc
{
    /// <summary>
    /// A destination of Greenshot (upload, e-mail, printer, Office, ...): the name Greenshot knows it by and the text to show
    /// </summary>
    public sealed record EditorDestination(string Designation, string Name)
    {
        public override string ToString() => Name;
    }

    /// <summary>
    /// What the editor gets from Greenshot: its destinations, which send a document somewhere. The editor itself only saves
    /// and copies.
    /// </summary>
    public interface IGreenshotLink
    {
        /// <summary>
        /// True while Greenshot answers on its pipe
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// The destinations of Greenshot, empty while it isn't connected
        /// </summary>
        IReadOnlyList<EditorDestination> Destinations { get; }

        /// <summary>
        /// Greenshot was found or lost, or its destinations changed. Raised on the UI thread.
        /// </summary>
        event Action Changed;

        /// <summary>
        /// Asks Greenshot for its destinations again, e.g. before they are shown
        /// </summary>
        Task RefreshAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends the document to the destination. Throws when Greenshot can't be reached or the export fails.
        /// </summary>
        Task ExportAsync(EditorDestination destination, GreenshotDocument document, string title, CancellationToken cancellationToken = default);
    }
}
