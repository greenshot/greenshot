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
using Greenshot.Document;
using Greenshot.Editor.Standalone.Ipc;
using Greenshot.Editor.Standalone.Tools;

namespace Greenshot.Editor.Standalone.ViewModels
{
    /// <summary>
    /// What all tabs and windows of the editor share: the style and options chosen for each tool, and the elements which were copied
    /// </summary>
    public sealed class EditorSession
    {
        public static EditorSession Current { get; } = new();

        private readonly Dictionary<ToolKind, Style> _styles = new();

        public ToolOptions Options { get; } = new();

        /// <summary>
        /// The style the user chose for new elements of the tool, over the default of the element type
        /// </summary>
        public Style StyleFor(ToolKind kind) => _styles.TryGetValue(kind, out var style) ? style : Style.Empty;

        public void SetStyle(ToolKind kind, Style style) => _styles[kind] = style;

        /// <summary>
        /// The copied elements with the resources they link to, null when nothing was copied
        /// </summary>
        public (IReadOnlyList<Element> Elements, IReadOnlyList<Resource> Resources)? CopiedElements { get; set; }

        /// <summary>
        /// Marks the clipboard content which was copied together with <see cref="CopiedElements"/>
        /// </summary>
        public string CopyMarker { get; set; }

        /// <summary>
        /// The connection to Greenshot, which has the destinations (upload, e-mail, printer, ...). Null when the editor runs alone.
        /// </summary>
        public IGreenshotLink Greenshot { get; set; }
    }
}
