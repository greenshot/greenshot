/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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

using System;
using System.Collections.Generic;
using Greenshot.Plugin;
using GreenshotPlugin.Core;

namespace GreenshotWin10Plugin
{
	/// <summary>
	/// This is the Win10Plugin
	/// </summary>
	public class Win10Plugin : IGreenshotPlugin
	{
		
		public void Dispose()
		{
			Dispose(true);
		}

		protected void Dispose(bool disposing)
		{
			if (disposing)
			{
			}
		}

		public void Configure()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// yields the windows 10 destinations if Windows 10 is detected
		/// </summary>
		/// <returns>IEnumerable with the destinations</returns>
		public IEnumerable<IDestination> Destinations()
		{
			if (!Environment.OSVersion.IsWindows10())
			{
				yield break;
			}
			yield return new Win10OcrDestination();
			yield return new Win10ShareDestination();
		}

		public IEnumerable<IProcessor> Processors()
		{
			yield break;
		}
		
		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="pluginHost">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="myAttributes">My own attributes</param>
		/// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
		public bool Initialize(IGreenshotHost pluginHost, PluginAttribute myAttributes)
		{
			return true;
		}

		public void Shutdown()
		{
		}
	}

}
