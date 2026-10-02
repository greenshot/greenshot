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
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Expressions;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Pipeline.Contracts;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Greenshot.Pipeline;
using Greenshot.Recipes;
using Newtonsoft.Json;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    /// <summary>
    /// Recipe extensions: slots, the composer, the extension format and its checks, and the built-in extensions
    /// </summary>
    public class RecipeExtensionTests
    {
        public RecipeExtensionTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static RecipeExtensionSettings AllOn(RecipeExtension extension) => new RecipeExtensionSettings();

        /// <summary>
        /// source -> after_capture -> process -> before_export -> before_destination -> export -> after_export -> notify
        /// </summary>
        private static CaptureRecipe CreateCaptureRecipe(string id = "recipe_test_capture")
        {
            var recipe = new CaptureRecipe(id, "Test capture")
                .AddNode(RecipeStepConfig.CreateSource("source", CaptureSourceType.Region))
                .AddNode(RecipeStepConfig.CreateProcessors("process"))
                .AddNode(RecipeStepConfig.CreateDestinations("export", new[] { "Clipboard" }))
                .AddNode(RecipeStepConfig.CreateNotification("notify"));
            recipe.Flow = new RecipeFlowConfig("source")
                .AddTransition("source", "process")
                .AddTransition("process", "export")
                .AddTransition("export", "notify");
            RecipeStepConfig.AddStandardSlots(recipe, "process", "export");
            return recipe;
        }

        private static RecipeExtension CreateBorderExtension(string id = "ext_test_border", int order = 200, string slot = RecipeSlots.BeforeExport, params string[] recipes)
        {
            var extension = new RecipeExtension(id, "Test border")
            {
                Extends = new ExtensionTarget { Recipes = recipes.Length > 0 ? recipes.ToList() : new List<string> { RecipeExtension.TargetCaptures }, Slot = slot, Order = order }
            }
                .AddOption(new RecipeOption { Key = "width", Type = ContractDataType.Integer, DefaultValue = 3, Min = 1, Max = 50 })
                .AddNode(new RecipeNodeConfig("border", WellKnownStepTypes.Effect, "Border")
                    .Set("Effect", "Border")
                    .Set("Width", "${option.width}"));
            extension.Flow = new RecipeFlowConfig("border");
            return extension;
        }

        private static List<string> Targets(CaptureRecipe recipe, string from) =>
            recipe.Flow.Transitions.TryGetValue(from, out var targets) ? targets : new List<string>();

        [Fact]
        public void AddStandardSlots_PutsTheSlotsAroundTheExport()
        {
            var recipe = CreateCaptureRecipe();

            Assert.Equal(new[] { "after_capture" }, Targets(recipe, "source"));
            Assert.Equal(new[] { "process" }, Targets(recipe, "after_capture"));
            Assert.Equal(new[] { "before_export" }, Targets(recipe, "process"));
            Assert.Equal(new[] { "before_destination" }, Targets(recipe, "before_export"));
            Assert.Equal(new[] { "export" }, Targets(recipe, "before_destination"));
            Assert.Equal(new[] { "after_export" }, Targets(recipe, "export"));
            Assert.Equal(new[] { "notify" }, Targets(recipe, "after_export"));
            Assert.True(RecipeValidator.Validate(recipe).IsValid, RecipeValidator.Validate(recipe).ToString());
        }

        [Fact]
        public void Compose_ReplacesTheSlotWithThePrefixedChain()
        {
            var recipe = CreateCaptureRecipe();
            var extension = CreateBorderExtension();

            var composed = RecipeComposer.Compose(recipe, new[] { extension }, AllOn);

            Assert.NotSame(recipe, composed);
            Assert.True(composed.IsComposed);
            Assert.Null(composed.FindNode("before_export"));
            var border = composed.FindNode("ext_test_border/border");
            Assert.NotNull(border);
            Assert.Equal("ext_test_border", border.ContributedBy);
            Assert.Equal("${option.ext_test_border.width}", border.Parameters["Width"]);
            Assert.Equal(new[] { "ext_test_border/border" }, Targets(composed, "process"));
            Assert.Equal(new[] { "before_destination" }, Targets(composed, "ext_test_border/border"));
            Assert.Equal(new[] { extension }, composed.AppliedExtensions);
            // The input is not changed, the other slots stay
            Assert.NotNull(recipe.FindNode("before_export"));
            Assert.Null(recipe.FindNode("ext_test_border/border"));
            Assert.NotNull(composed.FindNode("after_capture"));
            Assert.NotNull(composed.FindNode("after_export"));
        }

        [Fact]
        public void Compose_ComposedRecipeIsValid_AndItsOptionsResolve()
        {
            var composed = RecipeComposer.Compose(CreateCaptureRecipe(), new[] { CreateBorderExtension() }, AllOn);

            var validation = RecipeValidator.Validate(composed);
            Assert.True(validation.IsValid, validation.ToString());
            Assert.True(RecipeOptionStore.TryGetValue(composed, "ext_test_border.width", out var width));
            Assert.Equal(3, width);
            Assert.False(RecipeOptionStore.TryGetValue(composed, "other_extension.width", out _));
        }

        [Fact]
        public void Compose_SeveralChainsOnASlot_RunByOrderThenById()
        {
            var recipe = CreateCaptureRecipe();
            var late = CreateBorderExtension("ext_b_late", 300);
            var earlyB = CreateBorderExtension("ext_b_early", 100);
            var earlyA = CreateBorderExtension("ext_a_early", 100);

            var composed = RecipeComposer.Compose(recipe, new[] { late, earlyB, earlyA }, AllOn);

            Assert.Equal(new[] { "ext_a_early/border" }, Targets(composed, "process"));
            Assert.Equal(new[] { "ext_b_early/border" }, Targets(composed, "ext_a_early/border"));
            Assert.Equal(new[] { "ext_b_late/border" }, Targets(composed, "ext_b_early/border"));
            Assert.Equal(new[] { "before_destination" }, Targets(composed, "ext_b_late/border"));
        }

        [Fact]
        public void Compose_WithoutSlot_ReturnsTheRecipeItself()
        {
            var recipe = new CaptureRecipe("recipe_no_slot", "No slot")
                .AddNode(RecipeStepConfig.CreateSource("source"))
                .AddNode(RecipeStepConfig.CreateDestinations("export"));
            recipe.Flow = new RecipeFlowConfig("source").AddTransition("source", "export");

            Assert.Same(recipe, RecipeComposer.Compose(recipe, new[] { CreateBorderExtension() }, AllOn));
        }

        [Fact]
        public void Compose_SlotAccept_NoneOrAList()
        {
            var recipe = CreateCaptureRecipe();
            var extension = CreateBorderExtension();

            recipe.FindNode("before_export").Set("Accept", RecipeSlots.AcceptNone);
            Assert.Same(recipe, RecipeComposer.Compose(recipe, new[] { extension }, AllOn));

            recipe.FindNode("before_export").Set("Accept", new List<string> { "ext_other" });
            Assert.Same(recipe, RecipeComposer.Compose(recipe, new[] { extension }, AllOn));

            recipe.FindNode("before_export").Set("Accept", new List<string> { "ext_test_border" });
            Assert.NotSame(recipe, RecipeComposer.Compose(recipe, new[] { extension }, AllOn));
        }

        [Fact]
        public void Targets_AiToolRecipesOnlyById()
        {
            var aiRecipe = CreateCaptureRecipe("recipe_test_ai");
            aiRecipe.AddTrigger(TriggerConfig.CreateAiTool("test_tool", "A test tool", null));

            Assert.False(RecipeComposer.Targets(CreateBorderExtension(recipes: RecipeExtension.TargetCaptures), aiRecipe));
            Assert.False(RecipeComposer.Targets(CreateBorderExtension(recipes: RecipeExtension.TargetAll), aiRecipe));
            Assert.True(RecipeComposer.Targets(CreateBorderExtension(recipes: "recipe_test_ai"), aiRecipe));
            Assert.True(RecipeComposer.Targets(CreateBorderExtension(recipes: RecipeExtension.TargetAll), CreateCaptureRecipe()));
        }

        [Fact]
        public void Targets_CaptureNeedsASourceAndADestination()
        {
            var noDestination = new CaptureRecipe("recipe_test_nodest", "No destination")
                .AddNode(RecipeStepConfig.CreateSource("source"))
                .AddNode(RecipeStepConfig.CreateSlot("slot", RecipeSlots.BeforeExport));
            noDestination.Flow = new RecipeFlowConfig("source").AddTransition("source", "slot");

            Assert.False(RecipeComposer.Targets(CreateBorderExtension(), noDestination));
            Assert.True(RecipeComposer.Targets(CreateBorderExtension(recipes: RecipeExtension.TargetAll), noDestination));
        }

        [Fact]
        public void Compose_FollowsTheUsersSettings()
        {
            var recipe = CreateCaptureRecipe();
            var extension = CreateBorderExtension();

            Assert.Same(recipe, RecipeComposer.Compose(recipe, new[] { extension }, e => new RecipeExtensionSettings { Enabled = false }));
            Assert.Same(recipe, RecipeComposer.Compose(recipe, new[] { extension }, e => new RecipeExtensionSettings
            {
                ExceptRecipes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { recipe.Id }
            }));
            Assert.Same(recipe, RecipeComposer.Compose(recipe, new[] { extension }, e => new RecipeExtensionSettings
            {
                ApplyToAll = false,
                OnlyRecipes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "recipe_other" }
            }));
            Assert.NotSame(recipe, RecipeComposer.Compose(recipe, new[] { extension }, e => new RecipeExtensionSettings
            {
                ApplyToAll = false,
                OnlyRecipes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { recipe.Id }
            }));
        }

        [Fact]
        public void Compose_When_AddsADecisionWhichSkipsTheChain()
        {
            var extension = CreateBorderExtension();
            extension.When = "${payload.width > option.width}";

            var composed = RecipeComposer.Compose(CreateCaptureRecipe(), new[] { extension }, AllOn);

            var when = composed.FindNode("ext_test_border/when");
            Assert.NotNull(when);
            Assert.Equal(WellKnownStepTypes.Conditional, when.StepType);
            Assert.Equal(new[] { "ext_test_border/when" }, Targets(composed, "process"));
            Assert.Contains(composed.Flow.ConditionalTransitions, ct => ct.From == "ext_test_border/when" && ct.Branch == "run" && ct.To == "ext_test_border/border");
            Assert.Contains(composed.Flow.ConditionalTransitions, ct => ct.From == "ext_test_border/when" && ct.Branch == "skip" && ct.To == "before_destination");
            Assert.Contains("option.ext_test_border.width", JsonConvert.SerializeObject(when.Parameters));
            Assert.True(RecipeValidator.Validate(composed).IsValid, RecipeValidator.Validate(composed).ToString());
        }

        [Fact]
        public void Compose_OutTarget_LeadsToTheSlotsNextNodes()
        {
            var extension = new RecipeExtension("ext_test_branch", "Branching")
            {
                Extends = new ExtensionTarget { Recipes = new List<string> { RecipeExtension.TargetCaptures }, Slot = RecipeSlots.BeforeExport }
            }
                .AddNode(RecipeStepConfig.CreateConditional("decide", new[]
                {
                    new KeyValuePair<string, string>("big", "${payload.width > 800}"),
                    new KeyValuePair<string, string>("small", "else")
                }))
                .AddNode(RecipeStepConfig.CreateBorder("border"));
            extension.Flow = new RecipeFlowConfig("decide")
                .AddConditionalTransition("decide", "big", "border")
                .AddConditionalTransition("decide", "small", RecipeExtension.OutNode);
            Assert.True(RecipeValidator.Validate(extension).IsValid, RecipeValidator.Validate(extension).ToString());

            var composed = RecipeComposer.Compose(CreateCaptureRecipe(), new[] { extension }, AllOn);

            Assert.Contains(composed.Flow.ConditionalTransitions, ct => ct.From == "ext_test_branch/decide" && ct.Branch == "small" && ct.To == "before_destination");
            Assert.Contains(composed.Flow.ConditionalTransitions, ct => ct.From == "ext_test_branch/decide" && ct.Branch == "big" && ct.To == "ext_test_branch/border");
            Assert.Equal(new[] { "before_destination" }, Targets(composed, "ext_test_branch/border"));
            Assert.True(RecipeValidator.Validate(composed).IsValid, RecipeValidator.Validate(composed).ToString());
        }

        [Fact]
        public void Compose_BeforeDestination_BecomesADestinationChain()
        {
            var recipe = CreateCaptureRecipe();
            var extension = CreateBorderExtension(slot: RecipeSlots.BeforeDestination);

            var composed = RecipeComposer.Compose(recipe, new[] { extension }, e => new RecipeExtensionSettings
            {
                OnlyDestinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "EMail" }
            });

            Assert.NotNull(composed.FindNode("before_destination"));
            Assert.Null(composed.FindNode("ext_test_border/border"));
            var chain = Assert.Single(composed.DestinationChains);
            Assert.Same(extension, chain.Extension);
            Assert.True(chain.RunsFor("EMail"));
            Assert.False(chain.RunsFor("Clipboard"));
            Assert.Equal(new[] { "ext_test_border/border" }, chain.Flow.Flow.StartNodes);
            Assert.Contains(extension, composed.AppliedExtensions);
        }

        [Fact]
        public void Compose_SameExtensionInTwoSlots_GetsUniqueIds()
        {
            var recipe = CreateCaptureRecipe();
            recipe.FindNode("after_export").Set("Name", RecipeSlots.BeforeExport);

            var composed = RecipeComposer.Compose(recipe, new[] { CreateBorderExtension() }, AllOn);

            Assert.NotNull(composed.FindNode("ext_test_border/border"));
            Assert.NotNull(composed.FindNode("ext_test_border/after_export/border"));
            Assert.Equal(composed.Nodes.Count, composed.Nodes.Select(n => n.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }

        [Fact]
        public async Task ComposedRecipe_Runs_AndWhenSkipsTheChain()
        {
            async Task<List<string>> RunAsync(string when)
            {
                var extension = CreateBorderExtension();
                extension.When = when;
                var composed = RecipeComposer.Compose(CreateCaptureRecipe(), new[] { extension }, AllOn);

                using var bmp = new Bitmap(10, 10);
                using var context = new CaptureFlowContext(composed) { Payload = new CapturePayload(new Capture((Image)bmp.Clone())) };
                var log = new List<string>();
                var engine = new DagExecutionEngine(node => new MockTestStep(node.Id, ctx =>
                {
                    log.Add(node.Id);
                    return Task.CompletedTask;
                }));
                await engine.ExecuteAsync(composed, context);
                Assert.False(context.IsAborted);
                return log;
            }

            var ran = await RunAsync("${payload.width > 5}");
            Assert.Equal(new[] { "source", "after_capture", "process", "ext_test_border/when", "ext_test_border/border", "before_destination", "export", "after_export", "notify" }, ran);

            var skipped = await RunAsync("${payload.width > 500}");
            Assert.Equal(new[] { "source", "after_capture", "process", "ext_test_border/when", "before_destination", "export", "after_export", "notify" }, skipped);
        }

        [Fact]
        public void TemplateOption_IsEvaluatedWhereItIsUsed()
        {
            var extension = CreateBorderExtension()
                .AddOption(new RecipeOption { Key = "caption", Type = ContractDataType.String, Format = RecipeOption.FormatTemplate, DefaultValue = "Year ${now:yyyy} ${option.width}" })
                .AddOption(new RecipeOption { Key = "plain", Type = ContractDataType.String, DefaultValue = "Year ${now:yyyy}" });
            var composed = RecipeComposer.Compose(CreateCaptureRecipe(), new[] { extension }, AllOn);
            using var context = new CaptureFlowContext(composed);

            Assert.Equal($"Year {DateTime.Now:yyyy} ", ExpressionEvaluator.Instance.Evaluate("${option.ext_test_border.caption}", context));
            Assert.Equal("Year ${now:yyyy}", ExpressionEvaluator.Instance.Evaluate("${option.ext_test_border.plain}", context));
        }

        [Fact]
        public void Validate_Extension_Rules()
        {
            Assert.True(RecipeValidator.Validate(CreateBorderExtension()).IsValid);

            var withSource = CreateBorderExtension().AddNode(RecipeStepConfig.CreateSource("capture"));
            Assert.Contains(RecipeValidator.Validate(withSource).Errors, e => e.Contains("can't capture"));

            var withDestination = CreateBorderExtension().AddNode(RecipeStepConfig.CreateDestinations("export"));
            Assert.Contains(RecipeValidator.Validate(withDestination).Errors, e => e.Contains(RecipeSlots.AfterExport));
            var afterExport = CreateBorderExtension(slot: RecipeSlots.AfterExport).AddNode(RecipeStepConfig.CreateDestinations("export"));
            afterExport.Flow.AddTransition("border", "export");
            Assert.True(RecipeValidator.Validate(afterExport).IsValid, RecipeValidator.Validate(afterExport).ToString());

            var withSlot = CreateBorderExtension().AddNode(RecipeStepConfig.CreateSlot("slot", RecipeSlots.AfterExport));
            Assert.Contains(RecipeValidator.Validate(withSlot).Errors, e => e.Contains("can't extend another extension"));

            var unknownSlot = CreateBorderExtension(slot: "Somewhere");
            Assert.Contains(RecipeValidator.Validate(unknownSlot).Errors, e => e.Contains("unknown"));

            var reservedOption = CreateBorderExtension().AddOption(new RecipeOption { Key = "applyTo", Type = ContractDataType.String });
            Assert.Contains(RecipeValidator.Validate(reservedOption).Errors, e => e.Contains("reserved"));

            var reservedNode = CreateBorderExtension().AddNode(RecipeStepConfig.CreateBorder("Out"));
            Assert.Contains(RecipeValidator.Validate(reservedNode).Errors, e => e.Contains("reserved"));

            var badId = CreateBorderExtension("ext/border");
            Assert.Contains(RecipeValidator.Validate(badId).Errors, e => e.Contains("invalid"));

            var undeclaredInWhen = CreateBorderExtension();
            undeclaredInWhen.When = "${option.missing}";
            Assert.Contains(RecipeValidator.Validate(undeclaredInWhen).Errors, e => e.Contains("'missing'"));
        }

        [Fact]
        public void Validate_Extension_EveryBranchLeadsSomewhere()
        {
            var extension = new RecipeExtension("ext_test_unrouted", "Unrouted")
            {
                Extends = new ExtensionTarget { Recipes = new List<string> { RecipeExtension.TargetCaptures }, Slot = RecipeSlots.BeforeExport }
            }
                .AddNode(RecipeStepConfig.CreateConditional("decide", new[] { new KeyValuePair<string, string>("big", "${payload.width > 800}") }))
                .AddNode(RecipeStepConfig.CreateBorder("border"));
            extension.Flow = new RecipeFlowConfig("decide").AddConditionalTransition("decide", "big", "border");

            var errors = RecipeValidator.Validate(extension).Errors;
            Assert.Contains(errors, e => e.Contains("\"else\""));

            extension.Nodes[0] = RecipeStepConfig.CreateConditional("decide", new[]
            {
                new KeyValuePair<string, string>("big", "${payload.width > 800}"),
                new KeyValuePair<string, string>("small", "else")
            });
            Assert.Contains(RecipeValidator.Validate(extension).Errors, e => e.Contains("branch 'small' leads nowhere"));
        }

        [Fact]
        public void Serializer_Extension_RoundTripsWithItsKind()
        {
            var extension = CreateBorderExtension();
            extension.When = "${payload.width > 100}";

            string json = RecipeSerializer.Serialize(extension);
            Assert.StartsWith("{\r\n  \"kind\": \"extension\"", json.Replace("\r\n", "\n").Replace("\n", "\r\n"));
            var loaded = RecipeSerializer.DeserializeExtension(json);

            Assert.Equal(extension.Id, loaded.Id);
            Assert.Equal(RecipeSlots.BeforeExport, loaded.Extends.Slot);
            Assert.Equal(200, loaded.Extends.Order);
            Assert.Equal(new[] { RecipeExtension.TargetCaptures }, loaded.Extends.Recipes);
            Assert.Equal(extension.When, loaded.When);
            Assert.Equal("${option.width}", loaded.FindNode("border").GetParameter<string>("Width"));
        }

        [Fact]
        public void Serializer_KeepsRecipesAndExtensionsApart()
        {
            string extensionJson = RecipeSerializer.Serialize(CreateBorderExtension());
            Assert.Throws<JsonException>(() => RecipeSerializer.Deserialize(extensionJson));
            Assert.Throws<JsonException>(() => RecipeSerializer.DeserializeList("[" + extensionJson + "]"));

            string recipeJson = RecipeSerializer.Serialize(CreateCaptureRecipe());
            Assert.Contains("\"kind\": \"recipe\"", recipeJson);
            Assert.Throws<JsonException>(() => RecipeSerializer.DeserializeExtension(recipeJson));

            string withTriggers = extensionJson.Replace("\"kind\": \"extension\",", "\"kind\": \"extension\", \"triggers\": [],");
            Assert.Throws<JsonException>(() => RecipeSerializer.DeserializeExtension(withTriggers));

            // A recipe without "kind" is a recipe
            var recipe = RecipeSerializer.Deserialize(recipeJson.Replace("\"kind\": \"recipe\",", string.Empty));
            Assert.Equal(FlowDefinition.KindRecipe, recipe.Kind);
        }

        [Fact]
        public void BuiltInCaptureRecipes_HaveTheStandardSlots()
        {
            var manager = new RecipeManager();
            foreach (var id in new[] { RecipeManager.RecipeIdRegion, RecipeManager.RecipeIdWindow, RecipeManager.RecipeIdActiveWindow, RecipeManager.RecipeIdFullScreen, RecipeManager.RecipeIdLastRegion, RecipeManager.RecipeIdClipboard })
            {
                var recipe = manager.GetRecipeById(id);
                Assert.NotNull(recipe);
                foreach (var slot in RecipeSlots.All)
                {
                    Assert.Single(RecipeComposer.FindSlots(recipe, slot));
                }
                Assert.True(RecipeValidator.Validate(recipe).IsValid, $"{id}: {RecipeValidator.Validate(recipe)}");
            }
            Assert.Empty(RecipeComposer.FindSlots(manager.GetRecipeById(RecipeManager.RecipeIdFile), RecipeSlots.BeforeExport));
            Assert.Empty(RecipeComposer.FindSlots(manager.GetRecipeById(RecipeManager.RecipeIdOcr), RecipeSlots.BeforeExport));
        }

        [Fact]
        public void BuiltInExtensions_AreValid_OffByDefault_AndInOrder()
        {
            var extensions = BuiltInExtensions.Create();
            foreach (var extension in extensions)
            {
                var validation = RecipeValidator.Validate(extension);
                Assert.True(validation.IsValid, $"{extension.Id}: {validation}");
                Assert.True(extension.IsBuiltIn);
                Assert.Equal(RecipeSlots.BeforeDestination, extension.SlotName);
                Assert.False(extension.EnabledOption.GetDefaultValue() is true);
                Assert.True(extension.EnabledOption.QuickSettings);
            }

            // They run per destination, in their order; the flow itself is not changed
            var region = new RecipeManager().GetRecipeById(RecipeManager.RecipeIdRegion);
            var composed = RecipeComposer.Compose(region, extensions, AllOn);
            Assert.Equal(new[] { BuiltInExtensions.CaptionId, BuiltInExtensions.BorderId, BuiltInExtensions.DropShadowId }, composed.AppliedExtensions.Select(e => e.Id));
            Assert.Equal(new[] { BuiltInExtensions.CaptionId, BuiltInExtensions.BorderId, BuiltInExtensions.DropShadowId }, composed.DestinationChains.Select(c => c.Extension.Id));
            Assert.Equal(region.Nodes.Select(n => n.Id), composed.Nodes.Select(n => n.Id));
            var validation2 = RecipeValidator.Validate(composed);
            Assert.True(validation2.IsValid, validation2.ToString());
        }

        [Fact]
        public async Task BuiltInExtensions_RunWithTheRealSteps()
        {
            // The real steps (Effect, Annotation, Conditional) on a real image, the chains as the export steps run them
            _ = CapturePipeline.Instance;
            var recipe = CreateCaptureRecipe("recipe_test_real_steps");
            var composed = RecipeComposer.Compose(recipe, BuiltInExtensions.Create(), AllOn);
            Assert.Equal(3, composed.DestinationChains.Count);

            using var bmp = new Bitmap(100, 50);
            using var context = new CaptureFlowContext(composed) { Payload = new CapturePayload(new Capture((Image)bmp.Clone())) };
            var engine = new DagExecutionEngine(StepRegistry.Instance.CreateStep, StepRegistry.Instance.GetContract);
            foreach (var chain in composed.DestinationChains)
            {
                await engine.ExecuteAsync(chain.Flow, context);
                Assert.False(context.IsAborted, context.AbortReason);
            }

            // Caption bar below (font size 12: 24 px), then a border of 2 px on each side; the shadow makes it larger still
            var image = context.Payload.Surface.Image;
            Assert.True(image.Width >= 104, $"width {image.Width}");
            Assert.True(image.Height >= 50 + 24 + 4, $"height {image.Height}");
            Assert.Contains(context.ExecutionLog, line => line.Contains("ext_caption/text_bottom"));
            Assert.DoesNotContain(context.ExecutionLog, line => line.Contains("Executing node: [ext_caption/text_top]"));
            // The caption template is evaluated: the date, not "${now:...}"
            var caption = Assert.Single(context.Payload.Surface.Elements.OfType<Greenshot.Editor.Drawing.TextContainer>());
            Assert.StartsWith(DateTime.Now.ToString("yyyy-MM-dd"), caption.Text);
        }

        [Fact]
        public async Task DestinationChains_ChangeACopy_NotTheCapture()
        {
            _ = CapturePipeline.Instance;
            var composed = RecipeComposer.Compose(CreateCaptureRecipe("recipe_test_copy"), BuiltInExtensions.Create(), e => new RecipeExtensionSettings
            {
                Enabled = e.Id == BuiltInExtensions.BorderId,
                OnlyDestinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "EMail" }
            });

            using var bmp = new Bitmap(100, 50);
            using var context = new CaptureFlowContext(composed) { Payload = new CapturePayload(new Capture((Image)bmp.Clone())) };
            Assert.Empty(DestinationDispatcher.ChainsFor(context, "Clipboard"));
            var chains = DestinationDispatcher.ChainsFor(context, "EMail");
            Assert.Equal(BuiltInExtensions.BorderId, Assert.Single(chains).Extension.Id);

            var copy = await DestinationDispatcher.RunChainsOnCopyAsync(context, chains, "the test", default);

            Assert.NotNull(copy);
            Assert.Equal(new Size(104, 54), copy.Payload.EnsureSurface().Image.Size);
            Assert.Equal(new Size(100, 50), context.Payload.EnsureSurface().Image.Size);
        }

        [Fact]
        public void BuiltInExtensions_DontChangeTheRecipesUntilSwitchedOn()
        {
            var manager = new RecipeManager();
            var region = manager.GetRecipeById(RecipeManager.RecipeIdRegion);
            foreach (var extension in manager.GetAllExtensions())
            {
                RecipeOptionStore.Reset(extension.Id);
            }

            Assert.Same(region, manager.GetEffectiveRecipe(region));
            Assert.Empty(manager.GetExtensionsChanging(RecipeManager.RecipeIdRegion));

            var border = manager.GetExtensionById(BuiltInExtensions.BorderId);
            try
            {
                RecipeOptionStore.SetValue(border.Id, border.EnabledOption, true);
                var effective = manager.GetEffectiveRecipe(region);
                Assert.NotSame(region, effective);
                Assert.Equal(BuiltInExtensions.BorderId, Assert.Single(effective.DestinationChains).Extension.Id);
                Assert.Equal(new[] { BuiltInExtensions.BorderId }, manager.GetExtensionsChanging(RecipeManager.RecipeIdRegion).Select(e => e.Id));

                RecipeOptionStore.SetValue(border.Id, RecipeExtension.ExceptRecipesOption, RecipeManager.RecipeIdRegion);
                Assert.Same(region, manager.GetEffectiveRecipe(region));
            }
            finally
            {
                RecipeOptionStore.Reset(border.Id);
            }
        }
    }
}
