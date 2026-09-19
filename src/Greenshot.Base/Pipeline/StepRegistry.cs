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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Greenshot.Base.Core;
using Greenshot.Base.Recipes;
using log4net;

namespace Greenshot.Base.Pipeline
{
    /// <summary>
    /// Thread-safe registry mapping step types to step factory delegates.
    /// </summary>
    public class StepRegistry : IStepRegistry
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(StepRegistry));
        private readonly ConcurrentDictionary<string, Func<RecipeNodeConfig, ICaptureStep>> _factories =
            new ConcurrentDictionary<string, Func<RecipeNodeConfig, ICaptureStep>>(StringComparer.OrdinalIgnoreCase);

        private static StepRegistry _instance;
        public static StepRegistry Instance => _instance ??= new StepRegistry();

        public void RegisterStepFactory(string stepType, Func<RecipeNodeConfig, ICaptureStep> factory)
        {
            if (string.IsNullOrEmpty(stepType)) throw new ArgumentNullException(nameof(stepType));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            _factories[stepType] = factory;
            Log.DebugFormat("Registered step factory for step type '{0}'", stepType);
        }

        public bool IsRegistered(string stepType)
        {
            if (string.IsNullOrEmpty(stepType)) return false;

            if (_factories.ContainsKey(stepType)) return true;

            // Attempt dynamic discovery from registered IRecipeStepProvider services
            DiscoverProviders();

            return _factories.ContainsKey(stepType);
        }

        public IReadOnlyCollection<string> RegisteredStepTypes
        {
            get
            {
                DiscoverProviders();
                return new ReadOnlyCollection<string>(_factories.Keys.ToList());
            }
        }

        public void RegisterProvider(IRecipeStepProvider provider)
        {
            if (provider == null) return;
            if (!RecipeConfigHelper.IsRecipeFeatureEnabled())
            {
                Log.DebugFormat("Recipe feature/editor is not enabled; skipping step registration for provider '{0}'", provider.GetType().Name);
                return;
            }

            try
            {
                provider.RegisterSteps(this);
            }
            catch (Exception ex)
            {
                Log.Error($"Error registering steps from provider {provider.GetType().Name}", ex);
            }
        }

        public void RegisterProviders(IEnumerable<IRecipeStepProvider> providers)
        {
            if (providers == null) return;
            foreach (var provider in providers)
            {
                RegisterProvider(provider);
            }
        }

        private void DiscoverProviders()
        {
            if (!RecipeConfigHelper.IsRecipeFeatureEnabled()) return;

            try
            {
                var providers = SimpleServiceProvider.Current?.GetAllInstances<IRecipeStepProvider>();
                if (providers != null)
                {
                    foreach (var provider in providers)
                    {
                        RegisterProvider(provider);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug("Provider discovery encountered an error (can occur before DI is initialized)", ex);
            }
        }

        public ICaptureStep CreateStep(RecipeNodeConfig config)
        {
            if (config == null || string.IsNullOrEmpty(config.StepType)) return null;

            if (_factories.TryGetValue(config.StepType, out var factory))
            {
                return factory(config);
            }

            // Try dynamic discovery of newly loaded providers
            DiscoverProviders();

            if (_factories.TryGetValue(config.StepType, out factory))
            {
                return factory(config);
            }

            Log.WarnFormat("No step factory registered for step type '{0}'", config.StepType);
            return null;
        }
    }
}
