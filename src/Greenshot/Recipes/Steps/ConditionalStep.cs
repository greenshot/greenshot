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

using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Pipeline;

namespace Greenshot.Recipes.Steps
{
    /// <summary>
    /// A branch point: the engine evaluates the node's Branches after it ran and follows the conditional transitions
    /// of the first branch whose expression is true. The step itself does nothing; it exists so a Conditional node
    /// is a registered step type with a contract, like every other node. The branch expressions are evaluated by the
    /// engine when choosing the branch, so they are not evaluated as parameters (IEvaluatesOwnParameters).
    /// </summary>
    [StepInfo(WellKnownStepTypes.Conditional, "Conditional", "Continues with the conditional transitions of the first branch whose expression is true.", "Logic")]
    [StepParameter("Branches", ContractDataType.Object, Required = true, SupportsExpressions = false,
        Description = "List of { Key, Expression }; the first branch whose expression is true is taken, an Expression of else/default always matches")]
    public class ConditionalStep : ICaptureStep, IEvaluatesOwnParameters
    {
        public string Name { get; }

        public ConditionalStep(RecipeNodeConfig config)
        {
            Name = config?.Name ?? config?.Id ?? WellKnownStepTypes.Conditional;
        }

        public Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
