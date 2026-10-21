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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Threading;
using Greenshot.Plugin.Atlassian.Confluence;
using Greenshot.Plugin.Atlassian.Confluence.Api;
using Greenshot.Plugin.Atlassian.Confluence.Destinations;
using Greenshot.Plugin.Atlassian.Confluence.Recipes;
using Greenshot.Plugin.Atlassian.Confluence.Views;
using Greenshot.Plugin.Atlassian.Jira;
using Greenshot.Plugin.Atlassian.Jira.Api;
using Greenshot.Plugin.Atlassian.Jira.Destinations;
using Greenshot.Plugin.Atlassian.Jira.Recipes;
using Greenshot.Plugin.Atlassian.Jira.Views;
using Greenshot.Plugin.Atlassian.Views;

namespace Greenshot.Plugin.Atlassian;

/// <summary>
/// Jira and Confluence: upload to an issue or a page, the Jira title monitor, one sign-in for both on the same site
/// </summary>
public class AtlassianPlugin : IGreenshotPlugin, IConfigurablePlugin, IRecipeStepProvider
{
    private IJiraConfiguration _jiraConfig;
    private IConfluenceConfiguration _confluenceConfig;
    private JiraConnector _jiraConnector;
    private static ConfluenceConnector _confluenceConnector;

    /// <summary>
    /// The Confluence connector, its methods sign in (ask the user for the credentials) when needed
    /// </summary>
    public static ConfluenceConnector ConfluenceConnector => _confluenceConnector;

    public ValueTask DisposeAsync()
    {
        _jiraConnector?.Dispose();
        _jiraConnector = null;
        _confluenceConnector?.Dispose();
        _confluenceConnector = null;
        return default;
    }

    /// <summary>
    /// Name of the plugin
    /// </summary>
    public string Name => "Atlassian";

    public void ConfigureServices(IPluginServices services)
    {
        Texts.Register<IJiraLanguage>(new JiraLanguageImpl());
        Texts.Register<IConfluenceLanguage>(new ConfluenceLanguageImpl());
        var jiraSection = new JiraConfigurationImpl();
        services.AddConfiguration(jiraSection);
        _jiraConfig = jiraSection;
        var confluenceSection = new ConfluenceConfigurationImpl();
        services.AddConfiguration(confluenceSection);
        _confluenceConfig = confluenceSection;

        // The connectors need the loaded configuration
        services.AddServices(() =>
        {
            _jiraConnector = new JiraConnector();
            _confluenceConnector = new ConfluenceConnector();
            // Jira and Confluence on the same site (Cloud: https://example.atlassian.net and .../wiki) use the same account
            _jiraConnector.SignIn.ShareCredentialsWith(_confluenceConnector.SignIn);
            return new object[] { _jiraConnector, _confluenceConnector };
        });
        services.AddService<IIconProvider>(new JiraIconProvider());
        services.AddService<IIconProvider>(new ConfluenceIconProvider());
        services.AddService<IDestination>(new JiraDestination());
        services.AddService<IDestination>(new ConfluenceDestination());
        services.AddRecipeStepProvider(this);
        services.AddSettingsView<AtlassianSettingsViewModel>(settings => new AtlassianConfigurationView(settings));
    }

    public object CreateSettingsViewModel(IServiceProvider services) => new AtlassianSettingsViewModel(_jiraConfig, _confluenceConfig);

    /// <summary>
    /// Registers the recipe steps of Jira and Confluence.
    /// </summary>
    /// <param name="registry">The step registry.</param>
    public void RegisterSteps(IStepRegistry registry)
    {
        if (registry == null) return;
        registry.Register<JiraStep>(config => new JiraStep(config));
        registry.Register<ConfluenceStep>(config => new ConfluenceStep(config));
    }

    /// <summary>
    /// Register the upload dialogs, the connections wait for the first sign-in (see JiraConnector and ConfluenceConnector)
    /// </summary>
    public Task StartAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var dialogs = services.GetService<IDialogViewRegistry>();
        dialogs?.Register<JiraUploadRequest, JiraUploadChoice>(JiraUploadWindow.Show);
        dialogs?.Register<ConfluenceUploadRequest, ConfluenceUploadChoice>(ConfluenceUploadWindow.Show);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        UiDispatcher.Current.RunOnUiAsync(() =>
        {
            _jiraConnector?.Logout();
            _confluenceConnector?.Logout();
        }, cancellationToken);
}
