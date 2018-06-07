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
using System.Drawing;
using System.Windows.Forms;
using Dapplo.Windows.Clipboard;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Core;
using Greenshot.Addons.Core.Enums;
using Greenshot.Addons.Extensions;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;
using Greenshot.Configuration;

#endregion

namespace Greenshot.Destinations
{
    /// <summary>
    ///     Description of ClipboardDestination.
    /// </summary>
    [Destination("Clipboard", DestinationOrder.Clipboard)]
    public class ClipboardDestination : AbstractDestination
	{
	    public ClipboardDestination(
	        ICoreConfiguration coreConfiguration,
	        IGreenshotLanguage greenshotLanguage
	        ) : base(coreConfiguration, greenshotLanguage)
	    {
	    }

	    public override string Description => GreenshotLanguage.SettingsDestinationClipboard;

	    public override Keys EditorShortcutKeys => Keys.Control | Keys.Shift | Keys.C;

	    public override Bitmap DisplayIcon => GreenshotResources.GetBitmap("Clipboard.Image");

	    protected override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
	    {
			var exportInformation = new ExportInformation(Designation, Description);
			try
			{
				using (var clipboardAccessToken = ClipboardNative.Access())
				{
					clipboardAccessToken.ClearContents();
					
					//clipboardAccessToken.SetAsDeviceIndependendBitmap(surface);
					clipboardAccessToken.SetAsFormat17(surface);
				}
				exportInformation.ExportMade = true;
			}
			catch (Exception)
			{
				// TODO: Change to general logic in ProcessExport
				surface.SendMessageEvent(this, SurfaceMessageTyp.Error, "Error"); //GreenshotLanguage.editorclipboardfailed);
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}