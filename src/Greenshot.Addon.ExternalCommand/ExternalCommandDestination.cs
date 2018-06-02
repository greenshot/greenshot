#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Models;
using Dapplo.Ini;
using Dapplo.Log;
using Greenshot.Addon.ExternalCommand.Entities;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Core;
using Greenshot.Addons.Extensions;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;

#endregion

namespace Greenshot.Addon.ExternalCommand
{
    /// <summary>
    ///     ExternalCommandDestination provides a destination to export to an external command
    /// </summary>
    public class ExternalCommandDestination : AbstractDestination
	{
		private static readonly LogSource Log = new LogSource();

		private static readonly Regex UriRegexp = new Regex(
				@"((([A-Za-z]{3,9}:(?:\/\/)?)(?:[\-;:&=\+\$,\w]+@)?[A-Za-z0-9\.\-]+|(?:www\.|[\-;:&=\+\$,\w]+@)[A-Za-z0-9\.\-]+)((?:\/[\+~%\/\.\w\-_]*)?\??(?:[\-\+=&;%@\.\w_]*)#?(?:[\.\!\/\\\w]*))?)", RegexOptions.Compiled);

		private static readonly IExternalCommandConfiguration Config = IniConfig.Current.Get<IExternalCommandConfiguration>();
		private readonly string _presetCommand;
	    private readonly IExternalCommandConfiguration _externalCommandConfiguration;

	    public ExternalCommandDestination(string commando,
            IExternalCommandConfiguration externalCommandConfiguration,
		    ICoreConfiguration coreConfiguration,
		    IGreenshotLanguage greenshotLanguage
		) : base(coreConfiguration, greenshotLanguage)
	    {
	        _presetCommand = commando;
	        _externalCommandConfiguration = externalCommandConfiguration;
	    }

		public override string Designation => "External " + _presetCommand.Replace(',', '_');

		public override string Description => _presetCommand;

		public override Bitmap GetDisplayIcon(double dpi)
		{
			return IconCache.IconForCommand(_presetCommand, dpi > 100);
		}

	    public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
	    {
	        var exportInformation = new ExportInformation(Designation, Description);

	        var definition = _externalCommandConfiguration.Read(_presetCommand);
	        var fullPath = captureDetails.Filename;
	        if (fullPath == null)
	        {
	            fullPath = surface.SaveNamedTmpFile(CoreConfiguration, _externalCommandConfiguration);
	        }

	        using (var cli = new Cli(definition.Command))
	        {
	            var arguments = string.Format(definition.Arguments, fullPath);
	            // Execute
	            var output = await cli.ExecuteAsync(arguments);

	            if (definition.CommandBehavior.HasFlag(CommandBehaviors.ParseOutputForUris))
	            {
	                var uriMatches = UriRegexp.Matches(output.StandardOutput);
	                if (uriMatches.Count > 0)
	                {
	                    exportInformation.Uri = uriMatches[0].Groups[1].Value;

	                    ClipboardHelper.SetClipboardData(output.StandardOutput);
	                }
	            }

	            if (definition.CommandBehavior.HasFlag(CommandBehaviors.DeleteOnExit))
	            {
                    File.Delete(fullPath);
	            }
	        }


	        ProcessExport(exportInformation, surface);
	        return exportInformation;
	    }
	}
}