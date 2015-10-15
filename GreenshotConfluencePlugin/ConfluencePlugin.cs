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
using Dapplo.Config.Language;
using TranslationByMarkupExtension;
using System.ComponentModel.Composition;

namespace GreenshotConfluencePlugin
{
	/// <summary>
	/// This is the ConfluencePlugin base code
	/// </summary>
	[Export(typeof(IGreenshotPlugin))]
	public class ConfluencePlugin : IGreenshotPlugin
	{
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof (ConfluencePlugin));
		private static IConfluenceConfiguration _config;
		private static IConfluenceLanguage _language;
		private static ConfluenceApi _confluenceApi;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			//if (disposing) {}
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
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="pluginHost">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="myAttribute">My own attributes</param>
		public async Task<bool> InitializeAsync(IGreenshotHost pluginHost, PluginAttribute myAttribute, CancellationToken token = new CancellationToken())
		{
			// Register / get the confluence configuration
			_config = await IniConfig.Current.RegisterAndGetAsync<IConfluenceConfiguration>(token);
			_language = await LanguageLoader.Current.RegisterAndGetAsync<IConfluenceLanguage>(token);
			try
			{
				TranslationManager.Instance.TranslationProvider = new LanguageXMLTranslationProvider();
				//resources = new ComponentResourceManager(typeof(ConfluencePlugin));
			}
			catch (Exception ex)
			{
				LOG.ErrorFormat("Problem in ConfluencePlugin.Initialize: {0}", ex.Message);
				return false;
			}
			_confluenceApi = await GetConfluenceAPI();
			if (_confluenceApi != null)
			{
				LOG.Info("Loading spaces");
				// Store the task, so the compiler doesn't complain but do not wait so the task runs in the background
				var ignoreTask = _confluenceApi.LoadSpacesAsync(token: token).ContinueWith((_) => LOG.Info("Finished loading spaces"), token).ConfigureAwait(false);
			}
			return true;
		}

		public void Shutdown()
		{
			LOG.Debug("Confluence Plugin shutdown.");
			if (_confluenceApi != null)
			{
				_confluenceApi.Dispose();
				_confluenceApi = null;
			}
		}

		public static ConfluenceApi ConfluenceAPI
		{
			get
			{
				return _confluenceApi;
			}
		}

		public static async Task<ConfluenceApi> GetConfluenceAPI()
		{
			ConfluenceApi confluenceApi = null;
			if (!string.IsNullOrEmpty(_config.RestUrl))
			{
				try
				{
					// Get the system name, so the user knows where to login to
					CredentialsDialog dialog = new CredentialsDialog(_config.RestUrl);
					dialog.Name = null;
					while (dialog.Show(dialog.Name) == DialogResult.OK)
					{
						confluenceApi = new ConfluenceApi(new Uri(_config.RestUrl));
						confluenceApi.SetBasicAuthentication(dialog.Name, dialog.Password);
						try
						{
							// Try loading content for id 0, should be null (or something) but not give an exception
							await confluenceApi.GetContentAsync(1).ConfigureAwait(false);
							LOG.DebugFormat("Confluence access for User {0} worked", dialog.Name);
							if (dialog.SaveChecked)
							{
								dialog.Confirm(true);
							}
							return confluenceApi;
						}
						catch
						{
							LOG.DebugFormat("Confluence access for User {0} didn't work, probably a wrong password.", dialog.Name);
							confluenceApi.Dispose();
							confluenceApi = null;
							try
							{
								dialog.Confirm(false);
							}
							catch (ApplicationException e)
							{
								// exception handling ...
								LOG.Error("Problem using the credentials dialog", e);
							}
							// For every windows version after XP show an incorrect password baloon
							dialog.IncorrectPassword = true;
							// Make sure the dialog is display, the password was false!
							dialog.AlwaysDisplay = true;
						}
					}
				}
				catch (ApplicationException e)
				{
					// exception handling ...
					LOG.Error("Problem using the credentials dialog", e);
				}
			}
			return confluenceApi;
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
			var oldConfig = new
			{
				Url = _config.RestUrl
			};

			ConfluenceConfigurationForm configForm = new ConfluenceConfigurationForm();
			string url = _config.RestUrl;
			Nullable<bool> dialogResult = configForm.ShowDialog();
			if (dialogResult.HasValue && dialogResult.Value)
			{
				var newConfig = new
				{
					Url = _config.RestUrl
				};
				if (!newConfig.Equals(oldConfig))
				{
					_confluenceApi.Dispose();
					_confluenceApi = null;
				}
			}
		}
	}
}