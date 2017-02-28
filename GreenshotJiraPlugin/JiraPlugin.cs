#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Windows.Dpi;
using Greenshot.Forms;
using Greenshot.Helpers;
using GreenshotJiraPlugin.Forms;
using GreenshotPlugin.IniFile;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;
using log4net;

#endregion

namespace GreenshotJiraPlugin
{
	/// <summary>
	///     This is the JiraPlugin base code
	/// </summary>
	public sealed class JiraPlugin : IGreenshotPlugin
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(JiraPlugin));
		private JiraConfiguration _config;
		private JiraConnector _jiraConnector;

		public JiraPlugin()
		{
			Instance = this;
			// Added to prevent Greenshot from shutting down when there was an exception in a Task
			TaskScheduler.UnobservedTaskException += (sender, args) =>
			{
				try
				{
					Exception exceptionToLog = args.Exception;
					var exceptionText = EnvironmentInfo.BuildReport(exceptionToLog);
					Log.Error("Exception caught in the UnobservedTaskException handler.");
					Log.Error(exceptionText);
					new BugReportForm(exceptionText).ShowDialog();
				}
				finally
				{
					args.SetObserved();
				}
			};
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
		/// <param name="pluginHost">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="myAttributes">My own attributes</param>
		/// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
		public bool Initialize(IGreenshotHost pluginHost, PluginAttribute myAttributes)
		{
			// Register configuration (don't need the configuration itself)
			_config = IniConfig.GetIniSection<JiraConfiguration>();

			pluginHost.ContextMenuDpiHandler.OnDpiChanged.Subscribe(dpi =>
			{
				var size = DpiHandler.ScaleWithDpi(16, dpi);

				JiraConnector.UpdateSvgSize(size);
			});

			return true;
		}

		public void Shutdown()
		{
			Log.Debug("Jira Plugin shutdown.");
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
			var url = _config.Url;
			if (!ShowConfigDialog())
			{
				return;
			}
			// check for re-login
			if (JiraConnector == null || !JiraConnector.IsLoggedIn || string.IsNullOrEmpty(url))
			{
				return;
			}
			if (!url.Equals(_config.Url))
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