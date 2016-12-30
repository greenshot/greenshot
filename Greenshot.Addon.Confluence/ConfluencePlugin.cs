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
using Dapplo.Confluence;
using Dapplo.Log;
using Greenshot.Addon.Confluence.Model;
using Greenshot.Addon.Confluence.Support;
using Greenshot.Addon.Confluence.Windows;
using Greenshot.Addon.Core;

#endregion

namespace Greenshot.Addon.Confluence
{
	/// <summary>
	///     This is the ConfluencePlugin base code
	/// </summary>
	[StartupAction(StartupOrder = (int) GreenshotStartupOrder.Addon)]
	[ShutdownAction]
	public class ConfluencePlugin : IAsyncStartupAction, IShutdownAction
	{
		private static readonly LogSource Log = new LogSource();

		public static IConfluenceClient ConfluenceClient { get; private set; }

		[Import]
		public IConfluenceConfiguration ConfluenceConfiguration { get; set; }

		[Import]
		public IConfluenceLanguage ConfluenceLanguage { get; set; }

		public static ConfluencePlugin Instance { get; private set; }

		public ConfluenceModel Model { get; } = new ConfluenceModel();

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		///     Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
			var oldConfig = new
			{
				Url = ConfluenceConfiguration.RestUrl
			};

			var configForm = new ConfluenceConfigurationForm();
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
				ConfluenceClient = null;
			}
		}

		public void Shutdown()
		{
			Log.Debug().WriteLine("Confluence Plugin shutdown.");
			ConfluenceClient = null;
		}

		/// <summary>
		///     Initialize
		/// </summary>
		/// <param name="token"></param>
		public async Task StartAsync(CancellationToken token = new CancellationToken())
		{
			Instance = this;
			// Register / get the confluence configuration
			try
			{
				TranslationManager.Instance.TranslationProvider = new LanguageXMLTranslationProvider();
				//resources = new ComponentResourceManager(typeof(ConfluencePlugin));
			}
			catch (Exception ex)
			{
				Log.Error().WriteLine("Problem in ConfluencePlugin.Initialize: {0}", ex.Message);
				return;
			}
			ConfluenceClient = await GetConfluenceApiAsync(token);
			if (ConfluenceClient != null)
			{
				Log.Info().WriteLine("Loading spaces");
				// Store the task, so the compiler doesn't complain but do not wait so the task runs in the background
				var ignoringTask = ConfluenceClient.GetSpacesAsync(token).ContinueWith(async spacesTask =>
				{
					var spaces = await spacesTask;
					foreach (var space in spaces)
					{
						if (Model.Spaces.ContainsKey(space.Key))
						{
							Model.Spaces[space.Key] = space;
						}
						else
						{
							Model.Spaces.Add(space.Key, space);
						}
					}
					Log.Info().WriteLine("Finished loading spaces");
					return spaces;
				}, token).ConfigureAwait(false);
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			//if (disposing) {}
		}

		private async Task<IConfluenceClient> GetConfluenceApiAsync(CancellationToken token = default(CancellationToken))
		{
			IConfluenceClient confluenceApi = null;
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
					confluenceApi = Dapplo.Confluence.ConfluenceClient.Create(new Uri(ConfluenceConfiguration.RestUrl));
					confluenceApi.SetBasicAuthentication(dialog.Name, dialog.Password);
					try
					{
						// Try loading content for id 0, should be null (or something) but not give an exception
						await confluenceApi.GetSpacesAsync(token).ConfigureAwait(false);
						Log.Debug().WriteLine("Confluence access for User {0} worked", dialog.Name);
						if (dialog.SaveChecked)
						{
							dialog.Confirm(true);
						}
						return confluenceApi;
					}
					catch
					{
						Log.Debug().WriteLine("Confluence access for User {0} didn't work, probably a wrong password.", dialog.Name);
						confluenceApi = null;
						try
						{
							dialog.Confirm(false);
						}
						catch (ApplicationException e)
						{
							// exception handling ...
							Log.Error().WriteLine("Problem using the credentials dialog", e);
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
				Log.Error().WriteLine("Problem using the credentials dialog", e);
			}
			return confluenceApi;
		}
	}
}