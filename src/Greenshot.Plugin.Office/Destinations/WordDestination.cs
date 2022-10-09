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
using System.Linq;
using System.Text.RegularExpressions;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Plugin.Office.OfficeExport;

namespace Greenshot.Plugin.Office.Destinations
{
    /// <summary>
    /// Description of EmailDestination.
    /// </summary>
    public class WordDestination : AbstractDestination
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(WordDestination));
        private const int IconApplication = 0;
        private const int IconDocument = 1;
        private static readonly string ExePath;
        private readonly string _documentCaption;
        private readonly WordExporter _wordExporter = new WordExporter();

        static WordDestination()
        {
            ExePath = OfficeUtils.GetOfficeExePath("WINWORD.EXE") ?? PluginUtils.GetExePath("WINWORD.EXE");
            if (ExePath != null && !File.Exists(ExePath))
            {
                ExePath = null;
            }
        }

        public WordDestination()
        {
        }

        public WordDestination(string wordCaption)
        {
            _documentCaption = wordCaption;
        }

        public override string Designation => "Word";

        public override string Description => _documentCaption ?? "Microsoft Word";

        public override int Priority => 4;

        public override bool IsDynamic => true;

        public override bool IsActive => base.IsActive && ExePath != null;

        public override Image DisplayIcon => PluginUtils.GetCachedExeIcon(ExePath, !string.IsNullOrEmpty(_documentCaption) ? IconDocument : IconApplication);

        public override IEnumerable<IDestination> DynamicDestinations()
        {
            foreach (string wordCaption in _wordExporter.GetWordDocuments())
            {
                yield return new WordDestination(wordCaption);
            }
        }

        public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
        {
            ExportInformation exportInformation = new ExportInformation(Designation, Description);
            string tmpFile = captureDetails.Filename;
            if (tmpFile == null || surface.Modified || !Regex.IsMatch(tmpFile, @".*(\.png|\.gif|\.jpg|\.jpeg|\.tiff|\.bmp)$"))
            {
                tmpFile = ImageIO.SaveNamedTmpFile(surface, captureDetails, new SurfaceOutputSettings().PreventGreenshotFormat());
            }

            if (_documentCaption != null)
            {
                try
                {
                    _wordExporter.InsertIntoExistingDocument(_documentCaption, tmpFile);
                    exportInformation.ExportMade = true;
                }
                catch (Exception)
                {
                    try
                    {
                        _wordExporter.InsertIntoExistingDocument(_documentCaption, tmpFile);
                        exportInformation.ExportMade = true;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                        // TODO: Change to general logic in ProcessExport
                        surface.SendMessageEvent(this, SurfaceMessageTyp.Error, Language.GetFormattedString("destination_exportfailed", Description));
                    }
                }
            }
            else
            {
                if (!manuallyInitiated)
                {
                    var documents = _wordExporter.GetWordDocuments().ToList();
                    if (documents is { Count: > 0 })
                    {
                        var destinations = new List<IDestination>
                        {
                            new WordDestination()
                        };
                        foreach (string document in documents)
                        {
                            destinations.Add(new WordDestination(document));
                        }

                        // Return the ExportInformation from the picker without processing, as this indirectly comes from us self
                        return ShowPickerMenu(false, surface, captureDetails, destinations);
                    }
                }

                try
                {
                    _wordExporter.InsertIntoNewDocument(tmpFile, null, null);
                    exportInformation.ExportMade = true;
                }
                catch (Exception)
                {
                    // Retry once, just in case
                    try
                    {
                        _wordExporter.InsertIntoNewDocument(tmpFile, null, null);
                        exportInformation.ExportMade = true;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                        // TODO: Change to general logic in ProcessExport
                        surface.SendMessageEvent(this, SurfaceMessageTyp.Error, Language.GetFormattedString("destination_exportfailed", Description));
                    }
                }
            }

            ProcessExport(exportInformation, surface);
            return exportInformation;
        }
    }
}