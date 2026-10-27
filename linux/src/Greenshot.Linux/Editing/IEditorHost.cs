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

using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Core.DBus;
using Greenshot.Linux.Core.Destinations;
using Greenshot.Linux.Editor;

namespace Greenshot.Linux.Editing;

/// <summary>
/// What the editor uses of the running Greenshot: settings, the destinations and the notifications
/// </summary>
internal interface IEditorHost
{
    CoreSettings Settings { get; }

    EditorSettings EditorSettings { get; }

    /// <summary>
    /// The destinations a finished image can go to (not the editor or the picker)
    /// </summary>
    IReadOnlyList<IDestination> ExportDestinations { get; }

    FileChooserPortal? FileChooser { get; }

    /// <summary>
    /// Tell the user how an export went, like after a capture
    /// </summary>
    Task ReportAsync(IDestination destination, ExportResult result);

    /// <summary>
    /// Open a file in a new editor window
    /// </summary>
    Task OpenFileAsync(string path);

    /// <summary>
    /// The recipes of the editor's Recipes menu (Editor triggers)
    /// </summary>
    IReadOnlyList<Greenshot.Linux.Recipes.Triggers.IEditorTrigger> EditorRecipeTriggers { get; }

    /// <summary>
    /// The recipe manager and the recipe editor
    /// </summary>
    Greenshot.Linux.Recipes.IRecipeEditorService RecipeEditor { get; }
}
