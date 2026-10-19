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


namespace Greenshot.Base.Interfaces
{
    /// <summary>
    /// An error the user should read, with the details folded away for whoever has to fix it (a service, the author of a file),
    /// for destinations and flow steps (ShowDialogAsync). The view is registered by Greenshot; it only closes, the result is true.
    /// </summary>
    public sealed class ErrorDetailsRequest : IDialogViewModel<bool>
    {
        public ErrorDetailsRequest(string title, string message, string note, string details)
        {
            Title = title;
            Message = message;
            Note = note;
            Details = details;
        }

        public string Title { get; }

        /// <summary>
        /// What went wrong, in the words of the user
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Shown below the message, e.g. who can help; null: none
        /// </summary>
        public string Note { get; }

        /// <summary>
        /// Shown when the user expands the details, and copied with "Copy details"; no secrets. null: no details
        /// </summary>
        public string Details { get; }
    }
}
