/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using Greenshot.IniFile;
using Greenshot.Plugin;
using System;
using GreenshotJiraPlugin.Forms;

namespace GreenshotJiraPlugin {
	/// <summary>
	/// This is the JiraPlugin base code
	/// </summary>
	public class JiraPlugin : IGreenshotPlugin {
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(JiraPlugin));
		private PluginAttribute _jiraPluginAttributes;
		private JiraConnector _jiraConnector;
		private JiraConfiguration _config;
		private static JiraPlugin _instance;

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (_jiraConnector != null) {
					_jiraConnector.Dispose();
					_jiraConnector = null;
				}
			}
		}

		public static JiraPlugin Instance {
			get {
				return _instance;
			}
		}

		public JiraPlugin() {
			_instance = this;
		}
		
		public PluginAttribute JiraPluginAttributes {
			get {
				return _jiraPluginAttributes;
			}
		}

		public IEnumerable<IDestination> Destinations() {
			yield return new JiraDestination(this);
		}

		public IEnumerable<IProcessor> Processors() {
			yield break;
		}

		//Needed for a fail-fast
		public JiraConnector CurrentJiraConnector {
			get {
				return _jiraConnector;
			}
		}
		
		public JiraConnector JiraConnector {
			get {
				if (_jiraConnector == null) {
					_jiraConnector = new JiraConnector(true);
				}
				return _jiraConnector;
			}
		}

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="pluginHost">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="myAttributes">My own attributes</param>
		/// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
		public virtual bool Initialize(IGreenshotHost pluginHost, PluginAttribute myAttributes) {
			_jiraPluginAttributes = myAttributes;

			// Register configuration (don't need the configuration itself)
			_config = IniConfig.GetIniSection<JiraConfiguration>();
			new ComponentResourceManager(typeof(JiraPlugin));
			return true;
		}

		public virtual void Shutdown() {
			Log.Debug("Jira Plugin shutdown.");
			if (_jiraConnector != null) {
				_jiraConnector.Logout();
			}
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public virtual void Configure() {
			string url = _config.Url;
			if (ShowConfigDialog()) {
				// check for re-login
				if (_jiraConnector != null && _jiraConnector.IsLoggedIn && !string.IsNullOrEmpty(url)) {
					if (!url.Equals(_config.Url)) {
						_jiraConnector.Logout();
						_jiraConnector.Login();
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

		/// <summary>
		/// This will be called when Greenshot is shutting down
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void Closing(object sender, FormClosingEventArgs e) {
			Log.Debug("Application closing, calling logout of jira!");
			Shutdown();
		}
	}
}
