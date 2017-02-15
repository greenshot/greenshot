#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

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