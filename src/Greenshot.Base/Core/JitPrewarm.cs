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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using log4net;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// JIT-compile code in the background, so this doesn't happen on the UI thread when it's used the first time
    /// </summary>
    public static class JitPrewarm
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(JitPrewarm));

        /// <summary>
        /// JIT-compile the methods of the types in the namespaces.
        /// An assembly which runs from a native image (NGen, done by the installer) is skipped, there is nothing to compile.
        /// </summary>
        /// <param name="assemblies">Assembly array with the types</param>
        /// <param name="namespaces">string array with the namespaces of the types to prepare</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>int with the number of prepared methods</returns>
        public static int PrepareMethods(Assembly[] assemblies, string[] namespaces, CancellationToken cancellationToken)
        {
            const BindingFlags allMethods = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var nativeImages = NativeImageNames();
            int prepared = 0;
            var types = assemblies
                .Where(assembly => !nativeImages.Contains(assembly.GetName().Name + ".ni.dll"))
                .SelectMany(LoadableTypes)
                .Where(type => type.Namespace != null && namespaces.Contains(type.Namespace) && !type.ContainsGenericParameters);
            foreach (var type in types)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var method in type.GetMethods(allMethods).Cast<MethodBase>().Concat(type.GetConstructors(allMethods)))
                {
                    if (method.IsAbstract || method.ContainsGenericParameters || method.GetMethodBody() == null)
                    {
                        continue;
                    }

                    try
                    {
                        RuntimeHelpers.PrepareMethod(method.MethodHandle);
                        prepared++;
                    }
                    catch (Exception ex)
                    {
                        // Not important, the method is compiled when it's called
                        Log.DebugFormat("Couldn't prepare {0}.{1}: {2}", type.FullName, method.Name, ex.Message);
                    }
                }
            }

            return prepared;
        }

        /// <summary>
        /// The file names of the loaded native images (like Greenshot.Editor.ni.dll)
        /// </summary>
        private static ISet<string> NativeImageNames()
        {
            try
            {
                using var process = Process.GetCurrentProcess();
                return new HashSet<string>(process.Modules.Cast<ProcessModule>().Select(module => module.ModuleName), StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Log.Debug("Couldn't read the loaded modules", ex);
                return new HashSet<string>();
            }
        }

        private static Type[] LoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null).ToArray();
            }
        }
    }
}
