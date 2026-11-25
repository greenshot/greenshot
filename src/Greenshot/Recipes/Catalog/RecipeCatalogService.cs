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


using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Recipes;

namespace Greenshot.Recipes.Catalog
{
    /// <summary>
    /// The recipe catalog for the plugins (the recipe manager of the Recipe Editor plugin)
    /// </summary>
    public sealed class RecipeCatalogService : IRecipeCatalog
    {
        public bool CanCheckForUpdates => RecipeCatalogInstaller.CanCheckForUpdates();

        public string CatalogUrl => RecipeCatalogInstaller.GetCatalogUrl();

        public Task<RecipeCatalogCheck> CheckForUpdatesAsync(CancellationToken cancellationToken = default) => RecipeCatalogInstaller.CheckForUpdatesAsync(cancellationToken);

        public async Task<bool> UpdateAsync(string recipeId, CancellationToken cancellationToken = default) =>
            await RecipeCatalogInstaller.InstallAsync(recipeId, cancellationToken).ConfigureAwait(false) == RecipeCatalogInstallOutcome.Installed;
    }
}
