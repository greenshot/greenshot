/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Greenshot.Linux.Recipes.Contracts;

namespace Greenshot.Linux.Recipes.Pipeline
{
    /// <summary>
    /// Thread-safe registry of step types: factory and contract per step type.
    /// </summary>
    public class StepRegistry : IStepRegistry
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(StepRegistry));

        private sealed class Registration
        {
            public StepContract Contract;
            public Func<RecipeNodeConfig, ICaptureStep> Factory;
            // The provider which registered the step type, null when it wasn't registered by a provider
            public IRecipeStepProvider Provider;
        }

        // The providers which registered their step types: the discovery only registers new ones
        private readonly ConcurrentDictionary<IRecipeStepProvider, bool> _registeredProviders = new ConcurrentDictionary<IRecipeStepProvider, bool>();

        // The provider which is registering its step types on this thread
        [ThreadStatic]
        private static IRecipeStepProvider _registeringProvider;

        private readonly ConcurrentDictionary<string, Registration> _registrations = new ConcurrentDictionary<string, Registration>(StringComparer.OrdinalIgnoreCase);

        // Thread-safe: the first access can come from the UI thread and an IPC or pipeline thread at the same time,
        // and a second instance would silently lose what was registered in the first one.
        private static readonly Lazy<StepRegistry> LazyInstance = new Lazy<StepRegistry>(() => new StepRegistry(), LazyThreadSafetyMode.ExecutionAndPublication);
        public static StepRegistry Instance => LazyInstance.Value;

        public void Register(StepContract contract, Func<RecipeNodeConfig, ICaptureStep> factory)
        {
            if (contract == null) throw new ArgumentNullException(nameof(contract));
            if (string.IsNullOrWhiteSpace(contract.StepType)) throw new ArgumentException("The contract has no step type.", nameof(contract));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            foreach (var parameter in contract.Parameters.Where(p => p.ValueType != null))
            {
                if (!RecipeParameterTypes.IsKnown(parameter.ValueType))
                {
                    throw new InvalidOperationException($"Step type '{contract.StepType}': the type {parameter.ValueType} of the parameter '{parameter.Name}' is in no registered JSON context, " +
                                                        $"add it with [JsonSerializable] to the context given to {nameof(RegisterJsonTypes)}.");
                }
            }

            _registrations[contract.StepType] = new Registration { Contract = contract, Factory = factory, Provider = _registeringProvider };
            Log.DebugFormat("Registered step type '{0}'", contract.StepType);
        }

        public void RegisterJsonTypes(IJsonTypeInfoResolver resolver) => RecipeParameterTypes.AddJsonTypes(resolver);

        private Registration Find(string stepType, bool discover)
        {
            if (string.IsNullOrWhiteSpace(stepType)) return null;
            if (_registrations.TryGetValue(stepType, out var registration))
            {
                return registration;
            }
            if (!discover) return null;

            // Attempt dynamic discovery from registered IRecipeStepProvider services
            DiscoverProviders();
            return Find(stepType, false);
        }

        public StepContract GetContract(string stepType) => Find(stepType, true)?.Contract;

        public IReadOnlyCollection<StepContract> Contracts
        {
            get
            {
                DiscoverProviders();
                return _registrations.Values.Select(r => r.Contract).ToList().AsReadOnly();
            }
        }

        public bool IsRegistered(string stepType) => Find(stepType, true) != null;

        /// <summary>
        /// Determines whether a step exports the capture. Core step types are identified centrally;
        /// plugin-provided export steps are identified by their registered Export category.
        /// </summary>
        public bool IsDestinationStep(string stepType)
        {
            if (WellKnownStepTypes.IsDestination(stepType)) return true;
            return string.Equals(GetContract(stepType)?.Category, "Export", StringComparison.OrdinalIgnoreCase);
        }

        public IReadOnlyCollection<string> RegisteredStepTypes
        {
            get
            {
                DiscoverProviders();
                return new ReadOnlyCollection<string>(_registrations.Keys.ToList());
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
            if (!_registeredProviders.TryAdd(provider, true))
            {
                // Already registered: a lookup of an unknown step type doesn't run every provider again
                return;
            }

            var previousProvider = _registeringProvider;
            _registeringProvider = provider;
            try
            {
                provider.RegisterSteps(this);
            }
            catch (Exception ex)
            {
                Log.Error($"Error registering steps from provider {provider.GetType().Name}", ex);
            }
            finally
            {
                _registeringProvider = previousProvider;
            }
        }

        /// <summary>
        /// Removes the step types registered by the provider, recipes using them then show and handle them as unknown.
        /// The provider has to be removed from the services first, otherwise the discovery registers it again.
        /// </summary>
        public void UnregisterProvider(IRecipeStepProvider provider)
        {
            if (provider == null) return;
            _registeredProviders.TryRemove(provider, out _);
            foreach (var registration in _registrations.Where(registration => ReferenceEquals(registration.Value.Provider, provider)).ToList())
            {
                // Only removes the step type when it wasn't registered again in the meantime
                ((ICollection<KeyValuePair<string, Registration>>)_registrations).Remove(registration);
                Log.DebugFormat("Removed step type '{0}'", registration.Key);
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
                var providers = RecipeServices.StepProviders;
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

            var registration = Find(config.StepType, true);
            if (registration != null)
            {
                return registration.Factory(config);
            }

            Log.WarnFormat("No step factory registered for step type '{0}'", config.StepType);
            return null;
        }
    }
}
