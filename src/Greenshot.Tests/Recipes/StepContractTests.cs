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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Recipes.Triggers;
using Greenshot.Recipes;
using Greenshot.Recipes.Pipeline;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    /// <summary>
    /// One registry for step factories and contracts, and contracts that match what the steps do.
    /// </summary>
    [Collection(TestCollections.RecipeManager)]
    public class StepContractTests
    {
        public StepContractTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static StepContract Contract(string stepType, IEnumerable<ParameterContract> parameters = null, IEnumerable<VariableContract> outputs = null,
            IEnumerable<VariableContract> inputs = null, PayloadRequirement rawCapture = PayloadRequirement.None)
        {
            return new StepContract(stepType, parameters: parameters?.ToList(), outputVariables: outputs?.ToList(), inputVariables: inputs?.ToList(),
                payloadContract: new PayloadContract(rawCapture));
        }

        /// <summary>A registry with a built-in pipeline (all core and plugin step types), independent of the global one.</summary>
        private static StepRegistry CreatePipelineRegistry()
        {
            var registry = new StepRegistry();
            _ = new CapturePipeline(stepRegistry: registry);
            // Plugins are not services in the tests: register the ones the tests reference
            new Greenshot.Plugin.Zxing.ZxingPlugin().RegisterSteps(registry);
            registry.Register<Greenshot.Plugin.ExternalCommand.Recipes.ExternalCommandStep>(config => new Greenshot.Plugin.ExternalCommand.Recipes.ExternalCommandStep(config));
            return registry;
        }

        [Fact]
        public void Registry_ResolvesTheStepTypeCaseInsensitively_AndReplacesARegistration()
        {
            var registry = new StepRegistry();
            var contract = Contract("Print2");
            registry.Register(contract, config => new MockTestStep(config.StepType, _ => Task.CompletedTask));

            Assert.Same(contract, registry.GetContract("print2"));
            Assert.True(registry.IsRegistered("PRINT2"));
            Assert.False(registry.IsRegistered("Echo"));
            Assert.Equal("Print2", ((MockTestStep)registry.CreateStep(new RecipeNodeConfig("n", "Print2"))).Id);

            var replacement = Contract("Print2");
            registry.Register(replacement, config => null);
            Assert.Same(replacement, registry.GetContract("Print2"));
            Assert.Single(registry.RegisteredStepTypes, t => t == "Print2");
        }

        [Fact]
        public void EveryRegisteredStepType_HasAContractMatchingItsImplementation()
        {
            var registry = CreatePipelineRegistry();
            Assert.NotEmpty(registry.Contracts);

            foreach (var contract in registry.Contracts)
            {
                Assert.False(string.IsNullOrWhiteSpace(contract.DisplayName), contract.StepType);
                Assert.NotNull(contract.ImplementationType);

                // The factory registered under the step type creates the class the contract describes
                var step = registry.CreateStep(new RecipeNodeConfig("node", contract.StepType));
                Assert.NotNull(step);
                Assert.IsAssignableFrom(contract.ImplementationType, step);
            }
        }

        [Fact]
        public void EveryWellKnownStepType_IsRegistered()
        {
            var registry = CreatePipelineRegistry();
            var wellKnown = typeof(WellKnownStepTypes).GetFields()
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetValue(null));
            foreach (var stepType in wellKnown)
            {
                Assert.True(registry.IsRegistered(stepType), $"Step type '{stepType}' is not registered");
            }
        }

        [Fact]
        public void PluginSteps_AreRegisteredWithTheirContract()
        {
            var registry = CreatePipelineRegistry();
            var barcode = registry.GetContract(Greenshot.Plugin.Zxing.Recipes.ZxingStep.StepType);
            Assert.NotNull(barcode);
            Assert.Equal(typeof(Greenshot.Plugin.Zxing.Recipes.ZxingStep), barcode.ImplementationType);
            Assert.Contains(barcode.OutputVariables, v => v.Name == "Barcode.Format" && v.Conditional);
        }

        [Fact]
        public void BuiltInRecipes_MatchTheStepContracts()
        {
            var registry = CreatePipelineRegistry();
            var builtIns = RecipeManager.Instance.GetAllRecipes().Where(r => r.IsBuiltIn && !r.IsOverridden).ToList();
            Assert.NotEmpty(builtIns);
            foreach (var recipe in builtIns)
            {
                var contract = RecipeContract.Analyze(recipe, registry);
                Assert.True(contract.ValidationWarnings.Count == 0, $"{recipe.Id}: {string.Join(" | ", contract.ValidationWarnings)}");
            }
        }

        [Fact]
        public void ExampleRecipes_MatchTheStepContracts()
        {
            var registry = CreatePipelineRegistry();
            string examples = FindExamplesDirectory();
            var files = Directory.GetFiles(examples, "*.gsrecipe.json");
            Assert.NotEmpty(files);
            foreach (var file in files)
            {
                foreach (var recipe in RecipeSerializer.LoadListFromFile(file, validate: false))
                {
                    var contract = RecipeContract.Analyze(recipe, registry);
                    Assert.True(contract.ValidationWarnings.Count == 0, $"{Path.GetFileName(file)} / {recipe.Id}: {string.Join(" | ", contract.ValidationWarnings)}");
                }
            }
        }

        /// <summary>
        /// Every step the recipe editor's toolbox offers is a registered step type, and the parameters the editor
        /// gives a new step are ones the step reads, with allowed values.
        /// </summary>
        [Fact]
        public void RecipeEditor_ToolboxAndDefaults_MatchTheStepContracts()
        {
            var registry = CreatePipelineRegistry();
            var contracts = registry.Contracts.ToDictionary(c => c.StepType, StringComparer.OrdinalIgnoreCase);
            foreach (var pluginStep in new[]
                     {
                         typeof(Greenshot.Plugin.Imgur.Recipes.ImgurStep), typeof(Greenshot.Plugin.Jira.Recipes.JiraStep), typeof(Greenshot.Plugin.Confluence.Recipes.ConfluenceStep),
                         typeof(Greenshot.Plugin.Office.Recipes.OfficeStep), typeof(Greenshot.Plugin.Box.Recipes.BoxStep), typeof(Greenshot.Plugin.Dropbox.Recipes.DropboxStep)
                     })
            {
                var contract = StepContractBuilder.FromType(pluginStep);
                contracts[contract.StepType] = contract;
            }

            string xaml = File.ReadAllText(Path.Combine(FindRepositoryDirectory(), "src", "Greenshot.Plugin.RecipeEditor", "Views", "RecipeEditorWindow.xaml"));
            var toolboxStepTypes = System.Text.RegularExpressions.Regex.Matches(xaml, "Command=\"\\{Binding AddStepCommand\\}\" CommandParameter=\"(\\w+)\"")
                .Cast<System.Text.RegularExpressions.Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();
            Assert.NotEmpty(toolboxStepTypes);

            var setDefaults = typeof(Greenshot.Plugin.RecipeEditor.ViewModels.RecipeEditorViewModel)
                .GetMethod("SetDefaultParametersForStep", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(setDefaults);

            foreach (string stepType in toolboxStepTypes)
            {
                Assert.True(contracts.TryGetValue(stepType, out var contract), $"The toolbox offers '{stepType}', which is not a step type");
                var node = new RecipeNodeConfig("node", stepType);
                setDefaults.Invoke(null, new object[] { node });
                foreach (var parameter in node.Parameters)
                {
                    var declared = contract.FindParameter(parameter.Key);
                    Assert.True(declared != null || contract.AcceptsUndeclaredParameters, $"{stepType}: the editor sets '{parameter.Key}', which the step does not read");
                    if (declared != null && declared.AllowedValues.Count > 0 && parameter.Value is string value)
                    {
                        Assert.Contains(value, declared.AllowedValues, StringComparer.OrdinalIgnoreCase);
                    }
                }
            }
        }

        /// <summary>
        /// Parameters that take a file format do not hard-code the formats, they offer what the file format registry can save.
        /// </summary>
        [Theory]
        [InlineData(typeof(Greenshot.Recipes.Steps.DestinationExportStep))]
        [InlineData(typeof(Greenshot.Plugin.ExternalCommand.Recipes.ExternalCommandStep))]
        [InlineData(typeof(Greenshot.Plugin.Imgur.Recipes.ImgurStep))]
        [InlineData(typeof(Greenshot.Plugin.Jira.Recipes.JiraStep))]
        [InlineData(typeof(Greenshot.Plugin.Confluence.Recipes.ConfluenceStep))]
        public void FormatParameter_AllowedValues_ComeFromTheFileFormatRegistry(Type stepType)
        {
            var registry = SimpleServiceProvider.Current.GetInstance<Greenshot.Base.Core.FileFormat.IFileFormatRegistry>();
            var saveableIds = Greenshot.Base.Core.FileFormat.FileFormatRegistryExtensions.GetSaveableFileFormats(registry).Select(f => f.Id).ToList();
            Assert.Contains("png", saveableIds, StringComparer.OrdinalIgnoreCase);

            var format = StepContractBuilder.FromType(stepType).FindParameter("Format");
            Assert.NotNull(format);
            Assert.IsType<Greenshot.Base.Core.FileFormat.SaveableFileFormatIds>(format.AllowedValuesProvider);
            Assert.Equal(saveableIds.OrderBy(id => id), format.AllowedValues.OrderBy(id => id));
        }

        /// <summary>
        /// The provider is asked every time, so values registered after the contract was built (e.g. by a plugin) are allowed too.
        /// </summary>
        [Fact]
        public void AllowedValuesProvider_IsEvaluatedWhenRead()
        {
            var parameter = StepContractBuilder.FromType(typeof(ProviderTestStep)).FindParameter("Value");
            ProviderTestValues.Values = new[] { "a" };
            Assert.Equal(new[] { "a" }, parameter.AllowedValues);
            ProviderTestValues.Values = new[] { "a", "b" };
            Assert.Equal(new[] { "a", "b" }, parameter.AllowedValues);
        }

        [Fact]
        public void AllowedValuesProvider_MustImplementTheInterface()
        {
            Assert.Throws<ArgumentException>(() => StepContractBuilder.FromType(typeof(InvalidProviderTestStep)));
        }

        public sealed class ProviderTestValues : IAllowedValuesProvider
        {
            public static IReadOnlyList<string> Values { get; set; } = Array.Empty<string>();
            public IReadOnlyList<string> GetAllowedValues() => Values;
        }

        [StepParameter("Value", ContractDataType.Enum, AllowedValuesProvider = typeof(ProviderTestValues))]
        private sealed class ProviderTestStep
        {
        }

        [StepParameter("Value", ContractDataType.Enum, AllowedValuesProvider = typeof(string))]
        private sealed class InvalidProviderTestStep
        {
        }

        private static string FindRepositoryDirectory() => Path.GetDirectoryName(Path.GetDirectoryName(FindExamplesDirectory()));

        private static string FindExamplesDirectory()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, "docs", "examples");
                if (Directory.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }
            throw new DirectoryNotFoundException("docs/examples not found above " + AppDomain.CurrentDomain.BaseDirectory);
        }

        // --- Graph analysis ------------------------------------------------------------------------------------

        private static StepRegistry CreateAnalysisRegistry()
        {
            var registry = new StepRegistry();
            registry.Register(Contract("Capture", rawCapture: PayloadRequirement.Created), config => null);
            registry.Register(Contract("ProduceA", outputs: new[] { new VariableContract("A") }), config => null);
            registry.Register(Contract("ProduceB", outputs: new[] { new VariableContract("B") }), config => null);
            registry.Register(Contract("MaybeA", outputs: new[] { new VariableContract("A") { Conditional = true } }), config => null);
            registry.Register(Contract("Use", parameters: new[] { new ParameterContract("Text") }), config => null);
            registry.Register(Contract("NeedsImage", rawCapture: PayloadRequirement.Required), config => null);
            registry.Register(Contract("Strict", parameters: new[]
            {
                new ParameterContract("Mode", ContractDataType.Enum, required: true, allowedValues: new[] { "Fast", "Slow" }),
                new ParameterContract("Size", ContractDataType.Integer)
            }), config => null);
            registry.Register(Contract("Branch"), config => null);
            return registry;
        }

        private static RecipeNodeConfig Use(string id, string text) => new RecipeNodeConfig(id, "Use").Set("Text", text);

        private static IReadOnlyList<string> Warnings(CaptureRecipe recipe) => RecipeContract.Analyze(recipe, CreateAnalysisRegistry()).ValidationWarnings;

        [Fact]
        public void Analyze_FollowsTheFlow_NotTheNodeList()
        {
            // The consumer is listed first, but runs after the producer
            var recipe = new CaptureRecipe("order", "Order")
                .AddNode(Use("use", "${A}"))
                .AddNode(new RecipeNodeConfig("produce", "ProduceA"));
            recipe.Flow = new RecipeFlowConfig("produce").AddTransition("produce", "use");
            Assert.Empty(Warnings(recipe));

            // Listed first and running first: A is not set yet
            var wrong = new CaptureRecipe("order2", "Order 2")
                .AddNode(new RecipeNodeConfig("produce", "ProduceA"))
                .AddNode(Use("use", "${A}"));
            wrong.Flow = new RecipeFlowConfig("use").AddTransition("use", "produce");
            Assert.Contains(Warnings(wrong), w => w.Contains("'use'") && w.Contains("${A}") && w.Contains("do not run before it"));
        }

        [Fact]
        public void Analyze_VariableSetInOnlyOneBranch_IsReportedAtTheMerge()
        {
            var recipe = new CaptureRecipe("branches", "Branches")
                .AddNode(new RecipeNodeConfig("decide", "Branch"))
                .AddNode(new RecipeNodeConfig("left", "ProduceA"))
                .AddNode(new RecipeNodeConfig("right", "ProduceB"))
                .AddNode(Use("merge", "${A} ${B}"));
            recipe.Flow = new RecipeFlowConfig("decide")
                .AddConditionalTransition("decide", "yes", "left")
                .AddConditionalTransition("decide", "no", "right")
                .AddTransition("left", "merge")
                .AddTransition("right", "merge");

            var warnings = Warnings(recipe);
            Assert.Contains(warnings, w => w.Contains("'merge'") && w.Contains("${A}") && w.Contains("not set on every path"));
            Assert.Contains(warnings, w => w.Contains("'merge'") && w.Contains("${B}") && w.Contains("not set on every path"));
        }

        [Fact]
        public void Analyze_VariableSetInEveryBranch_IsGuaranteed()
        {
            var recipe = new CaptureRecipe("both", "Both")
                .AddNode(new RecipeNodeConfig("decide", "Branch"))
                .AddNode(new RecipeNodeConfig("left", "ProduceA"))
                .AddNode(new RecipeNodeConfig("right", "ProduceA"))
                .AddNode(Use("merge", "${A}"));
            recipe.Flow = new RecipeFlowConfig("decide")
                .AddConditionalTransition("decide", "yes", "left")
                .AddConditionalTransition("decide", "no", "right")
                .AddTransition("left", "merge")
                .AddTransition("right", "merge");
            Assert.Empty(Warnings(recipe));
        }

        [Fact]
        public void Analyze_ParallelBranchesJoining_ProvideBothVariables()
        {
            // Without a conditional both branches run, and the join waits for both
            var recipe = new CaptureRecipe("fork", "Fork")
                .AddNode(new RecipeNodeConfig("start", "Capture"))
                .AddNode(new RecipeNodeConfig("left", "ProduceA"))
                .AddNode(new RecipeNodeConfig("right", "ProduceB"))
                .AddNode(Use("join", "${A} ${B}"));
            recipe.Flow = new RecipeFlowConfig("start")
                .AddTransition("start", "left")
                .AddTransition("start", "right")
                .AddTransition("left", "join")
                .AddTransition("right", "join");
            Assert.Empty(Warnings(recipe));
        }

        [Fact]
        public void Analyze_ParallelBranch_DoesNotProvideForTheOtherBranch()
        {
            var recipe = new CaptureRecipe("siblings", "Siblings")
                .AddNode(new RecipeNodeConfig("start", "Capture"))
                .AddNode(new RecipeNodeConfig("left", "ProduceA"))
                .AddNode(Use("right", "${A}"));
            recipe.Flow = new RecipeFlowConfig("start").AddTransition("start", "left").AddTransition("start", "right");
            Assert.Contains(Warnings(recipe), w => w.Contains("'right'") && w.Contains("do not run before it"));
        }

        [Fact]
        public void Analyze_ConditionalOutput_IsNotGuaranteed_ButUsingItAfterItsStepIsFine()
        {
            var recipe = new CaptureRecipe("maybe", "Maybe")
                .AddNode(new RecipeNodeConfig("scan", "MaybeA"))
                .AddNode(Use("use", "${A}"));
            recipe.Flow = new RecipeFlowConfig("scan").AddTransition("scan", "use");

            var contract = RecipeContract.Analyze(recipe, CreateAnalysisRegistry());
            Assert.Empty(contract.ValidationWarnings);
            Assert.DoesNotContain("A", contract.Steps.Single(s => s.NodeId == "use").GuaranteedVariables);
            Assert.Contains(contract.Outputs, o => o.Name == "A" && o.Conditional);
        }

        [Fact]
        public void Analyze_ErrorTransition_ProvidesTheErrorVariablesButNotTheFailedOutputs()
        {
            var recipe = new CaptureRecipe("errors", "Errors")
                .AddNode(new RecipeNodeConfig("risky", "ProduceA"))
                .AddNode(Use("ok", "${A}"))
                .AddNode(Use("handler", "${LastError} ${A}"));
            recipe.Flow = new RecipeFlowConfig("risky").AddTransition("risky", "ok").AddErrorTransition("risky", "handler");

            var warnings = Warnings(recipe);
            Assert.DoesNotContain(warnings, w => w.Contains("'ok'"));
            Assert.DoesNotContain(warnings, w => w.Contains("${LastError}"));
            Assert.Contains(warnings, w => w.Contains("'handler'") && w.Contains("${A}"));
        }

        [Fact]
        public void Analyze_ReportsUnreachableNodes_AndMissingImages()
        {
            var recipe = new CaptureRecipe("unreachable", "Unreachable")
                .AddNode(new RecipeNodeConfig("start", "NeedsImage"))
                .AddNode(new RecipeNodeConfig("orphan", "ProduceA"));
            recipe.Flow = new RecipeFlowConfig("start");

            var warnings = Warnings(recipe);
            Assert.Contains(warnings, w => w.Contains("'orphan'") && w.Contains("never executed"));
            Assert.Contains(warnings, w => w.Contains("'start'") && w.Contains("needs an image"));
        }

        [Fact]
        public void Analyze_ExtensionTrigger_ProvidesTheImage()
        {
            var recipe = new CaptureRecipe("extension", "Extension")
                .AddNode(new RecipeNodeConfig("use", "NeedsImage"))
                .AddTrigger(new TriggerConfig(TriggerConfig.TypeExtension, "Browser") { Enabled = true });
            recipe.Flow = new RecipeFlowConfig("use");
            Assert.Empty(Warnings(recipe));
        }

        [Fact]
        public void Analyze_CommandlineArguments_AreInputs_OptionalOnesAreNotGuaranteed_ButMayBeUsed()
        {
            var recipe = new CaptureRecipe("cli", "Cli")
                .AddNode(Use("use", "${Required} ${WithDefault} ${Optional}"))
                .AddTrigger(TriggerConfig.CreateCommandline("cli", arguments: new[]
                {
                    new CommandlineArgument { Name = "required", Variable = "Required", Required = true },
                    new CommandlineArgument { Name = "with-default", Variable = "WithDefault", DefaultValue = "x" },
                    new CommandlineArgument { Name = "optional", Variable = "Optional" }
                }));
            recipe.Flow = new RecipeFlowConfig("use");

            var contract = RecipeContract.Analyze(recipe, CreateAnalysisRegistry());
            Assert.Contains(contract.Inputs, i => i.Name == "Required" && i.Required);
            Assert.Contains(contract.Inputs, i => i.Name == "Optional" && !i.Required);
            // An argument that was not given is simply empty
            Assert.Empty(contract.ValidationWarnings);
            var guaranteed = contract.Steps.Single(s => s.NodeId == "use").GuaranteedVariables;
            Assert.Contains("Required", guaranteed);
            Assert.Contains("WithDefault", guaranteed);
            Assert.DoesNotContain("Optional", guaranteed);
        }

        [Fact]
        public void Analyze_ChecksParameters()
        {
            var recipe = new CaptureRecipe("params", "Params")
                .AddNode(new RecipeNodeConfig("missing", "Strict").Set("Size", 3))
                .AddNode(new RecipeNodeConfig("wrong", "Strict").Set("Mode", "Medium").Set("Colour", "red"))
                .AddNode(new RecipeNodeConfig("fine", "Strict") { OnErrorNodeId = "missing" }.Set("Mode", "fast"));
            recipe.Flow = new RecipeFlowConfig(new[] { "missing", "wrong", "fine" });

            var warnings = Warnings(recipe);
            Assert.Contains(warnings, w => w.Contains("'missing'") && w.Contains("missing required parameter 'Mode'"));
            Assert.Contains(warnings, w => w.Contains("'wrong'") && w.Contains("'Medium'"));
            Assert.Contains(warnings, w => w.Contains("'wrong'") && w.Contains("'Colour'"));
            Assert.DoesNotContain(warnings, w => w.Contains("'fine'"));
            Assert.DoesNotContain(warnings, w => w.Contains("'Size'"));
        }

        [Fact]
        public void Analyze_NodeSpecificOutputNames_AreResolved()
        {
            var registry = CreatePipelineRegistry();
            var recipe = new CaptureRecipe("prompt", "Prompt")
                .AddNode(new RecipeNodeConfig("set", WellKnownStepTypes.SetVariable).Set("Variable", "Greeting").Set("Value", "hi"))
                .AddNode(new RecipeNodeConfig("print", WellKnownStepTypes.Stdout).Set("Text", "${Greeting} ${UserChoice.set}"));
            recipe.Flow = new RecipeFlowConfig("set").AddTransition("set", "print");

            var contract = RecipeContract.Analyze(recipe, registry);
            Assert.Contains(contract.Outputs, o => o.Name == "Greeting");
            Assert.Empty(contract.ValidationWarnings);
        }

        // --- Engine checks ---------------------------------------------------------------------------------------

        [Fact]
        public async Task Engine_MissingRequiredParameter_FailsTheNode()
        {
            var registry = CreateAnalysisRegistry();
            bool ran = false;
            registry.Register(Contract("Strict", parameters: new[] { new ParameterContract("Mode", required: true) }),
                config => new MockTestStep(config.Id, _ => { ran = true; return Task.CompletedTask; }));

            var recipe = new CaptureRecipe("strict", "Strict").AddNode(new RecipeNodeConfig("node", "Strict"));
            recipe.Flow = new RecipeFlowConfig("node");
            using var context = new CaptureFlowContext(recipe);
            await new DagExecutionEngine(registry.CreateStep, registry.GetContract).ExecuteAsync(recipe, context);

            Assert.False(ran);
            Assert.Equal(CaptureFlowState.Failed, context.State);
            Assert.Contains("missing required parameter 'Mode'", context.AbortReason ?? context.Error?.Message);
        }

        [Fact]
        public async Task Engine_ReportsUndeclaredAndMissingOutputs()
        {
            var registry = new StepRegistry();
            registry.Register(Contract("Writer", outputs: new[] { new VariableContract("Declared"), new VariableContract("Sometimes") { Conditional = true } }),
                config => new MockTestStep(config.Id, ctx => { ctx.Properties["Undeclared"] = 1; return Task.CompletedTask; }));

            var recipe = new CaptureRecipe("writer", "Writer").AddNode(new RecipeNodeConfig("node", "Writer"));
            recipe.Flow = new RecipeFlowConfig("node");
            var violations = new List<string>();
            var engine = new DagExecutionEngine(registry.CreateStep, registry.GetContract) { ContractViolation = violations.Add };
            using var context = new CaptureFlowContext(recipe);
            await engine.ExecuteAsync(recipe, context);

            Assert.Equal(2, violations.Count);
            Assert.Contains(violations, v => v.Contains("'Undeclared'"));
            Assert.Contains(violations, v => v.Contains("'Declared'"));
        }

        [Fact]
        public async Task Engine_StepsThatKeepTheirContract_ReportNothing()
        {
            var registry = CreatePipelineRegistry();
            var recipe = new CaptureRecipe("contracted", "Contracted")
                .AddNode(new RecipeNodeConfig("set", WellKnownStepTypes.SetVariable).Set("Variables", new Dictionary<string, object> { ["One"] = "1", ["Two"] = "2" }))
                .AddNode(new RecipeNodeConfig("out", WellKnownStepTypes.Stdout).Set("Text", "${One}"))
                .AddNode(new RecipeNodeConfig("err", WellKnownStepTypes.Stderr).Set("Text", "bad").Set("ExitCode", 4).Set("Abort", false));
            recipe.Flow = new RecipeFlowConfig("set").AddTransition("set", "out").AddTransition("out", "err");

            var violations = new List<string>();
            var engine = new DagExecutionEngine(registry.CreateStep, registry.GetContract) { ContractViolation = violations.Add };
            using var bitmap = new Bitmap(4, 4);
            using var context = new CaptureFlowContext(recipe) { Payload = new CapturePayload(new Capture((Image)bitmap.Clone())) };
            await engine.ExecuteAsync(recipe, context);

            Assert.Empty(violations);
            Assert.Equal(4, context.ExitCode);
        }
    }
}
