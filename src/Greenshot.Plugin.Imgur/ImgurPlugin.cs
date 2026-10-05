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
using System.Windows.Forms;
using Greenshot.Base.Core;
using Dapplo.Ini;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Threading;
using Greenshot.Plugin.Imgur.Forms;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Languages;

namespace Greenshot.Plugin.Imgur;

/// <summary>
/// This is the ImgurPlugin code
/// </summary>
public class ImgurPlugin : IGreenshotPlugin, IConfigurablePlugin, IRecipeStepProvider
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ImgurPlugin));
    private static IImgurConfiguration _config;
    private ToolStripMenuItem _historyMenuItem;
    private ToolStripMenuItem _itemPlugInConfig;

    public ValueTask DisposeAsync()
    {
        // The menu items are removed and disposed in StopAsync
        return default;
    }

    /// <summary>
    /// Name of the plugin
    /// </summary>
    public string Name => "Imgur";

    public void ConfigureServices(IPluginServices services)
    {
        Texts.Register<IImgurLanguage>(new ImgurLanguageImpl());
        var section = new ImgurConfigurationImpl();
        services.AddConfiguration(section);
        _config = section;

        services.AddService<IIconProvider>(ImgurDestination.Icons);
        services.AddService<IDestination>(new ImgurDestination());
        services.AddRecipeStepProvider(this);
        services.AddSettingsView<IImgurConfiguration>(config => new Forms.ImgurConfigurationControl(config));
    }

    public object CreateSettingsViewModel(IServiceProvider services) => _config;

    /// <summary>
    /// Registers recipe step factories provided by the Imgur plugin.
    /// </summary>
    /// <param name="registry">The step registry.</param>
    public void RegisterSteps(IStepRegistry registry)
    {
        if (registry == null) return;
        registry.Register<ImgurStep>(config => new ImgurStep(config));
    }

    /// <summary>
    /// Add the quick link to the context menu (on the UI thread)
    /// </summary>
    public Task StartAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        services.GetRequiredService<IUiDispatcher>().RunOnUiAsync(Start, cancellationToken);

    private void Start()
    {
        _itemPlugInConfig = new ToolStripMenuItem(PluginUtils.GetQuicklinkText("Imgur"))
        {
            Image = EmbeddedResources.GetImage(typeof(ImgurPlugin), "Imgur"),
            Visible = _config?.QuicklinkEnabled ?? false
        };
        _itemPlugInConfig.Click += delegate { ShowSettings(); };

        PluginUtils.AddToContextMenu(_itemPlugInConfig);
        Texts.Config.LanguageChanged += OnLanguageChanged;
        if (_config is INotifyPropertyChanged notify)
        {
            notify.PropertyChanged += OnConfigPropertyChanged;
        }

        UpdateHistoryMenuItem();
    }

    private void OnConfigPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IImgurConfiguration.QuicklinkEnabled))
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
            _itemPlugInConfig.Text = PluginUtils.GetQuicklinkText("Imgur");
        }

        if (_historyMenuItem != null)
        {
            _historyMenuItem.Text = Texts.Get<IImgurLanguage>().History;
        }
    }

    private void UpdateHistoryMenuItem()
    {
        if (_historyMenuItem == null)
        {
            return;
        }

        try
        {
            UiDispatcher.Current.InvokeAsync(() =>
            {
                var historyMenuItem = _historyMenuItem;
                if (historyMenuItem == null)
                {
                    return;
                }

                if (_config?.ImgurUploadHistory != null && _config.ImgurUploadHistory.Count > 0)
                {
                    historyMenuItem.Enabled = true;
                }
                else
                {
                    historyMenuItem.Enabled = false;
                }
            }).FireAndLog("Update the Imgur history menu item", Log);
        }
        catch (Exception ex)
        {
            Log.Error("Error loading history", ex);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        UiDispatcher.Current.RunOnUiAsync(() =>
        {
            Log.Debug("Imgur Plugin shutdown.");
            Texts.Config.LanguageChanged -= OnLanguageChanged;
            if (_config is INotifyPropertyChanged notify)
            {
                notify.PropertyChanged -= OnConfigPropertyChanged;
            }

            _historyMenuItem?.Dispose();
            _historyMenuItem = null;
            _itemPlugInConfig?.Dispose();
            _itemPlugInConfig = null;
        }, cancellationToken);

    /// <summary>
    /// Show the settings of this plugin
    /// </summary>
    private void ShowSettings()
    {
        SimpleServiceProvider.Current.GetInstance<IGreenshotMainForm>(isOptional: true)?.ShowSetting(Name);
    }
}