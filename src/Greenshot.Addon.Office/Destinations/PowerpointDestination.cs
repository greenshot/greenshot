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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Greenshot.Addon.Office.Configuration;
using Greenshot.Addon.Office.OfficeExport;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;
using Greenshot.Gfx;

namespace Greenshot.Addon.Office.Destinations
{
    /// <summary>
    ///     Description of PowerpointDestination.
    /// </summary>
    [Destination("Powerpoint", DestinationOrder.Powerpoint)]
    public class PowerpointDestination : AbstractDestination
	{
	    private readonly IOfficeConfiguration _officeConfiguration;
	    private readonly ExportNotification _exportNotification;

		private readonly string _exePath;
		private readonly string _presentationName;
	    private readonly PowerpointExporter _powerpointExporter;

	    private const int IconApplication = 0;
	    private const int IconPresentation = 1;

        /// <summary>
        /// Constructor used for dependency injection
        /// </summary>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        /// <param name="officeConfiguration">IOfficeConfiguration</param>
        /// <param name="exportNotification">ExportNotification</param>
        public PowerpointDestination(
		    ICoreConfiguration coreConfiguration,
		    IGreenshotLanguage greenshotLanguage,
            IOfficeConfiguration officeConfiguration,
		    ExportNotification exportNotification
        ) : base(coreConfiguration, greenshotLanguage)
        {
            _officeConfiguration = officeConfiguration;
            _exportNotification = exportNotification;
            _powerpointExporter = new PowerpointExporter(officeConfiguration);
            _exePath = PluginUtils.GetExePath("POWERPNT.EXE");
		    if (_exePath != null && !File.Exists(_exePath))
		    {
		        _exePath = null;
		    }
        }

        /// <summary>
        /// Constructor used for dependency injection
        /// </summary>
        /// <param name="presentationName">string with the name of the presentation</param>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        /// <param name="officeConfiguration">IOfficeConfiguration</param>
        /// <param name="exportNotification">ExportNotification</param>
        public PowerpointDestination(string presentationName,
		    ICoreConfiguration coreConfiguration,
		    IGreenshotLanguage greenshotLanguage,
		    IOfficeConfiguration officeConfiguration,
		    ExportNotification exportNotification) : this(coreConfiguration, greenshotLanguage, officeConfiguration, exportNotification)
		{
			_presentationName = presentationName;
		}

        /// <inherit />
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

        /// <inherit />
		public override bool IsDynamic => true;

        /// <inherit />
		public override bool IsActive => base.IsActive && _exePath != null;

        /// <inherit />
		public override IBitmapWithNativeSupport GetDisplayIcon(double dpi)
		{
			if (!string.IsNullOrEmpty(_presentationName))
			{
				return PluginUtils.GetCachedExeIcon(_exePath, IconPresentation, dpi > 100);
			}

			return PluginUtils.GetCachedExeIcon(_exePath, IconApplication, dpi > 100);
		}

        /// <inherit />
		public override IEnumerable<IDestination> DynamicDestinations()
		{
			return _powerpointExporter.GetPowerpointPresentations().Select(presentationName => new PowerpointDestination(presentationName, CoreConfiguration, GreenshotLanguage, _officeConfiguration, _exportNotification));
		}

        /// <inherit />
	    protected override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
			var tmpFile = captureDetails.Filename;
			var imageSize = Size.Empty;
			if (tmpFile == null || surface.Modified || !Regex.IsMatch(tmpFile, @".*(\.png|\.gif|\.jpg|\.jpeg|\.tiff|\.bmp)$"))
			{
				tmpFile = ImageOutput.SaveNamedTmpFile(surface, captureDetails, new SurfaceOutputSettings(CoreConfiguration).PreventGreenshotFormat());
				imageSize = surface.Screenshot.Size;
			}
			if (_presentationName != null)
			{
				exportInformation.ExportMade = _powerpointExporter.ExportToPresentation(_presentationName, tmpFile, imageSize, captureDetails.Title);
			}
			else
			{
				if (!manuallyInitiated)
				{
					var presentations = _powerpointExporter.GetPowerpointPresentations().ToList();
					if (presentations.Count > 0)
					{
						var destinations = new List<IDestination>
						{
						    new PowerpointDestination(CoreConfiguration, GreenshotLanguage, _officeConfiguration, _exportNotification)
						};
						foreach (var presentation in presentations)
						{
							destinations.Add(new PowerpointDestination(presentation, CoreConfiguration, GreenshotLanguage, _officeConfiguration, _exportNotification));
						}
						// Return the ExportInformation from the picker without processing, as this indirectly comes from us self
						return ShowPickerMenu(false, surface, captureDetails, destinations);
					}
				}
				else if (!exportInformation.ExportMade)
				{
					exportInformation.ExportMade = _powerpointExporter.InsertIntoNewPresentation(tmpFile, imageSize, captureDetails.Title);
				}
			}
		    _exportNotification.NotifyOfExport(this, exportInformation, surface);
            return exportInformation;
		}
	}
}