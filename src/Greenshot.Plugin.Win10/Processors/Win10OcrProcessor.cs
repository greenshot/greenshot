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

using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;

namespace Greenshot.Plugin.Win10.Processors
{
    /// <summary>
    /// This processor processes a capture to see if there is text on it
    /// </summary>
    public class Win10OcrProcessor : AbstractProcessor
    {
        private static readonly Win10Configuration Win10Configuration = IniConfig.GetIniSection<Win10Configuration>();
        public override string Designation => "Windows10OcrProcessor";

        public override string Description => Designation;

        public override bool ProcessCapture(ISurface surface, ICaptureDetails captureDetails)
        {
            if (!Win10Configuration.AlwaysRunOCROnCapture)
            {
                return false;
            }

            if (surface == null)
            {
                return false;
            }

            if (captureDetails == null || captureDetails.OcrInformation != null)
            {
                return false;
            }

            var ocrProvider = SimpleServiceProvider.Current.GetInstance<IOcrProvider>();

            if (ocrProvider == null)
            {
                return false;
            }

            var ocrResult = Task.Run(async () => await ocrProvider.DoOcrAsync(surface)).Result;

            if (!ocrResult.HasContent)
            {
                return false;
            }

            captureDetails.OcrInformation = ocrResult;

            return true;
        }
    }
}