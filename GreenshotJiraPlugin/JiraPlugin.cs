/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
using Jira;
using System;

namespace GreenshotJiraPlugin {
	/// <summary>
	/// This is the JiraPlugin base code
	/// </summary>
	public class JiraPlugin : IGreenshotPlugin {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(JiraPlugin));
		private PluginAttribute jiraPluginAttributes;
		private IGreenshotHost host;
		private JiraConnector jiraConnector = null;
		private JiraConfiguration config = null;
		private ComponentResourceManager resources;
		private static JiraPlugin instance = null;

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (jiraConnector != null) {
					jiraConnector.Dispose();
					jiraConnector = null;
				}
			}
		}

		public static JiraPlugin Instance {
			get {
				return instance;
			}
		}

		public JiraPlugin() {
			instance = this;
		}
		
		public PluginAttribute JiraPluginAttributes {
			get {
				return jiraPluginAttributes;
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
				return jiraConnector;
			}
		}
		
		public JiraConnector JiraConnector {
			get {
				if (jiraConnector == null) {
					jiraConnector = new JiraConnector(true);
				}
				return jiraConnector;
			}
		}

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="host">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="captureHost">Use the ICaptureHost interface to register in the MainContextMenu</param>
		/// <param name="pluginAttribute">My own attributes</param>
		/// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
		public virtual bool Initialize(IGreenshotHost pluginHost, PluginAttribute myAttributes) {
			this.host = (IGreenshotHost)pluginHost;
			jiraPluginAttributes = myAttributes;

			// Register configuration (don't need the configuration itself)
			config = IniConfig.GetIniSection<JiraConfiguration>();
			resources = new ComponentResourceManager(typeof(JiraPlugin));
			return true;
		}

		public virtual void Shutdown() {
			LOG.Debug("Jira Plugin shutdown.");
			if (jiraConnector != null) {
				jiraConnector.logout();
			}
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public virtual void Configure() {
			string url = config.Url;
			if (config.ShowConfigDialog()) {
				// check for re-login
				if (jiraConnector != null && jiraConnector.isLoggedIn && !string.IsNullOrEmpty(url)) {
					if (!url.Equals(config.Url)) {
						jiraConnector.logout();
						jiraConnector.login();
					}
				}
			}
		}

		/// <summary>
		/// This will be called when Greenshot is shutting down
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void Closing(object sender, FormClosingEventArgs e) {
			LOG.Debug("Application closing, calling logout of jira!");
			Shutdown();
		}
	}
}
