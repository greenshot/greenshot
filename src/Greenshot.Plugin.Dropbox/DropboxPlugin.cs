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
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Plugin.Dropbox.Forms;

namespace Greenshot.Plugin.Dropbox;

/// <summary>
/// This is the Dropbox base code
/// </summary>
public class DropboxPlugin : IGreenshotPlugin, IRecipeStepProvider
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(DropboxPlugin));
    private static IDropboxConfiguration _config;
    private ComponentResourceManager _resources;
    private ToolStripMenuItem _itemPlugInConfig;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing) return;
        if (_itemPlugInConfig == null) return;
        _itemPlugInConfig.Dispose();
        _itemPlugInConfig = null;
    }

    /// <summary>
    /// Name of the plugin
    /// </summary>
    public string Name => "Dropbox";

    /// <summary>
    /// Specifies if the plugin can be configured
    /// </summary>
    public bool IsConfigurable => true;

    /// <summary>
    /// Implementation of RegisterConfiguration phase: register INI section before file is loaded.
    /// </summary>
    public void RegisterConfiguration(IniConfig iniConfig)
    {
        var section = new DropboxConfigurationImpl();
        iniConfig.AddSection(section);
        _config = section;
    }

    /// <summary>
    /// Implementation of RegisterServices phase: register DI services after config is loaded.
    /// </summary>
    public void RegisterServices(IServiceLocator serviceLocator)
    {
        _resources = new ComponentResourceManager(typeof(DropboxPlugin));
        serviceLocator.AddService<IIconProvider>(DropboxDestination.Icons);
        serviceLocator.AddService<IDestination>(new DropboxDestination(this));
        if (RecipeConfigHelper.IsRecipeFeatureEnabled())
        {
            serviceLocator.AddService<IRecipeStepProvider>(this);
            StepRegistry.Instance.RegisterProvider(this);
        }
    }

    /// <summary>
    /// Registers recipe step factories provided by the Dropbox plugin.
    /// </summary>
    /// <param name="registry">The step registry.</param>
    public void RegisterSteps(IStepRegistry registry)
    {
        if (registry == null) return;
        registry.Register<DropboxStep>(config => new DropboxStep(config, this));
    }

    /// <summary>
    /// Implementation of the IGreenshotPlugin.Start
    /// </summary>
    public bool Start()
    {
        _itemPlugInConfig = new ToolStripMenuItem
        {
            Text = PluginUtils.GetQuicklinkText("Dropbox"),
            Image = (Image) _resources.GetObject("Dropbox"),
            Visible = _config?.QuicklinkEnabled ?? false
        };
        _itemPlugInConfig.Click += ConfigMenuClick;

        PluginUtils.AddToContextMenu(_itemPlugInConfig);
        Language.LanguageChanged += OnLanguageChanged;
        if (_config is INotifyPropertyChanged notify)
        {
            notify.PropertyChanged += OnConfigPropertyChanged;
        }
        return true;
    }

    private void OnConfigPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IDropboxConfiguration.QuicklinkEnabled))
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
            _itemPlugInConfig.Text = PluginUtils.GetQuicklinkText("Dropbox");
        }
    }

    public void Shutdown()
    {
        Log.Debug("Dropbox Plugin shutdown.");
        Language.LanguageChanged -= OnLanguageChanged;
        if (_config is INotifyPropertyChanged notify)
        {
            notify.PropertyChanged -= OnConfigPropertyChanged;
        }
    }

    /// <summary>
    /// Implementation of the IPlugin.Configure
    /// </summary>
    public void Configure()
    {
        var mainForm = SimpleServiceProvider.Current.GetInstance<IGreenshotMainForm>(isOptional: true);
        mainForm?.ShowSetting(Name);
    }

    public System.Windows.UIElement CreateConfigurationControl()
    {
        return new Forms.DropboxConfigurationControl(_config);
    }

    public void ConfigMenuClick(object sender, EventArgs eventArgs)
    {
        Configure();
    }

    /// <summary>
    /// Upload the capture to Dropbox, shows the progress to the user.
    /// </summary>
    /// <returns>true when uploaded, false when Dropbox didn't accept it, null when the user didn't authorize</returns>
    public async Task<bool?> UploadAsync(IExportSource source, ICaptureDetails captureDetails, IUserInteraction userInteraction, CancellationToken cancellationToken)
    {
        var outputSettings = new SurfaceOutputSettings(_config.UploadFormat, _config.UploadJpegQuality, false);
        string filename = Path.GetFileName(FilenameHelper.GetFilename(_config.UploadFormat, captureDetails));
        var image = await source.EncodeAsync(outputSettings, cancellationToken).ConfigureAwait(false);
        return await userInteraction.RunWithProgressAsync(Language.GetString("dropbox", LangKey.communication_wait),
            (progress, token) => DropboxUtils.UploadToDropboxAsync(image, filename, userInteraction, progress, token), cancellationToken).ConfigureAwait(false);
    }
}
