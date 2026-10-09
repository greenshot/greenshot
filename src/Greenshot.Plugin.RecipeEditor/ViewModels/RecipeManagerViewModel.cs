using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Greenshot.Base.Core;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Recipes.Triggers;
using Greenshot.Base.Wpf;
using Microsoft.Win32;
using Greenshot.Base.Threading;

namespace Greenshot.Plugin.RecipeEditor.ViewModels
{
    public class RecipeManagerItemViewModel : ViewModelBase
    {
        private readonly IRecipeManager _recipeManager;
        private readonly ICapturePipeline _pipeline;
        private readonly Action<CaptureRecipe> _onSelectInEditor;
        private readonly Action _onRecipeChanged;

        public CaptureRecipe Recipe { get; }

        /// <summary>
        /// An automatic step (recipe extension); <see cref="Recipe"/> is then its view as a recipe without triggers
        /// </summary>
        public RecipeExtension Extension { get; }

        public bool IsExtension => Extension != null;

        /// <summary>
        /// An automatic step in the list: switched with its on/off option, unloaded from its file
        /// </summary>
        public RecipeManagerItemViewModel(
            RecipeExtension extension,
            IRecipeManager recipeManager,
            ICapturePipeline pipeline,
            Action<RecipeExtension> onEditExtension,
            Action onRecipeChanged)
            : this((extension ?? throw new ArgumentNullException(nameof(extension))).AsRecipeView(), recipeManager, pipeline, null, onRecipeChanged)
        {
            Extension = extension;
            EditCommand = new RelayCommand(() => onEditExtension?.Invoke(extension), () => onEditExtension != null);
            TestRunCommand = new RelayCommand(() => { }, () => false);
        }

        public RecipeManagerItemViewModel(
            CaptureRecipe recipe,
            IRecipeManager recipeManager,
            ICapturePipeline pipeline,
            Action<CaptureRecipe> onSelectInEditor,
            Action onRecipeChanged)
        {
            Recipe = recipe ?? throw new ArgumentNullException(nameof(recipe));
            _recipeManager = recipeManager;
            _pipeline = pipeline;
            _onSelectInEditor = onSelectInEditor;
            _onRecipeChanged = onRecipeChanged;

            ToggleActiveCommand = new RelayCommand(() => IsEnabled = !IsEnabled);
            EditCommand = new RelayCommand(() => _onSelectInEditor?.Invoke(Recipe));
            UnloadCommand = new RelayCommand(ExecuteUnload, () => CanUnload);
            TestRunCommand = new RelayCommand(() => AsyncCommand.Run(ExecuteTestRunAsync, "Recipe test run"));
            ReviewApprovalCommand = new RelayCommand(ExecuteReviewApproval, () => HasFilePath);
            DetailsCommand = new RelayCommand(ExecuteShowDetails);
        }

        public string Id => Recipe.Id;
        public string Version => string.IsNullOrWhiteSpace(Recipe.Version) ? "1.0" : Recipe.Version;
        public string FilePath => IsExtension ? Extension.FilePath : Recipe.FilePath;
        public bool HasFilePath => !string.IsNullOrEmpty(FilePath);

        public bool IsBuiltIn => IsExtension ? Extension.IsBuiltIn : Recipe.IsBuiltIn;
        public bool IsOverridden => IsExtension ? Extension.IsOverridden : Recipe.IsOverridden;

        public string RecipeTypeBadge => IsOverridden ? "OVERRIDDEN" : (IsBuiltIn ? "BUILT-IN" : "CUSTOM");

        /// <summary>
        /// The recipe file was written by an AI tool (and approved by the user)
        /// </summary>
        public bool IsAiCreated => !string.IsNullOrEmpty(IsExtension ? Extension.ProposedBy : Recipe.ProposedBy);

        public string AiBadge => IsAiCreated ? $"🤖 BY {Recipe.ProposedBy}" : string.Empty;

        public string Name => IsExtension ? RecipeText.Translate(Extension.Name ?? Extension.Id) : Recipe.Name ?? Recipe.Id;
        public string Description => IsExtension ? RecipeText.Translate(Extension.Description) ?? "" : Recipe.Description ?? "";

        /// <summary>
        /// Triggers which are in the recipe but not switched on in its approval
        /// </summary>
        public bool HasUnapprovedTriggers => Recipe.Triggers?.Any(t => t != null && t.Enabled && !t.IsApproved) ?? false;

        public bool IsEnabled
        {
            get => IsExtension ? Extension.EnabledOption == null || RecipeOptionStore.GetValue(Extension, Extension.EnabledOption) is true : Recipe.IsEnabled;
            set
            {
                if (IsExtension)
                {
                    // The same switch as in Settings > Recipes and the quick settings
                    if (Extension.EnabledOption != null && IsEnabled != value)
                    {
                        RecipeOptionStore.SetValue(Extension.Id, Extension.EnabledOption, value);
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(StatusBadgeText));
                        _onRecipeChanged?.Invoke();
                    }
                    return;
                }
                if (Recipe.IsEnabled != value)
                {
                    Recipe.IsEnabled = value;
                    _recipeManager?.SetRecipeEnabled(Recipe.Id, value);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusBadgeText));
                    _onRecipeChanged?.Invoke();
                }
            }
        }

        public string StatusBadgeText => IsEnabled ? "ACTIVE" : "DEACTIVATED";

        public int StepCount => Recipe.Nodes?.Count ?? 0;

        public string TriggersSummary
        {
            get
            {
                if (IsExtension)
                {
                    int changed = _recipeManager?.GetAllRecipes().Count(r => RecipeComposer.CanExtend(Extension, r)) ?? 0;
                    return $"🧩 Automatic step at {Extension.SlotName}, fits {changed} recipe(s)";
                }
                if (Recipe.Triggers == null || Recipe.Triggers.Count == 0)
                {
                    return Recipe.ShowInContextMenu ? "📋 Systray (Default)" : "No triggers (Editor/DAG only)";
                }

                var list = new List<string>();
                foreach (var t in Recipe.Triggers.Where(t => t != null))
                {
                    string label = DescribeTrigger(t);
                    if (!t.Enabled)
                    {
                        label += " (disabled)";
                    }
                    else if (!t.IsApproved)
                    {
                        label += " (off, not approved)";
                    }
                    list.Add(label);
                }

                return string.Join("  •  ", list);
            }
        }

        private string DescribeTrigger(TriggerConfig t)
        {
            string type = t.TriggerType ?? string.Empty;
            bool Is(string triggerType) => string.Equals(type, triggerType, StringComparison.OrdinalIgnoreCase);
            if (Is(TriggerConfig.TypeHotkey))
            {
                string hk = t.GetParameter<string>("Hotkey");
                return string.IsNullOrWhiteSpace(hk) ? "⌨ Hotkey" : $"⌨ {hk}";
            }
            if (Is(TriggerConfig.TypeContextMenu))
            {
                return $"📋 Systray (\"{t.GetParameter<string>("MenuItemText") ?? Recipe.Name}\")";
            }
            if (Is(TriggerConfig.TypeEditor))
            {
                return $"🎨 Editor (\"{t.GetParameter<string>("MenuItemText") ?? Recipe.Name}\")";
            }
            if (Is(TriggerConfig.TypeClipboard))
            {
                return "📋 Clipboard Monitor";
            }
            if (Is(TriggerConfig.TypeCommandline))
            {
                string label = $"⌨ greenshot-cli.exe run {t.GetParameter<string>("Command") ?? Recipe.Id}";
                if (t.GetParameter("AllowBrowserInvocation", false))
                {
                    label += t.IsBrowserInvocationApproved ? " (+ web pages)" : " (web pages: not approved)";
                }
                return label;
            }
            if (Is(TriggerConfig.TypeAiTool))
            {
                return $"🤖 AI tool \"{t.GetParameter<string>("ToolName") ?? t.Name}\"";
            }
            if (Is(TriggerConfig.TypeOpenFile))
            {
                return "📂 Open file";
            }
            if (Is(TriggerConfig.TypeExtension))
            {
                return "🌐 Browser extension";
            }
            if (Is(TriggerConfig.TypeSchedule))
            {
                return "⏱ Schedule";
            }
            if (Is(TriggerConfig.TypeManual))
            {
                return "▶ Manual";
            }
            return t.Name ?? t.TriggerType;
        }

        public bool CanUnload => IsExtension ? HasFilePath : !IsBuiltIn || IsOverridden;
        public string UnloadButtonText => IsOverridden ? "Reset Default" : "Unload";
        public string UnloadToolTip => IsOverridden
            ? "Revert overridden recipe back to original default definition"
            : "Unload and unregister this custom recipe from Greenshot";

        public ICommand ToggleActiveCommand { get; }
        public ICommand EditCommand { get; private set; }
        public ICommand UnloadCommand { get; }
        public ICommand TestRunCommand { get; private set; }

        /// <summary>
        /// Shows the approval of the recipe file again: which triggers are switched on and which permissions are given
        /// </summary>
        public ICommand ReviewApprovalCommand { get; }

        /// <summary>
        /// Shows what the recipe does, its triggers, its approval and the changes against the built-in recipe it replaces
        /// </summary>
        public ICommand DetailsCommand { get; }

        private void ExecuteShowDetails()
        {
            _recipeManager?.ShowRecipeDetails(Recipe.Id);
        }

        private void ExecuteReviewApproval()
        {
            if (!HasFilePath || _recipeManager == null) return;
            var result = _recipeManager.ReviewApproval(Recipe.Id);
            if (result != null && !result.IsValid)
            {
                ThemedMessageBox.Show(string.Join("\n", result.Errors), "Recipe Permissions", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            _onRecipeChanged?.Invoke();
        }

        private void ExecuteUnload()
        {
            if (!CanUnload) return;

            string confirmTitle = IsOverridden ? "Reset Recipe to Default" : "Unload Recipe";
            string confirmMsg = IsOverridden
                ? $"Are you sure you want to revert '{Name}' back to its default built-in definition?"
                : $"Are you sure you want to unload '{Name}'? It will be unregistered from Greenshot.";

            if (ThemedMessageBox.Show(confirmMsg, confirmTitle, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            if (IsExtension)
            {
                _recipeManager?.UnregisterExtension(Extension.Id);
            }
            else if (IsOverridden)
            {
                _recipeManager?.ResetToDefault(Recipe.Id);
            }
            else
            {
                _recipeManager?.UnregisterRecipe(Recipe.Id);
            }

            _onRecipeChanged?.Invoke();
        }

        private async Task ExecuteTestRunAsync()
        {
            var valResult = RecipeValidator.Validate(Recipe);
            if (!valResult.IsValid)
            {
                ThemedMessageBox.Show($"Cannot test run recipe. Fix validation errors first:\n\n{string.Join("\n", valResult.Errors)}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var result = await TestRun.RunAsync(Recipe);
                string error = TestRun.ErrorOf(result);
                if (error != null)
                {
                    throw new InvalidOperationException(error, result.Error);
                }
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show($"Recipe execution encountered an error:\n{ex.Message}", "Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class RecipeManagerViewModel : ViewModelBase
    {
        private readonly IRecipeManager _recipeManager;
        private readonly ICapturePipeline _pipeline;
        private readonly Action<CaptureRecipe> _onSelectRecipeForEditor;
        private readonly Action _onNewRecipeRequested;
        private string _searchText = "";
        private string _selectedFilterCategory = "All";
        private RecipeManagerItemViewModel _selectedRecipe;
        private string _statusMessage = "";

        public ObservableCollection<RecipeManagerItemViewModel> AllRecipes { get; } = new ObservableCollection<RecipeManagerItemViewModel>();
        public ObservableCollection<RecipeManagerItemViewModel> FilteredRecipes { get; } = new ObservableCollection<RecipeManagerItemViewModel>();

        /// <summary>
        /// The automatic steps (recipe extensions), a group of their own under the recipes
        /// </summary>
        public ObservableCollection<RecipeManagerItemViewModel> AllExtensions { get; } = new ObservableCollection<RecipeManagerItemViewModel>();
        public ObservableCollection<RecipeManagerItemViewModel> FilteredExtensions { get; } = new ObservableCollection<RecipeManagerItemViewModel>();

        public bool HasFilteredExtensions => FilteredExtensions.Count > 0;

        private readonly Action<RecipeExtension> _onEditExtension;

        public event Action RequestClose;

        public RecipeManagerViewModel(
            IRecipeManager recipeManager = null,
            ICapturePipeline pipeline = null,
            Action<CaptureRecipe> onSelectRecipeForEditor = null,
            Action onNewRecipeRequested = null,
            Action<RecipeExtension> onEditExtension = null)
        {
            _onEditExtension = onEditExtension;
            _recipeManager = recipeManager ?? SimpleServiceProvider.Current?.GetInstance<IRecipeManager>(isOptional: true);
            _pipeline = pipeline ?? SimpleServiceProvider.Current?.GetInstance<ICapturePipeline>(isOptional: true);
            _onSelectRecipeForEditor = onSelectRecipeForEditor;
            _onNewRecipeRequested = onNewRecipeRequested;

            LoadRecipeFromFileCommand = new RelayCommand(ExecuteLoadRecipeFromFile);
            CreateNewRecipeCommand = new RelayCommand(ExecuteCreateNewRecipe);
            ReloadAllCommand = new RelayCommand(ExecuteReloadAll);
            ResetAllCommand = new RelayCommand(ExecuteResetAll, () => AllRecipes.Any(r => r.IsOverridden));
            CloseCommand = new RelayCommand(() => RequestClose?.Invoke());

            LoadRecipes();
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetField(ref _searchText, value))
                {
                    ApplyFilter();
                }
            }
        }

        public string SelectedFilterCategory
        {
            get => _selectedFilterCategory;
            set
            {
                if (SetField(ref _selectedFilterCategory, value))
                {
                    ApplyFilter();
                }
            }
        }

        public RecipeManagerItemViewModel SelectedRecipe
        {
            get => _selectedRecipe;
            set => SetField(ref _selectedRecipe, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetField(ref _statusMessage, value);
        }

        public int TotalCount => AllRecipes.Count;
        public int ActiveCount => AllRecipes.Count(r => r.IsEnabled);
        public int DeactivatedCount => AllRecipes.Count(r => !r.IsEnabled);

        public ICommand LoadRecipeFromFileCommand { get; }
        public ICommand CreateNewRecipeCommand { get; }
        public ICommand ReloadAllCommand { get; }

        /// <summary>
        /// Brings back every built-in recipe a file replaces
        /// </summary>
        public ICommand ResetAllCommand { get; }

        private void ExecuteResetAll()
        {
            var replaced = AllRecipes.Where(r => r.IsOverridden).Select(r => r.Name).ToList();
            if (replaced.Count == 0) return;
            if (ThemedMessageBox.Show($"Bring back the built-in version of these recipes?\n\n{string.Join("\n", replaced)}", "Reset All to Default",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }
            _recipeManager?.ResetAllToDefault();
            LoadRecipes();
        }
        public ICommand CloseCommand { get; }

        public void LoadRecipes()
        {
            AllRecipes.Clear();
            var recipes = _recipeManager?.GetAllRecipes() ?? Array.Empty<CaptureRecipe>();

            foreach (var recipe in recipes)
            {
                var item = new RecipeManagerItemViewModel(
                    recipe,
                    _recipeManager,
                    _pipeline,
                    r =>
                    {
                        _onSelectRecipeForEditor?.Invoke(r);
                        RequestClose?.Invoke();
                    },
                    () =>
                    {
                        LoadRecipes();
                    });

                AllRecipes.Add(item);
            }

            AllExtensions.Clear();
            foreach (var extension in (_recipeManager?.GetAllExtensions() ?? Array.Empty<RecipeExtension>()).OrderBy(e => e.Extends?.Order ?? 0).ThenBy(e => e.Id, StringComparer.OrdinalIgnoreCase))
            {
                AllExtensions.Add(new RecipeManagerItemViewModel(
                    extension,
                    _recipeManager,
                    _pipeline,
                    _onEditExtension == null ? (Action<RecipeExtension>)null : e =>
                    {
                        _onEditExtension(e);
                        RequestClose?.Invoke();
                    },
                    LoadRecipes));
            }

            ApplyFilter();
            UpdateStats();
        }

        private void ApplyFilter()
        {
            FilteredRecipes.Clear();
            FilteredExtensions.Clear();
            string query = _searchText?.Trim() ?? "";

            foreach (var item in AllRecipes.Concat(AllExtensions))
            {
                // Category filter
                bool categoryMatch = _selectedFilterCategory switch
                {
                    "Active" => item.IsEnabled,
                    "Deactivated" => !item.IsEnabled,
                    "Built-in" => item.IsBuiltIn && !item.IsOverridden,
                    "Custom" => !item.IsBuiltIn || item.IsOverridden,
                    "AI" => item.IsAiCreated,
                    _ => true
                };

                if (!categoryMatch) continue;

                // Search query filter
                if (!string.IsNullOrEmpty(query))
                {
                    bool queryMatch = item.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                      item.Id.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                      item.Description.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                      item.TriggersSummary.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                      (item.FilePath != null && item.FilePath.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);

                    if (!queryMatch) continue;
                }

                (item.IsExtension ? FilteredExtensions : FilteredRecipes).Add(item);
            }
            OnPropertyChanged(nameof(HasFilteredExtensions));

            if (SelectedRecipe == null && FilteredRecipes.Count > 0)
            {
                SelectedRecipe = FilteredRecipes[0];
            }
        }

        private void UpdateStats()
        {
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(ActiveCount));
            OnPropertyChanged(nameof(DeactivatedCount));
        }

        private void ExecuteLoadRecipeFromFile()
        {
            var dlg = new OpenFileDialog
            {
                Filter = RecipeSerializer.RecipeFileFilter,
                Title = "Load Capture Recipe into Greenshot"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    if (_recipeManager == null)
                    {
                        var recipe = RecipeSerializer.LoadFromFile(dlg.FileName);
                        recipe.FilePath = dlg.FileName;
                        var valResult = RecipeValidator.Validate(recipe);
                        if (!valResult.IsValid)
                        {
                            ThemedMessageBox.Show($"Recipe validation failed:\n\n{string.Join("\n", valResult.Errors)}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        _onSelectRecipeForEditor?.Invoke(recipe);
                        StatusMessage = $"Loaded {Path.GetFileName(dlg.FileName)}";
                        RequestClose?.Invoke();
                        return;
                    }

                    var result = _recipeManager.LoadRecipeFromFile(dlg.FileName);
                    if (!result.IsValid)
                    {
                        ThemedMessageBox.Show($"Failed to load recipe:\n\n{string.Join("\n", result.Errors)}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        StatusMessage = $"Validation failed for {Path.GetFileName(dlg.FileName)}";
                    }
                    else
                    {
                        StatusMessage = $"Loaded and registered {Path.GetFileName(dlg.FileName)}";
                        LoadRecipes();

                        // Select the newly loaded recipe in the list
                        var newlyLoaded = AllRecipes.FirstOrDefault(r => string.Equals(r.FilePath, dlg.FileName, StringComparison.OrdinalIgnoreCase));
                        if (newlyLoaded != null)
                        {
                            SelectedRecipe = newlyLoaded;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ThemedMessageBox.Show($"Failed to load recipe:\n{ex.Message}", "Error Loading Recipe", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteCreateNewRecipe()
        {
            _onNewRecipeRequested?.Invoke();
            RequestClose?.Invoke();
        }

        private void ExecuteReloadAll()
        {
            _recipeManager?.ReloadRecipes();
            LoadRecipes();
            StatusMessage = "Reloaded recipes from disk.";
        }
    }
}
