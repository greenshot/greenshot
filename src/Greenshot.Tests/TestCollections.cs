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

using Xunit;

namespace Greenshot.Tests
{
    /// <summary>
    /// xUnit runs test classes in parallel, unless they are in the same collection. Classes that change process-wide
    /// state must share a collection, otherwise one test can change the state while another one asserts on it.
    /// </summary>
    public static class TestCollections
    {
        /// <summary>Tests that register recipes in the global <c>RecipeManager.Instance</c>.</summary>
        public const string RecipeManager = "RecipeManager";

        /// <summary>Tests that change or depend on the global WPF theme (<c>WpfThemeHelper.IsDarkMode</c>, <c>ThemeManager.Instance</c>).</summary>
        public const string WpfThemeState = "WpfThemeState";

        /// <summary>Tests that change the real Windows clipboard: they run alone, never in parallel with other tests.</summary>
        public const string Clipboard = "Clipboard";
    }

    [CollectionDefinition(TestCollections.Clipboard, DisableParallelization = true)]
    public class ClipboardCollection
    {
    }

    [CollectionDefinition(TestCollections.RecipeManager)]
    public class RecipeManagerCollection
    {
    }

    [CollectionDefinition(TestCollections.WpfThemeState)]
    public class WpfThemeStateCollection
    {
    }
}
