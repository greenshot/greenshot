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
using Dapplo.Ini.Interfaces;
using Greenshot.Base.Recipes.Pipeline;

namespace Greenshot.Base.Interfaces.Plugin
{
    /// <summary>
    /// What a plugin can register in <see cref="IGreenshotPlugin.ConfigureServices"/>. Registration only: nothing is started.
    /// </summary>
    public interface IPluginServices
    {
        /// <summary>
        /// Register an INI section, it's filled from greenshot.ini (defaults, user and constants files) right away,
        /// so its values can be used directly after this call. The section of a plugin which isn't loaded stays in the file.
        /// </summary>
        void AddConfiguration<TSection>(TSection section) where TSection : class, IIniSection;

        /// <summary>
        /// Register a service.
        /// </summary>
        void AddService<TService>(TService service);

        /// <summary>
        /// Register services which need the loaded configuration, the factory is called after the configuration was loaded.
        /// </summary>
        void AddServices<TService>(Func<IEnumerable<TService>> factory);

        /// <summary>
        /// Register a provider of recipe steps, the host registers it when the recipe feature is enabled.
        /// </summary>
        void AddRecipeStepProvider(IRecipeStepProvider provider);

        /// <summary>
        /// Register a provider of recipe drawables, the host registers it when the recipe feature is enabled.
        /// </summary>
        void AddRecipeDrawableProvider(Drawing.IRecipeDrawableProvider provider);

        /// <summary>
        /// Register the view for the settings view model of the plugin (see <see cref="IConfigurablePlugin"/>).
        /// </summary>
        /// <typeparam name="TViewModel">type of the view model</typeparam>
        /// <param name="createView">creates the view (on the UI thread)</param>
        void AddSettingsView<TViewModel>(Func<TViewModel, object> createView);
    }
}
