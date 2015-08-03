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

using Greenshot.IniFile;
using Greenshot.Plugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace GreenshotOCR {
	internal enum ModiLanguage {
// ReSharper disable InconsistentNaming
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
// ReSharper restore InconsistentNaming
	}

	/// <summary>
	/// OCR Plugin Greenshot
	/// </summary>
	public class OcrPlugin : IGreenshotPlugin {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(OcrPlugin));
		private string _ocrCommand;
		private static OCRConfiguration _config;
		private PluginAttribute _myAttributes;
		private System.Windows.Forms.ToolStripMenuItem _ocrMenuItem = new System.Windows.Forms.ToolStripMenuItem();

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
			yield return new OCRDestination(_ocrCommand);
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
		public bool Initialize(IGreenshotHost greenshotHost, PluginAttribute myAttributes) {
			LOG.Debug("Initialize called of " + myAttributes.Name);
			_myAttributes = myAttributes;

			var dllPath = Path.GetDirectoryName(myAttributes.DllFile);

			if (dllPath != null)
			{
				_ocrCommand = Path.Combine(dllPath, "greenshotocrcommand.exe");
			}

			if (!HasModi()) {
				LOG.Warn("No MODI found!");
				return false;
			}
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
			LOG.Debug("Shutdown of " + _myAttributes.Name);
		}
		
		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public virtual void Configure() {
			if (!HasModi()) {
				MessageBox.Show("Greenshot OCR", "Sorry, is seems that Microsoft Office Document Imaging (MODI) is not installed, therefor the OCR Plugin cannot work.", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			var settingsForm = new SettingsForm(Enum.GetNames(typeof(ModiLanguage)), _config);
			DialogResult result = settingsForm.ShowDialog();
			if (result == DialogResult.OK) {
				// "Re"set hotkeys
				IniConfig.Save();
			}
		}

		/// <summary>
		/// Check if MODI is installed and available
		/// </summary>
		/// <returns></returns>
		private bool HasModi() {
			try {
				using (var process = Process.Start(_ocrCommand, "-c")) {
					if (process != null)
					{
						// TODO: Can change to async...
						process.WaitForExit();
						return process.ExitCode == 0;
					}
				}
			} catch(Exception e) {
				LOG.DebugFormat("Error trying to initiate MODI: {0}", e.Message);
			}
			LOG.InfoFormat("No Microsoft Office Document Imaging (MODI) found, disabling OCR");
			return false;
		}
	}
}