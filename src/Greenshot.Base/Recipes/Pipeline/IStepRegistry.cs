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
using Greenshot.Base.Recipes.Contracts;

namespace Greenshot.Base.Recipes.Pipeline
{
    /// <summary>
    /// The registry of step types: for each step type its factory and its contract, registered together.
    /// </summary>
    /// <remarks>
    /// The step type of the contract is the key recipes use ("stepType"); every step type has exactly one name.
    /// Registering the same step type again replaces its factory and contract.
    /// </remarks>
    public interface IStepRegistry
    {
        /// <summary>
        /// Registers a step type: the factory creating its steps and the contract describing them.
        /// </summary>
        void Register(StepContract contract, Func<RecipeNodeConfig, ICaptureStep> factory);

        /// <summary>
        /// True when the step type is registered.
        /// </summary>
        bool IsRegistered(string stepType);

        /// <summary>
        /// The registered step types.
        /// </summary>
        IReadOnlyCollection<string> RegisteredStepTypes { get; }

        /// <summary>
        /// The contracts of all registered step types.
        /// </summary>
        IReadOnlyCollection<StepContract> Contracts { get; }

        /// <summary>
        /// The contract of a step type, null when it is not registered.
        /// </summary>
        StepContract GetContract(string stepType);

        /// <summary>
        /// Registers all step types provided by the specified step provider.
        /// </summary>
        void RegisterProvider(IRecipeStepProvider provider);

        /// <summary>
        /// Registers all step types provided by the specified step providers.
        /// </summary>
        void RegisterProviders(IEnumerable<IRecipeStepProvider> providers);

        /// <summary>
        /// Instantiates an executable step from a node configuration.
        /// </summary>
        ICaptureStep CreateStep(RecipeNodeConfig config);
    }

    /// <summary>
    /// Registration helpers that build the contract from the step class' attributes.
    /// </summary>
    public static class StepRegistryExtensions
    {
        /// <summary>
        /// Registers <typeparamref name="TStep"/> under the step type of its [StepInfo] attribute.
        /// </summary>
        public static void Register<TStep>(this IStepRegistry registry, Func<RecipeNodeConfig, ICaptureStep> factory)
            where TStep : ICaptureStep
        {
            registry.Register(StepContractBuilder.FromType(typeof(TStep)), factory);
        }

        /// <summary>
        /// Registers <typeparamref name="TStep"/> under another step type than its [StepInfo] attribute,
        /// for a class that implements several step types (e.g. Border and Effect).
        /// </summary>
        public static void Register<TStep>(this IStepRegistry registry, string stepType, string displayName, string description,
            Func<RecipeNodeConfig, ICaptureStep> factory)
            where TStep : ICaptureStep
        {
            registry.Register(StepContractBuilder.FromType(typeof(TStep), stepType, displayName, description), factory);
        }
    }
}
