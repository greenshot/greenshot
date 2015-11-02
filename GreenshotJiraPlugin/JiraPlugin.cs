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

using GreenshotPlugin.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Addons;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;

namespace GreenshotJiraPlugin
{
	/// <summary>
	/// This is the JiraPlugin base code
	/// </summary>
	[Plugin("Jira", Configurable = true)]
	[StartupAction]
	public class JiraPlugin : IConfigurablePlugin, IStartupAction
	{
		private JiraMonitor _jiraMonitor;

		[Import]
		public IGreenshotHost GreenshotHost
		{
			get;
			set;
		}

		[Import]
		public IJiraConfiguration JiraConfiguration
		{
			get;
			set;
		}

		[Import]
		public IJiraLanguage JiraLanguage
		{
			get;
			set;
		}

		/// <summary>
		/// Get the JiraMonitor
		/// </summary>
		public JiraMonitor JiraMonitor
		{
			get
			{
				return _jiraMonitor;
			}
		}

		public IEnumerable<ILegacyDestination> Destinations()
		{
			yield return new JiraLegacyDestination(this);
		}

		/// <summary>
		/// Initialize
		/// </summary>
		/// <param name="token"></param>
		public Task StartAsync(CancellationToken token = new CancellationToken())
		{
			// Make sure the InitializeMonitor is called after the message loop is initialized!
			GreenshotHost.GreenshotForm.AsyncInvoke(InitializeMonitor);
			return Task.FromResult(true);
		}

		private void InitializeMonitor()
		{
			_jiraMonitor?.Dispose();
			if (!string.IsNullOrEmpty(JiraConfiguration.Password))
			{
				_jiraMonitor = new JiraMonitor();
				// Async call, will continue in the background!
				var backgroundTask = _jiraMonitor.AddJiraInstance(new Uri(JiraConfiguration.RestUrl), JiraConfiguration.Username, JiraConfiguration.Password).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
			if (ShowConfigDialog())
			{
				InitializeMonitor();
			}
		}

		/// <summary>
		/// A form for username/password
		/// </summary>
		/// <returns>bool true if OK was pressed, false if cancel</returns>
		private bool ShowConfigDialog()
		{
			var before = new
			{
				JiraConfiguration.RestUrl, JiraConfiguration.Password, JiraConfiguration.Username
			};

			var settingsForm = new SettingsForm(JiraConfiguration);
			DialogResult result = settingsForm.ShowDialog();
			if (result == DialogResult.OK)
			{
				var after = new
				{
					JiraConfiguration.RestUrl, JiraConfiguration.Password, JiraConfiguration.Username
				};
				return !before.Equals(after);
			}
			return false;
		}

		#region IDisposable Support

		private bool _disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					if (_jiraMonitor != null)
					{
						_jiraMonitor.Dispose();
						_jiraMonitor = null;
					}
				}

				_disposedValue = true;
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