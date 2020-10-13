// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
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

using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using Dapplo.Windows.Clipboard;
using Greenshot.Addon.ExternalCommand.Configuration;
using Greenshot.Addon.ExternalCommand.Entities;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Core;
using Greenshot.Addons.Extensions;
using Greenshot.Addons.Interfaces;
using Greenshot.Gfx;

namespace Greenshot.Addon.ExternalCommand
{
    /// <summary>
    ///     ExternalCommandDestination provides a destination to export to an external command
    /// </summary>
    public sealed class ExternalCommandDestination : AbstractDestination
	{
		private static readonly Regex UriRegexp = new Regex(
				@"((([A-Za-z]{3,9}:(?:\/\/)?)(?:[\-;:&=\+\$,\w]+@)?[A-Za-z0-9\.\-]+|(?:www\.|[\-;:&=\+\$,\w]+@)[A-Za-z0-9\.\-]+)((?:\/[\+~%\/\.\w\-_]*)?\??(?:[\-\+=&;%@\.\w_]*)#?(?:[\.\!\/\\\w]*))?)", RegexOptions.Compiled);

		private readonly ExternalCommandDefinition _externalCommandDefinition;
	    private readonly IExternalCommandConfiguration _externalCommandConfiguration;
	    private readonly ExportNotification _exportNotification;

	    public ExternalCommandDestination(ExternalCommandDefinition defintion,
            IExternalCommandConfiguration externalCommandConfiguration,
		    ICoreConfiguration coreConfiguration,
		    IGreenshotLanguage greenshotLanguage,
	        ExportNotification exportNotification
        ) : base(coreConfiguration, greenshotLanguage)
        {
	        _externalCommandDefinition = defintion;
	        _externalCommandConfiguration = externalCommandConfiguration;
            _exportNotification = exportNotification;
        }

	    /// <inheritdoc />
	    public override string Designation => "External " + _externalCommandDefinition.Name.Replace(',', '_');

	    /// <inheritdoc />
	    public override string Description => _externalCommandDefinition.Name;

	    /// <inheritdoc />
	    public override IBitmapWithNativeSupport GetDisplayIcon(double dpi)
		{
			return IconCache.IconForCommand(_externalCommandDefinition, dpi > 100);
		}

	    /// <inheritdoc />
	    public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
	    {
	        var exportInformation = new ExportInformation(Designation, Description);

	        var fullPath = captureDetails.Filename ?? surface.SaveNamedTmpFile(CoreConfiguration, _externalCommandConfiguration);

            var output = await Cli.Wrap(_externalCommandDefinition.Command)
                .WithArguments(string.Format(_externalCommandDefinition.Arguments, fullPath))
                .ExecuteBufferedAsync().ConfigureAwait(false);

	        if (_externalCommandDefinition.CommandBehavior.HasFlag(CommandBehaviors.ParseOutputForUris))
	        {
	            var uriMatches = UriRegexp.Matches(output.StandardOutput);
	            if (uriMatches.Count > 0)
	            {
	                exportInformation.Uri = uriMatches[0].Groups[1].Value;

                    using var clipboardAccessToken = ClipboardNative.Access();
                    clipboardAccessToken.ClearContents();
                    clipboardAccessToken.SetAsUrl(exportInformation.Uri);
                }
	        }

	        if (_externalCommandDefinition.CommandBehavior.HasFlag(CommandBehaviors.DeleteOnExit))
	        {
                File.Delete(fullPath);
	        }

	        _exportNotification.NotifyOfExport(this, exportInformation, surface);
            return exportInformation;
	    }
	}
}