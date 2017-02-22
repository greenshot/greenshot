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

#endregion

namespace GreenshotPlugin.Interfaces.Plugin
{
	public interface IGreenshotPlugin : IDisposable
	{
		/// <summary>
		///     Is called after the plugin is instanciated, the Plugin should keep a copy of the host and pluginAttribute.
		/// </summary>
		/// <param name="host">The IPluginHost that will be hosting the plugin</param>
		/// <param name="pluginAttribute">The PluginAttribute for the actual plugin</param>
		/// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
		bool Initialize(IGreenshotHost host, PluginAttribute pluginAttribute);

		/// <summary>
		///     Unload of the plugin
		/// </summary>
		void Shutdown();

		/// <summary>
		///     Open the Configuration Form, will/should not be called before handshaking is done
		/// </summary>
		void Configure();

		/// <summary>
		///     Return IDestination's, if the plugin wants to
		/// </summary>
		IEnumerable<IDestination> Destinations();

		/// <summary>
		///     Return IProcessor's, if the plugin wants to
		/// </summary>
		IEnumerable<IProcessor> Processors();
	}
}