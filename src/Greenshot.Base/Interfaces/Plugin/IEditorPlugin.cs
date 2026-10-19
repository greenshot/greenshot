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
using System.Windows.Forms;

namespace Greenshot.Base.Interfaces.Plugin;

/// <summary>
/// A plugin which adds entries to the plugin menu of every editor window
/// </summary>
public interface IEditorPlugin
{
    /// <summary>
    /// Called once for every editor window
    /// </summary>
    /// <param name="editorForm">The editor window</param>
    /// <param name="pluginMenu">The menu to add the entries to</param>
    /// <param name="currentSurface">The surface of the capture which is shown, a window can show several captures in tabs</param>
    void InitializeEditor(Form editorForm, ToolStripMenuItem pluginMenu, Func<ISurface> currentSurface);
}
