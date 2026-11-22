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
using System.Threading;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Triggers;
using Greenshot.Recipes.Approval;
using Greenshot.Recipes.Steps;
using Greenshot.Recipes.Triggers;
using log4net;
using System.Threading.Tasks;
using Greenshot.Base.Threading;
using Greenshot.Helpers;

namespace Greenshot.Recipes
{
    /// <summary>
    /// Manages built-in and user-defined capture recipes.
    /// Supports overriding built-ins and loading custom recipes from explicitly configured JSON files.
    /// Unauthenticated directory auto-discovery is disabled for security.
    /// </summary>
    public partial class RecipeManager : IRecipeManager
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
        public const string RecipeIdOpenCaptureFolder = "recipe_open_capture_folder";
        public const string RecipeIdLastCaptureInEditor = "recipe_last_capture_in_editor";
        public const string RecipeIdTrayMenu = "recipe_tray_menu";
        public const string RecipeIdOpenSettings = "recipe_open_settings";
        public const string RecipeIdEmptyEditor = "recipe_empty_editor";
        public const string RecipeIdQuickSettings = "recipe_quick_settings";

        private readonly Dictionary<string, CaptureRecipe> _builtInRecipes = new Dictionary<string, CaptureRecipe>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CaptureRecipe> _recipes = new Dictionary<string, CaptureRecipe>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The recipe extensions; until extension files are supported only the built-in ones
        /// </summary>
        private readonly Dictionary<string, RecipeExtension> _extensions = new Dictionary<string, RecipeExtension>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The built-in extensions, to bring one back when a file which replaced it is dropped
        /// </summary>
        private readonly Dictionary<string, RecipeExtension> _builtInExtensions = new Dictionary<string, RecipeExtension>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The composed (effective) recipes by recipe id, with the recipe they were composed from: rebuilt when recipes or option values change
        /// </summary>
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, (CaptureRecipe Base, CaptureRecipe Composed)> _composed =
            new System.Collections.Concurrent.ConcurrentDictionary<string, (CaptureRecipe Base, CaptureRecipe Composed)>(StringComparer.OrdinalIgnoreCase);
        public event EventHandler RecipesChanged;

        // The recipe files: loading, approving, watching and saving them (RecipeManager.RecipeFiles.cs and
        // RecipeManager.ExtensionFiles.cs). Greenshot Light has only the built-in recipes, it leaves those files out and with them these calls.

        /// <summary>
        /// Loads the recipe files of the configuration
        /// </summary>
        /// <param name="interactive">false: no window is shown, a file which needs the user (approval, errors) is only added to <paramref name="needsUser"/></param>
        /// <param name="needsUser">Collects the files which couldn't be loaded without the user</param>
        partial void LoadConfiguredRecipeFiles(bool interactive, List<string> needsUser);

        /// <summary>
        /// The recipe file is used: it is watched and loaded at the next start
        /// </summary>
        partial void AddRecipeFileToConfig(string filePath);

        /// <summary>
        /// The recipe file isn't loaded at the next start anymore
        /// </summary>
        partial void RemoveRecipeFileFromConfig(string filePath);

        /// <summary>
        /// The trust store keeps the approvals of the configured recipe files only
        /// </summary>
        static partial void ForgetApprovalsOfUnusedFiles();

        /// <summary>
        /// Stops the watchers of folders which hold no recipe or automatic step file which is used
        /// </summary>
        partial void PruneFileWatchers();

        /// <summary>
        /// The approval of a recipe or automatic step from a file, for its details
        /// </summary>
        partial void AddApprovalDetails(RecipeDetails details, string filePath, string recipeId);

        // Thread-safe: the first access can come from the UI thread and an IPC or pipeline thread at the same time,
        // and a second instance would silently lose what was registered in the first one.
        private static readonly Lazy<RecipeManager> LazyInstance = new Lazy<RecipeManager>(() => new RecipeManager(), LazyThreadSafetyMode.ExecutionAndPublication);
        public static RecipeManager Instance => LazyInstance.Value;

        public RecipeManager()
        {
            InitializeDefaultRecipes();
            InitializeBuiltInExtensions();
            // The recipe files and the triggers are loaded by ReloadRecipes at startup, once the plugins registered their recipes and steps
            // An extension switched on or off, or set to other recipes, changes the composed recipes;
            // a Hotkey option changes the hotkey of the recipe
            RecipeOptionStore.ValuesChanged += (sender, args) => OnOptionValuesChanged(sender as string);
        }

        private void InitializeBuiltInExtensions()
        {
            foreach (var extension in BuiltInExtensions.Create())
            {
                lock (_extensions)
                {
                    _builtInExtensions[extension.Id] = extension;
                }
                // Nothing is composed yet: GetEffectiveRecipe composes a recipe when it is needed
                AddExtension(extension);
            }
        }

        /// <summary>
        /// Adds an extension after checking it; an invalid one is not added
        /// </summary>
        public RecipeValidationResult RegisterExtension(RecipeExtension extension)
        {
            var result = AddExtension(extension);
            if (result.IsValid)
            {
                RecomposeRecipes();
            }
            return result;
        }

        private RecipeValidationResult AddExtension(RecipeExtension extension)
        {
            var result = RecipeValidator.Validate(extension);
            if (extension?.Extends?.Recipes != null)
            {
                lock (_extensions)
                {
                    foreach (var target in extension.Extends.Recipes.Where(t => t != null && _extensions.ContainsKey(t.Trim()) && !string.Equals(t.Trim(), extension.Id, StringComparison.OrdinalIgnoreCase)))
                    {
                        result.AddError($"Extension '{extension.Id}' targets the extension '{target}'; an extension can't extend another extension.");
                    }
                }
            }
            if (!result.IsValid)
            {
                Log.Error($"Recipe extension '{extension?.Id}' is not used: {string.Join("; ", result.Errors)}");
                return result;
            }

            lock (_extensions)
            {
                _extensions[extension.Id] = extension;
            }
            return result;
        }

        public IReadOnlyList<RecipeExtension> GetAllExtensions()
        {
            lock (_extensions)
            {
                return _extensions.Values.ToList();
            }
        }

        public RecipeExtension GetExtensionById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            lock (_extensions)
            {
                return _extensions.TryGetValue(id, out var extension) ? extension : null;
            }
        }

        public CaptureRecipe GetEffectiveRecipe(CaptureRecipe recipe)
        {
            if (recipe == null || recipe.IsComposed) return recipe;
            if (recipe.Id != null && _composed.TryGetValue(recipe.Id, out var entry) && ReferenceEquals(entry.Base, recipe))
            {
                return entry.Composed;
            }

            var composed = Compose(recipe);
            if (recipe.Id != null)
            {
                _composed[recipe.Id] = (recipe, composed);
            }
            return composed;
        }

        /// <summary>
        /// The extensions which currently change the recipe (switched on, for this recipe)
        /// </summary>
        public IReadOnlyList<RecipeExtension> GetExtensionsChanging(string recipeId)
        {
            var recipe = GetRecipeById(recipeId);
            return recipe == null ? Array.Empty<RecipeExtension>() : GetEffectiveRecipe(recipe).AppliedExtensions ?? Array.Empty<RecipeExtension>();
        }

        /// <summary>
        /// Which recipes an extension changes, where and when, in plain words (for its approval)
        /// </summary>
        public IReadOnlyList<string> DescribeExtensionReach(RecipeExtension extension)
        {
            var lines = new List<string>();
            if (extension == null) return lines;

            var recipes = GetAllRecipes().Where(r => RecipeComposer.CanExtend(extension, r)).Select(r => r.DisplayName).OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase).ToList();
            lines.Add(recipes.Count == 0
                ? "• Changes no recipe now (none has a slot it fits in)."
                : $"• Changes these recipes now: {string.Join(", ", recipes)}.");

            var targets = extension.Extends?.Recipes ?? new List<string>();
            if (targets.Any(t => t?.Trim() == RecipeExtension.TargetAll))
            {
                lines.Add("• Also every recipe added later which has the slot (\"*\").");
            }
            else if (targets.Any(t => string.Equals(t?.Trim(), RecipeExtension.TargetCaptures, StringComparison.OrdinalIgnoreCase)))
            {
                lines.Add("• Also every capture recipe added later (\"*capture\").");
            }

            lines.Add(extension.SlotName switch
            {
                RecipeSlots.AfterCapture => "• Where: after the capture and the selection, before the processors.",
                RecipeSlots.BeforeExport => "• Where: once before the destinations, when the image is final.",
                RecipeSlots.AfterExport => "• Where: after the destinations.",
                RecipeSlots.BeforeDestination => "• Where: for each destination, on its own copy of the capture.",
                _ => $"• Where: the slot '{extension.Extends?.Slot}'."
            });

            if (!string.IsNullOrWhiteSpace(extension.When))
            {
                lines.Add($"• Only when: {extension.When}");
            }
            lines.Add(extension.EnabledOption != null
                ? "• Switched on and off in Settings > Recipes; there you can also limit it to some recipes and destinations."
                : "• It has no on/off switch: it is always used once approved. You can limit it in Settings > Recipes.");
            return lines;
        }

        /// <summary>
        /// What the settings and the recipe manager show about an extension: what it does, which recipes it changes, its approval
        /// </summary>
        private RecipeDetails GetExtensionDetails(RecipeExtension extension)
        {
            var details = new RecipeDetails
            {
                WhatItDoes = RecipeDescriber.DescribeSteps(extension.AsRecipeView()).Select(l => l.IsDetail ? "    " + l.Text : l.Text).ToList(),
                Reach = DescribeExtensionReach(extension),
                ProposedBy = extension.ProposedBy
            };
            AddApprovalDetails(details, extension.FilePath, extension.Id);
            return details;
        }

        private CaptureRecipe Compose(CaptureRecipe recipe)
        {
            try
            {
                var composed = RecipeComposer.Compose(recipe, GetAllExtensions(), RecipeExtensionSettings.FromStore);
                if (!ReferenceEquals(composed, recipe))
                {
                    Log.DebugFormat("Recipe '{0}' runs with the extension(s) {1}.", recipe.Id, string.Join(", ", composed.AppliedExtensions.Select(e => e.Id)));
                }
                return composed;
            }
            catch (Exception ex)
            {
                Log.Error($"Couldn't add the extensions to recipe '{recipe.Id}', it runs without them.", ex);
                return recipe;
            }
        }

        /// <summary>
        /// Composes the recipes again: after they were loaded and when an option value changed
        /// </summary>
        private void RecomposeRecipes()
        {
            _composed.Clear();
            foreach (var recipe in GetAllRecipes())
            {
                GetEffectiveRecipe(recipe);
            }
        }

        private void InitializeDefaultRecipes()
        {
            // Read the disabled recipes once, not per recipe
            var disabled = GetDisabledRecipeIds();

            // The capture recipes get the standard slots (AfterCapture, BeforeExport, BeforeDestination, AfterExport), so the extensions
            // (border, drop shadow, caption, ...) can add their steps; opening a file, OCR and the AI tools have none.

            // 1. Interactive Region Capture
            var regionRecipe = new CaptureRecipe(
                RecipeIdRegion,
                "Capture region",
                "Interactively select a region on the screen")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.Region))
                .AddNode(RecipeStepConfig.CreateSelection("select", CaptureMode.Region))
                .AddNode(CreateTextOrImage())
                .AddNode(RecipeStepConfig.CreateClipboard("copy_text", "TextOnly"))
                .AddNode(RecipeStepConfig.CreateFeedback("feedback"))
                .AddNode(RecipeStepConfig.CreateDestinations("export"))
                .AddNode(RecipeStepConfig.CreateNotification("notify"));
            regionRecipe.Flow = new RecipeFlowConfig("acquire")
                .AddTransition("acquire", "select")
                .AddTransition("select", "text_or_image")
                .AddConditionalTransition("text_or_image", "Text", "copy_text")
                .AddConditionalTransition("text_or_image", "Image", "feedback")
                .AddTransition("feedback", "export")
                .AddTransition("export", "notify");
            RecipeStepConfig.AddStandardSlots(regionRecipe, "export", "export");
            // The hotkeys of the general settings start the built-in recipes
            regionRecipe.AddTrigger(TriggerConfig.CreateHotkey("${config.Core.RegionHotkey}", "Region Hotkey"));
            // The items of the tray menu are ContextMenu triggers of the built-in recipes, see TrayMenu
            regionRecipe.AddTrigger(TriggerConfig.CreateMainMenu("Core.contextmenu_capturearea", 10, "contextmenu_capturearea.Image"));
            RegisterBuiltIn(regionRecipe, disabled);

            // 2. Interactive Window Capture
            var windowRecipe = new CaptureRecipe(
                RecipeIdWindow,
                "Capture window",
                "Interactively select a window on the screen")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.Window))
                .AddNode(RecipeStepConfig.CreateSelection("select", CaptureMode.Window))
                .AddNode(CreateTextOrImage())
                .AddNode(RecipeStepConfig.CreateClipboard("copy_text", "TextOnly"))
                .AddNode(RecipeStepConfig.CreateFeedback("feedback"))
                .AddNode(RecipeStepConfig.CreateDestinations("export"))
                .AddNode(RecipeStepConfig.CreateNotification("notify"));
            windowRecipe.Flow = new RecipeFlowConfig("acquire")
                .AddTransition("acquire", "select")
                .AddTransition("select", "text_or_image")
                .AddConditionalTransition("text_or_image", "Text", "copy_text")
                .AddConditionalTransition("text_or_image", "Image", "feedback")
                .AddTransition("feedback", "export")
                .AddTransition("export", "notify");
            RecipeStepConfig.AddStandardSlots(windowRecipe, "export", "export");
            // The window hotkey selects a window, or captures the active one (setting "Capture windows interactively")
            var windowHotkey = TriggerConfig.CreateHotkey("${config.Core.WindowHotkey}", "Window Hotkey");
            windowHotkey.When = "${config.Core.CaptureWindowsInteractive}";
            windowRecipe.AddTrigger(windowHotkey);
            windowRecipe.AddTrigger(TriggerConfig.CreateMainMenu("Core.contextmenu_capturewindow", 30, "contextmenu_capturewindow.Image"));
            RegisterBuiltIn(windowRecipe, disabled);

            // 3. Active Window Capture
            var activeWindowRecipe = new CaptureRecipe(
                RecipeIdActiveWindow,
                "Capture active window",
                "Directly capture the active window")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.ActiveWindow))
                .AddNode(RecipeStepConfig.CreateFeedback("feedback"))
                .AddNode(RecipeStepConfig.CreateDestinations("export"))
                .AddNode(RecipeStepConfig.CreateNotification("notify"));
            activeWindowRecipe.Flow = new RecipeFlowConfig("acquire")
                .AddTransition("acquire", "feedback")
                .AddTransition("feedback", "export")
                .AddTransition("export", "notify");
            RecipeStepConfig.AddStandardSlots(activeWindowRecipe, "export", "export");
            var activeWindowHotkey = TriggerConfig.CreateHotkey("${config.Core.WindowHotkey}", "Window Hotkey");
            activeWindowHotkey.When = "${config.Core.CaptureWindowsInteractive == false}";
            activeWindowRecipe.AddTrigger(activeWindowHotkey);
            // "Capture window from list": a sub menu with the open windows, the chosen one is the TargetWindow
            activeWindowRecipe.AddTrigger(TriggerConfig.CreateMainMenu("Core.contextmenu_capturewindowfromlist", 110, expand: TriggerConfig.ExpandWindows));
            RegisterBuiltIn(activeWindowRecipe, disabled);

            // 4. Full Screen Capture
            var fullScreenRecipe = new CaptureRecipe(
                RecipeIdFullScreen,
                "Capture full screen",
                "Capture the entire screen or monitor")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.FullScreen))
                .AddNode(RecipeStepConfig.CreateFeedback("feedback"))
                .AddNode(RecipeStepConfig.CreateDestinations("export"))
                .AddNode(RecipeStepConfig.CreateNotification("notify"));
            fullScreenRecipe.Flow = new RecipeFlowConfig("acquire")
                .AddTransition("acquire", "feedback")
                .AddTransition("feedback", "export")
                .AddTransition("export", "notify");
            RecipeStepConfig.AddStandardSlots(fullScreenRecipe, "export", "export");
            fullScreenRecipe.AddTrigger(TriggerConfig.CreateHotkey("${config.Core.FullscreenHotkey}", "Fullscreen Hotkey"));
            // With more than one display a sub menu with "All" and each display
            fullScreenRecipe.AddTrigger(TriggerConfig.CreateMainMenu("Core.contextmenu_capturefullscreen", 40, "contextmenu_capturefullscreen.Image", TriggerConfig.ExpandDisplays));
            RegisterBuiltIn(fullScreenRecipe, disabled);

            // 5. Last Region Capture
            var lastRegionRecipe = new CaptureRecipe(
                RecipeIdLastRegion,
                "Capture last region",
                "Re-capture the coordinates of the previous region")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.LastRegion))
                .AddNode(RecipeStepConfig.CreateFeedback("feedback"))
                .AddNode(RecipeStepConfig.CreateDestinations("export"))
                .AddNode(RecipeStepConfig.CreateNotification("notify"));
            lastRegionRecipe.Flow = new RecipeFlowConfig("acquire")
                .AddTransition("acquire", "feedback")
                .AddTransition("feedback", "export")
                .AddTransition("export", "notify");
            RecipeStepConfig.AddStandardSlots(lastRegionRecipe, "export", "export");
            lastRegionRecipe.AddTrigger(TriggerConfig.CreateHotkey("${config.Core.LastregionHotkey}", "Last Region Hotkey"));
            lastRegionRecipe.AddTrigger(TriggerConfig.CreateMainMenu("Core.contextmenu_capturelastregion", 20, "contextmenu_capturelastregion.Image"));
            RegisterBuiltIn(lastRegionRecipe, disabled);

            // 6. Clipboard Import
            var clipboardRecipe = new CaptureRecipe(
                RecipeIdClipboard,
                "Capture from clipboard",
                "Import and process image from system clipboard")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.Clipboard, captureMouse: false))
                // The destinations of the settings, like the other captures (1.3 always opened the editor)
                .AddNode(RecipeStepConfig.CreateDestinations("export"))
                .AddNode(RecipeStepConfig.CreateNotification("notify"));
            clipboardRecipe.Flow = new RecipeFlowConfig("acquire")
                .AddTransition("acquire", "export")
                .AddTransition("export", "notify");
            RecipeStepConfig.AddStandardSlots(clipboardRecipe, "export", "export");
            clipboardRecipe.AddTrigger(TriggerConfig.CreateHotkey("${config.Core.ClipboardHotkey}", "Clipboard Hotkey"));
            clipboardRecipe.AddTrigger(TriggerConfig.CreateMainMenu("Core.contextmenu_captureclipboard", 210, "contextmenu_captureclipboard.Image"));
            RegisterBuiltIn(clipboardRecipe, disabled);

            // 7. File Import
            var fileRecipe = new CaptureRecipe(
                RecipeIdFile,
                "Open file",
                "Import an image or .greenshot file from disk")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.File, captureMouse: false))
                .AddNode(RecipeStepConfig.CreateDestinations("export", new[] { "Editor" }))
                .AddTrigger(TriggerConfig.CreateOpenFile(name: "Default Open With File Trigger"))
                // Started from the menu (or a tray icon click) the source asks for the file
                .AddTrigger(TriggerConfig.CreateMainMenu("Core.contextmenu_openfile", 220, "contextmenu_openfile.Image"));
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
                .AddNode(RecipeStepConfig.CreateClipboard("export", "TextOnly"));
            ocrRecipe.Flow = new RecipeFlowConfig("acquire")
                .AddTransition("acquire", "select")
                .AddTransition("select", "feedback")
                .AddTransition("feedback", "export");
            RegisterBuiltIn(ocrRecipe, disabled);

            InitializeShellRecipes(disabled);

            // The optional parts (browser extension, AI tools), Greenshot Light has none of them
            foreach (var provider in GreenshotModules.Create<IBuiltInRecipeProvider>())
            {
                foreach (var recipe in provider.CreateRecipes())
                {
                    RegisterBuiltIn(recipe, disabled);
                }
            }
        }

        /// <summary>
        /// The built-in recipes without a capture, for the tray menu and the clicks on the tray icon (Settings > General).
        /// Their names are language keys, they are shown translated.
        /// </summary>
        private void InitializeShellRecipes(ISet<string> disabled)
        {
            // 9. Open the folder of the last capture, the default of a double click on the tray icon
            var openFolderRecipe = new CaptureRecipe(
                RecipeIdOpenCaptureFolder,
                "Core.contextmenu_openrecentcapture",
                "Shows the last capture in the Windows Explorer, or the folder of the screenshots")
                .AddNode(new RecipeNodeConfig("show", WellKnownStepTypes.ShowInExplorer, "Show in Explorer"))
                .AddTrigger(TriggerConfig.CreateMainMenu("Core.contextmenu_openrecentcapture", 310));
            openFolderRecipe.Flow = new RecipeFlowConfig("show");
            RegisterBuiltIn(openFolderRecipe, disabled);

            // 10. Open the last capture in the editor
            var lastCaptureRecipe = new CaptureRecipe(
                RecipeIdLastCaptureInEditor,
                "Core.contextmenu_openlastcaptureineditor",
                "Opens the file of the last capture in the editor")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.File, captureMouse: false)
                    .Set("Filename", "${config.Core.OutputFileAsFullpath}"))
                .AddNode(RecipeStepConfig.CreateEditor("edit"));
            lastCaptureRecipe.Flow = new RecipeFlowConfig("acquire")
                .AddTransition("acquire", "edit");
            RegisterBuiltIn(lastCaptureRecipe, disabled);

            // 11. - 13. Open a window: the tray menu (the default of a click on the tray icon), the settings, an empty editor
            RegisterBuiltIn(CreateOpenWindowRecipe(RecipeIdTrayMenu, "Core.contextmenu_showtraymenu", "Opens the tray menu at the mouse", OpenWindowStep.TrayMenu), disabled);
            RegisterBuiltIn(CreateOpenWindowRecipe(RecipeIdOpenSettings, "Core.contextmenu_opensettings", "Opens the settings", OpenWindowStep.Settings), disabled);
            RegisterBuiltIn(CreateOpenWindowRecipe(RecipeIdEmptyEditor, "Core.contextmenu_openemptyeditor", "Opens an empty image in the editor", OpenWindowStep.EmptyEditor), disabled);
            // 14. The quick settings in a window, for a hotkey or a click on the tray icon
            RegisterBuiltIn(CreateOpenWindowRecipe(RecipeIdQuickSettings, "Core.contextmenu_quicksettings", "Opens the quick settings in a window", OpenWindowStep.QuickSettings), disabled);
        }

        private static CaptureRecipe CreateOpenWindowRecipe(string id, string name, string description, string window)
        {
            var recipe = new CaptureRecipe(id, name, description)
                .AddNode(new RecipeNodeConfig("open", WellKnownStepTypes.OpenWindow, "Open window")
                    .Set(OpenWindowStep.WindowParameter, window));
            recipe.Flow = new RecipeFlowConfig("open");
            return recipe;
        }

        /// <summary>
        /// After the selection of the region and window captures: text selected in the text mode (T) goes to the clipboard as text,
        /// everything else to the destinations
        /// </summary>
        private static RecipeNodeConfig CreateTextOrImage()
        {
            var node = RecipeStepConfig.CreateConditional("text_or_image", new[]
            {
                new KeyValuePair<string, string>("Text", "${var.SelectedMode == 'Text'}"),
                new KeyValuePair<string, string>("Image", "else")
            });
            node.Name = "Text or image";
            return node;
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

        private void RegisterBuiltIn(CaptureRecipe recipe, ISet<string> disabledRecipeIds)
        {
            recipe.IsBuiltIn = true;
            recipe.IsOverridden = false;
            recipe.IsEnabled = !disabledRecipeIds.Contains(recipe.Id);
            _builtInRecipes[recipe.Id] = recipe.Clone();
            _recipes[recipe.Id] = recipe;
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

        private void OnOptionValuesChanged(string recipeId)
        {
            RecomposeRecipes();
            // Register the triggers again when the recipe takes a hotkey or a trigger's 'when' from an option
            var recipe = GetRecipeById(recipeId);
            bool triggersUseOptions = recipe?.Triggers?.Any(t => t != null &&
                (RecipeValidator.FindOptionReferences(t.When).Count > 0 ||
                 string.Equals(t.TriggerType, TriggerConfig.TypeHotkey, StringComparison.OrdinalIgnoreCase) &&
                 RecipeOptionStore.TryParseReference(t.GetParameter<string>(TriggerConfig.HotkeyParameter), out _))) ?? false;
            if (triggersUseOptions)
            {
                var triggerManager = SimpleServiceProvider.Current.GetInstance<Greenshot.Base.Recipes.Triggers.ITriggerManager>(isOptional: true) as TriggerManager ?? TriggerManager.Instance;
                triggerManager?.SyncRecipeTriggers(GetAllRecipes());
            }
        }

        private void NotifyRecipesChanged()
        {
            PruneFileWatchers();
            RecomposeRecipes();
            var triggerManager = SimpleServiceProvider.Current.GetInstance<Greenshot.Base.Recipes.Triggers.ITriggerManager>(isOptional: true) as TriggerManager ?? TriggerManager.Instance;
            triggerManager?.SyncRecipeTriggers(GetAllRecipes());
            RecipesChanged?.Invoke(this, EventArgs.Empty);
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
                return GetExtensionById(recipeId) is RecipeExtension extension ? GetExtensionDetails(extension) : null;
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
                ProposedBy = recipe.ProposedBy,
                ChangedBy = GetExtensionsChanging(recipe.Id).Select(e => e.DisplayName).ToList()
            };

            AddApprovalDetails(details, recipe.FilePath, recipe.Id);

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
            ReloadRecipes(interactive: true);
        }

        /// <summary>
        /// Loads the recipes at startup without showing a window: the files which need the user (a changed file to approve, an error
        /// to show) are returned, to be loaded with <c>LoadRecipeFilesInteractively</c> (not in Greenshot Light, which has no recipe files) once Greenshot has started
        /// </summary>
        /// <returns>The recipe files which weren't loaded (or only partly)</returns>
        public IReadOnlyList<string> LoadRecipesAtStartup()
        {
            return ReloadRecipes(interactive: false);
        }

        private IReadOnlyList<string> ReloadRecipes(bool interactive)
        {
            var needsUser = new List<string>();
            lock (_recipes)
            {
                _recipes.Clear();
                var disabled = GetDisabledRecipeIds();
                // The plugins registered their recipes after the RecipeManager was created
                foreach (var recipe in SimpleServiceProvider.Current.GetAllInstances<IBuiltInRecipeProvider>().SelectMany(provider => provider.CreateRecipes()))
                {
                    RegisterBuiltIn(recipe, disabled);
                }
                foreach (var kvp in _builtInRecipes)
                {
                    var restored = kvp.Value.Clone();
                    restored.IsEnabled = !disabled.Contains(kvp.Key);
                    _recipes[kvp.Key] = restored;
                }
            }
            // The automatic steps from files are loaded again too: one removed from the configuration is gone, a built-in one it replaced comes back
            lock (_extensions)
            {
                foreach (var extensionId in _extensions.Values.Where(e => !string.IsNullOrEmpty(e.FilePath)).Select(e => e.Id).ToList())
                {
                    _extensions.Remove(extensionId);
                    if (_builtInExtensions.TryGetValue(extensionId, out var builtIn))
                    {
                        _extensions[extensionId] = builtIn;
                    }
                }
            }
            LoadConfiguredRecipeFiles(interactive, needsUser);
            ForgetApprovalsOfUnusedFiles();
            NotifyRecipesChanged();
            return needsUser;
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

        /// <summary>
        /// The hotkeys of the recipes which are on, as Settings > General and Self-Service show them: one per hotkey of the
        /// general settings ("${config.Core.RegionHotkey}", the two window recipes share one), one per Hotkey option and one per
        /// hotkey written in a recipe file. A trigger whose 'when' is false doesn't count.
        /// </summary>
        public IReadOnlyList<RecipeHotkey> GetActiveHotkeys()
        {
            var hotkeys = new List<RecipeHotkey>();
            foreach (var recipe in GetAllRecipes().Where(r => r.IsEnabled && r.Triggers != null))
            {
                foreach (var trigger in recipe.Triggers.Where(t => t != null &&
                             string.Equals(t.TriggerType, TriggerConfig.TypeHotkey, StringComparison.OrdinalIgnoreCase) && t.IsWanted(recipe)))
                {
                    string parameter = trigger.GetParameter<string>(TriggerConfig.HotkeyParameter);
                    var hotkey = new RecipeHotkey { Recipe = recipe, Hotkey = trigger.GetHotkey(recipe) };
                    if (TriggerConfig.TryParseConfigReference(parameter, out var settingName))
                    {
                        if (hotkeys.Any(h => string.Equals(h.SettingName, settingName, StringComparison.OrdinalIgnoreCase))) continue;
                        hotkey.SettingName = settingName;
                    }
                    else if (RecipeOptionStore.TryParseReference(parameter, out var optionKey))
                    {
                        hotkey.Option = recipe.FindOption(optionKey);
                    }
                    hotkeys.Add(hotkey);
                }
            }
            return hotkeys;
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
