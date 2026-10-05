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
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Expressions;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Pipeline.Contracts;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Greenshot.Recipes.Steps;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    public class RecipeContractAndStderrTests
    {
        public RecipeContractAndStderrTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public async Task StderrStep_EmitsToStderrWriter_SetsExitCode_AndAborts()
        {
            var recipe = new CaptureRecipe { Id = "test_error_recipe", Name = "Error Test" };
            using var context = new CaptureFlowContext(recipe);
            context.Properties["ErrorCode"] = "ERR_404";

            string capturedStderr = null;
            context.StderrWriter = text =>
            {
                capturedStderr = text;
                return Task.CompletedTask;
            };

            var nodeConfig = new RecipeNodeConfig
            {
                Id = "stderr_node",
                StepType = WellKnownStepTypes.Stderr,
                Parameters = new Dictionary<string, object>
                {
                    { "text", "Fatal error occurred with code: ${ErrorCode}" },
                    { "exitCode", 42 },
                    { "abort", true }
                }
            };

            await SingleNodeRunner.RunAsync(nodeConfig, cfg => new StderrStep(cfg), context);

            Assert.Equal("Fatal error occurred with code: ERR_404", capturedStderr);
            Assert.Equal(42, context.ExitCode);
            Assert.True(context.IsAborted);
            Assert.Equal("Fatal error occurred with code: ERR_404", context.AbortReason);
        }

        [Fact]
        public async Task StderrStep_DoesNotAbort_WhenAbortIsFalse()
        {
            var recipe = new CaptureRecipe { Id = "test_warn_recipe", Name = "Warning Test" };
            using var context = new CaptureFlowContext(recipe);

            string capturedStderr = null;
            context.StderrWriter = text =>
            {
                capturedStderr = text;
                return Task.CompletedTask;
            };

            var nodeConfig = new RecipeNodeConfig
            {
                Id = "stderr_node",
                StepType = WellKnownStepTypes.Stderr,
                Parameters = new Dictionary<string, object>
                {
                    { "Text", "A non-fatal error occurred" },
                    { "ExitCode", 2 },
                    { "Abort", false }
                }
            };

            await SingleNodeRunner.RunAsync(nodeConfig, cfg => new StderrStep(cfg), context);

            Assert.Equal("A non-fatal error occurred", capturedStderr);
            Assert.Equal(2, context.ExitCode);
            Assert.False(context.IsAborted);
        }

        [Fact]
        public void StepContractBuilder_ExtractsContractFromAttributes()
        {
            var contract = StepContractBuilder.FromType(typeof(StderrStep));
            Assert.NotNull(contract);
            Assert.Equal(WellKnownStepTypes.Stderr, contract.StepType);
            Assert.Equal("Stderr Output", contract.DisplayName);
            Assert.Equal("Diagnostics", contract.Category);

            Assert.Contains(contract.Parameters, p => p.Matches("text") && !p.Matches("message") && p.DataType == ContractDataType.String);
            Assert.Contains(contract.Parameters, p => p.Matches("exitCode") && p.DataType == ContractDataType.Integer && Convert.ToInt32(p.DefaultValue) == 1);
            Assert.Contains(contract.Parameters, p => p.Matches("abort") && p.DataType == ContractDataType.Boolean);
            Assert.Contains(contract.OutputVariables, v => v.Name == "ExitCode");
            Assert.Equal(typeof(StderrStep), contract.ImplementationType);
        }

        [Fact]
        public void RecipeContract_Analyze_ComputesInputsOutputsAndLifecycle()
        {
            var recipe = new CaptureRecipe
            {
                Id = "qr_extractor",
                Name = "QR Code Extractor",
                Description = "Extracts QR codes and outputs to stdout",
                Triggers = new List<TriggerConfig>
                {
                    new TriggerConfig
                    {
                        TriggerType = "Commandline",
                        Parameters = new Dictionary<string, object>
                        {
                            { "Command", "qr" },
                            { "Arguments", new List<CommandlineArgument>
                                {
                                    new CommandlineArgument { Name = "file", Variable = "Filename", Required = true, Description = "Input image file" },
                                    new CommandlineArgument { Name = "format", Variable = "Format", Required = false, DefaultValue = "png", Description = "Image format" }
                                }
                            },
                            { "Stdout", "${QrText}" }
                        }
                    }
                },
                Nodes = new List<RecipeNodeConfig>
                {
                    new RecipeNodeConfig
                    {
                        Id = "source_file",
                        StepType = WellKnownStepTypes.Source,
                        Parameters = new Dictionary<string, object>
                        {
                            { "sourceType", "File" },
                            { "filename", "${Filename}" }
                        }
                    },
                    new RecipeNodeConfig
                    {
                        Id = "zxing_qr",
                        StepType = Greenshot.Plugin.Zxing.ZxingStep.StepType,
                        Parameters = new Dictionary<string, object>()
                    },
                    new RecipeNodeConfig
                    {
                        Id = "print_qr",
                        StepType = WellKnownStepTypes.Stdout,
                        Parameters = new Dictionary<string, object>
                        {
                            { "text", "${Barcode.Text}" }
                        }
                    }
                }
            };
            // Without transitions only the first node would run: the analysis follows the graph, not the node list
            recipe.Flow = new RecipeFlowConfig("source_file")
                .AddTransition("source_file", "zxing_qr")
                .AddTransition("zxing_qr", "print_qr");

            var registry = new StepRegistry();
            registry.Register<SourceAcquisitionStep>(config => new SourceAcquisitionStep(config));
            registry.Register<StdoutStep>(config => new StdoutStep(config));
            registry.Register<Greenshot.Plugin.Zxing.ZxingStep>(config => new Greenshot.Plugin.Zxing.ZxingStep(config));

            var contract = RecipeContract.Analyze(recipe, registry);
            Assert.NotNull(contract);
            Assert.Equal("qr_extractor", contract.RecipeId);

            // Inputs computed from CommandlineTrigger arguments
            Assert.Contains(contract.Inputs, i => i.Name == "Filename" && i.Required);
            Assert.Contains(contract.Inputs, i => i.Name == "Format" && !i.Required && (string)i.ExampleValue == "png");

            // Outputs: the barcode text is only set when a barcode was found
            Assert.Contains(contract.Outputs, o => o.Name == "Barcode.Text" && o.Conditional);
            Assert.Empty(contract.ValidationWarnings);

            // Lifecycle
            Assert.True(contract.AcquiresImage);
            Assert.True(contract.ExtractsText);
        }

        [Fact]
        public void ExpressionEvaluator_EvaluatesPayloadAndContextProperties()
        {
            var recipe = new CaptureRecipe("test", "Test");
            using var bmp = new Bitmap(800, 600);
            var capture = new Capture((Image)bmp.Clone());
            capture.CaptureDetails.Title = "Application Window";

            using var context = new CaptureFlowContext(recipe)
            {
                Payload = new CapturePayload(capture)
                {
                    ExtractedText = "Scanned Content 123"
                }
            };
            context.Properties["CustomParam"] = "GreenshotAwesome";

            // Context property
            string res1 = ExpressionEvaluator.Instance.Evaluate<string>("${CustomParam}", context);
            Assert.Equal("GreenshotAwesome", res1);

            // Payload properties
            string res2 = ExpressionEvaluator.Instance.Evaluate<string>("${Payload.Width}x${Payload.Height}", context);
            Assert.Equal("800x600", res2);

            string res3 = ExpressionEvaluator.Instance.Evaluate<string>("${Payload.ExtractedText}", context);
            Assert.Equal("Scanned Content 123", res3);
        }
    }
}
