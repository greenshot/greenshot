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
using Greenshot.Pipeline;
using Greenshot.Recipes;
using Greenshot.UI.RecipeEditor.Layout;
using Microsoft.Win32;

namespace Greenshot.UI.RecipeEditor.ViewModels
{
    public class RecipeEditorViewModel : ViewModelBase
    {
        private readonly RecipeManager _recipeManager;
        private CaptureRecipe _activeRecipe;
        private StepNodeViewModel _selectedNode;
        private StepConnectionViewModel _selectedConnection;
        private string _statusMessage = "Ready";
        private bool _isDirty;
        private string _rawJsonText;
        private bool _isJsonViewVisible;

        public ObservableCollection<CaptureRecipe> AvailableRecipes { get; } = new ObservableCollection<CaptureRecipe>();
        public ObservableCollection<StepNodeViewModel> Nodes { get; } = new ObservableCollection<StepNodeViewModel>();
        public ObservableCollection<StepConnectionViewModel> Connections { get; } = new ObservableCollection<StepConnectionViewModel>();
        public ObservableCollection<TriggerItemViewModel> Triggers { get; } = new ObservableCollection<TriggerItemViewModel>();
        public PendingConnectionViewModel PendingConnection { get; } = new PendingConnectionViewModel();

        public CaptureRecipe ActiveRecipe
        {
            get => _activeRecipe;
            set
            {
                if (SetField(ref _activeRecipe, value))
                {
                    LoadRecipeIntoCanvas(value);
                    OnPropertyChanged(nameof(RecipeTitle));
                    OnPropertyChanged(nameof(RecipeDescription));
                    OnPropertyChanged(nameof(RecipeVersion));
                    OnPropertyChanged(nameof(SelectedStartNode));
                }
            }
        }

        public StepNodeViewModel SelectedStartNode
        {
            get => Nodes.FirstOrDefault(n => n.IsStartNode);
            set
            {
                if (value != null)
                {
                    SetStartNode(value);
                }
            }
        }

        public string RecipeTitle
        {
            get => _activeRecipe?.Name ?? "Untitled Recipe";
            set
            {
                if (_activeRecipe != null && _activeRecipe.Name != value)
                {
                    _activeRecipe.Name = value;
                    IsDirty = true;
                    OnPropertyChanged();
                }
            }
        }

        public string RecipeDescription
        {
            get => _activeRecipe?.Description ?? "";
            set
            {
                if (_activeRecipe != null && _activeRecipe.Description != value)
                {
                    _activeRecipe.Description = value;
                    IsDirty = true;
                    OnPropertyChanged();
                }
            }
        }

        public string RecipeVersion
        {
            get => _activeRecipe?.Version ?? "1.0";
            set
            {
                if (_activeRecipe != null && _activeRecipe.Version != value)
                {
                    _activeRecipe.Version = value;
                    IsDirty = true;
                    OnPropertyChanged();
                }
            }
        }

        private int _selectedInspectorTabIndex;

        public int SelectedInspectorTabIndex
        {
            get => _selectedInspectorTabIndex;
            set => SetField(ref _selectedInspectorTabIndex, value);
        }

        public StepNodeViewModel SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (SetField(ref _selectedNode, value))
                {
                    foreach (var n in Nodes) n.IsSelected = (n == value);
                    if (value != null)
                    {
                        if (_selectedConnection != null)
                        {
                            SelectedConnection = null;
                        }
                        SelectedInspectorTabIndex = 0; // Auto-focus Step Inspector tab
                    }
                    UpdateConnectionHighlighting();
                    OnPropertyChanged(nameof(HasSelectedNode));
                    (DeleteSelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool HasSelectedNode => SelectedNode != null;

        public StepConnectionViewModel SelectedConnection
        {
            get => _selectedConnection;
            set
            {
                if (SetField(ref _selectedConnection, value))
                {
                    if (value != null && _selectedNode != null)
                    {
                        SelectedNode = null;
                    }
                    OnPropertyChanged(nameof(HasSelectedConnection));
                    (DeleteSelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool HasSelectedConnection => SelectedConnection != null;

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetField(ref _statusMessage, value);
        }

        public bool IsDirty
        {
            get => _isDirty;
            set => SetField(ref _isDirty, value);
        }

        public string RawJsonText
        {
            get => _rawJsonText;
            set => SetField(ref _rawJsonText, value);
        }

        public bool IsJsonViewVisible
        {
            get => _isJsonViewVisible;
            set => SetField(ref _isJsonViewVisible, value);
        }



        // Commands
        public ICommand NewRecipeCommand { get; }
        public ICommand OpenRecipeCommand { get; }
        public ICommand SaveRecipeCommand { get; }
        public ICommand SaveAsCommand { get; }
        public ICommand AutoLayoutCommand { get; }
        public ICommand TestRunCommand { get; }
        public ICommand DeleteSelectedCommand { get; }
        public ICommand DisconnectConnectorCommand { get; }
        public ICommand AddStepCommand { get; }
        public ICommand ToggleJsonViewCommand { get; }
        public ICommand ApplyJsonCommand { get; }
        public ICommand SetStartNodeCommand { get; }
        public ICommand AddTriggerCommand { get; }
        public ICommand RemoveTriggerCommand { get; }

        public RecipeEditorViewModel(RecipeManager recipeManager = null)
        {
            _recipeManager = recipeManager ?? RecipeManager.Instance;

            NewRecipeCommand = new RelayCommand(NewRecipe);
            OpenRecipeCommand = new RelayCommand(OpenRecipeDialog);
            SaveRecipeCommand = new RelayCommand(SaveRecipe);
            SaveAsCommand = new RelayCommand(SaveAsRecipe);
            AutoLayoutCommand = new RelayCommand(PerformAutoLayout);
            TestRunCommand = new RelayCommand(async () => await ExecuteTestRunAsync());
            DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => SelectedNode != null || SelectedConnection != null);
            AddStepCommand = new RelayCommand(p => AddStep(p as string));
            ToggleJsonViewCommand = new RelayCommand(ToggleJsonView);
            ApplyJsonCommand = new RelayCommand(ApplyJson);
            SetStartNodeCommand = new RelayCommand(p => SetStartNode(p as StepNodeViewModel ?? SelectedNode));
            AddTriggerCommand = new RelayCommand(p => AddTrigger(p as string));
            RemoveTriggerCommand = new RelayCommand(p => RemoveTrigger(p as TriggerItemViewModel));

            DisconnectConnectorCommand = new RelayCommand(p =>
            {
                if (p is StepPortViewModel port)
                {
                    var conns = Connections.Where(c => c.Source == port || c.Target == port).ToList();
                    foreach (var c in conns) RemoveConnection(c);
                }
            });

            PendingConnection.StartedCommand = new RelayCommand(p =>
            {
                if (p is StepPortViewModel port)
                {
                    PendingConnection.Source = port;
                    PendingConnection.IsVisible = true;
                }
            });

            PendingConnection.CompletedCommand = new RelayCommand(p =>
            {
                var sourcePort = PendingConnection.Source;
                var targetPort = p as StepPortViewModel ?? PendingConnection.Target;

                if (sourcePort != null && targetPort != null && sourcePort != targetPort)
                {
                    Connect(sourcePort, targetPort);
                }

                PendingConnection.Source = null;
                PendingConnection.Target = null;
                PendingConnection.IsVisible = false;
            });

            RefreshAvailableRecipes();
        }

        public void RefreshAvailableRecipes()
        {
            AvailableRecipes.Clear();
            var all = _recipeManager.GetAllRecipes();
            foreach (var r in all)
            {
                AvailableRecipes.Add(r);
            }

            if (ActiveRecipe == null && AvailableRecipes.Count > 0)
            {
                ActiveRecipe = AvailableRecipes[0];
            }
        }

        public void LoadRecipeIntoCanvas(CaptureRecipe recipe)
        {
            Nodes.Clear();
            Connections.Clear();
            SelectedNode = null;

            if (recipe == null)
            {
                Triggers.Clear();
                return;
            }

            LoadTriggersFromRecipe(recipe);

            // Map Nodes
            var nodeMap = new Dictionary<string, StepNodeViewModel>(StringComparer.OrdinalIgnoreCase);
            double defaultX = 350;
            double defaultY = 80;

            foreach (var nodeConfig in recipe.Nodes)
            {
                var vm = new StepNodeViewModel(nodeConfig, new Point(defaultX, defaultY), SetStartNode, DeleteNode);
                vm.IsStartNode = string.Equals(recipe.Flow.StartNode, nodeConfig.Id, StringComparison.OrdinalIgnoreCase) ||
                                 (recipe.Flow.StartNodes != null && recipe.Flow.StartNodes.Contains(nodeConfig.Id, StringComparer.OrdinalIgnoreCase));
                Nodes.Add(vm);
                nodeMap[nodeConfig.Id] = vm;
                defaultY += 140;
            }

            // Map Connections
            if (recipe.Flow?.Transitions != null)
            {
                foreach (var kvp in recipe.Flow.Transitions)
                {
                    string fromId = kvp.Key;
                    if (nodeMap.TryGetValue(fromId, out var sourceNode))
                    {
                        foreach (var toId in kvp.Value)
                        {
                            if (nodeMap.TryGetValue(toId, out var targetNode))
                            {
                                var conn = new StepConnectionViewModel(sourceNode.OutputPort, targetNode.InputPort, RemoveConnection);
                                Connections.Add(conn);
                                sourceNode.OutputPort.IsConnected = true;
                                targetNode.InputPort.IsConnected = true;
                            }
                        }
                    }
                }
            }

            // Apply DagAutoLayout
            PerformAutoLayout();
            ValidateGraphCycles();
            OnPropertyChanged(nameof(SelectedStartNode));
            IsDirty = false;
            StatusMessage = $"Loaded recipe '{recipe.Name}' ({recipe.Nodes.Count} steps, {Triggers.Count} triggers)";
        }

        public void SetStartNode(StepNodeViewModel node)
        {
            if (node == null) return;
            foreach (var n in Nodes)
            {
                n.IsStartNode = (n == node);
            }
            if (ActiveRecipe?.Flow != null)
            {
                ActiveRecipe.Flow.StartNode = node.Id;
            }
            OnPropertyChanged(nameof(SelectedStartNode));
            IsDirty = true;
            StatusMessage = $"'{node.DisplayName}' set as Start Step";
        }

        public void LoadTriggersFromRecipe(CaptureRecipe recipe)
        {
            Triggers.Clear();
            if (recipe?.Triggers != null)
            {
                foreach (var tc in recipe.Triggers)
                {
                    Triggers.Add(new TriggerItemViewModel(tc, SyncTriggersToRecipe, RemoveTrigger));
                }
            }
        }

        public void SyncTriggersToRecipe()
        {
            if (ActiveRecipe != null)
            {
                ActiveRecipe.Triggers = Triggers.Select(t => t.Config).ToList();
                IsDirty = true;
            }
        }

        public void AddTrigger(string type = "Hotkey")
        {
            if (string.IsNullOrEmpty(type)) type = "Hotkey";
            var config = new TriggerConfig(type, $"{type} Trigger");
            if (string.Equals(type, "Hotkey", StringComparison.OrdinalIgnoreCase))
            {
                config.Parameters["Hotkey"] = "Ctrl + Alt + R";
            }
            else if (string.Equals(type, "ContextMenu", StringComparison.OrdinalIgnoreCase))
            {
                config.Parameters["MenuItemText"] = ActiveRecipe?.Name ?? "Capture with Recipe";
                config.Parameters["Group"] = "Recipes";
            }
            else if (string.Equals(type, "Clipboard", StringComparison.OrdinalIgnoreCase))
            {
                config.Parameters["Pattern"] = "";
            }

            var item = new TriggerItemViewModel(config, SyncTriggersToRecipe, RemoveTrigger);
            Triggers.Add(item);
            SyncTriggersToRecipe();
            StatusMessage = $"Added {type} trigger";
        }

        public void RemoveTrigger(TriggerItemViewModel item)
        {
            if (item != null && Triggers.Contains(item))
            {
                Triggers.Remove(item);
                SyncTriggersToRecipe();
                StatusMessage = "Trigger removed";
            }
        }

        public void PerformAutoLayout()
        {
            string startId = ActiveRecipe?.Flow?.StartNode;
            DagAutoLayout.ApplyLayout(Nodes, Connections, startId);
        }

        public void Connect(StepPortViewModel source, StepPortViewModel target)
        {
            if (source == null || target == null) return;
            if (source.Node == target.Node) return; // Prevent self connection

            // Ensure source is output and target is input
            StepPortViewModel fromPort;
            StepPortViewModel toPort;

            if (!source.IsInput && target.IsInput)
            {
                fromPort = source;
                toPort = target;
            }
            else if (source.IsInput && !target.IsInput)
            {
                fromPort = target;
                toPort = source;
            }
            else
            {
                return; // Cannot connect input-to-input or output-to-output
            }

            // Prevent duplicate connection
            bool exists = Connections.Any(c => c.SourceNode == fromPort.Node && c.TargetNode == toPort.Node);
            if (!exists)
            {
                var conn = new StepConnectionViewModel(fromPort, toPort, RemoveConnection);
                Connections.Add(conn);
                fromPort.IsConnected = true;
                toPort.IsConnected = true;
                SyncRecipeTransitions();
                ValidateGraphCycles();
                IsDirty = true;
                StatusMessage = $"Connected {fromPort.Node.DisplayName} -> {toPort.Node.DisplayName}";
            }
        }

        public void RemoveConnection(StepConnectionViewModel conn)
        {
            if (conn != null && Connections.Contains(conn))
            {
                Connections.Remove(conn);

                if (conn.Source != null)
                {
                    conn.Source.IsConnected = Connections.Any(c => c.Source == conn.Source);
                }
                if (conn.Target != null)
                {
                    conn.Target.IsConnected = Connections.Any(c => c.Target == conn.Target);
                }

                if (SelectedConnection == conn)
                {
                    SelectedConnection = null;
                }

                SyncRecipeTransitions();
                ValidateGraphCycles();
                IsDirty = true;
                StatusMessage = "Connection removed";
            }
        }

        public void AddStep(string stepType)
        {
            if (string.IsNullOrEmpty(stepType)) return;

            string id = $"{stepType.ToLowerInvariant()}_{Guid.NewGuid().ToString("N").Substring(0, 4)}";
            var config = new RecipeNodeConfig(id, stepType, stepType);
            
            // Set defaults
            SetDefaultParametersForStep(config);

            // Position at center below existing nodes
            double x = 350;
            double y = Nodes.Count > 0 ? Nodes.Max(n => n.Location.Y) + 140 : 100;

            var nodeVm = new StepNodeViewModel(config, new Point(x, y), SetStartNode, DeleteNode);
            if (Nodes.Count == 0)
            {
                nodeVm.IsStartNode = true;
                if (ActiveRecipe != null) ActiveRecipe.Flow.StartNode = id;
            }

            Nodes.Add(nodeVm);
            if (ActiveRecipe != null)
            {
                ActiveRecipe.Nodes.Add(config);
            }

            SelectedNode = nodeVm;
            IsDirty = true;
            StatusMessage = $"Added step: {stepType}";
        }

        public void DeleteNode(StepNodeViewModel nodeToDelete)
        {
            if (nodeToDelete == null) return;
            var connsToRemove = Connections.Where(c => c.SourceNode == nodeToDelete || c.TargetNode == nodeToDelete).ToList();
            foreach (var c in connsToRemove)
            {
                Connections.Remove(c);
            }

            Nodes.Remove(nodeToDelete);
            ActiveRecipe?.Nodes.Remove(nodeToDelete.Config);

            // Update port IsConnected flags for all remaining nodes
            foreach (var n in Nodes)
            {
                n.InputPort.IsConnected = Connections.Any(c => c.Target == n.InputPort);
                n.OutputPort.IsConnected = Connections.Any(c => c.Source == n.OutputPort);
            }

            SyncRecipeTransitions();
            ValidateGraphCycles();

            if (SelectedNode == nodeToDelete)
            {
                SelectedNode = Nodes.FirstOrDefault();
            }
            IsDirty = true;
            StatusMessage = $"Deleted step '{nodeToDelete.DisplayName}' ({nodeToDelete.Id})";
        }

        public void DeleteSelected()
        {
            if (SelectedConnection != null)
            {
                RemoveConnection(SelectedConnection);
                return;
            }

            if (SelectedNode != null)
            {
                DeleteNode(SelectedNode);
            }
        }

        private void SyncRecipeTransitions()
        {
            if (ActiveRecipe == null) return;
            var transitions = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var c in Connections)
            {
                if (c.SourceNode != null && c.TargetNode != null)
                {
                    if (!transitions.TryGetValue(c.SourceNode.Id, out var list))
                    {
                        list = new List<string>();
                        transitions[c.SourceNode.Id] = list;
                    }
                    if (!list.Contains(c.TargetNode.Id, StringComparer.OrdinalIgnoreCase))
                    {
                        list.Add(c.TargetNode.Id);
                    }
                }
            }

            ActiveRecipe.Flow.Transitions = transitions;
        }

        private void ValidateGraphCycles()
        {
            var valResult = RecipeValidator.Validate(ActiveRecipe);
            bool hasCycle = !valResult.IsValid && valResult.Errors.Any(e => e.IndexOf("cycle", StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (var c in Connections) c.IsCycle = hasCycle;
            foreach (var n in Nodes) n.IsInCycle = hasCycle;

            if (!valResult.IsValid)
            {
                StatusMessage = $"Validation Warning: {valResult.Errors.FirstOrDefault()}";
            }
            else
            {
                StatusMessage = "Workflow DAG Valid";
            }
        }

        private void UpdateConnectionHighlighting()
        {
            foreach (var c in Connections)
            {
                c.IsActive = (SelectedNode != null && (c.SourceNode == SelectedNode || c.TargetNode == SelectedNode));
            }
        }

        public void NewRecipe()
        {
            var recipe = new CaptureRecipe(
                $"recipe_{Guid.NewGuid().ToString("N").Substring(0, 6)}",
                "New Custom Workflow",
                "Custom visual capture recipe")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.Region))
                .AddNode(RecipeStepConfig.CreateFeedback("feedback"))
                .AddNode(RecipeStepConfig.CreateDestinations("export"));

            recipe.Flow = new RecipeFlowConfig("acquire")
                .AddTransition("acquire", "feedback")
                .AddTransition("feedback", "export");

            ActiveRecipe = recipe;
            IsDirty = true;
            StatusMessage = "Created new recipe.";
        }

        public void OpenRecipeDialog()
        {
            var dlg = new OpenFileDialog
            {
                Filter = RecipeSerializer.RecipeFileFilter,
                Title = "Open Greenshot Recipe"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var recipe = RecipeSerializer.LoadFromFile(dlg.FileName);
                    recipe.FilePath = dlg.FileName;
                    ActiveRecipe = recipe;
                    StatusMessage = $"Loaded: {Path.GetFileName(dlg.FileName)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load recipe file:\n{ex.Message}", "Error Loading Recipe", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void SaveRecipe()
        {
            if (ActiveRecipe == null) return;
            SyncRecipeTransitions();
            SyncTriggersToRecipe();

            if (string.IsNullOrEmpty(ActiveRecipe.FilePath))
            {
                SaveAsRecipe();
                return;
            }

            try
            {
                RecipeSerializer.SaveToFile(ActiveRecipe, ActiveRecipe.FilePath);
                _recipeManager.RegisterRecipe(ActiveRecipe);
                IsDirty = false;
                StatusMessage = $"Saved recipe to {Path.GetFileName(ActiveRecipe.FilePath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save recipe:\n{ex.Message}", "Error Saving Recipe", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SaveAsRecipe()
        {
            if (ActiveRecipe == null) return;
            SyncRecipeTransitions();
            SyncTriggersToRecipe();

            var dlg = new SaveFileDialog
            {
                Filter = RecipeSerializer.RecipeFileFilter,
                FileName = $"{ActiveRecipe.Id}.gsrecipe.json",
                Title = "Save Greenshot Recipe As..."
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    ActiveRecipe.FilePath = dlg.FileName;
                    RecipeSerializer.SaveToFile(ActiveRecipe, dlg.FileName);
                    _recipeManager.RegisterRecipe(ActiveRecipe);
                    IsDirty = false;
                    StatusMessage = $"Saved recipe to {Path.GetFileName(dlg.FileName)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save recipe:\n{ex.Message}", "Error Saving Recipe", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public async Task ExecuteTestRunAsync()
        {
            if (ActiveRecipe == null) return;
            SyncRecipeTransitions();

            var valResult = RecipeValidator.Validate(ActiveRecipe);
            if (!valResult.IsValid)
            {
                MessageBox.Show($"Cannot test run recipe. Fix validation errors first:\n\n{string.Join("\n", valResult.Errors)}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            StatusMessage = $"Executing test run for '{ActiveRecipe.Name}'...";
            try
            {
                await CapturePipeline.Instance.ExecuteAsync(ActiveRecipe);
                StatusMessage = $"Test run of '{ActiveRecipe.Name}' completed successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Test run error: {ex.Message}";
                MessageBox.Show($"Recipe execution encountered an error:\n{ex.Message}", "Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ToggleJsonView()
        {
            if (!IsJsonViewVisible)
            {
                SyncRecipeTransitions();
                RawJsonText = RecipeSerializer.Serialize(ActiveRecipe);
                IsJsonViewVisible = true;
            }
            else
            {
                IsJsonViewVisible = false;
            }
        }

        public void ApplyJson()
        {
            try
            {
                var parsed = RecipeSerializer.Deserialize(RawJsonText);
                ActiveRecipe = parsed;
                IsJsonViewVisible = false;
                IsDirty = true;
                StatusMessage = "Applied recipe JSON changes.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Invalid JSON syntax or schema:\n{ex.Message}", "JSON Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void SetDefaultParametersForStep(RecipeNodeConfig node)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            switch (node.StepType)
            {
                case WellKnownStepTypes.Source:
                    dict["SourceType"] = "Region";
                    dict["CaptureMouseCursor"] = null;
                    dict["DelayMs"] = 0;
                    break;
                case WellKnownStepTypes.InteractiveSelection:
                    dict["SelectionMode"] = "Region";
                    dict["AllowWindowSnapping"] = true;
                    break;
                case WellKnownStepTypes.Border:
                    dict["Width"] = 2;
                    dict["Color"] = "#0078D7";
                    break;
                case WellKnownStepTypes.Effect:
                    dict["Effect"] = "DropShadow";
                    dict["ShadowSize"] = 10;
                    dict["Darkness"] = 0.6;
                    break;
                case WellKnownStepTypes.TextEffect:
                case "ObfuscateText":
                    dict["Effect"] = "Redact";
                    dict["FillColor"] = "#000000";
                    dict["Patterns"] = new List<string> { @"\b\d{4}-\d{4}-\d{4}-\d{4}\b" };
                    break;
                case WellKnownStepTypes.Drawable:
                    dict["Drawables"] = new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object>
                        {
                            ["Type"] = "Text",
                            ["Text"] = "Captured: ${now:yyyy-MM-dd}",
                            ["HorizontalAnchor"] = "Right",
                            ["VerticalAnchor"] = "Bottom",
                            ["OffsetX"] = "-15",
                            ["OffsetY"] = "-10",
                            ["FillColor"] = "rgba(0,0,0,160)",
                            ["LineColor"] = "#0078D7",
                            ["Width"] = "220",
                            ["Height"] = "32"
                        }
                    };
                    break;
                case WellKnownStepTypes.SetVariable:
                    dict["Variables"] = new Dictionary<string, object>
                    {
                        { "captured_by", "${user.username}" },
                        { "timestamp", "${now:yyyy-MM-dd_HH-mm-ss}" }
                    };
                    break;
                case WellKnownStepTypes.ImmediateFeedback:
                    dict["PlaySound"] = true;
                    break;
                case WellKnownStepTypes.Destinations:
                    dict["DestinationDesignations"] = new List<string> { "Editor" };
                    break;
                case WellKnownStepTypes.SaveFile:
                case "SaveToFile":
                    dict["Destination"] = "File";
                    dict["SaveDirectory"] = "";
                    dict["FilenamePattern"] = "greenshot ${capturetime}";
                    dict["Format"] = "png";
                    dict["AllowOverwrite"] = false;
                    break;
                case WellKnownStepTypes.Clipboard:
                    dict["Destination"] = "Clipboard";
                    dict["ClipboardMode"] = "ImageOnly";
                    dict["ClipboardFormatPNG"] = true;
                    dict["ClipboardFormatDIB"] = true;
                    dict["ClipboardFormatDIBV5"] = false;
                    dict["ClipboardFormatBitmap"] = false;
                    dict["ClipboardFormatHTML"] = true;
                    dict["ClipboardFormatHTMLDataUrl"] = false;
                    dict["ClipboardFormatText"] = false;
                    dict["ClipboardCustomText"] = "${ocr_text}";
                    break;
                case WellKnownStepTypes.Editor:
                    dict["Destination"] = "Editor";
                    break;
                case WellKnownStepTypes.Printer:
                    dict["Destination"] = "Printer";
                    dict["ShowPrintDialog"] = true;
                    break;
                case WellKnownStepTypes.Email:
                    dict["Destination"] = "EMail";
                    dict["EmailSubject"] = "Screenshot";
                    break;
                case WellKnownStepTypes.CustomDestination:
                    dict["CustomDestinationId"] = "Imgur";
                    break;
                case WellKnownStepTypes.Notification:
                    dict["Title"] = "Greenshot Capture";
                    dict["Message"] = "Capture completed";
                    break;
                case WellKnownStepTypes.Conditional:
                    dict["Condition"] = "${payload.width > 800}";
                    break;
                case WellKnownStepTypes.Processors:
                    dict["Processors"] = new List<string>();
                    break;
            }
            node.Parameters = dict;
        }
    }
}
