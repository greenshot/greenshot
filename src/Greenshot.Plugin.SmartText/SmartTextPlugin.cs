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
using Greenshot.Base.Definitions;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Recipes.Triggers;
using Greenshot.Plugin.SmartText.Destinations;
using Greenshot.Plugin.SmartText.Detection;
using Greenshot.Plugin.SmartText.Recipes;
using Greenshot.Plugin.SmartText.ViewModels;
using Greenshot.Plugin.SmartText.Views;

namespace Greenshot.Plugin.SmartText;

/// <summary>
/// Smart text: finds data in the OCR text of a capture (web addresses, e-mail addresses, phone numbers, own patterns) to pick in a
/// window or to use in a recipe. A beta feature: without "beta tester" nothing is registered. See docs/vNext/smart-text.md.
/// </summary>
public class SmartTextPlugin : IGreenshotPlugin, IConfigurablePlugin, IRecipeStepProvider, IBuiltInRecipeProvider
{
    public const string RecipeIdSmartText = "recipe_smart_text";

    public ValueTask DisposeAsync() => default;

    public string Name => "SmartText";

    /// <summary>
    /// The feature is for beta testers only
    /// </summary>
    internal static bool IsEnabled => IniConfigRegistry.GetSection<ICoreConfiguration>()?.IsBetaTester == true;

    internal static ISmartTextLanguage Language => Texts.Get<ISmartTextLanguage>();

    public void ConfigureServices(IPluginServices services)
    {
        Texts.Register<ISmartTextLanguage>(new SmartTextLanguageImpl());
        services.AddConfiguration(new SmartTextConfigurationImpl());

        // Decided with the loaded configuration: nothing is registered for a user who isn't a beta tester
        services.AddServices<IDestination>(() => IsEnabled ? new IDestination[] { new SmartTextDestination() } : Array.Empty<IDestination>());
        services.AddServices<IBuiltInRecipeProvider>(() => IsEnabled ? new IBuiltInRecipeProvider[] { this } : Array.Empty<IBuiltInRecipeProvider>());
        services.AddRecipeStepProvider(this);
        services.AddSettingsView<SmartTextSettingsViewModel>(viewModel => new SmartTextSettingsView(viewModel));
    }

    public object CreateSettingsViewModel(IServiceProvider services) => IsEnabled ? new SmartTextSettingsViewModel(SmartTextDefinitions.CreateStore()) : null;

    public void RegisterSteps(IStepRegistry registry)
    {
        if (registry == null || !IsEnabled) return;
        registry.RegisterJsonTypes(SmartTextStep.JsonTypes);
        registry.Register<SmartTextStep>(config => new SmartTextStep(config));
    }

    /// <summary>
    /// "Capture smart text": select a region (its text is read while selecting), pick in the window, copy what was picked
    /// </summary>
    public IEnumerable<CaptureRecipe> CreateRecipes()
    {
        var recipe = new CaptureRecipe(RecipeIdSmartText, "SmartText.recipe_name", "SmartText.recipe_description")
            .AddNode(RecipeStepConfig.CreateSource("acquire", CaptureSourceType.TextOcr, captureMouse: false))
            .AddNode(RecipeStepConfig.CreateSelection("select", CaptureMode.Text))
            .AddNode(CreateStep("find"))
            .AddNode(RecipeStepConfig.CreateConditional("picked", new[]
            {
                new KeyValuePair<string, string>("Yes", "${var.Found.picked > 0}"),
                new KeyValuePair<string, string>("No", "else")
            }))
            .AddNode(CreateCopy("copy"));
        recipe.Flow = new RecipeFlowConfig("acquire")
            .AddTransition("acquire", "select")
            .AddTransition("select", "find")
            .AddTransition("find", "picked")
            .AddConditionalTransition("picked", "Yes", "copy");
        // In the tray menu with the captures
        recipe.AddTrigger(TriggerConfig.CreateMainMenu("SmartText.recipe_name", 50));
        yield return recipe;
    }

    private static RecipeNodeConfig CreateStep(string id)
    {
        var node = new RecipeNodeConfig(id, SmartTextStep.StepType, "Smart text");
        node.Set(SmartTextStep.PickParameter, SmartTextStep.PickAsk);
        return node;
    }

    private static RecipeNodeConfig CreateCopy(string id)
    {
        var node = RecipeStepConfig.CreateClipboard(id, "TextOnly");
        node.Set("ClipboardCustomText", "${var.Found.all}");
        return node;
    }

    /// <summary>
    /// Like the commands: a pattern file added or changed outside Greenshot is left out at the start, once Greenshot runs the user
    /// sees the pattern and decides whether to use it
    /// </summary>
    public Task StartAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        if (IsEnabled)
        {
            DefinitionApprovalAtStartup.AskWhenIdle(SmartTextDefinitions.CreateStore, ConfirmAtStartup);
        }

        return Task.CompletedTask;
    }

    private static bool ConfirmAtStartup(StoredDefinition<SmartTextPattern> stored) =>
        DefinitionImport.Confirm(null, SmartTextSettingsViewModel.DescribeApproval(stored.Definition, stored.Id),
            new[] { Language.ApproveButton, Language.ApproveLater }) == 0;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
