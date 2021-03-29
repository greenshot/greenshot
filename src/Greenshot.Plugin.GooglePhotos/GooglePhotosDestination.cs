/*
 * A GooglePhotos Plugin for Greenshot
 * Copyright (C) 2011  Francis Noel
 * 
 * For more information see: https://getgreenshot.org/
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

using System.ComponentModel;
using System.Drawing;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;

namespace Greenshot.Plugin.GooglePhotos
{
    public class GooglePhotosDestination : AbstractDestination
    {
        private readonly GooglePhotosPlugin _plugin;

        public GooglePhotosDestination(GooglePhotosPlugin plugin)
        {
            _plugin = plugin;
        }

        public override string Designation => "GooglePhotos";

        public override string Description => Language.GetString("googlephotos", LangKey.upload_menu_item);

        public override Image DisplayIcon
        {
            get
            {
                ComponentResourceManager resources = new ComponentResourceManager(typeof(GooglePhotosPlugin));
                return (Image) resources.GetObject("GooglePhotos");
            }
        }

        public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
        {
            ExportInformation exportInformation = new ExportInformation(Designation, Description);
            bool uploaded = _plugin.Upload(captureDetails, surface, out var uploadUrl);
            if (uploaded)
            {
                exportInformation.ExportMade = true;
                exportInformation.Uri = uploadUrl;
            }

            ProcessExport(exportInformation, surface);
            return exportInformation;
        }
    }
}