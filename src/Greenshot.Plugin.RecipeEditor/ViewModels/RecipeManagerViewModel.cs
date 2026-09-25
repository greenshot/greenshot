using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Greenshot.Base.Wpf;
using Microsoft.Win32;

namespace Greenshot.Plugin.RecipeEditor.ViewModels
{
    public class RecipeManagerItemViewModel : ViewModelBase
    {
        private readonly IRecipeManager _recipeManager;
        private readonly ICapturePipeline _pipeline;
        private readonly Action<CaptureRecipe> _onSelectInEditor;
        private readonly Action _onRecipeChanged;

        public CaptureRecipe Recipe { get; }

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
            TestRunCommand = new RelayCommand(async () => await ExecuteTestRunAsync());
        }

        public string Id => Recipe.Id;
        public string Name => Recipe.Name ?? Recipe.Id;
        public string Version => string.IsNullOrWhiteSpace(Recipe.Version) ? "1.0" : Recipe.Version;
        public string Description => Recipe.Description ?? "";
        public string FilePath => Recipe.FilePath;
        public bool HasFilePath => !string.IsNullOrEmpty(Recipe.FilePath);

        public bool IsBuiltIn => Recipe.IsBuiltIn;
        public bool IsOverridden => Recipe.IsOverridden;
        public bool IsCustom => !Recipe.IsBuiltIn;

        public string RecipeTypeBadge => IsOverridden ? "OVERRIDDEN" : (IsBuiltIn ? "BUILT-IN" : "CUSTOM");

        public bool IsEnabled
        {
            get => Recipe.IsEnabled;
            set
            {
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

        public string SourceSummary
        {
            get
            {
                var sourceNode = Recipe.FindFirstNodeByType(WellKnownStepTypes.Source);
                if (sourceNode != null)
                {
                    return sourceNode.GetParameter<string>("SourceType") ?? "Capture Source";
                }
                if (Recipe.HasVideoStep()) return "Video Recording";
                return "Workflow";
            }
        }

        public string TriggersSummary
        {
            get
            {
                if (Recipe.Triggers == null || Recipe.Triggers.Count == 0)
                {
                    return Recipe.ShowInContextMenu ? "📋 Systray (Default)" : "No triggers (Editor/DAG only)";
                }

                var list = new List<string>();
                foreach (var t in Recipe.Triggers)
                {
                    if (string.Equals(t.TriggerType, TriggerConfig.TypeHotkey, StringComparison.OrdinalIgnoreCase))
                    {
                        string hk = t.GetParameter<string>("Hotkey");
                        list.Add(string.IsNullOrWhiteSpace(hk) ? "⌨ Hotkey" : $"⌨ {hk}");
                    }
                    else if (string.Equals(t.TriggerType, TriggerConfig.TypeContextMenu, StringComparison.OrdinalIgnoreCase))
                    {
                        string txt = t.GetParameter<string>("MenuItemText") ?? Recipe.Name;
                        list.Add($"📋 Systray (\"{txt}\")");
                    }
                    else if (string.Equals(t.TriggerType, TriggerConfig.TypeEditor, StringComparison.OrdinalIgnoreCase))
                    {
                        string txt = t.GetParameter<string>("MenuItemText") ?? Recipe.Name;
                        list.Add($"🎨 Editor (\"{txt}\")");
                    }
                    else if (string.Equals(t.TriggerType, TriggerConfig.TypeClipboard, StringComparison.OrdinalIgnoreCase))
                    {
                        list.Add("📋 Clipboard Monitor");
                    }
                    else
                    {
                        list.Add(t.Name ?? t.TriggerType);
                    }
                }

                return string.Join("  •  ", list);
            }
        }

        public bool CanUnload => !IsBuiltIn || IsOverridden;
        public string UnloadButtonText => IsOverridden ? "Reset Default" : "Unload";
        public string UnloadToolTip => IsOverridden
            ? "Revert overridden recipe back to original default definition"
            : "Unload and unregister this custom recipe from Greenshot";

        public ICommand ToggleActiveCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand UnloadCommand { get; }
        public ICommand TestRunCommand { get; }

        private void ExecuteUnload()
        {
            if (!CanUnload) return;

            string confirmTitle = IsOverridden ? "Reset Recipe to Default" : "Unload Recipe";
            string confirmMsg = IsOverridden
                ? $"Are you sure you want to revert '{Name}' back to its default built-in definition?"
                : $"Are you sure you want to unload '{Name}'? It will be unregistered from Greenshot.";

            if (MessageBox.Show(confirmMsg, confirmTitle, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            if (IsOverridden)
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
            var pipeline = _pipeline ?? SimpleServiceProvider.Current?.GetInstance<ICapturePipeline>(isOptional: true);
            if (pipeline == null)
            {
                MessageBox.Show("Capture pipeline service is not available.", "Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var valResult = RecipeValidator.Validate(Recipe);
            if (!valResult.IsValid)
            {
                MessageBox.Show($"Cannot test run recipe. Fix validation errors first:\n\n{string.Join("\n", valResult.Errors)}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var recipeToTest = TriggerRecipePreparer.PrepareForTestRun(Recipe);
                await pipeline.ExecuteAsync(recipeToTest);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Recipe execution encountered an error:\n{ex.Message}", "Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        public event Action RequestClose;

        public RecipeManagerViewModel(
            IRecipeManager recipeManager = null,
            ICapturePipeline pipeline = null,
            Action<CaptureRecipe> onSelectRecipeForEditor = null,
            Action onNewRecipeRequested = null)
        {
            _recipeManager = recipeManager ?? SimpleServiceProvider.Current?.GetInstance<IRecipeManager>(isOptional: true);
            _pipeline = pipeline ?? SimpleServiceProvider.Current?.GetInstance<ICapturePipeline>(isOptional: true);
            _onSelectRecipeForEditor = onSelectRecipeForEditor;
            _onNewRecipeRequested = onNewRecipeRequested;

            LoadRecipeFromFileCommand = new RelayCommand(ExecuteLoadRecipeFromFile);
            CreateNewRecipeCommand = new RelayCommand(ExecuteCreateNewRecipe);
            ReloadAllCommand = new RelayCommand(ExecuteReloadAll);
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
        public int BuiltInCount => AllRecipes.Count(r => r.IsBuiltIn && !r.IsOverridden);
        public int CustomCount => AllRecipes.Count(r => !r.IsBuiltIn || r.IsOverridden);

        public ICommand LoadRecipeFromFileCommand { get; }
        public ICommand CreateNewRecipeCommand { get; }
        public ICommand ReloadAllCommand { get; }
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

            ApplyFilter();
            UpdateStats();
        }

        private void ApplyFilter()
        {
            FilteredRecipes.Clear();
            string query = _searchText?.Trim() ?? "";

            foreach (var item in AllRecipes)
            {
                // Category filter
                bool categoryMatch = _selectedFilterCategory switch
                {
                    "Active" => item.IsEnabled,
                    "Deactivated" => !item.IsEnabled,
                    "Built-in" => item.IsBuiltIn && !item.IsOverridden,
                    "Custom" => !item.IsBuiltIn || item.IsOverridden,
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

                FilteredRecipes.Add(item);
            }

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
            OnPropertyChanged(nameof(BuiltInCount));
            OnPropertyChanged(nameof(CustomCount));
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
                            MessageBox.Show($"Recipe validation failed:\n\n{string.Join("\n", valResult.Errors)}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                        MessageBox.Show($"Failed to load recipe:\n\n{string.Join("\n", result.Errors)}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    MessageBox.Show($"Failed to load recipe:\n{ex.Message}", "Error Loading Recipe", MessageBoxButton.OK, MessageBoxImage.Error);
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
