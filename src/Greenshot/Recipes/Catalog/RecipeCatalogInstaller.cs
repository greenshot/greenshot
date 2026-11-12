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
        /// Shows why the catalog can't be reached, true when the user wants to try again
        /// </summary>
        internal static Func<string, CancellationToken, Task<bool>> AskRetry { get; set; } = AskRetryAsync;

        /// <summary>
        /// The directory of the recipes from the catalog; replaceable for tests
        /// </summary>
        internal static Func<string> DirectoryProvider { get; set; } = GetDefaultDirectory;

        /// <summary>
        /// The recipe settings (recipes on, catalog installs allowed); replaceable for tests
        /// </summary>
        internal static Func<IRecipeConfiguration> ConfigurationProvider { get; set; } = RecipeConfigHelper.TryGetRecipeConfiguration;

        /// <summary>
        /// The version of this Greenshot, compared with the minGreenshot of a recipe; replaceable for tests. The file version (1.4.123,
        /// with the build number): the assembly version is only 1.4.0.0, so a recipe could not ask for a newer 1.4 build.
        /// </summary>
        internal static Func<Version> GreenshotVersionProvider { get; set; } = () =>
            TryParseVersion(EnvironmentInfo.GetVersion(typeof(EnvironmentInfo).Assembly, true), out var fileVersion)
                ? fileVersion
                : typeof(EnvironmentInfo).Assembly.GetName().Version;

        public static string GetPageUrl(string recipeId) => string.Format(PageUrlFormat, Uri.EscapeDataString(recipeId ?? string.Empty));

        /// <summary>
        /// The catalog on the website, with the recipes this Greenshot can use
        /// </summary>
        public static string GetCatalogUrl() => "https://getgreenshot.org/recipes/?from=greenshot&version=" + Uri.EscapeDataString(GreenshotVersionProvider()?.ToString(3) ?? string.Empty);

        /// <summary>
        /// "Get More Recipes" is offered to beta testers, when recipe files can be installed and the administrator allows the catalog
        /// </summary>
        public static bool CanGetMoreRecipes(ICoreConfiguration coreConfiguration) =>
            coreConfiguration?.IsBetaTester == true && GetRefusal("catalog") == null;

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
                while (true)
                {
                    try
                    {
                        return await InstallCoreAsync(recipeId, cancellationToken).ConfigureAwait(false);
                    }
                    catch (RecipeCatalogException ex) when (ex.CanRetry)
                    {
                        // The catalog may only be away for a moment (e.g. a timeout at GitHub): the user can try again without a restart
                        Log.WarnFormat("Couldn't reach the recipe catalog for '{0}': {1}", recipeId, ex.Message);
                        if (!await AskRetry(ex.Message, cancellationToken).ConfigureAwait(false))
                        {
                            return RecipeCatalogInstallOutcome.Refused;
                        }
                        Log.InfoFormat("Trying again to install '{0}' from the recipe catalog.", recipeId);
                    }
                }
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
            if (NeedsNewerGreenshot(entry))
            {
                throw new RecipeCatalogException(string.Format(Texts.Recipe.CatalogNeedsNewer, entry.Name ?? entry.Id, entry.MinGreenshot));
            }

            byte[] bytes = await client.GetRecipeFileAsync(entry, cancellationToken).ConfigureAwait(false);
            string content = RecipeManager.DecodeRecipeFile(bytes);
            string contentHash = RecipeTrustStore.ComputeSha256(bytes);
            Log.InfoFormat("Downloaded the recipe '{0}' {1} from the catalog ({2} bytes, SHA-256 {3}), checking it.", entry.Id, entry.Version, bytes.Length, contentHash);
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
            if (previousRecord != null && string.Equals(previousRecord.Sha256Hash, contentHash, StringComparison.OrdinalIgnoreCase))
            {
                if (exists)
                {
                    throw new RecipeCatalogException(string.Format(Texts.Recipe.CatalogAlreadyInstalled, entry.Name ?? entry.Id, entry.Version));
                }
                if (!parsed.Validation.IsValid)
                {
                    // Installed, but this Greenshot can't use it (e.g. after a Greenshot update), and the catalog has nothing newer yet
                    throw new RecipeCatalogException(string.Format(Texts.Recipe.CatalogNoNewerVersion, entry.Name ?? entry.Id, entry.Version));
                }
            }
            if (!parsed.Validation.IsValid)
            {
                // The catalog version itself doesn't pass the checks of this Greenshot: say so, its errors would look like the old ones
                Log.WarnFormat("The recipe '{0}' {1} from the catalog doesn't work with this Greenshot: {2}", entry.Id, entry.Version, string.Join("; ", parsed.Validation.Errors));
                throw new RecipeCatalogException(string.Format(Texts.Recipe.CatalogVersionInvalid, entry.Name ?? entry.Id, entry.Version) +
                                                 "\n\n" + string.Join("\n", parsed.Validation.Errors.Select(e => "- " + e)));
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
        /// The catalog version needs a newer Greenshot than this one
        /// </summary>
        private static bool NeedsNewerGreenshot(RecipeCatalogEntry entry) =>
            TryParseVersion(entry?.MinGreenshot, out var minimum) && GreenshotVersionProvider() is { } running && running < minimum;

        /// <summary>
        /// The recipe from the catalog in this file: a file Recipes\Catalog\&lt;id&gt;.gsrecipe.json which Greenshot uses (it has an
        /// approval record). Found by its place, so also when the file can't be read any more. Null for any other file.
        /// </summary>
        internal static InstalledCatalogRecipe GetInstalled(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !RecipeManager.RecipeFilesSupported)
            {
                return null;
            }
            string fullPath;
            string directory;
            try
            {
                fullPath = Path.GetFullPath(filePath);
                directory = Path.GetFullPath(DirectoryProvider()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is IOException || ex is System.Security.SecurityException)
            {
                return null;
            }
            string fileName = Path.GetFileName(fullPath);
            if (!string.Equals(Path.GetDirectoryName(fullPath), directory, StringComparison.OrdinalIgnoreCase) ||
                !fileName.EndsWith(RecipeSerializer.RecipeFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            string id = fileName.Substring(0, fileName.Length - RecipeSerializer.RecipeFileExtension.Length);
            var record = IdPattern.IsMatch(id) ? RecipeTrustStore.GetTrustRecord(fullPath) : null;
            if (record == null)
            {
                return null;
            }
            // catalog:id@version, written at the install; a later approval of a change made outside Greenshot only knows the version of the file
            string originPrefix = RecipeTrustRecord.CatalogOriginPrefix + id + "@";
            string version = record.Origin != null && record.Origin.StartsWith(originPrefix, StringComparison.Ordinal)
                ? record.Origin.Substring(originPrefix.Length)
                : record.RecipeVersion;
            return new InstalledCatalogRecipe
            {
                Id = id,
                Name = record.RecipeName,
                Version = version,
                FilePath = fullPath,
                ApprovedSha256 = record.Sha256Hash
            };
        }

        /// <summary>
        /// The recipes from the catalog which are installed
        /// </summary>
        internal static IReadOnlyList<InstalledCatalogRecipe> GetInstalledRecipes()
        {
            var installed = new List<InstalledCatalogRecipe>();
            if (!RecipeManager.RecipeFilesSupported)
            {
                return installed;
            }
            try
            {
                string directory = DirectoryProvider();
                if (!Directory.Exists(directory))
                {
                    return installed;
                }
                foreach (var file in Directory.GetFiles(directory, "*" + RecipeSerializer.RecipeFileExtension))
                {
                    var recipe = GetInstalled(file);
                    if (recipe != null)
                    {
                        installed.Add(recipe);
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Log.Warn("Couldn't list the recipes from the catalog.", ex);
            }
            return installed;
        }

        /// <summary>
        /// The id of the recipe from the catalog in this file when it can be updated from the catalog, null otherwise
        /// </summary>
        public static string GetUpdatableId(string filePath)
        {
            var installed = GetInstalled(filePath);
            return installed != null && GetRefusal(installed.Id) == null ? installed.Id : null;
        }

        /// <summary>
        /// Recipes from the catalog are installed, and the catalog may be used
        /// </summary>
        public static bool CanCheckForUpdates() => GetRefusal("catalog") == null && GetInstalledRecipes().Count > 0;

        /// <summary>
        /// The installed recipes from the catalog which have another version in the signed index, or which Greenshot doesn't use
        /// because they can't be loaded. An older version in the catalog is not offered.
        /// </summary>
        public static async Task<RecipeCatalogCheck> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
        {
            var check = new RecipeCatalogCheck();
            string refusal = GetRefusal("catalog");
            if (refusal != null)
            {
                check.Error = refusal;
                return check;
            }
            var installed = GetInstalledRecipes();
            if (installed.Count == 0)
            {
                return check;
            }
            RecipeCatalogIndex index;
            try
            {
                index = await ClientFactory().GetIndexAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (RecipeCatalogException ex)
            {
                Log.WarnFormat("Couldn't check the recipes from the catalog for updates: {0}", ex.Message);
                check.Error = ex.Message;
                return check;
            }

            var recipeManager = RecipeManager.Instance;
            var recipes = new List<RecipeCatalogStatus>();
            foreach (var recipe in installed)
            {
                var entry = RecipeCatalogClient.Find(index, recipe.Id);
                string loadedPath = recipeManager.GetRecipeById(recipe.Id)?.FilePath ?? recipeManager.GetExtensionById(recipe.Id)?.FilePath;
                bool isLoaded = string.Equals(loadedPath, recipe.FilePath, StringComparison.OrdinalIgnoreCase);
                bool isOlder = TryParseVersion(entry?.Version, out var catalogVersion) && TryParseVersion(recipe.Version, out var installedVersion) && catalogVersion < installedVersion;
                bool hasUpdate = entry != null && !isOlder && !string.Equals(entry.Sha256, recipe.ApprovedSha256, StringComparison.OrdinalIgnoreCase);
                if (!hasUpdate && isLoaded)
                {
                    continue;
                }
                recipes.Add(new RecipeCatalogStatus
                {
                    Id = recipe.Id,
                    Name = entry?.Name ?? recipe.Name ?? recipe.Id,
                    FilePath = recipe.FilePath,
                    InstalledVersion = recipe.Version,
                    CatalogVersion = entry?.Version,
                    MinGreenshot = entry?.MinGreenshot,
                    IsLoaded = isLoaded,
                    HasUpdate = hasUpdate,
                    NeedsNewerGreenshot = hasUpdate && NeedsNewerGreenshot(entry)
                });
            }
            Log.InfoFormat("Checked {0} recipe(s) from the catalog: {1} with another version, {2} not loaded.", installed.Count, recipes.Count(r => r.HasUpdate), recipes.Count(r => !r.IsLoaded));
            check.Recipes = recipes;
            return check;
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

        /// <summary>
        /// A recipe from the catalog, as installed
        /// </summary>
        internal sealed class InstalledCatalogRecipe
        {
            public string Id { get; set; }

            public string Name { get; set; }

            /// <summary>
            /// The installed version, may be empty
            /// </summary>
            public string Version { get; set; }

            public string FilePath { get; set; }

            /// <summary>
            /// The hash of the approved content
            /// </summary>
            public string ApprovedSha256 { get; set; }
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

        private static Task<bool> AskRetryAsync(string text, CancellationToken cancellationToken)
        {
            return UiDispatcher.Current.InvokeAsync(() =>
                ThemedMessageBox.ShowChoice(IntPtr.Zero, Texts.Recipe.CatalogMessageTitle, text, System.Windows.MessageBoxImage.Warning,
                    new[] { Texts.Recipe.PickerRetry, Texts.Core.Cancel }, defaultIndex: 0, cancelIndex: 1) == 0, cancellationToken);
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
