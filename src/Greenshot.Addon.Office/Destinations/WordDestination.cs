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
using System.Text.RegularExpressions;
using Dapplo.Log;
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
    ///     Description of EmailDestination.
    /// </summary>
    [Destination("Word", DestinationOrder.Word)]
    public class WordDestination : AbstractDestination
	{
	    private readonly IOfficeConfiguration _officeConfiguration;
	    private readonly ExportNotification _exportNotification;
	    private const int IconApplication = 0;
		private const int IconDocument = 1;
		private static readonly LogSource Log = new LogSource();
		private readonly string _exePath;
		private readonly string _documentCaption;
	    private readonly WordExporter _wordExporter;

        /// <summary>
        /// Constructor used for dependency injection
        /// </summary>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        /// <param name="officeConfiguration">IOfficeConfiguration</param>
        /// <param name="exportNotification">ExportNotification</param>
        public WordDestination(
		    ICoreConfiguration coreConfiguration,
		    IGreenshotLanguage greenshotLanguage,
		    IOfficeConfiguration officeConfiguration,
		    ExportNotification exportNotification
            ) : base(coreConfiguration, greenshotLanguage)
        {
            _officeConfiguration = officeConfiguration;
            _exportNotification = exportNotification;
            _wordExporter = new WordExporter(officeConfiguration);
            _exePath = PluginUtils.GetExePath("WINWORD.EXE");
		    if (_exePath != null && !File.Exists(_exePath))
		    {
		        _exePath = null;
		    }
        }

        /// <summary>
        /// Constructor used for dependency injection
        /// </summary>
        /// <param name="wordCaption">string with the caption of the word document</param>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        /// <param name="officeConfiguration">IOfficeConfiguration</param>
        /// <param name="exportNotification">ExportNotification</param>
        protected WordDestination(string wordCaption,
		    ICoreConfiguration coreConfiguration,
	        IGreenshotLanguage greenshotLanguage,
		    IOfficeConfiguration officeConfiguration,
            ExportNotification exportNotification) : this(coreConfiguration, greenshotLanguage, officeConfiguration, exportNotification)
		{
			_documentCaption = wordCaption;
		}

        /// <inherit />
        public override string Description => _documentCaption ?? "Microsoft Word";

        /// <inherit />
		public override bool IsDynamic => true;

        /// <inherit />
		public override bool IsActive => base.IsActive && _exePath != null;

        /// <inherit />
		public override IBitmapWithNativeSupport GetDisplayIcon(double dpi)
		{
			return PluginUtils.GetCachedExeIcon(_exePath, !string.IsNullOrEmpty(_documentCaption) ? IconDocument : IconApplication, dpi > 100);
		}

        /// <inherit />
        public override IEnumerable<IDestination> DynamicDestinations()
		{
			return _wordExporter.GetWordDocuments().Select(wordCaption => new WordDestination(wordCaption, CoreConfiguration, GreenshotLanguage, _officeConfiguration, _exportNotification));
		}

        /// <inherit />
	    protected override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
			var tmpFile = captureDetails.Filename;
			if (tmpFile == null || surface.Modified || !Regex.IsMatch(tmpFile, @".*(\.png|\.gif|\.jpg|\.jpeg|\.tiff|\.bmp)$"))
			{
				tmpFile = ImageOutput.SaveNamedTmpFile(surface, captureDetails, new SurfaceOutputSettings(CoreConfiguration).PreventGreenshotFormat());
			}
			if (_documentCaption != null)
			{
				try
				{
				    _wordExporter.InsertIntoExistingDocument(_documentCaption, tmpFile);
					exportInformation.ExportMade = true;
				}
				catch (Exception)
				{
					try
					{
					    _wordExporter.InsertIntoExistingDocument(_documentCaption, tmpFile);
						exportInformation.ExportMade = true;
					}
					catch (Exception ex)
					{
						Log.Error().WriteLine(ex);
						// TODO: Change to general logic in ProcessExport
						surface.SendMessageEvent(this, SurfaceMessageTyp.Error, string.Format(GreenshotLanguage.DestinationExportFailed, Description));
                    }
				}
			}
			else
			{
				if (!manuallyInitiated)
				{
					var documents = _wordExporter.GetWordDocuments().ToList();
					if (documents.Count > 0)
					{
						var destinations = new List<IDestination>
						{
							new WordDestination(CoreConfiguration, GreenshotLanguage, _officeConfiguration, _exportNotification)
						};
						foreach (var document in documents)
						{
							destinations.Add(new WordDestination(document, CoreConfiguration, GreenshotLanguage, _officeConfiguration, _exportNotification));
						}
						// Return the ExportInformation from the picker without processing, as this indirectly comes from us self
						return ShowPickerMenu(false, surface, captureDetails, destinations);
					}
				}
				try
				{
					_wordExporter.InsertIntoNewDocument(tmpFile, null, null);
					exportInformation.ExportMade = true;
				}
				catch (Exception)
				{
					// Retry once, just in case
					try
					{
						_wordExporter.InsertIntoNewDocument(tmpFile, null, null);
						exportInformation.ExportMade = true;
					}
					catch (Exception ex)
					{
						Log.Error().WriteLine(ex);
						// TODO: Change to general logic in ProcessExport
						surface.SendMessageEvent(this, SurfaceMessageTyp.Error, string.Format(GreenshotLanguage.DestinationExportFailed, Description));
					}
				}
			}
		    _exportNotification.NotifyOfExport(this, exportInformation, surface);
            return exportInformation;
		}
	}
}