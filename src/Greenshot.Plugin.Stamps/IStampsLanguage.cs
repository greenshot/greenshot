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


using System.ComponentModel;
using Dapplo.Ini.Internationalization.Attributes;

namespace Greenshot.Plugin.Stamps
{
    /// <summary>
    /// The texts of the [Stamps] section of greenshot.stamps.{ietf}.ini, the English text is shown per property.
    /// Generated from the language files, add a key to the en-US file and a property here.
    /// </summary>
    [IniLanguageSection("Stamps", ModuleName = "stamps")]
    public interface IStampsLanguage : INotifyPropertyChanged
    {
        /// <summary>
        /// Stamps...
        /// </summary>
        string Menu { get; }

        /// <summary>
        /// Stamps
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Search:
        /// </summary>
        string Search { get; }

        /// <summary>
        /// Add...
        /// </summary>
        string Add { get; }

        /// <summary>
        /// Add stamps
        /// </summary>
        string AddTitle { get; }

        /// <summary>
        /// Image files
        /// </summary>
        string ImageFiles { get; }

        /// <summary>
        /// Paste
        /// </summary>
        string Paste { get; }

        /// <summary>
        /// Insert
        /// </summary>
        string Insert { get; }

        /// <summary>
        /// Insert the stamp into the editor (Enter or double-click)
        /// </summary>
        string InsertTooltip { get; }

        /// <summary>
        /// Copy
        /// </summary>
        string Copy { get; }

        /// <summary>
        /// Copy the stamp for other programs (Ctrl+C), or drag it there
        /// </summary>
        string CopyTooltip { get; }

        /// <summary>
        /// Delete
        /// </summary>
        string Delete { get; }

        /// <summary>
        /// Delete the stamp "{0}"? Its file is removed from the stamps folder.
        /// </summary>
        string DeleteQuestion { get; }

        /// <summary>
        /// Open folder
        /// </summary>
        string OpenFolder { get; }

        /// <summary>
        /// Name:
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Tags:
        /// </summary>
        string Tags { get; }

        /// <summary>
        /// Separate the tags with commas.
        /// </summary>
        string TagsHint { get; }

        /// <summary>
        /// No stamps yet: add images with Add, drop files here or paste an image with Ctrl+V.
        /// </summary>
        string Empty { get; }

        /// <summary>
        /// No stamp matches the search.
        /// </summary>
        string NoMatch { get; }

        /// <summary>
        /// {0} of {1} stamps
        /// </summary>
        string StampCount { get; }

        /// <summary>
        /// Inserted "{0}" into the editor.
        /// </summary>
        string Inserted { get; }

        /// <summary>
        /// Copied "{0}" to the clipboard.
        /// </summary>
        string Copied { get; }

        /// <summary>
        /// Open a capture in the editor to insert stamps.
        /// </summary>
        string NoEditor { get; }

        /// <summary>
        /// Could not add "{0}": {1}
        /// </summary>
        string AddFailed { get; }

        /// <summary>
        /// Could not insert "{0}": {1}
        /// </summary>
        string InsertFailed { get; }
    }
}
