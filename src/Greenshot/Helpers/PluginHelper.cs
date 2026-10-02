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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Ini;
using Dapplo.Ini.Interfaces;
using Greenshot.Base.Core;
using Greenshot.Base.Drawing;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using log4net;
using Greenshot.Base.Threading;

namespace Greenshot.Helpers
{
    /// <summary>
    /// The PluginHelper takes care of all plugin related functionality: loading, registration, the parallel start (with
    /// a timeout, the main window doesn't wait for it), the settings views and stopping the plugins.
    /// </summary>
    public class PluginHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PluginHelper));
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();
        private static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(30);

        private static readonly string ApplicationPath = Path.GetDirectoryName(Application.ExecutablePath);
        private static readonly string PafPath = Path.Combine(Application.StartupPath, @"App\Greenshot");

        private readonly Dictionary<Type, Func<object, object>> _settingsViews = new();
        private readonly List<IGreenshotPlugin> _startedPlugins = new();

        public static PluginHelper Instance { get; } = new PluginHelper();

        /// <summary>
        /// The started plugins
        /// </summary>
        public IReadOnlyList<IGreenshotPlugin> Plugins
        {
            get
            {
                lock (_startedPlugins)
                {
                    return _startedPlugins.ToList();
                }
            }
        }

        /// <summary>
        /// The view for the settings of the plugin, null when it has none
        /// </summary>
        public object CreateSettingsView(IGreenshotPlugin plugin)
        {
            if (plugin is not IConfigurablePlugin configurablePlugin)
            {
                return null;
            }

            var viewModel = configurablePlugin.CreateSettingsViewModel(SimpleServiceProvider.Current);
            if (viewModel == null)
            {
                return null;
            }

            foreach (var settingsView in _settingsViews)
            {
                if (settingsView.Key.IsInstanceOfType(viewModel))
                {
                    return settingsView.Value(viewModel);
                }
            }

            Log.WarnFormat("No settings view registered for {0} of plugin {1}", viewModel.GetType(), plugin.Name);
            return null;
        }

        /// <summary>
        /// Stop all plugins, each with a timeout, and dispose them.
        /// </summary>
        public async Task ShutdownAsync(TimeSpan timeout)
        {
            var plugins = Plugins;
            lock (_startedPlugins)
            {
                _startedPlugins.Clear();
            }

            foreach (var plugin in plugins)
            {
                using var timeoutSource = new CancellationTokenSource(timeout);
                try
                {
                    // A plugin which ignores the cancellation doesn't hold up the exit
                    await plugin.StopAsync(timeoutSource.Token).WaitAsync(timeout);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error stopping plugin {plugin.Name}", ex);
                }

                try
                {
                    await plugin.DisposeAsync().AsTask().WaitAsync(timeout);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error disposing plugin {plugin.Name}", ex);
                }
            }
        }

        private IEnumerable<string> FindPluginsOnPath(string path)
        {
            var pluginFiles = Enumerable.Empty<string>();
            if (!Directory.Exists(path)) return pluginFiles;
            try
            {
                pluginFiles = Directory.GetFiles(path, "Greenshot.Plugin.*.dll", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                Log.Error("Error loading plugin: ", ex);
            }

            return pluginFiles;
        }

        /// <summary>
        /// Load the plugins using the three-phase initialisation pattern:
        /// <list type="number">
        ///   <item><description>Phase 1 — every plugin registers its configuration sections (no file I/O).</description></item>
        ///   <item><description>The INI file is read once, populating all sections (core + plugin).</description></item>
        ///   <item><description>Phase 2 — every plugin registers its services into the DI container.</description></item>
        ///   <item><description>Phase 3 — every plugin runs its remaining start-up logic.</description></item>
        /// </list>
        /// </summary>
        /// <summary>
        /// Load the plugins and let them register (synchronous, their configuration sections are filled from greenshot.ini when added), then start them
        /// in parallel without waiting: the returned task completes when all started (or failed / timed out), it never throws.
        /// </summary>
        /// <returns>Task which completes when all plugins started</returns>
        public Task LoadPluginsAsync()
        {
            var pluginFiles = new List<string>();

            if (GreenshotEnvironment.IsPortable)
            {
                pluginFiles.AddRange(FindPluginsOnPath(PafPath));
            }
            else
            {
                pluginFiles.AddRange(FindPluginsOnPath(ApplicationPath));
            }

            // Instantiate all plugins first, greenshot.ini is already loaded so the include / exclude settings apply
            var plugins = new List<IGreenshotPlugin>();
            foreach (string pluginFile in pluginFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(pluginFile);

                    if (IsPluginExcludedByConfig(assembly, pluginFile))
                    {
                        continue;
                    }

                    var assemblyName = assembly.GetName().Name;
                    var pluginEntryName = $"{assemblyName}.{assemblyName.Replace("Greenshot.Plugin.", string.Empty)}Plugin";
                    var pluginEntryType = assembly.GetType(pluginEntryName, false, true);

                    if (pluginEntryType == null)
                    {
                        Log.ErrorFormat("Can't find plugin type {0} in \"{1}\"", pluginEntryName, pluginFile);
                        continue;
                    }

                    var plugin = (IGreenshotPlugin)Activator.CreateInstance(pluginEntryType);
                    if (plugin != null)
                    {
                        plugins.Add(plugin);
                    }
                    else
                    {
                        Log.ErrorFormat("Can't create an instance of {0} from \"{1}\"", pluginEntryName, pluginFile);
                    }
                }
                catch (Exception e)
                {
                    Log.ErrorFormat("Can't load Plugin: {0}", pluginFile);
                    Log.Error(e);
                }
            }

            // ── Registration: configuration sections (filled from the already read greenshot.ini), services, views ───
            var activeIniConfig = IniConfigRegistry.Get();
            var registrations = new List<(IGreenshotPlugin Plugin, PluginServices Services)>();
            foreach (var plugin in plugins)
            {
                var pluginServices = new PluginServices(activeIniConfig, _settingsViews);
                try
                {
                    plugin.ConfigureServices(pluginServices);
                    registrations.Add((plugin, pluginServices));
                }
                catch (Exception e)
                {
                    Log.ErrorFormat("Error during ConfigureServices for plugin {0}", plugin.Name);
                    Log.Error(e);
                }
            }

            // ── What needs the configuration ─────────────────────────────────────
            bool recipesEnabled = RecipeConfigHelper.IsRecipeFeatureEnabled();
            var toStart = new List<IGreenshotPlugin>();
            foreach (var (plugin, pluginServices) in registrations)
            {
                try
                {
                    pluginServices.RegisterDeferred(recipesEnabled);
                    toStart.Add(plugin);
                }
                catch (Exception e)
                {
                    Log.ErrorFormat("Error registering the services of plugin {0}", plugin.Name);
                    Log.Error(e);
                }
            }

            // ── Start all in parallel, nobody waits for it ───────────────────────
            return Task.WhenAll(toStart.Select(StartPluginAsync));
        }

        /// <summary>
        /// Start one plugin with a timeout, a failure is logged and the plugin disabled.
        /// </summary>
        private async Task StartPluginAsync(IGreenshotPlugin plugin)
        {
            using var timeoutSource = new CancellationTokenSource(StartTimeout);
            try
            {
                await plugin.StartAsync(SimpleServiceProvider.Current, timeoutSource.Token);
                lock (_startedPlugins)
                {
                    _startedPlugins.Add(plugin);
                }

                SimpleServiceProvider.Current.AddService(plugin);
            }
            catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
            {
                Log.ErrorFormat("Plugin {0} didn't start within {1}, it is disabled.", plugin.Name, StartTimeout);
            }
            catch (Exception e)
            {
                Log.ErrorFormat("Error during the start of plugin {0}, it is disabled.", plugin.Name);
                Log.Error(e);
            }
        }

        private bool IsPluginExcludedByConfig(Assembly assembly, string pluginFile)
        {
            // Get plugin identifier from assembly attributes
            string pluginConfigIdentifier = GetPluginIdentifier(assembly, pluginFile);

            if (CoreConfig.IncludePlugins is { } includePlugins
                && includePlugins.Count(p => !string.IsNullOrWhiteSpace(p)) > 0 // ignore empty entries i.e. a whitespace
                && !includePlugins.Contains(pluginConfigIdentifier))
            {
                Log.WarnFormat("Include plugin list: {0}", string.Join(",", includePlugins));
                Log.WarnFormat("Skipping the not included plugin '{0}' with version {1} from {2}", pluginConfigIdentifier, assembly.GetName().Version, pluginFile);
                return true;
            }

            if (CoreConfig.ExcludePlugins is { } excludePlugins
                && excludePlugins.Contains(pluginConfigIdentifier))
            {
                Log.WarnFormat("Exclude plugin list: {0}", string.Join(",", excludePlugins));
                Log.WarnFormat("Skipping the excluded plugin '{0}' with version {1} from {2}", pluginConfigIdentifier, assembly.GetName().Version, pluginFile);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieves the plugin identifier for the specified assembly.
        /// </summary>
        private string GetPluginIdentifier(Assembly assembly, string pluginFile)
        {
            // Try to find PluginIdentifierAttribute
            var attribute = assembly
                .GetCustomAttributes<AssemblyPluginIdentifierAttribute>()
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(attribute?.Identifier))
            {
               return attribute.Identifier;
            }

            // If no attribute found, fall back to the sub namespace
            var pluginSubNamespace = assembly.GetName().Name.Replace("Greenshot.Plugin.", string.Empty);
            Log.WarnFormat("No '{0}' found in '{1}'. Use plugin namespace '{2}' as fallback.", nameof(AssemblyPluginIdentifierAttribute), pluginFile, pluginSubNamespace);
            return pluginSubNamespace;
        }

        /// <summary>
        /// The registrations of one plugin
        /// </summary>
        private sealed class PluginServices : IPluginServices
        {
            private readonly IniConfig _iniConfig;
            private readonly Dictionary<Type, Func<object, object>> _settingsViews;
            private readonly List<Action> _deferred = new();
            private readonly List<IRecipeStepProvider> _recipeStepProviders = new();
            private readonly List<IRecipeDrawableProvider> _recipeDrawableProviders = new();

            public PluginServices(IniConfig iniConfig, Dictionary<Type, Func<object, object>> settingsViews)
            {
                _iniConfig = iniConfig;
                _settingsViews = settingsViews;
            }

            public void AddConfiguration<TSection>(TSection section) where TSection : class, IIniSection
            {
                _iniConfig.AddSection(section);
            }

            public void AddService<TService>(TService service)
            {
                SimpleServiceProvider.Current.AddService(service);
            }

            public void AddServices<TService>(Func<IEnumerable<TService>> factory)
            {
                _deferred.Add(() => SimpleServiceProvider.Current.AddService(factory()));
            }

            public void AddRecipeStepProvider(IRecipeStepProvider provider)
            {
                _recipeStepProviders.Add(provider);
            }

            public void AddRecipeDrawableProvider(IRecipeDrawableProvider provider)
            {
                _recipeDrawableProviders.Add(provider);
            }

            public void AddSettingsView<TViewModel>(Func<TViewModel, object> createView)
            {
                _settingsViews[typeof(TViewModel)] = viewModel => createView((TViewModel)viewModel);
            }

            /// <summary>
            /// Register what needs the loaded configuration
            /// </summary>
            public void RegisterDeferred(bool recipesEnabled)
            {
                foreach (var deferred in _deferred)
                {
                    deferred();
                }

                if (!recipesEnabled)
                {
                    return;
                }

                foreach (var recipeStepProvider in _recipeStepProviders)
                {
                    SimpleServiceProvider.Current.AddService(recipeStepProvider);
                    StepRegistry.Instance.RegisterProvider(recipeStepProvider);
                }

                foreach (var recipeDrawableProvider in _recipeDrawableProviders)
                {
                    SimpleServiceProvider.Current.AddService(recipeDrawableProvider);
                    RecipeDrawableRegistry.Instance.RegisterProvider(recipeDrawableProvider);
                }
            }
        }
    }
}
