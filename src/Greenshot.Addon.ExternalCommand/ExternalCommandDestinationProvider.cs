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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dapplo.Addons;
using Dapplo.CaliburnMicro;
using Dapplo.Log;
using Greenshot.Addon.ExternalCommand.Configuration;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Core;

namespace Greenshot.Addon.ExternalCommand
{
    /// <summary>
    ///     Generate the external command destinations
    /// </summary>
    [Service(nameof(ExternalCommandDestinationProvider), nameof(CaliburnServices.ConfigurationService))]
    public sealed class ExternalCommandDestinationProvider : IStartup, IDestinationProvider
	{
		private static readonly LogSource Log = new LogSource();
		private readonly IExternalCommandConfiguration _externalCommandConfig;
	    private readonly ICoreConfiguration _coreConfiguration;
	    private readonly IGreenshotLanguage _greenshotLanguage;
	    private readonly ExportNotification _exportNotification;

	    public ExternalCommandDestinationProvider(
	        IExternalCommandConfiguration externalCommandConfiguration,
	        ICoreConfiguration coreConfiguration,
	        IGreenshotLanguage greenshotLanguage,
	        ExportNotification exportNotification)
	    {
	        _externalCommandConfig = externalCommandConfiguration;
	        _coreConfiguration = coreConfiguration;
	        _greenshotLanguage = greenshotLanguage;
	        _exportNotification = exportNotification;
	    }

	    /// <inheritdoc />
	    public IEnumerable<Lazy<IDestination, DestinationAttribute>> Provide()
		{
		    return _externalCommandConfig.Commands
		        .Select(command => _externalCommandConfig.Read(command))
		        .Select(definition => new Lazy<IDestination, DestinationAttribute>(() => new ExternalCommandDestination(definition, _externalCommandConfig, _coreConfiguration, _greenshotLanguage, _exportNotification), new DestinationAttribute(definition.Name)));
		}


		/// <summary>
		///     Check and eventually fix the command settings
		/// </summary>
		/// <param name="command"></param>
		/// <returns>false if the command is not correctly configured</returns>
		private bool IsCommandValid(string command)
		{
			if (!_externalCommandConfig.RunInbackground.ContainsKey(command))
			{
				Log.Warn().WriteLine("Found missing runInbackground for {0}", command);
				// Fix it
				_externalCommandConfig.RunInbackground.Add(command, true);
			}
			if (!_externalCommandConfig.Argument.ContainsKey(command))
			{
				Log.Warn().WriteLine("Found missing argument for {0}", command);
				// Fix it
				_externalCommandConfig.Argument.Add(command, "{0}");
			}
			if (!_externalCommandConfig.Commandline.ContainsKey(command))
			{
				Log.Warn().WriteLine("Found missing commandline for {0}", command);
				return false;
			}
			var commandline = FilenameHelper.FillVariables(_externalCommandConfig.Commandline[command], true);
			commandline = FilenameHelper.FillCmdVariables(commandline);

			if (File.Exists(commandline))
			{
				return true;
			}
			Log.Warn().WriteLine("Found 'invalid' commandline {0} for command {1}", _externalCommandConfig.Commandline[command], command);
			return false;
		}

	    /// <inheritdoc />
	    public void Startup()
	    {
	        // Check configuration & cleanup
	        foreach (var command in _externalCommandConfig.Commands.Where(command => !IsCommandValid(command)).ToList())
	        {
	            _externalCommandConfig.Delete(command);
	        }
        }
	}
}