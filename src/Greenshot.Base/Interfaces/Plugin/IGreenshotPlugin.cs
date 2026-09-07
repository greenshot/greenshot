/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using Dapplo.Ini;

namespace Greenshot.Base.Interfaces.Plugin
{
    /// <summary>
    /// This defines the plugin
    /// </summary>
    public interface IGreenshotPlugin : IDisposable
    {
        /// <summary>
        /// Phase 1 — called before the INI file is read.
        /// The plugin receives the shared <see cref="Dapplo.Ini.IniConfig"/> and must register
        /// its configuration section(s) by calling <c>iniConfig.AddSection(new XxxImpl())</c>.
        /// Translations may also be registered here.
        /// No file I/O has occurred at this point.
        /// </summary>
        /// <param name="iniConfig">The application-wide Dapplo.Ini config object.</param>
        void RegisterConfiguration(IniConfig iniConfig);

        /// <summary>
        /// Phase 2 — called after the INI file has been loaded.
        /// The plugin should register its services into the supplied DI container.
        /// Configuration values are safe to read at this point.
        /// </summary>
        /// <param name="serviceLocator">The application-wide service locator.</param>
        void RegisterServices(IServiceLocator serviceLocator);

        /// <summary>
        /// Phase 3 — called after all services have been registered.
        /// The plugin may perform its remaining start-up work here; both configuration
        /// and all registered services are guaranteed to be available.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the plugin started successfully and should be shown;
        /// <c>false</c> to indicate that the plugin is not active.
        /// </returns>
        bool Start();

        /// <summary>
        /// Unload of the plugin
        /// </summary>
        void Shutdown();

        /// <summary>
        /// Open the Configuration Form, will/should not be called before handshaking is done
        /// </summary>
        void Configure();

        /// <summary>
        /// Define the name of the plugin
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Specifies if the plugin can be configured
        /// </summary>
        bool IsConfigurable { get; }
    }
}