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
using Newtonsoft.Json.Linq;

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
                    OnPropertyChanged(nameof(HasSelection));
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
                if (_selectedConnection != null)
                {
                    _selectedConnection.IsSelected = false;
                }
                if (SetField(ref _selectedConnection, value))
                {
                    if (_selectedConnection != null)
                    {
                        _selectedConnection.IsSelected = true;
                        if (_selectedNode != null)
                        {
                            SelectedNode = null;
                        }
                    }
                    OnPropertyChanged(nameof(HasSelectedConnection));
                    OnPropertyChanged(nameof(HasSelection));
                    (DeleteSelectedCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool HasSelectedConnection => SelectedConnection != null;
        public bool HasSelection => HasSelectedNode || HasSelectedConnection;

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

        private string _mermaidText = "";
        private bool _isMermaidViewVisible;

        public string MermaidText
        {
            get => _mermaidText;
            set => SetField(ref _mermaidText, value);
        }

        public bool IsMermaidViewVisible
        {
            get => _isMermaidViewVisible;
            set => SetField(ref _isMermaidViewVisible, value);
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
        public ICommand ToggleMermaidViewCommand { get; }
        public ICommand CopyMermaidCommand { get; }
        public ICommand SetStartNodeCommand { get; }
        public ICommand ToggleStartNodeCommand { get; }
        public ICommand AddTriggerCommand { get; }
        public ICommand RemoveTriggerCommand { get; }
        public ICommand ToggleThemeCommand { get; }

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
            ToggleMermaidViewCommand = new RelayCommand(ToggleMermaidView);
            CopyMermaidCommand = new RelayCommand(CopyMermaidToClipboard);
            SetStartNodeCommand = new RelayCommand(p => SetStartNode(p as StepNodeViewModel ?? SelectedNode));
            ToggleStartNodeCommand = new RelayCommand(p => ToggleStartNode(p as StepNodeViewModel ?? SelectedNode));
            AddTriggerCommand = new RelayCommand(p => AddTrigger(p as string));
            RemoveTriggerCommand = new RelayCommand(p => RemoveTrigger(p as TriggerItemViewModel));
            ToggleThemeCommand = new RelayCommand(WpfThemeHelper.ToggleTheme);

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
                    StatusMessage = $"Connecting from {(port.IsInput ? "Input" : "Output")} pin of '{port.Node?.DisplayName}'... Drop onto {(port.IsInput ? "an Output" : "an Input")} pin.";
                }
            });

            PendingConnection.CompletedCommand = new RelayCommand(p =>
            {
                var sourcePort = PendingConnection.Source;
                var targetPort = p as StepPortViewModel ?? PendingConnection.Target;

                if (sourcePort != null)
                {
                    if (targetPort == null)
                    {
                        StatusMessage = "Connection cancelled (dropped on canvas empty space).";
                    }
                    else if (sourcePort == targetPort)
                    {
                        StatusMessage = "Cannot connect a pin to itself.";
                    }
                    else
                    {
                        Connect(sourcePort, targetPort);
                    }
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

            bool hasExplicitStarts = recipe.Flow?.StartNodes != null && recipe.Flow.StartNodes.Count > 0;

            foreach (var nodeConfig in recipe.Nodes)
            {
                var vm = new StepNodeViewModel(nodeConfig, new Point(defaultX, defaultY), SetStartNode, DeleteNode, HandleNodeIdChanged, OnNodeStartToggled);
                if (hasExplicitStarts)
                {
                    vm.IsStartNode = recipe.Flow.StartNodes.Contains(nodeConfig.Id, StringComparer.OrdinalIgnoreCase);
                }
                Nodes.Add(vm);
                nodeMap[nodeConfig.Id] = vm;
                defaultY += 140;
            }

            // If no explicit start nodes were defined, infer start nodes from graph topology
            if (!hasExplicitStarts && Nodes.Count > 0)
            {
                var targetNodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (recipe.Flow?.Transitions != null)
                {
                    foreach (var kvp in recipe.Flow.Transitions)
                    {
                        if (kvp.Value != null)
                        {
                            foreach (var toId in kvp.Value)
                            {
                                if (!string.IsNullOrWhiteSpace(toId)) targetNodeIds.Add(toId);
                            }
                        }
                    }
                }
                if (recipe.Flow?.ConditionalTransitions != null)
                {
                    foreach (var ct in recipe.Flow.ConditionalTransitions)
                    {
                        if (!string.IsNullOrWhiteSpace(ct?.To)) targetNodeIds.Add(ct.To);
                    }
                }

                bool foundRoot = false;
                foreach (var node in Nodes)
                {
                    if (!targetNodeIds.Contains(node.Id))
                    {
                        node.IsStartNode = true;
                        foundRoot = true;
                    }
                }
                if (!foundRoot && Nodes.Count > 0)
                {
                    Nodes[0].IsStartNode = true;
                }

                if (recipe.Flow != null)
                {
                    recipe.Flow.StartNodes = Nodes.Where(n => n.IsStartNode).Select(n => n.Id).ToList();
                }
            }

            // Map Standard Connections
            if (recipe.Flow?.Transitions != null)
            {
                foreach (var kvp in recipe.Flow.Transitions)
                {
                    string fromId = kvp.Key;
                    if (nodeMap.TryGetValue(fromId, out var sourceNode))
                    {
                        var targetList = kvp.Value ?? new List<string>();
                        foreach (var toId in targetList)
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

            // Map Conditional Transitions (Branch-specific connections)
            if (recipe.Flow?.ConditionalTransitions != null)
            {
                foreach (var ct in recipe.Flow.ConditionalTransitions)
                {
                    if (string.IsNullOrWhiteSpace(ct?.From) || string.IsNullOrWhiteSpace(ct?.To)) continue;
                    if (nodeMap.TryGetValue(ct.From, out var sourceNode) && nodeMap.TryGetValue(ct.To, out var targetNode))
                    {
                        var branch = sourceNode.ConditionBranches.FirstOrDefault(b => string.Equals(b.Key, ct.Branch, StringComparison.OrdinalIgnoreCase));
                        var promptChoice = sourceNode.PromptChoices.FirstOrDefault(p => string.Equals(p.Key, ct.Branch, StringComparison.OrdinalIgnoreCase));
                        StepPortViewModel outPort = branch?.Port ?? promptChoice?.Port ?? sourceNode.OutputPort;
                        var conn = new StepConnectionViewModel(outPort, targetNode.InputPort, RemoveConnection);
                        Connections.Add(conn);
                        outPort.IsConnected = true;
                        targetNode.InputPort.IsConnected = true;
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

        private void OnNodeStartToggled(StepNodeViewModel node)
        {
            if (ActiveRecipe?.Flow != null)
            {
                ActiveRecipe.Flow.StartNodes = Nodes.Where(n => n.IsStartNode).Select(n => n.Id).ToList();
                IsDirty = true;
                OnPropertyChanged(nameof(SelectedStartNode));
                StatusMessage = node.IsStartNode ? $"Added '{node.DisplayName}' to Start Steps" : $"Removed '{node.DisplayName}' from Start Steps";
            }
        }

        public void SetStartNode(StepNodeViewModel node)
        {
            if (node == null) return;
            node.IsStartNode = true;
            if (ActiveRecipe?.Flow != null)
            {
                ActiveRecipe.Flow.StartNodes = Nodes.Where(n => n.IsStartNode).Select(n => n.Id).ToList();
            }
            OnPropertyChanged(nameof(SelectedStartNode));
            IsDirty = true;
            StatusMessage = $"'{node.DisplayName}' marked as Start Step";
        }

        public void ToggleStartNode(StepNodeViewModel node)
        {
            if (node == null) return;
            node.IsStartNode = !node.IsStartNode;
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
            else if (string.Equals(type, "Editor", StringComparison.OrdinalIgnoreCase))
            {
                config.Parameters["MenuItemText"] = ActiveRecipe?.Name ?? "Apply Recipe";
                config.Parameters["Group"] = "Recipes";
            }
            else if (string.Equals(type, "Clipboard", StringComparison.OrdinalIgnoreCase))
            {
                config.Parameters["FormatFilter"] = "";
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
            var startIds = Nodes.Where(n => n.IsStartNode).Select(n => n.Id).ToList();
            if (startIds.Count == 0 && ActiveRecipe?.Flow?.StartNodes != null)
            {
                startIds = ActiveRecipe.Flow.StartNodes.ToList();
            }
            DagAutoLayout.ApplyLayout(Nodes, Connections, startIds);
        }

        public void Connect(StepPortViewModel source, StepPortViewModel target)
        {
            if (source == null || target == null) return;
            if (source.Node == target.Node)
            {
                StatusMessage = "Cannot connect a step to itself.";
                return;
            }

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
                StatusMessage = source.IsInput
                    ? "Cannot connect two Input pins together. Please connect an Output pin to an Input pin."
                    : "Cannot connect two Output pins together. Please connect an Output pin to an Input pin.";
                return;
            }

            // Prevent duplicate connection from same source port to same target node
            bool exists = Connections.Any(c => c.Source == fromPort && c.TargetNode == toPort.Node);
            if (exists)
            {
                StatusMessage = $"Connection from '{fromPort.Node.DisplayName}' to '{toPort.Node.DisplayName}' already exists.";
                return;
            }

            var conn = new StepConnectionViewModel(fromPort, toPort, RemoveConnection);
            Connections.Add(conn);
            fromPort.IsConnected = true;
            toPort.IsConnected = true;
            SyncRecipeTransitions();
            ValidateGraphCycles();
            IsDirty = true;
            if (conn.IsCycle)
            {
                StatusMessage = $"Warning: Connected '{fromPort.Node.DisplayName}' -> '{toPort.Node.DisplayName}' (Creates a Cycle in the DAG!)";
            }
            else
            {
                StatusMessage = $"Connected '{fromPort.Node.DisplayName}' -> '{toPort.Node.DisplayName}'";
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

            var nodeVm = new StepNodeViewModel(config, new Point(x, y), SetStartNode, DeleteNode, HandleNodeIdChanged, OnNodeStartToggled);
            if (Nodes.Count == 0)
            {
                nodeVm.IsStartNode = true;
                if (ActiveRecipe != null) ActiveRecipe.Flow.StartNodes = new List<string> { id };
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

        private void HandleNodeIdChanged(StepNodeViewModel node, string oldId, string newId)
        {
            if (node == null || string.IsNullOrWhiteSpace(newId) || string.Equals(oldId, newId, StringComparison.OrdinalIgnoreCase)) return;

            // Check for collision with another node
            bool duplicate = Nodes.Any(n => n != node && string.Equals(n.Id, newId, StringComparison.OrdinalIgnoreCase));
            if (duplicate)
            {
                StatusMessage = $"Cannot rename to '{newId}': A step with this ID already exists.";
                node.ResetId(oldId);
                return;
            }

            // Update StartNodes
            if (ActiveRecipe?.Flow?.StartNodes != null)
            {
                for (int i = 0; i < ActiveRecipe.Flow.StartNodes.Count; i++)
                {
                    if (string.Equals(ActiveRecipe.Flow.StartNodes[i], oldId, StringComparison.OrdinalIgnoreCase))
                    {
                        ActiveRecipe.Flow.StartNodes[i] = newId;
                    }
                }
            }

            // Update Transitions (as key and as targets)
            if (ActiveRecipe?.Flow?.Transitions != null)
            {
                if (ActiveRecipe.Flow.Transitions.TryGetValue(oldId, out var targets))
                {
                    ActiveRecipe.Flow.Transitions.Remove(oldId);
                    ActiveRecipe.Flow.Transitions[newId] = targets;
                }

                foreach (var kvp in ActiveRecipe.Flow.Transitions)
                {
                    var list = kvp.Value;
                    if (list != null)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (string.Equals(list[i], oldId, StringComparison.OrdinalIgnoreCase))
                            {
                                list[i] = newId;
                            }
                        }
                    }
                }
            }

            // Update ConditionalTransitions
            if (ActiveRecipe?.Flow?.ConditionalTransitions != null)
            {
                foreach (var ct in ActiveRecipe.Flow.ConditionalTransitions)
                {
                    if (string.Equals(ct.From, oldId, StringComparison.OrdinalIgnoreCase)) ct.From = newId;
                    if (string.Equals(ct.To, oldId, StringComparison.OrdinalIgnoreCase)) ct.To = newId;
                }
            }

            SyncRecipeTransitions();
            IsDirty = true;
            StatusMessage = $"Renamed step ID from '{oldId}' to '{newId}'";
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
                foreach (var b in n.ConditionBranches)
                {
                    b.Port.IsConnected = Connections.Any(c => c.Source == b.Port);
                }
                foreach (var p in n.PromptChoices)
                {
                    p.Port.IsConnected = Connections.Any(c => c.Source == p.Port);
                }
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
            if (ActiveRecipe?.Flow == null) return;
            var transitions = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var conditionalTransitions = new List<RecipeConditionalTransitionConfig>();

            foreach (var c in Connections)
            {
                if (c.SourceNode != null && c.TargetNode != null)
                {
                    var branch = c.SourceNode.ConditionBranches.FirstOrDefault(b => b.Port == c.Source);
                    var promptChoice = c.SourceNode.PromptChoices.FirstOrDefault(p => p.Port == c.Source);
                    if (branch != null)
                    {
                        conditionalTransitions.Add(new RecipeConditionalTransitionConfig(c.SourceNode.Id, branch.Key, c.TargetNode.Id));
                    }
                    else if (promptChoice != null)
                    {
                        conditionalTransitions.Add(new RecipeConditionalTransitionConfig(c.SourceNode.Id, promptChoice.Key, c.TargetNode.Id));
                    }
                    else
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
            }

            ActiveRecipe.Flow.StartNodes = Nodes.Where(n => n.IsStartNode).Select(n => n.Id).ToList();
            ActiveRecipe.Flow.Transitions = transitions;
            ActiveRecipe.Flow.ConditionalTransitions = conditionalTransitions;
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
                    var valResult = RecipeValidator.Validate(recipe);
                    if (!valResult.IsValid)
                    {
                        RecipeApprovalWindow.ShowValidationError(dlg.FileName, valResult, recipe);
                        StatusMessage = $"Recipe failed validation: {Path.GetFileName(dlg.FileName)}";
                    }
                    else
                    {
                        recipe.FilePath = dlg.FileName;
                        ActiveRecipe = recipe;
                        StatusMessage = $"Loaded: {Path.GetFileName(dlg.FileName)}";
                    }
                }
                catch (Exception ex)
                {
                    RecipeApprovalWindow.ShowValidationError(dlg.FileName, rawErrorMessage: ex.Message);
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
                IsMermaidViewVisible = false;
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

        public void ToggleMermaidView()
        {
            if (!IsMermaidViewVisible)
            {
                SyncRecipeTransitions();
                MermaidText = GenerateMermaidDsl(ActiveRecipe);
                IsMermaidViewVisible = true;
                IsJsonViewVisible = false;
            }
            else
            {
                IsMermaidViewVisible = false;
            }
        }

        public void CopyMermaidToClipboard()
        {
            try
            {
                if (!string.IsNullOrEmpty(MermaidText))
                {
                    Clipboard.SetText(MermaidText);
                    StatusMessage = "Copied Mermaid DSL to clipboard.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to copy to clipboard: {ex.Message}";
            }
        }

        public string GenerateMermaidDsl(CaptureRecipe recipe)
        {
            if (recipe == null) return "flowchart TD\n";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("flowchart TD");

            var startNodes = recipe.Flow?.StartNodes?.Where(sn => !string.IsNullOrWhiteSpace(sn)).ToList() 
                             ?? (recipe.Nodes.Count > 0 ? new List<string> { recipe.Nodes[0].Id } : new List<string>());

            var triggerNodeIds = new List<string>();

            // Triggers Subgraph
            if (recipe.Triggers != null && recipe.Triggers.Count > 0)
            {
                sb.AppendLine("    %% Triggers");
                sb.AppendLine("    subgraph Triggers [\"🚀 Triggers\"]");
                for (int i = 0; i < recipe.Triggers.Count; i++)
                {
                    var t = recipe.Triggers[i];
                    string tId = $"trigger_{i}";
                    triggerNodeIds.Add(tId);

                    string tLabel = t.TriggerType;
                    if (string.Equals(t.TriggerType, TriggerConfig.TypeHotkey, StringComparison.OrdinalIgnoreCase))
                    {
                        string hotkey = t.GetParameter<string>("Hotkey", "Shortcut");
                        tLabel = $"⚡ Hotkey: {hotkey}";
                    }
                    else if (string.Equals(t.TriggerType, TriggerConfig.TypeContextMenu, StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(t.TriggerType, TriggerConfig.TypeSystray, StringComparison.OrdinalIgnoreCase))
                    {
                        string menuText = t.GetParameter<string>("MenuItemText", t.Name ?? "Context Menu");
                        tLabel = $"🖱️ Menu: {menuText}";
                    }
                    else if (string.Equals(t.TriggerType, TriggerConfig.TypeEditor, StringComparison.OrdinalIgnoreCase))
                    {
                        string menuText = t.GetParameter<string>("MenuItemText", t.Name ?? "Editor Menu");
                        tLabel = $"🎨 Editor: {menuText}";
                    }
                    else if (string.Equals(t.TriggerType, TriggerConfig.TypeClipboard, StringComparison.OrdinalIgnoreCase))
                    {
                        tLabel = "📋 Clipboard Monitor";
                    }
                    else if (string.Equals(t.TriggerType, TriggerConfig.TypeManual, StringComparison.OrdinalIgnoreCase))
                    {
                        tLabel = $"⚡ Manual: {t.Name ?? "User Action"}";
                    }
                    else if (string.Equals(t.TriggerType, TriggerConfig.TypeSchedule, StringComparison.OrdinalIgnoreCase))
                    {
                        string cron = t.GetParameter<string>("CronExpression", t.GetParameter<string>("IntervalSeconds", "Timer"));
                        tLabel = $"⏰ Schedule: {cron}";
                    }
                    else
                    {
                        tLabel = $"⚡ {t.Name ?? t.TriggerType}";
                    }

                    if (!t.Enabled)
                    {
                        tLabel += " (Disabled)";
                    }

                    tLabel = tLabel.Replace("\"", "'");
                    sb.AppendLine($"        {tId}([\"{tLabel}\"])");
                }
                sb.AppendLine("    end");
                sb.AppendLine();
            }

            sb.AppendLine("    %% Node Definitions");

            var nodeMap = new Dictionary<string, RecipeNodeConfig>(StringComparer.OrdinalIgnoreCase);
            foreach (var n in recipe.Nodes)
            {
                nodeMap[n.Id] = n;
                string label = string.IsNullOrWhiteSpace(n.Name) ? n.StepType : $"{n.Name} ({n.StepType})";
                label = label.Replace("\"", "'");

                if (string.Equals(n.StepType, WellKnownStepTypes.Conditional, StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"    {n.Id}{{\"{label} [{n.Id}]\"}}");
                }
                else
                {
                    sb.AppendLine($"    {n.Id}[\"{label} [{n.Id}]\"]");
                }
            }

            // Trigger Invocations to Start Nodes
            if (triggerNodeIds.Count > 0 && startNodes.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("    %% Trigger Invocations");
                foreach (var tId in triggerNodeIds)
                {
                    foreach (var sn in startNodes)
                    {
                        sb.AppendLine($"    {tId} -.-> {sn}");
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("    %% Unconditional Transitions");
            if (recipe.Flow?.Transitions != null)
            {
                foreach (var kvp in recipe.Flow.Transitions)
                {
                    string fromId = kvp.Key;
                    var targets = kvp.Value;
                    if (targets != null)
                    {
                        foreach (var toId in targets)
                        {
                            sb.AppendLine($"    {fromId} --> {toId}");
                        }
                    }
                }
            }

            if (recipe.Flow?.ConditionalTransitions != null && recipe.Flow.ConditionalTransitions.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("    %% Conditional Branch Transitions");
                foreach (var ct in recipe.Flow.ConditionalTransitions)
                {
                    if (string.IsNullOrWhiteSpace(ct?.From) || string.IsNullOrWhiteSpace(ct?.To)) continue;

                    string expr = "";
                    if (nodeMap.TryGetValue(ct.From, out var srcNode) && srcNode.Parameters != null && srcNode.Parameters.TryGetValue("branches", out var bObj))
                    {
                        if (bObj is JArray arr)
                        {
                            var match = arr.FirstOrDefault(t => string.Equals((string)t["key"], ct.Branch, StringComparison.OrdinalIgnoreCase));
                            if (match != null) expr = (string)match["expression"];
                        }
                    }

                    string edgeLabel = string.IsNullOrEmpty(expr) ? ct.Branch : $"{ct.Branch}: {expr}";
                    edgeLabel = edgeLabel.Replace("\"", "'");
                    sb.AppendLine($"    {ct.From} -- \"{edgeLabel}\" --> {ct.To}");
                }
            }

            if (startNodes.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("    %% Start Nodes Styling");
                sb.AppendLine("    classDef startNode fill:#238636,stroke:#2ea043,stroke-width:2px,color:#ffffff;");
                foreach (var sn in startNodes)
                {
                    if (!string.IsNullOrWhiteSpace(sn))
                    {
                        sb.AppendLine($"    class {sn} startNode;");
                    }
                }
            }

            if (triggerNodeIds.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("    %% Triggers Styling");
                sb.AppendLine("    classDef triggerNode fill:#d97706,stroke:#f59e0b,stroke-width:2px,color:#ffffff;");
                sb.AppendLine($"    class {string.Join(",", triggerNodeIds)} triggerNode;");
            }

            return sb.ToString();
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
                case WellKnownStepTypes.Annotation:
                    dict["Annotations"] = new List<Dictionary<string, object>>
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
                    dict["Branches"] = new List<object>
                    {
                        new Dictionary<string, string> { { "key", "A" }, { "expression", "${payload.width > 800}" } },
                        new Dictionary<string, string> { { "key", "B" }, { "expression", "else" } }
                    };
                    break;
                case WellKnownStepTypes.UserPrompt:
                case "PromptChoice":
                    dict["Title"] = "Greenshot decision";
                    dict["Message"] = "Please confirm the next step for this capture:";
                    dict["ShowPreview"] = true;
                    dict["TimeoutSeconds"] = 0;
                    dict["Choices"] = new List<object>
                    {
                        new Dictionary<string, object> { { "Key", "Yes" }, { "Label", "Yes, Proceed" }, { "Style", "Primary" }, { "IsDefault", true }, { "IsCancel", false } },
                        new Dictionary<string, object> { { "Key", "No" }, { "Label", "No, Cancel" }, { "Style", "Secondary" }, { "IsDefault", false }, { "IsCancel", true } }
                    };
                    break;
                case WellKnownStepTypes.Processors:
                    dict["Processors"] = new List<string>();
                    break;
                case "ExternalCommand":
                case "ExecuteCommand":
                case "RunCommand":
                case var _ when node.StepType != null && node.StepType.StartsWith("ExternalCommand", StringComparison.OrdinalIgnoreCase):
                    dict["CommandLine"] = "cmd.exe";
                    dict["Arguments"] = "/c echo Processing {0}";
                    dict["Format"] = "png";
                    dict["RunInBackground"] = false;
                    dict["OutputToClipboard"] = false;
                    dict["UriToClipboard"] = false;
                    dict["ReloadAfterExecution"] = false;
                    break;
                case "Imgur":
                case "ImgurUpload":
                case "UploadToImgur":
                    dict["Format"] = "png";
                    dict["CopyLinkToClipboard"] = true;
                    dict["OpenInBrowser"] = false;
                    break;
                case "Jira":
                case "JiraUpload":
                case "UploadToJira":
                    dict["IssueKey"] = "PROJECT-123";
                    dict["Format"] = "png";
                    dict["JpegQuality"] = 80;
                    break;
                case "Confluence":
                case "ConfluenceUpload":
                case "UploadToConfluence":
                    dict["PageId"] = "123456";
                    dict["Format"] = "png";
                    dict["JpegQuality"] = 80;
                    break;
                case "Office":
                case "Excel":
                case "PowerPoint":
                case "Powerpoint":
                case "Word":
                case "OneNote":
                case "Outlook":
                    dict["Application"] = string.Equals(node.StepType, "Office", StringComparison.OrdinalIgnoreCase) ? "Word" : node.StepType;
                    break;
                case "Zxing":
                case "ZxingQr":
                case "ZxingBarcode":
                case "BarcodeScan":
                case "DecodeBarcode":
                case "QrCode":
                    dict["SetVariable"] = "barcode_text";
                    dict["CopyToClipboard"] = true;
                    break;
                case "Box":
                case "BoxUpload":
                case "UploadToBox":
                    dict["Format"] = "png";
                    break;
                case "Dropbox":
                case "DropboxUpload":
                case "UploadToDropbox":
                    dict["Format"] = "png";
                    break;
            }
            node.Parameters = dict;
        }
    }
}
