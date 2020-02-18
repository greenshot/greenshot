/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.Windows.Forms;
using Greenshot.IniFile;
using Greenshot.Plugin;
using System;
using System.Threading.Tasks;
using Dapplo.Log;
using GreenshotJiraPlugin.Forms;
using GreenshotPlugin.Core;
using log4net;

namespace GreenshotJiraPlugin {
	/// <summary>
	/// This is the JiraPlugin base code
	/// </summary>
    [Plugin("Jira", true)]
	public class JiraPlugin : IGreenshotPlugin {
		private static readonly ILog Log = LogManager.GetLogger(typeof(JiraPlugin));
		private JiraConfiguration _config;

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected void Dispose(bool disposing) {
			if (disposing)
            {
                var jiraConnector = SimpleServiceProvider.Current.GetInstance<JiraConnector>();
                jiraConnector?.Dispose();
			}
		}

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
		public bool Initialize() {
			// Register configuration (don't need the configuration itself)
			_config = IniConfig.GetIniSection<JiraConfiguration>();

			// Provide the JiraConnector
			SimpleServiceProvider.Current.AddService(new JiraConnector());
			// Provide the IDestination
			SimpleServiceProvider.Current.AddService(new JiraDestination());

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

			// Add a SVG converter, although it doesn't fit to the Jira plugin there is currently no other way
			ImageHelper.StreamConverters["svg"] = (stream, s) =>
			{
				stream.Position = 0;
				try
				{
					return SvgImage.FromStream(stream).Image;
				}
				catch (Exception ex)
				{
					Log.Error("Can't load SVG", ex);
				}
				return null;
			};

			return true;
		}

		public void Shutdown() {
			Log.Debug("Jira Plugin shutdown.");
            var jiraConnector = SimpleServiceProvider.Current.GetInstance<JiraConnector>();
            jiraConnector?.Logout();
        }

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure() {
			string url = _config.Url;
			if (ShowConfigDialog()) {
				// check for re-login
                var jiraConnector = SimpleServiceProvider.Current.GetInstance<JiraConnector>();
				if (jiraConnector != null && jiraConnector.IsLoggedIn && !string.IsNullOrEmpty(url)) {
					if (!url.Equals(_config.Url)) {
                        jiraConnector.Logout();
						Task.Run(async () =>
						{
							await jiraConnector.LoginAsync();
						});
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
