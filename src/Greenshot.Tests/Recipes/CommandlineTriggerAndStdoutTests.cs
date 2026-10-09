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
using System.IO;
using System.Threading.Tasks;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Recipes.Sources;
using Greenshot.Base.Recipes.Triggers;
using Greenshot.Recipes.Steps;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    public class CommandlineTriggerAndStdoutTests
    {
        public CommandlineTriggerAndStdoutTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public async Task FileCaptureSource_MissingFilename_AbortsWithClearErrorMessage()
        {
            var recipe = new CaptureRecipe { Id = "test_recipe", Name = "Test" };
            using var context = new CaptureFlowContext(recipe);

            var nodeConfig = new RecipeNodeConfig
            {
                Id = "source_file",
                StepType = "Source",
                Parameters = new Dictionary<string, object>
                {
                    { "sourceType", "File" }
                }
            };

            var source = new FileCaptureSource(nodeConfig);
            var payload = await source.AcquireAsync(context);

            Assert.Null(payload);
            Assert.True(context.IsAborted);
            Assert.Contains("requires a file path", context.AbortReason);
        }

        [Fact]
        public async Task FileCaptureSource_ResolvesFilenameFromNodeConfigExpression()
        {
            // Create a temporary dummy image file
            string tempFile = Path.Combine(Path.GetTempPath(), $"greenshot_test_{Guid.NewGuid():N}.png");
            using (var bmp = new System.Drawing.Bitmap(10, 10))
            {
                bmp.Save(tempFile, System.Drawing.Imaging.ImageFormat.Png);
            }

            try
            {
                var recipe = new CaptureRecipe { Id = "test_recipe", Name = "Test" };
                using var context = new CaptureFlowContext(recipe);
                context.Properties["CustomFilePath"] = tempFile;

                var nodeConfig = new RecipeNodeConfig
                {
                    Id = "source_file",
                    StepType = "Source",
                    Parameters = new Dictionary<string, object>
                    {
                        { "sourceType", "File" },
                        { "filename", "${CustomFilePath}" }
                    }
                };

                // Run through the engine, which resolves "${CustomFilePath}" before the source sees it
                ICapturePayload payload = null;
                await SingleNodeRunner.RunAsync(nodeConfig,
                    cfg => new MockTestStep(cfg.Id, async ctx => payload = await new FileCaptureSource(cfg).AcquireAsync(ctx)),
                    context);

                Assert.NotNull(payload);
                Assert.False(context.IsAborted);
                Assert.Equal(tempFile, payload.RawCapture?.CaptureDetails?.Filename);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public async Task StdoutStep_EvaluatesExpressionAndStreamsOutputImmediately()
        {
            var recipe = new CaptureRecipe { Id = "test_recipe", Name = "Test" };
            using var context = new CaptureFlowContext(recipe);
            context.Properties["ResultText"] = "Hello from pipeline";

            var streamedOutput = new List<string>();
            context.StdoutWriter = text =>
            {
                streamedOutput.Add(text);
                return Task.CompletedTask;
            };

            var nodeConfig = new RecipeNodeConfig
            {
                Id = "stdout_node",
                StepType = WellKnownStepTypes.Stdout,
                Parameters = new Dictionary<string, object>
                {
                    { "text", "Output: ${ResultText}" }
                }
            };

            await SingleNodeRunner.RunAsync(nodeConfig, cfg => new StdoutStep(cfg), context);

            Assert.Single(streamedOutput);
            Assert.Equal("Output: Hello from pipeline", streamedOutput[0]);
            Assert.Equal("Output: Hello from pipeline", context.Properties["LastStdout"]);
            Assert.True((bool)context.Properties["StdoutEmitted"]);
        }

        [Fact]
        public void CommandlineTrigger_HoldsArgumentsAndStdoutDeclarations()
        {
            var args = new List<CommandlineArgument>
            {
                new CommandlineArgument
                {
                    Name = "file",
                    Variable = "Filename",
                    Description = "Input file path",
                    Required = true
                },
                new CommandlineArgument
                {
                    Name = "format",
                    Variable = "Format",
                    Description = "Output format",
                    Required = false,
                    DefaultValue = "jpg"
                }
            };

            var trigger = new CommandlineTrigger(
                "cmd_trigger_1",
                "CLI Convert",
                "recipe_convert",
                "convert",
                "Convert format",
                false,
                "${Destination.Filename}",
                args);

            Assert.Equal("convert", trigger.Command);
            Assert.Equal("${Destination.Filename}", trigger.Stdout);
            Assert.Equal(2, trigger.Arguments.Count);
            Assert.Equal("file", trigger.Arguments[0].Name);
            Assert.Equal("Filename", trigger.Arguments[0].Variable);
            Assert.True(trigger.Arguments[0].Required);
            Assert.Equal("jpg", trigger.Arguments[1].DefaultValue);
        }

        [Fact]
        public void TriggerConfig_CreateCommandline_StoresArgumentsAndStdout()
        {
            var args = new List<CommandlineArgument>
            {
                new CommandlineArgument { Name = "barcode_file", Variable = "Filename", Required = true, Description = "File path" }
            };

            var tc = TriggerConfig.CreateCommandline("qr", "Decode QR code", false, "${Barcode.Text}", args);

            Assert.Equal(TriggerConfig.TypeCommandline, tc.TriggerType);
            Assert.Equal("qr", tc.GetParameter<string>("Command"));
            Assert.Equal("Decode QR code", tc.GetParameter<string>("Description"));
            Assert.False(tc.GetParameter<bool>("FireAndForget"));
            Assert.Equal("${Barcode.Text}", tc.GetParameter<string>("Stdout"));

            var retrievedArgs = tc.GetParameter<List<CommandlineArgument>>("Arguments");
            Assert.NotNull(retrievedArgs);
            Assert.Single(retrievedArgs);
            Assert.Equal("barcode_file", retrievedArgs[0].Name);
            Assert.Equal("Filename", retrievedArgs[0].Variable);
            Assert.True(retrievedArgs[0].Required);
        }

        [Fact]
        public void CaptureFlowContext_CreateBranchContext_PreservesStdoutWriter()
        {
            var recipe = new CaptureRecipe { Id = "test_recipe", Name = "Test" };
            using var parentContext = new CaptureFlowContext(recipe);
            string captured = null;
            parentContext.StdoutWriter = text =>
            {
                captured = text;
                return Task.CompletedTask;
            };

            using var branchContext = parentContext.CreateBranchContext();
            Assert.NotNull(branchContext.StdoutWriter);

            branchContext.StdoutWriter("streamed from branch");
            Assert.Equal("streamed from branch", captured);
        }
    }
}

