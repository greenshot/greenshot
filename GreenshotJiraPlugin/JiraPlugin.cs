#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Windows.Dpi;
using GreenshotJiraPlugin.Forms;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;
using Dapplo.Log;

#endregion

namespace GreenshotJiraPlugin
{
    /// <summary>
    ///     This is the JiraPlugin base code
    /// </summary>
    [Export(typeof(IGreenshotPlugin))]
    public sealed class JiraPlugin : IGreenshotPlugin
	{
	    private readonly IGreenshotHost _greenshotHost;
	    private readonly ICoreConfiguration _coreConfiguration;
	    private readonly IJiraConfiguration _jiraConfiguration;
	    private static readonly LogSource Log = new LogSource();

		private JiraConnector _jiraConnector;

        [ImportingConstructor]
		public JiraPlugin(IGreenshotHost greenshotHost, ICoreConfiguration coreConfiguration, IJiraConfiguration jiraConfiguration)
        {
            _greenshotHost = greenshotHost;
            _coreConfiguration = coreConfiguration;
            _jiraConfiguration = jiraConfiguration;
            Instance = this;
        }

		public static JiraPlugin Instance { get; private set; }

		//Needed for a fail-fast
		public JiraConnector CurrentJiraConnector => JiraConnector;

		public JiraConnector JiraConnector
		{
			get
			{
				lock (Instance)
				{
					if (_jiraConnector == null)
					{
						JiraConnector = new JiraConnector();
					}
				}
				return _jiraConnector;
			}
			private set { _jiraConnector = value; }
		}

		public void Dispose()
		{
			Dispose(true);
		}

		public IEnumerable<IDestination> Destinations()
		{
			yield return new JiraDestination(this);
		}

		public IEnumerable<IProcessor> Processors()
		{
			yield break;
		}

		/// <summary>
		///     Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
		public bool Initialize()
		{
			_greenshotHost.ContextMenuDpiHandler.OnDpiChanged.Subscribe(dpi =>
			{
				var size = DpiHandler.ScaleWithDpi(_coreConfiguration.IconSize.Width, dpi);

				JiraConnector.UpdateSvgSize(size);
			});

			return true;
		}

		public void Shutdown()
		{
			Log.Debug().WriteLine("Jira Plugin shutdown.");
			if (JiraConnector != null)
			{
				Task.Run(async () => await JiraConnector.LogoutAsync());
			}
		}

		/// <summary>
		///     Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
			var url = _jiraConfiguration.Url;
			if (!ShowConfigDialog())
			{
				return;
			}
			// check for re-login
			if (JiraConnector == null || !JiraConnector.IsLoggedIn || string.IsNullOrEmpty(url))
			{
				return;
			}
			if (!url.Equals(_jiraConfiguration.Url))
			{
				Task.Run(async () =>
				{
					await JiraConnector.LogoutAsync();
					await JiraConnector.LoginAsync();
				});
			}
		}

		private void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}
			if (JiraConnector == null)
			{
				return;
			}
			JiraConnector.Dispose();
			JiraConnector = null;
		}

		/// <summary>
		///     A form for username/password
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