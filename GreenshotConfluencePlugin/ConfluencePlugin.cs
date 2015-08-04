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

using Confluence;
using Dapplo.Config.Ini;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TranslationByMarkupExtension;

namespace GreenshotConfluencePlugin
{
	/// <summary>
	/// This is the ConfluencePlugin base code
	/// </summary>
	public class ConfluencePlugin : IGreenshotPlugin {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ConfluencePlugin));
		private static ConfluenceConnector _confluenceConnector;
		private static ConfluenceConfiguration _config;

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			//if (disposing) {}
		}

		private static void CreateConfluenceConntector() {
			if (_confluenceConnector == null) {
				if (_config.Url.Contains("soap-axis")) {
					_confluenceConnector = new ConfluenceConnector(_config.Url, _config.Timeout);
				} else {
					_confluenceConnector = new ConfluenceConnector(_config.Url + ConfluenceConnector.DEFAULT_POSTFIX2, _config.Timeout);
				}
			}
		}

		public static ConfluenceConnector ConfluenceConnectorNoLogin {
			get {
				return _confluenceConnector;
			}
		}

		public static ConfluenceConnector ConfluenceConnector {
			get {
				if (_confluenceConnector == null) {
					CreateConfluenceConntector();
				}
				try {
					if (_confluenceConnector != null && !_confluenceConnector.isLoggedIn) {
						_confluenceConnector.login();
					}
				} catch (Exception e) {
					MessageBox.Show(Language.GetFormattedString("confluence", LangKey.login_error, e.Message));
				}
				return _confluenceConnector;
			}
		}

		public IEnumerable<IDestination> Destinations() {
			if (ConfluenceDestination.IsInitialized) {
				yield return new ConfluenceDestination();
			}
		}

		public IEnumerable<IProcessor> Processors() {
			yield break;
		}

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="pluginHost">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="myAttributes">My own attributes</param>
		public async Task<bool> InitializeAsync(IGreenshotHost pluginHost, PluginAttribute myAttributes, CancellationToken token = new CancellationToken()) {
			// Register / get the confluence configuration
			_config = await IniConfig.Get("Greenshot", "greenshot").RegisterAndGetAsync<ConfluenceConfiguration>();
			try {
				TranslationManager.Instance.TranslationProvider = new LanguageXMLTranslationProvider();
				//resources = new ComponentResourceManager(typeof(ConfluencePlugin));
			} catch (Exception ex) {
				LOG.ErrorFormat("Problem in ConfluencePlugin.Initialize: {0}", ex.Message);
				return false;
			}
			return true;
		}

		public void Shutdown() {
			LOG.Debug("Confluence Plugin shutdown.");
			if (_confluenceConnector != null) {
				_confluenceConnector.logout();
				_confluenceConnector = null;
			}
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure() {
			ConfluenceConfiguration clonedConfig = _config.Clone();
			ConfluenceConfigurationForm configForm = new ConfluenceConfigurationForm(clonedConfig);
			string url = _config.Url;
			Nullable<bool> dialogResult = configForm.ShowDialog();
			if (dialogResult.HasValue && dialogResult.Value) {
				// copy the new object to the old...
				clonedConfig.CloneTo(_config);
				// TODO: Save? IniConfig.Save();
				if (_confluenceConnector != null) {
					if (!url.Equals(_config.Url)) {
						if (_confluenceConnector.isLoggedIn) {
							_confluenceConnector.logout();
						}
						_confluenceConnector = null;
					}
				}
			}
		}
	}
}
