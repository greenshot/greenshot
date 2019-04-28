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
using System.Windows.Forms;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Resources;
using Greenshot.Gfx;
using Greenshot.Helpers.Mapi;

namespace Greenshot.Destinations
{
    /// <summary>
    ///     Description of EmailDestination.
    /// </summary>
    [Destination("EMail", DestinationOrder.Email)]
    public class EmailDestination : AbstractDestination
	{
	    private readonly ExportNotification _exportNotification;
	    private static readonly IBitmapWithNativeSupport MailIcon = GreenshotResources.Instance.GetBitmap("Email.Image");
		private static bool _isActiveFlag;
		private static string _mapiClient;

		static EmailDestination()
		{
			// Logic to decide what email implementation we use
			if (EmailConfigHelper.HasMapi())
			{
				_isActiveFlag = true;
				_mapiClient = EmailConfigHelper.GetMapiClient();
				if (!string.IsNullOrEmpty(_mapiClient))
				{
					// Active as we have a mapi client, can be disabled later
					_isActiveFlag = true;
				}
			}
		}

	    public EmailDestination(
	        ICoreConfiguration coreConfiguration,
	        IGreenshotLanguage greenshotLanguage,
	        ExportNotification exportNotification) : base(coreConfiguration, greenshotLanguage)
	    {
	        _exportNotification = exportNotification;
	    }

	    public override string Description
		{
			get
			{
				// Make sure there is some kind of "mail" name
				if (_mapiClient == null)
				{
                    // TODO: Change language
				    _mapiClient = "Email";  //GreenshotLanguage.editor_email;
				}
				return _mapiClient;
			}
		}

		public override bool IsActive
		{
			get
			{
				if (_isActiveFlag)
				{
					// Disable if the office plugin is installed and the client is outlook
					// TODO: Change this! It always creates an exception, as the plugin has not been loaded the type is not there :(
					var outlookdestination = Type.GetType("Greenshot.Addon.Office.Destinations.OutlookDestination,Greenshot.Addon.Office");
					if (outlookdestination != null)
					{
						if (_mapiClient.ToLower().Contains("microsoft outlook"))
						{
							_isActiveFlag = false;
						}
					}
				}
				return base.IsActive && _isActiveFlag;
			}
		}

		public override Keys EditorShortcutKeys => Keys.Control | Keys.E;

		public override IBitmapWithNativeSupport DisplayIcon => MailIcon;

	    protected override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
			MapiMailMessage.SendImage(surface, captureDetails);
			exportInformation.ExportMade = true;
		    _exportNotification.NotifyOfExport(this, exportInformation, surface);
            return exportInformation;
		}
	}
}