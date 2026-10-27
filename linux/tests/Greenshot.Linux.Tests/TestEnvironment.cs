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

using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Plugins.ExternalCommand;
using Greenshot.Linux.Recipes;
using Greenshot.Linux.Recipes.Approval;
using Greenshot.Linux.Recipes.Pipeline;

namespace Greenshot.Linux.Tests;

/// <summary>
/// What the recipe tests (ported from Greenshot.Tests) need: greenshot.ini in a temporary folder, the English texts and the
/// steps of the plugins, like the TestEnvironment of Windows
/// </summary>
public static class TestEnvironment
{
    private static readonly object SyncLock = new();
    private static bool _initialized;

    public static string Folder { get; } = Path.Combine(Path.GetTempPath(), "gs-recipes-" + Guid.NewGuid().ToString("N"));

    public static string[] LanguageFolders { get; } =
    [
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../src/Greenshot/Languages")),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/Greenshot.Linux/Languages")),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../src/Greenshot.Plugin.ExternalCommand/Languages")),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../src/Greenshot.Plugin.RecipeEditor/Languages")),
    ];

    public static void EnsureInitialized()
    {
        if (_initialized) return;
        lock (SyncLock)
        {
            if (_initialized) return;
            Directory.CreateDirectory(Folder);
            var settings = CoreSettings.Load(Path.Combine(Folder, "greenshot.ini"));
            RecipeServices.Settings = settings;
            RecipeTrustStore.UseStoreFile(Path.Combine(Folder, "recipe_trust.dat"));
            Texts.Initialize("en-US", LanguageFolders);
            RecipeServices.StepProviders = [new ExternalCommandPlugin(settings)];
            _initialized = true;
        }
    }
}

/// <summary>
/// The collections of the ported tests: the recipe manager and the static texts are shared, so these don't run in parallel
/// </summary>
public static class TestCollections
{
    public const string RecipeManager = nameof(Languages.TextsTests);
}
