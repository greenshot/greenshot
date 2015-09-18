/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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

using Dapplo.Config.Ini;
using Greenshot.Core;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using GreenshotPlugin.Extensions;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GreenshotOCR {
	/// <summary>
	/// Description of OCRDestination.
	/// </summary>
	public class OCRDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(OCRDestination));
		private static IOCRConfiguration _config = IniConfig.Current.Get<IOCRConfiguration>();
		private const int MIN_WIDTH = 130;
		private const int MIN_HEIGHT = 130;
		private string _ocrCommand;

		public override string Designation {
			get {
				return "OCR";
			}
		}

		public override string Description {
			get {
				return "OCR";
			}
		}

		public override Image DisplayIcon {
			get {
				string exePath = PluginUtils.GetExePath("MSPVIEW.EXE");
				if (exePath != null && File.Exists(exePath)) {
					return PluginUtils.GetCachedExeIcon(exePath, 0);
				}
				return null;
			}
		}

		public OCRDestination(string ocrCommand) {
			_ocrCommand = ocrCommand;
		}

		public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails, CancellationToken token = default(CancellationToken)) {
			var exportInformation = new ExportInformation {
				DestinationDesignation = Designation,
				DestinationDescription = Description
			};
			exportInformation.ExportMade = await DoOcrAsync(surface, token) != null;
			return exportInformation;
		}

		/// <summary>
		/// Handling of the CaptureTaken "event" from the ICaptureHost
		/// We do the OCR here!
		/// </summary>
		/// <param name="surface">Has the Image</param>
		private async Task<string> DoOcrAsync(ISurface surface, CancellationToken token = default(CancellationToken)) {
			var outputSettings = new SurfaceOutputSettings(OutputFormat.bmp, 0, true);
			outputSettings.ReduceColors = true;
			// We only want the background
			outputSettings.SaveBackgroundOnly = true;
			// Force Grayscale output
			outputSettings.Effects.Add(new GrayscaleEffect());

			// Also we need to check the size, resize if needed to 130x130 this is the minimum
			if (surface.Image.Width < MIN_WIDTH || surface.Image.Height < MIN_HEIGHT) {
				int addedWidth = MIN_WIDTH - surface.Image.Width;
				if (addedWidth < 0) {
					addedWidth = 0;
				}
				int addedHeight = MIN_HEIGHT - surface.Image.Height;
				if (addedHeight < 0) {
					addedHeight = 0;
				}
				IEffect effect = new ResizeCanvasEffect(addedWidth / 2, addedWidth / 2, addedHeight / 2, addedHeight / 2);
				outputSettings.Effects.Add(effect);
			}
			string filePath = ImageOutput.SaveToTmpFile(surface, outputSettings, null);

			LOG.Debug("Saved tmp file to: " + filePath);

			string text = "";
			try {
				var processStartInfo = new ProcessStartInfo(_ocrCommand, "\"" + filePath + "\" " + _config.Language + " " + _config.Orientimage + " " + _config.StraightenImage) {
					CreateNoWindow = true,
					RedirectStandardOutput = true,
					UseShellExecute = false
				};
				using (Process process = Process.Start(processStartInfo)) {
					if (process != null) {
						await process.WaitForExitAsync(token);
						if (process.ExitCode == 0) {
							text = process.StandardOutput.ReadToEnd();
						}
					}
				}
			} catch (Exception e) {
				LOG.Error("Error while calling Microsoft Office Document Imaging (MODI) to OCR: ", e);
			} finally {
				if (File.Exists(filePath)) {
					LOG.Debug("Cleaning up tmp file: " + filePath);
					File.Delete(filePath);
				}
			}

			if (text.Trim().Length == 0) {
				LOG.Info("No text returned");
				return null;
			}

			try {
				LOG.DebugFormat("Pasting OCR Text to Clipboard: {0}", text);
				ClipboardHelper.SetClipboardData(text);
			} catch (Exception e) {
				LOG.Error("Problem pasting text to clipboard: ", e);
			}
			return text;
		}

	}
}
