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

using System.Runtime.CompilerServices;
using Greenshot.Base.Core;

namespace Greenshot.Tests
{
    /// <summary>
    /// Runs once when the test assembly is loaded, before any test.
    /// </summary>
    internal static class TestModuleInitializer
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            // The hotkey tests (and every recipe with a hotkey trigger) register hotkeys, which would install the
            // low-level keyboard hook of Dapplo.Windows: when the test host exits after all tests passed, the hook's
            // thread throws and the test run is reported as crashed. The tests feed keys with HandleKeyboardEvent instead.
            HotkeyManager.UseKeyboardHook = false;
        }
    }
}

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// .NET Framework 4.8 doesn't have this attribute, the C# compiler only needs it by name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class ModuleInitializerAttribute : Attribute
    {
    }
}
