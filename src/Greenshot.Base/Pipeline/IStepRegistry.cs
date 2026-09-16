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
using Greenshot.Base.Recipes;

namespace Greenshot.Base.Pipeline
{
    /// <summary>
    /// Registry responsible for creating executable ICaptureStep instances from RecipeNodeConfig definitions.
    /// </summary>
    public interface IStepRegistry
    {
        /// <summary>
        /// Registers a factory for a given step type identifier.
        /// </summary>
        void RegisterStepFactory(string stepType, Func<RecipeNodeConfig, ICaptureStep> factory);

        /// <summary>
        /// Checks whether a factory for the specified step type has been registered.
        /// </summary>
        /// <param name="stepType">The step type identifier to check.</param>
        /// <returns>True if a factory exists for the step type; otherwise false.</returns>
        bool IsRegistered(string stepType);

        /// <summary>
        /// Gets a collection of all currently registered step type identifiers.
        /// </summary>
        IReadOnlyCollection<string> RegisteredStepTypes { get; }

        /// <summary>
        /// Registers all step factories provided by the specified step provider.
        /// </summary>
        /// <param name="provider">The step provider.</param>
        void RegisterProvider(IRecipeStepProvider provider);

        /// <summary>
        /// Registers all step factories provided by the specified step providers.
        /// </summary>
        /// <param name="providers">The collection of step providers.</param>
        void RegisterProviders(IEnumerable<IRecipeStepProvider> providers);

        /// <summary>
        /// Instantiates an executable step from a node configuration.
        /// </summary>
        ICaptureStep CreateStep(RecipeNodeConfig config);
    }
}
