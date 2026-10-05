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
using Greenshot.Base.Recipes.Triggers;
using Greenshot.Helpers;
using Greenshot.Ipc.BrowserExtension;
using Greenshot.Recipes;

[assembly: GreenshotModule(typeof(BrowserExtensionRecipes), 10)]

namespace Greenshot.Ipc.BrowserExtension
{
    /// <summary>
    /// The built-in recipe for captures from the browser extension
    /// </summary>
    internal sealed class BrowserExtensionRecipes : IBuiltInRecipeProvider
    {
        public IEnumerable<CaptureRecipe> CreateRecipes()
        {
            var extensionRecipe = new CaptureRecipe(
                RecipeManager.RecipeIdExtension,
                "Capture from browser extension",
                "Process screenshots received from the browser extension and choose destination interactively")
                .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.Extension, captureMouse: false))
                .AddNode(RecipeStepConfig.CreateDynamicDestination("export", "Export Browser Capture"))
                .AddTrigger(TriggerConfig.CreateExtension(name: "Default Browser Extension Trigger"));
            extensionRecipe.Flow = new RecipeFlowConfig("acquire")
                .AddTransition("acquire", "export");
            RecipeStepConfig.AddStandardSlots(extensionRecipe, "export", "export");
            yield return extensionRecipe;
        }
    }
}
