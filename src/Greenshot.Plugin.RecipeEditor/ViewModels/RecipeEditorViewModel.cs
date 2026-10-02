using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Greenshot.Base.Wpf;
using Greenshot.Plugin.RecipeEditor.Layout;
using log4net;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Greenshot.Base.Threading;

namespace Greenshot.Plugin.RecipeEditor.ViewModels
{
    public class RecipeEditorViewModel : ViewModelBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RecipeEditorViewModel));
        private readonly IRecipeManager _recipeManager;
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
        public ObservableCollection<FileFormatOption> OutputFormatOptions { get; } = new ObservableCollection<FileFormatOption>();

        public CaptureRecipe ActiveRecipe
        {
            get => _activeRecipe;
            set
            {
                var previous = _activeRecipe;
                if (!_isRestoring && previous != null && !ReferenceEquals(previous, value))
                {
                    KeepHistory(previous.Id);
                }
                if (SetField(ref _activeRecipe, value))
                {
                    LoadRecipeIntoCanvas(value);
                    MarkAsSaved();
                    if (!_isRestoring)
                    {
                        ResetHistory();
                    }
                    OnPropertyChanged(nameof(SelectedRecipe));
                    OnPropertyChanged(nameof(RecipeId));
                    OnPropertyChanged(nameof(RecipeTitle));
                    OnPropertyChanged(nameof(RecipeDescription));
                    OnPropertyChanged(nameof(RecipeVersion));
                    OnPropertyChanged(nameof(IsActiveRecipeEnabled));
                    OnPropertyChanged(nameof(ActiveRecipeStatusText));
                    OnPropertyChanged(nameof(CanUnloadActiveRecipe));
                    OnPropertyChanged(nameof(UnloadActiveRecipeText));
                    OnPropertyChanged(nameof(UnloadActiveRecipeToolTip));
                }
            }
        }

        public bool IsActiveRecipeEnabled
        {
            get => _activeRecipe?.IsEnabled ?? false;
            set
            {
                if (_activeRecipe != null && _activeRecipe.IsEnabled != value)
                {
                    // Takes effect right away, it isn't an unsaved change of the recipe
                    RefreshUnsavedState();
                    bool hadChanges = IsDirty;
                    _activeRecipe.IsEnabled = value;
                    _recipeManager?.SetRecipeEnabled(_activeRecipe.Id, value);
                    var registered = _recipeManager?.GetRecipeById(_activeRecipe.Id);
                    if (registered != null)
                    {
                        _openedFromContent = RecipeSerializer.Serialize(registered);
                    }
                    if (!hadChanges)
                    {
                        MarkAsSaved();
                    }
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ActiveRecipeStatusText));
                    RefreshAvailableRecipes();
                }
            }
        }

        public string ActiveRecipeStatusText => (_activeRecipe?.IsEnabled ?? false) ? "Active" : "Deactivated";

        public bool CanUnloadActiveRecipe => _activeRecipe != null && (!_activeRecipe.IsBuiltIn || _activeRecipe.IsOverridden);

        public string UnloadActiveRecipeText => (_activeRecipe?.IsOverridden ?? false) ? "Reset Default" : "Unload";

        public string UnloadActiveRecipeToolTip => (_activeRecipe?.IsOverridden ?? false)
            ? "Revert overridden recipe to default built-in definition"
            : "Unload custom recipe from Greenshot";

        public string RecipeId
        {
            get => _activeRecipe?.Id ?? "";
            set
            {
                if (_activeRecipe != null && _activeRecipe.Id != value)
                {
                    _activeRecipe.Id = value;
                    IsDirty = true;
                    OnPropertyChanged();
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
                    AddCurrentFormatOption(value?.OutputFileFormat);
                    AddCurrentFormatOption(value?.ExternalCommandFormat);
                    AddCurrentFormatOption(value?.ImgurFormat);
                    AddCurrentFormatOption(value?.JiraFormat);
                    AddCurrentFormatOption(value?.ConfluenceFormat);
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
            set
            {
                if (SetField(ref _isDirty, value))
                {
                    OnPropertyChanged(nameof(WindowTitle));
                    OnPropertyChanged(nameof(TitleRecipeText));
                    OnPropertyChanged(nameof(CanReviewApproval));
                }
            }
        }

        /// <summary>
        /// The recipe as it was loaded or last saved (serialized), null for a recipe which was never saved. The editor works on a copy
        /// of the registered recipe: changes reach Greenshot only by saving, with the approval that needs.
        /// </summary>
        private string _savedContent;

        /// <summary>
        /// The registered recipe the working copy was made from (serialized), to see whether it changed elsewhere
        /// </summary>
        private string _openedFromContent;

        /// <summary>
        /// The content the approval notice was made for
        /// </summary>
        private string _noticeContent;

        private string _approvalNotice;

        /// <summary>
        /// What is pending for the current recipe: what saving it will ask, or that its file waits for approval. Null when nothing is pending.
        /// </summary>
        public string ApprovalNotice
        {
            get => _approvalNotice;
            private set
            {
                if (SetField(ref _approvalNotice, value))
                {
                    OnPropertyChanged(nameof(HasApprovalNotice));
                    OnPropertyChanged(nameof(CanReviewApproval));
                }
            }
        }

        public bool HasApprovalNotice => !string.IsNullOrEmpty(_approvalNotice);

        /// <summary>
        /// The notice is about the approval of the saved file (not about saving): it can be reviewed from the editor
        /// </summary>
        public bool CanReviewApproval => HasApprovalNotice && !IsDirty && !string.IsNullOrEmpty(_recipeManager?.GetRecipeById(_activeRecipe?.Id)?.FilePath);

        /// <summary>
        /// Shows the approval of the recipe's file again, to switch its triggers and permissions on or off
        /// </summary>
        public void ReviewApproval()
        {
            if (_recipeManager == null || _activeRecipe == null) return;
            if (!ConfirmDiscardChanges()) return;
            var result = _recipeManager.ReviewApproval(_activeRecipe.Id);
            if (result != null && !result.IsValid)
            {
                ThemedMessageBox.Show(string.Join("\n", result.Errors), "Recipe Permissions", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            RefreshAvailableRecipes();
            UpdateApprovalNotice(force: true);
        }

        public string WindowTitle => _activeRecipe == null ? "Greenshot - Recipe Visual Editor" : $"{(IsDirty ? "* " : "")}{_activeRecipe.Name} - Greenshot Recipe Visual Editor";

        public string TitleRecipeText => _activeRecipe == null ? "" : $"  ·  {_activeRecipe.Name}{(IsDirty ? "  (unsaved changes)" : "")}";

        /// <summary>
        /// The registered recipe selected in the recipe list; selecting another one asks what to do with unsaved changes
        /// </summary>
        public CaptureRecipe SelectedRecipe
        {
            get => _activeRecipe == null ? null : AvailableRecipes.FirstOrDefault(r => string.Equals(r.Id, _activeRecipe.Id, StringComparison.OrdinalIgnoreCase));
            set
            {
                if (value == null || ReferenceEquals(value, SelectedRecipe))
                {
                    return;
                }
                if (!ConfirmDiscardChanges())
                {
                    // Back to the recipe which is still open, after the list finished its selection change
                    _ = Application.Current?.Dispatcher.BeginInvoke(new Action(() => OnPropertyChanged(nameof(SelectedRecipe))));
                    return;
                }
                OpenWorkingCopy(value);
            }
        }

        /// <summary>
        /// Opens a copy of a registered recipe for editing
        /// </summary>
        private void OpenWorkingCopy(CaptureRecipe registered)
        {
            if (registered != null && _activeRecipe != null && ReferenceEquals(registered, _activeRecipe))
            {
                return;
            }
            var copy = registered?.Clone();
            _openedFromContent = registered == null ? null : RecipeSerializer.Serialize(registered);
            ActiveRecipe = copy;
            // Opening the same id again: the setter only reloads for another object, which a copy always is
        }

        /// <summary>
        /// The recipe as it would be saved now
        /// </summary>
        private string GetCurrentContent()
        {
            if (_activeRecipe == null)
            {
                return null;
            }
            SyncRecipeTransitions();
            _activeRecipe.Triggers = Triggers.Select(t => t.Config).ToList();
            return RecipeSerializer.Serialize(_activeRecipe);
        }

        /// <summary>
        /// The current state is the saved one
        /// </summary>
        private void MarkAsSaved()
        {
            _savedContent = GetCurrentContent();
            IsDirty = false;
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(TitleRecipeText));
            UpdateApprovalNotice(force: true);
        }

        /// <summary>
        /// Compares the recipe with the saved state, shows whether there are unsaved changes and what is pending for its approval.
        /// Called regularly by the editor window, as steps and triggers change their configuration directly.
        /// </summary>
        public void RefreshUnsavedState()
        {
            if (_activeRecipe == null)
            {
                IsDirty = false;
                ApprovalNotice = null;
                return;
            }
            string current = GetCurrentContent();
            IsDirty = _savedContent == null || !string.Equals(current, _savedContent, StringComparison.Ordinal);
            RecordHistory(current, force: false);
            UpdateApprovalNotice(force: false, current);
        }

        /// <summary>
        /// A state of the recipe for undo and redo (memento): its content and where its steps were on the canvas
        /// </summary>
        private sealed class RecipeMemento
        {
            public string Content { get; set; }
            public Dictionary<string, Point> Locations { get; set; }
        }

        private const int MaxHistory = 100;
        private readonly List<RecipeMemento> _undoHistory = new List<RecipeMemento>();
        private readonly Stack<RecipeMemento> _redoHistory = new Stack<RecipeMemento>();

        /// <summary>
        /// The state the history is at
        /// </summary>
        private RecipeMemento _currentMemento;

        /// <summary>
        /// A change seen at the last check, recorded once it stopped changing (typing and dragging become one step)
        /// </summary>
        private string _pendingContent;

        private bool _isRestoring;

        public bool CanUndo => _undoHistory.Count > 0 || (_currentMemento != null && _pendingContent != null);

        public bool CanRedo => _redoHistory.Count > 0;

        private RecipeMemento CreateMemento(string content) => new RecipeMemento
        {
            Content = content,
            Locations = Nodes.Where(n => !string.IsNullOrEmpty(n.Id)).GroupBy(n => n.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Location, StringComparer.OrdinalIgnoreCase)
        };

        /// <summary>
        /// The history of the other recipes opened while the editor is open, by id
        /// </summary>
        private readonly Dictionary<string, (List<RecipeMemento> Undo, List<RecipeMemento> Redo, RecipeMemento Current)> _keptHistories =
            new Dictionary<string, (List<RecipeMemento>, List<RecipeMemento>, RecipeMemento)>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Keeps the history of the recipe which is closed in the editor, for when it is opened again
        /// </summary>
        private void KeepHistory(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId) || _currentMemento == null)
            {
                return;
            }
            // Redo order: the next state to redo first
            _keptHistories[recipeId] = (_undoHistory.ToList(), _redoHistory.ToList(), _currentMemento);
        }

        /// <summary>
        /// Starts the history of the recipe which was opened, with what was kept from earlier in this editor
        /// </summary>
        private void ResetHistory()
        {
            _undoHistory.Clear();
            _redoHistory.Clear();
            _pendingContent = null;
            _currentMemento = _activeRecipe == null ? null : CreateMemento(GetCurrentContent());
            if (_activeRecipe != null && _keptHistories.TryGetValue(_activeRecipe.Id ?? "", out var kept))
            {
                _keptHistories.Remove(_activeRecipe.Id);
                _undoHistory.AddRange(kept.Undo);
                if (string.Equals(kept.Current.Content, _currentMemento.Content, StringComparison.Ordinal))
                {
                    for (int i = kept.Redo.Count - 1; i >= 0; i--)
                    {
                        _redoHistory.Push(kept.Redo[i]);
                    }
                }
                else
                {
                    // It changed since (discarded changes, or saved elsewhere): undo goes back to how it was left
                    _undoHistory.Add(kept.Current);
                }
                while (_undoHistory.Count > MaxHistory)
                {
                    _undoHistory.RemoveAt(0);
                }
            }
            RaiseHistoryChanged();
        }

        /// <summary>
        /// Records a changed recipe as a step of the history, once it stopped changing or when forced (before undo and redo)
        /// </summary>
        private void RecordHistory(string current, bool force)
        {
            if (_isRestoring || _currentMemento == null || current == null || string.Equals(current, _currentMemento.Content, StringComparison.Ordinal))
            {
                if (_pendingContent != null)
                {
                    _pendingContent = null;
                    RaiseHistoryChanged();
                }
                return;
            }
            if (!force && !string.Equals(current, _pendingContent, StringComparison.Ordinal))
            {
                // Still changing: wait for the next check
                bool wasPending = _pendingContent != null;
                _pendingContent = current;
                if (!wasPending)
                {
                    RaiseHistoryChanged();
                }
                return;
            }
            _undoHistory.Add(_currentMemento);
            if (_undoHistory.Count > MaxHistory)
            {
                _undoHistory.RemoveAt(0);
            }
            _redoHistory.Clear();
            _currentMemento = CreateMemento(current);
            _pendingContent = null;
            RaiseHistoryChanged();
        }

        private void RaiseHistoryChanged()
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            (UndoCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RedoCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        public void Undo()
        {
            if (_activeRecipe == null) return;
            RecordHistory(GetCurrentContent(), force: true);
            if (_undoHistory.Count == 0) return;
            var target = _undoHistory[_undoHistory.Count - 1];
            _undoHistory.RemoveAt(_undoHistory.Count - 1);
            _redoHistory.Push(CreateMemento(_currentMemento.Content));
            Restore(target);
            StatusMessage = "Undone";
        }

        public void Redo()
        {
            if (_activeRecipe == null) return;
            RecordHistory(GetCurrentContent(), force: true);
            if (_redoHistory.Count == 0) return;
            var target = _redoHistory.Pop();
            _undoHistory.Add(CreateMemento(_currentMemento.Content));
            Restore(target);
            StatusMessage = "Redone";
        }

        /// <summary>
        /// Puts a state of the history into the editor; the recipe keeps its file and what was saved
        /// </summary>
        private void Restore(RecipeMemento memento)
        {
            var previous = _activeRecipe;
            var restored = RecipeSerializer.Deserialize(memento.Content, validate: false);
            restored.FilePath = previous.FilePath;
            restored.IsBuiltIn = previous.IsBuiltIn;
            restored.IsOverridden = previous.IsOverridden;
            restored.IsEnabled = previous.IsEnabled;
            restored.ProposedBy = previous.ProposedBy;
            string savedContent = _savedContent;
            _isRestoring = true;
            try
            {
                ActiveRecipe = restored;
                foreach (var node in Nodes)
                {
                    if (!string.IsNullOrEmpty(node.Id) && memento.Locations.TryGetValue(node.Id, out var location))
                    {
                        node.Location = location;
                    }
                }
            }
            finally
            {
                _isRestoring = false;
            }
            _savedContent = savedContent;
            // The restored content as the editor writes it, so it isn't recorded as a new change
            _currentMemento = CreateMemento(GetCurrentContent());
            _pendingContent = null;
            RefreshUnsavedState();
            RaiseHistoryChanged();
        }

        private void UpdateApprovalNotice(bool force, string current = null)
        {
            if (_recipeManager == null || _activeRecipe == null)
            {
                ApprovalNotice = null;
                return;
            }
            current ??= GetCurrentContent();
            // The notice is about saving for unsaved changes, about the file otherwise
            string noticeKey = (IsDirty ? "unsaved:" : "saved:") + current;
            if (!force && string.Equals(noticeKey, _noticeContent, StringComparison.Ordinal))
            {
                return;
            }
            _noticeContent = noticeKey;
            try
            {
                ApprovalNotice = IsDirty ? DescribeSaveDecision() : DescribePendingApproval();
            }
            catch (Exception ex)
            {
                Log.Warn("Could not check the approval of the recipe in the editor.", ex);
                ApprovalNotice = null;
            }
        }

        private string DescribeSaveDecision()
        {
            var reasons = _recipeManager.GetSaveDecisionReasons(_activeRecipe, _activeRecipe.FilePath);
            return reasons.Count == 0 ? null : "Saving asks for your approval: " + string.Join(" ", reasons.Select(r => r.TrimEnd('.') + "."));
        }

        private string DescribePendingApproval()
        {
            var registered = _recipeManager.GetRecipeById(_activeRecipe.Id);
            if (registered == null)
            {
                return null;
            }
            if (!string.IsNullOrEmpty(registered.FilePath) && File.Exists(registered.FilePath))
            {
                var details = _recipeManager.GetRecipeDetails(registered.Id);
                if (details != null && !details.IsApprovalCurrent)
                {
                    return details.ApprovedHash == null
                        ? "This recipe file isn't approved yet. Greenshot asks for your approval before it runs."
                        : "This recipe file changed outside Greenshot. Greenshot asks for your approval before it runs.";
                }
            }
            int switchedOff = registered.Triggers?.Count(t => t != null && t.Enabled && !t.IsApproved) ?? 0;
            if (switchedOff > 0)
            {
                // Name them: "off" alone reads like a bug when the switch was simply left off in the approval
                const string offSuffix = " (off, not approved)";
                var names = _recipeManager.GetRecipeDetails(registered.Id)?.Triggers?
                    .Where(t => t.EndsWith(offSuffix, StringComparison.Ordinal))
                    .Select(t => "\"" + t.Substring(0, t.Length - offSuffix.Length) + "\"")
                    .ToList() ?? new List<string>();
                string which = names.Count > 0 ? ": " + string.Join(", ", names) : "";
                return switchedOff == 1
                    ? $"1 trigger was left off when this recipe was approved{which}. Review the approval to switch it on."
                    : $"{switchedOff} triggers were left off when this recipe was approved{which}. Review the approval to switch them on.";
            }
            return null;
        }

        /// <summary>
        /// Asks what to do with unsaved changes: save them, discard them, or cancel. True when the editor can go on (saved or discarded).
        /// </summary>
        public bool ConfirmDiscardChanges()
        {
            RefreshUnsavedState();
            if (!IsDirty || _activeRecipe == null)
            {
                return true;
            }
            int answer = ThemedMessageBox.ShowChoice(null, "Unsaved Changes", $"\"{_activeRecipe.Name}\" has unsaved changes. Do you want to save them?",
                MessageBoxImage.Warning, new[] { "Save", "Don't Save", "Cancel" }, defaultIndex: 0, cancelIndex: 2);
            switch (answer)
            {
                case 0:
                    return TrySaveRecipe();
                case 1:
                    // The changes were only made on the copy, the registered recipe is unchanged
                    _savedContent = GetCurrentContent();
                    IsDirty = false;
                    return true;
                default:
                    return false;
            }
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
        public ICommand UndoCommand { get; }
        public ICommand ReviewApprovalCommand { get; }
        public ICommand RedoCommand { get; }
        public ICommand AutoLayoutCommand { get; }
        public ICommand TestRunCommand { get; }
        public ICommand DeleteSelectedCommand { get; }
        public ICommand DisconnectConnectorCommand { get; }
        public ICommand AddStepCommand { get; }
        public ICommand ToggleJsonViewCommand { get; }
        public ICommand ApplyJsonCommand { get; }
        public ICommand ToggleMermaidViewCommand { get; }
        public ICommand CopyMermaidCommand { get; }
        public ICommand CopyRecipeIdCommand { get; }
        public ICommand SetStartNodeCommand { get; }
        public ICommand ToggleStartNodeCommand { get; }
        public ICommand AddTriggerCommand { get; }
        public ICommand RemoveTriggerCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand OpenRecipeManagerCommand { get; }
        public ICommand ToggleActiveRecipeEnabledCommand { get; }
        public ICommand UnloadActiveRecipeCommand { get; }
        public ObservableCollection<ErrorTransitionItemViewModel> ErrorTransitions { get; } = new ObservableCollection<ErrorTransitionItemViewModel>();
        public ICommand AddErrorTransitionCommand { get; }
        public ICommand RemoveErrorTransitionCommand { get; }

        public RecipeEditorViewModel(IRecipeManager recipeManager = null)
        {
            _recipeManager = recipeManager ?? SimpleServiceProvider.Current.GetInstance<IRecipeManager>(isOptional: true);
            InitializeOutputFormatOptions();

            NewRecipeCommand = new RelayCommand(NewRecipe);
            OpenRecipeCommand = new RelayCommand(OpenRecipeDialog);
            SaveRecipeCommand = new RelayCommand(SaveRecipe);
            SaveAsCommand = new RelayCommand(SaveAsRecipe);
            UndoCommand = new RelayCommand(Undo, () => CanUndo);
            ReviewApprovalCommand = new RelayCommand(ReviewApproval);
            RedoCommand = new RelayCommand(Redo, () => CanRedo);
            AutoLayoutCommand = new RelayCommand(PerformAutoLayout);
            TestRunCommand = new RelayCommand(() => AsyncCommand.Run(ExecuteTestRunAsync, "Recipe test run"));
            DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => SelectedNode != null || SelectedConnection != null);
            AddStepCommand = new RelayCommand(p => AddStep(p as string));
            ToggleJsonViewCommand = new RelayCommand(ToggleJsonView);
            ApplyJsonCommand = new RelayCommand(ApplyJson);
            ToggleMermaidViewCommand = new RelayCommand(ToggleMermaidView);
            CopyMermaidCommand = new RelayCommand(CopyMermaidToClipboard);
            CopyRecipeIdCommand = new RelayCommand(() =>
            {
                if (!string.IsNullOrWhiteSpace(RecipeId))
                {
                    StatusMessage = ClipboardHelper.TrySetClipboardData(RecipeId, out var copyError)
                        ? $"Copied Recipe ID '{RecipeId}' to clipboard"
                        : $"Failed to copy to clipboard: {copyError}";
                }
            });
            SetStartNodeCommand = new RelayCommand(p => SetStartNode(p as StepNodeViewModel ?? SelectedNode));
            ToggleStartNodeCommand = new RelayCommand(p => ToggleStartNode(p as StepNodeViewModel ?? SelectedNode));
            AddTriggerCommand = new RelayCommand(p => AddTrigger(p as string));
            RemoveTriggerCommand = new RelayCommand(p => RemoveTrigger(p as TriggerItemViewModel));
            AddErrorTransitionCommand = new RelayCommand(AddErrorTransition);
            RemoveErrorTransitionCommand = new RelayCommand(p => DeleteErrorTransition(p as ErrorTransitionItemViewModel));
            ToggleThemeCommand = new RelayCommand(WpfThemeHelper.ToggleTheme);
            OpenRecipeManagerCommand = new RelayCommand(OpenRecipeManager);
            ToggleActiveRecipeEnabledCommand = new RelayCommand(() => IsActiveRecipeEnabled = !IsActiveRecipeEnabled, () => ActiveRecipe != null);
            UnloadActiveRecipeCommand = new RelayCommand(UnloadActiveRecipe, () => CanUnloadActiveRecipe);

            if (_recipeManager != null)
            {
                _recipeManager.RecipesChanged += OnRecipesChanged;
            }

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

        private void InitializeOutputFormatOptions()
        {
            OutputFormatOptions.Clear();
            OutputFormatOptions.Add(new FileFormatOption
            {
                Id = string.Empty,
                DisplayName = "(From Configuration)",
                DisplayNameWithPreferredExtension = "(From Configuration)"
            });

            var registry = SimpleServiceProvider.Current.GetInstance<IFileFormatRegistry>(true);
            if (registry == null)
            {
                return;
            }

            foreach (var option in registry.GetSaveableFileFormatOptions())
            {
                OutputFormatOptions.Add(option);
            }
        }

        private void AddCurrentFormatOption(string formatId)
        {
            if (!string.IsNullOrWhiteSpace(formatId) &&
                !OutputFormatOptions.Any(option => string.Equals(option.Id, formatId, StringComparison.OrdinalIgnoreCase)))
            {
                OutputFormatOptions.Add(new FileFormatOption
                {
                    Id = formatId,
                    DisplayName = formatId,
                    DisplayNameWithPreferredExtension = formatId
                });
            }
        }

        private void OnRecipesChanged(object sender, EventArgs e)
        {
            UiDispatcher.Current.InvokeAsync(RefreshAvailableRecipes).FireAndLog("Refresh the available recipes");
        }

        /// <summary>
        /// The editor window closed: stop following the recipe manager
        /// </summary>
        public void Detach()
        {
            if (_recipeManager != null)
            {
                _recipeManager.RecipesChanged -= OnRecipesChanged;
            }
        }

        public void RefreshAvailableRecipes()
        {
            string currentId = ActiveRecipe?.Id;
            AvailableRecipes.Clear();
            var all = _recipeManager?.GetAllRecipes() ?? Array.Empty<CaptureRecipe>();
            foreach (var r in all)
            {
                AvailableRecipes.Add(r);
            }

            RefreshUnsavedState();
            if (!string.IsNullOrEmpty(currentId))
            {
                var match = AvailableRecipes.FirstOrDefault(r => string.Equals(r.Id, currentId, StringComparison.OrdinalIgnoreCase));
                if (IsDirty)
                {
                    // Unsaved changes stay in the editor, also when the recipe changed or was removed elsewhere
                }
                else if (match != null)
                {
                    // Show the registered recipe when it changed elsewhere (reset, approval, another save)
                    if (!string.Equals(RecipeSerializer.Serialize(match), _openedFromContent, StringComparison.Ordinal))
                    {
                        OpenWorkingCopy(match);
                    }
                }
                else if (_savedContent != null && AvailableRecipes.Count > 0)
                {
                    OpenWorkingCopy(AvailableRecipes[0]);
                }
            }
            else if (ActiveRecipe == null && AvailableRecipes.Count > 0)
            {
                OpenWorkingCopy(AvailableRecipes[0]);
            }

            OnPropertyChanged(nameof(SelectedRecipe));
            UpdateApprovalNotice(force: true);

            OnPropertyChanged(nameof(IsActiveRecipeEnabled));
            OnPropertyChanged(nameof(ActiveRecipeStatusText));
            OnPropertyChanged(nameof(CanUnloadActiveRecipe));
            OnPropertyChanged(nameof(UnloadActiveRecipeText));
            OnPropertyChanged(nameof(UnloadActiveRecipeToolTip));
        }

        public void SelectRecipeById(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId)) return;
            if (string.Equals(ActiveRecipe?.Id, recipeId, StringComparison.OrdinalIgnoreCase))
            {
                // Already open, with its changes
                return;
            }
            var found = AvailableRecipes.FirstOrDefault(r => string.Equals(r.Id, recipeId, StringComparison.OrdinalIgnoreCase));
            if (found != null && ConfirmDiscardChanges())
            {
                OpenWorkingCopy(found);
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
                vm.RecipeNameProvider = () => RecipeTitle;
                vm.SlotExtensionsProvider = GetSlotExtensions;
                vm.OtherNodesProvider = () => Nodes.Where(n => n != vm);
                vm.AvailableRecipesProvider = () => AvailableRecipes;
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

            // Map Error Transitions
            ErrorTransitions.Clear();
            if (recipe.Flow?.ErrorTransitions != null)
            {
                foreach (var et in recipe.Flow.ErrorTransitions)
                {
                    ErrorTransitions.Add(new ErrorTransitionItemViewModel(et, DeleteErrorTransition, () => Nodes, () => AvailableRecipes));
                }
            }

            // Apply DagAutoLayout
            PerformAutoLayout();
            ValidateGraphCycles();
            IsDirty = false;
            StatusMessage = $"Loaded recipe '{recipe.Name}' ({recipe.Nodes.Count} steps, {Triggers.Count} triggers)";
        }

        private void OnNodeStartToggled(StepNodeViewModel node)
        {
            if (ActiveRecipe?.Flow != null)
            {
                ActiveRecipe.Flow.StartNodes = Nodes.Where(n => n.IsStartNode).Select(n => n.Id).ToList();
                IsDirty = true;
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
            else if (string.Equals(type, TriggerConfig.TypeCommandline, StringComparison.OrdinalIgnoreCase))
            {
                config.Parameters["Command"] = ActiveRecipe?.Id ?? "custom";
                config.Parameters["Description"] = ActiveRecipe?.Description ?? "Custom commandline recipe";
                config.Parameters["FireAndForget"] = false;
            }
            else if (string.Equals(type, TriggerConfig.TypeOpenFile, StringComparison.OrdinalIgnoreCase))
            {
                config.Parameters["Filter"] = "";
                config.Parameters["FireAndForget"] = false;
            }
            else if (string.Equals(type, TriggerConfig.TypeExtension, StringComparison.OrdinalIgnoreCase))
            {
                config.Parameters["Browser"] = "";
                config.Parameters["FireAndForget"] = false;
            }
            else if (string.Equals(type, TriggerConfig.TypeAiTool, StringComparison.OrdinalIgnoreCase))
            {
                config.Parameters["ToolName"] = new string((ActiveRecipe?.Id ?? "my_tool").Select(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' ? c : '_').Take(64).ToArray());
                config.Parameters["Title"] = ActiveRecipe?.Name ?? "My tool";
                config.Parameters["Description"] = ActiveRecipe?.Description ?? "";
                config.Parameters["ReadOnly"] = true;
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
            nodeVm.RecipeNameProvider = () => RecipeTitle;
            nodeVm.SlotExtensionsProvider = GetSlotExtensions;
            nodeVm.OtherNodesProvider = () => Nodes.Where(n => n != nodeVm);
            nodeVm.AvailableRecipesProvider = () => AvailableRecipes;
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

        public void SyncRecipeTransitions()
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
            ActiveRecipe.Flow.ErrorTransitions = ErrorTransitions.Select(e => e.ToConfig()).ToList();
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

        public void AddErrorTransition()
        {
            string fallbackTarget = Nodes.FirstOrDefault(n => n.IsDynamicDestination)?.Id 
                                  ?? Nodes.FirstOrDefault(n => string.Equals(n.StepType, WellKnownStepTypes.Editor, StringComparison.OrdinalIgnoreCase))?.Id
                                  ?? Nodes.FirstOrDefault()?.Id;
            var defaultConfig = new RecipeErrorTransitionConfig("*", fallbackTarget);
            var item = new ErrorTransitionItemViewModel(defaultConfig, DeleteErrorTransition, () => Nodes, () => AvailableRecipes);
            ErrorTransitions.Add(item);
            SyncRecipeTransitions();
            IsDirty = true;
            StatusMessage = "Added recipe error fallback route";
        }

        public void DeleteErrorTransition(ErrorTransitionItemViewModel item)
        {
            if (item == null) return;
            ErrorTransitions.Remove(item);
            SyncRecipeTransitions();
            IsDirty = true;
            StatusMessage = "Removed recipe error fallback route";
        }

        /// <summary>
        /// The extensions which plug into a slot of the recipe, by order: they target the recipe and the slot accepts them.
        /// Shown greyed out on the slot; "(off)" when the user switched it off or not for this recipe.
        /// </summary>
        private IReadOnlyList<string> GetSlotExtensions(StepNodeViewModel node)
        {
            string slotName = RecipeSlots.GetSlotName(node?.Config);
            var recipe = _activeRecipe;
            if (slotName == null || recipe == null || _recipeManager == null) return Array.Empty<string>();
            try
            {
                return _recipeManager.GetAllExtensions()
                    .Where(e => e.SlotName == slotName && RecipeSlots.Accepts(node.Config, e.Id) && RecipeComposer.Targets(e, recipe))
                    .OrderBy(e => e.Extends.Order).ThenBy(e => e.Id, StringComparer.OrdinalIgnoreCase)
                    .Select(e =>
                    {
                        string name = RecipeText.Translate(e.Name ?? e.Id);
                        return RecipeExtensionSettings.FromStore(e).AllowsRecipe(recipe.Id) ? name : $"{name} (off)";
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Debug("Couldn't find the extensions of a slot", ex);
                return Array.Empty<string>();
            }
        }

        public void NewRecipe()
        {
            if (!ConfirmDiscardChanges()) return;
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
            // New recipes accept extensions (border, drop shadow, caption, ...) like the built-in capture recipes
            RecipeStepConfig.AddStandardSlots(recipe, "export", "export");

            ErrorTransitions.Clear();
            _openedFromContent = null;
            ActiveRecipe = recipe;
            // Never saved: unsaved until it is
            _savedContent = null;
            IsDirty = true;
            StatusMessage = "Created new recipe.";
        }

        public void OpenRecipeManager()
        {
            var vm = new RecipeManagerViewModel(
                _recipeManager,
                SimpleServiceProvider.Current?.GetInstance<ICapturePipeline>(isOptional: true),
                selected =>
                {
                    RefreshAvailableRecipes();
                    SelectRecipeById(selected.Id);
                },
                () =>
                {
                    NewRecipe();
                });

            var dlg = new Dialogs.RecipeManagerDialog(vm)
            {
                Owner = Application.Current?.Windows.OfType<RecipeEditorWindow>().FirstOrDefault()
            };
            dlg.ShowDialog();
            RefreshAvailableRecipes();
            if (ActiveRecipe != null)
            {
                SelectRecipeById(ActiveRecipe.Id);
            }
        }

        public void UnloadActiveRecipe()
        {
            if (!CanUnloadActiveRecipe || ActiveRecipe == null) return;
            RefreshUnsavedState();

            string title = ActiveRecipe.IsOverridden ? "Reset Recipe to Default" : "Unload Recipe";
            string msg = ActiveRecipe.IsOverridden
                ? $"Revert '{ActiveRecipe.Name}' to default built-in definition?"
                : $"Unload '{ActiveRecipe.Name}' from Greenshot?";

            if (IsDirty)
            {
                msg += "\n\nYour unsaved changes are discarded.";
            }
            if (ThemedMessageBox.Show(msg, title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }
            // Discarded: what Greenshot has after this is shown
            _savedContent = GetCurrentContent();
            IsDirty = false;

            string recipeId = ActiveRecipe.Id;
            if (ActiveRecipe.IsOverridden)
            {
                _recipeManager?.ResetToDefault(recipeId);
            }
            else
            {
                _recipeManager?.UnregisterRecipe(recipeId);
            }

            // Shows the built-in recipe after a reset, or the first recipe after an unload
            _openedFromContent = null;
            RefreshAvailableRecipes();
            if (AvailableRecipes.Count == 0)
            {
                OpenWorkingCopy(null);
            }

            StatusMessage = $"Unloaded recipe '{recipeId}'.";
        }

        public void OpenRecipeDialog()
        {
            var dlg = new OpenFileDialog
            {
                Filter = RecipeSerializer.RecipeFileFilter,
                Title = "Open Greenshot Recipe"
            };

            if (dlg.ShowDialog() == true && ConfirmDiscardChanges())
            {
                try
                {
                    if (_recipeManager != null)
                    {
                        var valResult = _recipeManager.LoadRecipeFromFile(dlg.FileName);
                        if (!valResult.IsValid)
                        {
                            string msg = $"Recipe failed validation:\n" + string.Join("\n", valResult.Errors);
                            ThemedMessageBox.Show(msg, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            StatusMessage = $"Recipe failed validation: {Path.GetFileName(dlg.FileName)}";
                        }
                        else
                        {
                            RefreshAvailableRecipes();
                            var newlyLoaded = AvailableRecipes.FirstOrDefault(r => string.Equals(r.FilePath, dlg.FileName, StringComparison.OrdinalIgnoreCase))
                                              ?? AvailableRecipes.LastOrDefault();
                            if (newlyLoaded != null)
                            {
                                OpenWorkingCopy(newlyLoaded);
                            }
                            StatusMessage = $"Loaded and registered: {Path.GetFileName(dlg.FileName)}";
                        }
                    }
                    else
                    {
                        var recipe = RecipeSerializer.LoadFromFile(dlg.FileName);
                        var valResult = RecipeValidator.Validate(recipe);
                        if (!valResult.IsValid)
                        {
                            string msg = $"Recipe failed validation:\n" + string.Join("\n", valResult.Errors);
                            ThemedMessageBox.Show(msg, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            StatusMessage = $"Recipe failed validation: {Path.GetFileName(dlg.FileName)}";
                        }
                        else
                        {
                            recipe.FilePath = dlg.FileName;
                            ActiveRecipe = recipe;
                            StatusMessage = $"Loaded: {Path.GetFileName(dlg.FileName)}";
                        }
                    }
                }
                catch (Exception ex)
                {
                    ThemedMessageBox.Show($"Failed to load recipe:\n{ex.Message}", "Error Loading Recipe", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void SaveRecipe() => TrySaveRecipe();

        /// <summary>
        /// Saves the recipe, to a new file when it has none. False when it wasn't saved.
        /// </summary>
        public bool TrySaveRecipe()
        {
            if (ActiveRecipe == null) return false;
            SyncRecipeTransitions();
            SyncTriggersToRecipe();

            if (string.IsNullOrEmpty(ActiveRecipe.FilePath))
            {
                return TrySaveAsRecipe();
            }

            try
            {
                if (!SaveActiveRecipeTo(ActiveRecipe.FilePath)) return false;
                StatusMessage = $"Saved recipe to {Path.GetFileName(ActiveRecipe.FilePath)}";
                return true;
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show($"Failed to save recipe:\n{ex.Message}", "Error Saving Recipe", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Saves the recipe through the recipe manager, which renews its approval for exactly the saved content (and asks only when
        /// the change adds something that needs a decision). False when it wasn't saved.
        /// </summary>
        private bool SaveActiveRecipeTo(string filePath)
        {
            if (_recipeManager == null)
            {
                RecipeSerializer.SaveToFile(ActiveRecipe, filePath);
                ActiveRecipe.FilePath = filePath;
                MarkAsSaved();
                return true;
            }
            // The manager registers what it saves: give it a copy, the editor keeps working on its own
            var saved = ActiveRecipe.Clone();
            var result = _recipeManager.SaveRecipeToFile(saved, filePath);
            if (!result.IsValid)
            {
                ThemedMessageBox.Show(string.Join("\n", result.Errors), "Recipe Not Saved", MessageBoxButton.OK, MessageBoxImage.Warning);
                StatusMessage = "The recipe was not saved.";
                return false;
            }
            ActiveRecipe.FilePath = saved.FilePath;
            ActiveRecipe.IsBuiltIn = saved.IsBuiltIn;
            ActiveRecipe.IsOverridden = saved.IsOverridden;
            ActiveRecipe.ProposedBy = saved.ProposedBy;
            _openedFromContent = RecipeSerializer.Serialize(saved);
            MarkAsSaved();
            OnPropertyChanged(nameof(CanUnloadActiveRecipe));
            OnPropertyChanged(nameof(UnloadActiveRecipeText));
            OnPropertyChanged(nameof(UnloadActiveRecipeToolTip));
            return true;
        }

        public void SaveAsRecipe() => TrySaveAsRecipe();

        /// <summary>
        /// Saves the recipe to a file the user picks. False when it wasn't saved.
        /// </summary>
        public bool TrySaveAsRecipe()
        {
            if (ActiveRecipe == null) return false;
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
                    if (!SaveActiveRecipeTo(dlg.FileName)) return false;
                    StatusMessage = $"Saved recipe to {Path.GetFileName(dlg.FileName)}";
                    return true;
                }
                catch (Exception ex)
                {
                    ThemedMessageBox.Show($"Failed to save recipe:\n{ex.Message}", "Error Saving Recipe", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            return false;
        }

        public async Task ExecuteTestRunAsync()
        {
            if (ActiveRecipe == null) return;
            SyncRecipeTransitions();

            var valResult = RecipeValidator.Validate(ActiveRecipe);
            if (!valResult.IsValid)
            {
                ThemedMessageBox.Show($"Cannot test run recipe. Fix validation errors first:\n\n{string.Join("\n", valResult.Errors)}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            StatusMessage = $"Executing test run for '{ActiveRecipe.Name}'...";
            try
            {
                var result = await TestRun.RunAsync(ActiveRecipe);
                string error = TestRun.ErrorOf(result);
                if (error != null)
                {
                    throw new InvalidOperationException(error, result.Error);
                }

                StatusMessage = $"Test run of '{ActiveRecipe.Name}' ended: {result.State}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Test run error: {ex.Message}";
                ThemedMessageBox.Show($"Recipe execution encountered an error:\n{ex.Message}", "Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                // The same recipe, changed: it keeps its file and what was saved
                var previous = ActiveRecipe;
                if (previous != null)
                {
                    parsed.FilePath = previous.FilePath;
                    parsed.IsBuiltIn = previous.IsBuiltIn;
                    parsed.IsOverridden = previous.IsOverridden;
                    parsed.IsEnabled = previous.IsEnabled;
                    parsed.ProposedBy = previous.ProposedBy;
                }
                string savedContent = _savedContent;
                RecordHistory(GetCurrentContent(), force: true);
                var memento = _currentMemento;
                var undo = _undoHistory.ToList();
                _isRestoring = true;
                try
                {
                    ActiveRecipe = parsed;
                }
                finally
                {
                    _isRestoring = false;
                }
                _savedContent = savedContent;
                // Undo goes back to the recipe before the JSON was applied
                if (memento != null)
                {
                    _undoHistory.Clear();
                    _undoHistory.AddRange(undo);
                    _currentMemento = memento;
                }
                IsJsonViewVisible = false;
                RefreshUnsavedState();
                RecordHistory(GetCurrentContent(), force: true);
                StatusMessage = "Applied recipe JSON changes.";
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show($"Invalid JSON syntax or schema:\n{ex.Message}", "JSON Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    ClipboardHelper.SetClipboardData(MermaidText);
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
                    else if (string.Equals(t.TriggerType, TriggerConfig.TypeCommandline, StringComparison.OrdinalIgnoreCase))
                    {
                        string cmd = t.GetParameter<string>("Command", t.Name ?? "command");
                        tLabel = $"💻 CLI: {cmd}";
                    }
                    else if (string.Equals(t.TriggerType, TriggerConfig.TypeOpenFile, StringComparison.OrdinalIgnoreCase))
                    {
                        tLabel = "📂 Open With File";
                    }
                    else if (string.Equals(t.TriggerType, TriggerConfig.TypeExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        string browser = t.GetParameter<string>("Browser", "");
                        tLabel = string.IsNullOrEmpty(browser) ? "🌐 Browser Extension" : $"🌐 Extension ({browser})";
                    }
                    else if (string.Equals(t.TriggerType, TriggerConfig.TypeAiTool, StringComparison.OrdinalIgnoreCase))
                    {
                        tLabel = $"🤖 AI tool: {t.GetParameter<string>("ToolName", t.Name ?? "tool")}";
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
                    if (nodeMap.TryGetValue(ct.From, out var srcNode) && srcNode.Parameters != null && srcNode.Parameters.TryGetValue("Branches", out var bObj))
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

            if (recipe.Flow?.ErrorTransitions != null && recipe.Flow.ErrorTransitions.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("    %% Error Handling Fallbacks");
                foreach (var et in recipe.Flow.ErrorTransitions)
                {
                    if (string.IsNullOrWhiteSpace(et?.From)) continue;
                    string target = !string.IsNullOrEmpty(et.To) ? et.To : $"Recipe_{et.TargetRecipeId}";
                    sb.AppendLine($"    {et.From} -. \"Error: {et.ErrorType}\" .-> {target}");
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
                case WellKnownStepTypes.RecordVideo:
                    dict["SourceType"] = "ActiveWindow";
                    dict["UntilWindowCloses"] = true;
                    dict["OutputFilePath"] = @"%USERPROFILE%\Videos\Greenshot\Recording_{yyyyMMdd_HHmmss}.mp4";
                    dict["Preset"] = "Balanced";
                    dict["Format"] = "Mp4_H264";
                    dict["FrameRate"] = 30;
                    dict["Bitrate"] = 2500000;
                    dict["CaptureMouseCursor"] = true;
                    dict["ShowCaptureBorder"] = false;
                    dict["WindowResizeBehavior"] = "LetterboxFixedCanvas";
                    dict["AudioSource"] = "None";
                    dict["PreventSleepWhileRecording"] = true;
                    dict["AutoPauseOnSessionLock"] = true;
                    dict["DelayMs"] = 0;
                    break;
                case WellKnownStepTypes.InteractiveSelection:
                    dict["SelectionMode"] = "Region";
                    dict["AllowWindowSnapping"] = true;
                    break;
                case WellKnownStepTypes.Effect:
                    dict["Effect"] = "DropShadow";
                    dict["ShadowSize"] = 10;
                    dict["Darkness"] = 0.6;
                    break;
                case WellKnownStepTypes.TextEffect:
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
                case WellKnownStepTypes.DynamicDestination:
                    dict["Title"] = "Export Capture";
                    dict["ShowPreview"] = true;
                    dict["AllowRecipeForwarding"] = true;
                    dict["TimeoutSeconds"] = 0;
                    dict["Destinations"] = new List<string>();
                    break;
                case WellKnownStepTypes.SaveFile:
                    dict["SaveDirectory"] = "";
                    dict["FilenamePattern"] = "greenshot ${capturetime}";
                    dict["Format"] = "png";
                    dict["AllowOverwrite"] = false;
                    break;
                case WellKnownStepTypes.Clipboard:
                    dict["ClipboardMode"] = "ImageOnly";
                    dict["ClipboardFormatPNG"] = true;
                    dict["ClipboardFormatDIB"] = true;
                    dict["ClipboardFormatDIBV5"] = false;
                    dict["ClipboardFormatBitmap"] = false;
                    dict["ClipboardFormatHTML"] = true;
                    dict["ClipboardFormatHTMLDataUrl"] = false;
                    dict["ClipboardFormatText"] = false;
                    dict["ClipboardCustomText"] = "${Payload.ExtractedText}";
                    break;
                case WellKnownStepTypes.Printer:
                    dict["ShowPrintDialog"] = true;
                    break;
                case WellKnownStepTypes.CustomDestination:
                    dict["CustomDestinationId"] = "Imgur";
                    break;
                case WellKnownStepTypes.Notification:
                    dict["ShowNotification"] = true;
                    break;
                case WellKnownStepTypes.Conditional:
                    dict["Branches"] = new List<object>
                    {
                        new Dictionary<string, string> { { "key", "A" }, { "expression", "${payload.width > 800}" } },
                        new Dictionary<string, string> { { "key", "B" }, { "expression", "else" } }
                    };
                    break;
                case WellKnownStepTypes.UserPrompt:
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
                case WellKnownStepTypes.Stdout:
                    dict["Text"] = "${Payload.ExtractedText}";
                    break;
                case WellKnownStepTypes.Stderr:
                    dict["Text"] = "The recipe failed.";
                    dict["ExitCode"] = 1;
                    dict["Abort"] = true;
                    break;
                case WellKnownStepTypes.Processors:
                    dict["ProcessorIds"] = new List<string>();
                    break;
                case WellKnownStepTypes.Slot:
                    dict["Name"] = RecipeSlots.BeforeExport;
                    break;
                case "ExternalCommand":
                    dict["CommandLine"] = "cmd.exe";
                    dict["Arguments"] = "/c echo Processing {0}";
                    dict["Format"] = "png";
                    dict["RunInBackground"] = false;
                    dict["OutputToClipboard"] = false;
                    dict["UriToClipboard"] = false;
                    dict["ReloadAfterExecution"] = false;
                    break;
                case "Imgur":
                    dict["Format"] = "png";
                    dict["CopyLinkToClipboard"] = true;
                    break;
                case "Jira":
                    dict["IssueKey"] = "PROJECT-123";
                    dict["Format"] = "png";
                    dict["JpegQuality"] = 80;
                    break;
                case "Confluence":
                    dict["PageId"] = "123456";
                    dict["Format"] = "png";
                    dict["JpegQuality"] = 80;
                    break;
                case "Office":
                    dict["Application"] = "Word";
                    break;
                case "BarcodeScan":
                    dict["SetVariable"] = "barcode_text";
                    dict["CopyToClipboard"] = true;
                    break;
            }
            node.Parameters = dict;
        }
    }
}
