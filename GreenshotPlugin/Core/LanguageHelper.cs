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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace GreenshotPlugin.Core {
	
	public interface ILanguage {
		void Load();
		string GetString(Enum id);
		string GetFormattedString(Enum id, object param);
		string GetHelpFilePath();

		/// <summary>
		/// Set language
		/// </summary>
		/// <param name="wantedIETF">wanted IETF</param>
		/// <returns>Actuall IETF </returns>
		string SetLanguage(string cultureInfo);
		void SynchronizeLanguageToCulture();
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

	}
	/// <summary>
	/// Description of Language.
	/// </summary>
	public class LanguageContainer : ILanguage {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(LanguageContainer));
		private static char [] TRIMCHARS = new char[] {' ', '\t', '\n', '\r'};
		private const string DEFAULT_LANGUAGE= "en-US";
		private static string APPLICATIONDATA_LANGUAGE_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),@"Greenshot\Languages\");
		private static string STARTUP_LANGUAGE_PATH = Path.Combine(Application.StartupPath, @"Languages\");
		private const string HELP_FILENAME_PATTERN = @"help-*.html";

		private Dictionary<string, string> strings = new Dictionary<string, string>();
		private List<LanguageConfiguration> languages = new List<LanguageConfiguration>();
		private string currentIETF = null;
		private string languageFilePattern;

		public LanguageContainer() {
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
		
		public void SynchronizeLanguageToCulture() {
			if (!CurrentLanguage.Equals(Thread.CurrentThread.CurrentUICulture.Name)) {
				SetLanguage(Thread.CurrentThread.CurrentUICulture.Name);
			}
		}

		/// <summary>
		/// Set language
		/// </summary>
		/// <param name="wantedIETF">wanted IETF</param>
		/// <returns>Actuall IETF </returns>
		public string SetLanguage(string wantedIETF) {
			LOG.Debug("SetLanguage called for : " + wantedIETF);
			Dictionary<string, LanguageConfiguration> identifiedLanguages = new Dictionary<string, LanguageConfiguration>();
						
			if (languages == null || languages.Count == 0) {
				throw new FileNotFoundException("No language files found!");
			}

			// Find selected languages in available languages
			foreach(LanguageConfiguration language in languages) {
				LOG.Debug("Found language: " + language.Ietf);
				identifiedLanguages.Add(language.Ietf, language);
			}
			
			LanguageConfiguration selectedLanguage = null;
			try {
				selectedLanguage = identifiedLanguages[wantedIETF];
			} catch (KeyNotFoundException) {
				LOG.Warn("Selecteded language " + wantedIETF + " not found.");
			}

			// Make best match for language (e.g. en -> "en-US")
			if (selectedLanguage == null) {
				foreach(string ietf in identifiedLanguages.Keys) {
					if (ietf.StartsWith(wantedIETF)) {
						try {
							selectedLanguage = identifiedLanguages[ietf];
							LOG.Info("Selecteded language " + ietf + " by near match for: " + wantedIETF);
							wantedIETF = ietf;
							break;
						} catch (KeyNotFoundException) {
							LOG.Warn("Selecteded language " + wantedIETF + " not found.");
						}
					}
				}
			}

			if (selectedLanguage == null && !DEFAULT_LANGUAGE.Equals(wantedIETF)) {
				try {
					selectedLanguage = identifiedLanguages[DEFAULT_LANGUAGE];
				} catch (KeyNotFoundException) {
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

			currentIETF = selectedLanguage.Ietf;
			return currentIETF;
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
			languageDirectories.Add(STARTUP_LANGUAGE_PATH);
			languageDirectories.Add(APPLICATIONDATA_LANGUAGE_PATH);
			foreach(string path in languageDirectories) {
				// Search in executable directory
				if (Directory.Exists(path)) {
					foreach(string languageFile in Directory.GetFiles(path, languageFilePattern, SearchOption.AllDirectories)) {
						LanguageConfiguration languageConfig = LanguageConfiguration.Load(languageFile);
						if (languageConfig != null) {
							LOG.Info("Loaded language: " + languageConfig.Description);
							loadedLanguages.Add(languageConfig);
						}
					}
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

		public string GetString(Enum id) {
			if(!strings.ContainsKey(id.ToString())) {
				AdoptMissingResourcesFromDefaultLanguage();
			}
			try {
				return strings[id.ToString()];
			} catch (KeyNotFoundException) {
				return "string ###"+id+"### not found";
			}
		}

		public string GetFormattedString(Enum id, object param) {
			if(!strings.ContainsKey(id.ToString())) {
				AdoptMissingResourcesFromDefaultLanguage();
			}
			try {
				return String.Format(strings[id.ToString()], param);
			} catch (KeyNotFoundException) {
				return "string ###"+id+"### not found";
			}
		}
		
		private void AdoptMissingResourcesFromDefaultLanguage() {
			LanguageConfiguration defaultLanguageConfiguration = GetDefaultLanguageConfiguration();
			if (defaultLanguageConfiguration != null) {
				foreach(Resource resource in GetDefaultLanguageConfiguration().Resources) {
					if(!strings.ContainsKey(resource.Name)) {
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
		public string Description {get;set;}
		
		public string ietf;
		public string Ietf {get;set;}
		
		public string version;
		public string Version {get;set;}
		
		public string file;
		public string File {get;set;}
		
		public List<Resource> Resources;
		
		public LanguageConfiguration() {
			Resources = new List<Resource>();
		}
		
		/// <summary>
		/// loads a language configuration from a file path
		/// </summary>
		public static LanguageConfiguration Load(string path) {
			LanguageConfiguration ret = null;
			try {
				XmlDocument doc = new XmlDocument();
				doc.Load(path);
				XmlNodeList nodes = doc.GetElementsByTagName("language");
				if(nodes.Count > 0) {
					ret = new LanguageConfiguration();
					ret.File = path;
					XmlNode node = nodes.Item(0);
					ret.Description = node.Attributes["description"].Value;
					ret.Ietf = node.Attributes["ietf"].Value;
					ret.Version = node.Attributes["version"].Value;
					
					XmlNodeList resourceNodes = doc.GetElementsByTagName("resource");
					ret.Resources = new List<Resource>(resourceNodes.Count);
					foreach(XmlNode resourceNode in resourceNodes) {
						Resource res = new Resource();
						res.Name = resourceNode.Attributes["name"].Value;
						res.Text = resourceNode.InnerText;
						ret.Resources.Add(res);
					}
				} else {
					throw new XmlException("Root element <language> is missing");
				}
			} catch(Exception e) {
				LOG.Error("Could not load language file "+path, e);
			}
			return ret;
			
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
