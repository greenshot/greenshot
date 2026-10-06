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
    /// The notification after an export: where the capture went (or why not), and what the user can do with it next.
    /// The actions run on the UI thread, an action which isn't possible is null.
    /// </summary>
    public sealed class ExportNotification
    {
        /// <summary>
        /// False when the export failed
        /// </summary>
        public bool Succeeded { get; set; }

        /// <summary>
        /// The first line, e.g. "Exported to: File"
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The second line: the file, the link, or the error; can be null
        /// </summary>
        public string Detail { get; set; }

        /// <summary>
        /// A PNG file with the icon of the destination, null to show Greenshot's icon
        /// </summary>
        public string IconPath { get; set; }

        /// <summary>
        /// A PNG file with a preview of the capture, null to show none
        /// </summary>
        public string PreviewPath { get; set; }

        /// <summary>
        /// Show the buttons for the actions (not only a click on the notification)
        /// </summary>
        public bool ShowButtons { get; set; }

        /// <summary>
        /// Open the file or link the capture was exported to
        /// </summary>
        public Action Open { get; set; }

        /// <summary>
        /// Show the destination picker and export the capture there too
        /// </summary>
        public Action SendTo { get; set; }

        /// <summary>
        /// Open the capture in the editor
        /// </summary>
        public Action Edit { get; set; }

        /// <summary>
        /// How long the notification is kept, null: as long as Windows keeps it
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// What a click on the notification itself does: open the export, or edit the capture
        /// </summary>
        public Action DefaultAction => Open ?? Edit;
    }
}
