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

using System.ComponentModel;
using System.Drawing;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;

namespace Greenshot.Plugin.Imgur
{
    /// <summary>
    /// Description of ImgurDestination.
    /// </summary>
    public class ImgurDestination : AbstractDestination
    {
        private readonly ImgurPlugin _plugin;

        public ImgurDestination(ImgurPlugin plugin)
        {
            _plugin = plugin;
        }

        public override string Designation => "Imgur";

        public override string Description => Language.GetString("imgur", LangKey.upload_menu_item);

        public override Image DisplayIcon
        {
            get
            {
                ComponentResourceManager resources = new ComponentResourceManager(typeof(ImgurPlugin));
                return (Image) resources.GetObject("Imgur");
            }
        }

        public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
        {
            ExportInformation exportInformation = new ExportInformation(Designation, Description)
            {
                ExportMade = _plugin.Upload(captureDetails, surface, out var uploadUrl),
                Uri = uploadUrl
            };
            ProcessExport(exportInformation, surface);
            return exportInformation;
        }
    }
}