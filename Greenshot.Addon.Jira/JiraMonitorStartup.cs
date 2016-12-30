//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Addons;
using Dapplo.Utils;
using Greenshot.Addon.Core;
using Greenshot.Addon.Jira.Forms;
using Greenshot.Core.Interfaces;

#endregion

namespace Greenshot.Addon.Jira
{
	/// <summary>
	///     This starts the jira monitor at startup
	/// </summary>
	[StartupAction(StartupOrder = (int) GreenshotStartupOrder.Addon)]
	public class JiraMonitorStartup : IAsyncStartupAction
	{
		private JiraDestination _jiraDestination;

		[Import]
		private IJiraConfiguration JiraConfiguration { get; set; }

		[Import]
		private IJiraLanguage JiraLanguage { get; set; }

		/// <summary>
		///     Get the JiraMonitor
		/// </summary>
		public JiraMonitor JiraMonitor { get; private set; }

		[Import]
		private IServiceExporter ServiceExporter { get; set; }

		[Import]
		private IServiceLocator ServiceLocator { get; set; }

		/// <summary>
		///     Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
			if (ShowConfigDialog())
			{
				UiContext.RunOn(async () => await InitializeMonitor());
			}
		}

		/// <summary>
		///     Initialize
		/// </summary>
		/// <param name="token"></param>
		public Task StartAsync(CancellationToken token = new CancellationToken())
		{
			// Make sure the InitializeMonitor is called after the message loop is initialized!
			return UiContext.RunOn(async () => { await InitializeMonitor(); }, token);
		}

		private async Task InitializeMonitor()
		{
			JiraMonitor?.Dispose();
			if (!string.IsNullOrEmpty(JiraConfiguration.Password))
			{
				JiraMonitor = new JiraMonitor();
				// Async call, will continue in the background!
				await JiraMonitor.AddJiraInstanceAsync(new Uri(JiraConfiguration.RestUrl.TrimEnd('/')), JiraConfiguration.Username, JiraConfiguration.Password);
				if (_jiraDestination == null)
				{
					_jiraDestination = new JiraDestination();
					ServiceLocator.FillImports(_jiraDestination);
					ServiceExporter.Export<IDestination>(_jiraDestination);
				}
				_jiraDestination.Monitor = JiraMonitor;
			}
		}

		/// <summary>
		///     A form for username/password
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

		private bool _disposedValue; // To detect redundant calls

		protected void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					if (JiraMonitor != null)
					{
						JiraMonitor.Dispose();
						JiraMonitor = null;
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