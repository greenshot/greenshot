// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Dapplo.Config.Ini;
using Greenshot.Addon.ExternalCommand.Entities;
using Greenshot.Addons.Core;
#pragma warning disable 1591

namespace Greenshot.Addon.ExternalCommand.Configuration
{
	/// <summary>
	///     Description of FlickrConfiguration.
	/// </summary>
	[IniSection("ExternalCommand")]
	[Description("Greenshot ExternalCommand Plugin configuration")]
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
    public interface IExternalCommandConfiguration : IIniSection, IDestinationFileConfiguration
	{
		[Description("The commands that are available.")]
		IList<string> Commands { get; set; }

		[Description("The commandline for the output command.")]
		IDictionary<string, string> Commandline { get; set; }

		[Description("The arguments for the output command.")]
		IDictionary<string, string> Argument { get; set; }

		[Description("Should the command be started in the background. (obsolete)")]
		IDictionary<string, bool> RunInbackground { get; set; }

	    [Description("Command behaviors.")]
	    IDictionary<string, CommandBehaviors> Behaviors { get; set; }

        [Description("If a build in command was deleted manually, it should not be recreated.")]
		IList<string> DeletedBuildInCommands { get; set; }
	}
}