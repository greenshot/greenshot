/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using System.ComponentModel;
using System.Drawing;

namespace GreenshotWebDavPlugin
{
    /// <summary>
    /// This is the WebDAV Plugin destination code
    /// </summary>
    class WebDavDestination : AbstractDestination
    {
        private readonly WebDavPlugin _plugin;

        public WebDavDestination(WebDavPlugin plugin)
        {
            _plugin = plugin;
        }

        /// <summary>
        /// The name shown in the destinations menu
        /// </summary>
        public override string Designation => "WebDAV";

        /// <summary>
        /// The description shown in the destinations menu
        /// </summary>
        public override string Description => Language.GetString("webdav", LanguageKeys.upload_menu_item);

        /// <summary>
        /// The icon shown in the destinations menu
        /// </summary>
        public override Image DisplayIcon
        {
            get
            {
                ComponentResourceManager resources = new ComponentResourceManager(typeof(WebDavPlugin));
                return (Image)resources.GetObject("WebDAV");
            }
        }

        /// <summary>
        /// Export capture handler
        /// </summary>
        /// <param name="manuallyInitiated"></param>
        /// <param name="surface"></param>
        /// <param name="captureDetails"></param>
        /// <returns>An object containing if the upload was successful</returns>
        public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
        {
            ExportInformation exportInformation = new ExportInformation(Designation, Description)
            {
                ExportMade = _plugin.Upload(captureDetails, surface)
            };
            ProcessExport(exportInformation, surface);
            return exportInformation;
        }
    }
}
