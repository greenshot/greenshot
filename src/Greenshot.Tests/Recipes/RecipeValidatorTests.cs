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

using System.Collections.Generic;
using Greenshot.Base.Recipes;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    public class RecipeValidatorTests
    {
        public RecipeValidatorTests()
        {
            TestEnvironment.EnsureInitialized();
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
            Assert.True(result.HasExternalCommands, "Route B Destinations step with External designation must be flagged as having external commands.");
            Assert.Contains(result.ExternalCommands, c => c.Contains("External MS Paint"));
        }

        [Fact]
        public void Validate_RouteB_CustomDestinationExternal_FlagsExternalCommands()
        {
            var recipe = new CaptureRecipe("route_b_custom", "Route B Custom Destination")
                .AddNode(new RecipeNodeConfig { Id = "start", StepType = "Source" })
                .AddNode(new RecipeNodeConfig
                {
                    Id = "dest_step",
                    StepType = "CustomDestination",
                    Parameters = new Dictionary<string, object>
                    {
                        { "CustomDestinationId", "External Tool" }
                    }
                });

            recipe.Flow = new RecipeFlowConfig("start").AddTransition("start", "dest_step");

            var result = RecipeValidator.Validate(recipe);
            Assert.True(result.HasExternalCommands, "CustomDestination pointing to External destination must be flagged.");
            Assert.Contains(result.ExternalCommands, c => c.Contains("External Tool"));
        }

        [Fact]
        public void Validate_LatentParameterOnBorderNode_FlagsExternalCommands()
        {
            var recipe = new CaptureRecipe("latent_recipe", "Latent Parameter Test")
                .AddNode(new RecipeNodeConfig { Id = "start", StepType = "Source" })
                .AddNode(new RecipeNodeConfig
                {
                    Id = "border_step",
                    StepType = "Border",
                    Parameters = new Dictionary<string, object>
                    {
                        { "Width", 2 },
                        { "Path", @"C:\Windows\System32\calc.exe" }
                    }
                });

            recipe.Flow = new RecipeFlowConfig("start").AddTransition("start", "border_step");

            var result = RecipeValidator.Validate(recipe);
            Assert.True(result.HasExternalCommands, "Border step carrying executable Path parameter must be flagged.");
            Assert.Contains(result.ExternalCommands, c => c.Contains("calc.exe"));
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
    }
}