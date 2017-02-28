#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Windows.Forms;
using Greenshot.Configuration;
using Greenshot.Helpers;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces;

#endregion

namespace Greenshot.Destinations
{
	/// <summary>
	///     Description of EmailDestination.
	/// </summary>
	public class EmailDestination : AbstractDestination
	{
		public const string DESIGNATION = "EMail";
		private static readonly Image MailIcon = GreenshotResources.getImage("Email.Image");
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

		public override string Designation => DESIGNATION;

		public override string Description
		{
			get
			{
				// Make sure there is some kind of "mail" name
				if (_mapiClient == null)
				{
					_mapiClient = Language.GetString(LangKey.editor_email);
				}
				return _mapiClient;
			}
		}

		public override int Priority => 3;

		public override bool IsActive
		{
			get
			{
				if (_isActiveFlag)
				{
					// Disable if the office plugin is installed and the client is outlook
					// TODO: Change this! It always creates an exception, as the plugin has not been loaded the type is not there :(
					var outlookdestination = Type.GetType("GreenshotOfficePlugin.Destinations.OutlookDestination,GreenshotOfficePlugin");
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

		public override Image DisplayIcon => MailIcon;

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
			MapiMailMessage.SendImage(surface, captureDetails);
			exportInformation.ExportMade = true;
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}