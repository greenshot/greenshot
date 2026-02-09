/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Drawing;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Configuration;
using Greenshot.Forms;

namespace Greenshot.Destinations
{
    /// <summary>
    /// Description of ViewerDestination.
    /// </summary>
    public class ViewerDestination : AbstractDestination
    {
        public override string Designation => nameof(WellKnownDestinations.Viewer);

        public override string Description => Language.GetString(LangKey.settings_destination_viewer);

        public override int Priority => 3;

        public override Image DisplayIcon => GreenshotResources.GetImage("Image.Image");

        public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
        {
            var exportInformation = new ExportInformation(Designation, Description);
            
            // Create and show the viewer form
            var viewerForm = new ViewerForm(surface, captureDetails);
            viewerForm.Show();
            
            exportInformation.ExportMade = true;
            ProcessExport(exportInformation, surface);
            
            return exportInformation;
        }
    }
}
