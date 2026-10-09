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

namespace Greenshot.Base.Interfaces.Plugin
{
    /// <summary>
    /// A Greenshot plugin (roadmap section 5.4). The host creates it, lets it register what it offers, loads the configuration
    /// and then starts all plugins in parallel with a timeout, without waiting for them: a slow or failing plugin is logged
    /// and never blocks the startup.
    /// </summary>
    public interface IGreenshotPlugin : IAsyncDisposable
    {
        /// <summary>
        /// Name of the plugin
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Registration only: configuration sections, services, destinations, settings views. Synchronous and without I/O,
        /// the configuration isn't loaded yet (use the factory overloads for what needs it).
        /// </summary>
        /// <param name="services">IPluginServices</param>
        void ConfigureServices(IPluginServices services);

        /// <summary>
        /// Start the plugin (e.g. add menu entries on the UI thread via the IUiDispatcher), the configuration is loaded.
        /// </summary>
        /// <param name="services">the services of the host (IUiDispatcher, IUserInteraction, IStaWorkerFactory, ...)</param>
        /// <param name="cancellationToken">CancellationToken, cancelled when the start takes too long</param>
        Task StartAsync(IServiceProvider services, CancellationToken cancellationToken);

        /// <summary>
        /// Stop the plugin (remove menu entries, unsubscribe events), called when Greenshot exits.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken, cancelled when the stop takes too long</param>
        Task StopAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// A plugin with settings: the host shows the view model with the view the plugin registered for it (IPluginServices.AddSettingsView).
    /// </summary>
    public interface IConfigurablePlugin
    {
        /// <summary>
        /// The view model of the settings of the plugin
        /// </summary>
        /// <param name="services">the services of the host</param>
        object CreateSettingsViewModel(IServiceProvider services);
    }
}
