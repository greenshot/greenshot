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

using Dapplo.Config.Ini;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TranslationByMarkupExtension;

namespace GreenshotConfluencePlugin
{
	/// <summary>
	/// This is the ConfluencePlugin base code
	/// </summary>
	public class ConfluencePlugin : IGreenshotPlugin {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ConfluencePlugin));
		private static ConfluenceConfiguration _config;
		private static ConfluenceAPI _confluenceApi;

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			//if (disposing) {}
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
			if (_confluenceApi != null) {
				_confluenceApi.Dispose();
				_confluenceApi = null;
			}
		}

		public static ConfluenceAPI ConfluenceAPI {
			get {
				return _confluenceApi;
			}
		}

		public static dynamic Spaces {
			get;
			set;
		}

		public async static Task<ConfluenceAPI> GetConfluenceAPI() {
			ConfluenceAPI confluenceApi = null;
			if (!string.IsNullOrEmpty(_config.RestUrl)) {
				try {
					// Get the system name, so the user knows where to login to
					CredentialsDialog dialog = new CredentialsDialog(_config.RestUrl);
					dialog.Name = "Confluence";
					while (dialog.Show(dialog.Name) == DialogResult.OK) {
						confluenceApi = new ConfluenceAPI(new Uri(_config.RestUrl));
						confluenceApi.SetBasicAuthentication(dialog.Name, dialog.Password);
						try {
							Spaces = await confluenceApi.GetSpacesAsync().ConfigureAwait(false);
							if (dialog.SaveChecked) {
								dialog.Confirm(true);
							}
							return _confluenceApi;
						} catch {
							confluenceApi.Dispose();
							confluenceApi = null;
							try {
								dialog.Confirm(false);
							} catch (ApplicationException e) {
								// exception handling ...
								LOG.Error("Problem using the credentials dialog", e);
							}
							// For every windows version after XP show an incorrect password baloon
							dialog.IncorrectPassword = true;
							// Make sure the dialog is display, the password was false!
							dialog.AlwaysDisplay = true;
						}
					}
				} catch (ApplicationException e) {
					// exception handling ...
					LOG.Error("Problem using the credentials dialog", e);
				}
			}
			return confluenceApi;

		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure() {
			var oldConfig = new {
				Url = _config.RestUrl
			};

			ConfluenceConfigurationForm configForm = new ConfluenceConfigurationForm();
			string url = _config.RestUrl;
			Nullable<bool> dialogResult = configForm.ShowDialog();
			if (dialogResult.HasValue && dialogResult.Value) {
				var newConfig = new {
					Url = _config.RestUrl
				};
				if (!newConfig.Equals(oldConfig)) {
					_confluenceApi.Dispose();
					_confluenceApi = null;
				}
			}
		}
	}
}
