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

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using GreenshotPlugin.Core;
using GreenshotPlugin.IniFile;
using GreenshotPlugin.Interfaces;
using log4net;
using ZXing;

namespace Greenshot.Processors  {
	/// <summary>
	/// This processor processes a capture to see if there is a QR ode on it
	/// </summary>
	public class ZXingQrProcessor : AbstractProcessor {
		private static readonly ILog LOG = LogManager.GetLogger(typeof(TitleFixProcessor));
		private static readonly CoreConfiguration config = IniConfig.GetIniSection<CoreConfiguration>();

		public ZXingQrProcessor() {

        }

		public override string Designation => "QRProcessor";

        public override string Description => Designation;

        public override bool ProcessCapture(ISurface surface, ICaptureDetails captureDetails) {
            // create a barcode reader instance
            IBarcodeReader reader = new BarcodeReader();
            // detect and decode the barcode inside the bitmap
            var result = reader.Decode((Bitmap)surface.Image);
            // do something with the result
            if (result != null)
            {
                LOG.InfoFormat("Found QR of type {0} - {1}", result.BarcodeFormat, result.Text);
                captureDetails.QrResult = result;
                return true;
            }
            return false;
		}
	}
}
