/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2014 Thomas Braun, Jens Klingen, Robin Krom
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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using Greenshot.Core;

//using Microsoft.Win32;

namespace GreenshotOCR {
	// Needed for the drop down, available languages for OCR
	public enum ModiLanguage {
		CHINESE_SIMPLIFIED = 2052,
		CHINESE_TRADITIONAL = 1028,
		CZECH = 5,
		DANISH = 6,
		DUTCH = 19,
		ENGLISH = 9,
		FINNISH = 11,
		FRENCH = 12,
		GERMAN = 7,
		GREEK = 8,
		HUNGARIAN = 14,
		ITALIAN = 16,
		JAPANESE = 17,
		KOREAN = 18,
		NORWEGIAN = 20,
		POLISH = 21,
		PORTUGUESE = 22,
		RUSSIAN = 25,
		SPANISH = 10,
		SWEDISH = 29,
		TURKISH = 31,
		SYSDEFAULT = 2048
	}
	/// <summary>
	/// OCR Plugin Greenshot
	/// </summary>
	public class OcrPlugin : IGreenshotPlugin {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(OcrPlugin));
		private const string CONFIG_FILENAME = "ocr-config.properties";
		private string OCR_COMMAND;
		private static IGreenshotHost host;
		private static OCRConfiguration config;
		private PluginAttribute myAttributes;
		private ToolStripMenuItem ocrMenuItem = new ToolStripMenuItem();

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (ocrMenuItem != null) {
					ocrMenuItem.Dispose();
					ocrMenuItem = null;
				}
			}
		}

		public OcrPlugin() { }

		public IEnumerable<IDestination> Destinations() {
			yield return new OCRDestination(this);
		}
		public IEnumerable<IProcessor> Processors() {
			yield break;
		}

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="host">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="captureHost">Use the ICaptureHost interface to register in the MainContextMenu</param>
		/// <param name="pluginAttribute">My own attributes</param>
		/// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
		public virtual bool Initialize(IGreenshotHost greenshotHost, PluginAttribute myAttributes) {
			LOG.Debug("Initialize called of " + myAttributes.Name);
			host = greenshotHost;
			this.myAttributes = myAttributes;
			
			OCR_COMMAND = Path.Combine(Path.GetDirectoryName(myAttributes.DllFile), "greenshotocrcommand.exe");

			if (!HasMODI()) {
				LOG.Warn("No MODI found!");
				return false;
			}
			// Load configuration
			config = IniConfig.GetIniSection<OCRConfiguration>();
			
			if (config.Language != null) {
				config.Language = config.Language.Replace("miLANG_","").Replace("_"," ");
			}
			return true;
		}
		
		/// <summary>
		/// Implementation of the IGreenshotPlugin.Shutdown
		/// </summary>
		public void Shutdown() {
			LOG.Debug("Shutdown of " + myAttributes.Name);
		}
		
		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public virtual void Configure() {
			if (!HasMODI()) {
				MessageBox.Show("Sorry, is seems that Microsoft Office Document Imaging (MODI) is not installed, therefor the OCR Plugin cannot work.");
				return;
			}
			SettingsForm settingsForm = new SettingsForm(Enum.GetNames(typeof(ModiLanguage)), config);
			DialogResult result = settingsForm.ShowDialog();
			if (result == DialogResult.OK) {
				// "Re"set hotkeys
				IniConfig.Save();
			}
		}


		/// <summary>
		/// Handling of the CaptureTaken "event" from the ICaptureHost
		/// We do the OCR here!
		/// </summary>
		/// <param name="ImageOutputEventArgs">Has the Image and the capture details</param>
		private const int MIN_WIDTH = 130;
		private const int MIN_HEIGHT = 130;
		public string DoOCR(ISurface surface) {
			string filePath = null;
			SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(OutputFormat.bmp, 0, true);
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
			filePath = ImageOutput.SaveToTmpFile(surface, outputSettings, null);

			LOG.Debug("Saved tmp file to: " + filePath);

			string text = "";
			try {
				ProcessStartInfo processStartInfo = new ProcessStartInfo(OCR_COMMAND, "\"" + filePath + "\" " + config.Language + " " + config.Orientimage + " " + config.StraightenImage);
				processStartInfo.CreateNoWindow = true;
				processStartInfo.RedirectStandardOutput = true;
				processStartInfo.UseShellExecute = false;
				Process process = Process.Start(processStartInfo);
				process.WaitForExit(30*1000);
				if (process.ExitCode == 0) {
					text = process.StandardOutput.ReadToEnd();
				}
			} catch (Exception e) {
				LOG.Error("Error while calling Microsoft Office Document Imaging (MODI) to OCR: ", e);
			} finally {
				if (File.Exists(filePath)) {
					LOG.Debug("Cleaning up tmp file: " + filePath);
					File.Delete(filePath);
				}
			}

			if (text == null || text.Trim().Length == 0) {
				LOG.Info("No text returned");
				return null;
			}

			try {
				LOG.DebugFormat("Pasting OCR Text to Clipboard: {0}", text);
				// Paste to Clipboard (the Plugin currently doesn't have access to the ClipboardHelper from Greenshot
				IDataObject ido = new DataObject();
				ido.SetData(DataFormats.Text, true, text);
				Clipboard.SetDataObject(ido, true);
			} catch (Exception e) {
				LOG.Error("Problem pasting text to clipboard: ", e);
			}
			return text;
		}

		private bool HasMODI() {
			try {
				Process process = Process.Start(OCR_COMMAND, "-c");
				process.WaitForExit();
				int errorCode = process.ExitCode;
				return errorCode == 0;
			} catch(Exception e) {
				LOG.DebugFormat("Error trying to initiate MODI: {0}", e.Message);
			}
			LOG.InfoFormat("No Microsoft Office Document Imaging (MODI) found, disabling OCR");
			return false;
		}
	}
}