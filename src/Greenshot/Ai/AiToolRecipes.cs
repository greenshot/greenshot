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
using Greenshot.Ai;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Triggers;
using Greenshot.Helpers;
using Greenshot.Recipes;

[assembly: GreenshotModule(typeof(AiToolRecipes), 20)]

namespace Greenshot.Ai
{
    /// <summary>
    /// The built-in recipes behind the AI tools of greenshot-mcp.exe
    /// </summary>
    internal sealed class AiToolRecipes : IBuiltInRecipeProvider
    {
        public IEnumerable<CaptureRecipe> CreateRecipes() => RegisterAiToolRecipes();

        /// <summary>
        /// The tools AI tools get (greenshot-mcp.exe): capturing a window, a region or the screen, optionally with OCR.
        /// The image and text go back to the AI tool, there is no destination.
        /// </summary>
        private static IEnumerable<CaptureRecipe> RegisterAiToolRecipes()
        {
            var ocrArgument = new CommandlineArgument
            {
                Name = "ocr",
                Variable = "Ocr",
                Type = ContractDataType.Boolean,
                DefaultValue = "false",
                Description = "true to also return the text in the image (OCR) with the position of each line"
            };

            var windowRecipe = new CaptureRecipe(
                RecipeManager.RecipeIdAiCaptureWindow,
                "AI tool: capture window",
                "Captures a window for an AI tool, with its exact contents (also when it is covered), without activating it")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.Window, captureMouse: false, delayMs: 0).Set("WindowHandle", "${Window}"))
                .AddNode(CreateOcrCondition("ocr_wanted"))
                .AddNode(RecipeStepConfig.CreateProcessors("ocr", new[] { "Windows10OcrProcessor" }))
                .AddTrigger(TriggerConfig.CreateAiTool(
                    "capture_window",
                    "Screenshot of one window, with its exact contents even when other windows cover it. The window is not activated. " +
                    "Use list_windows first and pass the id of the window (e.g. w7).",
                    new[]
                    {
                        new CommandlineArgument
                        {
                            Name = "window",
                            Variable = "Window",
                            Type = ContractDataType.Window,
                            Required = true,
                            Description = "The id of the window from list_windows, e.g. w7"
                        },
                        ocrArgument
                    },
                    title: "Capture window"));
            windowRecipe.Flow = new RecipeFlowConfig("acquire")
                .AddTransition("acquire", "ocr_wanted")
                .AddConditionalTransition("ocr_wanted", "ocr", "ocr");
            yield return windowRecipe;

            var regionRecipe = new CaptureRecipe(
                RecipeManager.RecipeIdAiCaptureRegion,
                "AI tool: capture region",
                "Captures a part of the screen for an AI tool")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.Region, captureMouse: false, delayMs: 0))
                .AddNode(CreateOcrCondition("ocr_wanted"))
                .AddNode(RecipeStepConfig.CreateProcessors("ocr", new[] { "Windows10OcrProcessor" }))
                .AddTrigger(TriggerConfig.CreateAiTool(
                    "capture_region",
                    "Screenshot of a part of the screen, in screen coordinates (list_windows has the bounds of the windows and displays). " +
                    "Use it to see details at full resolution.",
                    new[]
                    {
                        new CommandlineArgument
                        {
                            Name = "region",
                            Variable = "PreSuppliedRegion",
                            Type = ContractDataType.Region,
                            Required = true,
                            Description = "x,y,width,height in screen coordinates, e.g. 0,0,800,600"
                        },
                        ocrArgument
                    },
                    title: "Capture region"));
            regionRecipe.Flow = new RecipeFlowConfig("acquire")
                .AddTransition("acquire", "ocr_wanted")
                .AddConditionalTransition("ocr_wanted", "ocr", "ocr");
            yield return regionRecipe;

            var screenRecipe = new CaptureRecipe(
                RecipeManager.RecipeIdAiCaptureScreen,
                "AI tool: capture screen",
                "Captures all displays for an AI tool")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.FullScreen, captureMouse: false, delayMs: 0, screenMode: ScreenCaptureMode.FullScreen))
                .AddNode(CreateOcrCondition("ocr_wanted"))
                .AddNode(RecipeStepConfig.CreateProcessors("ocr", new[] { "Windows10OcrProcessor" }))
                .AddTrigger(TriggerConfig.CreateAiTool(
                    "capture_screen",
                    "Screenshot of all displays. For one display or a part of the screen use capture_region with the bounds from list_windows.",
                    new[] { ocrArgument },
                    title: "Capture screen"));
            screenRecipe.Flow = new RecipeFlowConfig("acquire")
                .AddTransition("acquire", "ocr_wanted")
                .AddConditionalTransition("ocr_wanted", "ocr", "ocr");
            yield return screenRecipe;
        }

        /// <summary>
        /// Continues with the node "ocr" when the Ocr argument is true, otherwise the flow ends
        /// </summary>
        private static RecipeNodeConfig CreateOcrCondition(string id)
        {
            return RecipeStepConfig.CreateConditional(id, new[] { new KeyValuePair<string, string>("ocr", "${Ocr}") }).WithName("OCR wanted?");
        }
    }
}
