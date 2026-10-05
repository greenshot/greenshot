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
using System.Reflection;

namespace Greenshot.Helpers
{
    /// <summary>
    /// Registers an optional part of Greenshot (the browser extension, AI tools). The attribute lives in the folder of the part,
    /// so leaving the folder out of the build (Greenshot Light) also leaves out its registration.
    /// The module type implements one or more of the extension interfaces, e.g. <see cref="Greenshot.Ipc.IIpcCommandExtension"/>
    /// or <see cref="Greenshot.Recipes.IBuiltInRecipeProvider"/>, and needs a public parameterless constructor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class GreenshotModuleAttribute : Attribute
    {
        public GreenshotModuleAttribute(Type moduleType, int order = 0)
        {
            ModuleType = moduleType;
            Order = order;
        }

        public Type ModuleType { get; }

        /// <summary>
        /// Modules with a lower order come first (e.g. their built-in recipes are registered first)
        /// </summary>
        public int Order { get; }
    }

    /// <summary>
    /// Finds the modules registered with <see cref="GreenshotModuleAttribute"/>
    /// </summary>
    internal static class GreenshotModules
    {
        private static readonly Lazy<IReadOnlyList<Type>> ModuleTypes = new Lazy<IReadOnlyList<Type>>(() =>
            typeof(GreenshotModules).Assembly.GetCustomAttributes<GreenshotModuleAttribute>()
                .OrderBy(a => a.Order)
                .ThenBy(a => a.ModuleType.FullName, StringComparer.Ordinal)
                .Select(a => a.ModuleType)
                .ToList());

        /// <summary>
        /// New instances of the modules which implement T, in their order
        /// </summary>
        public static IReadOnlyList<T> Create<T>() where T : class
        {
            return ModuleTypes.Value
                .Where(t => typeof(T).IsAssignableFrom(t))
                .Select(t => (T)Activator.CreateInstance(t))
                .ToList();
        }
    }
}
