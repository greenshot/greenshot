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

using System.Collections.Generic;
using System.ComponentModel;

namespace GreenshotPlugin.Configuration
{
	/// <summary>
	/// This interface represents all the generic plugin/extension settings
	/// </summary>
	public interface IExtensionConfiguration
	{
		[Description("Comma separated list of Plugins which are allowed. If something in the list, than every plugin not in the list will not be loaded!")]
		IList<string> IncludePlugins
		{
			get;
			set;
		}

		[Description("Comma separated list of Plugins which are NOT allowed.")]
		IList<string> ExcludePlugins
		{
			get;
			set;
		}

		[Description("Comma separated list of destinations which should be disabled.")]
		IList<string> ExcludeDestinations
		{
			get;
			set;
		}
	}
}