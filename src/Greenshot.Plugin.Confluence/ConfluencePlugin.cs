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
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using ToolStripMenuItem = System.Windows.Forms.ToolStripMenuItem;
using Greenshot.Base.Core;
using Dapplo.Ini;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Threading;
using Greenshot.Plugin.Confluence.Forms;
using Greenshot.Base.Languages;

namespace Greenshot.Plugin.Confluence;

/// <summary>
/// This is the ConfluencePlugin base code
/// </summary>
public class ConfluencePlugin : IGreenshotPlugin, IConfigurablePlugin, IRecipeStepProvider
{
    private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ConfluencePlugin));
    private static ConfluenceConnector _confluenceConnector;
    private static IConfluenceConfiguration _config;
    private ToolStripMenuItem _itemPlugInConfig;

    public ValueTask DisposeAsync()
    {
        // The menu item is removed and disposed in StopAsync
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
            _confluenceConnector = new ConfluenceConnector(_config.Url, _config.Timeout);
        }
    }

    public static ConfluenceConnector ConfluenceConnectorNoLogin
    {
        get { return _confluenceConnector; }
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
        if (ConfluenceDestination.IsInitialized)
        {
            services.AddService<IDestination>(new ConfluenceDestination());
        }

        services.AddRecipeStepProvider(this);
        services.AddSettingsView<IConfluenceConfiguration>(config => new ConfluenceConfigurationControl(config));
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
    /// Register the dialog, add the quick link to the context menu (on the UI thread)
    /// </summary>
    public Task StartAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        services.GetService<IDialogViewRegistry>()?.Register<ConfluenceUploadRequest, ConfluenceUploadChoice>(Forms.ConfluenceUpload.Show);
        return services.GetRequiredService<IUiDispatcher>().RunOnUiAsync(Start, cancellationToken);
    }

    private void Start()
    {
        _itemPlugInConfig = new ToolStripMenuItem
        {
            Image = ConfluenceDestination.LoadConfluenceIcon(),
            Text = PluginUtils.GetQuicklinkText("Confluence"),
            Visible = _config?.QuicklinkEnabled ?? false
        };
        _itemPlugInConfig.Click += delegate { ShowSettings(); };

        PluginUtils.AddToContextMenu(_itemPlugInConfig);
        Texts.Config.LanguageChanged += OnLanguageChanged;
        if (_config is INotifyPropertyChanged notify)
        {
            notify.PropertyChanged += OnConfigPropertyChanged;
        }
    }

    private void OnConfigPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IConfluenceConfiguration.QuicklinkEnabled))
        {
            if (_itemPlugInConfig != null)
            {
                _itemPlugInConfig.Visible = _config?.QuicklinkEnabled ?? false;
            }
        }
    }

    public void OnLanguageChanged(object sender, EventArgs e)
    {
        if (_itemPlugInConfig != null)
        {
            _itemPlugInConfig.Text = PluginUtils.GetQuicklinkText("Confluence");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        UiDispatcher.Current.RunOnUiAsync(() =>
        {
            LOG.Debug("Confluence Plugin shutdown.");
            Texts.Config.LanguageChanged -= OnLanguageChanged;
            if (_config is INotifyPropertyChanged notify)
            {
                notify.PropertyChanged -= OnConfigPropertyChanged;
            }

            _itemPlugInConfig?.Dispose();
            _itemPlugInConfig = null;
            if (_confluenceConnector != null)
            {
                _confluenceConnector.Logout();
                _confluenceConnector = null;
            }
        }, cancellationToken);

    /// <summary>
    /// Show the settings of this plugin
    /// </summary>
    private void ShowSettings()
    {
        SimpleServiceProvider.Current.GetInstance<IGreenshotMainForm>(isOptional: true)?.ShowSetting(Name);
    }
}