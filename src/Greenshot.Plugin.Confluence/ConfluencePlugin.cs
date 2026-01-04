/*
 * Greenshot - a free and open source screenshot tool
 * Copyright © 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Plugin.Confluence.Forms;
using Greenshot.Plugin.Confluence.Support;

namespace Greenshot.Plugin.Confluence
{
    /// <summary>
    /// This is the ConfluencePlugin base code
    /// </summary>
    public class ConfluencePlugin : IGreenshotPlugin
    {
        private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ConfluencePlugin));
        private static ConfluenceConnector _confluenceConnector;
        private static ConfluenceConfiguration _config;

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
                if (_config.Url.Contains("soap-axis"))
                {
                    _confluenceConnector = new ConfluenceConnector(_config.Url, _config.Timeout);
                }
                else
                {
                    _confluenceConnector = new ConfluenceConnector(_config.Url + ConfluenceConfiguration.DEFAULT_POSTFIX2, _config.Timeout);
                }
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
        /// Implementation of the IGreenshotPlugin.Initialize
        /// </summary>
        public bool Initialize()
        {
            // Register configuration (don't need the configuration itself)
            _config = IniConfig.GetIniSection<ConfluenceConfiguration>();
            if (_config.IsDirty)
            {
                IniConfig.Save();
            }

            try
            {
                TranslationManager.Instance.TranslationProvider = new LanguageXMLTranslationProvider();
                //resources = new ComponentResourceManager(typeof(ConfluencePlugin));
            }
            catch (Exception ex)
            {
                LOG.ErrorFormat("Problem in ConfluencePlugin.Initialize: {0}", ex.Message);
                return false;
            }

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
            ConfluenceConfiguration clonedConfig = _config.Clone();
            ConfluenceConfigurationForm configForm = new ConfluenceConfigurationForm(clonedConfig);
            string url = _config.Url;
            bool? dialogResult = configForm.ShowDialog();
            if (dialogResult.HasValue && dialogResult.Value)
            {
                // copy the new object to the old...
                clonedConfig.CloneTo(_config);
                IniConfig.Save();
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
        }
    }
}