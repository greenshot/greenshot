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

using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CliWrap;
using Greenshot.Addon.ExternalCommand.Entities;
using Greenshot.Addons;
using Greenshot.Addons.Core;
using Greenshot.Addons.Extensions;
using Greenshot.Addons.Interfaces;

#endregion

namespace Greenshot.Addon.ExternalCommand
{
    /// <summary>
    ///     ExternalCommandDestination provides a destination to export to an external command
    /// </summary>
    public class ExternalCommandDestination : AbstractDestination
	{
		private static readonly Regex UriRegexp = new Regex(
				@"((([A-Za-z]{3,9}:(?:\/\/)?)(?:[\-;:&=\+\$,\w]+@)?[A-Za-z0-9\.\-]+|(?:www\.|[\-;:&=\+\$,\w]+@)[A-Za-z0-9\.\-]+)((?:\/[\+~%\/\.\w\-_]*)?\??(?:[\-\+=&;%@\.\w_]*)#?(?:[\.\!\/\\\w]*))?)", RegexOptions.Compiled);

		private readonly ExternalCommandDefinition _externalCommandDefinition;
	    private readonly IExternalCommandConfiguration _externalCommandConfiguration;

	    public ExternalCommandDestination(ExternalCommandDefinition defintion,
            IExternalCommandConfiguration externalCommandConfiguration,
		    ICoreConfiguration coreConfiguration,
		    IGreenshotLanguage greenshotLanguage
		) : base(coreConfiguration, greenshotLanguage)
	    {
	        _externalCommandDefinition = defintion;
	        _externalCommandConfiguration = externalCommandConfiguration;
	    }

		public override string Designation => "External " + _externalCommandDefinition.Name.Replace(',', '_');

		public override string Description => _externalCommandDefinition.Name;

		public override Bitmap GetDisplayIcon(double dpi)
		{
			return IconCache.IconForCommand(_externalCommandDefinition, dpi > 100);
		}

	    public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
	    {
	        var exportInformation = new ExportInformation(Designation, Description);

	        var fullPath = captureDetails.Filename;
	        if (fullPath == null)
	        {
	            fullPath = surface.SaveNamedTmpFile(CoreConfiguration, _externalCommandConfiguration);
	        }

	        using (var cli = new Cli(_externalCommandDefinition.Command))
	        {
	            var arguments = string.Format(_externalCommandDefinition.Arguments, fullPath);
	            // Execute
	            var output = await cli.ExecuteAsync(arguments);

	            if (_externalCommandDefinition.CommandBehavior.HasFlag(CommandBehaviors.ParseOutputForUris))
	            {
	                var uriMatches = UriRegexp.Matches(output.StandardOutput);
	                if (uriMatches.Count > 0)
	                {
	                    exportInformation.Uri = uriMatches[0].Groups[1].Value;

	                    ClipboardHelper.SetClipboardData(output.StandardOutput);
	                }
	            }

	            if (_externalCommandDefinition.CommandBehavior.HasFlag(CommandBehaviors.DeleteOnExit))
	            {
                    File.Delete(fullPath);
	            }
	        }


	        ProcessExport(exportInformation, surface);
	        return exportInformation;
	    }
	}
}