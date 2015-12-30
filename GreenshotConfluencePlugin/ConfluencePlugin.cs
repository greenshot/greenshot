/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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

using Dapplo.Addons;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces.Plugin;
using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GreenshotConfluencePlugin.Support;

namespace GreenshotConfluencePlugin
{
	/// <summary>
	/// This is the ConfluencePlugin base code
	/// </summary>
	[Plugin("Confluence", Configurable = true)]
	[StartupAction, ShutdownAction]
	public class ConfluencePlugin : IConfigurablePlugin, IStartupAction, IShutdownAction
	{
		private static readonly Serilog.ILogger LOG = Serilog.Log.Logger.ForContext(typeof(ConfluencePlugin));
		private static ConfluenceApi _confluenceApi;

		[Import]
		public IConfluenceConfiguration ConfluenceConfiguration
		{
			get;
			set;
		}

		[Import]
		public IConfluenceLanguage ConfluenceLanguage
		{
			get;
			set;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			//if (disposing) {}
		}

		/// <summary>
		/// Initialize
		/// </summary>
		/// <param name="token"></param>
		public async Task StartAsync(CancellationToken token = new CancellationToken())
		{
			// Register / get the confluence configuration
			try
			{
				TranslationManager.Instance.TranslationProvider = new LanguageXMLTranslationProvider();
				//resources = new ComponentResourceManager(typeof(ConfluencePlugin));
			}
			catch (Exception ex)
			{
				LOG.Error("Problem in ConfluencePlugin.Initialize: {0}", ex.Message);
				return;
			}
			_confluenceApi = await GetConfluenceApi();
			if (_confluenceApi != null)
			{
				LOG.Information("Loading spaces");
				// Store the task, so the compiler doesn't complain but do not wait so the task runs in the background
				var ignoreTask = _confluenceApi.LoadSpacesAsync(token: token).ContinueWith((_) => LOG.Information("Finished loading spaces"), token).ConfigureAwait(false);
			}
		}

		public Task ShutdownAsync(CancellationToken token = new CancellationToken())
		{
			LOG.Debug("Confluence Plugin shutdown.");
			if (_confluenceApi != null)
			{
				_confluenceApi.Dispose();
				_confluenceApi = null;
			}
			return Task.FromResult(true);
		}

		public static ConfluenceApi ConfluenceAPI
		{
			get
			{
				return _confluenceApi;
			}
		}

		public async Task<ConfluenceApi> GetConfluenceApi()
		{
			ConfluenceApi confluenceApi = null;
			if (string.IsNullOrEmpty(ConfluenceConfiguration.RestUrl))
			{
				return null;
			}
			try
			{
				// Get the system name, so the user knows where to login to
				var dialog = new CredentialsDialog(ConfluenceConfiguration.RestUrl)
				{
					Name = null
				};
				while (dialog.Show(dialog.Name) == DialogResult.OK)
				{
					confluenceApi = new ConfluenceApi(new Uri(ConfluenceConfiguration.RestUrl));
					confluenceApi.SetBasicAuthentication(dialog.Name, dialog.Password);
					try
					{
						// Try loading content for id 0, should be null (or something) but not give an exception
						await confluenceApi.GetContentAsync(1).ConfigureAwait(false);
						LOG.Debug("Confluence access for User {0} worked", dialog.Name);
						if (dialog.SaveChecked)
						{
							dialog.Confirm(true);
						}
						return confluenceApi;
					}
					catch
					{
						LOG.Debug("Confluence access for User {0} didn't work, probably a wrong password.", dialog.Name);
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
			return confluenceApi;
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
			var oldConfig = new
			{
				Url = ConfluenceConfiguration.RestUrl
			};

			Forms.ConfluenceConfigurationForm configForm = new Forms.ConfluenceConfigurationForm();
			var dialogResult = configForm.ShowDialog();
			if (!dialogResult.HasValue || !dialogResult.Value)
			{
				return;
			}
			var newConfig = new
			{
				Url = ConfluenceConfiguration.RestUrl
			};
			if (!newConfig.Equals(oldConfig))
			{
				_confluenceApi.Dispose();
				_confluenceApi = null;
			}
		}
	}
}