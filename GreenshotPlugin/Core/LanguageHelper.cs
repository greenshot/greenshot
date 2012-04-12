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
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;

using Greenshot.IniFile;
using Microsoft.Win32;

namespace GreenshotPlugin.Core {
	
	public interface ILanguage {
		void Load();
		bool hasKey(Enum key);
		bool hasKey(string key);
		string GetString(Enum id);
		string GetString(string id);
		string GetFormattedString(Enum id, object param);
		string GetFormattedString(string id, object param);
		string GetHelpFilePath();

		void FreeResources();
		
		string CurrentLanguage {
			get;
		}

		List<LanguageConfiguration> SupportedLanguages {
			get;
		}
		
		String LanguageFilePattern {
			get;
			set;
		}
		
		LanguageConfiguration CurrentLanguageConfiguration {
			get;
		}
	}
	/// <summary>
	/// Description of Language.
	/// </summary>
	public class LanguageContainer : ILanguage {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(LanguageContainer));
		private static char [] TRIMCHARS = new char[] {' ', '\t', '\n', '\r'};
		private const string DEFAULT_LANGUAGE= "en-US";
		private static string APPLICATIONDATA_LANGUAGE_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),@"Greenshot\Languages\");
		private static string APPLICATION_PATH = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		private static string STARTUP_LANGUAGE_PATH = Path.Combine(APPLICATION_PATH, @"Languages");
		private static string PAF_LANGUAGE_PATH = Path.Combine(APPLICATION_PATH, @"App\Greenshot\Languages");
		private const string HELP_FILENAME_PATTERN = @"help-*.html";
		private const string LANGUAGE_GROUPS_KEY = @"SYSTEM\CurrentControlSet\Control\Nls\Language Groups";
		private static string globalLanguage = DEFAULT_LANGUAGE;
		private string additionalPath = null;	// Set if the designer is used

		private Dictionary<string, string> strings = new Dictionary<string, string>();
		private List<LanguageConfiguration> languages = new List<LanguageConfiguration>();
		private string currentIETF = null;
		private string languageFilePattern;
		private static List<string> supportedLanguageGroups = new List<string>();
		private static List<LanguageContainer> instances = new List<LanguageContainer>();

		static LanguageContainer() {
			try {
				using (RegistryKey languageGroupsKey = Registry.LocalMachine.OpenSubKey(LANGUAGE_GROUPS_KEY, false)) {
					if (languageGroupsKey != null) {
						string [] groups = languageGroupsKey.GetValueNames();
						foreach(string group in groups) {
							string groupValue = (string)languageGroupsKey.GetValue(group);
							bool isGroupInstalled = "1".Equals(groupValue);
							if (isGroupInstalled) {
								supportedLanguageGroups.Add(group.ToLower());
							}
						}
					}
				}
			} catch(Exception e) {
				LOG.Warn("Couldn't read the installed language groups.", e);
			}
		}

		public LanguageContainer() {
			instances.Add(this);
		}

		public LanguageContainer(string filePattern) : this(filePattern, null) {
		}

		public LanguageContainer(string filePattern, string additionalPath) {
			this.additionalPath = additionalPath;
			LanguageFilePattern = filePattern;
			Load();
			SetInstanceLanguage(globalLanguage);
		}

		public String LanguageFilePattern {
			get {
				return languageFilePattern;
			}
			set {
				languageFilePattern = value;
			}
		}

		public void Load() {
			languages = LoadFiles(languageFilePattern);
		}

		public string CurrentLanguage {
			get {
				return currentIETF;
			}
		}

		public List<LanguageConfiguration> SupportedLanguages {
			get {
				return languages;
			}
		}
		
		public LanguageConfiguration CurrentLanguageConfiguration {
			get {
				foreach(LanguageConfiguration languageConfiguration in SupportedLanguages) {
					if (currentIETF.Equals(languageConfiguration.Ietf)) {
						return languageConfiguration;
					}
				}
				return null;
			}
		}
		
		public static void SynchronizeLanguageToCulture() {
			if (globalLanguage == null || !globalLanguage.Equals(Thread.CurrentThread.CurrentUICulture.Name)) {
				SetGlobalLanguage(Thread.CurrentThread.CurrentUICulture.Name);
			}
		}

		public static void SetGlobalLanguage(string wantedIETF) {
			globalLanguage = wantedIETF;
			foreach (LanguageContainer langInstance in instances) {
				langInstance.SetInstanceLanguage(wantedIETF);
			}
		}

		/// <summary>
		/// Set language
		/// </summary>
		/// <param name="wantedIETF">wanted IETF</param>
		/// <returns>Actuall IETF </returns>
		public string SetInstanceLanguage(string wantedIETF) {
			LOG.Debug("SetLanguage called for : " + wantedIETF);
			Dictionary<string, LanguageConfiguration> identifiedLanguages = new Dictionary<string, LanguageConfiguration>();

			if (languages == null || languages.Count == 0) {
				throw new FileNotFoundException(string.Format("No language files for {0} found!", wantedIETF));
			}

			// Find selected languages in available languages
			foreach(LanguageConfiguration language in languages) {
				LOG.Debug("Found language: " + language.Ietf);
				if (!identifiedLanguages.ContainsKey(language.Ietf)) {
					identifiedLanguages.Add(language.Ietf, language);
				} else {
					LOG.WarnFormat("Found double language file: {0}", language.File);
				}
			}
			
			LanguageConfiguration selectedLanguage = null;
			if (identifiedLanguages.ContainsKey(wantedIETF)) {
				selectedLanguage = identifiedLanguages[wantedIETF];
			} else {
				LOG.Warn("Selecteded language " + wantedIETF + " not found.");
			}

			// Make best match for language (e.g. en -> "en-US")
			if (selectedLanguage == null) {
				foreach(string ietf in identifiedLanguages.Keys) {
					if (ietf.StartsWith(wantedIETF)) {
						if (identifiedLanguages.ContainsKey(ietf)) {
							selectedLanguage = identifiedLanguages[ietf];
							LOG.Info("Selecteded language " + ietf + " by near match for: " + wantedIETF);
							wantedIETF = ietf;
							break;
						} else {
							LOG.Warn("Selecteded language " + wantedIETF + " not found.");
						}
					}
				}
			}

			if (selectedLanguage == null && !DEFAULT_LANGUAGE.Equals(wantedIETF)) {
				if (identifiedLanguages.ContainsKey(DEFAULT_LANGUAGE)) {
					selectedLanguage = identifiedLanguages[DEFAULT_LANGUAGE];
				} else {
					LOG.Warn("No english language file found!!");
				}
			}
			if (selectedLanguage == null) {
				// Select first (maybe only) language!
				selectedLanguage = languages[0];
				LOG.Warn("Selected " + selectedLanguage.Ietf + " as fallback language!");
			}

			// build directionary for the strings
			strings.Clear();
			foreach(Resource resource in selectedLanguage.Resources) {
				AddResource(resource);
			}
			
			// Make sure we have all the missing resources, right after setting the language
			// this way we can free all other resources.
			AdoptMissingResourcesFromDefaultLanguage();

			currentIETF = selectedLanguage.Ietf;
			Thread.CurrentThread.CurrentUICulture = new CultureInfo(currentIETF);

			return currentIETF;
		}
		
		/// <summary>
		/// Free all language resources which aren't needed
		/// </summary>
		public void FreeResources() {
			languages = null;
		}

		private void AddResource(Resource resource) {
			try {
				if (resource.Text != null) {
					strings.Add(resource.Name, resource.Text.Trim(TRIMCHARS));
				} else {
					LOG.Warn("Resource is null: " + resource.Name);
					strings.Add(resource.Name, "");
				}
			} catch (ArgumentException ae) {
				LOG.Error("Problem adding " + resource.Name, ae);
				throw ae;
			}
		}
		
		private List<LanguageConfiguration> LoadFiles(string languageFilePattern) {
			List<LanguageConfiguration> loadedLanguages = new List<LanguageConfiguration>();
			List<string> languageDirectories = new List<string>();
			if (IniConfig.IsPortable) {
				languageDirectories.Add(PAF_LANGUAGE_PATH);
			} else {
				languageDirectories.Add(APPLICATIONDATA_LANGUAGE_PATH);
			}
			if (additionalPath != null) {
				languageDirectories.Add(additionalPath);
			}
			languageDirectories.Add(STARTUP_LANGUAGE_PATH);
			foreach(string path in languageDirectories) {
				if (!Directory.Exists(path)) {
					LOG.InfoFormat("Skipping non existing language path {0}", path);
					continue;
				}
				// Search in executable directory
				LOG.InfoFormat("Searching language directory '{0}' for language files with pattern '{1}'", path, languageFilePattern);
				try {
					foreach(string languageFile in Directory.GetFiles(path, languageFilePattern, SearchOption.AllDirectories)) {
						LOG.DebugFormat("Found language file: {0}", languageFile);
						LanguageConfiguration languageConfig = LanguageConfiguration.Load(languageFile);
						if (languageConfig != null) {
							if (string.IsNullOrEmpty(languageConfig.LanguageGroup) || supportedLanguageGroups.Contains(languageConfig.LanguageGroup)) {
								LOG.InfoFormat("Loaded language: {0}", languageConfig.Description);
								loadedLanguages.Add(languageConfig);
							} else {
								LOG.InfoFormat("Skipping unsupported language: {0}", languageConfig.Description);
							}
						}
					}
				} catch (DirectoryNotFoundException) {
					LOG.InfoFormat("Non existing language directory: {0}", path);
				} catch (Exception e) {
					LOG.Error("Error trying for read directory " + path, e);
				}
			}

			if (loadedLanguages.Count == 0) {
				// Try to force internal english
				try {
					LOG.Info("No languages found, using embedded en-US.");
					using (Stream stream = Assembly.GetEntryAssembly().GetManifestResourceStream("Greenshot.Languages.language-en-US.xml")) {
						LanguageConfiguration languageConfig = LanguageConfiguration.Load(stream);
						if (languageConfig != null) {
							loadedLanguages.Add(languageConfig);
						}
					}
				} catch (Exception ie) {
					LOG.Error("Can't read internal 'Greenshot.Languages.language-en-US.xml'", ie);
				}
			}
			return loadedLanguages;
		}
		
		public void Validate(Enum languageKeys) {
			Dictionary<string, List<string>> keysPerLanguage = new Dictionary<string, List<string>>();
			foreach(LanguageConfiguration languageToValidate in languages) {
				List<string> keys = new List<string>();
				foreach(Resource resource in languageToValidate.Resources) {
					keys.Add(resource.Name);
				}
				keys.Sort();
				keysPerLanguage.Add(languageToValidate.Ietf, keys);
			}
			
			// Make list of values in the enum
			List<string> fixedKeys = new List<string>();
			foreach(Enum langKey in Enum.GetValues(languageKeys.GetType())) {
				fixedKeys.Add(langKey.ToString());
			}

			foreach(string ietf in keysPerLanguage.Keys) {
				List<string> keys = keysPerLanguage[ietf];
				foreach(string key in fixedKeys) {
					if (!keys.Contains(key)) {
						LOG.Warn(ietf + " is missing resource with name [" + key + "]");
					}
				}
				foreach(string key in keys) {
					if (!fixedKeys.Contains(key)) {
						LOG.Warn(ietf + " has additional resource with name [" + key + "]");
					}
				}
			}
		}
		
		private void ToEnum() {
			if (!LOG.IsDebugEnabled) {
				return;
			}
			StringBuilder EnumClass = new StringBuilder();
			EnumClass.AppendLine("/*");
			EnumClass.AppendLine(" * Auto generated");
			EnumClass.AppendLine(" */");
			EnumClass.AppendLine("using System;");
			EnumClass.AppendLine();
			EnumClass.AppendLine("namespace Greenshot.Configuration {");
			EnumClass.AppendLine("    public enum LangKey {");

			List<string> keys = new List<string>();
			foreach(LanguageConfiguration foundLanguage in languages) {
				if (foundLanguage.Ietf.Equals(DEFAULT_LANGUAGE)) {
					foreach(Resource resource in foundLanguage.Resources) {
						keys.Add(resource.Name);
					}
				}
			}
			keys.Sort();
			bool added = false;
			foreach(string key in keys) {
				if (added) {
					EnumClass.AppendLine(",");
				}
				EnumClass.Append("		" + key);
				added = true;
			}
			EnumClass.AppendLine();
			EnumClass.AppendLine("    }");
			EnumClass.AppendLine("}");
			LOG.Debug("LangKeys should be: \r\n" + EnumClass.ToString());
		}
		
		public bool hasKey(Enum key) {
			if (key == null) {
				return false;
			}
			return hasKey(key.ToString());
		}

		public bool hasKey(string key) {
			if (key == null) {
				return false;
			}
			return strings.ContainsKey(key);
		}

		public string GetString(Enum key) {
			if (key == null) {
				return null;
			}
			return GetString(key.ToString());
		}

		public string GetString(string key) {
			if (key == null) {
				return null;
			}
			try {
				return strings[key];
			} catch (KeyNotFoundException) {
				return "string ###"+key+"### not found";
			}
		}

		public string GetFormattedString(Enum key, object param) {
			return GetFormattedString(key.ToString(), param);
		}

		public string GetFormattedString(string key, object param) {
			try {
				return String.Format(strings[key], param);
			} catch (KeyNotFoundException) {
				return "string ###"+key+"### not found";
			}
		}
		
		private void AdoptMissingResourcesFromDefaultLanguage() {
			LanguageConfiguration defaultLanguageConfiguration = GetDefaultLanguageConfiguration();
			if (defaultLanguageConfiguration != null) {
				foreach(Resource resource in defaultLanguageConfiguration.Resources) {
					if(resource != null && !strings.ContainsKey(resource.Name)) {
						AddResource(resource);
						if(LOG.IsWarnEnabled) {
							LOG.Warn("Adopted missing string resource from default language: "+resource.Name);
						}
					}
				}
			} else {
				LOG.Warn("Default language file is missing! The default language file is: " + DEFAULT_LANGUAGE);
			}
		}
		
		private LanguageConfiguration GetDefaultLanguageConfiguration() {
			foreach(LanguageConfiguration language in languages) {
				if(language.Ietf == DEFAULT_LANGUAGE) return language;
			}
			return null;
		}
		
		/// finds a returns the path of the best matching help file.
		/// 1st tries to find file for currentLanguage, 2nd for defaultLanguage.
		/// if neither is found, the first help file found is returned
		public string GetHelpFilePath() {
			List<string> helpFiles = new List<string>();
			// Search in executable directory
			if (Directory.Exists(STARTUP_LANGUAGE_PATH)) {
				helpFiles.AddRange(Directory.GetFiles(STARTUP_LANGUAGE_PATH, HELP_FILENAME_PATTERN, SearchOption.AllDirectories));
			}
			// Search in ApplicationData directory
			if (Directory.Exists(APPLICATIONDATA_LANGUAGE_PATH)) {
				helpFiles.AddRange(Directory.GetFiles(APPLICATIONDATA_LANGUAGE_PATH, HELP_FILENAME_PATTERN, SearchOption.AllDirectories));
		    }

			foreach(string helpFile in helpFiles) {
				if(helpFile.EndsWith(currentIETF+".html")) {
					return helpFile;
				}
			}
			foreach(string helpFile in helpFiles) {
				if(helpFile.EndsWith(DEFAULT_LANGUAGE+".html")) {
					return helpFile;
				}
			}
			LOG.Warn("Help file not found for default language, will load "+helpFiles[0]);
			return helpFiles[0];
		}

	}
	
	public class LanguageConfiguration {
		
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(LanguageConfiguration));
		
		public string description;
		public string Description {
			get {
				return description;
			}
			set {
				description = value;
			}
		}
		
		public string ietf;
		public string Ietf {
			get {
				return ietf;
			}
			set {
				ietf = value;
			}
		}
		
		public string version;
		public string Version {
			get {
				return version;
			}
			set {
				version = value;
			}
		}

		public string languageGroup;
		public string LanguageGroup {
			get {
				return languageGroup;
			}
			set {
				languageGroup = value;
			}
		}
		
		public string file;
		public string File {
			get {
				return file;
			}
			set {
				file = value;
			}
		}
		
		public List<Resource> Resources;
		
		public LanguageConfiguration() {
			Resources = new List<Resource>();
		}
		
		/// <summary>
		/// loads a language configuration from a file path
		/// </summary>
		public static LanguageConfiguration Load(string path) {
			LanguageConfiguration languageConfiguration = null;
			try {
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.Load(path);
				languageConfiguration = ProcessXML(xmlDocument);
				languageConfiguration.File = path;
			} catch(Exception e) {
				LOG.Error("Could not load language file "+path, e);
			}
			return languageConfiguration;
		}

		/// <summary>
		/// loads a language configuration from a stream
		/// </summary>
		public static LanguageConfiguration Load(Stream stream) {
			LanguageConfiguration languageConfiguration = null;
			try {
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.Load(stream);
				languageConfiguration = ProcessXML(xmlDocument);
				languageConfiguration.File = "internal";
			} catch(Exception e) {
				LOG.Error("Could not load internal language file", e);
			}
			return languageConfiguration;
		}

		private static LanguageConfiguration ProcessXML(XmlDocument xmlDocument) {
			LanguageConfiguration languageConfiguration = null;
			XmlNodeList nodes = xmlDocument.GetElementsByTagName("language");
			if(nodes.Count > 0) {
				languageConfiguration = new LanguageConfiguration();
				XmlNode node = nodes.Item(0);
				languageConfiguration.Description = node.Attributes["description"].Value;
				languageConfiguration.Ietf = node.Attributes["ietf"].Value;
				languageConfiguration.Version = node.Attributes["version"].Value;
				if (node.Attributes["languagegroup"] != null) {
					string languageGroup = node.Attributes["languagegroup"].Value;
					languageConfiguration.LanguageGroup = languageGroup.ToLower();
				}
				
				XmlNodeList resourceNodes = xmlDocument.GetElementsByTagName("resource");
				languageConfiguration.Resources = new List<Resource>(resourceNodes.Count);
				foreach(XmlNode resourceNode in resourceNodes) {
					Resource res = new Resource();
					res.Name = resourceNode.Attributes["name"].Value;
					res.Text = resourceNode.InnerText;
					languageConfiguration.Resources.Add(res);
				}
			} else {
				throw new XmlException("Root element <language> is missing");
			}
			return languageConfiguration;
		}
	}

	public class Resource {
		
		public string name;
		public string Name {get; set;}
		
		public string text;
		public string Text {get; set;}

		public override int GetHashCode() {
			int hash = 7;
			if (Text != null) {
				hash = hash ^ Text.GetHashCode();
			}
			if (Name != null) {
				hash = hash ^ Name.GetHashCode();
			}
			return hash;
		}
		
		public override bool Equals(object obj) {
			if (obj == null) {
				return false;
			}
		    if (obj is Resource) {
				Resource other = (Resource) obj;
				if(Name == null) {
					if (other.Name != null) {
						return false;
					}
					return true;
				}
				if(Text == null) {
					if (other.Text != null) {
						return false;
					}
					return true;
				}
				return (Name.Equals(other.Name) && Text.Equals(other.Text));
			}
			return false;
		}
	}
}
