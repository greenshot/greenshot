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
using System.Threading.Tasks;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Recipes.Pipeline;

namespace Greenshot.Tests.Recipes
{
    /// <summary>
    /// Runs a single node through the <see cref="DagExecutionEngine"/>, exactly like a recipe would,
    /// so steps get their parameters resolved (or not, for <see cref="IEvaluatesOwnParameters"/>) by the engine.
    /// </summary>
    internal static class SingleNodeRunner
    {
        public static Task RunAsync(RecipeNodeConfig node, Func<RecipeNodeConfig, ICaptureStep> stepFactory, CaptureFlowContext context)
        {
            var recipe = new CaptureRecipe(context.Recipe?.Id ?? "single_node_recipe", "Single Node").AddNode(node);
            recipe.Flow = new RecipeFlowConfig(node.Id);
            return new DagExecutionEngine(stepFactory).ExecuteAsync(recipe, context);
        }
    }
}
