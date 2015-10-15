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

using Dapplo.Addons;
using Dapplo.Config.Ini;
using Greenshot.Plugin;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GreenshotEditorPlugin
{
	/// <summary>
	/// The editor as the plugin
	/// </summary>
	[Plugin]
	[StartupAction]
	public class EditorPlugin : IGreenshotPlugin, IStartupAction
	{
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof (EditorPlugin));

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
		/// <param name="myAttribute">My own attributes</param>
		/// <param name="token"></param>
		public async Task StartAsync(CancellationToken token = new CancellationToken())
		{
			var iniConfig = IniConfig.Current;

			// Make sure the defaults are set
			await iniConfig.RegisterAndGetAsync<IEditorConfiguration>(token);
			//await LanguageLoader.Current.RegisterAndGetAsync<IEditorLanguage>(token);
		}
	}
}