/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Greenshot.IniFile {
	public class IniConfig {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(IniConfig));
		private const string INI_EXTENSION = ".ini";
		private const string DEFAULTS_POSTFIX = "-defaults";
		private const string FIXED_POSTFIX = "-fixed";

		private static FileSystemWatcher watcher;
		private static string applicationName = null;
		private static string configName = null;

		private static Dictionary<string, IniSection> sectionMap = new Dictionary<string, IniSection>();
		private static Dictionary<string, Dictionary<string, string>> sections = new Dictionary<string, Dictionary<string, string>>();
		public static event FileSystemEventHandler IniChanged;
		private static bool portableCheckMade = false;

		private static bool portable = false;
		public static bool IsPortable {
			get {
				return portable;
			}
		}

		/// <summary>
		/// Initialize the ini config
		/// </summary>
		/// <param name="applicationName"></param>
		/// <param name="configName"></param>
		public static void Init(string appName, string confName) {
			applicationName = appName;
			configName = confName;
			Reload();
			WatchConfigFile(true);
		}

		public static bool IsInited {
			get {
				return applicationName != null && configName != null;
			}
		}

		public static void ForceIniInStartupPath() {
			if (portableCheckMade) {
				throw new Exception("ForceLocal should be called before any file is read");
			}
			portable = false;
			portableCheckMade = true;
			string applicationStartupPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			if (applicationName == null || configName == null) {
				Init();
			}
			string forcedIni =  Path.Combine(applicationStartupPath, applicationName + INI_EXTENSION);
			if (!File.Exists(forcedIni)) {
				using (File.Create(forcedIni)) {}
			}
		}

		/// <summary>
		/// Default init
		/// </summary>
		public static void Init() {
			AssemblyProductAttribute[] assemblyProductAttributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false) as AssemblyProductAttribute[];
			if (assemblyProductAttributes.Length > 0) {
				string productName = assemblyProductAttributes[0].Product;
				LOG.InfoFormat("Using ProductName {0}", productName);
				Init(productName, productName);
			} else {
				throw new InvalidOperationException("Assembly ProductName not set.");
			}
		}

		/// <summary>
		/// Enable watching the configuration
		/// </summary>
		/// <param name="sendEvents"></param>
		private static void WatchConfigFile(bool sendEvents) {
			string iniLocation = CreateIniLocation(configName + INI_EXTENSION);
			// Wait with watching until the file is there
			if (Directory.Exists(Path.GetDirectoryName(iniLocation))) {
				if (watcher == null) {
					//LOG.DebugFormat("Starting FileSystemWatcher for {0}", iniLocation);
					// Monitor the ini file
					watcher = new FileSystemWatcher();
					watcher.Path = Path.GetDirectoryName(iniLocation);
					watcher.Filter = "greenshot.ini";
					watcher.NotifyFilter = NotifyFilters.LastWrite;
					watcher.Changed += new FileSystemEventHandler(ConfigFileChanged);
				}
			}
			if (watcher != null) {
				watcher.EnableRaisingEvents = sendEvents;
			}
		}

		/// <summary>
		/// Get the location of the configuration
		/// </summary>
		public static string ConfigLocation {
			get {
				return CreateIniLocation(configName + INI_EXTENSION);
			}
		}

		/// <summary>
		/// Called by the filesystem watcher when a file is changed.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="e"></param>
		private static void ConfigFileChanged(object source, FileSystemEventArgs e) {
			string iniLocation = CreateIniLocation(configName + INI_EXTENSION);
			if (iniLocation.Equals(e.FullPath)) {
				LOG.InfoFormat("Config file {0} was changed, reloading", e.FullPath);

				// Try to reread the configuration
				int retries = 10;
				bool configRead = false;
				while (!configRead && retries != 0) {
					try {
						IniConfig.Reload();
						configRead = true;
					} catch (IOException) {
						retries--;
						Thread.Sleep(100);
					}
				}

				if (configRead && IniChanged != null) {
					IniChanged.Invoke(source, e);
				}
			}
		}
	
		/// <summary>
		/// Create the location of the configuration file
		/// </summary>
		private static string CreateIniLocation(string configFilename) {
			if (applicationName == null || configName == null) {
				throw new InvalidOperationException("IniConfig.Init not called!");
			}
			string iniFilePath = null;
			string applicationStartupPath = "";
			try {
				applicationStartupPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			} catch (Exception exception) {
				LOG.WarnFormat("Problem retrieving the AssemblyLocation: {0} (Designer mode?)", exception.Message);
				applicationStartupPath = @".";
			}
			string pafPath = Path.Combine(applicationStartupPath, @"App\" + applicationName);
			
			if (portable || !portableCheckMade) {
				if (!portable) {
					LOG.Info("Checking for portable mode.");
					portableCheckMade = true;
					if (Directory.Exists(pafPath)) {
						portable = true;
						LOG.Info("Portable mode active!");
					}
				}
				if (portable) {
					string pafConfigPath = Path.Combine(applicationStartupPath, @"Data\Settings");
					try {
						if (!Directory.Exists(pafConfigPath)) {
							Directory.CreateDirectory(pafConfigPath);
						}
						iniFilePath = Path.Combine(pafConfigPath, configFilename);
					} catch(Exception e) {
						LOG.InfoFormat("Portable mode NOT possible, couldn't create directory '{0}'! Reason: {1}", pafConfigPath, e.Message);
					}
				}
			}
			if (iniFilePath == null) {
				// check if file is in the same location as started from, if this is the case
				// we will use this file instead of the Applicationdate folder
				// Done for Feature Request #2741508
				iniFilePath = Path.Combine(applicationStartupPath, configFilename);
				if (!File.Exists(iniFilePath)) {
					string iniDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), applicationName);
					if (!Directory.Exists(iniDirectory)) {
						Directory.CreateDirectory(iniDirectory);
					}
					iniFilePath = Path.Combine(iniDirectory, configFilename);
				}
			}
			LOG.InfoFormat("Using ini file {0}", iniFilePath);
			return iniFilePath;
		}

		/// <summary>
		/// Reload the Ini file
		/// </summary>
		public static void Reload() {
			// Clear the current properties
			sections = new Dictionary<string, Dictionary<string, string>>();
			// Load the defaults
			Read(CreateIniLocation(configName + DEFAULTS_POSTFIX + INI_EXTENSION));
			// Load the normal
			Read(CreateIniLocation(configName + INI_EXTENSION));
			// Load the fixed settings
			Read(CreateIniLocation(configName + FIXED_POSTFIX + INI_EXTENSION));

			foreach (IniSection section in sectionMap.Values) {
				section.Fill(PropertiesForSection(section));
			}
		}

		/// <summary>
		/// Read the ini file into the Dictionary
		/// </summary>
		/// <param name="iniLocation">Path & Filename of ini file</param>
		private static void Read(string iniLocation) {
			if (!File.Exists(iniLocation)) {
				//LOG.Info("Can't find file: " + iniLocation);
				return;
			}
			LOG.DebugFormat("Loading ini-file: {0}", iniLocation);
			//LOG.Info("Reading ini-properties from file: " + iniLocation);
			Dictionary<string, Dictionary<string, string>> newSections = IniReader.read(iniLocation, Encoding.UTF8);
			// Merge the newly loaded properties to the already available
			foreach(string section in newSections.Keys) {
				Dictionary<string, string> newProperties = newSections[section];
				if (!sections.ContainsKey(section)) {
					// This section is not yet loaded, simply add the complete section
					sections.Add(section, newProperties);
				} else {
					// Overwrite or add every property from the newly loaded section to the available one
					Dictionary<string, string> currentProperties = sections[section];
					foreach(string propertyName in newProperties.Keys) {
						string propertyValue = newProperties[propertyName];
						if (currentProperties.ContainsKey(propertyName)) {
							// Override current value as we are loading in a certain order which insures the default, current and fixed
							currentProperties[propertyName] = propertyValue;
						} else {
							// Add "new" value
							currentProperties.Add(propertyName, propertyValue);
						}
					}
				}
			}
		}

		/// <summary>
		/// Method used for internal tricks...
		/// </summary>
		/// <param name="sectionName"></param>
		/// <returns></returns>
		public static IniSection GetIniSection(string sectionName) {
			if (sectionMap.ContainsKey(sectionName)) {
				return sectionMap[sectionName];
			}
			return null;
		}

		/// <summary>
		/// A generic method which returns an instance of the supplied type, filled with it's configuration
		/// </summary>
		/// <returns>Filled instance of IniSection type which was supplied</returns>
		public static T GetIniSection<T>() where T : IniSection {
			T section;

			Type iniSectionType = typeof(T);
			string sectionName = getSectionName(iniSectionType);
			if (sectionMap.ContainsKey(sectionName)) {
				//LOG.Debug("Returning pre-mapped section " + sectionName);
				section = (T)sectionMap[sectionName];
			} else {
				// Create instance of this type
				section = (T)Activator.CreateInstance(iniSectionType);

				// Store for later save & retrieval
				sectionMap.Add(sectionName, section);
				section.Fill(PropertiesForSection(section));
			}
			if (section.IsDirty) {
				LOG.DebugFormat("Section {0} is marked dirty, saving!", sectionName);
				IniConfig.Save();
			}
			return section;
		}

		public static Dictionary<string, string> PropertiesForSection(IniSection section) {
			Type iniSectionType = section.GetType();
			string sectionName = getSectionName(iniSectionType);
			// Get the properties for the section
			Dictionary<string, string> properties = null;
			if (sections.ContainsKey(sectionName)) {
				properties = sections[sectionName];
			} else {
				sections.Add(sectionName, new Dictionary<string, string>());
				properties = sections[sectionName];
			}
			return properties;
		}

		private static string getSectionName(Type iniSectionType) {
			Attribute[] classAttributes = Attribute.GetCustomAttributes(iniSectionType);
			foreach (Attribute attribute in classAttributes) {
				if (attribute is IniSectionAttribute) {
					IniSectionAttribute iniSectionAttribute = (IniSectionAttribute)attribute;
					return iniSectionAttribute.Name;
				}
			}
			return null;
		}

		public static void Save() {
			string iniLocation = CreateIniLocation(configName + INI_EXTENSION);
			try {
				SaveInternally(iniLocation);
			} catch (Exception ex) {
				LOG.Error("A problem occured while writing the configuration file to: " + iniLocation);
				LOG.Error(ex);
			}
		}
		

		private static void SaveInternally(string iniLocation) {
			WatchConfigFile(false);

			LOG.Info("Saving configuration to: " + iniLocation);
			if (!Directory.Exists(Path.GetDirectoryName(iniLocation))) {
				Directory.CreateDirectory(Path.GetDirectoryName(iniLocation));
			}
			TextWriter writer = new StreamWriter(iniLocation, false, Encoding.UTF8);
			foreach (IniSection section in sectionMap.Values) {
				section.Write(writer, false);
				// Add empty line after section
				writer.WriteLine();
				section.IsDirty = false;
			}
			writer.WriteLine();
			// Write left over properties
			foreach (string sectionName in sections.Keys) {
				// Check if the section is one that is "registered", if so skip it!
				if (!sectionMap.ContainsKey(sectionName)) {
					writer.WriteLine("; The section {0} hasn't been 'claimed' since the last start of Greenshot, therefor it doesn't have additional information here!", sectionName);
					writer.WriteLine("; The reason could be that the section {0} just hasn't been used, a plugin has an error and can't claim it or maybe the whole section {0} is obsolete.", sectionName);
					// Write section name
					writer.WriteLine("[{0}]", sectionName);
					Dictionary<string, string> properties = sections[sectionName];
					// Loop and write properties
					foreach (string propertyName in properties.Keys) {
						writer.WriteLine("{0}={1}", propertyName, properties[propertyName]);
					}
					writer.WriteLine();
				}
			}
			writer.Close();
			WatchConfigFile(true);
		}
	}
}
