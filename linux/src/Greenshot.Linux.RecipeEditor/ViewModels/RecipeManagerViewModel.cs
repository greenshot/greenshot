using System;
#nullable disable

using Greenshot.Linux.Core.Languages;
using System.Windows.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Greenshot.Linux.Recipes;
using Greenshot.Linux.Recipes.Pipeline;
using Greenshot.Linux.Recipes.Triggers;

namespace Greenshot.Linux.RecipeEditor.ViewModels
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

        public string RecipeTypeBadge => IsOverridden ? Texts.Get("RecipeEditor", "badge_overridden") : (IsBuiltIn ? Texts.Get("RecipeEditor", "badge_built_in") : Texts.Get("RecipeEditor", "badge_custom"));

        /// <summary>
        /// The recipe file was written by an AI tool (and approved by the user)
        /// </summary>
        public bool IsAiCreated => !string.IsNullOrEmpty(IsExtension ? Extension.ProposedBy : Recipe.ProposedBy);

        public string AiBadge => IsAiCreated ? "🤖 " + string.Format(Texts.Get("RecipeEditor", "badge_ai_by"), Recipe.ProposedBy) : string.Empty;

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

        public string StatusBadgeText => IsEnabled ? Texts.Get("RecipeEditor", "badge_active") : Texts.Get("RecipeEditor", "badge_deactivated");

        public int StepCount => Recipe.Nodes?.Count ?? 0;

        public string TriggersSummary
        {
            get
            {
                if (IsExtension)
                {
                    int changed = _recipeManager?.GetAllRecipes().Count(r => RecipeComposer.CanExtend(Extension, r)) ?? 0;
                    return "🧩 " + string.Format(Texts.Get("RecipeEditor", "trigger_summary_extension"), Extension.SlotName, changed);
                }
                if (Recipe.Triggers == null || Recipe.Triggers.Count == 0)
                {
                    return Recipe.ShowInContextMenu ? "📋 " + Texts.Get("RecipeEditor", "trigger_summary_default_context_menu") : Texts.Get("RecipeEditor", "trigger_summary_none");
                }

                var list = new List<string>();
                foreach (var t in Recipe.Triggers.Where(t => t != null))
                {
                    string label = DescribeTrigger(t);
                    if (!t.Enabled)
                    {
                        label += " " + Texts.Get("RecipeEditor", "trigger_suffix_disabled");
                    }
                    else if (!t.IsApproved)
                    {
                        label += " " + Texts.Get("RecipeEditor", "trigger_suffix_not_approved");
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
                return string.IsNullOrWhiteSpace(hk) ? "⌨ " + Texts.Get("RecipeEditor", "trigger_hotkey") : $"⌨ {hk}";
            }
            if (Is(TriggerConfig.TypeContextMenu))
            {
                return "📋 " + string.Format(Texts.Get("RecipeEditor", "trigger_summary_context_menu"), t.GetParameter<string>("MenuItemText") ?? Recipe.Name);
            }
            if (Is(TriggerConfig.TypeEditor))
            {
                return "🎨 " + string.Format(Texts.Get("RecipeEditor", "trigger_summary_editor"), t.GetParameter<string>("MenuItemText") ?? Recipe.Name);
            }
            if (Is(TriggerConfig.TypeClipboard))
            {
                return "📋 " + Texts.Get("RecipeEditor", "trigger_clipboard_monitor");
            }
            if (Is(TriggerConfig.TypeCommandline))
            {
                string label = $"⌨ greenshot -r {t.GetParameter<string>("Command") ?? Recipe.Id}";
                if (t.GetParameter("AllowBrowserInvocation", false))
                {
                    label += t.IsBrowserInvocationApproved ? " " + Texts.Get("RecipeEditor", "trigger_suffix_web_pages") : " " + Texts.Get("RecipeEditor", "trigger_suffix_web_pages_not_approved");
                }
                return label;
            }
            if (Is(TriggerConfig.TypeAiTool))
            {
                return "🤖 " + string.Format(Texts.Get("RecipeEditor", "trigger_summary_ai_tool"), t.GetParameter<string>("ToolName") ?? t.Name);
            }
            if (Is(TriggerConfig.TypeOpenFile))
            {
                return "📂 " + Texts.Get("RecipeEditor", "trigger_open_file");
            }
            if (Is(TriggerConfig.TypeExtension))
            {
                return "🌐 " + Texts.Get("RecipeEditor", "trigger_browser_extension");
            }
            if (Is(TriggerConfig.TypeSchedule))
            {
                return "⏱ " + Texts.Get("RecipeEditor", "trigger_schedule");
            }
            if (Is(TriggerConfig.TypeManual))
            {
                return "▶ " + Texts.Get("RecipeEditor", "trigger_manual");
            }
            return t.Name ?? t.TriggerType;
        }

        public bool CanUnload => IsExtension ? HasFilePath : !IsBuiltIn || IsOverridden;
        public string UnloadButtonText => IsOverridden ? Texts.Get("RecipeEditor", "btn_reset_default") : Texts.Get("RecipeEditor", "btn_unload");
        public string UnloadToolTip => IsOverridden
            ? Texts.Get("RecipeEditor", "tooltip_reset_default")
            : Texts.Get("RecipeEditor", "tooltip_unload");

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
                ThemedMessageBox.Show(string.Join("\n", result.Errors), Texts.Get("RecipeEditor", "title_recipe_permissions"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            _onRecipeChanged?.Invoke();
        }

        private void ExecuteUnload()
        {
            if (!CanUnload) return;

            string confirmTitle = IsOverridden ? Texts.Get("RecipeEditor", "title_reset_recipe") : Texts.Get("RecipeEditor", "title_unload_recipe");
            string confirmMsg = IsOverridden
                ? string.Format(Texts.Get("RecipeEditor", "confirm_reset_recipe"), Name)
                : string.Format(Texts.Get("RecipeEditor", "confirm_unload_recipe"), Name);

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
                ThemedMessageBox.Show(Texts.Get("RecipeEditor", "error_test_run_validation") + "\n\n" + string.Join("\n", valResult.Errors), Texts.Get("RecipeEditor", "title_validation_error"), MessageBoxButton.OK, MessageBoxImage.Warning);
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
                ThemedMessageBox.Show(Texts.Get("RecipeEditor", "error_execution") + "\n" + ex.Message, Texts.Get("RecipeEditor", "title_execution_error"), MessageBoxButton.OK, MessageBoxImage.Error);
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
            _recipeManager = recipeManager ?? RecipeServices.Manager;
            _pipeline = pipeline ?? RecipeServices.Pipeline;
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
            if (ThemedMessageBox.Show(Texts.Get("RecipeEditor", "confirm_reset_all") + "\n\n" + string.Join("\n", replaced), Texts.Get("RecipeEditor", "title_reset_all"),
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
                Title = Texts.Get("RecipeEditor", "title_load_recipe")
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
                            ThemedMessageBox.Show(Texts.Get("RecipeEditor", "error_recipe_validation") + "\n\n" + string.Join("\n", valResult.Errors), Texts.Get("RecipeEditor", "title_validation_error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        _onSelectRecipeForEditor?.Invoke(recipe);
                        StatusMessage = string.Format(Texts.Get("RecipeEditor", "status_loaded"), Path.GetFileName(dlg.FileName));
                        RequestClose?.Invoke();
                        return;
                    }

                    var result = _recipeManager.LoadRecipeFromFile(dlg.FileName);
                    if (!result.IsValid)
                    {
                        ThemedMessageBox.Show(Texts.Get("Recipe", "import_failed") + "\n\n" + string.Join("\n", result.Errors), Texts.Get("RecipeEditor", "title_validation_error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                        StatusMessage = string.Format(Texts.Get("RecipeEditor", "status_validation_failed"), Path.GetFileName(dlg.FileName));
                    }
                    else
                    {
                        StatusMessage = string.Format(Texts.Get("RecipeEditor", "status_loaded_registered"), Path.GetFileName(dlg.FileName));
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
                    ThemedMessageBox.Show(Texts.Get("Recipe", "import_failed") + "\n" + ex.Message, Texts.Get("RecipeEditor", "title_error_loading_recipe"), MessageBoxButton.OK, MessageBoxImage.Error);
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
            StatusMessage = Texts.Get("RecipeEditor", "status_reloaded");
        }
    }
}
