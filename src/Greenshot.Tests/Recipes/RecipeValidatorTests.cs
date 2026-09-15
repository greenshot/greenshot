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
            StepRegistry.Instance.RegisterStepFactory(WellKnownStepTypes.Destinations, config => new DestinationExportStep(config));
            StepRegistry.Instance.RegisterStepFactory("ExternalCommand", config => new ExternalCommandStep(config));
            StepRegistry.Instance.RegisterStepFactory("ExternalCommand.MS Paint", config => new ExternalCommandStep(config));
        }

        [Fact]
        public void Validate_ValidDagRecipe_PassesValidation()
        {
            var recipe = new CaptureRecipe("valid_recipe", "Valid Recipe")
                .AddNode(new RecipeNodeConfig { Id = "start", StepType = "Source" })
                .AddNode(new RecipeNodeConfig { Id = "process", StepType = "Border", Parameters = new Dictionary<string, object> { { "Width", 2 } } })
                .AddNode(new RecipeNodeConfig { Id = "end", StepType = "Clipboard" });

            recipe.Flow = new RecipeFlowConfig("start")
                .AddTransition("start", "process")
                .AddTransition("process", "end");

            var result = RecipeValidator.Validate(recipe);
            Assert.True(result.IsValid, string.Join(", ", result.Errors));
        }

        [Fact]
        public void Validate_CycleInFlow_DetectsLoopError()
        {
            var recipe = new CaptureRecipe("cycle_recipe", "Cycle Recipe")
                .AddNode(new RecipeNodeConfig { Id = "a", StepType = "Source" })
                .AddNode(new RecipeNodeConfig { Id = "b", StepType = "Border" })
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
        public void Validate_RouteA_DottedStepTypeAndPath_FlagsExternalCommands()
        {
            var recipe = new CaptureRecipe("route_a_recipe", "Route A PoC")
                .AddNode(new RecipeNodeConfig { Id = "start", StepType = "Source" })
                .AddNode(new RecipeNodeConfig
                {
                    Id = "cmd_step",
                    StepType = "ExternalCommand.MS Paint",
                    Parameters = new Dictionary<string, object>
                    {
                        { "Path", @"C:\Windows\System32\cmd.exe" },
                        { "Arguments", "\"{0}\"" }
                    }
                });

            recipe.Flow = new RecipeFlowConfig("start").AddTransition("start", "cmd_step");

            var result = RecipeValidator.Validate(recipe);
            Assert.True(result.HasExternalCommands, "Route A dotted step type with Path parameter must be flagged as having external commands.");
            Assert.Contains(result.ExternalCommands, c => c.Contains(@"C:\Windows\System32\cmd.exe") || c.Contains("MS Paint"));
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
                        { "Destinations", new List<string> { "External MS Paint" } }
                    }
                });

            recipe.Flow = new RecipeFlowConfig("start").AddTransition("start", "dest_step");

            var result = RecipeValidator.Validate(recipe);
            Assert.True(result.HasExternalCommands, "Route B Destinations step with External designation must be flagged as having external commands.");
            Assert.Contains(result.ExternalCommands, c => c.Contains("MS Paint"));
        }

        [Fact]
        public void Validate_RouteB_ScalarDestinationString_FlagsExternalCommands()
        {
            var extDest = new ExternalCommandDestination("MS Paint");
            SimpleServiceProvider.Current.AddService<IDestination>(extDest);

            var recipe = new CaptureRecipe("route_b_scalar", "Route B Scalar PoC")
                .AddNode(new RecipeNodeConfig { Id = "start", StepType = "Source" })
                .AddNode(new RecipeNodeConfig
                {
                    Id = "dest_step",
                    StepType = "Destinations",
                    Parameters = new Dictionary<string, object>
                    {
                        { "Destinations", "External MS Paint" }
                    }
                });

            recipe.Flow = new RecipeFlowConfig("start").AddTransition("start", "dest_step");

            var result = RecipeValidator.Validate(recipe);
            Assert.True(result.HasExternalCommands, "Route B Destinations step with scalar Destinations string must be flagged.");
            Assert.Contains(result.ExternalCommands, c => c.Contains("MS Paint"));
        }

        [Fact]
        public void Validate_RouteB_CommaSeparatedDestinationsString_FlagsExternalCommands()
        {
            var extDest = new ExternalCommandDestination("MS Paint");
            SimpleServiceProvider.Current.AddService<IDestination>(extDest);

            var recipe = new CaptureRecipe("route_b_comma", "Route B Comma PoC")
                .AddNode(new RecipeNodeConfig { Id = "start", StepType = "Source" })
                .AddNode(new RecipeNodeConfig
                {
                    Id = "dest_step",
                    StepType = "Destinations",
                    Parameters = new Dictionary<string, object>
                    {
                        { "Destinations", "Clipboard, External MS Paint" }
                    }
                });

            recipe.Flow = new RecipeFlowConfig("start").AddTransition("start", "dest_step");

            var result = RecipeValidator.Validate(recipe);
            Assert.True(result.HasExternalCommands, "Route B comma-separated destinations string must be flagged as having external commands.");
            Assert.Contains(result.ExternalCommands, c => c.Contains("MS Paint"));
        }

        [Fact]
        public void Validate_SafeBuiltInRecipe_HasExternalCommandsFalse()
        {
            var recipe = new CaptureRecipe("safe_recipe", "Safe Recipe")
                .AddNode(new RecipeNodeConfig { Id = "start", StepType = "Source" })
                .AddNode(new RecipeNodeConfig { Id = "border", StepType = "Border", Parameters = new Dictionary<string, object> { { "Width", 2 } } })
                .AddNode(new RecipeNodeConfig { Id = "clip", StepType = "Clipboard" });

            recipe.Flow = new RecipeFlowConfig("start")
                .AddTransition("start", "border")
                .AddTransition("border", "clip");

            var result = RecipeValidator.Validate(recipe);
            Assert.True(result.IsValid);
            Assert.False(result.HasExternalCommands);
            Assert.Empty(result.ExternalCommands);
        }

        private class MockAuthorizedDestination : AbstractDestination, IRequiresRecipeAuthorization
        {
            public override string Designation => "SecurityAuditDestination";
            public override string Description => "Custom Security Destination";
            public override IEnumerable<IDestination> DynamicDestinations() => Enumerable.Empty<IDestination>();
            public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) => null;
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
                        { "Destinations", "SecurityAuditDestination" }
                    }
                });

            recipe.Flow = new RecipeFlowConfig("start").AddTransition("start", "dest_step");

            var result = RecipeValidator.Validate(recipe);
            Assert.True(result.HasExternalCommands, "Destination implementing IRequiresRecipeAuthorization must be flagged even without 'External ' prefix.");
            Assert.Contains(result.ExternalCommands, c => c.Contains(@"C:\Security\audit.exe"));
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
    }
}