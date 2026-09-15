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
using Greenshot.Base.Pipeline;

namespace Greenshot.Base.Interfaces.Drawing
{
    /// <summary>
    /// Registry mapping drawable type identifiers to factory delegates that instantiate IDrawableContainer instances
    /// for recipe pipeline execution.
    /// </summary>
    public interface IRecipeDrawableRegistry
    {
        /// <summary>
        /// Registers a factory delegate for a specific drawable type name.
        /// </summary>
        /// <param name="drawableType">Type name (case-insensitive), e.g. "Rectangle", "QRCode", "Emoji".</param>
        /// <param name="factory">Factory delegate accepting the surface, resolved parameters dictionary, and flow context.</param>
        void RegisterDrawableFactory(string drawableType, Func<ISurface, Dictionary<string, object>, CaptureFlowContext, IDrawableContainer> factory);

        /// <summary>
        /// Registers a factory delegate for a specific drawable type name with explicit scale options.
        /// </summary>
        /// <param name="drawableType">Type name (case-insensitive), e.g. "Rectangle", "QRCode", "Emoji".</param>
        /// <param name="factory">Factory delegate accepting the surface, resolved parameters dictionary, and flow context.</param>
        /// <param name="scaleOptions">Scale options defining behavior such as rational aspect ratio locking.</param>
        void RegisterDrawableFactory(string drawableType, Func<ISurface, Dictionary<string, object>, CaptureFlowContext, IDrawableContainer> factory, ScaleOptions scaleOptions);

        /// <summary>
        /// Gets the scale options for a registered drawable type, indicating whether it requires aspect ratio locking.
        /// </summary>
        /// <param name="drawableType">Type name (case-insensitive).</param>
        /// <returns>ScaleOptions for the drawable, or ScaleOptions.Default if unconstrained or unknown.</returns>
        ScaleOptions GetScaleOptions(string drawableType);

        /// <summary>
        /// Checks whether a factory is registered for the specified drawable type name.
        /// </summary>
        /// <param name="drawableType">Type name (case-insensitive).</param>
        bool IsRegistered(string drawableType);

        /// <summary>
        /// Creates a drawable container for the given drawable type using the registered factory.
        /// </summary>
        /// <param name="drawableType">Type name (case-insensitive).</param>
        /// <param name="surface">Target capture surface.</param>
        /// <param name="parameters">Resolved parameters dictionary.</param>
        /// <param name="context">Capture flow context.</param>
        /// <returns>An instantiated IDrawableContainer, or null if type is not registered or cannot be instantiated.</returns>
        IDrawableContainer CreateDrawable(string drawableType, ISurface surface, Dictionary<string, object> parameters, CaptureFlowContext context);

        /// <summary>
        /// Registers all drawable types provided by the given provider.
        /// </summary>
        /// <param name="provider">The provider contributing custom drawables.</param>
        void RegisterProvider(IRecipeDrawableProvider provider);

        /// <summary>
        /// Registers all drawable types from a collection of providers.
        /// </summary>
        /// <param name="providers">The collection of providers.</param>
        void RegisterProviders(IEnumerable<IRecipeDrawableProvider> providers);

        /// <summary>
        /// Registers an interactive configurator/editor delegate for a drawable type.
        /// </summary>
        /// <param name="drawableType">Type name (case-insensitive).</param>
        /// <param name="configurator">Delegate accepting the parameters dictionary and optional parent window, returning true if modified.</param>
        void RegisterDrawableConfigurator(string drawableType, Func<IDictionary<string, object>, object, bool> configurator);

        /// <summary>
        /// Checks whether an interactive configurator is available for the specified drawable type.
        /// </summary>
        /// <param name="drawableType">Type name (case-insensitive).</param>
        bool CanConfigureDrawable(string drawableType);

        /// <summary>
        /// Opens the registered interactive configurator for the specified drawable type.
        /// </summary>
        /// <param name="drawableType">Type name (case-insensitive).</param>
        /// <param name="parameters">The parameters dictionary to read and update.</param>
        /// <param name="owner">Optional owner window.</param>
        /// <returns>True if configuration was edited and confirmed by the user, false otherwise.</returns>
        bool ConfigureDrawable(string drawableType, IDictionary<string, object> parameters, object owner = null);

        /// <summary>
        /// Gets all currently registered drawable type names.
        /// </summary>
        IReadOnlyCollection<string> RegisteredDrawableTypes { get; }
    }

    /// <summary>
    /// Interface implemented by plugins or extensions that contribute custom drawables to the recipe pipeline.
    /// </summary>
    public interface IRecipeDrawableProvider
    {
        /// <summary>
        /// Registers drawable factories into the recipe drawable registry.
        /// </summary>
        /// <param name="registry">The recipe drawable registry to register factories with.</param>
        void RegisterDrawables(IRecipeDrawableRegistry registry);

        /// <summary>
        /// Returns an optional JSON Schema fragment describing the configuration properties
        /// accepted by the custom drawables provided by this extension, or null if none is provided.
        /// </summary>
        string GetDrawableSchemaJson();
    }
}
