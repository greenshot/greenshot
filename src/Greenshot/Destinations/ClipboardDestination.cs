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
using Dapplo.Windows.Clipboard;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Core;
using Greenshot.Addons.Core.Enums;
using Greenshot.Addons.Extensions;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;
using Greenshot.Addons.Resources;
using Greenshot.Core.Enums;
using Greenshot.Gfx;

namespace Greenshot.Destinations
{
    /// <summary>
    ///     Description of ClipboardDestination.
    /// </summary>
    [Destination("Clipboard", DestinationOrder.Clipboard)]
    public class ClipboardDestination : AbstractDestination
	{
	    private readonly ExportNotification _exportNotification;

        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        /// <param name="exportNotification">ExportNotification</param>
	    public ClipboardDestination(
	        ICoreConfiguration coreConfiguration,
	        IGreenshotLanguage greenshotLanguage,
	        ExportNotification exportNotification
            ) : base(coreConfiguration, greenshotLanguage)
	    {
	        _exportNotification = exportNotification;
	    }

        /// <inheritdoc />
	    public override string Description => GreenshotLanguage.SettingsDestinationClipboard;

        /// <inheritdoc />
	    public override Keys EditorShortcutKeys => Keys.Control | Keys.Shift | Keys.C;

        /// <inheritdoc />
	    public override IBitmapWithNativeSupport DisplayIcon => GreenshotResources.Instance.GetBitmap("Clipboard.Image");

        /// <inheritdoc />
	    protected override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
	    {
			var exportInformation = new ExportInformation(Designation, Description);
			try
			{
				using (var clipboardAccessToken = ClipboardNative.Access())
				{
					clipboardAccessToken.ClearContents();
                    // TODO: Test if this works
				    if (!string.IsNullOrEmpty(surface.LastSaveFullPath))
				    {
				        clipboardAccessToken.SetAsUnicodeString(surface.LastSaveFullPath);
                    }
                    
				    foreach (var clipboardFormat in CoreConfiguration.ClipboardFormats)
				    {
				        switch (clipboardFormat)
				        {
                            case ClipboardFormats.DIB:
                                clipboardAccessToken.SetAsDeviceIndependendBitmap(surface, CoreConfiguration);
                                break;
				            case ClipboardFormats.DIBV5:
				                clipboardAccessToken.SetAsFormat17(surface, CoreConfiguration);
				                break;
				            case ClipboardFormats.PNG:
				                clipboardAccessToken.SetAsBitmap(surface, new SurfaceOutputSettings(CoreConfiguration, OutputFormats.png));
				                break;
				            case ClipboardFormats.BITMAP:
				                clipboardAccessToken.SetAsBitmap(surface, new SurfaceOutputSettings(CoreConfiguration, OutputFormats.bmp));
				                break;
				            case ClipboardFormats.HTML:
				                clipboardAccessToken.SetAsHtml(surface, CoreConfiguration);
				                break;
				            case ClipboardFormats.HTMLDATAURL:
				                clipboardAccessToken.SetAsEmbeddedHtml(surface, CoreConfiguration);
				                break;
				        }
                    }
				}
				exportInformation.ExportMade = true;
			}
			catch (Exception)
			{
				// TODO: Change to general logic in ProcessExport
				surface.SendMessageEvent(this, SurfaceMessageTyp.Error, "Error"); //GreenshotLanguage.editorclipboardfailed);
			}
	        _exportNotification.NotifyOfExport(this, exportInformation, surface);
            return exportInformation;
		}
	}
}