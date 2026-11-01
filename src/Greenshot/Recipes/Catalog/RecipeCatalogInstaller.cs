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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Http;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes;
using Greenshot.Base.Threading;
using Greenshot.Base.Wpf;
using Greenshot.Recipes.Approval;
using log4net;

namespace Greenshot.Recipes.Catalog
{
    /// <summary>
    /// Installs a recipe from the recipe catalog (greenshot:install-recipe?id=... on getgreenshot.org): the recipe must be in the signed
    /// index with the hash the index names, then the approval window shows it with its settings, and only what the user approves is
    /// saved, in Recipes\Catalog next to greenshot.ini. The catalog never skips the approval.
    /// </summary>
    public static class RecipeCatalogInstaller
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RecipeCatalogInstaller));

        /// <summary>
        /// The page of a recipe on the website
        /// </summary>
        public const string PageUrlFormat = "https://getgreenshot.org/recipes/{0}/?from=greenshot";

        /// <summary>
        /// Ids in the catalog: letters, digits, '_' and '-' (the folder name in the catalog repository)
        /// </summary>
        private static readonly Regex IdPattern = new Regex("^[A-Za-z0-9][A-Za-z0-9_-]{0,99}$", RegexOptions.CultureInvariant);

        /// <summary>
        /// One install at a time: a web page can't open window after window, a further link while one is open is dropped
        /// </summary>
        private static readonly SemaphoreSlim InstallLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Reads the catalog; replaceable for tests
        /// </summary>
        internal static Func<RecipeCatalogClient> ClientFactory { get; set; } = () => new RecipeCatalogClient(HttpService.Current, RecipeCatalogSignature.CatalogKeys);

        /// <summary>
        /// Shows the recipe and returns the user's decision; replaceable for tests
        /// </summary>
        internal static Func<RecipeApprovalRequest, CancellationToken, Task<ApprovalResult>> ApprovalPrompt { get; set; } = ShowApprovalAsync;

        /// <summary>
        /// Tells the user why nothing was installed; replaceable for tests
        /// </summary>
        internal static Func<string, CancellationToken, Task> ShowMessage { get; set; } = ShowMessageAsync;

        /// <summary>
        /// The directory of the recipes from the catalog; replaceable for tests
        /// </summary>
        internal static Func<string> DirectoryProvider { get; set; } = GetDefaultDirectory;

        /// <summary>
        /// The recipe settings (recipes on, catalog installs allowed); replaceable for tests
        /// </summary>
        internal static Func<IRecipeConfiguration> ConfigurationProvider { get; set; } = RecipeConfigHelper.TryGetRecipeConfiguration;

        /// <summary>
        /// The version of this Greenshot, compared with the minGreenshot of a recipe; replaceable for tests
        /// </summary>
        internal static Func<Version> GreenshotVersionProvider { get; set; } = () => typeof(EnvironmentInfo).Assembly.GetName().Version;

        public static string GetPageUrl(string recipeId) => string.Format(PageUrlFormat, Uri.EscapeDataString(recipeId ?? string.Empty));

        /// <summary>
        /// Installs (or updates) the recipe with this id from the catalog. Everything that goes wrong is told to the user.
        /// </summary>
        /// <param name="recipeId">The id of the recipe in the catalog</param>
        /// <param name="cancellationToken">CancellationToken</param>
        public static async Task<RecipeCatalogInstallOutcome> InstallAsync(string recipeId, CancellationToken cancellationToken = default)
        {
            string refusal = GetRefusal(recipeId);
            if (refusal != null)
            {
                Log.InfoFormat("Not installing '{0}' from the recipe catalog: {1}", recipeId, refusal);
                await ShowMessage(refusal, cancellationToken).ConfigureAwait(false);
                return RecipeCatalogInstallOutcome.Refused;
            }

            if (!await InstallLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            {
                Log.InfoFormat("Not installing '{0}' from the recipe catalog: another install is waiting for the user.", recipeId);
                return RecipeCatalogInstallOutcome.Busy;
            }
            try
            {
                return await InstallCoreAsync(recipeId, cancellationToken).ConfigureAwait(false);
            }
            catch (RecipeCatalogException ex)
            {
                Log.WarnFormat("Not installing '{0}' from the recipe catalog: {1}", recipeId, ex.Message);
                await ShowMessage(ex.Message, cancellationToken).ConfigureAwait(false);
                return RecipeCatalogInstallOutcome.Refused;
            }
            finally
            {
                InstallLock.Release();
            }
        }

        /// <summary>
        /// Why this Greenshot doesn't install the recipe, null when it may
        /// </summary>
        internal static string GetRefusal(string recipeId)
        {
            if (!RecipeManager.RecipeFilesSupported)
            {
                return Texts.Recipe.CatalogNotSupported;
            }
            var configuration = ConfigurationProvider();
            if (!RecipeConfigHelper.IsRecipeFeatureEnabled(configuration))
            {
                return Texts.Recipe.CatalogRecipesOff;
            }
            if (!configuration.AllowCatalogInstall)
            {
                return Texts.Recipe.CatalogNotAllowed;
            }
            if (string.IsNullOrEmpty(recipeId) || !IdPattern.IsMatch(recipeId))
            {
                return string.Format(Texts.Recipe.CatalogNotFound, recipeId);
            }
            return null;
        }

        private static async Task<RecipeCatalogInstallOutcome> InstallCoreAsync(string recipeId, CancellationToken cancellationToken)
        {
            var client = ClientFactory();
            var index = await client.GetIndexAsync(cancellationToken).ConfigureAwait(false);
            var entry = RecipeCatalogClient.Find(index, recipeId) ?? throw new RecipeCatalogException(string.Format(Texts.Recipe.CatalogNotFound, recipeId));
            if (TryParseVersion(entry.MinGreenshot, out var minimum) && GreenshotVersionProvider() is { } running && running < minimum)
            {
                throw new RecipeCatalogException(string.Format(Texts.Recipe.CatalogNeedsNewer, entry.Name ?? entry.Id, entry.MinGreenshot));
            }

            byte[] bytes = await client.GetRecipeFileAsync(entry, cancellationToken).ConfigureAwait(false);
            string content = RecipeManager.DecodeRecipeFile(bytes);
            string contentHash = RecipeTrustStore.ComputeSha256(bytes);
            var parsed = Parse(content, entry.Id);

            var recipeManager = RecipeManager.Instance;
            string filePath = Path.Combine(DirectoryProvider(), entry.Id + RecipeSerializer.RecipeFileExtension);
            var existingRecipe = recipeManager.GetRecipeById(entry.Id);
            var existingExtension = existingRecipe == null ? recipeManager.GetExtensionById(entry.Id) : null;
            bool exists = existingRecipe != null || existingExtension != null;
            string existingPath = existingRecipe?.FilePath ?? existingExtension?.FilePath;
            // An installed catalog recipe is updated, any other recipe with the id stays as it is
            if (exists && !string.Equals(existingPath, Path.GetFullPath(filePath), StringComparison.OrdinalIgnoreCase))
            {
                throw new RecipeCatalogException(string.Format(Texts.Recipe.CatalogIdTaken, entry.Id, existingPath ?? Texts.Recipe.ApprovalBuiltinPath));
            }
            var previousRecord = File.Exists(filePath) ? RecipeTrustStore.GetTrustRecord(filePath) : null;
            if (exists && previousRecord != null && string.Equals(previousRecord.Sha256Hash, contentHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new RecipeCatalogException(string.Format(Texts.Recipe.CatalogAlreadyInstalled, entry.Name ?? entry.Id, entry.Version));
            }

            var request = new RecipeApprovalRequest
            {
                Recipe = parsed.Recipe,
                Extension = parsed.Extension,
                ExtensionReach = parsed.Extension != null ? recipeManager.DescribeExtensionReach(parsed.Extension) : null,
                FilePath = filePath,
                Content = content,
                ContentHash = contentHash,
                Validation = parsed.Validation,
                PreviousRecord = previousRecord,
                PreviousContent = previousRecord?.ApprovedContent,
                CatalogEntry = entry,
                EditOptions = true
            };
            var decision = await ApprovalPrompt(request, cancellationToken).ConfigureAwait(false);
            if (decision?.Approval == null || !parsed.Validation.IsValid)
            {
                Log.InfoFormat("The recipe '{0}' {1} from the catalog was not installed.", entry.Id, entry.Version);
                return RecipeCatalogInstallOutcome.Rejected;
            }

            var approval = decision.Approval;
            approval.RecipeId = entry.Id;
            string error = await SaveAsync(recipeManager, filePath, bytes, content, contentHash, approval, entry, parsed.Definition, decision.OptionValues, cancellationToken).ConfigureAwait(false);
            if (error != null)
            {
                await ShowMessage(string.Format(Texts.Recipe.CatalogSaveFailed, error), cancellationToken).ConfigureAwait(false);
                return RecipeCatalogInstallOutcome.Failed;
            }
            Log.InfoFormat("Installed the recipe '{0}' {1} from the recipe catalog to '{2}'.", entry.Id, entry.Version, filePath);
            return RecipeCatalogInstallOutcome.Installed;
        }

        /// <summary>
        /// The recipe (or automatic step) of a catalog file: exactly the one with the id of its catalog entry
        /// </summary>
        internal static ParsedCatalogRecipe Parse(string content, string recipeId)
        {
            var parsed = new ParsedCatalogRecipe();
            try
            {
                if (RecipeManager.IsExtensionContent(content))
                {
                    var extension = RecipeSerializer.DeserializeExtension(content, validate: false);
                    parsed.Extension = extension;
                    parsed.Recipe = extension.AsRecipeView();
                    parsed.Definition = extension;
                    parsed.Validation = RecipeManager.Instance.ValidateExtension(extension);
                }
                else
                {
                    var recipes = RecipeSerializer.DeserializeList(content, validate: false);
                    if (recipes.Count == 1 && recipes[0] != null)
                    {
                        parsed.Recipe = recipes[0];
                        parsed.Definition = recipes[0];
                        parsed.Validation = RecipeValidator.Validate(recipes[0]);
                    }
                }
            }
            catch (Exception ex) when (ex is System.Text.Json.JsonException || ex is InvalidOperationException || ex is ArgumentException)
            {
                Log.Warn($"The catalog file of '{recipeId}' can't be read.", ex);
            }
            if (parsed.Definition == null || !string.Equals(parsed.Definition.Id, recipeId, StringComparison.Ordinal))
            {
                throw new RecipeCatalogException(string.Format(Texts.Recipe.CatalogWrongFile, recipeId));
            }
            // Where it comes from is decided by Greenshot, not by the file
            if (parsed.Extension != null)
            {
                parsed.Extension.FilePath = null;
                parsed.Extension.IsBuiltIn = false;
                parsed.Extension.IsOverridden = false;
                parsed.Extension.ProposedBy = null;
            }
            else
            {
                parsed.Recipe.FilePath = null;
                parsed.Recipe.IsBuiltIn = false;
                parsed.Recipe.IsOverridden = false;
                parsed.Recipe.ProposedBy = null;
            }
            return parsed;
        }

        internal sealed class ParsedCatalogRecipe
        {
            public CaptureRecipe Recipe { get; set; }

            public RecipeExtension Extension { get; set; }

            /// <summary>
            /// The recipe or the automatic step
            /// </summary>
            public FlowDefinition Definition { get; set; }

            public RecipeValidationResult Validation { get; set; }
        }

        /// <summary>
        /// Writes the approved bytes, records the approval for exactly them (origin catalog:id@version), stores the option values the user
        /// set and loads the recipe (on the UI thread, which registers the hotkeys)
        /// </summary>
        private static async Task<string> SaveAsync(RecipeManager recipeManager, string filePath, byte[] bytes, string content, string contentHash, RecipeApproval approval,
            RecipeCatalogEntry entry, FlowDefinition definition, IReadOnlyDictionary<string, object> optionValues, CancellationToken cancellationToken)
        {
            // Recorded before writing, so the change on disk is known as Greenshot's own and doesn't ask again
            RecipeTrustStore.RecordApproval(filePath, contentHash, approval, content, $"{RecipeTrustRecord.CatalogOriginPrefix}{entry.Id}@{entry.Version}", definition.Name, entry.Version);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllBytes(filePath, bytes);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Log.Error($"Could not save the recipe '{entry.Id}' from the catalog to '{filePath}'.", ex);
                return ex.Message;
            }

            // Values outside the file (greenshot.ini), so they don't change what was approved
            foreach (var option in (definition.Options ?? new List<RecipeOption>()).Where(o => o != null))
            {
                if (optionValues != null && optionValues.TryGetValue(option.Key, out var value))
                {
                    RecipeOptionStore.SetValue(entry.Id, option, value);
                }
            }

            // Loads what is on disk now: when the file changed since it was written, the hash doesn't match and nothing is loaded
            var result = await UiDispatcher.Current.InvokeAsync(() => recipeManager.LoadRecipeFromFile(filePath, interactiveApproval: false), cancellationToken).ConfigureAwait(false);
            return result.IsValid ? null : string.Join("; ", result.Errors);
        }

        internal static bool TryParseVersion(string text, out Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(text) || !Version.TryParse(text.Trim(), out var parsed))
            {
                return false;
            }
            // 1.4 and 1.4.0 are the same version, so are 1.4.0 and 1.4.0.0
            version = new Version(parsed.Major, parsed.Minor, Math.Max(parsed.Build, 0), Math.Max(parsed.Revision, 0));
            return true;
        }

        private static Task<ApprovalResult> ShowApprovalAsync(RecipeApprovalRequest request, CancellationToken cancellationToken)
        {
            return UiDispatcher.Current.InvokeAsync(() => RecipeManager.Instance.RequestInteractiveApprovalWithOptions(request), cancellationToken);
        }

        private static Task ShowMessageAsync(string text, CancellationToken cancellationToken)
        {
            return UiDispatcher.Current.InvokeAsync(() =>
            {
                ThemedMessageBox.Show(text, Texts.Recipe.CatalogMessageTitle, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }, cancellationToken);
        }

        private static string GetDefaultDirectory()
        {
            string configDirectory = null;
            try
            {
                string configLocation = GreenshotEnvironment.ConfigLocation;
                configDirectory = string.IsNullOrEmpty(configLocation) ? null : Path.GetDirectoryName(configLocation);
            }
            catch (Exception ex)
            {
                Log.Debug("Could not get the directory of greenshot.ini.", ex);
            }
            if (string.IsNullOrEmpty(configDirectory))
            {
                configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Greenshot");
            }
            return Path.Combine(configDirectory, "Recipes", "Catalog");
        }
    }

    /// <summary>
    /// How an install from the catalog ended
    /// </summary>
    public enum RecipeCatalogInstallOutcome
    {
        Installed,

        /// <summary>
        /// The user didn't approve it (or it isn't valid)
        /// </summary>
        Rejected,

        /// <summary>
        /// Not installed, the user was told why
        /// </summary>
        Refused,

        /// <summary>
        /// Another install was waiting for the user, nothing was shown
        /// </summary>
        Busy,

        /// <summary>
        /// Approved, but it couldn't be saved or loaded
        /// </summary>
        Failed
    }
}
