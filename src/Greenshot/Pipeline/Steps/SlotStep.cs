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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Pipeline.Contracts;
using Greenshot.Base.Recipes;

namespace Greenshot.Pipeline.Steps
{
    /// <summary>
    /// A named place where recipe extensions put their steps. The recipe Greenshot runs has the slot replaced by the
    /// extensions (see <see cref="RecipeComposer"/>); a slot no extension uses does nothing.
    /// </summary>
    [StepInfo(WellKnownStepTypes.Slot, "Slot", "A place where extensions (border, drop shadow, caption, ...) add their steps; does nothing itself.", "Flow")]
    [StepParameter("Name", ContractDataType.Enum, Required = true, Description = "Which slot", AllowedValues = new[] { RecipeSlots.AfterCapture, RecipeSlots.BeforeExport, RecipeSlots.AfterExport, RecipeSlots.BeforeDestination }, SupportsExpressions = false)]
    [StepParameter("Accept", ContractDataType.Object, DefaultValue = RecipeSlots.AcceptAll, Description = "Which extensions: \"all\", \"none\" or a list of extension ids", SupportsExpressions = false)]
    public class SlotStep : ICaptureStep
    {
        public string Name { get; }

        public SlotStep(RecipeNodeConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            Name = config.Name ?? config.Id ?? WellKnownStepTypes.Slot;
        }

        public Task ExecuteAsync(CaptureFlowContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
