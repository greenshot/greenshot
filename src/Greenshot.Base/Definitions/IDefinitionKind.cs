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

using Greenshot.Base.Recipes.Pipeline;

namespace Greenshot.Base.Definitions;

/// <summary>
/// What a plugin tells the shared definitions code about its kind of definition (an uploader, a command):
/// how it is named, stored, described and approved. The plugin keeps what only it knows, how to run one.
/// See docs/vNext/destination-definitions.md.
/// </summary>
/// <typeparam name="T">the definition, stored as JSON</typeparam>
public interface IDefinitionKind<T> where T : class
{
    /// <summary>
    /// The designation of the destination with the definitions, e.g. "CustomUploader". The destination of one definition is
    /// "CustomUploader:name", its icon key "customuploader:name".
    /// </summary>
    string Designation { get; }

    /// <summary>
    /// The name of the definition, unique (ignoring case) among the definitions of the kind
    /// </summary>
    string NameOf(T definition);

    /// <summary>
    /// Read and check a stored definition like an imported file, so what runs stays what the user approved.
    /// </summary>
    /// <returns>null when it can't be read or doesn't pass the checks anymore</returns>
    T Read(string json);

    /// <summary>
    /// The definition as JSON, ASCII only: it is encrypted with <see cref="Core.StringExtensions.Encrypt"/>, which keeps only ASCII
    /// </summary>
    string Write(T definition);

    /// <summary>
    /// What the definition does in a few words, e.g. the server of an uploader: shown in the list next to the name
    /// </summary>
    string Describe(T definition);

    /// <summary>
    /// What the user approves when a recipe uses the definition. A definition which changes (imported again with another
    /// server, edited) must give another target, so it needs a new approval.
    /// </summary>
    RecipeGatedAction CreateGatedAction(T definition);
}
