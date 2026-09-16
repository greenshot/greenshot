/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Collections.Generic;
using System.Drawing;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Plugin.Imgur
{
    public class ImgurDestination : AbstractDestination
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ImgurDestination));

        public override string Designation => "Imgur";

        public override string Description => Language.GetString("imgur", LangKey.upload_menu_item) ?? "Upload to Imgur";

        public override Image DisplayIcon => null;

        public override IEnumerable<IDestination> DynamicDestinations()
        {
            yield break;
        }

        public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
        {
            var exportInformation = new ExportInformation(Designation, Description);
            var outputSettings = new SurfaceOutputSettings(OutputFormat.png, 90, false);

            var info = ImgurStep.UploadToImgur(surface, captureDetails, outputSettings, captureDetails.Title, null);
            if (info != null && !string.IsNullOrEmpty(info.Original))
            {
                exportInformation.ExportMade = true;
                exportInformation.Uri = info.Original;
                surface.UploadUrl = info.Original;
                ProcessExport(exportInformation, surface);
            }
            else
            {
                exportInformation.ExportMade = false;
                exportInformation.ErrorMessage = "Imgur upload failed";
            }

            return exportInformation;
        }
    }
}
