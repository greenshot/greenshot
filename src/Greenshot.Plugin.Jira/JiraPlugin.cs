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
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Threading;
using Greenshot.Plugin.Jira.Api;
using Greenshot.Plugin.Jira.Destinations;
using Greenshot.Plugin.Jira.Recipes;
using Greenshot.Plugin.Jira.Views;
using log4net;
using System.Threading;
using Greenshot.Base.Languages;

namespace Greenshot.Plugin.Jira;

/// <summary>
/// This is the JiraPlugin base code
/// </summary>
public class JiraPlugin : IGreenshotPlugin, IConfigurablePlugin, IRecipeStepProvider
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(JiraPlugin));
    private IJiraConfiguration _config;
    private JiraConnector _jiraConnector;

    public ValueTask DisposeAsync()
    {
        _jiraConnector?.Dispose();
        _jiraConnector = null;
        return default;
    }

    /// <summary>
    /// Name of the plugin
    /// </summary>
    public string Name => "Jira";

    public void ConfigureServices(IPluginServices services)
    {
        Texts.Register<IJiraLanguage>(new JiraLanguageImpl());
        var section = new JiraConfigurationImpl();
        services.AddConfiguration(section);
        _config = section;

        // The connector needs the loaded configuration
        services.AddServices(() =>
        {
            _jiraConnector = new JiraConnector();
            return new[] { _jiraConnector };
        });
        services.AddService<IIconProvider>(new JiraIconProvider());
        services.AddService<IDestination>(new JiraDestination());
        services.AddRecipeStepProvider(this);
        services.AddSettingsView<IJiraConfiguration>(config => new JiraConfigurationView(config));
    }

    public object CreateSettingsViewModel(IServiceProvider services) => _config;

    /// <summary>
    /// Registers recipe step factories provided by the Jira plugin.
    /// </summary>
    /// <param name="registry">The step registry.</param>
    public void RegisterSteps(IStepRegistry registry)
    {
        if (registry == null) return;
        registry.Register<JiraStep>(config => new JiraStep(config));
    }

    /// <summary>
    /// Register the upload dialog, the connection waits for the first login (see JiraConnector)
    /// </summary>
    public Task StartAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        services.GetService<IDialogViewRegistry>()?.Register<JiraUploadRequest, JiraUploadChoice>(JiraUploadWindow.Show);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        UiDispatcher.Current.RunOnUiAsync(() => _jiraConnector?.Logout(), cancellationToken);

}