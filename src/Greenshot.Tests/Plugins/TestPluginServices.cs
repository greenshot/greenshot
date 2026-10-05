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
using System.Linq;
using Dapplo.Ini.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Recipes.Pipeline;

namespace Greenshot.Tests.Plugins
{
    /// <summary>
    /// Collects what a plugin registers, for the tests.
    /// </summary>
    public sealed class TestPluginServices : IPluginServices
    {
        private readonly List<object> _services = new();
        private readonly Dictionary<Type, Func<object, object>> _settingsViews = new();

        public List<IIniSection> Sections { get; } = new();

        public List<IRecipeStepProvider> RecipeStepProviders { get; } = new();

        public List<IRecipeDrawableProvider> RecipeDrawableProviders { get; } = new();

        public void AddConfiguration<TSection>(TSection section) where TSection : class, IIniSection => Sections.Add(section);

        public void AddService<TService>(TService service) => _services.Add(service);

        public void AddServices<TService>(Func<IEnumerable<TService>> factory) => _services.AddRange(factory().Cast<object>());

        public void AddRecipeStepProvider(IRecipeStepProvider provider) => RecipeStepProviders.Add(provider);

        public void AddRecipeDrawableProvider(IRecipeDrawableProvider provider) => RecipeDrawableProviders.Add(provider);

        public void AddSettingsView<TViewModel>(Func<TViewModel, object> createView) => _settingsViews[typeof(TViewModel)] = viewModel => createView((TViewModel)viewModel);

        public IEnumerable<TService> GetServices<TService>() => _services.OfType<TService>();

        /// <summary>
        /// The view the plugin registered for the view model
        /// </summary>
        public object CreateSettingsView(object viewModel) =>
            _settingsViews.Where(settingsView => settingsView.Key.IsInstanceOfType(viewModel)).Select(settingsView => settingsView.Value(viewModel)).FirstOrDefault();
    }
}
