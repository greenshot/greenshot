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

using System;
using System.Drawing;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Forms;
using System.Windows.Forms;

namespace Greenshot.Destinations
{
    /// <summary>
    /// This uses the Share from Windows 10 to make the capture available to apps.
    /// </summary>
    public class Win10ShareDestination : AbstractDestination
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Win10ShareDestination));

        public override string Designation { get; } = "Windows10Share";
        public override string Description { get; } = "Windows 10 share";

        /// <summary>
        /// Icon for the App-share, the icon was found via: https://help4windows.com/windows_8_shell32_dll.shtml
        /// </summary>
        public override Image DisplayIcon => PluginUtils.GetCachedExeIcon(FilenameHelper.FillCmdVariables(@"%windir%\system32\shell32.dll"), 238);

        /// <summary>
        /// Share the screenshot with a windows app
        /// </summary>
        /// <param name="manuallyInitiated">bool</param>
        /// <param name="surface">ISurface</param>
        /// <param name="captureDetails">ICaptureDetails</param>
        /// <returns>ExportInformation</returns>
        public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
        {
            var exportInformation = new ExportInformation(Designation, Description);
            try
            {
                var sharingForm = new SharingForm(surface, captureDetails);
                var result = sharingForm.ShowDialog();

                if (result == DialogResult.Abort)
                {
                    exportInformation.ExportMade = false;
                }
                if (result == DialogResult.OK)
                {
                    exportInformation.ExportMade = true;
                    exportInformation.DestinationDescription = sharingForm.AppName;
                }
            }
            catch (Exception ex)
            {
                exportInformation.ExportMade = false;
                exportInformation.ErrorMessage = ex.Message;
            }

            ProcessExport(exportInformation, surface);
            return exportInformation;
        }
    }
}