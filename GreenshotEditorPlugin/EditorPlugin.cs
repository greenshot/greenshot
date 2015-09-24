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
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Config.Language;
using GreenshotPlugin.Configuration;

namespace GreenshotEditorPlugin
{
	/// <summary>
	/// The editor as the plugin
	/// </summary>
	public class EditorPlugin : IGreenshotPlugin
	{
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(EditorPlugin));
		private PluginAttribute _myAttributes;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
			}
		}

		public IEnumerable<IDestination> Destinations()
		{
			yield return new EditorDestination();
		}

		public IEnumerable<IProcessor> Processors()
		{
			yield break;
		}

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="pluginHost">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="pluginAttributes">My own attributes</param>
		/// <param name="token"></param>
		public async Task<bool> InitializeAsync(IGreenshotHost pluginHost, PluginAttribute pluginAttributes, CancellationToken token = new CancellationToken())
		{
			LOG.DebugFormat("Initialize called of {0}", pluginAttributes.Name);
			var iniConfig = IniConfig.Current;

			// Make sure the defaults are set
			await iniConfig.RegisterAndGetAsync<IEditorConfiguration>(token);
			await LanguageLoader.Current.RegisterAndGetAsync<IEditorLanguage>(token);
            _myAttributes = pluginAttributes;
			return true;
		}

		public void Shutdown()
		{
			LOG.Debug("Shutdown of " + _myAttributes.Name);
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
		}
	}
}