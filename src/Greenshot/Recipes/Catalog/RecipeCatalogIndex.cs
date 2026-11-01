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
using System.Text.Json.Serialization;

namespace Greenshot.Recipes.Catalog
{
    /// <summary>
    /// index.json of the recipe catalog (greenshot/greenshot-recipes): one entry per recipe, written by its tools/catalog.py and
    /// signed by the catalog's release workflow
    /// </summary>
    public sealed class RecipeCatalogIndex
    {
        public int IndexVersion { get; set; }

        /// <summary>
        /// owner/name of the catalog repository
        /// </summary>
        public string Repository { get; set; }

        /// <summary>
        /// The commit the download links point to
        /// </summary>
        public string Commit { get; set; }

        public string Generated { get; set; }

        public List<RecipeCatalogEntry> Recipes { get; set; } = new List<RecipeCatalogEntry>();
    }

    /// <summary>
    /// One recipe of the catalog
    /// </summary>
    public sealed class RecipeCatalogEntry
    {
        public string Id { get; set; }

        public string Version { get; set; }

        public string Name { get; set; }

        public string Summary { get; set; }

        /// <summary>
        /// "recipe" or "extension"
        /// </summary>
        public string Kind { get; set; }

        public List<RecipeCatalogAuthor> Authors { get; set; } = new List<RecipeCatalogAuthor>();

        public string License { get; set; }

        /// <summary>
        /// The oldest Greenshot version the recipe works with, e.g. 1.4.0
        /// </summary>
        public string MinGreenshot { get; set; }

        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// SHA-256 of the recipe file, hex
        /// </summary>
        public string Sha256 { get; set; }

        /// <summary>
        /// The recipe file at the commit of the index
        /// </summary>
        public string Url { get; set; }

        public string Readme { get; set; }
    }

    public sealed class RecipeCatalogAuthor
    {
        public string Name { get; set; }

        /// <summary>
        /// The GitHub login, may be empty
        /// </summary>
        public string Github { get; set; }
    }

    /// <summary>
    /// The JSON types of the catalog, generated at build time
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(RecipeCatalogIndex))]
    internal partial class RecipeCatalogJsonContext : JsonSerializerContext
    {
    }
}
