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

using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Recipes.Pipeline;

namespace Greenshot.Linux.Recipes
{
    /// <summary>
    /// What the app gives the recipe engine, the services SimpleServiceProvider has on Windows
    /// </summary>
    public static class RecipeServices
    {
        /// <summary>
        /// greenshot.ini: ${config.*}, the recipe options, the settings of the steps
        /// </summary>
        public static CoreSettings Settings { get; set; }

        public static IUiDispatcher Ui { get; set; }

        /// <summary>
        /// Where the windows are, as far as the compositor tells (Sway, Hyprland); for the AI tools
        /// </summary>
        public static Greenshot.Linux.Core.Compositor.ICompositor Compositor { get; set; }

        /// <summary>
        /// The app, for the steps which capture, select, export and show windows
        /// </summary>
        public static IRecipeHost Host { get; set; }

        public static IUserInteraction UserInteraction { get; set; }

        /// <summary>
        /// The providers of plugin steps (IRecipeStepProvider), the plugins register their steps with them
        /// </summary>
        public static IReadOnlyList<IRecipeStepProvider> StepProviders { get; set; } = Array.Empty<IRecipeStepProvider>();

        /// <summary>
        /// The recipes: loaded files, built-in recipes, extensions, approvals
        /// </summary>
        public static IRecipeManager Manager { get; set; }

        public static ICapturePipeline Pipeline { get; set; }

        public static ICaptureFlowRunner Runner { get; set; }

        /// <summary>
        /// Global hotkeys of recipe triggers
        /// </summary>
        public static IHotkeyService Hotkeys { get; set; }

        /// <summary>
        /// The approval window of recipe files
        /// </summary>
        public static IRecipeApprovalUi ApprovalUi { get; set; }

        /// <summary>
        /// Recipes of other parts (AI tools, ...), registered like the built-in ones
        /// </summary>
        public static IReadOnlyList<IBuiltInRecipeProvider> BuiltInRecipeProviders { get; set; } = Array.Empty<IBuiltInRecipeProvider>();

        /// <summary>
        /// A destination by its designation for the approval, also when it is switched off (e.g. an external command)
        /// </summary>
        public static Func<string, Greenshot.Linux.Core.Destinations.IDestination> AuthorizationDestinations { get; set; }
    }

    /// <summary>
    /// Binds global hotkeys for recipe triggers (the GlobalShortcuts portal)
    /// </summary>
    public interface IHotkeyService
    {
        void Register(string id, string description, string hotkey, Action pressed);

        void Unregister(string id);
    }

    /// <summary>
    /// [Recipe] of greenshot.ini, IRecipeConfiguration of Windows
    /// </summary>
    public static class RecipeConfigHelper
    {
        public static RecipeConfiguration TryGetRecipeConfiguration() => RecipeServices.Settings == null ? null : new RecipeConfiguration();

        public const string SectionName = "Recipe";

        /// <summary>
        /// EnableRecipeFeature, on unless set to False
        /// </summary>
        public static bool IsRecipeFeatureEnabled(RecipeConfiguration configuration) => configuration != null && configuration.Enabled;

        public static bool IsRecipeFeatureEnabled() =>
            !string.Equals(RecipeServices.Settings?.GetValue(SectionName, "EnableRecipeFeature"), "False", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// The recipe files to load (RecipeFiles, separated by ;), Greenshot doesn't scan a folder
        /// </summary>
        public static IReadOnlyList<string> RecipeFiles
        {
            get => (RecipeServices.Settings?.GetValue(SectionName, "RecipeFiles") ?? string.Empty)
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            set => RecipeServices.Settings?.SetValue(SectionName, "RecipeFiles", string.Join(";", value));
        }

        /// <summary>
        /// The recipes switched off (DisabledRecipeIds, separated by ;)
        /// </summary>
        public static IReadOnlyList<string> DisabledRecipeIds
        {
            get => (RecipeServices.Settings?.GetValue(SectionName, "DisabledRecipeIds") ?? string.Empty)
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            set => RecipeServices.Settings?.SetValue(SectionName, "DisabledRecipeIds", string.Join(";", value));
        }
    }
}
