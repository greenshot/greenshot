/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Windows;
using Greenshot.Base.Core;
using Dapplo.Ini;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Plugin.Confluence.Forms;
using Greenshot.Plugin.Confluence.Support;

namespace Greenshot.Plugin.Confluence;

/// <summary>
/// This is the ConfluencePlugin base code
/// </summary>
public class ConfluencePlugin : IGreenshotPlugin
{
    private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ConfluencePlugin));
    private static ConfluenceConnector _confluenceConnector;
    private static IConfluenceConfiguration _config;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        //if (disposing) {}
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

        return true;
    }

    public void Shutdown()
    {
        LOG.Debug("Confluence Plugin shutdown.");
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
        ConfluenceConfigurationForm configForm = new ConfluenceConfigurationForm(_config);
        string url = _config.Url;
        bool? dialogResult = configForm.ShowDialog();
        if (dialogResult.HasValue && dialogResult.Value)
        {
            if (_confluenceConnector != null)
            {
                if (!url.Equals(_config.Url))
                {
                    if (_confluenceConnector.IsLoggedIn)
                    {
                        _confluenceConnector.Logout();
                    }

                    _confluenceConnector = null;
                }
            }
        }
        else
        {
            // User cancelled — reload to discard any changes made by the form binding.
            IniConfigRegistry.Get().Reload();
        }
    }
}