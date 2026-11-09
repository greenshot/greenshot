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


using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// The recipes installed from the recipe catalog: which have a newer version in the catalog, and updating one of them. A recipe from
    /// the catalog is recognized by its file (Recipes\Catalog\&lt;id&gt;.gsrecipe.json next to greenshot.ini), also when it can't be read
    /// any more, e.g. after a Greenshot update changed what recipes may contain.
    /// </summary>
    public interface IRecipeCatalog
    {
        /// <summary>
        /// Recipes from the catalog are installed and the catalog may be used (recipes on, the administrator allows the catalog)
        /// </summary>
        bool CanCheckForUpdates { get; }

        /// <summary>
        /// The catalog on the website, with the recipes this Greenshot can use
        /// </summary>
        string CatalogUrl { get; }

        /// <summary>
        /// Compares the installed recipes from the catalog with the signed catalog index
        /// </summary>
        Task<RecipeCatalogCheck> CheckForUpdatesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Installs the version of the catalog, after the user approved it; the user is told when nothing was installed
        /// </summary>
        /// <returns>true when the recipe was updated</returns>
        Task<bool> UpdateAsync(string recipeId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// The result of <see cref="IRecipeCatalog.CheckForUpdatesAsync"/>
    /// </summary>
    public sealed class RecipeCatalogCheck
    {
        /// <summary>
        /// Why the catalog couldn't be read, null when it was
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// The installed recipes from the catalog which have a newer version, or which aren't loaded
        /// </summary>
        public IReadOnlyList<RecipeCatalogStatus> Recipes { get; set; } = new List<RecipeCatalogStatus>();
    }

    /// <summary>
    /// An installed recipe from the catalog compared with the catalog
    /// </summary>
    public sealed class RecipeCatalogStatus
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string FilePath { get; set; }

        /// <summary>
        /// The version that was installed, may be empty
        /// </summary>
        public string InstalledVersion { get; set; }

        /// <summary>
        /// The version in the catalog, null when the catalog doesn't have the recipe any more
        /// </summary>
        public string CatalogVersion { get; set; }

        /// <summary>
        /// The oldest Greenshot the catalog version works with
        /// </summary>
        public string MinGreenshot { get; set; }

        /// <summary>
        /// Greenshot uses the recipe; false when it couldn't be loaded (e.g. it no longer passes the checks of this Greenshot)
        /// </summary>
        public bool IsLoaded { get; set; }

        /// <summary>
        /// The catalog has another version than the installed one
        /// </summary>
        public bool HasUpdate { get; set; }

        /// <summary>
        /// The catalog version needs a newer Greenshot (<see cref="MinGreenshot"/>)
        /// </summary>
        public bool NeedsNewerGreenshot { get; set; }

        /// <summary>
        /// The catalog version can be installed here
        /// </summary>
        public bool CanUpdate => HasUpdate && !NeedsNewerGreenshot;
    }
}
