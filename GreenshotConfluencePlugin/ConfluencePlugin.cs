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
using System.Windows;
using Confluence;
using GreenshotPlugin.Core;
using GreenshotPlugin.IniFile;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;
using Dapplo.Log;
using TranslationByMarkupExtension;

#endregion

namespace GreenshotConfluencePlugin
{
	/// <summary>
	///     This is the ConfluencePlugin base code
	/// </summary>
	public sealed class ConfluencePlugin : IGreenshotPlugin
	{
		private static readonly LogSource Log = new LogSource();
		private static ConfluenceConfiguration _config;

		public static ConfluenceConnector ConfluenceConnectorNoLogin { get; private set; }

		public static ConfluenceConnector ConfluenceConnector
		{
			get
			{
				if (ConfluenceConnectorNoLogin == null)
				{
					CreateConfluenceConntector();
				}
				try
				{
					if (ConfluenceConnectorNoLogin != null && !ConfluenceConnectorNoLogin.IsLoggedIn)
					{
						ConfluenceConnectorNoLogin.Login();
					}
				}
				catch (Exception e)
				{
					MessageBox.Show(Language.GetFormattedString("confluence", LangKey.login_error, e.Message));
				}
				return ConfluenceConnectorNoLogin;
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}

		public IEnumerable<IDestination> Destinations()
		{
			if (ConfluenceDestination.IsInitialized)
			{
				yield return new ConfluenceDestination();
			}
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
		public bool Initialize(IGreenshotHost pluginHost, PluginAttribute myAttributes)
		{
			// Register configuration (don't need the configuration itself)
			_config = IniConfig.GetIniSection<ConfluenceConfiguration>();
			if (_config.IsDirty)
			{
				IniConfig.Save();
			}
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
			if (ConfluenceConnectorNoLogin != null)
			{
				ConfluenceConnectorNoLogin.Logout();
				ConfluenceConnectorNoLogin = null;
			}
		}

		/// <summary>
		///     Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
			var clonedConfig = _config.Clone();
			var configForm = new ConfluenceConfigurationForm(clonedConfig);
			var url = _config.Url;
			var dialogResult = configForm.ShowDialog();
			if (dialogResult.HasValue && dialogResult.Value)
			{
				// copy the new object to the old...
				clonedConfig.CloneTo(_config);
				IniConfig.Save();
				if (ConfluenceConnectorNoLogin != null)
				{
					if (!url.Equals(_config.Url))
					{
						if (ConfluenceConnectorNoLogin.IsLoggedIn)
						{
							ConfluenceConnectorNoLogin.Logout();
						}
						ConfluenceConnectorNoLogin = null;
					}
				}
			}
		}

		private void Dispose(bool disposing)
		{
			//if (disposing) {}
		}

		private static void CreateConfluenceConntector()
		{
			if (ConfluenceConnectorNoLogin == null)
			{
				if (_config.Url.Contains("soap-axis"))
				{
					ConfluenceConnectorNoLogin = new ConfluenceConnector(_config.Url, _config.Timeout);
				}
				else
				{
					ConfluenceConnectorNoLogin = new ConfluenceConnector(_config.Url + ConfluenceConfiguration.DEFAULT_POSTFIX2, _config.Timeout);
				}
			}
		}
	}
}