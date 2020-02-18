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

using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using GreenshotPlugin.Effects;

//using Microsoft.Win32;

namespace GreenshotOCRPlugin {
	/// <summary>
	/// OCR Plugin Greenshot
	/// </summary>
    [Plugin("Ocr", true)]
	public class OcrPlugin : IGreenshotPlugin {
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(OcrPlugin));
		private string _ocrCommand;
		private static OCRConfiguration _config;
		private ToolStripMenuItem _ocrMenuItem = new ToolStripMenuItem();

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (_ocrMenuItem == null) return;
            _ocrMenuItem.Dispose();
            _ocrMenuItem = null;
        }

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
		public bool Initialize() {
			Log.Debug("Initialize called");

			var ocrDirectory = Path.GetDirectoryName(GetType().Assembly.Location);
			if (ocrDirectory == null)
			{
				return false;
			}
			_ocrCommand = Path.Combine(ocrDirectory, "greenshotocrcommand.exe");

			if (!HasModi()) {
				Log.Warn("No MODI found!");
				return false;
			}
			// Provide the IDestination
            SimpleServiceProvider.Current.AddService(new OCRDestination(this));
			// Load configuration
			_config = IniConfig.GetIniSection<OCRConfiguration>();
			
			if (_config.Language != null) {
				_config.Language = _config.Language.Replace("miLANG_","").Replace("_"," ");
			}
			return true;
		}
		
		/// <summary>
		/// Implementation of the IGreenshotPlugin.Shutdown
		/// </summary>
		public void Shutdown() {
			Log.Debug("Shutdown");
		}
		
		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure() {
			if (!HasModi()) {
				MessageBox.Show("Sorry, is seems that Microsoft Office Document Imaging (MODI) is not installed, therefor the OCR Plugin cannot work.");
				return;
			}
			SettingsForm settingsForm = new SettingsForm(Enum.GetNames(typeof(ModiLanguage)), _config);
			DialogResult result = settingsForm.ShowDialog();
			if (result == DialogResult.OK) {
				// "Re"set hotkeys
				IniConfig.Save();
			}
		}


		private const int MinWidth = 130;
		private const int MinHeight = 130;
		/// <summary>
		/// Handling of the CaptureTaken "event" from the ICaptureHost
		/// We do the OCR here!
		/// </summary>
		/// <param name="surface">Has the Image and the capture details</param>
		public string DoOcr(ISurface surface) {
			SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(OutputFormat.bmp, 0, true)
			{
				ReduceColors = true,
				SaveBackgroundOnly = true
			};
			// We only want the background
			// Force Grayscale output
			outputSettings.Effects.Add(new GrayscaleEffect());

			// Also we need to check the size, resize if needed to 130x130 this is the minimum
			if (surface.Image.Width < MinWidth || surface.Image.Height < MinHeight) {
				int addedWidth = MinWidth - surface.Image.Width;
				if (addedWidth < 0) {
					addedWidth = 0;
				}
				int addedHeight = MinHeight - surface.Image.Height;
				if (addedHeight < 0) {
					addedHeight = 0;
				}
				IEffect effect = new ResizeCanvasEffect(addedWidth / 2, addedWidth / 2, addedHeight / 2, addedHeight / 2);
				outputSettings.Effects.Add(effect);
			}
			var filePath = ImageOutput.SaveToTmpFile(surface, outputSettings, null);

			Log.Debug("Saved tmp file to: " + filePath);

			string text = "";
			try {
				ProcessStartInfo processStartInfo = new ProcessStartInfo(_ocrCommand, "\"" + filePath + "\" " + _config.Language + " " + _config.Orientimage + " " + _config.StraightenImage)
				{
					CreateNoWindow = true,
					RedirectStandardOutput = true,
					UseShellExecute = false
				};
                using Process process = Process.Start(processStartInfo);
                if (process != null)
                {
                    process.WaitForExit(30 * 1000);
                    if (process.ExitCode == 0) {
                        text = process.StandardOutput.ReadToEnd();
                    }
                }
            } catch (Exception e) {
				Log.Error("Error while calling Microsoft Office Document Imaging (MODI) to OCR: ", e);
			} finally {
				if (File.Exists(filePath)) {
					Log.Debug("Cleaning up tmp file: " + filePath);
					File.Delete(filePath);
				}
			}

			if (string.IsNullOrEmpty(text)) {
				Log.Info("No text returned");
				return null;
			}

			// For for BUG-1884:
			text = text.Trim();

			try {
				Log.DebugFormat("Pasting OCR Text to Clipboard: {0}", text);
				// Paste to Clipboard (the Plugin currently doesn't have access to the ClipboardHelper from Greenshot
				IDataObject ido = new DataObject();
				ido.SetData(DataFormats.Text, true, text);
				Clipboard.SetDataObject(ido, true);
			} catch (Exception e) {
				Log.Error("Problem pasting text to clipboard: ", e);
			}
			return text;
		}

		private bool HasModi() {
			try
            {
                using Process process = Process.Start(_ocrCommand, "-c");
                if (process != null)
                {
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            } catch(Exception e) {
				Log.DebugFormat("Error trying to initiate MODI: {0}", e.Message);
			}
			Log.InfoFormat("No Microsoft Office Document Imaging (MODI) found, disabling OCR");
			return false;
		}
	}
}