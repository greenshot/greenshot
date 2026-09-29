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
using Greenshot.Base.Core;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;

namespace Greenshot.Plugin.RecipeEditor.ViewModels
{
    /// <summary>
    /// Test runs of recipes go through the flow runner like every other flow (tracked, cancellable, on the pool).
    /// </summary>
    internal static class TestRun
    {
        /// <summary>
        /// Start the test run and wait for it without blocking, the continuation runs on the calling (UI) thread.
        /// </summary>
        /// <returns>the result of the flow</returns>
        public static async Task<CaptureFlowResult> RunAsync(CaptureRecipe recipe)
        {
            var runner = SimpleServiceProvider.Current.GetInstance<ICaptureFlowRunner>(isOptional: true)
                         ?? throw new InvalidOperationException("The capture flow runner is not available.");
            var recipeToTest = TriggerRecipePreparer.PrepareForTestRun(recipe);
            var handle = runner.Start(recipeToTest);
            return await handle.Completion;
        }

        /// <summary>
        /// The error of a failed flow, null when it didn't fail
        /// </summary>
        public static string ErrorOf(CaptureFlowResult result) =>
            result?.State == CaptureFlowState.Failed ? result.Error?.Message ?? result.Reason ?? "unknown error" : null;
    }
}
