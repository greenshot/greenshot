/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using GreenshotPlugin.UnmanagedHelpers;
using GreenshotPlugin.Core;
using Greenshot.Plugin;
using Greenshot.IniFile;

namespace Greenshot.Configuration {
	public enum ScreenshotDestinations {Editor=1, FileDefault=2, FileWithDialog=4, Clipboard=8, Printer=16, EMail=32}
	
	/// <summary>
	/// AppConfig is used for loading and saving the configuration. All public fields
	/// in this class are serialized with the BinaryFormatter and then saved to the
	/// config file. After loading the values from file, SetDefaults iterates over
	/// all public fields an sets fields set to null to the default value.
	/// </summary>
	[Serializable]
	public class AppConfig {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(AppConfig));
		private static readonly Regex FIXOLD_REGEXP = new Regex(@"%(?<variable>[\w]+)%", RegexOptions.Compiled);
		private const string VAR_PREFIX = "${";
		private const string VAR_POSTFIX = "}";

		//private static string loc = Assembly.GetExecutingAssembly().Location;
		//private static string oldFilename = Path.Combine(loc.Substring(0,loc.LastIndexOf(@"\")),"config.dat");
		private const string CONFIG_FILE_NAME = "config.dat";
		private static string configfilepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),@"Greenshot\");
		
		// the configuration part - all public vars are stored in the config file
		// don't use "null" and "0" as default value!
			
		#region general application config
		public bool? General_IsFirstLaunch = true;
		#endregion

		#region capture config
		public bool? Capture_Mousepointer = true;
		public bool? Capture_Windows_Interactive = false;
		public int Capture_Wait_Time = 101;
		public bool? fixedWaitTime = false;
		#endregion
		
		#region user interface config
		public string Ui_Language = "";
		public bool? Ui_Effects_CameraSound = true;
		#endregion
		
		#region output config
		public ScreenshotDestinations Output_Destinations = ScreenshotDestinations.Editor;
		
		
		public string Output_File_Path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
		public string Output_File_FilenamePattern = "${capturetime}_${title}";
		public string Output_File_Format = ImageFormat.Png.ToString();
		public bool? Output_File_CopyPathToClipboard = false;
		public int Output_File_JpegQuality = 80;
		public bool? Output_File_PromptJpegQuality = false;
		public int Output_File_IncrementingNumber = 1;
		
		public string Output_FileAs_Fullpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),"dummy.png");
		
		public bool? Output_Print_PromptOptions = true;
		public bool? Output_Print_AllowRotate = true;
		public bool? Output_Print_AllowEnlarge = true;
		public bool? Output_Print_AllowShrink = true;
		public bool? Output_Print_Center = true;
		public bool? Output_Print_Timestamp = true;
		#endregion
		
		#region editor config
		public WindowPlacement Editor_Placement;
		public Color[] Editor_RecentColors = new Color[12];
		public Font Editor_Font = null;
		                                                                                    
		#endregion
		
		/// <summary>
		/// a private constructor because this is a singleton
		/// </summary>
		private AppConfig()	{
		}
		
		/// <summary>
		/// Remove the old %VAR% syntax
		/// </summary>
		/// <param name="oldPattern">String with old syntax %VAR%</param>
		/// <returns>The fixed pattern</returns>
		private static string FixFallback(string oldPattern) {
				return FIXOLD_REGEXP.Replace(oldPattern, new MatchEvaluator(delegate(Match m) { return VAR_PREFIX + m.Groups["variable"].Value + VAR_POSTFIX;}));
		}

		/// <summary>
		/// loads the configuration from the config file
		/// </summary>
		/// <returns>an instance of AppConfig with all values set from the config file</returns>
		private static AppConfig Load() {
			AppConfig conf = null;
			if (File.Exists(Path.Combine(Application.StartupPath, CONFIG_FILE_NAME))) {
				configfilepath = Application.StartupPath;
			}
			string configfilename = Path.Combine(configfilepath, CONFIG_FILE_NAME);
			try {
				LOG.DebugFormat("Loading configuration from: {0}", configfilename);
				using (FileStream fileStream = File.Open(configfilename, FileMode.Open, FileAccess.Read)) {
					BinaryFormatter binaryFormatter = new BinaryFormatter();
					conf = (AppConfig) binaryFormatter.Deserialize(fileStream);
				}
				conf.Output_File_FilenamePattern = FixFallback(conf.Output_File_FilenamePattern);
				conf.Output_File_Path = FixFallback(conf.Output_File_Path);
			} catch (Exception e) {
				LOG.Warn("(ignoring) Problem loading configuration from: " + configfilename, e);
			}
			return conf;
		}
		
		public static void UpgradeToIni() {
			bool normalIni = File.Exists(Path.Combine(configfilepath, CONFIG_FILE_NAME));
			bool startupIni = File.Exists(Path.Combine(Application.StartupPath, CONFIG_FILE_NAME));
			if (startupIni || normalIni) {
				AppConfig appConfig = Load();
				
				if (appConfig != null) {
					LOG.Info("Migrating old configuration");
					CoreConfiguration coreConfiguration = IniConfig.GetIniSection<CoreConfiguration>();
					EditorConfiguration editorConfiguration = IniConfig.GetIniSection<EditorConfiguration>();
					// copy values
					try {
						coreConfiguration.OutputFileFilenamePattern = appConfig.Output_File_FilenamePattern;
						if (appConfig.Output_File_Format != null) {
							coreConfiguration.OutputFileFormat = (OutputFormat)Enum.Parse(typeof(OutputFormat), appConfig.Output_File_Format.ToLower());
						}
						coreConfiguration.OutputFileIncrementingNumber = unchecked((uint)appConfig.Output_File_IncrementingNumber);
						coreConfiguration.OutputFileJpegQuality = appConfig.Output_File_JpegQuality;
						coreConfiguration.OutputFilePath = appConfig.Output_File_Path;
						coreConfiguration.OutputFilePromptQuality = (bool)appConfig.Output_File_PromptJpegQuality;
						coreConfiguration.Language = appConfig.Ui_Language;
						coreConfiguration.PlayCameraSound = (bool)appConfig.Ui_Effects_CameraSound;
						coreConfiguration.CaptureMousepointer = (bool)appConfig.Capture_Mousepointer;
						coreConfiguration.OutputFileCopyPathToClipboard = (bool)appConfig.Output_File_CopyPathToClipboard;
						coreConfiguration.OutputPrintAllowEnlarge = (bool)appConfig.Output_Print_AllowEnlarge;
						coreConfiguration.OutputPrintAllowRotate = (bool)appConfig.Output_Print_AllowRotate;
						coreConfiguration.OutputPrintAllowShrink = (bool)appConfig.Output_Print_AllowShrink;
						coreConfiguration.OutputPrintCenter = (bool)appConfig.Output_Print_Center;
						coreConfiguration.OutputPrintPromptOptions = (bool)appConfig.Output_Print_PromptOptions;
						coreConfiguration.OutputPrintFooter = (bool)appConfig.Output_Print_Timestamp;
						int delay = appConfig.Capture_Wait_Time-1;
						if (delay < 0) {
							delay = 0;
						}
						coreConfiguration.CaptureDelay = delay;
						if ((appConfig.Output_Destinations & ScreenshotDestinations.Clipboard) == ScreenshotDestinations.Clipboard) {
							coreConfiguration.OutputDestinations.Add("Clipboard");
						}
						if ((appConfig.Output_Destinations & ScreenshotDestinations.Editor) == ScreenshotDestinations.Editor) {
							coreConfiguration.OutputDestinations.Add("Editor");
						}
						if ((appConfig.Output_Destinations & ScreenshotDestinations.EMail) == ScreenshotDestinations.EMail) {
							coreConfiguration.OutputDestinations.Add("EMail");
						}
						if ((appConfig.Output_Destinations & ScreenshotDestinations.Printer) == ScreenshotDestinations.Printer) {
							coreConfiguration.OutputDestinations.Add("Printer");
						}
						if ((appConfig.Output_Destinations & ScreenshotDestinations.FileDefault) == ScreenshotDestinations.FileDefault) {
							coreConfiguration.OutputDestinations.Add("File");
						}
						if ((appConfig.Output_Destinations & ScreenshotDestinations.FileWithDialog) == ScreenshotDestinations.FileWithDialog) {
							coreConfiguration.OutputDestinations.Add("FileWithDialog");
						}
						IniConfig.Save();
					} catch (Exception e) {
						LOG.Error(e);
					}
				}
				try {
					LOG.Info("Deleting old configuration");
					File.Delete(Path.Combine(configfilepath, CONFIG_FILE_NAME));
				} catch (Exception e) {
					LOG.Error(e);
				}
			}
		}

		/// <summary>
		/// Checks for the existence of a configuration file.
		/// First in greenshot's Applicationdata folder (where it is stored since 0.6),
		/// then (if it cannot be found there) in greenshot's program directory (where older 
		/// versions might have stored it).
		/// If the latter is the case, the file is moved to the new location, so that a user does not lose
		/// their configuration after upgrading. 
		/// If there is no file in both locations, a virgin config file is created.
		/// </summary>
		private static void CheckConfigFile() {
			// check if file is in the same location as started from, if this is the case
			// we will use this file instead of the ApplicationDate folder
			// Done for Feature Request #2741508
			if (File.Exists(Path.Combine(Application.StartupPath, CONFIG_FILE_NAME))) {
				configfilepath = Application.StartupPath;
			}
		}
	}
}
