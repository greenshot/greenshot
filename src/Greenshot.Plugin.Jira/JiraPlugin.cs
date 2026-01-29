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
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.WinForms.ContentConverter;
using Dapplo.Jira.SvgWinForms.Converters;
using Dapplo.Log;
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Plugin.Jira.Forms;
using log4net;

namespace Greenshot.Plugin.Jira
{
    /// <summary>
    /// This is the JiraPlugin base code
    /// </summary>
    public class JiraPlugin : IGreenshotPlugin
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(JiraPlugin));
        private JiraConfiguration _config;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;
            var jiraConnector = SimpleServiceProvider.Current.GetInstance<JiraConnector>();
            jiraConnector?.Dispose();
        }

        /// <summary>
        /// Name of the plugin
        /// </summary>
        public string Name => "Jira";

        /// <summary>
        /// Specifies if the plugin can be configured
        /// </summary>
        public bool IsConfigurable => true;

        /// <summary>
        /// Implementation of the IGreenshotPlugin.Initialize
        /// </summary>
        /// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
        public bool Initialize()
        {
            // Register configuration (don't need the configuration itself)
            _config = IniConfig.GetIniSection<JiraConfiguration>();

            // Provide the JiraConnector
            SimpleServiceProvider.Current.AddService(new JiraConnector());
            // Provide the IDestination
            SimpleServiceProvider.Current.AddService<IDestination>(new JiraDestination());

            if (HttpExtensionsGlobals.HttpContentConverters.All(x => x.GetType() != typeof(SvgBitmapHttpContentConverter)))
            {
                HttpExtensionsGlobals.HttpContentConverters.Add(SvgBitmapHttpContentConverter.Instance.Value);
            }
            BitmapHttpContentConverter.RegisterGlobally();

            // Make sure the log is enabled for the correct level.
            if (Log.IsDebugEnabled)
            {
                LogSettings.RegisterDefaultLogger<Log4NetLogger>(LogLevels.Verbose);
            }
            else if (Log.IsInfoEnabled)
            {
                LogSettings.RegisterDefaultLogger<Log4NetLogger>(LogLevels.Info);
            }
            else if (Log.IsWarnEnabled)
            {
                LogSettings.RegisterDefaultLogger<Log4NetLogger>(LogLevels.Warn);
            }
            else if (Log.IsErrorEnabled)
            {
                LogSettings.RegisterDefaultLogger<Log4NetLogger>(LogLevels.Error);
            }
            else if (Log.IsErrorEnabled)
            {
                LogSettings.RegisterDefaultLogger<Log4NetLogger>(LogLevels.Error);
            }
            else
            {
                LogSettings.RegisterDefaultLogger<Log4NetLogger>(LogLevels.Fatal);
            }

            return true;
        }

        public void Shutdown()
        {
            Log.Debug("Jira Plugin shutdown.");
            var jiraConnector = SimpleServiceProvider.Current.GetInstance<JiraConnector>();
            jiraConnector?.Logout();
        }

        /// <summary>
        /// Implementation of the IPlugin.Configure
        /// </summary>
        public void Configure()
        {
            string url = _config.Url;
            if (ShowConfigDialog())
            {
                // check for re-login
                var jiraConnector = SimpleServiceProvider.Current.GetInstance<JiraConnector>();
                if (jiraConnector != null && jiraConnector.IsLoggedIn && !string.IsNullOrEmpty(url))
                {
                    if (!url.Equals(_config.Url))
                    {
                        jiraConnector.Logout();
                        Task.Run(async () => { await jiraConnector.LoginAsync(); });
                    }
                }
            }
        }

        /// <summary>
        /// A form for username/password
        /// </summary>
        /// <returns>bool true if OK was pressed, false if cancel</returns>
        private bool ShowConfigDialog()
        {
            var settingsForm = new SettingsForm();
            var result = settingsForm.ShowDialog();
            if (result == DialogResult.OK)
            {
                return true;
            }

            return false;
        }
    }
}