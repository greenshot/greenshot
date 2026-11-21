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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Recipes;
using Greenshot.Base.Threading;
using Greenshot.Helpers;
using Greenshot.Recipes.Approval;
using Greenshot.Recipes.Views;

namespace Greenshot.Recipes
{
    /// <summary>
    /// Recipe files: loaded from the configuration, approved by the user (SHA-256 trust pinning in the trust store), watched for
    /// changes and saved by the recipe editor. Greenshot Light has only the built-in recipes and leaves this file out.
    /// </summary>
    public partial class RecipeManager
    {
        private readonly Dictionary<string, FileSystemWatcher> _fileWatchers = new Dictionary<string, FileSystemWatcher>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Recipe files changed on disk which wait to be checked (debounced: editors write files in several steps)
        /// </summary>
        private readonly Dictionary<string, Timer> _changedFileTimers = new Dictionary<string, Timer>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The content (hash) of a changed file the user was already asked about, not asked again
        /// </summary>
        private readonly Dictionary<string, string> _askedForChangedFile = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Wait after the last change of a file before asking, ms
        /// </summary>
        private const int ChangedFileDelay = 1500;

        partial void AddRecipeFileToConfig(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;
            // Only a file which is used is watched, not one which wasn't approved or valid
            SetupWatcherForFile(filePath);
            try
            {
                var recipeConfig = RecipeConfigHelper.TryGetRecipeConfiguration();
                if (recipeConfig == null) return;

                string existing = recipeConfig.RecipeFiles ?? "";
                var currentPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var configuredPaths = new List<string>();

                foreach (string p in existing.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        string norm = Path.GetFullPath(p.Trim());
                        if (currentPaths.Add(norm))
                        {
                            configuredPaths.Add(norm);
                        }
                    }
                    catch { }
                }

                string targetNorm = Path.GetFullPath(filePath);
                if (currentPaths.Add(targetNorm))
                {
                    configuredPaths.Add(targetNorm);
                    recipeConfig.RecipeFiles = string.Join(";", configuredPaths);
                    IniConfigRegistry.Get()?.Save();
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to update configured recipe files for '{filePath}'", ex);
            }
        }

        /// <summary>
        /// Greenshot stops loading the recipe file (e.g. one which fails the checks): its recipes go, the file itself stays
        /// </summary>
        public void RemoveRecipeFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;
            List<string> recipeIds;
            lock (_recipes)
            {
                recipeIds = _recipes.Values.Where(recipe => IsSameFile(recipe.FilePath, filePath)).Select(recipe => recipe.Id).ToList();
            }

            foreach (string recipeId in recipeIds)
            {
                UnregisterRecipe(recipeId);
            }

            RemoveRecipeFileFromConfig(filePath);
            Log.InfoFormat("Removed the recipe file '{0}' from Greenshot, the file stays", filePath);
            NotifyRecipesChanged();
        }

        private static bool IsSameFile(string first, string second)
        {
            if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second)) return false;
            try
            {
                return string.Equals(Path.GetFullPath(first), Path.GetFullPath(second), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return false;
            }
        }

        partial void RemoveRecipeFileFromConfig(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;
            try
            {
                var recipeConfig = RecipeConfigHelper.TryGetRecipeConfiguration();
                if (recipeConfig == null) return;

                string existing = recipeConfig.RecipeFiles ?? "";
                string targetNorm = Path.GetFullPath(filePath);
                var configuredPaths = new List<string>();

                foreach (string p in existing.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        string norm = Path.GetFullPath(p.Trim());
                        if (!string.Equals(norm, targetNorm, StringComparison.OrdinalIgnoreCase))
                        {
                            configuredPaths.Add(norm);
                        }
                    }
                    catch { }
                }

                recipeConfig.RecipeFiles = string.Join(";", configuredPaths);
                IniConfigRegistry.Get()?.Save();
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to remove recipe file from config: '{filePath}'", ex);
            }
        }

        /// <summary>
        /// True while ReloadRecipes loads the configured files: it notifies once for all of them
        /// </summary>
        private bool _loadingConfiguredFiles;

        /// <summary>
        /// Loads the recipe files of the configuration
        /// </summary>
        /// <param name="interactive">false: no window is shown, a file which needs the user (approval, errors) is only added to <paramref name="needsUser"/></param>
        /// <param name="needsUser">Collects the files which couldn't be loaded without the user</param>
        partial void LoadConfiguredRecipeFiles(bool interactive, List<string> needsUser)
        {
            _loadingConfiguredFiles = true;
            try
            {
                LoadConfiguredRecipeFilesCore(interactive, needsUser);
            }
            finally
            {
                _loadingConfiguredFiles = false;
            }
        }

        private void LoadConfiguredRecipeFilesCore(bool interactive, List<string> needsUser)
        {
            if (!RecipeConfigHelper.IsRecipeFeatureEnabled())
            {
                Log.Debug("Recipe feature is disabled. Skipping external recipe file loading.");
                return;
            }

            var recipeConfig = RecipeConfigHelper.TryGetRecipeConfiguration();
            string configured = recipeConfig?.RecipeFiles;
            if (string.IsNullOrWhiteSpace(configured)) return;

            var paths = configured.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawPath in paths)
            {
                var trimmed = rawPath.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                try
                {
                    string fullPath = Path.GetFullPath(trimmed);
                    if (File.Exists(fullPath))
                    {
                        var result = LoadRecipeFromFile(fullPath, interactive);
                        if (!result.IsValid)
                        {
                            Log.WarnFormat("Validation failed for configured recipe file '{0}': {1}", fullPath, string.Join("; ", result.Errors));
                            if (!interactive)
                            {
                                needsUser.Add(fullPath);
                            }
                        }
                    }
                    else
                    {
                        Log.WarnFormat("Configured recipe file '{0}' was not found.", fullPath);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error loading configured recipe file '{trimmed}'", ex);
                }
            }
        }

        private void SetupWatcherForFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                string dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;

                lock (_fileWatchers)
                {
                    if (!_fileWatchers.ContainsKey(dir))
                    {
                        var watcher = new FileSystemWatcher(dir, "*.gsrecipe.json")
                        {
                            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                            EnableRaisingEvents = true
                        };

                        FileSystemEventHandler handler = (s, e) =>
                        {
                            Log.InfoFormat("Detected change on disk for recipe file '{0}' ({1}).", e.FullPath, e.ChangeType);
                            ScheduleChangedFileCheck(e.FullPath);
                        };

                        watcher.Changed += handler;
                        watcher.Created += handler;
                        // Editors which save to a temporary file and rename it to the recipe file
                        watcher.Renamed += (s, e) =>
                        {
                            Log.InfoFormat("Detected recipe file rename on disk: '{0}' -> '{1}'.", e.OldFullPath, e.FullPath);
                            ScheduleChangedFileCheck(e.FullPath);
                        };

                        _fileWatchers[dir] = watcher;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to initialize FileSystemWatcher for '{filePath}'", ex);
            }
        }

        /// <summary>
        /// A recipe file changed on disk: check it shortly after the last change
        /// </summary>
        private void ScheduleChangedFileCheck(string filePath)
        {
            lock (_changedFileTimers)
            {
                if (_changedFileTimers.TryGetValue(filePath, out var timer))
                {
                    timer.Change(ChangedFileDelay, Timeout.Infinite);
                    return;
                }
                _changedFileTimers[filePath] = new Timer(_ =>
                {
                    lock (_changedFileTimers)
                    {
                        if (_changedFileTimers.TryGetValue(filePath, out var done))
                        {
                            done.Dispose();
                            _changedFileTimers.Remove(filePath);
                        }
                    }
                    // On the UI thread, like every other change of the recipes: it can change the triggers (hotkeys, clipboard listener)
                    UiDispatcher.Current.InvokeAsync(() => CheckChangedFile(filePath)).FireAndLog("Check a changed recipe file");
                }, null, ChangedFileDelay, Timeout.Infinite);
            }
        }

        /// <summary>
        /// A recipe file Greenshot uses was changed outside Greenshot (Greenshot's own saves are approved before they are written):
        /// ask now, not when the recipe runs the next time.
        /// </summary>
        private void CheckChangedFile(string filePath)
        {
            try
            {
                string fullPath = Path.GetFullPath(filePath);
                bool inUse;
                lock (_recipes)
                {
                    inUse = _recipes.Values.Any(r => !string.IsNullOrEmpty(r.FilePath) && string.Equals(Path.GetFullPath(r.FilePath), fullPath, StringComparison.OrdinalIgnoreCase));
                }
                var extensionsOfFile = GetExtensionsOfFile(fullPath);
                inUse |= extensionsOfFile.Count > 0;
                if (!inUse || !File.Exists(fullPath) || RecipeTrustStore.IsRecipeApproved(fullPath, out string currentHash, out _))
                {
                    return;
                }
                lock (_askedForChangedFile)
                {
                    if (_askedForChangedFile.TryGetValue(fullPath, out var askedHash) && string.Equals(askedHash, currentHash, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                    _askedForChangedFile[fullPath] = currentHash;
                }
                // An automatic step changes other recipes: it is not used until the change is approved
                foreach (var extension in extensionsOfFile)
                {
                    DropExtension(extension.Id);
                }
                Log.InfoFormat("Recipe file '{0}' was changed outside Greenshot, asking for approval.", fullPath);
                LoadRecipeFromFile(fullPath, interactiveApproval: true, forceApprovalPrompt: false);
            }
            catch (Exception ex)
            {
                Log.Warn($"Could not check the changed recipe file '{filePath}'.", ex);
            }
        }

        public RecipeValidationResult SaveRecipeToFile(CaptureRecipe recipe, string filePath)
        {
            var result = new RecipeValidationResult();
            if (recipe == null || string.IsNullOrWhiteSpace(filePath))
            {
                result.AddError("No recipe or file to save to.");
                return result;
            }

            string fullPath = Path.GetFullPath(filePath);
            recipe.FilePath = fullPath;
            // The bytes which are approved are the bytes which are written
            string content = RecipeSerializer.Serialize(recipe);
            byte[] bytes = new UTF8Encoding(false).GetBytes(content);
            string contentHash = RecipeTrustStore.ComputeSha256(bytes);

            var validation = RecipeValidator.Validate(recipe);
            RecipeApproval approval = null;
            if (validation.IsValid)
            {
                approval = CreateEditApproval(recipe, fullPath, validation, out var decision, out string approvedContent);
                if (decision.IsNeeded)
                {
                    Log.InfoFormat("The change of recipe '{0}' needs the user's decision: {1}", recipe.Id, string.Join(" ", decision.Reasons));
                    var request = CreateApprovalRequest(recipe, fullPath, content, contentHash, validation);
                    // The user is the author: no "switched off" start like for a file an AI tool wrote
                    request.StartSwitchedOff = false;
                    request.OwnEditReasons = decision.Reasons;
                    request.SuggestedApproval = new RecipeApproval { RecipeId = recipe.Id, ApprovedTriggers = approval.ApprovedTriggers.Concat(decision.TriggerKeys).ToList() };
                    if (request.PreviousContent == null && !string.IsNullOrEmpty(approvedContent))
                    {
                        request.PreviousContent = approvedContent;
                    }
                    approval = RequestInteractiveApproval(request);
                    if (approval == null)
                    {
                        result.AddError("The recipe was not saved, its changes were not approved.");
                        return result;
                    }
                }
                approval.RecipeId = recipe.Id;
                // Recorded before writing, so the change on disk is known as Greenshot's own
                RecipeTrustStore.RecordApproval(fullPath, contentHash, approval, content, recipeName: recipe.Name, recipeVersion: recipe.Version);
            }

            try
            {
                string directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllBytes(fullPath, bytes);
            }
            catch (Exception ex)
            {
                Log.Error($"Could not save the recipe '{recipe.Id}' to '{fullPath}'.", ex);
                result.AddError($"Could not save the recipe: {ex.Message}");
                return result;
            }

            // Without an approval (an invalid recipe, saved as work in progress) none of its triggers is approved until it is loaded again
            RecipeApprovalPolicy.Apply(recipe, approval);
            recipe.ProposedBy = null;
            lock (_recipes)
            {
                bool isBuiltIn = _builtInRecipes.ContainsKey(recipe.Id);
                recipe.IsBuiltIn = isBuiltIn;
                recipe.IsOverridden = isBuiltIn;
            }
            RegisterRecipe(recipe);
            foreach (var warning in validation.Warnings) result.AddWarning(warning);
            if (!validation.IsValid)
            {
                // Saved as work in progress: it is approved when it is loaded the next time
                foreach (var error in validation.Errors) result.AddWarning(error);
            }
            return result;
        }

        public IReadOnlyList<string> GetSaveDecisionReasons(CaptureRecipe recipe, string filePath)
        {
            if (recipe == null)
            {
                return Array.Empty<string>();
            }
            var validation = RecipeValidator.Validate(recipe);
            if (!validation.IsValid)
            {
                return Array.Empty<string>();
            }
            string fullPath = string.IsNullOrWhiteSpace(filePath) ? null : Path.GetFullPath(filePath);
            CreateEditApproval(recipe, fullPath, validation, out var decision, out _);
            return decision.Reasons;
        }

        /// <summary>
        /// The approval of a recipe saved in the recipe editor, and what the user has to decide about it
        /// </summary>
        private RecipeApproval CreateEditApproval(CaptureRecipe recipe, string fullPath, RecipeValidationResult validation, out RecipeEditDecision decision, out string approvedContent)
        {
            var record = fullPath == null ? null : RecipeTrustStore.GetTrustRecord(fullPath);
            var previousApproval = record?.GetApproval(recipe.Id);
            approvedContent = GetApprovedContent(record, fullPath);
            var approvedVersion = RecipeEditApproval.FindRecipe(approvedContent, recipe.Id);
            return RecipeEditApproval.Create(recipe, validation, approvedVersion, previousApproval, GetBuiltInRecipe(recipe.Id) != null, out decision);
        }

        /// <summary>
        /// The approved content of a recipe file: from its trust record, or, for a record from before the content was kept, the file
        /// on disk when it is still the approved one. Null when it isn't known.
        /// </summary>
        internal static string GetApprovedContent(RecipeTrustRecord record, string fullPath)
        {
            if (record == null)
            {
                return null;
            }
            if (!string.IsNullOrEmpty(record.ApprovedContent))
            {
                return record.ApprovedContent;
            }
            try
            {
                if (!File.Exists(fullPath))
                {
                    return null;
                }
                byte[] bytes = File.ReadAllBytes(fullPath);
                return string.Equals(RecipeTrustStore.ComputeSha256(bytes), record.Sha256Hash, StringComparison.OrdinalIgnoreCase) ? DecodeRecipeFile(bytes) : null;
            }
            catch (Exception ex)
            {
                Log.Warn($"Could not read the approved version of '{fullPath}'.", ex);
                return null;
            }
        }

        public RecipeValidationResult LoadRecipeFromFile(string filePath)
        {
            return LoadRecipeFromFile(filePath, interactiveApproval: true, forceApprovalPrompt: false);
        }

        public RecipeValidationResult LoadRecipeFromFile(string filePath, bool interactiveApproval)
        {
            return LoadRecipeFromFile(filePath, interactiveApproval, forceApprovalPrompt: false);
        }

        public RecipeValidationResult LoadRecipeFromFile(string filePath, bool interactiveApproval, bool forceApprovalPrompt)
        {
            var overallResult = new RecipeValidationResult();
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                overallResult.AddError($"File not found: {filePath}");
                return overallResult;
            }

            try
            {
                // Read the file once: what is parsed, shown and approved are the same bytes (the file can change in between)
                byte[] bytes = File.ReadAllBytes(filePath);
                string content = DecodeRecipeFile(bytes);
                string contentHash = RecipeTrustStore.ComputeSha256(bytes);
                Log.InfoFormat("Loading the recipe file '{0}' ({1} bytes, SHA-256 {2}).", filePath, bytes.Length, contentHash);
                if (IsExtensionContent(content))
                {
                    // An automatic step: approved and used like a recipe of the file, but it changes other recipes
                    return LoadExtensionFromContent(filePath, content, contentHash, interactiveApproval, forceApprovalPrompt);
                }
                var recipes = RecipeSerializer.DeserializeList(content, validate: false);
                bool anyChanged = false;

                foreach (var recipe in recipes)
                {
                    recipe.FilePath = filePath;
                    var valResult = RecipeValidator.Validate(recipe);
                    if (!valResult.IsValid)
                    {
                        foreach (var err in valResult.Errors) overallResult.AddError($"[{recipe.Id ?? "unknown"}]: {err}");
                        Log.WarnFormat("The recipe '{0}' {1} from '{2}' fails the checks: {3}", recipe.Id, recipe.Version, filePath, string.Join("; ", valResult.Errors));
                        if (interactiveApproval)
                        {
                            RecipeApprovalWindow.ShowValidationError(filePath, valResult, recipe);
                        }
                        continue;
                    }

                    // Security check: SHA-256 trust pinning, per recipe of the file
                    var approval = forceApprovalPrompt ? null : RecipeTrustStore.GetApproval(filePath, contentHash, recipe.Id);
                    if (approval == null)
                    {
                        if (interactiveApproval)
                        {
                            var decision = RequestInteractiveApprovalWithOptions(CreateApprovalRequest(recipe, filePath, content, contentHash, valResult));
                            approval = decision?.Approval;
                            if (approval != null)
                            {
                                RecipeTrustStore.RecordApproval(filePath, contentHash, approval, content, recipeName: recipe.Name, recipeVersion: recipe.Version);
                            }
                            else if (decision?.IsRevoked == true)
                            {
                                RevokeApproval(recipe.Id, filePath);
                                overallResult.AddError($"The approval of recipe '{recipe.Name}' ({recipe.Id}) was revoked.");
                                continue;
                            }
                            else if (forceApprovalPrompt && (approval = RecipeTrustStore.GetApproval(filePath, contentHash, recipe.Id)) != null)
                            {
                                // A review which was closed or kept as is changes nothing
                                Log.InfoFormat("The review of recipe '{0}' was closed, its approval stays as it was.", recipe.Id);
                            }
                            else
                            {
                                overallResult.AddError($"User rejected recipe '{recipe.Name}' ({recipe.Id}) from '{filePath}'.");
                                continue;
                            }
                        }
                        else
                        {
                            overallResult.AddError($"Recipe '{recipe.Name}' from '{filePath}' is not approved and interactive approval is disabled.");
                            continue;
                        }
                    }

                    var missingGates = RecipeApprovalPolicy.GetMissingGates(valResult, approval);
                    if (missingGates.Count > 0)
                    {
                        overallResult.AddError($"Recipe '{recipe.Name}' needs {string.Join(", ", missingGates.Select(RecipeApprovalPolicy.GetGateName))}, but that was not allowed.");
                        continue;
                    }

                    foreach (var warn in valResult.Warnings) overallResult.AddWarning($"[{recipe.Id}]: {warn}");

                    // Only the approved triggers can start the recipe
                    RecipeApprovalPolicy.Apply(recipe, approval);
                    recipe.FilePath = Path.GetFullPath(filePath);
                    var trustRecord = RecipeTrustStore.GetTrustRecord(filePath);
                    recipe.ProposedBy = trustRecord?.IsAiCreated == true ? trustRecord.Origin.Substring(RecipeTrustRecord.AiOriginPrefix.Length) : null;
                    recipe.IsEnabled = !GetDisabledRecipeIds().Contains(recipe.Id);

                    lock (_recipes)
                    {
                        if (_builtInRecipes.ContainsKey(recipe.Id))
                        {
                            recipe.IsBuiltIn = true;
                            recipe.IsOverridden = true;
                            _recipes[recipe.Id] = recipe;
                            Log.InfoFormat("Overrode built-in recipe '{0}' with custom configuration from '{1}'", recipe.Id, filePath);
                        }
                        else
                        {
                            recipe.IsBuiltIn = false;
                            recipe.IsOverridden = false;
                            _recipes[recipe.Id] = recipe;
                            Log.InfoFormat("Registered custom recipe '{0}' from '{1}'", recipe.Id, filePath);
                        }
                        anyChanged = true;
                    }

                    AddRecipeFileToConfig(filePath);
                }

                if (anyChanged && !_loadingConfiguredFiles)
                {
                    NotifyRecipesChanged();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to parse recipe file '{filePath}'", ex);
                overallResult.AddError($"Exception reading recipe file: {ex.Message}");
                if (interactiveApproval)
                {
                    RecipeApprovalWindow.ShowValidationError(filePath, rawErrorMessage: ex.Message);
                }
            }

            return overallResult;
        }

        /// <summary>
        /// The text of a recipe file, the encoding detected like File.ReadAllText does
        /// </summary>
        internal static string DecodeRecipeFile(byte[] bytes)
        {
            using (var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// What the approval window shows for a recipe from a file: the content that was read, the earlier approval, and the
        /// built-in recipe it replaces
        /// </summary>
        private RecipeApprovalRequest CreateApprovalRequest(CaptureRecipe recipe, string filePath, string content, string contentHash, RecipeValidationResult valResult)
        {
            var previousRecord = RecipeTrustStore.GetTrustRecord(filePath);
            var request = new RecipeApprovalRequest
            {
                Recipe = recipe,
                FilePath = filePath,
                Content = content,
                ContentHash = contentHash,
                Validation = valResult,
                PreviousRecord = previousRecord,
                // A file an AI tool created, changed on disk: it starts switched off, like a proposal
                StartSwitchedOff = previousRecord?.IsAiCreated == true
            };
            var builtIn = GetBuiltInRecipe(recipe.Id);
            if (builtIn != null)
            {
                request.ReplacedRecipe = builtIn;
                request.ReplacesBuiltIn = true;
                request.PreviousContent = RecipeSerializer.Serialize(builtIn);
            }
            return request;
        }

        /// <summary>
        /// Show the approval dialog (modal, on the UI thread). Returns what the user approved, null when the recipe was rejected.
        /// </summary>
        internal ApprovalResult RequestInteractiveApprovalWithOptions(RecipeApprovalRequest request)
        {
            if (!UiDispatcher.Current.CheckAccess())
            {
                Log.WarnFormat("The approval of '{0}' can only be asked on the UI thread, the recipe is not approved.", request.FilePath);
                return null;
            }

            var window = new RecipeApprovalWindow(request)
            {
                Topmost = true,
                ShowActivated = true,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen
            };

            // The active WPF window (e.g. RecipeEditorWindow) owns the approval, otherwise it stands alone (topmost)
            IntPtr ownerHwnd = IntPtr.Zero;
            if (System.Windows.Application.Current != null)
            {
                try
                {
                    var activeWpfWindow = System.Windows.Application.Current.Windows
                        .OfType<System.Windows.Window>()
                        .FirstOrDefault(w => w.IsActive && w != window);
                    if (activeWpfWindow != null)
                    {
                        ownerHwnd = new System.Windows.Interop.WindowInteropHelper(activeWpfWindow).Handle;
                    }
                }
                catch
                {
                    ownerHwnd = IntPtr.Zero;
                }
            }

            if (ownerHwnd != IntPtr.Zero)
            {
                new System.Windows.Interop.WindowInteropHelper(window).Owner = ownerHwnd;
            }

            if (window.ShowDialog() == true && window.Approval != null)
            {
                window.Approval.ReplacesBuiltIn = request.ReplacesBuiltIn;
                return new ApprovalResult(window.Approval, window.OpenInEditor) { OptionValues = window.OptionValues };
            }
            return window.IsRevoked ? ApprovalResult.Revoked : null;
        }

        private RecipeApproval RequestInteractiveApproval(RecipeApprovalRequest request)
        {
            return RequestInteractiveApprovalWithOptions(request)?.Approval;
        }

        /// <summary>
        /// Ensures that the recipe's backing file on disk is approved and current.
        /// If the file has changed since approval, prompts the user interactively (if possible)
        /// and reloads the recipe. Returns the valid/updated recipe, or null if unapproved/rejected.
        /// </summary>
        public async Task<CaptureRecipe> EnsureRecipeApprovedAndUpToDateAsync(CaptureRecipe currentRecipe, CancellationToken cancellationToken = default)
        {
            if (currentRecipe == null) return null;
            if (string.IsNullOrEmpty(currentRecipe.FilePath))
            {
                // Built-in in-memory recipe without external file
                return currentRecipe;
            }

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(currentRecipe.FilePath);
            }
            catch (Exception ex)
            {
                Log.Warn($"Invalid recipe file path: {currentRecipe.FilePath}", ex);
                return currentRecipe;
            }

            if (!File.Exists(fullPath))
            {
                Log.WarnFormat("External recipe file '{0}' no longer exists on disk.", fullPath);
                return null;
            }

            // Check if file hash still matches DPAPI trust store
            bool isApproved = RecipeTrustStore.IsRecipeApproved(fullPath, out string currentHash, out bool allowExternalCommands);
            if (!isApproved)
            {
                Log.InfoFormat("Recipe file '{0}' was modified on disk or is not approved. Prompting user for approval before execution.", fullPath);
                // The approval dialog is shown on the UI thread, the flow waits for it without blocking
                var result = await UiDispatcher.Current.InvokeAsync(() => LoadRecipeFromFile(fullPath, interactiveApproval: true, forceApprovalPrompt: false), cancellationToken).ConfigureAwait(false);
                if (!result.IsValid)
                {
                    Log.WarnFormat("Recipe re-approval or reload failed for '{0}': {1}", fullPath, string.Join("; ", result.Errors));
                    return null;
                }

                // Return the newly loaded and updated recipe
                return GetRecipeById(currentRecipe.Id)
                    ?? GetAllRecipes().FirstOrDefault(r => string.Equals(r.FilePath, fullPath, StringComparison.OrdinalIgnoreCase));
            }

            return currentRecipe;
        }

        /// <summary>
        /// Revokes the approval of a recipe from a file: it no longer runs (a built-in recipe it replaced comes back), and isn't
        /// loaded again until the user opens its file again. False when the recipe has no file.
        /// </summary>
        public bool RevokeApproval(string recipeId, string filePath = null)
        {
            var extension = GetExtensionById(recipeId);
            bool isExtension = !string.IsNullOrEmpty(extension?.FilePath) && GetRecipeById(recipeId)?.FilePath == null;
            filePath ??= GetRecipeById(recipeId)?.FilePath ?? extension?.FilePath;
            if (string.IsNullOrEmpty(recipeId) || string.IsNullOrEmpty(filePath))
            {
                return false;
            }
            RecipeTrustStore.RevokeRecipeApproval(filePath, recipeId);
            if (isExtension || GetExtensionsOfFile(Path.GetFullPath(filePath)).Any(e => string.Equals(e.Id, recipeId, StringComparison.OrdinalIgnoreCase)))
            {
                DropExtension(recipeId);
            }
            else
            {
                UnregisterRecipe(recipeId);
            }
            Log.InfoFormat("The user revoked the approval of recipe '{0}' from '{1}'.", recipeId, filePath);
            return true;
        }

        public RecipeValidationResult ReviewApproval(string recipeId)
        {
            string filePath = GetRecipeById(recipeId)?.FilePath ?? GetExtensionById(recipeId)?.FilePath;
            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }
            // Shows the approval window with the current approval; what the user confirms replaces it
            return LoadRecipeFromFile(filePath, interactiveApproval: true, forceApprovalPrompt: true);
        }

        public bool ShowRecipeDetails(string recipeId)
        {
            var recipe = GetRecipeById(recipeId);
            if (recipe == null && GetExtensionById(recipeId) is RecipeExtension extension && UiDispatcher.Current.CheckAccess())
            {
                return ShowExtensionDetails(extension);
            }
            if (recipe == null || !UiDispatcher.Current.CheckAccess())
            {
                return false;
            }

            RecipeApprovalRequest request;
            if (!string.IsNullOrEmpty(recipe.FilePath) && File.Exists(recipe.FilePath))
            {
                byte[] bytes = File.ReadAllBytes(recipe.FilePath);
                string content = DecodeRecipeFile(bytes);
                var fileRecipe = RecipeSerializer.DeserializeList(content, validate: false)
                    .FirstOrDefault(r => string.Equals(r.Id, recipeId, StringComparison.OrdinalIgnoreCase)) ?? recipe.Clone();
                fileRecipe.FilePath = recipe.FilePath;
                request = CreateApprovalRequest(fileRecipe, recipe.FilePath, content, RecipeTrustStore.ComputeSha256(bytes), RecipeValidator.Validate(fileRecipe));
                // Only the details: no "starts switched off" for files an AI tool wrote, the switches show the approval
                request.StartSwitchedOff = false;
            }
            else
            {
                request = new RecipeApprovalRequest
                {
                    Recipe = recipe,
                    Content = RecipeSerializer.Serialize(recipe),
                    Validation = RecipeValidator.Validate(recipe)
                };
            }
            request.IsReadOnly = true;

            var window = new RecipeApprovalWindow(request)
            {
                Owner = System.Windows.Application.Current?.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive),
                ShowActivated = true
            };
            window.WindowStartupLocation = window.Owner != null ? System.Windows.WindowStartupLocation.CenterOwner : System.Windows.WindowStartupLocation.CenterScreen;
            window.ShowDialog();
            return true;
        }

        /// <summary>
        /// Loads the recipe files again, asking the user where needed (approval, errors), e.g. the ones held back by <see cref="LoadRecipesAtStartup"/>
        /// </summary>
        public void LoadRecipeFilesInteractively(IEnumerable<string> filePaths)
        {
            _loadingConfiguredFiles = true;
            try
            {
                foreach (var filePath in filePaths)
                {
                    LoadRecipeFromFile(filePath, interactiveApproval: true);
                }
            }
            finally
            {
                _loadingConfiguredFiles = false;
            }
            NotifyRecipesChanged();
        }

        /// <summary>
        /// The trust store keeps the approvals of the configured recipe files only, a file which is configured again is asked for again
        /// </summary>
        static partial void ForgetApprovalsOfUnusedFiles()
        {
            // Without the recipe configuration (Recipe Editor plugin not loaded, recipes off) it isn't known which files are used
            var recipeConfig = RecipeConfigHelper.TryGetRecipeConfiguration();
            if (recipeConfig == null || !RecipeConfigHelper.IsRecipeFeatureEnabled(recipeConfig))
            {
                return;
            }
            var configuredFiles = new List<string>();
            foreach (var path in (recipeConfig.RecipeFiles ?? string.Empty).Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    configuredFiles.Add(Path.GetFullPath(path.Trim()));
                }
                catch (Exception ex)
                {
                    Log.Debug($"Ignoring the invalid recipe file path '{path}'.", ex);
                }
            }
            RecipeTrustStore.KeepOnly(configuredFiles);
        }

        /// <summary>
        /// Stops the watchers of folders which hold no recipe or automatic step file which is used
        /// </summary>
        partial void PruneFileWatchers()
        {
            var usedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void AddDirectoryOf(string filePath)
            {
                if (string.IsNullOrEmpty(filePath)) return;
                try
                {
                    usedDirectories.Add(Path.GetDirectoryName(Path.GetFullPath(filePath)));
                }
                catch (Exception ex)
                {
                    Log.Debug($"Ignoring the invalid recipe file path '{filePath}'.", ex);
                }
            }
            foreach (var recipe in GetAllRecipes())
            {
                AddDirectoryOf(recipe.FilePath);
            }
            foreach (var extension in GetAllExtensions())
            {
                AddDirectoryOf(extension.FilePath);
            }
            lock (_fileWatchers)
            {
                foreach (var directory in _fileWatchers.Keys.Where(directory => !usedDirectories.Contains(directory)).ToList())
                {
                    _fileWatchers[directory].Dispose();
                    _fileWatchers.Remove(directory);
                    Log.DebugFormat("Stopped watching '{0}' for recipe file changes.", directory);
                }
            }
        }
        partial void AddApprovalDetails(RecipeDetails details, string filePath, string recipeId)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }
            var record = RecipeTrustStore.GetTrustRecord(filePath);
            if (record == null)
            {
                return;
            }
            details.ApprovedAt = record.ApprovedAt.ToLocalTime();
            details.ApprovedHash = record.Sha256Hash;
            details.IsApprovalCurrent = string.Equals(RecipeTrustStore.ComputeSha256(filePath), record.Sha256Hash, StringComparison.OrdinalIgnoreCase);
            details.Permissions = record.GetApproval(recipeId)?.AllowedGates?.Select(RecipeApprovalPolicy.GetGateName).ToList() ?? new List<string>();
        }
    }
}
