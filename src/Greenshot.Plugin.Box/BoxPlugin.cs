/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Dapplo.Ini;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Threading;
using Greenshot.Plugin.Box.Api;
using Greenshot.Plugin.Box.Destinations;
using Greenshot.Base.Languages;
using Greenshot.Plugin.Box.Recipes;
using Greenshot.Plugin.Box.Views;

namespace Greenshot.Plugin.Box;

/// <summary>
/// This is the Box base code
/// </summary>
public class BoxPlugin : IGreenshotPlugin, IConfigurablePlugin, IRecipeStepProvider
{
    private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(BoxPlugin));
    private static IBoxConfiguration _config;
    private TrayMenuEntry _itemPlugInConfig;

    public ValueTask DisposeAsync()
    {
        // The menu item is removed and disposed in StopAsync
        return default;
    }

    /// <summary>
    /// Name of the plugin
    /// </summary>
    public string Name => "Box";

    public void ConfigureServices(IPluginServices services)
    {
        Texts.Register<IBoxLanguage>(new BoxLanguageImpl());
        var section = new BoxConfigurationImpl();
        services.AddConfiguration(section);
        _config = section;

        services.AddService<IIconProvider>(BoxDestination.Icons);
        services.AddService<IDestination>(new BoxDestination(this));
        services.AddRecipeStepProvider(this);
        services.AddSettingsView<IBoxConfiguration>(config => new BoxConfigurationView(config));
    }

    public object CreateSettingsViewModel(IServiceProvider services) => _config;

    /// <summary>
    /// Registers recipe step factories provided by the Box plugin.
    /// </summary>
    /// <param name="registry">The step registry.</param>
    public void RegisterSteps(IStepRegistry registry)
    {
        if (registry == null) return;
        registry.Register<BoxStep>(config => new BoxStep(config, this));
    }

    /// <summary>
    /// Add the quick link to the context menu (on the UI thread)
    /// </summary>
    public Task StartAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        services.GetRequiredService<IUiDispatcher>().RunOnUiAsync(Start, cancellationToken);

    private void Start()
    {
        _itemPlugInConfig = new TrayMenuEntry
        {
            Image = EmbeddedResources.GetImage(typeof(BoxPlugin), "Box"),
            Text = PluginUtils.GetQuicklinkText("Box"),
            Visible = _config?.QuicklinkEnabled ?? false
        };
        _itemPlugInConfig.Click += ConfigMenuClick;

        PluginUtils.AddToContextMenu(_itemPlugInConfig);
        Texts.Config.LanguageChanged += OnLanguageChanged;
        if (_config is INotifyPropertyChanged notify)
        {
            notify.PropertyChanged += OnConfigPropertyChanged;
        }
    }

    private void OnConfigPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IBoxConfiguration.QuicklinkEnabled))
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
            _itemPlugInConfig.Text = PluginUtils.GetQuicklinkText("Box");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        UiDispatcher.Current.RunOnUiAsync(() =>
        {
            LOG.Debug("Box Plugin shutdown.");
            Texts.Config.LanguageChanged -= OnLanguageChanged;
            if (_config is INotifyPropertyChanged notify)
            {
                notify.PropertyChanged -= OnConfigPropertyChanged;
            }

            _itemPlugInConfig?.Dispose();
            _itemPlugInConfig = null;
        }, cancellationToken);

    private void ConfigMenuClick(object sender, EventArgs eventArgs)
    {
        // Show the settings of this plugin
        SimpleServiceProvider.Current.GetInstance<IGreenshotShell>(isOptional: true)?.ShowSetting(Name);
    }

    /// <summary>
    /// Upload the capture to Box, shows the progress to the user.
    /// </summary>
    /// <returns>the url of the upload, null when the user didn't authorize</returns>
    public async Task<string> UploadAsync(IExportSource source, ICaptureDetails captureDetails, IUserInteraction userInteraction, CancellationToken cancellationToken)
    {
        var outputSettings = new SurfaceOutputSettings(_config.UploadFormat, _config.UploadJpegQuality, false);
        string filename = Path.GetFileName(FilenameHelper.GetFilename(_config.UploadFormat, captureDetails));
        var image = await source.EncodeAsync(outputSettings, cancellationToken).ConfigureAwait(false);

        string url = await userInteraction.RunWithProgressAsync(Texts.Get<IBoxLanguage>().CommunicationWait,
            (progress, token) => BoxUtils.UploadToBoxAsync(image, filename, userInteraction, progress, token), cancellationToken).ConfigureAwait(false);

        if (url != null && _config.AfterUploadLinkToClipBoard)
        {
            await ClipboardService.Current.SetTextAsync(url, cancellationToken).ConfigureAwait(false);
        }

        return url;
    }
}
