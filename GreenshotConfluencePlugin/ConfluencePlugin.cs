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
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;
using Dapplo.Log;
using GreenshotConfluencePlugin.Support;

#endregion

namespace GreenshotConfluencePlugin
{
    /// <summary>
    ///     This is the ConfluencePlugin base code
    /// </summary>
    [Export(typeof(IGreenshotPlugin))]
    public sealed class ConfluencePlugin : IGreenshotPlugin
	{
		private static readonly LogSource Log = new LogSource();
		private readonly IConfluenceConfiguration _confluenceConfiguration;
	    private readonly ConfluenceConnector _confluenceConnector;

	    [ImportingConstructor]
	    public ConfluencePlugin(IConfluenceConfiguration confluenceConfiguration, ConfluenceConnector confluenceConnector)
	    {
	        _confluenceConfiguration = confluenceConfiguration;
	        _confluenceConnector = confluenceConnector;
	    }

		public void Dispose()
		{
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
		public bool Initialize()
		{
			try
			{
				TranslationManager.Instance.TranslationProvider = new LanguageXMLTranslationProvider();
				//resources = new ComponentResourceManager(typeof(ConfluencePlugin));
			}
			catch (Exception ex)
			{
				Log.Error().WriteLine("Problem in ConfluencePlugin.Initialize: {0}", ex.Message);
				return false;
			}
			return true;
		}

		public void Shutdown()
		{
			Log.Debug().WriteLine("Confluence Plugin shutdown.");
			if (_confluenceConnector.IsLoggedIn)
			{
			    _confluenceConnector.Logout();
			}
		}

		/// <summary>
		///     Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
			var clonedConfig = _confluenceConfiguration.Clone();
			var configForm = new Forms.ConfluenceConfigurationForm(clonedConfig);
			var url = _confluenceConfiguration.Url;
			var dialogResult = configForm.ShowDialog();
			if (dialogResult.HasValue && dialogResult.Value)
			{
			    // copy the new object to the old...
				clonedConfig.CloneTo(_confluenceConfiguration);
			    if (_confluenceConnector.IsLoggedIn && !url.Equals(_confluenceConfiguration.Url))
			    {
			        _confluenceConnector.Logout();
			    }
			}
		}
		
	}
}