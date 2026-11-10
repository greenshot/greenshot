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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Triggers;
using Greenshot.Plugin.Divoom.Destinations;
using Greenshot.Plugin.Divoom.ViewModels;
using Greenshot.Plugin.Divoom.Views;

namespace Greenshot.Plugin.Divoom;

/// <summary>
/// Shows a part of the screen on a Divoom pixel display in the network (e.g. Pixoo 64): "Capture for Divoom" starts the
/// fixed size capture with the size of its screen, the capture goes to the Divoom destination. A beta feature: without
/// "beta tester" nothing is registered. See docs/vNext/divoom.md.
/// </summary>
public class DivoomPlugin : IGreenshotPlugin, IConfigurablePlugin, IBuiltInRecipeProvider
{
    /// <summary>
    /// The id of the built-in recipe "Capture for Divoom"
    /// </summary>
    public const string RecipeIdCapture = "recipe_divoom";

    public ValueTask DisposeAsync() => default;

    public string Name => "Divoom";

    /// <summary>
    /// The feature is for beta testers only
    /// </summary>
    internal static bool IsEnabled => IniConfigRegistry.GetSection<ICoreConfiguration>()?.IsBetaTester == true;

    /// <summary>
    /// The settings of the plugin, null before it registered them
    /// </summary>
    internal static IDivoomConfiguration Configuration { get; private set; }

    public void ConfigureServices(IPluginServices services)
    {
        Texts.Register<IDivoomLanguage>(new DivoomLanguageImpl());
        var section = new DivoomConfigurationImpl();
        services.AddConfiguration(section);
        Configuration = section;

        // Decided with the loaded configuration: nothing is registered for a user who isn't a beta tester
        services.AddServices<IDestination>(() => IsEnabled ? new IDestination[] { new DivoomDestination() } : Array.Empty<IDestination>());
        services.AddServices<IBuiltInRecipeProvider>(() => IsEnabled ? new IBuiltInRecipeProvider[] { this } : Array.Empty<IBuiltInRecipeProvider>());
        services.AddSettingsView<DivoomSettingsViewModel>(viewModel => new DivoomSettingsView(viewModel));
    }

    public object CreateSettingsViewModel(IServiceProvider services) => IsEnabled ? new DivoomSettingsViewModel(Configuration) : null;

    public Task StartAsync(IServiceProvider services, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// "Capture for Divoom" in the tray menu: the fixed size capture with only the size of the screen of the device (its preview
    /// shows it enlarged as the device will), without the mouse cursor and the extensions of the other captures (a border
    /// doesn't fit on 64 pixels), the capture goes to the device
    /// </summary>
    public IEnumerable<CaptureRecipe> CreateRecipes()
    {
        var recipe = new CaptureRecipe(
                RecipeIdCapture,
                "Divoom.capture_menu",
                "Select an area of the size of the screen of the Divoom device and show it there")
            .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.Region, captureMouse: false))
            .AddNode(RecipeStepConfig.CreateSelection("select", CaptureMode.Region, allowWindowSnapping: false)
                .Set("FixedCaptureArea", "${config.Divoom.PanelSize}x${config.Divoom.PanelSize}"))
            .AddNode(RecipeStepConfig.CreateDestinations("export", new[] { DivoomDestination.DesignationName }))
            .AddNode(RecipeStepConfig.CreateNotification("notify"))
            .AddTrigger(TriggerConfig.CreateMainMenu("Divoom.capture_menu", 50, "contextmenu_capturearea.Image"));
        recipe.Flow = new RecipeFlowConfig("acquire")
            .AddTransition("acquire", "select")
            .AddTransition("select", "export")
            .AddTransition("export", "notify");
        yield return recipe;
    }
}
