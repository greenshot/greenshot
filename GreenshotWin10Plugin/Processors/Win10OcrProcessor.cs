/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Threading.Tasks;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Ocr;

namespace GreenshotWin10Plugin.Processors  {
	/// <summary>
	/// This processor processes a capture to see if there is text on it
	/// </summary>
	public class Win10OcrProcessor : AbstractProcessor {
        public override string Designation => "Windows10OcrProcessor";

        public override string Description => Designation;

        public override bool ProcessCapture(ISurface surface, ICaptureDetails captureDetails)
        {
            if (captureDetails.OcrInformation != null)
            {
                return false;
            }
            var ocrProvider = SimpleServiceProvider.Current.GetInstance<IOcrProvider>();

            var ocrResult = Task.Run(async () => await ocrProvider.DoOcrAsync(surface)).Result;

            if (!ocrResult.HasContent) return false;

            captureDetails.OcrInformation = ocrResult;

            return true;
        }
	}
}
