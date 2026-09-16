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
using Greenshot.Base.Interfaces;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Greenshot.Pipeline.Steps;
using Greenshot.Triggers;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    public class TriggerIntegrationTests
    {
        public TriggerIntegrationTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void EditorTrigger_CreationAndConfiguration_SetsPropertiesCorrectly()
        {
            var config = TriggerConfig.CreateEditor("My Editor Recipe", "Templates");

            Assert.Equal(TriggerConfig.TypeEditor, config.TriggerType);
            Assert.Equal("My Editor Recipe", config.GetParameter<string>("MenuItemText"));
            Assert.Equal("Templates", config.GetParameter<string>("Group"));

            var trigger = new EditorTrigger("recipe_123", config);
            Assert.Equal("My Editor Recipe", trigger.Name);
            Assert.Equal("recipe_123", trigger.TargetRecipeId);
            Assert.Equal(TriggerConfig.TypeEditor, trigger.TriggerType);
            Assert.Equal("Templates", trigger.Group);
            Assert.Equal("My Editor Recipe", trigger.MenuItemText);
        }

        [Fact]
        public void ClipboardTrigger_CreationAndConfiguration_SetsFormatFilter()
        {
            var config = TriggerConfig.CreateClipboard(true, "PNG,DeviceIndependentBitmap");

            Assert.Equal(TriggerConfig.TypeClipboard, config.TriggerType);
            Assert.Equal("PNG,DeviceIndependentBitmap", config.GetParameter<string>("FormatFilter"));

            var trigger = new ClipboardTrigger("recipe_456", config);
            Assert.Equal("recipe_456", trigger.TargetRecipeId);
            Assert.Equal(TriggerConfig.TypeClipboard, trigger.TriggerType);
            Assert.Equal("PNG,DeviceIndependentBitmap", trigger.FormatFilter);
        }

        [Fact]
        public void CaptureRecipe_HasDestinationStep_And_HasEditorDestination_DetectsCorrectly()
        {
            var recipeWithoutEditor = new CaptureRecipe("no_editor", "No Editor")
                .AddNode(new RecipeNodeConfig { Id = "s1", StepType = "Annotation" });

            Assert.False(recipeWithoutEditor.HasDestinationStep());
            Assert.False(recipeWithoutEditor.HasEditorDestination());

            var recipeWithFile = new CaptureRecipe("file_only", "File Only")
                .AddNode(new RecipeNodeConfig { Id = "s1", StepType = "SaveFile" });

            Assert.True(recipeWithFile.HasDestinationStep());
            Assert.False(recipeWithFile.HasEditorDestination());

            var recipeWithEditorStep = new CaptureRecipe("editor_step", "Editor Step")
                .AddNode(new RecipeNodeConfig { Id = "s1", StepType = "Editor" });

            Assert.True(recipeWithEditorStep.HasDestinationStep());
            Assert.True(recipeWithEditorStep.HasEditorDestination());

            var recipeWithDestinationsEditor = new CaptureRecipe("dest_editor", "Destinations Editor")
                .AddNode(new RecipeNodeConfig
                {
                    Id = "s1",
                    StepType = "Destinations",
                    Parameters = new Dictionary<string, object>
                    {
                        ["DestinationDesignations"] = new List<string> { "Editor" }
                    }
                });

            Assert.True(recipeWithDestinationsEditor.HasDestinationStep());
            Assert.True(recipeWithDestinationsEditor.HasEditorDestination());
        }

        [Fact]
        public async Task SourceAcquisitionStep_WhenPayloadAlreadyProvided_SkipsAcquisition()
        {
            using var bmp = new Bitmap(100, 100);
            var capture = new Capture((Image)bmp.Clone());
            var payload = new CapturePayload(capture);
            var surface = payload.EnsureSurface();

            var recipe = new CaptureRecipe("pre_supplied", "Pre-Supplied Payload");
            using var context = new CaptureFlowContext(recipe)
            {
                Payload = payload
            };

            var nodeConfig = new RecipeNodeConfig { Id = "src", StepType = "Source" };
            var step = new SourceAcquisitionStep(nodeConfig);

            await step.ExecuteAsync(context);

            // Context payload and surface remain intact and were not replaced by screen capture
            Assert.NotNull(context.Payload);
            Assert.Same(payload, context.Payload);
            Assert.Same(surface, context.Payload.Surface);
        }
    }
}
