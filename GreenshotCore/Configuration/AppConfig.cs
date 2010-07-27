/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows.Forms;

using Greenshot.Core;
using Greenshot.Drawing;
using Greenshot.Drawing.Fields;

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

		//private static string loc = Assembly.GetExecutingAssembly().Location;
		//private static string oldFilename = Path.Combine(loc.Substring(0,loc.LastIndexOf(@"\")),"config.dat");
		private const string CONFIG_FILE_NAME = "config.dat";
		private static string configfilepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),@"Greenshot\");
		private static AppConfig instance = null;
		
		public Dictionary<string, object> LastUsedFieldValues = new Dictionary<string, object>();
		
		// the configuration part - all public vars are stored in the config file
		// don't use "null" and "0" as default value!
			
		#region general application config
		public bool? General_RegisterHotkeys = true;
		public bool? General_IsFirstLaunch = true;
		#endregion
		
		#region capture config
		public bool? Capture_Mousepointer = true;
		public bool? Capture_Windows_Interactive = false;
		public int Capture_Wait_Time = 100;
		public bool? Capture_Complete_Window = false;
		public bool? Capture_Window_Content = false;
		#endregion
		
		#region user interface config
		public string Ui_Language = "";
		public bool? Ui_Effects_Flashlight = false;
		public bool? Ui_Effects_CameraSound = true;
		#endregion
		
		#region output config
		public ScreenshotDestinations Output_Destinations = ScreenshotDestinations.Editor;
		
		
		public string Output_File_Path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
		public string Output_File_FilenamePattern = "%title%_%YYYY%-%MM%-%DD%_%hh%-%mm%-%ss%";
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
		public Size Editor_WindowSize = new Size(540, 380);
		public Point Editor_WindowLocation = new Point(100, 100);
		public String Editor_WindowState = "Normal";
		public Rectangle Editor_Previous_Screenbounds = Rectangle.Empty;
		public Color[] Editor_RecentColors = new Color[12];
		public Font Editor_Font = null;
		                                                                                    
		#endregion
		
		/// <summary>
		/// a private constructor because this is a singleton
		/// </summary>
		private AppConfig()	{
		}
		
		/// <summary>
		/// get an instance of AppConfig
		/// </summary>
		/// <returns></returns>
		public static AppConfig GetInstance() {
			if (instance == null) {
				instance = Load();
			}
			return instance;
		}
		
		public static void Reload() {
			AppConfig newInstance = Load();
			instance.Copy(newInstance);
		}

		/// <summary>
		/// loads the configuration from the config file
		/// </summary>
		/// <returns>an instance of AppConfig with all values set from the config file</returns>
		private static AppConfig Load() {
			AppConfig conf;
			CheckConfigFile();
			string configfilename = Path.Combine(configfilepath, CONFIG_FILE_NAME);
			try {
				LOG.Debug("Loading configuration from: " + configfilename);
				using (FileStream fileStream = File.Open(configfilename, FileMode.Open, FileAccess.Read)) {
					BinaryFormatter binaryFormatter = new BinaryFormatter();
					conf = (AppConfig) binaryFormatter.Deserialize(fileStream);
				}
				conf.SetDefaults();
				return conf;
			} catch (SerializationException e) {
				LOG.Error("Problem loading configuration from: " + configfilename, e);
				AppConfig config = new AppConfig();
				config.Store();
				return config;
			} catch (Exception e) {
				LOG.Error("Problem loading configuration from: " + configfilename, e);
				MessageBox.Show(String.Format("Could not load Greenshot's configuration file. Please check access permissions for '{0}'.\n",configfilename),"Error");
				Process.GetCurrentProcess().Kill();
			}
			return null;
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
			// we will use this file instead of the Applicationdate folder
			// Done for Feature Request #2741508
			if (File.Exists(Path.Combine(Application.StartupPath, CONFIG_FILE_NAME))) {
				configfilepath = Application.StartupPath;
			} else if (!File.Exists(Path.Combine(configfilepath, CONFIG_FILE_NAME))) {
				Directory.CreateDirectory(configfilepath);
				new AppConfig().Store();
			}
		}

		/// <summary>
		/// saves the configuration values to the supplied config file
		/// </summary>
		public void Store() {
			Store(configfilepath);
		}

		/// <summary>
		/// saves the configuration values to the config path
		/// </summary>
		public void Store(string configpath) {
			string configfilename = Path.Combine(configpath, CONFIG_FILE_NAME);
			try {
				LOG.Debug("Saving configuration to: " + configfilename);
				using (FileStream fileStream = File.Open(configfilename, FileMode.Create)) {
					BinaryFormatter formatter = new BinaryFormatter();
					formatter.Serialize(fileStream, this);
				}
			} catch (UnauthorizedAccessException e) {
				LOG.Error("Problem saving configuration to: " + configfilename, e);
				MessageBox.Show(Language.GetInstance().GetFormattedString(LangKey.config_unauthorizedaccess_write,configfilename),Language.GetInstance().GetString(LangKey.error));
			}
		}
		
		/// <summary>
		/// when new fields are added to this class, they are instanced
		/// with null by default. this method iterates over all public
		/// fields and uses reflection to set them to the proper default value.
		/// </summary>
		public void SetDefaults() {
			Type type = this.GetType();
			FieldInfo[] fieldInfos = type.GetFields();
			foreach (FieldInfo fi in fieldInfos) {
				object o = fi.GetValue(this);
				int i;
				if (o == null || (int.TryParse(o.ToString(), out i) && i == 0)) {
					// found field with value null. setting to default.
					AppConfig tmpConf = new AppConfig();
					Type tmpType = tmpConf.GetType();
					FieldInfo defaultField = tmpType.GetField(fi.Name);
					fi.SetValue(this, defaultField.GetValue(tmpConf));
				}
			}
		}

		private void Copy(AppConfig newConfig) {
			Type type = this.GetType();

			// Copy fields
			FieldInfo[] fieldInfos = type.GetFields();
			foreach (FieldInfo fi in fieldInfos) {
				object newValue = fi.GetValue(newConfig);
				fi.SetValue(this, newValue);
			}
			// Update language
			if (newConfig.Ui_Language != null && !newConfig.Ui_Language.Equals(Language.GetInstance().CurrentLanguage)) {
				string newLang = Language.GetInstance().SetLanguage(newConfig.Ui_Language);
				// check if the language was not wat was supplied (near match)
				if (newConfig.Ui_Language.Equals(newLang)) {
					// Store change
					this.Store();
				}
			}
		}

		public static Properties GetAvailableProperties() {
			Properties properties = new Properties();
			Type type = typeof(AppConfig);
			FieldInfo[] fieldInfos = type.GetFields();
			foreach (FieldInfo fi in fieldInfos) {
				Type fieldType = fi.FieldType;
				if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition().Equals(typeof(Nullable<>))) {
					// We are dealing with a generic type that is nullable
					fieldType = Nullable.GetUnderlyingType(fieldType);
				}
				if (fieldType == typeof(string) || fieldType == typeof(bool) || fieldType == typeof(int)) {
					properties.AddProperty(fi.Name, fieldType.Name);
				}
			}
			return properties;
		}

		public void SetProperties(Properties properties) {
			Type type = this.GetType();
			FieldInfo[] fieldInfos = type.GetFields();
			foreach(string key in properties.Keys) {
				FieldInfo currentField = type.GetField(key);
				if (currentField != null) {
					object currentValue = currentField.GetValue(this);
					LOG.Debug("Before: " + currentField.Name + "=" + currentValue);
					if (currentField.FieldType == typeof(string)) {
						currentField.SetValue(this, properties.GetProperty(key));
					} else if (currentField.FieldType == typeof(bool) ||currentField.FieldType == typeof(bool?)) {
						currentField.SetValue(this, properties.GetBoolProperty(key));
					} else if (currentField.FieldType == typeof(int) || currentField.FieldType == typeof(int?)) {
						currentField.SetValue(this, properties.GetIntProperty(key));
					}
					LOG.Debug("After: " + currentField.Name + "=" + currentField.GetValue(this));
				} else {
					LOG.Warn("Configuration for " + key + " not found! (Incorrect key?)");
				}
			}
		}
		
		public void UpdateLastUsedFieldValue(IField f) {
			if(f.Value != null) {
				string key = GetKeyForField(f);
				LastUsedFieldValues[key] = f.Value;
			}
		}
		
		public IField GetLastUsedValueForField(IField f) {
			string key = GetKeyForField(f);
			if(LastUsedFieldValues.ContainsKey(key)) {
				f.Value = LastUsedFieldValues[key];
			} 
			return f;
		}
		
		/// <param name="f"></param>
		/// <returns></returns>
		/// <param name="f"></param>
		/// <returns>the key under which last used value for the Field can be stored/retrieved</returns>
		private string GetKeyForField(IField f) {
			if(f.Scope == null) {
				return f.FieldType.ToString();
			} else {
				return f.FieldType.ToString() + "-" + f.Scope;
			}
		}
	}
}
