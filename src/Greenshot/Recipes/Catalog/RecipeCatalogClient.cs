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
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Http;
using Greenshot.Base.Languages;
using Greenshot.Recipes.Approval;
using log4net;

namespace Greenshot.Recipes.Catalog
{
    /// <summary>
    /// Reads the recipe catalog: the latest index.json of greenshot/greenshot-recipes with its signature, and a recipe file listed in it.
    /// Only a recipe in a correctly signed index, with the SHA-256 the index names, comes out of here.
    /// </summary>
    public sealed class RecipeCatalogClient
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RecipeCatalogClient));

        /// <summary>
        /// The index of the latest catalog release; GitHub redirects to the file of that release
        /// </summary>
        public const string IndexUrl = "https://github.com/greenshot/greenshot-recipes/releases/latest/download/index.json";

        public const string SignatureUrl = IndexUrl + ".sig";

        /// <summary>
        /// Recipe files are only downloaded from the catalog repository
        /// </summary>
        public const string RecipeUrlPrefix = "https://raw.githubusercontent.com/greenshot/greenshot-recipes/";

        /// <summary>
        /// The largest index and recipe file read
        /// </summary>
        public const int MaxFileLength = 1024 * 1024;

        private readonly HttpService _httpService;
        private readonly IReadOnlyList<string> _publicKeys;

        public RecipeCatalogClient(HttpService httpService, IReadOnlyList<string> publicKeys)
        {
            _httpService = httpService ?? throw new ArgumentNullException(nameof(httpService));
            _publicKeys = publicKeys ?? throw new ArgumentNullException(nameof(publicKeys));
        }

        /// <summary>
        /// The latest signed index
        /// </summary>
        /// <exception cref="RecipeCatalogException">the index can't be read or its signature is wrong</exception>
        public async Task<RecipeCatalogIndex> GetIndexAsync(CancellationToken cancellationToken)
        {
            // A new release between the two downloads gives an index and a signature which don't belong together: read both again once
            for (int attempt = 1; ; attempt++)
            {
                byte[] index = await DownloadAsync(IndexUrl, cancellationToken).ConfigureAwait(false);
                byte[] signature = await DownloadAsync(SignatureUrl, cancellationToken).ConfigureAwait(false);
                if (RecipeCatalogSignature.Verify(index, signature, _publicKeys))
                {
                    return ParseIndex(index);
                }
                if (attempt == 2)
                {
                    Log.WarnFormat("The signature of the recipe catalog index ({0} bytes) is not from a key Greenshot trusts.", index.Length);
                    throw new RecipeCatalogException(Texts.Recipe.CatalogNotSigned);
                }
            }
        }

        internal static RecipeCatalogIndex ParseIndex(byte[] index)
        {
            try
            {
                var parsed = JsonSerializer.Deserialize(index, RecipeCatalogJsonContext.Default.RecipeCatalogIndex);
                if (parsed?.Recipes == null || parsed.IndexVersion != 1)
                {
                    throw new RecipeCatalogException(Texts.Recipe.CatalogInvalid);
                }
                return parsed;
            }
            catch (JsonException ex)
            {
                Log.Warn("The recipe catalog index can't be read.", ex);
                throw new RecipeCatalogException(Texts.Recipe.CatalogInvalid);
            }
        }

        /// <summary>
        /// The recipe file of the entry, exactly the bytes the index names
        /// </summary>
        /// <exception cref="RecipeCatalogException">the file can't be read or isn't the one of the index</exception>
        public async Task<byte[]> GetRecipeFileAsync(RecipeCatalogEntry entry, CancellationToken cancellationToken)
        {
            if (entry?.Url == null || !entry.Url.StartsWith(RecipeUrlPrefix, StringComparison.Ordinal))
            {
                throw new RecipeCatalogException(string.Format(Texts.Recipe.CatalogWrongFile, entry?.Id));
            }
            byte[] bytes = await DownloadAsync(entry.Url, cancellationToken).ConfigureAwait(false);
            string hash = RecipeTrustStore.ComputeSha256(bytes);
            if (!string.Equals(hash, entry.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                Log.WarnFormat("The recipe '{0}' from {1} has the SHA-256 {2}, the catalog index names {3}.", entry.Id, entry.Url, hash, entry.Sha256);
                throw new RecipeCatalogException(string.Format(Texts.Recipe.CatalogWrongFile, entry.Id));
            }
            return bytes;
        }

        /// <summary>
        /// The entry of the recipe, null when the catalog doesn't have it
        /// </summary>
        public static RecipeCatalogEntry Find(RecipeCatalogIndex index, string recipeId)
        {
            return index?.Recipes?.FirstOrDefault(r => r != null && string.Equals(r.Id, recipeId, StringComparison.Ordinal));
        }

        private async Task<byte[]> DownloadAsync(string url, CancellationToken cancellationToken)
        {
            byte[] bytes;
            try
            {
                bytes = await _httpService.GetBytesAsync(url, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TimeoutException)
            {
                Log.Warn($"Couldn't download {url} from the recipe catalog.", ex);
                throw new RecipeCatalogException(string.Format(Texts.Recipe.CatalogUnreachable, ex.Message));
            }
            if (bytes.Length > MaxFileLength)
            {
                throw new RecipeCatalogException(Texts.Recipe.CatalogInvalid);
            }
            return bytes;
        }
    }

    /// <summary>
    /// The recipe catalog can't be used, the message is for the user
    /// </summary>
    public sealed class RecipeCatalogException : Exception
    {
        public RecipeCatalogException(string message) : base(message)
        {
        }
    }
}
