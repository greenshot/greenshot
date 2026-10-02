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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using Greenshot.Base.Pipeline.Contracts;
using Greenshot.Base.Recipes;
using Greenshot.Base.Wpf;

namespace Greenshot.Plugin.RecipeEditor.ViewModels
{
    /// <summary>
    /// Editing an automatic step (recipe extension): the canvas shows its flow between In and Out, the recipe tab where it
    /// goes (which recipes, slot, order, when). <see cref="ActiveRecipe"/> is its view as a recipe without triggers, what
    /// belongs to the extension only is kept in <see cref="_extension"/>; saving writes an extension file.
    /// </summary>
    public partial class RecipeEditorViewModel
    {
        /// <summary>
        /// The node id (and step type) of the In node on the canvas of an automatic step
        /// </summary>
        public const string InNodeId = "In";

        /// <summary>
        /// The automatic step which is edited, null when a recipe is edited (its nodes are those of <see cref="ActiveRecipe"/>)
        /// </summary>
        private RecipeExtension _extension;

        /// <summary>
        /// Set while an automatic step is put into the editor, so <see cref="ActiveRecipe"/> keeps it
        /// </summary>
        private bool _openingExtension;

        public bool IsExtensionMode => _extension != null;

        public bool IsRecipeMode => _extension == null;

        public IReadOnlyList<string> ExtensionSlotNames => RecipeSlots.All;

        /// <summary>
        /// Where the automatic step goes: AfterCapture, BeforeExport, AfterExport or BeforeDestination
        /// </summary>
        public string ExtensionSlot
        {
            get => _extension?.Extends?.Slot;
            set
            {
                if (_extension == null || value == _extension.Extends.Slot) return;
                _extension.Extends.Slot = value;
                OnExtensionChanged();
            }
        }

        /// <summary>
        /// Which recipes: ids, "*" or "*capture", separated by commas
        /// </summary>
        public string ExtensionTargets
        {
            get => _extension == null ? null : string.Join(", ", _extension.Extends.Recipes ?? new List<string>());
            set
            {
                if (_extension == null) return;
                var targets = RecipeSlots.SplitList(value).ToList();
                if (targets.SequenceEqual(_extension.Extends.Recipes ?? new List<string>(), StringComparer.OrdinalIgnoreCase)) return;
                _extension.Extends.Recipes = targets;
                OnExtensionChanged();
            }
        }

        /// <summary>
        /// Several automatic steps on one slot run by order, then by id
        /// </summary>
        public int ExtensionOrder
        {
            get => _extension?.Extends?.Order ?? 0;
            set
            {
                if (_extension == null || value == _extension.Extends.Order) return;
                _extension.Extends.Order = value;
                OnExtensionChanged();
            }
        }

        /// <summary>
        /// Optional condition, e.g. ${payload.width > 800}
        /// </summary>
        public string ExtensionWhen
        {
            get => _extension?.When;
            set
            {
                if (_extension == null) return;
                string when = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
                if (when == _extension.When) return;
                _extension.When = when;
                OnExtensionChanged();
            }
        }

        /// <summary>
        /// Which recipes it fits now
        /// </summary>
        public string ExtensionReachText
        {
            get
            {
                if (_extension == null) return null;
                var view = BuildExtension(sync: false);
                var recipes = (_recipeManager?.GetAllRecipes() ?? Array.Empty<CaptureRecipe>()).Where(r => RecipeComposer.CanExtend(view, r)).Select(r => r.Name ?? r.Id).ToList();
                return recipes.Count == 0 ? "It fits no recipe now (none has this slot, or no recipe matches)." : $"It fits {recipes.Count} recipe(s) now: {string.Join(", ", recipes)}";
            }
        }

        private void OnExtensionChanged()
        {
            IsDirty = true;
            OnPropertyChanged(nameof(ExtensionSlot));
            OnPropertyChanged(nameof(ExtensionTargets));
            OnPropertyChanged(nameof(ExtensionOrder));
            OnPropertyChanged(nameof(ExtensionWhen));
            OnPropertyChanged(nameof(ExtensionReachText));
            ValidateGraphCycles();
        }

        private void RaiseModeChanged()
        {
            OnPropertyChanged(nameof(IsExtensionMode));
            OnPropertyChanged(nameof(IsRecipeMode));
            OnExtensionChanged();
            IsDirty = false;
        }

        /// <summary>
        /// Opens a copy of an automatic step for editing
        /// </summary>
        public void OpenExtension(RecipeExtension extension)
        {
            if (extension == null || !ConfirmDiscardChanges()) return;
            OpenExtensionCopy(extension, RecipeSerializer.Serialize(extension));
            StatusMessage = $"Opened automatic step '{RecipeText.Translate(extension.Name ?? extension.Id)}'";
        }

        private void OpenExtensionCopy(RecipeExtension extension, string openedFromContent)
        {
            var copy = extension.Clone();
            _openingExtension = true;
            try
            {
                _extension = copy;
                _openedFromContent = openedFromContent;
                var view = copy.AsRecipeView();
                view.Name = RecipeText.Translate(copy.Name);
                view.Description = RecipeText.Translate(copy.Description);
                ActiveRecipe = view;
            }
            finally
            {
                _openingExtension = false;
            }
            RaiseModeChanged();
        }

        /// <summary>
        /// A new automatic step: a border on every capture, per destination, switched in Settings > Recipes
        /// </summary>
        public void NewExtension()
        {
            if (!ConfirmDiscardChanges()) return;
            var extension = new RecipeExtension($"ext_{Guid.NewGuid().ToString("N").Substring(0, 6)}", "New Automatic Step", "Changes every capture before it goes to a destination")
            {
                Extends = new ExtensionTarget { Recipes = new List<string> { RecipeExtension.TargetCaptures }, Slot = RecipeSlots.BeforeDestination, Order = 500 }
            }
                .AddOption(new RecipeOption { Key = RecipeExtension.EnabledOptionKey, Type = ContractDataType.Boolean, DefaultValue = true, Label = "On", QuickSettings = true })
                .AddNode(RecipeStepConfig.CreateBorder("border"));
            extension.Flow = new RecipeFlowConfig("border");
            OpenExtensionCopy(extension, null);
            // Never saved: unsaved until it is
            _savedContent = null;
            IsDirty = true;
            StatusMessage = "Created a new automatic step: connect its steps between In and Out, set where it goes in the recipe tab.";
        }

        /// <summary>
        /// The automatic step as it would be saved: the canvas and the recipe tab
        /// </summary>
        private RecipeExtension BuildExtension(bool sync = true)
        {
            if (_extension == null || ActiveRecipe == null) return null;
            if (sync)
            {
                SyncRecipeTransitions();
            }
            var extension = new RecipeExtension
            {
                Version = ActiveRecipe.Version,
                Id = ActiveRecipe.Id,
                Name = ActiveRecipe.Name,
                Description = ActiveRecipe.Description,
                Requires = ActiveRecipe.Requires,
                Nodes = ActiveRecipe.Nodes,
                Flow = ActiveRecipe.Flow,
                Options = ActiveRecipe.Options,
                Extends = _extension.Extends.Clone(),
                When = _extension.When,
                FilePath = ActiveRecipe.FilePath,
                IsBuiltIn = _extension.IsBuiltIn,
                IsOverridden = _extension.IsOverridden,
                ProposedBy = _extension.ProposedBy
            };
            return extension;
        }

        /// <summary>
        /// Saves the automatic step through the recipe manager, which renews its approval (asking when the change needs a decision)
        /// </summary>
        private bool SaveActiveExtensionTo(string filePath)
        {
            var extension = BuildExtension()?.Clone();
            if (extension == null) return false;
            if (_recipeManager == null)
            {
                File.WriteAllText(filePath, RecipeSerializer.Serialize(extension));
                ActiveRecipe.FilePath = filePath;
                MarkAsSaved();
                return true;
            }
            var result = _recipeManager.SaveExtensionToFile(extension, filePath);
            if (!result.IsValid)
            {
                ThemedMessageBox.Show(string.Join("\n", result.Errors), "Automatic Step Not Saved", MessageBoxButton.OK, MessageBoxImage.Warning);
                StatusMessage = "The automatic step was not saved.";
                return false;
            }
            ActiveRecipe.FilePath = extension.FilePath;
            _extension.FilePath = extension.FilePath;
            _extension.IsBuiltIn = false;
            _extension.IsOverridden = extension.IsOverridden;
            _extension.ProposedBy = null;
            _openedFromContent = RecipeSerializer.Serialize(extension);
            MarkAsSaved();
            return true;
        }

        /// <summary>
        /// The JSON view of an automatic step was edited: the same automatic step, changed (it keeps its file); undo goes back
        /// </summary>
        private void ApplyExtensionJson()
        {
            var parsed = RecipeSerializer.DeserializeExtension(RawJsonText);
            parsed.FilePath = ActiveRecipe?.FilePath;
            parsed.IsBuiltIn = _extension.IsBuiltIn;
            parsed.IsOverridden = _extension.IsOverridden;
            parsed.ProposedBy = _extension.ProposedBy;
            string savedContent = _savedContent;
            string openedFrom = _openedFromContent;
            RecordHistory(GetCurrentContent(), force: true);
            var memento = _currentMemento;
            var undo = _undoHistory.ToList();
            _isRestoring = true;
            try
            {
                OpenExtensionCopy(parsed, openedFrom);
            }
            finally
            {
                _isRestoring = false;
            }
            _savedContent = savedContent;
            if (memento != null)
            {
                _undoHistory.Clear();
                _undoHistory.AddRange(undo);
                _undoHistory.Add(memento);
            }
            _currentMemento = CreateMemento(GetCurrentContent());
            RefreshUnsavedState();
            RaiseHistoryChanged();
            IsJsonViewVisible = false;
            StatusMessage = "Applied the JSON of the automatic step.";
        }

        /// <summary>
        /// The In and Out nodes of an automatic step's canvas: not steps, they show where it starts and where the recipe goes on
        /// </summary>
        private StepNodeViewModel CreateBoundaryNode(string id, Point location)
        {
            var node = new StepNodeViewModel(new RecipeNodeConfig(id, id, id == InNodeId ? "In" : "Out"), location, null, DeleteNode, HandleNodeIdChanged, null);
            return node;
        }

        /// <summary>
        /// The steps of an automatic step without a next step lead to Out: an arrow for each, which means the same
        /// </summary>
        private void ConnectEndsToOut()
        {
            var outNode = Nodes.FirstOrDefault(n => n.IsOutNode);
            if (outNode == null) return;
            foreach (var node in Nodes.Where(n => !n.IsBoundary && !n.HasDynamicOutputPorts).ToList())
            {
                if (!Connections.Any(c => c.Source == node.OutputPort))
                {
                    Connections.Add(new StepConnectionViewModel(node.OutputPort, outNode.InputPort, RemoveConnection));
                }
            }
        }

        /// <summary>
        /// Puts In and Out on the canvas of an automatic step and connects them like its flow: In to its start steps,
        /// the steps which lead to "Out" to Out
        /// </summary>
        private void AddBoundaryNodes(CaptureRecipe recipe, IDictionary<string, StepNodeViewModel> nodeMap)
        {
            var inNode = CreateBoundaryNode(InNodeId, new Point(350, 0));
            var outNode = CreateBoundaryNode(RecipeExtension.OutNode, new Point(350, 80 + 140 * (recipe.Nodes.Count + 1)));
            Nodes.Insert(0, inNode);
            Nodes.Add(outNode);
            nodeMap[InNodeId] = inNode;
            nodeMap[RecipeExtension.OutNode] = outNode;
            // The start flags of the nodes, set from the flow's start steps while the nodes were added
            foreach (var startNode in Nodes.Where(n => n.IsStartNode && !n.IsBoundary).ToList())
            {
                Connections.Add(new StepConnectionViewModel(inNode.OutputPort, startNode.InputPort, RemoveConnection));
            }
        }
    }
}
