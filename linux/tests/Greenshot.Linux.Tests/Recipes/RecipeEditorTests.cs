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


#nullable disable

using System.Linq;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.RecipeEditor;
using Greenshot.Linux.RecipeEditor.ViewModels;
using Greenshot.Linux.Recipes;
using Greenshot.Linux.Recipes.Contracts;

namespace Greenshot.Linux.Tests.Recipes
{
    /// <summary>
    /// The view models of the recipe editor and the recipe manager
    /// </summary>
    [Collection(TestCollections.RecipeManager)]
    public class RecipeEditorTests
    {
        public RecipeEditorTests()
        {
            TestEnvironment.EnsureInitialized();
            // Tests of the same collection load other language files
            Texts.Initialize("en-US", TestEnvironment.LanguageFolders);
        }

        private static CaptureRecipe CreateCaptureRecipe(string id = "recipe_test_capture")
        {
            var recipe = new CaptureRecipe(id, "Test capture")
                .AddNode(RecipeStepConfig.CreateSource("source", CaptureSourceType.Region))
                .AddNode(RecipeStepConfig.CreateOcr("process"))
                .AddNode(RecipeStepConfig.CreateDestinations("export", new[] { "Clipboard" }))
                .AddNode(RecipeStepConfig.CreateNotification("notify"));
            recipe.Flow = new RecipeFlowConfig("source")
                .AddTransition("source", "process")
                .AddTransition("process", "export")
                .AddTransition("export", "notify");
            RecipeStepConfig.AddStandardSlots(recipe, "process", "export");
            return recipe;
        }

        [Fact]
        public void Editor_InsertsADecision_WithEachBranchLeadingOn()
        {
            var editor = new RecipeEditorViewModel();
            var recipe = new CaptureRecipe("recipe_test_decision", "Decision")
                .AddNode(RecipeStepConfig.CreateSource("source"))
                .AddNode(RecipeStepConfig.CreateDestinations("export"));
            recipe.Flow = new RecipeFlowConfig("source").AddTransition("source", "export");
            editor.ActiveRecipe = recipe;

            editor.InsertStepIntoConnection(editor.Connections.Single(), WellKnownStepTypes.Conditional);
            var decision = editor.SelectedNode;

            // Each branch leads to export (until the user changes one), the hidden normal output is not used
            Assert.DoesNotContain(editor.Connections, c => c.Source == decision.OutputPort);
            Assert.Equal(decision.ConditionBranches.Count, editor.Connections.Count(c => c.SourceNode == decision && c.TargetNode.Id == "export"));
            var flow = editor.ActiveRecipe.Flow;
            Assert.Equal(new[] { decision.Id }, flow.GetTargets("source"));
            Assert.Empty(flow.GetTargets(decision.Id));
            Assert.Equal(decision.ConditionBranches.Count, flow.ConditionalTransitions.Count(ct => ct.From == decision.Id && ct.To == "export"));
        }


        [Fact]
        public void Editor_EditsAnAutomaticStep_BetweenInAndOut()
        {
            var editor = new RecipeEditorViewModel();
            var caption = BuiltInExtensions.CreateCaption();
            editor.OpenExtension(caption);

            Assert.True(editor.IsExtensionMode);
            Assert.Equal(RecipeSlots.BeforeDestination, editor.ExtensionSlot);
            Assert.Equal(RecipeExtension.TargetCaptures, editor.ExtensionTargets);
            var inNode = editor.Nodes.Single(n => n.IsInNode);
            Assert.Single(editor.Nodes, n => n.IsOutNode);
            Assert.True(editor.Connections.Any(c => c.SourceNode == inNode && c.TargetNode.Id == "position"), string.Join(", ", editor.Connections.Select(c => $"{c.SourceNode?.Id}->{c.TargetNode?.Id}")) + " starts=" + string.Join(",", editor.ActiveRecipe.Flow.StartNodes));
            // In and Out are not steps of the automatic step
            Assert.DoesNotContain(editor.ActiveRecipe.Nodes, n => n.Id == "In" || n.Id == "Out");

            // Insert a step after In: it becomes the start step, the old start follows it
            editor.SelectedNode = inNode;
            editor.AddStep(WellKnownStepTypes.Effect);
            var effect = editor.SelectedNode;
            editor.ExtensionWhen = "${payload.width > 10}";
            editor.ExtensionTargets = "recipe_region, recipe_window";

            editor.ToggleJsonView();
            var saved = RecipeSerializer.DeserializeExtension(editor.RawJsonText, validate: false);
            Assert.Equal(new[] { effect.Id }, saved.Flow.StartNodes);
            Assert.Equal(new[] { "position" }, saved.Flow.GetTargets(effect.Id));
            Assert.Equal("${payload.width > 10}", saved.When);
            Assert.Equal(new[] { "recipe_region", "recipe_window" }, saved.Extends.Recipes);
            Assert.Equal(caption.Options.Count, saved.Options.Count);
            Assert.True(RecipeValidator.Validate(saved).IsValid, RecipeValidator.Validate(saved).ToString());

            // A connection to Out is a transition to "Out"
            var outNode = editor.Nodes.Single(n => n.IsOutNode);
            editor.Connect(editor.Nodes.Single(n => n.Id == "text_top").OutputPort, outNode.InputPort);
            editor.SyncRecipeTransitions();
            Assert.Contains(RecipeExtension.OutNode, editor.ActiveRecipe.Flow.GetTargets("text_top"));

            // Opening a recipe leaves the automatic step
            editor.ActiveRecipe = CreateCaptureRecipe();
            Assert.False(editor.IsExtensionMode);
            Assert.DoesNotContain(editor.Nodes, n => n.IsBoundary);
        }


        [Fact]
        public void Editor_DeclaresOptions()
        {
            var editor = new RecipeEditorViewModel();
            editor.ActiveRecipe = CreateCaptureRecipe("recipe_test_options");
            editor.AddOption();
            var option = Assert.Single(editor.RecipeOptions);
            option.Key = "width";
            option.Type = nameof(ContractDataType.Integer);
            option.DefaultText = "4";
            option.MinText = "1";
            option.MaxText = "20";
            var declared = Assert.Single(editor.ActiveRecipe.Options);
            Assert.Equal("width", declared.Key);
            Assert.Equal(ContractDataType.Integer, declared.Type);
            Assert.Equal(4, declared.DefaultValue);
            Assert.Equal(20m, declared.Max);

            option.Type = nameof(ContractDataType.Enum);
            option.ChoicesText = "small = Small\nlarge";
            Assert.Equal(new[] { "small", "large" }, declared.Choices.Select(c => c.Value));
            Assert.Equal("Small", declared.Choices[0].Label);

            option.RemoveCommand.Execute(null);
            Assert.Null(editor.ActiveRecipe.Options);
        }


        [Fact]
        public void RecipeManagerViewModel_ToggleCommand_FlipsEnabledState()
        {
            var manager = new RecipeManager();
            var vm = new RecipeManagerViewModel(manager);
            var item = vm.AllRecipes.First(r => r.IsBuiltIn);

            bool initial = item.IsEnabled;
            item.ToggleActiveCommand.Execute(null);

            Assert.NotEqual(initial, item.IsEnabled);
            Assert.Equal(item.IsEnabled, manager.IsRecipeEnabled(item.Id));

            // Toggle back
            item.ToggleActiveCommand.Execute(null);
            Assert.Equal(initial, item.IsEnabled);
            Assert.Equal(initial, manager.IsRecipeEnabled(item.Id));
        }


        [Fact]
        public void DynamicDestinationStep_ConfiguresCorrectlyInStepNodeViewModel()
        {
            var config = RecipeStepConfig.CreateDynamicDestination("dyn_dest", "Select Destination", true, true, new[] { "Clipboard", "Editor" }, 15);
            var vm = new StepNodeViewModel(config, new Point(0, 0));

            Assert.True(vm.IsDynamicDestination);
            Assert.Equal("Select Destination", vm.DynamicDestinationTitle);
            Assert.True(vm.DynamicDestinationShowPreview);
            Assert.True(vm.DynamicDestinationAllowRecipeForwarding);
            Assert.Equal(15, vm.DynamicDestinationTimeoutSeconds);
            Assert.Contains("Clipboard", vm.DynamicDestinationSpecificDestinations);
            Assert.Contains("Editor", vm.DynamicDestinationSpecificDestinations);
            Assert.Contains("15s", vm.Summary);
        }

        [Fact]
        public void StepNodeViewModel_OnError_IsTheErrorTransitionOfTheStep()
        {
            var editorVm = new RecipeEditorViewModel(RecipeManager.Instance);
            try
            {
                editorVm.NewRecipe();
                var vm = editorVm.Nodes.First(n => n.Id == "export");
                Assert.Equal("Default", vm.OnErrorAction);
                Assert.Empty(editorVm.ActiveRecipe.Flow.ErrorTransitions);

                // A step: one error transition for any error
                vm.OnErrorAction = "Step";
                vm.OnErrorNodeId = "feedback";
                var transition = Assert.Single(editorVm.ActiveRecipe.Flow.ErrorTransitions);
                Assert.Equal(("export", "feedback", (string)null, "*"), (transition.From, transition.To, transition.Recipe, transition.OnError));
                Assert.True(vm.IsOnErrorStep);
                Assert.Single(editorVm.ErrorTransitions);

                // A recipe: the same transition, now to the recipe
                vm.OnErrorAction = "Recipe";
                vm.OnErrorRecipeId = "rec_recovery";
                transition = Assert.Single(editorVm.ActiveRecipe.Flow.ErrorTransitions);
                Assert.Equal(("export", (string)null, "rec_recovery"), (transition.From, transition.To, transition.Recipe));
                Assert.True(vm.IsOnErrorRecipe);
                Assert.Null(vm.OnErrorNodeId);

                // Changed in the list of error transitions: the step shows it
                editorVm.ErrorTransitions[0].TargetType = "Step";
                editorVm.ErrorTransitions[0].ToNodeId = "acquire";
                Assert.Equal("acquire", vm.OnErrorNodeId);

                // Default removes it
                vm.OnErrorAction = "Default";
                Assert.Empty(editorVm.ActiveRecipe.Flow.ErrorTransitions);
                Assert.Empty(editorVm.ErrorTransitions);
            }
            finally
            {
                editorVm.Detach();
            }
        }

        [Fact]
        public void RecipeEditorViewModel_UndoAndRedo_RestoreTheChangeAndItsApprovalNotice()
        {
            var editorVm = new RecipeEditorViewModel(RecipeManager.Instance);
            try
            {
                editorVm.NewRecipe();
                editorVm.RefreshUnsavedState();
                Assert.False(editorVm.CanUndo);
                Assert.DoesNotContain("clipboard", editorVm.ApprovalNotice ?? "");

                // A trigger which starts the recipe on its own: saving will ask
                editorVm.AddTrigger("Clipboard");
                editorVm.RefreshUnsavedState();
                editorVm.RefreshUnsavedState();
                Assert.True(editorVm.CanUndo);
                Assert.Contains("clipboard", editorVm.ApprovalNotice);

                editorVm.Undo();
                Assert.Empty(editorVm.ActiveRecipe.Triggers);
                Assert.DoesNotContain("clipboard", editorVm.ApprovalNotice ?? "");
                Assert.True(editorVm.CanRedo);

                editorVm.Redo();
                Assert.Single(editorVm.ActiveRecipe.Triggers);
                Assert.Contains("clipboard", editorVm.ApprovalNotice);
                Assert.True(editorVm.IsDirty);
            }
            finally
            {
                editorVm.Detach();
            }
        }

        [Fact]
        public void RecipeEditorViewModel_ManagesErrorTransitionsAndDynamicDestination()
        {
            var editorVm = new RecipeEditorViewModel();
            editorVm.NewRecipe();

            // Add DynamicDestination step
            editorVm.AddStep(WellKnownStepTypes.DynamicDestination);
            var dynStep = System.Linq.Enumerable.FirstOrDefault(editorVm.Nodes, n => n.IsDynamicDestination);
            Assert.NotNull(dynStep);
            Assert.Equal("Export Capture", dynStep.DynamicDestinationTitle);

            // Add error transition
            editorVm.AddErrorTransition();
            Assert.Single(editorVm.ErrorTransitions);

            var errItem = editorVm.ErrorTransitions[0];
            errItem.FromNodeId = "acquire";
            errItem.TargetType = "Step";
            errItem.ToNodeId = dynStep.Id;
            errItem.ErrorType = "ClipboardException";

            editorVm.SyncRecipeTransitions();

            Assert.NotNull(editorVm.ActiveRecipe.Flow.ErrorTransitions);
            Assert.Single(editorVm.ActiveRecipe.Flow.ErrorTransitions);
            var savedTransition = editorVm.ActiveRecipe.Flow.ErrorTransitions.First();
            Assert.Equal("acquire", savedTransition.From);
            Assert.Equal(dynStep.Id, savedTransition.To);
            Assert.Equal("ClipboardException", savedTransition.OnError);
        }

        /// <summary>
        /// A list in the inspector clears its choice when another step is selected (Avalonia writes that back):
        /// it must not end up in the selected step, a Name parameter makes an Annotation step invalid
        /// </summary>
        [Fact]
        public void ClearedChoicesOfTheInspector_DoNotChangeTheStep()
        {
            var node = new StepNodeViewModel(new RecipeNodeConfig("annotate", WellKnownStepTypes.Annotation), new Point(0, 0));

            node.SlotName = null;
            node.OutputFileFormat = null;
            node.ExternalCommandFormat = null;
            node.JiraFormat = null;
            node.ConfluenceFormat = null;
            node.OnErrorNodeId = null;

            Assert.False(node.Config.Parameters?.ContainsKey("Name") ?? false);
            Assert.False(node.Config.Parameters?.ContainsKey("Format") ?? false);
        }
    }
}
