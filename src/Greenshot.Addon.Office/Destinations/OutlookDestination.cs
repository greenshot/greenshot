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
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Dapplo.Log;
using Greenshot.Addon.Office.Configuration;
using Greenshot.Addon.Office.OfficeExport;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;
using Greenshot.Addons.Resources;
using Greenshot.Gfx;
using Microsoft.Office.Interop.Outlook;

namespace Greenshot.Addon.Office.Destinations
{
    /// <summary>
    ///     Description of OutlookDestination.
    /// </summary>
    [Destination("Outlook", DestinationOrder.Outlook)]
    public class OutlookDestination : AbstractDestination
	{
	    private const int IconApplication = 0;
		private const int IconMeeting = 2;
		private const string MapiClient = "Microsoft Outlook";
		private static readonly LogSource Log = new LogSource();

	    private readonly IOfficeConfiguration _officeConfiguration;
	    private readonly ExportNotification _exportNotification;
	    private static readonly IBitmapWithNativeSupport MailIcon = GreenshotResources.Instance.GetBitmap("Email.Image");
		private readonly string _exePath;
		private readonly bool _isActiveFlag;
		private readonly string _outlookInspectorCaption;
		private readonly OlObjectClass _outlookInspectorType;
	    private readonly OutlookExporter _outlookExporter;

        /// <summary>
        /// Constructor used for dependency injection
        /// </summary>
        /// <param name="officeConfiguration">IOfficeConfiguration</param>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        /// <param name="exportNotification">ExportNotification</param>
        public OutlookDestination(
            IOfficeConfiguration officeConfiguration,
		    ICoreConfiguration coreConfiguration,
		    IGreenshotLanguage greenshotLanguage,
            ExportNotification exportNotification
            ) : base(coreConfiguration, greenshotLanguage)
        {
            _officeConfiguration = officeConfiguration;
            _outlookExporter = new OutlookExporter(officeConfiguration);
            _exportNotification = exportNotification;
            if (EmailConfigHelper.HasOutlook())
		    {
		        _isActiveFlag = true;
		    }
		    _exePath = PluginUtils.GetExePath("OUTLOOK.EXE");
		    if (_exePath != null && !File.Exists(_exePath))
		    {
		        _exePath = null;
		    }
		    if (_exePath == null)
		    {
		        _isActiveFlag = false;
		    }
        }

        /// <summary>
        /// Constructor used for dependency injection
        /// </summary>
        /// <param name="outlookInspectorCaption">OlObjectClass</param>
        /// <param name="outlookInspectorType">OlObjectClass</param>
        /// <param name="officeConfiguration">IOfficeConfiguration</param>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        /// <param name="exportNotification">ExportNotification</param>
        protected OutlookDestination(
	        string outlookInspectorCaption,
	        OlObjectClass outlookInspectorType,
            IOfficeConfiguration officeConfiguration,
	        ICoreConfiguration coreConfiguration,
	        IGreenshotLanguage greenshotLanguage,
	        ExportNotification exportNotification
            ) : this(officeConfiguration, coreConfiguration, greenshotLanguage, exportNotification)
		{
            _outlookInspectorCaption = outlookInspectorCaption;
			_outlookInspectorType = outlookInspectorType;
		}

        /// <inherit />
		public override string Description => _outlookInspectorCaption ?? MapiClient;

        /// <inherit />
        public override bool IsActive => base.IsActive && _isActiveFlag;

        /// <inherit />
		public override bool IsDynamic => true;

        /// <inherit />
		public override Keys EditorShortcutKeys => Keys.Control | Keys.E;

        /// <inherit />
		public override IBitmapWithNativeSupport GetDisplayIcon(double dpi)
		{
			if (_outlookInspectorCaption == null)
			{
				return PluginUtils.GetCachedExeIcon(_exePath, IconApplication, dpi > 100);
			}
			if (OlObjectClass.olAppointment.Equals(_outlookInspectorType))
			{
				// Make sure we loaded the icon, maybe the configuration has been changed!
				return PluginUtils.GetCachedExeIcon(_exePath, IconMeeting, dpi > 100);
			}
			return MailIcon;
		}

        /// <inherit />
		public override IEnumerable<IDestination> DynamicDestinations()
		{
			var inspectorCaptions = _outlookExporter.RetrievePossibleTargets();
			if (inspectorCaptions == null)
			{
				yield break;
			}
			foreach (var inspectorCaption in inspectorCaptions.Keys)
			{
				yield return new OutlookDestination(inspectorCaption, inspectorCaptions[inspectorCaption], _officeConfiguration, CoreConfiguration, GreenshotLanguage, _exportNotification);
			}
		}

		/// <summary>
		///     Export the capture to outlook
		/// </summary>
		/// <param name="manuallyInitiated"></param>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <returns></returns>
		protected override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
			// Outlook logic
			var tmpFile = captureDetails.Filename;
			if (tmpFile == null || surface.Modified || !Regex.IsMatch(tmpFile, @".*(\.png|\.gif|\.jpg|\.jpeg|\.tiff|\.bmp)$"))
			{
				tmpFile = ImageOutput.SaveNamedTmpFile(surface, captureDetails, new SurfaceOutputSettings(CoreConfiguration).PreventGreenshotFormat());
			}
			else
			{
				Log.Info().WriteLine("Using already available file: {0}", tmpFile);
			}

			// Create a attachment name for the image
			var attachmentName = captureDetails.Title;
			if (!string.IsNullOrEmpty(attachmentName))
			{
				attachmentName = attachmentName.Trim();
			}
			// Set default if non is set
			if (string.IsNullOrEmpty(attachmentName))
			{
				attachmentName = "Greenshot Capture";
			}
			// Make sure it's "clean" so it doesn't corrupt the header
			attachmentName = Regex.Replace(attachmentName, @"[^\x20\d\w]", "");

			if (_outlookInspectorCaption != null)
			{
			    _outlookExporter.ExportToInspector(_outlookInspectorCaption, tmpFile, attachmentName);
				exportInformation.ExportMade = true;
			}
			else
			{
				if (!manuallyInitiated)
				{
					var inspectorCaptions = _outlookExporter.RetrievePossibleTargets();
					if (inspectorCaptions != null && inspectorCaptions.Count > 0)
					{
						var destinations = new List<IDestination>
						{
							new OutlookDestination(_officeConfiguration, CoreConfiguration, GreenshotLanguage, _exportNotification)
						};
						foreach (var inspectorCaption in inspectorCaptions.Keys)
						{
							destinations.Add(new OutlookDestination(inspectorCaption, inspectorCaptions[inspectorCaption], _officeConfiguration, CoreConfiguration, GreenshotLanguage, _exportNotification));
						}
						// Return the ExportInformation from the picker without processing, as this indirectly comes from us self
						return ShowPickerMenu(false, surface, captureDetails, destinations);
					}
				}
				else
				{
					exportInformation.ExportMade = _outlookExporter.ExportToOutlook(_officeConfiguration.OutlookEmailFormat, tmpFile,
						FilenameHelper.FillPattern(_officeConfiguration.EmailSubjectPattern, captureDetails, false), attachmentName, _officeConfiguration.EmailTo, _officeConfiguration.EmailCC,
					    _officeConfiguration.EmailBCC, null);
				}
			}
		    _exportNotification.NotifyOfExport(this, exportInformation, surface);
            return exportInformation;
		}
	}
}