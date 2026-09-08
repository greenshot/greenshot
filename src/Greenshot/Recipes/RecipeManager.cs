/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes;
using log4net;

namespace Greenshot.Recipes
{
    /// <summary>
    /// Manages built-in and user-defined capture recipes.
    /// Supports overriding built-ins and loading custom recipes from explicitly configured JSON files.
    /// Unauthenticated directory auto-discovery is disabled for security.
    /// </summary>
    public class RecipeManager : IRecipeManager
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RecipeManager));
        private static ICoreConfiguration CoreConfig
        {
            get
            {
                try
                {
                    return IniConfigRegistry.GetSection<ICoreConfiguration>();
                }
                catch
                {
                    return null;
                }
            }
        }

        public const string RecipeIdRegion = "recipe_region";
        public const string RecipeIdWindow = "recipe_window";
        public const string RecipeIdActiveWindow = "recipe_active_window";
        public const string RecipeIdFullScreen = "recipe_fullscreen";
        public const string RecipeIdLastRegion = "recipe_lastregion";
        public const string RecipeIdClipboard = "recipe_clipboard";
        public const string RecipeIdFile = "recipe_file";
        public const string RecipeIdOcr = "recipe_ocr";

        private readonly Dictionary<string, CaptureRecipe> _builtInRecipes = new Dictionary<string, CaptureRecipe>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CaptureRecipe> _recipes = new Dictionary<string, CaptureRecipe>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FileSystemWatcher> _fileWatchers = new Dictionary<string, FileSystemWatcher>(StringComparer.OrdinalIgnoreCase);

        public event EventHandler RecipesChanged;

        private static RecipeManager _instance;
        public static RecipeManager Instance => _instance ??= new RecipeManager();

        public RecipeManager()
        {
            InitializeDefaultRecipes();
            LoadConfiguredRecipeFiles();
            NotifyRecipesChanged();
        }

        private void InitializeDefaultRecipes()
        {
            // 1. Interactive Region Capture
            // Pre-selection Processors runs processors whose PreferredTiming == PreSelection
            // (e.g. ZXing QR scan, Win10 OCR) while the CaptureForm is open so their results
            // appear as interactive hotspots. Post-selection runs PostSelection processors after
            // the user confirms the crop.
            var regionRecipe = new CaptureRecipe(
                RecipeIdRegion,
                Language.GetString("contextmenu_capturearea") ?? "Capture region",
                "Interactively select a region on the screen")
                .AddStep(RecipeStepConfig.CreateSource(CaptureSourceType.Region))
                .AddStep(RecipeStepConfig.CreateProcessors(timing: ProcessorTiming.PreSelection).WithName("Scan Before Selection"))
                .AddStep(RecipeStepConfig.CreateSelection(CaptureMode.Region))
                .AddStep(RecipeStepConfig.CreateFeedback())
                .AddStep(RecipeStepConfig.CreateProcessors(timing: ProcessorTiming.PostSelection).WithName("Process After Selection"))
                .AddStep(RecipeStepConfig.CreateDestinations())
                .AddStep(RecipeStepConfig.CreateNotification());
            RegisterBuiltIn(regionRecipe);

            // 2. Interactive Window Capture
            var windowRecipe = new CaptureRecipe(
                RecipeIdWindow,
                Language.GetString("contextmenu_capturewindow") ?? "Capture window",
                "Interactively select a window on the screen")
                .AddStep(RecipeStepConfig.CreateSource(CaptureSourceType.Window))
                .AddStep(RecipeStepConfig.CreateProcessors(timing: ProcessorTiming.PreSelection).WithName("Scan Before Selection"))
                .AddStep(RecipeStepConfig.CreateSelection(CaptureMode.Window))
                .AddStep(RecipeStepConfig.CreateFeedback())
                .AddStep(RecipeStepConfig.CreateProcessors(timing: ProcessorTiming.PostSelection).WithName("Process After Selection"))
                .AddStep(RecipeStepConfig.CreateDestinations())
                .AddStep(RecipeStepConfig.CreateNotification());
            RegisterBuiltIn(windowRecipe);

            // 3. Active Window Capture
            var activeWindowRecipe = new CaptureRecipe(
                RecipeIdActiveWindow,
                "Capture active window",
                "Directly capture the active window")
                .AddStep(RecipeStepConfig.CreateSource(CaptureSourceType.ActiveWindow))
                .AddStep(RecipeStepConfig.CreateFeedback())
                .AddStep(RecipeStepConfig.CreateProcessors())
                .AddStep(RecipeStepConfig.CreateDestinations())
                .AddStep(RecipeStepConfig.CreateNotification());
            RegisterBuiltIn(activeWindowRecipe);

            // 4. Full Screen Capture
            var fullScreenRecipe = new CaptureRecipe(
                RecipeIdFullScreen,
                Language.GetString("contextmenu_capturefullscreen") ?? "Capture full screen",
                "Capture the entire screen or monitor")
                .AddStep(RecipeStepConfig.CreateSource(CaptureSourceType.FullScreen))
                .AddStep(RecipeStepConfig.CreateFeedback())
                .AddStep(RecipeStepConfig.CreateProcessors())
                .AddStep(RecipeStepConfig.CreateDestinations())
                .AddStep(RecipeStepConfig.CreateNotification());
            RegisterBuiltIn(fullScreenRecipe);

            // 5. Last Region Capture
            var lastRegionRecipe = new CaptureRecipe(
                RecipeIdLastRegion,
                Language.GetString("contextmenu_capturelastregion") ?? "Capture last region",
                "Re-capture the coordinates of the previous region")
                .AddStep(RecipeStepConfig.CreateSource(CaptureSourceType.LastRegion))
                .AddStep(RecipeStepConfig.CreateFeedback())
                .AddStep(RecipeStepConfig.CreateProcessors())
                .AddStep(RecipeStepConfig.CreateDestinations())
                .AddStep(RecipeStepConfig.CreateNotification());
            RegisterBuiltIn(lastRegionRecipe);

            // 6. Clipboard Import
            var clipboardRecipe = new CaptureRecipe(
                RecipeIdClipboard,
                Language.GetString("contextmenu_captureclipboard") ?? "Capture from clipboard",
                "Import and process image from system clipboard")
                .AddStep(RecipeStepConfig.CreateSource(CaptureSourceType.Clipboard, captureMouse: false))
                .AddStep(RecipeStepConfig.CreateDestinations(new[] { "Editor" }));
            RegisterBuiltIn(clipboardRecipe);

            // 7. File Import
            var fileRecipe = new CaptureRecipe(
                RecipeIdFile,
                Language.GetString("contextmenu_openfile") ?? "Open file",
                "Import an image or .greenshot file from disk")
                .AddStep(RecipeStepConfig.CreateSource(CaptureSourceType.File, captureMouse: false))
                .AddStep(RecipeStepConfig.CreateDestinations(new[] { "Editor" }));
            RegisterBuiltIn(fileRecipe);

            // 8. OCR Text Capture
            var ocrRecipe = new CaptureRecipe(
                RecipeIdOcr,
                "OCR text to clipboard",
                "Select a region and extract text directly to clipboard")
                .AddStep(RecipeStepConfig.CreateSource(CaptureSourceType.TextOcr, captureMouse: false))
                .AddStep(RecipeStepConfig.CreateSelection(CaptureMode.Text))
                .AddStep(RecipeStepConfig.CreateFeedback())
                .AddStep(RecipeStepConfig.CreateProcessors(new[] { "Windows10OcrProcessor" }))
                .AddStep(RecipeStepConfig.CreateDestinations(new[] { "Clipboard" }));
            RegisterBuiltIn(ocrRecipe);
        }

        private void RegisterBuiltIn(CaptureRecipe recipe)
        {
            recipe.IsBuiltIn = true;
            recipe.IsOverridden = false;
            _builtInRecipes[recipe.Id] = recipe.Clone();
            _recipes[recipe.Id] = recipe;
        }

        public void LoadConfiguredRecipeFiles()
        {
            if (!CoreConfig.IsBetaTester)
            {
                Log.Debug("CoreConfig.IsBetaTester is false. Skipping external recipe file loading.");
                return;
            }

            string configured = CoreConfig.RecipeFiles;
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
                        var result = LoadRecipeFromFile(fullPath);
                        if (!result.IsValid)
                        {
                            Log.WarnFormat("Validation failed for configured recipe file '{0}': {1}", fullPath, string.Join("; ", result.Errors));
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
                        };

                        watcher.Changed += handler;
                        watcher.Created += handler;
                        watcher.Renamed += (s, e) =>
                        {
                            Log.InfoFormat("Detected recipe file rename on disk: '{0}' -> '{1}'.", e.OldFullPath, e.FullPath);
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

            SetupWatcherForFile(filePath);

            try
            {
                var recipes = RecipeSerializer.LoadListFromFile(filePath, validate: false);
                bool anyChanged = false;

                foreach (var recipe in recipes)
                {
                    var valResult = RecipeValidator.Validate(recipe);
                    if (!valResult.IsValid)
                    {
                        foreach (var err in valResult.Errors) overallResult.AddError($"[{recipe.Id ?? "unknown"}]: {err}");
                        continue;
                    }

                    // Security check: SHA-256 cryptographic trust pinning
                    string currentHash = null;
                    bool allowExternalCommands = false;
                    bool isApproved = RecipeTrustStore.IsRecipeApproved(filePath, out currentHash, out allowExternalCommands);
                    if (forceApprovalPrompt)
                    {
                        isApproved = false;
                    }

                    if (!isApproved)
                    {
                        if (string.IsNullOrEmpty(currentHash))
                        {
                            currentHash = RecipeTrustStore.ComputeSha256(filePath);
                        }

                        if (interactiveApproval)
                        {
                            bool approved = RequestInteractiveApproval(recipe, filePath, valResult, out allowExternalCommands);
                            if (approved)
                            {
                                RecipeTrustStore.RecordApproval(filePath, currentHash, allowExternalCommands, recipe.Name, recipe.Version);
                                forceApprovalPrompt = false; // Once user approves file, don't force prompt again for subsequent recipes in same file
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

                    if (valResult.HasExternalCommands && !allowExternalCommands)
                    {
                        overallResult.AddError($"Recipe '{recipe.Name}' contains external commands, but authorization was not granted.");
                        continue;
                    }

                    foreach (var warn in valResult.Warnings) overallResult.AddWarning($"[{recipe.Id}]: {warn}");

                    recipe.FilePath = Path.GetFullPath(filePath);

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
                }

                if (anyChanged)
                {
                    NotifyRecipesChanged();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to parse recipe file '{filePath}'", ex);
                overallResult.AddError($"Exception reading recipe file: {ex.Message}");
            }

            return overallResult;
        }

        private bool RequestInteractiveApproval(CaptureRecipe recipe, string filePath, RecipeValidationResult valResult, out bool allowExternalCommands)
        {
            allowExternalCommands = false;
            bool approved = false;
            bool localAllow = false;

            void Show()
            {
                var window = new UI.RecipeApprovalWindow(recipe, filePath, valResult)
                {
                    Topmost = true,
                    ShowActivated = true,
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen
                };

                // Only set owner if mainForm is actually visible.
                // If mainForm is a hidden system tray form, setting it as owner causes Windows to push the dialog behind other active windows!
                var mainForm = SimpleServiceProvider.Current.GetInstance<System.Windows.Forms.Form>(isOptional: true);
                if (mainForm != null && mainForm.IsHandleCreated && mainForm.Visible)
                {
                    new System.Windows.Interop.WindowInteropHelper(window).Owner = mainForm.Handle;
                }

                if (window.ShowDialog() == true)
                {
                    approved = true;
                    localAllow = window.AllowExternalCommands;
                }
            }

            if (System.Threading.Thread.CurrentThread.GetApartmentState() == System.Threading.ApartmentState.STA)
            {
                Show();
            }
            else
            {
                var staThread = new System.Threading.Thread(() => Show());
                staThread.SetApartmentState(System.Threading.ApartmentState.STA);
                staThread.Start();
                staThread.Join();
            }

            allowExternalCommands = localAllow;
            return approved;
        }

        private void NotifyRecipesChanged()
        {
            var triggerManager = SimpleServiceProvider.Current.GetInstance<Greenshot.Base.Triggers.ITriggerManager>(isOptional: true) as Triggers.TriggerManager ?? Triggers.TriggerManager.Instance;
            triggerManager?.SyncRecipeTriggers(GetAllRecipes());
            RecipesChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Ensures that the recipe's backing file on disk is approved and current.
        /// If the file has changed since approval, prompts the user interactively (if possible)
        /// and reloads the recipe. Returns the valid/updated recipe, or null if unapproved/rejected.
        /// </summary>
        public CaptureRecipe EnsureRecipeApprovedAndUpToDate(CaptureRecipe currentRecipe)
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
                var result = LoadRecipeFromFile(fullPath, interactiveApproval: true, forceApprovalPrompt: false);
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

        public bool ResetToDefault(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return false;

            lock (_recipes)
            {
                if (_builtInRecipes.TryGetValue(recipeId, out var original))
                {
                    _recipes[recipeId] = original.Clone();
                    Log.InfoFormat("Reset recipe '{0}' back to default built-in definition.", recipeId);
                    NotifyRecipesChanged();
                    return true;
                }
            }

            return false;
        }

        public void ResetAllToDefault()
        {
            lock (_recipes)
            {
                foreach (var kvp in _builtInRecipes)
                {
                    _recipes[kvp.Key] = kvp.Value.Clone();
                }
            }
            Log.Info("Reset all built-in recipes to defaults.");
            NotifyRecipesChanged();
        }

        public void ReloadRecipes()
        {
            lock (_recipes)
            {
                _recipes.Clear();
                foreach (var kvp in _builtInRecipes)
                {
                    _recipes[kvp.Key] = kvp.Value.Clone();
                }
            }
            LoadConfiguredRecipeFiles();
            NotifyRecipesChanged();
        }

        public IReadOnlyList<CaptureRecipe> GetAllRecipes()
        {
            lock (_recipes)
            {
                return _recipes.Values.ToList();
            }
        }

        public CaptureRecipe GetRecipeById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            lock (_recipes)
            {
                return _recipes.TryGetValue(id, out var recipe) ? recipe : null;
            }
        }

        public void RegisterRecipe(CaptureRecipe recipe)
        {
            if (recipe == null || string.IsNullOrEmpty(recipe.Id)) throw new ArgumentException("Recipe or Recipe.Id cannot be null/empty");

            lock (_recipes)
            {
                _recipes[recipe.Id] = recipe;
            }

            Log.InfoFormat("Registered recipe: {0} ({1})", recipe.Name, recipe.Id);
            NotifyRecipesChanged();
        }

        public bool UnregisterRecipe(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return false;

            lock (_recipes)
            {
                if (_recipes.TryGetValue(recipeId, out var recipe))
                {
                    if (recipe.IsBuiltIn && !recipe.IsOverridden)
                    {
                        Log.WarnFormat("Cannot unregister built-in recipe: {0}", recipeId);
                        return false;
                    }

                    if (recipe.IsOverridden)
                    {
                        // Reset overridden recipe back to original built-in
                        _recipes[recipeId] = _builtInRecipes[recipeId].Clone();
                        Log.InfoFormat("Reverted overridden recipe '{0}' back to default definition.", recipeId);
                        NotifyRecipesChanged();
                        return true;
                    }

                    _recipes.Remove(recipeId);
                    Log.InfoFormat("Unregistered recipe: {0}", recipeId);
                    NotifyRecipesChanged();
                    return true;
                }
            }

            return false;
        }
    }
}
