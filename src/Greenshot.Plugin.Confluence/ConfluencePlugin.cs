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
using System.Windows;
using ToolStripMenuItem = System.Windows.Forms.ToolStripMenuItem;
using Greenshot.Base.Core;
using Dapplo.Ini;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Pipeline;
using Greenshot.Plugin.Confluence.Forms;
using Greenshot.Plugin.Confluence.Support;

namespace Greenshot.Plugin.Confluence;

/// <summary>
/// This is the ConfluencePlugin base code
/// </summary>
public class ConfluencePlugin : IGreenshotPlugin, IRecipeStepProvider
{
    private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ConfluencePlugin));
    private static ConfluenceConnector _confluenceConnector;
    private static IConfluenceConfiguration _config;
    private ToolStripMenuItem _itemPlugInConfig;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing) return;
        if (_itemPlugInConfig != null)
        {
            _itemPlugInConfig.Dispose();
            _itemPlugInConfig = null;
        }
    }

    /// <summary>
    /// Name of the plugin
    /// </summary>
    public string Name => "Confluence";

    /// <summary>
    /// Specifies if the plugin can be configured
    /// </summary>
    public bool IsConfigurable => true;

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

    public static ConfluenceConnector ConfluenceConnector
    {
        get
        {
            if (_confluenceConnector == null)
            {
                CreateConfluenceConnector();
            }

            try
            {
                if (_confluenceConnector != null && !_confluenceConnector.IsLoggedIn)
                {
                    _confluenceConnector.Login();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(Language.GetFormattedString("confluence", LangKey.login_error, e.Message));
            }

            return _confluenceConnector;
        }
    }

    /// <summary>
    /// Implementation of RegisterConfiguration phase: register INI section before file is loaded.
    /// </summary>
    public void RegisterConfiguration(IniConfig iniConfig)
    {
        var section = new ConfluenceConfigurationImpl();
        iniConfig.AddSection(section);
        _config = section;
    }

    /// <summary>
    /// Implementation of RegisterServices phase: register DI services after config is loaded.
    /// </summary>
    public void RegisterServices(IServiceLocator serviceLocator)
    {
        try
        {
            TranslationManager.Instance.TranslationProvider = new LanguageXMLTranslationProvider();
        }
        catch (Exception ex)
        {
            LOG.ErrorFormat("Problem registering Confluence services: {0}", ex.Message);
        }

        serviceLocator.AddService<IRecipeStepProvider>(this);
        StepRegistry.Instance.RegisterProvider(this);
    }

    /// <summary>
    /// Registers recipe step factories provided by the Confluence plugin.
    /// </summary>
    /// <param name="registry">The step registry.</param>
    public void RegisterSteps(IStepRegistry registry)
    {
        if (registry == null) return;
        registry.RegisterStepFactory("Confluence", config => new ConfluenceStep(config));
        registry.RegisterStepFactory("ConfluenceUpload", config => new ConfluenceStep(config));
        registry.RegisterStepFactory("UploadToConfluence", config => new ConfluenceStep(config));
    }

    /// <summary>
    /// Implementation of the IGreenshotPlugin.Start
    /// </summary>
    public bool Start()
    {
        if (ConfluenceDestination.IsInitialized)
        {
            SimpleServiceProvider.Current.AddService<IDestination>(new ConfluenceDestination());
        }

        _itemPlugInConfig = new ToolStripMenuItem
        {
            Image = ConfluenceDestination.LoadConfluenceIcon(),
            Text = PluginUtils.GetQuicklinkText("Confluence"),
            Visible = _config?.QuicklinkEnabled ?? false
        };
        _itemPlugInConfig.Click += delegate { Configure(); };

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

    public void Shutdown()
    {
        LOG.Debug("Confluence Plugin shutdown.");
        Language.LanguageChanged -= OnLanguageChanged;
        if (_config is INotifyPropertyChanged notify)
        {
            notify.PropertyChanged -= OnConfigPropertyChanged;
        }
        if (_confluenceConnector != null)
        {
            _confluenceConnector.Logout();
            _confluenceConnector = null;
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

    public UIElement CreateConfigurationControl()
    {
        return _config != null ? new ConfluenceConfigurationControl(_config) : null;
    }
}