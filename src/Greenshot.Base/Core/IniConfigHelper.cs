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
using System.Linq;
using System.Reflection;
using Dapplo.Ini;
using Dapplo.Ini.Interfaces;
using log4net;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Helper to ensure IniConfigRegistry is initialized and populated with default values
    /// when running outside of normal application startup (e.g. in the Windows Forms Designer
    /// or in unit tests).
    /// </summary>
    public static class IniConfigHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(IniConfigHelper));
        private static readonly object SyncLock = new();

        /// <summary>
        /// Normal production INI file name.
        /// </summary>
        public const string NormalConfigName = "greenshot.ini";

        /// <summary>
        /// Design-time / test fallback INI file name. Distinct from <see cref="NormalConfigName"/>
        /// so design-time initialization never alters production settings or disk files.
        /// </summary>
        public const string DesignTimeConfigName = "greenshot-designtime.ini";

        /// <summary>
        /// Ensures that an <see cref="IniConfig"/> instance is registered in <see cref="IniConfigRegistry"/>.
        /// If neither <see cref="NormalConfigName"/> nor <see cref="DesignTimeConfigName"/> is registered,
        /// this creates an in-memory <see cref="DesignTimeConfigName"/> config populated with default values
        /// for all known section interfaces.
        /// </summary>
        /// <returns>The active <see cref="IniConfig"/>.</returns>
        public static IniConfig EnsureInitialized()
        {
            if (IniConfigRegistry.TryGet(NormalConfigName, out var config))
            {
                return config;
            }

            lock (SyncLock)
            {
                if (IniConfigRegistry.TryGet(NormalConfigName, out config))
                {
                    return config;
                }

                if (IniConfigRegistry.TryGet(DesignTimeConfigName, out config))
                {
                    // Check if ICoreConfiguration is registered for the current Type context.
                    // In the Visual Studio WinForms Designer, Dapplo.Ini can persist in the VS host process across rebuilds,
                    // while Greenshot.Base.dll is recompiled and reloaded with fresh Type identities.
                    try
                    {
                        config.GetSection<ICoreConfiguration>();
                        return config;
                    }
                    catch (InvalidOperationException)
                    {
                        // ICoreConfiguration is missing or registered under an older Type identity.
                        // Add an ICoreConfiguration section mapped to the current Type.
                        var coreConfigInstance = new CoreConfigurationImpl();
                        coreConfigInstance.ResetToDefaults();
                        (coreConfigInstance as IAfterLoad)?.OnAfterLoad();
                        config.AddSection<ICoreConfiguration>(coreConfigInstance);
                        return config;
                    }
                }

                Log.Debug("Initializing design-time fallback configuration.");

                // Register custom converters before building configuration
                IniValueConverters.Register();

                // Build design-time configuration with core section defaults
                var coreConfig = new CoreConfigurationImpl();
                coreConfig.ResetToDefaults();
                (coreConfig as IAfterLoad)?.OnAfterLoad();

                var builder = IniConfigRegistry.ForFile(DesignTimeConfigName)
                    .RegisterSection<ICoreConfiguration>(coreConfig);

                config = builder.Create();

                return config;
            }
        }

        /// <summary>
        /// Ensures that the specified section interface <typeparamref name="T"/> is registered
        /// on the active <see cref="IniConfig"/> and initialized with default values.
        /// </summary>
        /// <typeparam name="T">The section interface type deriving from <see cref="IIniSection"/>.</typeparam>
        /// <param name="factory">Optional factory to create the section instance if not already registered.</param>
        /// <returns>The section instance.</returns>
        public static T EnsureSection<T>(Func<T> factory = null) where T : class, IIniSection
        {
            var config = EnsureInitialized();
            try
            {
                return config.GetSection<T>();
            }
            catch (InvalidOperationException)
            {
                lock (SyncLock)
                {
                    try
                    {
                        return config.GetSection<T>();
                    }
                    catch (InvalidOperationException)
                    {
                        T instance = null;
                        var sections = config.GetSections();
                        if (sections != null)
                        {
                            foreach (var s in sections)
                            {
                                if (s is T typed)
                                {
                                    instance = typed;
                                    break;
                                }
                            }
                        }

                        if (instance == null)
                        {
                            instance = factory != null ? factory() : CreateSectionInstance<T>();
                            if (instance != null)
                            {
                                instance.ResetToDefaults();
                                (instance as IAfterLoad)?.OnAfterLoad();
                            }
                        }

                        if (instance != null)
                        {
                            config.AddSection<T>(instance);
                            return instance;
                        }

                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Unregisters the design-time configuration from <see cref="IniConfigRegistry"/>.
        /// Called during normal application startup (<c>GreenshotMain</c>) to ensure a clean registry.
        /// </summary>
        public static void UnregisterDesignTimeConfig()
        {
            IniConfigRegistry.Unregister(DesignTimeConfigName);
        }

        private static T CreateSectionInstance<T>() where T : class, IIniSection
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly == null || assembly.IsDynamic)
                {
                    continue;
                }

                string name = assembly.GetName().Name;
                if (!name.StartsWith("Greenshot", StringComparison.OrdinalIgnoreCase) &&
                    !name.StartsWith("Dapplo", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Type[] types;
                try
                {
                    types = assembly.GetExportedTypes();
                }
                catch (ReflectionTypeLoadException rtle)
                {
                    types = rtle.Types.Where(t => t != null).ToArray();
                }
                catch
                {
                    continue;
                }

                foreach (var type in types)
                {
                    try
                    {
                        if (type != null && !type.IsAbstract && !type.IsInterface && typeof(T).IsAssignableFrom(type))
                        {
                            var ctor = type.GetConstructor(Type.EmptyTypes);
                            if (ctor != null)
                            {
                                return (T)ctor.Invoke(null);
                            }
                        }
                    }
                    catch
                    {
                        // Ignore
                    }
                }
            }

            return null;
        }
    }
}
