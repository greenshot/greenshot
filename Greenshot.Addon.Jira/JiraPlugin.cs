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
using Dapplo.Log;
using Dapplo.Windows.Dpi;
using Greenshot.Addons.Addons;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;

#endregion

namespace Greenshot.Addon.Jira
{
    /// <summary>
    ///     This is the JiraPlugin base code
    /// </summary>
    [Export(typeof(IGreenshotPlugin))]
    public sealed class JiraPlugin : IGreenshotPlugin
	{
	    private static readonly LogSource Log = new LogSource();
	    private readonly IGreenshotHost _greenshotHost;
	    private readonly ICoreConfiguration _coreConfiguration;
	    private readonly JiraConnector _jiraConnector;


        [ImportingConstructor]
		public JiraPlugin(IGreenshotHost greenshotHost, ICoreConfiguration coreConfiguration, JiraConnector jiraConnector)
        {
            _greenshotHost = greenshotHost;
            _coreConfiguration = coreConfiguration;
            _jiraConnector = jiraConnector;
            Instance = this;
        }

		public static JiraPlugin Instance { get; private set; }

		public void Dispose()
		{
			Dispose(true);
		}

		public IEnumerable<IDestination> Destinations()
		{
		    yield break;
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

			    _jiraConnector.UpdateSvgSize(size);
			});

			return true;
		}

		public void Shutdown()
		{
			Log.Debug().WriteLine("Jira Plugin shutdown.");
			if (_jiraConnector != null)
			{
				Task.Run(async () => await _jiraConnector.LogoutAsync());
			}
		}

		private void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

		    _jiraConnector?.Dispose();
		}
	}
}