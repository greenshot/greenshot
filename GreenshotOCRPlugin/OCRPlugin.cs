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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using GreenshotPlugin.Core;
using GreenshotPlugin.Core.Enums;
using GreenshotPlugin.Effects;
using GreenshotPlugin.IniFile;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;

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
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(OcrPlugin));
		private string _ocrCommand;
		private static OCRConfiguration config;
		private PluginAttribute _myAttributes;
		private ToolStripMenuItem _ocrMenuItem = new ToolStripMenuItem();

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (_ocrMenuItem != null) {
					_ocrMenuItem.Dispose();
					_ocrMenuItem = null;
				}
			}
		}

		public IEnumerable<IDestination> Destinations() {
			yield return new OCRDestination(this);
		}
		public IEnumerable<IProcessor> Processors() {
			yield break;
		}

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="greenshotHost">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="myAttributes">My own attributes</param>
		/// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
		public virtual bool Initialize(IGreenshotHost greenshotHost, PluginAttribute myAttributes) {
			Log.Debug("Initialize called of " + myAttributes.Name);
			_myAttributes = myAttributes;

			var ocrDirectory = Path.GetDirectoryName(myAttributes.DllFile);
			if (ocrDirectory == null)
			{
				return false;
			}
			_ocrCommand = Path.Combine(ocrDirectory, "greenshotocrcommand.exe");

			if (!HasModi()) {
				Log.Warn("No MODI found!");
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
			Log.Debug("Shutdown of " + _myAttributes.Name);
		}
		
		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public virtual void Configure() {
			if (!HasModi()) {
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


		private const int MinWidth = 130;
		private const int MinHeight = 130;
		/// <summary>
		/// Handling of the CaptureTaken "event" from the ICaptureHost
		/// We do the OCR here!
		/// </summary>
		/// <param name="surface">Has the Image and the capture details</param>
		public string DoOcr(ISurface surface) {
			SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(OutputFormats.bmp, 0, true)
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
				ProcessStartInfo processStartInfo = new ProcessStartInfo(_ocrCommand, "\"" + filePath + "\" " + config.Language + " " + config.Orientimage + " " + config.StraightenImage)
				{
					CreateNoWindow = true,
					RedirectStandardOutput = true,
					UseShellExecute = false
				};
				using (Process process = Process.Start(processStartInfo)) {
					if (process != null)
					{
						process.WaitForExit(30 * 1000);
						if (process.ExitCode == 0) {
							text = process.StandardOutput.ReadToEnd();
						}
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
			try {
				using (Process process = Process.Start(_ocrCommand, "-c")) {
					if (process != null)
					{
						process.WaitForExit();
						return process.ExitCode == 0;
					}
				}
			} catch(Exception e) {
				Log.DebugFormat("Error trying to initiate MODI: {0}", e.Message);
			}
			Log.InfoFormat("No Microsoft Office Document Imaging (MODI) found, disabling OCR");
			return false;
		}
	}
}