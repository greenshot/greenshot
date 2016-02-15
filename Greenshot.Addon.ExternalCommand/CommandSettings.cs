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

using Dapplo.Config.Ini;
using Greenshot.Addon.Configuration;

namespace Greenshot.Addon.ExternalCommand
{
	public class CommandSettings
	{
		private static readonly IExternalCommandConfiguration ExternalCommandConfiguration = IniConfig.Current.Get<IExternalCommandConfiguration>();

		public CommandSettings(string commando)
		{
			Name = commando;
			Commandline = ExternalCommandConfiguration.Commandline[commando];
			Arguments = ExternalCommandConfiguration.Argument[commando];
			RunInbackground = ExternalCommandConfiguration.RunInbackground[commando];
			// TODO: Make configurable
			Format = OutputFormat.png;
		}

		public string Name
		{
			get;
			set;
		}

		public bool RunInbackground
		{
			get; set;
		}

		public string Commandline
		{
			get;
			set;
		}

		public string Arguments
		{
			get;
			set;
		}

		public OutputFormat Format
		{
			get;
			set;
		}
	}
}
