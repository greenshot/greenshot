/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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
using System;

namespace GreenshotJiraPlugin {
	/// <summary>
	/// This is the JiraPlugin base code
	/// </summary>
	public class JiraPlugin : IGreenshotPlugin {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(JiraPlugin));
		private PluginAttribute jiraPluginAttributes;
		private IGreenshotHost _host;
		private JiraConfiguration config = null;
		private ComponentResourceManager resources;
		private JiraMonitor _jiraMonitor;

		/// <summary>
		/// Get the JiraMonitor
		/// </summary>
		public JiraMonitor JiraMonitor {
			get {
				return _jiraMonitor;
			}
		}

		public IEnumerable<IDestination> Destinations() {
			yield return new JiraDestination(this);
		}

		public IEnumerable<IProcessor> Processors() {
			yield break;
		}


		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="host">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="captureHost">Use the ICaptureHost interface to register in the MainContextMenu</param>
		/// <param name="pluginAttribute">My own attributes</param>
		/// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
		public bool Initialize(IGreenshotHost pluginHost, PluginAttribute myAttributes) {
			_host = (IGreenshotHost)pluginHost;
			jiraPluginAttributes = myAttributes;

			// Register configuration (don't need the configuration itself)
			config = IniConfig.GetIniSection<JiraConfiguration>();
			resources = new ComponentResourceManager(typeof(JiraPlugin));
			InitializeMonitor();
			return true;
		}

		public void InitializeMonitor()
		{
			if (_jiraMonitor != null)
			{
				_jiraMonitor.Dispose();
			}
			if (!string.IsNullOrEmpty(config.Password))
			{
				_jiraMonitor = new JiraMonitor();
				_jiraMonitor.AddJiraInstance(config.RestUrl, config.Username, config.Password);
			}
		}

		public void Shutdown() {
			LOG.Debug("Jira Plugin shutdown.");
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure() {
			if (config.ShowConfigDialog())
			{
				InitializeMonitor();
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

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					if (_jiraMonitor != null)
					{
						_jiraMonitor.Dispose();
						_jiraMonitor = null;
					}
				}

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~JiraPlugin() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion

	}
}
