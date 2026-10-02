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
using System.Text;
using System.Threading;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Pipeline.Contracts;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using log4net;
using System.Threading.Tasks;
using Greenshot.Base.Threading;

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
        public const string RecipeIdExtension = "recipe_browser_extension";
        public const string RecipeIdAiCaptureWindow = "recipe_ai_capture_window";
        public const string RecipeIdAiCaptureRegion = "recipe_ai_capture_region";
        public const string RecipeIdAiCaptureScreen = "recipe_ai_capture_screen";

        private readonly Dictionary<string, CaptureRecipe> _builtInRecipes = new Dictionary<string, CaptureRecipe>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CaptureRecipe> _recipes = new Dictionary<string, CaptureRecipe>(StringComparer.OrdinalIgnoreCase);
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

        public event EventHandler RecipesChanged;

        // Thread-safe: the first access can come from the UI thread and an IPC or pipeline thread at the same time,
        // and a second instance would silently lose what was registered in the first one.
        private static readonly Lazy<RecipeManager> LazyInstance = new Lazy<RecipeManager>(() => new RecipeManager(), LazyThreadSafetyMode.ExecutionAndPublication);
        public static RecipeManager Instance => LazyInstance.Value;

        public RecipeManager()
        {
            InitializeDefaultRecipes();
            LoadConfiguredRecipeFiles();
            NotifyRecipesChanged();
        }

        private void InitializeDefaultRecipes()
        {
            // Read the disabled recipes once, not per recipe
            var disabled = GetDisabledRecipeIds();

            // 1. Interactive Region Capture
            var regionRecipe = new CaptureRecipe(
                RecipeIdRegion,
                "Capture region",
                "Interactively select a region on the screen")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.Region))
                .AddNode(RecipeStepConfig.CreateProcessors("scan_pre", timing: ProcessorTiming.PreSelection).WithName("Scan Before Selection"))
                .AddNode(RecipeStepConfig.CreateSelection("select", CaptureMode.Region))
                .AddNode(RecipeStepConfig.CreateFeedback("feedback"))
                .AddNode(RecipeStepConfig.CreateProcessors("scan_post", timing: ProcessorTiming.PostSelection).WithName("Process After Selection"))
                .AddNode(RecipeStepConfig.CreateDestinations("export"))
                .AddNode(RecipeStepConfig.CreateNotification("notify"));
            regionRecipe.Flow = new RecipeFlowConfig("acquire")
                .AddTransition("acquire", "scan_pre")
                .AddTransition("scan_pre", "select")
                .AddTransition("select", "feedback")
                .AddTransition("feedback", "scan_post")
                .AddTransition("scan_post", "export")
                .AddTransition("export", "notify");
            RegisterBuiltIn(regionRecipe, disabled);

            // 2. Interactive Window Capture
            var windowRecipe = new CaptureRecipe(
                RecipeIdWindow,
                "Capture window",
                "Interactively select a window on the screen")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.Window))
                .AddNode(RecipeStepConfig.CreateProcessors("scan_pre", timing: ProcessorTiming.PreSelection).WithName("Scan Before Selection"))
                .AddNode(RecipeStepConfig.CreateSelection("select", CaptureMode.Window))
                .AddNode(RecipeStepConfig.CreateFeedback("feedback"))
                .AddNode(RecipeStepConfig.CreateProcessors("scan_post", timing: ProcessorTiming.PostSelection).WithName("Process After Selection"))
                .AddNode(RecipeStepConfig.CreateDestinations("export"))
                .AddNode(RecipeStepConfig.CreateNotification("notify"));
            windowRecipe.Flow = new RecipeFlowConfig("acquire")
                .AddTransition("acquire", "scan_pre")
                .AddTransition("scan_pre", "select")
                .AddTransition("select", "feedback")
                .AddTransition("feedback", "scan_post")
                .AddTransition("scan_post", "export")
                .AddTransition("export", "notify");
            RegisterBuiltIn(windowRecipe, disabled);

            // 3. Active Window Capture
            var activeWindowRecipe = new CaptureRecipe(
                RecipeIdActiveWindow,
                "Capture active window",
                "Directly capture the active window")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.ActiveWindow))
                .AddNode(RecipeStepConfig.CreateFeedback("feedback"))
                .AddNode(RecipeStepConfig.CreateProcessors("processors"))
                .AddNode(RecipeStepConfig.CreateDestinations("export"))
                .AddNode(RecipeStepConfig.CreateNotification("notify"));
            activeWindowRecipe.Flow = new RecipeFlowConfig("acquire")
                .AddTransition("acquire", "feedback")
                .AddTransition("feedback", "processors")
                .AddTransition("processors", "export")
                .AddTransition("export", "notify");
            RegisterBuiltIn(activeWindowRecipe, disabled);

            // 4. Full Screen Capture
            var fullScreenRecipe = new CaptureRecipe(
                RecipeIdFullScreen,
                "Capture full screen",
                "Capture the entire screen or monitor")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.FullScreen))
                .AddNode(RecipeStepConfig.CreateFeedback("feedback"))
                .AddNode(RecipeStepConfig.CreateProcessors("processors"))
                .AddNode(RecipeStepConfig.CreateDestinations("export"))
                .AddNode(RecipeStepConfig.CreateNotification("notify"));
            fullScreenRecipe.Flow = new RecipeFlowConfig("acquire")
                .AddTransition("acquire", "feedback")
                .AddTransition("feedback", "processors")
                .AddTransition("processors", "export")
                .AddTransition("export", "notify");
            RegisterBuiltIn(fullScreenRecipe, disabled);

            // 5. Last Region Capture
            var lastRegionRecipe = new CaptureRecipe(
                RecipeIdLastRegion,
                "Capture last region",
                "Re-capture the coordinates of the previous region")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.LastRegion))
                .AddNode(RecipeStepConfig.CreateFeedback("feedback"))
                .AddNode(RecipeStepConfig.CreateProcessors("processors"))
                .AddNode(RecipeStepConfig.CreateDestinations("export"))
                .AddNode(RecipeStepConfig.CreateNotification("notify"));
            lastRegionRecipe.Flow = new RecipeFlowConfig("acquire")
                .AddTransition("acquire", "feedback")
                .AddTransition("feedback", "processors")
                .AddTransition("processors", "export")
                .AddTransition("export", "notify");
            RegisterBuiltIn(lastRegionRecipe, disabled);

            // 6. Clipboard Import
            var clipboardRecipe = new CaptureRecipe(
                RecipeIdClipboard,
                "Capture from clipboard",
                "Import and process image from system clipboard")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.Clipboard, captureMouse: false))
                .AddNode(RecipeStepConfig.CreateDestinations("export", new[] { "Editor" }));
            clipboardRecipe.Flow = new RecipeFlowConfig("acquire")
                .AddTransition("acquire", "export");
            RegisterBuiltIn(clipboardRecipe, disabled);

            // 7. File Import
            var fileRecipe = new CaptureRecipe(
                RecipeIdFile,
                "Open file",
                "Import an image or .greenshot file from disk")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.File, captureMouse: false))
                .AddNode(RecipeStepConfig.CreateDestinations("export", new[] { "Editor" }))
                .AddTrigger(TriggerConfig.CreateOpenFile(name: "Default Open With File Trigger"));
            fileRecipe.Flow = new RecipeFlowConfig("acquire")
                .AddTransition("acquire", "export");
            RegisterBuiltIn(fileRecipe, disabled);

            // 8. OCR Text Capture
            var ocrRecipe = new CaptureRecipe(
                RecipeIdOcr,
                "OCR text to clipboard",
                "Select a region and extract text directly to clipboard")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.TextOcr, captureMouse: false))
                .AddNode(RecipeStepConfig.CreateSelection("select", CaptureMode.Text))
                .AddNode(RecipeStepConfig.CreateFeedback("feedback"))
                .AddNode(RecipeStepConfig.CreateProcessors("ocr", new[] { "Windows10OcrProcessor" }))
                .AddNode(RecipeStepConfig.CreateClipboard("export", "TextOnly"));
            ocrRecipe.Flow = new RecipeFlowConfig("acquire")
                .AddTransition("acquire", "select")
                .AddTransition("select", "feedback")
                .AddTransition("feedback", "ocr")
                .AddTransition("ocr", "export");
            RegisterBuiltIn(ocrRecipe, disabled);

            // 9. Browser Extension Capture
            var extensionRecipe = new CaptureRecipe(
                RecipeIdExtension,
                "Capture from browser extension",
                "Process screenshots received from the browser extension and choose destination interactively")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.Extension, captureMouse: false))
                .AddNode(RecipeStepConfig.CreateDynamicDestination("export", "Export Browser Capture"))
                .AddTrigger(TriggerConfig.CreateExtension(name: "Default Browser Extension Trigger"));
            extensionRecipe.Flow = new RecipeFlowConfig("acquire")
                .AddTransition("acquire", "export");
            RegisterBuiltIn(extensionRecipe, disabled);

            RegisterAiToolRecipes(disabled);
        }

        /// <summary>
        /// The tools AI tools get (greenshot-mcp.exe): capturing a window, a region or the screen, optionally with OCR.
        /// The image and text go back to the AI tool, there is no destination.
        /// </summary>
        private void RegisterAiToolRecipes(ISet<string> disabled)
        {
            var ocrArgument = new CommandlineArgument
            {
                Name = "ocr",
                Variable = "Ocr",
                Type = ContractDataType.Boolean,
                DefaultValue = "false",
                Description = "true to also return the text in the image (OCR) with the position of each line"
            };

            var windowRecipe = new CaptureRecipe(
                RecipeIdAiCaptureWindow,
                "AI tool: capture window",
                "Captures a window for an AI tool, with its exact contents (also when it is covered), without activating it")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.Window, captureMouse: false, delayMs: 0).Set("WindowHandle", "${Window}"))
                .AddNode(CreateOcrCondition("ocr_wanted"))
                .AddNode(RecipeStepConfig.CreateProcessors("ocr", new[] { "Windows10OcrProcessor" }))
                .AddTrigger(TriggerConfig.CreateAiTool(
                    "capture_window",
                    "Screenshot of one window, with its exact contents even when other windows cover it. The window is not activated. " +
                    "Use list_windows first and pass the id of the window (e.g. w7).",
                    new[]
                    {
                        new CommandlineArgument
                        {
                            Name = "window",
                            Variable = "Window",
                            Type = ContractDataType.Window,
                            Required = true,
                            Description = "The id of the window from list_windows, e.g. w7"
                        },
                        ocrArgument
                    },
                    title: "Capture window"));
            windowRecipe.Flow = new RecipeFlowConfig("acquire")
                .AddTransition("acquire", "ocr_wanted")
                .AddConditionalTransition("ocr_wanted", "ocr", "ocr");
            RegisterBuiltIn(windowRecipe, disabled);

            var regionRecipe = new CaptureRecipe(
                RecipeIdAiCaptureRegion,
                "AI tool: capture region",
                "Captures a part of the screen for an AI tool")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.Region, captureMouse: false, delayMs: 0))
                .AddNode(CreateOcrCondition("ocr_wanted"))
                .AddNode(RecipeStepConfig.CreateProcessors("ocr", new[] { "Windows10OcrProcessor" }))
                .AddTrigger(TriggerConfig.CreateAiTool(
                    "capture_region",
                    "Screenshot of a part of the screen, in screen coordinates (list_windows has the bounds of the windows and displays). " +
                    "Use it to see details at full resolution.",
                    new[]
                    {
                        new CommandlineArgument
                        {
                            Name = "region",
                            Variable = "PreSuppliedRegion",
                            Type = ContractDataType.Region,
                            Required = true,
                            Description = "x,y,width,height in screen coordinates, e.g. 0,0,800,600"
                        },
                        ocrArgument
                    },
                    title: "Capture region"));
            regionRecipe.Flow = new RecipeFlowConfig("acquire")
                .AddTransition("acquire", "ocr_wanted")
                .AddConditionalTransition("ocr_wanted", "ocr", "ocr");
            RegisterBuiltIn(regionRecipe, disabled);

            var screenRecipe = new CaptureRecipe(
                RecipeIdAiCaptureScreen,
                "AI tool: capture screen",
                "Captures all displays for an AI tool")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.FullScreen, captureMouse: false, delayMs: 0, screenMode: ScreenCaptureMode.FullScreen))
                .AddNode(CreateOcrCondition("ocr_wanted"))
                .AddNode(RecipeStepConfig.CreateProcessors("ocr", new[] { "Windows10OcrProcessor" }))
                .AddTrigger(TriggerConfig.CreateAiTool(
                    "capture_screen",
                    "Screenshot of all displays. For one display or a part of the screen use capture_region with the bounds from list_windows.",
                    new[] { ocrArgument },
                    title: "Capture screen"));
            screenRecipe.Flow = new RecipeFlowConfig("acquire")
                .AddTransition("acquire", "ocr_wanted")
                .AddConditionalTransition("ocr_wanted", "ocr", "ocr");
            RegisterBuiltIn(screenRecipe, disabled);
        }

        /// <summary>
        /// Continues with the node "ocr" when the Ocr argument is true, otherwise the flow ends
        /// </summary>
        private static RecipeNodeConfig CreateOcrCondition(string id)
        {
            return RecipeStepConfig.CreateConditional(id, new[] { new KeyValuePair<string, string>("ocr", "${Ocr}") }).WithName("OCR wanted?");
        }

        private HashSet<string> GetDisabledRecipeIds()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var recipeConfig = RecipeConfigHelper.TryGetRecipeConfiguration();
                string raw = recipeConfig?.DisabledRecipeIds;
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    foreach (var id in raw.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var trimmed = id.Trim();
                        if (!string.IsNullOrEmpty(trimmed))
                        {
                            set.Add(trimmed);
                        }
                    }
                }
            }
            catch
            {
                // Fallback to empty if config unavailable
            }
            return set;
        }

        private void AddRecipeFileToConfig(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;
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

        private void RemoveRecipeFileFromConfig(string filePath)
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

        private void RegisterBuiltIn(CaptureRecipe recipe, ISet<string> disabledRecipeIds)
        {
            recipe.IsBuiltIn = true;
            recipe.IsOverridden = false;
            recipe.IsEnabled = !disabledRecipeIds.Contains(recipe.Id);
            _builtInRecipes[recipe.Id] = recipe.Clone();
            _recipes[recipe.Id] = recipe;
        }

        public void LoadConfiguredRecipeFiles()
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
                            ScheduleChangedFileCheck(e.FullPath);
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
                    CheckChangedFile(filePath);
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
                Log.InfoFormat("Recipe file '{0}' was changed outside Greenshot, asking for approval.", fullPath);
                UiDispatcher.Current.InvokeAsync(() => LoadRecipeFromFile(fullPath, interactiveApproval: true, forceApprovalPrompt: false)).FireAndLog("Approval of a changed recipe file");
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
            SetupWatcherForFile(fullPath);

            if (approval != null)
            {
                RecipeApprovalPolicy.Apply(recipe, approval);
            }
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

            SetupWatcherForFile(filePath);

            try
            {
                // Read the file once: what is parsed, shown and approved are the same bytes (the file can change in between)
                byte[] bytes = File.ReadAllBytes(filePath);
                string content = DecodeRecipeFile(bytes);
                string contentHash = RecipeTrustStore.ComputeSha256(bytes);
                var recipes = RecipeSerializer.DeserializeList(content, validate: false);
                bool anyChanged = false;

                foreach (var recipe in recipes)
                {
                    recipe.FilePath = filePath;
                    var valResult = RecipeValidator.Validate(recipe);
                    if (!valResult.IsValid)
                    {
                        foreach (var err in valResult.Errors) overallResult.AddError($"[{recipe.Id ?? "unknown"}]: {err}");
                        if (interactiveApproval)
                        {
                            UI.RecipeApprovalWindow.ShowValidationError(filePath, valResult, recipe);
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

                if (anyChanged)
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
                    UI.RecipeApprovalWindow.ShowValidationError(filePath, rawErrorMessage: ex.Message);
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
        private UI.RecipeApprovalRequest CreateApprovalRequest(CaptureRecipe recipe, string filePath, string content, string contentHash, RecipeValidationResult valResult)
        {
            var previousRecord = RecipeTrustStore.GetTrustRecord(filePath);
            var request = new UI.RecipeApprovalRequest
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
        /// A copy of the built-in recipe with this id, null when there is none
        /// </summary>
        public CaptureRecipe GetBuiltInRecipe(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return null;
            lock (_recipes)
            {
                return _builtInRecipes.TryGetValue(recipeId, out var builtIn) ? builtIn.Clone() : null;
            }
        }

        /// <summary>
        /// The built-in recipes which are replaced by a recipe from a file
        /// </summary>
        public IReadOnlyList<CaptureRecipe> GetReplacedBuiltInRecipes()
        {
            lock (_recipes)
            {
                return _recipes.Values.Where(r => r.IsBuiltIn && r.IsOverridden).OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToList();
            }
        }

        /// <summary>
        /// Show the approval dialog (modal, on the UI thread). Returns what the user approved, null when the recipe was rejected.
        /// </summary>
        internal UI.RecipeApprovalWindow.ApprovalResult RequestInteractiveApprovalWithOptions(UI.RecipeApprovalRequest request)
        {
            if (!UiDispatcher.Current.CheckAccess())
            {
                Log.WarnFormat("The approval of '{0}' can only be asked on the UI thread, the recipe is not approved.", request.FilePath);
                return null;
            }

            var window = new UI.RecipeApprovalWindow(request)
            {
                Topmost = true,
                ShowActivated = true,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen
            };

            // Safely determine owner handle.
            // Note: If this runs on a separate STA thread (e.g. from FileWatcher or non-UI thread),
            // accessing mainForm.Handle or mainForm.Visible directly will throw an InvalidOperationException (Cross-thread operation).
            IntPtr ownerHwnd = IntPtr.Zero;
            var mainForm = SimpleServiceProvider.Current.GetInstance<System.Windows.Forms.Form>(isOptional: true);
            if (mainForm != null && mainForm.IsHandleCreated)
            {
                try
                {
                    if (mainForm.Visible && !mainForm.Disposing && !mainForm.IsDisposed)
                    {
                        ownerHwnd = mainForm.Handle;
                    }
                }
                catch
                {
                    ownerHwnd = IntPtr.Zero;
                }
            }

            // If MainForm is not available or hidden, check for active WPF window (e.g. RecipeEditorWindow)
            if (ownerHwnd == IntPtr.Zero && System.Windows.Application.Current != null)
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
                return new UI.RecipeApprovalWindow.ApprovalResult(window.Approval, window.OpenInEditor);
            }
            return window.IsRevoked ? UI.RecipeApprovalWindow.ApprovalResult.Revoked : null;
        }

        private RecipeApproval RequestInteractiveApproval(UI.RecipeApprovalRequest request)
        {
            return RequestInteractiveApprovalWithOptions(request)?.Approval;
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

        public bool ResetToDefault(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return false;

            lock (_recipes)
            {
                if (_builtInRecipes.TryGetValue(recipeId, out var original))
                {
                    string oldFilePath = _recipes.TryGetValue(recipeId, out var cur) ? cur.FilePath : null;
                    var restored = original.Clone();
                    restored.IsEnabled = !GetDisabledRecipeIds().Contains(recipeId);
                    _recipes[recipeId] = restored;
                    RemoveRecipeFileFromConfig(oldFilePath);
                    Log.InfoFormat("Reset recipe '{0}' back to default built-in definition.", recipeId);
                    NotifyRecipesChanged();
                    return true;
                }
            }

            return false;
        }

        public RecipeDetails GetRecipeDetails(string recipeId)
        {
            var recipe = GetRecipeById(recipeId);
            if (recipe == null)
            {
                return null;
            }
            var details = new RecipeDetails
            {
                WhatItDoes = RecipeDescriber.DescribeSteps(recipe).Select(l => l.IsDetail ? "    " + l.Text : l.Text).ToList(),
                Triggers = RecipeDescriber.DescribeTriggers(recipe).Select(t =>
                {
                    var trigger = recipe.Triggers[int.Parse(t.Key.Substring(0, t.Key.IndexOf(':')))];
                    bool isBrowser = t.Key.EndsWith(RecipeApprovalPolicy.BrowserInvocationSuffix);
                    bool on = isBrowser ? trigger.IsActive && trigger.IsBrowserInvocationApproved : trigger.IsActive;
                    string state = t.IsDisabled ? "disabled" : on ? "on" : "off, not approved";
                    return $"{t.Label} ({state})";
                }).ToList(),
                ProposedBy = recipe.ProposedBy
            };

            if (!string.IsNullOrEmpty(recipe.FilePath))
            {
                var record = RecipeTrustStore.GetTrustRecord(recipe.FilePath);
                if (record != null)
                {
                    details.ApprovedAt = record.ApprovedAt.ToLocalTime();
                    details.ApprovedHash = record.Sha256Hash;
                    details.IsApprovalCurrent = string.Equals(RecipeTrustStore.ComputeSha256(recipe.FilePath), record.Sha256Hash, StringComparison.OrdinalIgnoreCase);
                    var approval = record.GetApproval(recipe.Id);
                    details.Permissions = approval?.AllowedGates?.Select(RecipeApprovalPolicy.GetGateName).ToList() ?? new List<string>();
                }
            }

            var builtIn = recipe.IsOverridden ? GetBuiltInRecipe(recipe.Id) : null;
            if (builtIn != null)
            {
                var current = recipe.Clone();
                current.FilePath = null;
                current.IsBuiltIn = builtIn.IsBuiltIn;
                current.IsOverridden = builtIn.IsOverridden;
                current.IsEnabled = builtIn.IsEnabled;
                details.BuiltInDiff = RecipeTextDiff.ToUnifiedText(RecipeSerializer.Serialize(builtIn), RecipeSerializer.Serialize(current));
            }
            return details;
        }

        /// <summary>
        /// Revokes the approval of a recipe from a file: it no longer runs (a built-in recipe it replaced comes back), and isn't
        /// loaded again until the user opens its file again. False when the recipe has no file.
        /// </summary>
        public bool RevokeApproval(string recipeId, string filePath = null)
        {
            filePath ??= GetRecipeById(recipeId)?.FilePath;
            if (string.IsNullOrEmpty(recipeId) || string.IsNullOrEmpty(filePath))
            {
                return false;
            }
            RecipeTrustStore.RevokeRecipeApproval(filePath, recipeId);
            UnregisterRecipe(recipeId);
            Log.InfoFormat("The user revoked the approval of recipe '{0}' from '{1}'.", recipeId, filePath);
            return true;
        }

        public RecipeValidationResult ReviewApproval(string recipeId)
        {
            var recipe = GetRecipeById(recipeId);
            if (string.IsNullOrEmpty(recipe?.FilePath))
            {
                return null;
            }
            // Shows the approval window with the current approval; what the user confirms replaces it
            return LoadRecipeFromFile(recipe.FilePath, interactiveApproval: true, forceApprovalPrompt: true);
        }

        public void ResetAllToDefault()
        {
            lock (_recipes)
            {
                var disabled = GetDisabledRecipeIds();
                foreach (var kvp in _builtInRecipes)
                {
                    // The file which replaced it isn't loaded again after a restart
                    if (_recipes.TryGetValue(kvp.Key, out var current) && current.IsOverridden)
                    {
                        RemoveRecipeFileFromConfig(current.FilePath);
                    }
                    var restored = kvp.Value.Clone();
                    restored.IsEnabled = !disabled.Contains(kvp.Key);
                    _recipes[kvp.Key] = restored;
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
                var disabled = GetDisabledRecipeIds();
                foreach (var kvp in _builtInRecipes)
                {
                    var restored = kvp.Value.Clone();
                    restored.IsEnabled = !disabled.Contains(kvp.Key);
                    _recipes[kvp.Key] = restored;
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

            recipe.IsEnabled = !GetDisabledRecipeIds().Contains(recipe.Id);

            lock (_recipes)
            {
                _recipes[recipe.Id] = recipe;
            }

            if (!string.IsNullOrEmpty(recipe.FilePath))
            {
                AddRecipeFileToConfig(recipe.FilePath);
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

                    string filePathToRemove = recipe.FilePath;

                    if (recipe.IsOverridden)
                    {
                        // Reset overridden recipe back to original built-in
                        var original = _builtInRecipes[recipeId].Clone();
                        original.IsEnabled = !GetDisabledRecipeIds().Contains(recipeId);
                        _recipes[recipeId] = original;
                        RemoveRecipeFileFromConfig(filePathToRemove);
                        Log.InfoFormat("Reverted overridden recipe '{0}' back to default definition.", recipeId);
                        NotifyRecipesChanged();
                        return true;
                    }

                    _recipes.Remove(recipeId);
                    RemoveRecipeFileFromConfig(filePathToRemove);
                    Log.InfoFormat("Unregistered recipe: {0}", recipeId);
                    NotifyRecipesChanged();
                    return true;
                }
            }

            return false;
        }

        public bool IsRecipeEnabled(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return false;
            lock (_recipes)
            {
                if (_recipes.TryGetValue(recipeId, out var recipe))
                {
                    return recipe.IsEnabled;
                }
            }
            return !GetDisabledRecipeIds().Contains(recipeId);
        }

        public void SetRecipeEnabled(string recipeId, bool enabled)
        {
            if (string.IsNullOrEmpty(recipeId)) return;

            lock (_recipes)
            {
                if (_recipes.TryGetValue(recipeId, out var recipe))
                {
                    recipe.IsEnabled = enabled;
                }
            }

            try
            {
                var recipeConfig = RecipeConfigHelper.TryGetRecipeConfiguration();
                if (recipeConfig != null)
                {
                    var disabled = GetDisabledRecipeIds();
                    if (enabled)
                    {
                        disabled.Remove(recipeId);
                    }
                    else
                    {
                        disabled.Add(recipeId);
                    }
                    recipeConfig.DisabledRecipeIds = string.Join(";", disabled);
                    IniConfigRegistry.Get()?.Save();
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to persist disabled state for recipe '{recipeId}'", ex);
            }

            Log.InfoFormat("Recipe '{0}' {1}.", recipeId, enabled ? "activated" : "deactivated");
            NotifyRecipesChanged();
        }
    }
}
