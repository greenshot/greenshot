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
using System.Linq;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Greenshot.Pipeline.Steps;
using Greenshot.Plugin.ExternalCommand;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    public class RecipeValidatorTests
    {
        public RecipeValidatorTests()
        {
            TestEnvironment.EnsureInitialized();
            StepRegistry.Instance.Register<DestinationExportStep>(config => new DestinationExportStep(config));
            StepRegistry.Instance.Register<ExternalCommandStep>(config => new ExternalCommandStep(config));
        }

        [Fact]
        public void Validate_ValidDagRecipe_PassesValidation()
        {
            var recipe = new CaptureRecipe("valid_recipe", "Valid Recipe")
                .AddNode(new RecipeNodeConfig { Id = "start", StepType = "Source" })
                .AddNode(new RecipeNodeConfig { Id = "process", StepType = "Effect", Parameters = new Dictionary<string, object> { { "Effect", "Border" }, { "Width", 2 } } })
                .AddNode(new RecipeNodeConfig { Id = "end", StepType = "Clipboard" });

            recipe.Flow = new RecipeFlowConfig("start")
                .AddTransition("start", "process")
                .AddTransition("process", "end");

            var result = RecipeValidator.Validate(recipe);
            Assert.True(result.IsValid, string.Join(", ", result.Errors));
        }

        [Theory]
        [InlineData(".png;.jpg", true)]
        [InlineData(".png", true)]
        [InlineData("*.png;*.jpg", false)]
        [InlineData("png", false)]
        [InlineData(".png,.jpg", false)]
        public void Validate_OpenFileFilter_AcceptsOnlyExtensionsSeparatedBySemicolons(string filter, bool valid)
        {
            var recipe = new CaptureRecipe("open_file_filter", "Open File Filter")
                .AddNode(new RecipeNodeConfig { Id = "start", StepType = "Source" })
                .AddNode(new RecipeNodeConfig { Id = "end", StepType = "Clipboard" })
                .AddTrigger(TriggerConfig.CreateOpenFile(filter));
            recipe.Flow = new RecipeFlowConfig("start").AddTransition("start", "end");

            var result = RecipeValidator.Validate(recipe);
            Assert.Equal(valid, !result.Errors.Any(e => e.Contains("Filter")));
        }

        [Fact]
        public void Validate_CycleInFlow_DetectsLoopError()
        {
            var recipe = new CaptureRecipe("cycle_recipe", "Cycle Recipe")
                .AddNode(new RecipeNodeConfig { Id = "a", StepType = "Source" })
                .AddNode(new RecipeNodeConfig { Id = "b", StepType = "Effect" })
                .AddNode(new RecipeNodeConfig { Id = "c", StepType = "Clipboard" });

            recipe.Flow = new RecipeFlowConfig("a")
                .AddTransition("a", "b")
                .AddTransition("b", "c")
                .AddTransition("c", "a"); // Loop back to A

            var result = RecipeValidator.Validate(recipe);
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Cycle/loop detected"));
        }

        [Fact]
        public void Validate_MissingStartNodeOrDuplicateId_ReportsErrors()
        {
            var recipe = new CaptureRecipe("dup_recipe", "Duplicate Recipe")
                .AddNode(new RecipeNodeConfig { Id = "same_id", StepType = "Source" })
                .AddNode(new RecipeNodeConfig { Id = "same_id", StepType = "Clipboard" });

            recipe.Flow = new RecipeFlowConfig("non_existent_start")
                .AddTransition("same_id", "target_missing");

            var result = RecipeValidator.Validate(recipe);
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Duplicate node id"));
            Assert.Contains(result.Errors, e => e.Contains("does not match any defined node id"));
        }

        [Fact]
        public void Validate_RouteA_ExternalCommandStep_FlagsExternalCommands()
        {
            var recipe = new CaptureRecipe("route_a_recipe", "Route A PoC")
                .AddNode(new RecipeNodeConfig { Id = "start", StepType = "Source" })
                .AddNode(new RecipeNodeConfig
                {
                    Id = "cmd_step",
                    StepType = "ExternalCommand",
                    Parameters = new Dictionary<string, object>
                    {
                        { "CommandLine", @"C:\Windows\System32\cmd.exe" },
                        { "Arguments", "\"{0}\"" }
                    }
                });

            recipe.Flow = new RecipeFlowConfig("start").AddTransition("start", "cmd_step");

            var result = RecipeValidator.Validate(recipe);
            Assert.True(result.HasGatedActions, "An ExternalCommand step must be flagged as having external commands.");
            Assert.Contains(result.GatedActions, c => c.Target.Contains(@"C:\Windows\System32\cmd.exe"));
        }

        [Fact]
        public void Validate_RouteB_DestinationsExternal_FlagsExternalCommands()
        {
            var extDest = new ExternalCommandDestination("MS Paint");
            SimpleServiceProvider.Current.AddService<IDestination>(extDest);

            var recipe = new CaptureRecipe("route_b_recipe", "Route B PoC")
                .AddNode(new RecipeNodeConfig { Id = "start", StepType = "Source" })
                .AddNode(new RecipeNodeConfig
                {
                    Id = "dest_step",
                    StepType = "Destinations",
                    Parameters = new Dictionary<string, object>
                    {
                        { "DestinationDesignations", new List<string> { "External MS Paint" } }
                    }
                });

            recipe.Flow = new RecipeFlowConfig("start").AddTransition("start", "dest_step");

            var result = RecipeValidator.Validate(recipe);
            Assert.True(result.HasGatedActions, "Route B Destinations step with External designation must be flagged as having external commands.");
            Assert.Contains(result.GatedActions, c => c.Target.IndexOf("Paint", StringComparison.OrdinalIgnoreCase) >= 0 || c.Target.IndexOf("pbrush", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        [Fact]
        public void Validate_SafeBuiltInRecipe_HasNoGatedActions()
        {
            var recipe = new CaptureRecipe("safe_recipe", "Safe Recipe")
                .AddNode(new RecipeNodeConfig { Id = "start", StepType = "Source" })
                .AddNode(new RecipeNodeConfig { Id = "border", StepType = "Effect", Parameters = new Dictionary<string, object> { { "Effect", "Border" }, { "Width", 2 } } })
                .AddNode(new RecipeNodeConfig { Id = "clip", StepType = "Clipboard" });

            recipe.Flow = new RecipeFlowConfig("start")
                .AddTransition("start", "border")
                .AddTransition("border", "clip");

            var result = RecipeValidator.Validate(recipe);
            Assert.True(result.IsValid);
            Assert.False(result.HasGatedActions);
            Assert.Empty(result.GatedActions);
        }

        private class MockAuthorizedDestination : DestinationBase, IRequiresRecipeAuthorization
        {
            public override string Designation => "SecurityAuditDestination";
            public override DestinationDescriptor Descriptor { get; } = new DestinationDescriptor("Custom Security Destination");
            public override System.Threading.Tasks.Task<ExportResult> ExportAsync(ExportRequest request, System.Threading.CancellationToken cancellationToken)
                => System.Threading.Tasks.Task.FromResult(ExportResult.Declined);
            public IEnumerable<RecipeGatedAction> GetGatedActions()
            {
                yield return new RecipeGatedAction(RecipeGateType.ExternalCommand, @"C:\Security\audit.exe");
            }
        }

        [Fact]
        public void Validate_CustomDestinationImplementingInterface_DetectedWithoutExternalPrefix()
        {
            var mockDest = new MockAuthorizedDestination();
            SimpleServiceProvider.Current.AddService<IDestination>(mockDest);

            var recipe = new CaptureRecipe("custom_auth_dest", "Custom Destination Authorization Test")
                .AddNode(new RecipeNodeConfig { Id = "start", StepType = "Source" })
                .AddNode(new RecipeNodeConfig
                {
                    Id = "dest_step",
                    StepType = "Destinations",
                    Parameters = new Dictionary<string, object>
                    {
                        { "DestinationDesignations", new List<string> { "SecurityAuditDestination" } }
                    }
                });

            recipe.Flow = new RecipeFlowConfig("start").AddTransition("start", "dest_step");

            var result = RecipeValidator.Validate(recipe);
            Assert.True(result.HasGatedActions, "Destination implementing IRequiresRecipeAuthorization must be flagged even without 'External ' prefix.");
            Assert.Contains(result.GatedActions, c => c.Target.Contains(@"C:\Security\audit.exe"));
            Assert.Single(result.GatedActions);
            Assert.Equal(RecipeGateType.ExternalCommand, result.GatedActions[0].GateType);
        }

        [Fact]
        public void Validate_RequiresMissingExtension_ReturnsErrorWithUrl()
        {
            RecipeValidator.ExtensionAvailabilityCheck = req => (false, null);
            try
            {
                var recipe = new CaptureRecipe("req_test", "Requirement Test")
                    .AddNode(new RecipeNodeConfig { Id = "start", StepType = "Source" });
                recipe.Requires = new List<RecipeRequirement>
                {
                    new RecipeRequirement
                    {
                        Id = "Greenshot.Plugin.Zxing",
                        Name = "ZXing Extension",
                        MinVersion = "1.3.0",
                        Url = "https://getgreenshot.org/plugins/zxing"
                    }
                };
                recipe.Flow = new RecipeFlowConfig("start");

                var result = RecipeValidator.Validate(recipe);
                Assert.False(result.IsValid);
                Assert.Contains(result.Errors, e => e.Contains("Greenshot.Plugin.Zxing") && e.Contains("https://getgreenshot.org/plugins/zxing") && e.Contains("v1.3.0+"));
            }
            finally
            {
                RecipeValidator.ExtensionAvailabilityCheck = null;
            }
        }

        [Fact]
        public void Validate_RequiresInstalledExtension_PassesValidation()
        {
            RecipeValidator.ExtensionAvailabilityCheck = req => (true, "1.4.0");
            try
            {
                var recipe = new CaptureRecipe("req_pass", "Requirement Pass")
                    .AddNode(new RecipeNodeConfig { Id = "start", StepType = "Source" });
                recipe.Requires = new List<RecipeRequirement>
                {
                    new RecipeRequirement
                    {
                        Id = "Greenshot.Plugin.Zxing",
                        MinVersion = "1.3.0"
                    }
                };
                recipe.Flow = new RecipeFlowConfig("start");

                var result = RecipeValidator.Validate(recipe);
                Assert.True(result.IsValid, string.Join(", ", result.Errors));
            }
            finally
            {
                RecipeValidator.ExtensionAvailabilityCheck = null;
            }
        }

        [Fact]
        public void Validate_RequiresOutdatedExtension_ReturnsVersionError()
        {
            RecipeValidator.ExtensionAvailabilityCheck = req => (true, "1.1.0");
            try
            {
                var recipe = new CaptureRecipe("req_outdated", "Requirement Outdated")
                    .AddNode(new RecipeNodeConfig { Id = "start", StepType = "Source" });
                recipe.Requires = new List<RecipeRequirement>
                {
                    new RecipeRequirement
                    {
                        Id = "Greenshot.Plugin.Zxing",
                        Name = "ZXing Barcode Plugin",
                        MinVersion = "1.3.0"
                    }
                };
                recipe.Flow = new RecipeFlowConfig("start");

                var result = RecipeValidator.Validate(recipe);
                Assert.False(result.IsValid);
                Assert.Contains(result.Errors, e => e.Contains("version 1.3.0 or newer") && e.Contains("1.1.0"));
            }
            finally
            {
                RecipeValidator.ExtensionAvailabilityCheck = null;
            }
        }

        [Fact]
        public void Validate_UnregisteredCustomAnnotation_ReturnsError()
        {
            var recipe = new CaptureRecipe("unreg_annotation", "Unregistered Annotation Test")
                .AddNode(new RecipeNodeConfig { Id = "start", StepType = "Source" })
                .AddNode(new RecipeNodeConfig
                {
                    Id = "annot_step",
                    StepType = "Annotation",
                    Parameters = new Dictionary<string, object>
                    {
                        ["Annotations"] = new List<object>
                        {
                            new Dictionary<string, object> { ["Type"] = "NonExistentCustomDrawable" }
                        }
                    }
                });
            recipe.Flow = new RecipeFlowConfig("start").AddTransition("start", "annot_step");

            var result = RecipeValidator.Validate(recipe);
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("NonExistentCustomDrawable") && e.Contains("not available"));
        }

        [Fact]
        public void LoadFromFile_RecordActiveWindowRecipe_IsValid()
        {
            // The test brings its own recipe file instead of depending on example files in the repository
            const string recipeJson = @"{
  ""version"": ""1.0"",
  ""id"": ""recipe_record_active_window"",
  ""name"": ""Record Active Window"",
  ""description"": ""Records the active window as video, started with the Pause key"",
  ""triggers"": [
    {
      ""triggerType"": ""Hotkey"",
      ""name"": ""Pause Hotkey"",
      ""enabled"": true,
      ""parameters"": {
        ""hotkey"": ""Pause""
      }
    }
  ],
  ""requires"": [],
  ""nodes"": [
    {
      ""id"": ""record"",
      ""stepType"": ""RecordVideo"",
      ""name"": ""Record Active Window"",
      ""enabled"": true,
      ""parameters"": {
        ""sourceType"": ""ActiveWindow""
      }
    }
  ],
  ""flow"": {
    ""startNodes"": [ ""record"" ]
  }
}";
            string recipePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"greenshot_test_{Guid.NewGuid():N}.gsrecipe.json");
            System.IO.File.WriteAllText(recipePath, recipeJson);
            try
            {
                var recipe = RecipeSerializer.LoadFromFile(recipePath, validate: false);

                Assert.NotNull(recipe);
                Assert.Equal("recipe_record_active_window", recipe.Id);
                Assert.Contains(recipe.Triggers, t => t.TriggerType == "Hotkey" && t.GetParameter<string>("hotkey") == "Pause");
                Assert.Contains(recipe.Nodes, n => n.StepType == WellKnownStepTypes.RecordVideo);

                var validation = RecipeValidator.Validate(recipe);
                Assert.True(validation.IsValid, string.Join("; ", validation.Errors));
            }
            finally
            {
                System.IO.File.Delete(recipePath);
            }
        }
    }
}