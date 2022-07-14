/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.IO;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Plugin.Office.OfficeExport;
using Greenshot.Plugin.Office.OfficeExport.Entities;
using System.Linq;

namespace Greenshot.Plugin.Office.Destinations
{
    public class OneNoteDestination : AbstractDestination
    {
        private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(WordDestination));
        private const int ICON_APPLICATION = 0;
        public const string DESIGNATION = "OneNote";
        private static readonly string exePath;
        private readonly OneNotePage page;
        private readonly OneNoteExporter _oneNoteExporter = new();

        static OneNoteDestination()
        {
            exePath = PluginUtils.GetExePath("ONENOTE.EXE");
            if (exePath != null && File.Exists(exePath))
            {
                WindowDetails.AddProcessToExcludeFromFreeze("onenote");
            }
            else
            {
                exePath = null;
            }
        }

        public OneNoteDestination()
        {
        }

        public OneNoteDestination(OneNotePage page) => this.page = page;

        public override string Designation => DESIGNATION;

        public override string Description => page == null ? "Microsoft OneNote" : page.DisplayName;

        public override int Priority => 4;

        public override bool IsDynamic => true;

        public override bool IsActive => base.IsActive && exePath != null;

        public override Image DisplayIcon => PluginUtils.GetCachedExeIcon(exePath, ICON_APPLICATION);

        public override IEnumerable<IDestination> DynamicDestinations()
        {
            return from OneNotePage page in _oneNoteExporter.GetPages()
                   select new OneNoteDestination(page);
        }

        public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
        {
            ExportInformation exportInformation = new(Designation, Description);

            if (page == null)
            {
                try
                {
                    exportInformation.ExportMade = _oneNoteExporter.ExportToNewPage(surface);
                }
                catch (Exception ex)
                {
                    exportInformation.ErrorMessage = ex.Message;
                    LOG.Error(ex);
                }
            }
            else
            {
                try
                {
                    exportInformation.ExportMade = _oneNoteExporter.ExportToPage(surface, page);
                }
                catch (Exception ex)
                {
                    exportInformation.ErrorMessage = ex.Message;
                    LOG.Error(ex);
                }
            }

            return exportInformation;
        }
    }
}