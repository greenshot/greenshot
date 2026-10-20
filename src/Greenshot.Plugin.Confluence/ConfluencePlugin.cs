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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Threading;
using Greenshot.Plugin.Confluence.Api;
using Greenshot.Plugin.Confluence.Destinations;
using Greenshot.Base.Languages;
using Greenshot.Plugin.Confluence.Recipes;
using Greenshot.Plugin.Confluence.Views;

namespace Greenshot.Plugin.Confluence;

/// <summary>
/// This is the ConfluencePlugin base code
/// </summary>
public class ConfluencePlugin : IGreenshotPlugin, IConfigurablePlugin, IRecipeStepProvider
{
    private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ConfluencePlugin));
    private static ConfluenceConnector _confluenceConnector;
    private static IConfluenceConfiguration _config;

    public ValueTask DisposeAsync()
    {
        return default;
    }

    /// <summary>
    /// Name of the plugin
    /// </summary>
    public string Name => "Confluence";

    private static void CreateConfluenceConnector()
    {
        if (_confluenceConnector == null)
        {
            _confluenceConnector = new ConfluenceConnector();
        }
    }

    /// <summary>
    /// The connector, created when needed. Its methods log in (ask the user for the credentials) when needed.
    /// </summary>
    public static ConfluenceConnector ConfluenceConnector
    {
        get
        {
            if (_confluenceConnector == null)
            {
                CreateConfluenceConnector();
            }

            return _confluenceConnector;
        }
    }

    public void ConfigureServices(IPluginServices services)
    {
        Texts.Register<IConfluenceLanguage>(new ConfluenceLanguageImpl());
        var section = new ConfluenceConfigurationImpl();
        services.AddConfiguration(section);
        _config = section;

        services.AddService<IIconProvider>(new ConfluenceIconProvider());
        services.AddService<IDestination>(new ConfluenceDestination());

        services.AddRecipeStepProvider(this);
        services.AddSettingsView<IConfluenceConfiguration>(config => new ConfluenceConfigurationView(config));
    }

    public object CreateSettingsViewModel(IServiceProvider services) => _config;

    /// <summary>
    /// Registers recipe step factories provided by the Confluence plugin.
    /// </summary>
    /// <param name="registry">The step registry.</param>
    public void RegisterSteps(IStepRegistry registry)
    {
        if (registry == null) return;
        registry.Register<ConfluenceStep>(config => new ConfluenceStep(config));
    }

    /// <summary>
    /// Register the upload dialog
    /// </summary>
    public Task StartAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        services.GetService<IDialogViewRegistry>()?.Register<ConfluenceUploadRequest, ConfluenceUploadChoice>(ConfluenceUploadWindow.Show);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        UiDispatcher.Current.RunOnUiAsync(() =>
        {
            if (_confluenceConnector != null)
            {
                _confluenceConnector.Dispose();
                _confluenceConnector = null;
            }
        }, cancellationToken);

}