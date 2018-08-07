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

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Greenshot.Addon.Office.OfficeExport;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;

#endregion

namespace Greenshot.Addon.Office.Destinations
{
    /// <summary>
    ///     Description of PowerpointDestination.
    /// </summary>
    [Destination("Powerpoint", DestinationOrder.Powerpoint)]
    public class PowerpointDestination : AbstractDestination
	{
	    private readonly ExportNotification _exportNotification;
	    private const int IconApplication = 0;
		private const int IconPresentation = 1;

		private readonly string _exePath;
		private readonly string _presentationName;


		public PowerpointDestination(
		    ICoreConfiguration coreConfiguration,
		    IGreenshotLanguage greenshotLanguage,
		    ExportNotification exportNotification
        ) : base(coreConfiguration, greenshotLanguage, exportNotification)
        {
            _exportNotification = exportNotification;
            _exePath = PluginUtils.GetExePath("POWERPNT.EXE");
		    if (_exePath != null && !File.Exists(_exePath))
		    {
		        _exePath = null;
		    }
        }

		public PowerpointDestination(string presentationName,
		    ICoreConfiguration coreConfiguration,
		    IGreenshotLanguage greenshotLanguage,
		    ExportNotification exportNotification) : this(coreConfiguration, greenshotLanguage, exportNotification)
		{
			_presentationName = presentationName;
		}

	    public override string Description
		{
			get
			{
				if (_presentationName == null)
				{
					return "Microsoft Powerpoint";
				}
				return _presentationName;
			}
		}

		public override bool IsDynamic => true;

		public override bool IsActive => base.IsActive && _exePath != null;

		public override Bitmap GetDisplayIcon(double dpi)
		{
			if (!string.IsNullOrEmpty(_presentationName))
			{
				return PluginUtils.GetCachedExeIcon(_exePath, IconPresentation, dpi > 100);
			}

			return PluginUtils.GetCachedExeIcon(_exePath, IconApplication, dpi > 100);
		}

		public override IEnumerable<IDestination> DynamicDestinations()
		{
			return PowerpointExporter.GetPowerpointPresentations().Select(presentationName => new PowerpointDestination(presentationName, CoreConfiguration, GreenshotLanguage, _exportNotification));
		}

	    protected override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
			var tmpFile = captureDetails.Filename;
			var imageSize = Size.Empty;
			if (tmpFile == null || surface.Modified || !Regex.IsMatch(tmpFile, @".*(\.png|\.gif|\.jpg|\.jpeg|\.tiff|\.bmp)$"))
			{
				tmpFile = ImageOutput.SaveNamedTmpFile(surface, captureDetails, new SurfaceOutputSettings().PreventGreenshotFormat());
				imageSize = surface.Screenshot.Size;
			}
			if (_presentationName != null)
			{
				exportInformation.ExportMade = PowerpointExporter.ExportToPresentation(_presentationName, tmpFile, imageSize, captureDetails.Title);
			}
			else
			{
				if (!manuallyInitiated)
				{
					var presentations = PowerpointExporter.GetPowerpointPresentations();
					if (presentations != null && presentations.Count > 0)
					{
						var destinations = new List<IDestination>
						{
						    new PowerpointDestination(CoreConfiguration, GreenshotLanguage, _exportNotification)
						};
						foreach (var presentation in presentations)
						{
							destinations.Add(new PowerpointDestination(presentation, CoreConfiguration, GreenshotLanguage, _exportNotification));
						}
						// Return the ExportInformation from the picker without processing, as this indirectly comes from us self
						return ShowPickerMenu(false, surface, captureDetails, destinations);
					}
				}
				else if (!exportInformation.ExportMade)
				{
					exportInformation.ExportMade = PowerpointExporter.InsertIntoNewPresentation(tmpFile, imageSize, captureDetails.Title);
				}
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}