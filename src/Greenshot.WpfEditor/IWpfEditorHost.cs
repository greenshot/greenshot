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
using System.Threading.Tasks;
using Greenshot.Base.Wpf.Emoji;
using Greenshot.Document;

namespace Greenshot.WpfEditor
{
    /// <summary>
    /// What Greenshot gives the editor: its destinations, which work on the document as a whole (upload, e-mail, Office, ...),
    /// and where settings are kept. The editor itself only saves, copies and prints.
    /// </summary>
    public interface IWpfEditorHost
    {
        IReadOnlyList<EditorDestination> Destinations { get; }

        /// <summary>
        /// The emoji picked last, shared with the other editor
        /// </summary>
        IEmojiHistory EmojiHistory { get; }
    }

    /// <summary>
    /// A destination of Greenshot: its name, and what sends the document there
    /// </summary>
    public sealed record EditorDestination(string Name, Func<GreenshotDocument, string, Task> ExportAsync);
}
