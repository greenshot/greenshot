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
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Pipeline;
using log4net;

namespace Greenshot.Base.Drawing
{
    /// <summary>
    /// Thread-safe registry mapping drawable type names to factory delegates that create IDrawableContainer instances.
    /// </summary>
    public class RecipeDrawableRegistry : IRecipeDrawableRegistry
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RecipeDrawableRegistry));

        private readonly ConcurrentDictionary<string, Func<ISurface, Dictionary<string, object>, CaptureFlowContext, IDrawableContainer>> _factories =
            new ConcurrentDictionary<string, Func<ISurface, Dictionary<string, object>, CaptureFlowContext, IDrawableContainer>>(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, ScaleOptions> _scaleOptions =
            new ConcurrentDictionary<string, ScaleOptions>(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, Func<IDictionary<string, object>, object, bool>> _configurators =
            new ConcurrentDictionary<string, Func<IDictionary<string, object>, object, bool>>(StringComparer.OrdinalIgnoreCase);

        private static RecipeDrawableRegistry _instance;
        public static RecipeDrawableRegistry Instance => _instance ??= new RecipeDrawableRegistry();

        public void RegisterDrawableFactory(string drawableType, Func<ISurface, Dictionary<string, object>, CaptureFlowContext, IDrawableContainer> factory)
        {
            RegisterDrawableFactory(drawableType, factory, ScaleOptions.Default);
        }

        public void RegisterDrawableFactory(string drawableType, Func<ISurface, Dictionary<string, object>, CaptureFlowContext, IDrawableContainer> factory, ScaleOptions scaleOptions)
        {
            if (string.IsNullOrEmpty(drawableType)) throw new ArgumentNullException(nameof(drawableType));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            _factories[drawableType] = factory;
            _scaleOptions[drawableType] = scaleOptions;
            Log.DebugFormat("Registered recipe drawable factory for '{0}' (ScaleOptions={1})", drawableType, scaleOptions);
        }

        public ScaleOptions GetScaleOptions(string drawableType)
        {
            if (string.IsNullOrEmpty(drawableType)) return ScaleOptions.Default;

            if (_scaleOptions.TryGetValue(drawableType, out var options) && options != ScaleOptions.Default)
            {
                return options;
            }

            DiscoverProviders();

            if (_scaleOptions.TryGetValue(drawableType, out options))
            {
                return options;
            }

            return ScaleOptions.Default;
        }

        public bool IsRegistered(string drawableType)
        {
            if (string.IsNullOrEmpty(drawableType)) return false;

            if (_factories.ContainsKey(drawableType)) return true;

            // Attempt dynamic discovery from registered IRecipeDrawableProvider services
            DiscoverProviders();

            return _factories.ContainsKey(drawableType);
        }

        public IReadOnlyCollection<string> RegisteredDrawableTypes
        {
            get
            {
                DiscoverProviders();
                return new ReadOnlyCollection<string>(_factories.Keys.ToList());
            }
        }

        public void RegisterDrawableConfigurator(string drawableType, Func<IDictionary<string, object>, object, bool> configurator)
        {
            if (string.IsNullOrEmpty(drawableType)) throw new ArgumentNullException(nameof(drawableType));
            if (configurator == null) throw new ArgumentNullException(nameof(configurator));

            _configurators[drawableType] = configurator;
            Log.DebugFormat("Registered recipe drawable configurator for '{0}'", drawableType);
        }

        public bool CanConfigureDrawable(string drawableType)
        {
            if (string.IsNullOrEmpty(drawableType)) return false;
            if (_configurators.ContainsKey(drawableType)) return true;
            DiscoverProviders();
            return _configurators.ContainsKey(drawableType);
        }

        public bool ConfigureDrawable(string drawableType, IDictionary<string, object> parameters, object owner = null)
        {
            if (string.IsNullOrEmpty(drawableType) || parameters == null) return false;

            if (_configurators.TryGetValue(drawableType, out var configurator))
            {
                return configurator(parameters, owner);
            }

            DiscoverProviders();

            if (_configurators.TryGetValue(drawableType, out configurator))
            {
                return configurator(parameters, owner);
            }

            return false;
        }

        public void RegisterProvider(IRecipeDrawableProvider provider)
        {
            if (provider == null) return;
            try
            {
                provider.RegisterDrawables(this);
            }
            catch (Exception ex)
            {
                Log.Error($"Error registering recipe drawables from provider {provider.GetType().Name}", ex);
            }
        }

        public void RegisterProviders(IEnumerable<IRecipeDrawableProvider> providers)
        {
            if (providers == null) return;
            foreach (var provider in providers)
            {
                RegisterProvider(provider);
            }
        }

        private void DiscoverProviders()
        {
            try
            {
                var providers = SimpleServiceProvider.Current?.GetAllInstances<IRecipeDrawableProvider>();
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
                Log.Debug("Recipe drawable provider discovery encountered an error (can occur before DI is initialized)", ex);
            }
        }

        public IDrawableContainer CreateDrawable(string drawableType, ISurface surface, Dictionary<string, object> parameters, CaptureFlowContext context)
        {
            if (string.IsNullOrEmpty(drawableType) || surface == null) return null;

            if (_factories.TryGetValue(drawableType, out var factory))
            {
                return factory(surface, parameters, context);
            }

            // Try dynamic discovery of newly loaded providers
            DiscoverProviders();

            if (_factories.TryGetValue(drawableType, out factory))
            {
                return factory(surface, parameters, context);
            }

            return null;
        }
    }
}
