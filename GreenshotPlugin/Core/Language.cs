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
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using Greenshot.IniFile;
using log4net;
using Microsoft.Win32;

namespace GreenshotPlugin.Core {
	public delegate void LanguageChangedHandler(object sender, EventArgs e);
	/// <summary>
	/// This class supplies the GUI with translations, based upon keys.
	/// The language resources are loaded from the language files found on fixed or supplied paths
	/// </summary>
	public class Language {
		private static readonly ILog Log = LogManager.GetLogger(typeof(Language));
		private static readonly IList<string> LanguagePaths = new List<string>();
		private static readonly IDictionary<string, List<LanguageFile>> LanguageFiles = new Dictionary<string, List<LanguageFile>>();
		private static readonly IDictionary<string, string> HelpFiles = new Dictionary<string, string>();
		private const string DefaultLanguage = "en-US";
		private const string HelpFilenamePattern = @"help-*.html";
		private const string LanguageFilenamePattern = @"language*.xml";
		private static readonly Regex PrefixRegexp = new Regex(@"language_([a-zA-Z0-9]+).*");
		private static readonly Regex IetfCleanRegexp = new Regex(@"[^a-zA-Z]+");
		private static readonly Regex IetfRegexp = new Regex(@"^.*([a-zA-Z]{2,3}-[a-zA-Z]{1,2})\.xml$");
		private const string LanguageGroupsKey = @"SYSTEM\CurrentControlSet\Control\Nls\Language Groups";
		private static readonly IList<string> UnsupportedLanguageGroups = new List<string>();
		private static readonly IDictionary<string, string> Resources = new Dictionary<string, string>();
		private static string _currentLanguage;

		public static event LanguageChangedHandler LanguageChanged;

		/// <summary>
		/// Static initializer for the language code
		/// </summary>
		static Language() {
			if (!IniConfig.IsInitialized) {
				Log.Warn("IniConfig hasn't been initialized yet! (Design mode?)");
				IniConfig.Init("greenshot", "greenshot");
			}
			if (!LogHelper.IsInitialized) {
				Log.Warn("Log4net hasn't been initialized yet! (Design mode?)");
				LogHelper.InitializeLog4Net();
			}
			
			try {
				string applicationFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
	
				// PAF Path
				if (applicationFolder != null)
				{
					AddPath(Path.Combine(applicationFolder, @"App\Greenshot\Languages"));
				}
				// Application data path
				string applicationDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				AddPath(Path.Combine(applicationDataFolder, @"Greenshot\Languages\"));

				// Startup path
				if (applicationFolder != null)
				{
					AddPath(Path.Combine(applicationFolder, @"Languages"));
				}
			}
			catch (Exception pathException) {
				Log.Error(pathException);
			}

			try {
				using (RegistryKey languageGroupsKey = Registry.LocalMachine.OpenSubKey(LanguageGroupsKey, false)) {
					if (languageGroupsKey != null) {
						string [] groups = languageGroupsKey.GetValueNames();
						foreach(string group in groups) {
							string groupValue = (string)languageGroupsKey.GetValue(group);
							bool isGroupNotInstalled = "0".Equals(groupValue);
							if (isGroupNotInstalled) {
								UnsupportedLanguageGroups.Add(group.ToLower());
							}
						}
					}
				}
			} catch(Exception e) {
				Log.Warn("Couldn't read the installed language groups.", e);
			}

			var coreConfig = IniConfig.GetIniSection<CoreConfiguration>();
			ScanFiles();
			if (!string.IsNullOrEmpty(coreConfig.Language)) {
				CurrentLanguage = coreConfig.Language;
				if (CurrentLanguage != null && CurrentLanguage != coreConfig.Language) {
					coreConfig.Language = CurrentLanguage;
				}
			}

			if (CurrentLanguage == null) {
				Log.Warn("Couldn't set language from configuration, changing to default. Installation problem?");
				CurrentLanguage = DefaultLanguage;
				if (CurrentLanguage != null) {
					coreConfig.Language = CurrentLanguage;
				}
			}

			if (CurrentLanguage == null) {
				Log.Error("Couldn't set language, installation problem?");
			}
		}

		/// <summary>
		/// Internal method to add a path to the paths that will be scanned for language files!
		/// </summary>
		/// <param name="path"></param>
		/// <returns>true if the path exists and is added</returns>
		private static bool AddPath(string path) {
			if (!LanguagePaths.Contains(path)) {
				if (Directory.Exists(path)) {
					Log.DebugFormat("Adding language path {0}", path);
					LanguagePaths.Add(path);
					return true;
				} else {
					Log.InfoFormat("Not adding non existing language path {0}", path);
				}
			}
			return false;
		}

		/// <summary>
		/// Add a new path to the paths that will be scanned for language files!
		/// </summary>
		/// <param name="path"></param>
		/// <returns>true if the path exists and is added</returns>
		public static bool AddLanguageFilePath(string path) {
			if (!LanguagePaths.Contains(path)) {
				Log.DebugFormat("New language path {0}", path);
				if (AddPath(path)) {
					ScanFiles();
					Reload();
				} else {
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Load the files for the specified ietf
		/// </summary>
		/// <param name="ietf"></param>
		private static void LoadFiles(string ietf) {
			ietf = ReformatIETF(ietf);
			if (!LanguageFiles.ContainsKey(ietf)) {
				Log.ErrorFormat("No language {0} available.", ietf);
				return;
			}
			List<LanguageFile> filesToLoad = LanguageFiles[ietf];
			foreach (LanguageFile fileToLoad in filesToLoad) {
				LoadResources(fileToLoad);
			}
		}

		/// <summary>
		/// Load the language resources from the scanned files
		/// </summary>
		private static void Reload() {
			Resources.Clear();
			LoadFiles(DefaultLanguage);
			if (_currentLanguage != null && !_currentLanguage.Equals(DefaultLanguage)) {
				LoadFiles(_currentLanguage);
			}
		}

		/// <summary>
		/// Get or set the current language
		/// </summary>
		public static string CurrentLanguage {
			get {
				return _currentLanguage;
			}
			set {
				string ietf = FindBestIETFMatch(value);
				if (!LanguageFiles.ContainsKey(ietf)) {
					Log.WarnFormat("No match for language {0} found!", ietf);
				} else {
					if (_currentLanguage == null || !_currentLanguage.Equals(ietf)) {
						_currentLanguage = ietf;
						Reload();
						if (LanguageChanged == null)
						{
							return;
						}
						try {
							LanguageChanged(null, null);
						}
						catch
						{
							// ignored
						}
						return;
					}
				}
				Log.Debug("CurrentLanguage not changed!");
			}
		}

		/// <summary>
		/// Try to find the best match for the supplied IETF
		/// </summary>
		/// <param name="inputIETF"></param>
		/// <returns>IETF</returns>
		private static string FindBestIETFMatch(string inputIETF) {
			string returnIETF = inputIETF;
			if (string.IsNullOrEmpty(returnIETF)) {
				returnIETF = DefaultLanguage;
			}
			returnIETF = ReformatIETF(returnIETF);
			if (!LanguageFiles.ContainsKey(returnIETF)) {
				Log.WarnFormat("Unknown language {0}, trying best match!", returnIETF);
				if (returnIETF.Length == 5) {
					returnIETF = returnIETF.Substring(0, 2);
				}
				foreach (string availableIETF in LanguageFiles.Keys) {
					if (availableIETF.StartsWith(returnIETF)) {
						Log.InfoFormat("Found language {0}, best match for {1}!", availableIETF, returnIETF);
						returnIETF = availableIETF;
						break;
					}
				}
			}
			return returnIETF;
		}

		/// <summary>
		/// This helper method clears all non alpha characters from the IETF, and does a reformatting.
		/// This prevents problems with multiple formats or typos.
		/// </summary>
		/// <param name="inputIETF"></param>
		/// <returns></returns>
		private static string ReformatIETF(string inputIETF) {
			string returnIETF = null;
			if (!string.IsNullOrEmpty(inputIETF)) {
				returnIETF = inputIETF.ToLower();
				returnIETF = IetfCleanRegexp.Replace(returnIETF, "");
				if (returnIETF.Length == 4) {
					returnIETF = returnIETF.Substring(0, 2) + "-" + returnIETF.Substring(2, 2).ToUpper();
				}
			}
			return returnIETF;
		}

		/// <summary>
		/// Return a list of all the supported languages
		/// </summary>
		public static IList<LanguageFile> SupportedLanguages {
			get {
				IList<LanguageFile> languages = new List<LanguageFile>();
				// Loop over all languages with all the files in there
				foreach (List<LanguageFile> langs in LanguageFiles.Values) {
					// Loop over all the files for a language
					foreach (LanguageFile langFile in langs) {
						// Only take the ones without prefix, these are the "base" language files
						if (langFile.Prefix == null) {
							languages.Add(langFile);
							break;
						}
					}
				}
				return languages;
			}
		}

		/// <summary>
		/// Return the path to the help-file
		/// </summary>
		public static string HelpFilePath {
			get {
				if (HelpFiles.ContainsKey(_currentLanguage)) {
					return HelpFiles[_currentLanguage];
				}
				return HelpFiles[DefaultLanguage];
			}
		}

		/// <summary>
		/// Load the resources from the language file
		/// </summary>
		/// <param name="languageFile">File to load from</param>
		private static void LoadResources(LanguageFile languageFile) {
			Log.InfoFormat("Loading language file {0}", languageFile.Filepath);
			try {
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.Load(languageFile.Filepath);
				XmlNodeList resourceNodes = xmlDocument.GetElementsByTagName("resource");
				foreach (XmlNode resourceNode in resourceNodes) {
					string key = resourceNode.Attributes["name"].Value;
					if (!string.IsNullOrEmpty(languageFile.Prefix)) {
						key = languageFile.Prefix + "." + key;
					}
					string text = resourceNode.InnerText;
					if (!string.IsNullOrEmpty(text)) {
						text = text.Trim();
					}
					if (!Resources.ContainsKey(key)) {
						Resources.Add(key, text);
					} else {
						Resources[key] = text;
					}
				}
			} catch (Exception e) {
				Log.Error("Could not load language file " + languageFile.Filepath, e);
			}
		}

		/// <summary>
		/// Load the language file information
		/// </summary>
		/// <param name="languageFilePath"></param>
		/// <returns></returns>
		private static LanguageFile LoadFileInfo(string languageFilePath) {
			try {
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.Load(languageFilePath);
				XmlNodeList nodes = xmlDocument.GetElementsByTagName("language");
				if (nodes.Count > 0) {
					LanguageFile languageFile = new LanguageFile
					{
						Filepath = languageFilePath
					};
					XmlNode node = nodes.Item(0);
					if (node?.Attributes != null)
					{
						languageFile.Description = node.Attributes["description"].Value;
						if (node.Attributes["ietf"] != null) {
							languageFile.Ietf = ReformatIETF(node.Attributes["ietf"].Value);
						}
						if (node.Attributes["version"] != null) {
							languageFile.Version = new Version(node.Attributes["version"].Value);
						}
						if (node.Attributes["prefix"] != null) {
							languageFile.Prefix = node.Attributes["prefix"].Value.ToLower();
						}
						if (node.Attributes["languagegroup"] != null) {
							string languageGroup = node.Attributes["languagegroup"].Value;
							languageFile.LanguageGroup = languageGroup.ToLower();
						}
					}
					return languageFile;
				}
				throw new XmlException("Root element <language> is missing");
			} catch (Exception e) {
				Log.Error("Could not load language file " + languageFilePath, e);
			}
			return null;
		}

		/// <summary>
		/// Scan the files in all directories
		/// </summary>
		private static void ScanFiles() {
			LanguageFiles.Clear();
			HelpFiles.Clear();
			foreach (string languagePath in LanguagePaths) {
				if (!Directory.Exists(languagePath)) {
					Log.InfoFormat("Skipping non existing language path {0}", languagePath);
					continue;
				}
				Log.InfoFormat("Searching language directory '{0}' for language files with pattern '{1}'", languagePath, LanguageFilenamePattern);
				try {
					foreach (string languageFilepath in Directory.GetFiles(languagePath, LanguageFilenamePattern, SearchOption.AllDirectories)) {
						//LOG.DebugFormat("Found language file: {0}", languageFilepath);
						LanguageFile languageFile = LoadFileInfo(languageFilepath);
						if (languageFile == null) {
							continue;
						}
						if (string.IsNullOrEmpty(languageFile.Ietf)) {
							Log.WarnFormat("Fixing missing ietf in language-file {0}", languageFilepath);
							string languageFilename = Path.GetFileName(languageFilepath);
							if (IetfRegexp.IsMatch(languageFilename)) {
								string replacementIETF = IetfRegexp.Replace(languageFilename, "$1");
								languageFile.Ietf = ReformatIETF(replacementIETF);
								Log.InfoFormat("Fixed IETF to {0}", languageFile.Ietf);
							} else {
								Log.ErrorFormat("Missing ietf , no recover possible... skipping language-file {0}!", languageFilepath);
								continue;
							}
						}

						// Check if we can display the file
						if (!string.IsNullOrEmpty(languageFile.LanguageGroup) && UnsupportedLanguageGroups.Contains(languageFile.LanguageGroup)) {
							Log.InfoFormat("Skipping unsuported (not able to display) language {0} from file {1}", languageFile.Description, languageFilepath);
							continue;
						}

						// build prefix, based on the filename, but only if it's not set in the file itself.
						if (string.IsNullOrEmpty(languageFile.Prefix)) {
							string languageFilename = Path.GetFileNameWithoutExtension(languageFilepath);
							if (PrefixRegexp.IsMatch(languageFilename)) {
								languageFile.Prefix = PrefixRegexp.Replace(languageFilename, "$1");
								if (!string.IsNullOrEmpty(languageFile.Prefix)) {
									languageFile.Prefix = languageFile.Prefix.Replace("plugin", "").ToLower();
								}
							}
						}
						List<LanguageFile> currentFiles = null;
						if (LanguageFiles.ContainsKey(languageFile.Ietf)) {
							currentFiles = LanguageFiles[languageFile.Ietf];
							bool needToAdd = true;
							List<LanguageFile> deleteList = new List<LanguageFile>();
							foreach (LanguageFile compareWithLangfile in currentFiles) {
								if ((languageFile.Prefix == null && compareWithLangfile.Prefix == null) || (languageFile.Prefix != null && languageFile.Prefix.Equals(compareWithLangfile.Prefix))) {
									if (compareWithLangfile.Version > languageFile.Version) {
										Log.WarnFormat("Skipping {0}:{1}:{2} as {3}:{4}:{5} is newer", languageFile.Filepath, languageFile.Prefix, languageFile.Version, compareWithLangfile.Filepath, compareWithLangfile.Prefix, compareWithLangfile.Version);
										needToAdd = false;
										break;
									} else {
										Log.WarnFormat("Found {0}:{1}:{2} and deleting {3}:{4}:{5}", languageFile.Filepath, languageFile.Prefix, languageFile.Version, compareWithLangfile.Filepath, compareWithLangfile.Prefix, compareWithLangfile.Version);
										deleteList.Add(compareWithLangfile);
									}
								}
							}
							if (needToAdd) {
								foreach (LanguageFile deleteFile in deleteList) {
									currentFiles.Remove(deleteFile);
								}
								Log.InfoFormat("Added language definition {0} from: {1}", languageFile.Description, languageFile.Filepath);
								currentFiles.Add(languageFile);
							}
						} else {
							currentFiles = new List<LanguageFile> {languageFile};
							LanguageFiles.Add(languageFile.Ietf, currentFiles);
							Log.InfoFormat("Added language definition {0} from: {1}", languageFile.Description, languageFile.Filepath);
						}
					}
				} catch (DirectoryNotFoundException) {
					Log.InfoFormat("Non existing language directory: {0}", languagePath);
				} catch (Exception e) {
					Log.Error("Error trying for read directory " + languagePath, e);
				}

				// Now find the help files
				Log.InfoFormat("Searching language directory '{0}' for help files with pattern '{1}'", languagePath, HelpFilenamePattern);
				try {
					foreach (string helpFilepath in Directory.GetFiles(languagePath, HelpFilenamePattern, SearchOption.AllDirectories)) {
						Log.DebugFormat("Found help file: {0}", helpFilepath);
						string helpFilename = Path.GetFileName(helpFilepath);
						string ietf = ReformatIETF(helpFilename.Replace(".html", "").Replace("help-", ""));
						if (!HelpFiles.ContainsKey(ietf)) {
							HelpFiles.Add(ietf, helpFilepath);
						} else {
							Log.WarnFormat("skipping help file {0}, already a file with the same IETF {1} found!", helpFilepath, ietf);
						}
					}
				} catch (DirectoryNotFoundException) {
					Log.InfoFormat("Non existing language directory: {0}", languagePath);
				} catch (Exception e) {
					Log.Error("Error trying for read directory " + languagePath, e);
				}
			}
		}

		/// <summary>
		/// Check if a resource with prefix.key exists
		/// </summary>
		/// <param name="prefix"></param>
		/// <param name="key"></param>
		/// <returns>true if available</returns>
		public static bool HasKey(string prefix, Enum key) {
			if (key == null) {
				return false;
			}
			return HasKey(prefix + "." + key);
		}

		/// <summary>
		/// Check if a resource with key exists
		/// </summary>
		/// <param name="key"></param>
		/// <returns>true if available</returns>
		public static bool HasKey(Enum key) {
			if (key == null) {
				return false;
			}
			return HasKey(key.ToString());
		}

		/// <summary>
		/// Check if a resource with prefix.key exists
		/// </summary>
		/// <param name="prefix"></param>
		/// <param name="key"></param>
		/// <returns>true if available</returns>
		public static bool HasKey(string prefix, string key) {
			return HasKey(prefix + "." + key);
		}

		/// <summary>
		/// Check if a resource with key exists
		/// </summary>
		/// <param name="key"></param>
		/// <returns>true if available</returns>
		public static bool HasKey(string key) {
			if (key == null) {
				return false;
			}
			return Resources.ContainsKey(key);
		}
		
		/// <summary>
		/// TryGet method which combines HasKey & GetString
		/// </summary>
		/// <param name="key"></param>
		/// <param name="languageString">out string</param>
		/// <returns></returns>
		public static bool TryGetString(string key, out string languageString) {
			return Resources.TryGetValue(key, out languageString);
		}

		/// <summary>
		/// TryGet method which combines HasKey & GetString
		/// </summary>
		/// <param name="prefix">string with prefix</param>
		/// <param name="key">string with key</param>
		/// <param name="languageString">out string</param>
		/// <returns></returns>
		public static bool TryGetString(string prefix, string key, out string languageString) {
			return Resources.TryGetValue(prefix + "." + key, out languageString);
		}

		/// <summary>
		/// TryGet method which combines HasKey & GetString
		/// </summary>
		/// <param name="prefix">string with prefix</param>
		/// <param name="key">Enum with key</param>
		/// <param name="languageString">out string</param>
		/// <returns></returns>
		public static bool TryGetString(string prefix, Enum key, out string languageString)
		{
			return Resources.TryGetValue(prefix + "." + key, out languageString);
		}


		public static string Translate(object key) {
			string typename = key.GetType().Name;
			string enumKey = typename + "." + key;
			if (HasKey(enumKey)) {
				return GetString(enumKey);
			}
			return key.ToString();
		}

		/// <summary>
		/// Get the resource for key
		/// </summary>
		/// <param name="key"></param>
		/// <returns>resource or a "string ###key### not found"</returns>
		public static string GetString(Enum key) {
			if (key == null) {
				return null;
			}
			return GetString(key.ToString());
		}

		/// <summary>
		/// Get the resource for prefix.key
		/// </summary>
		/// <param name="prefix"></param>
		/// <param name="key"></param>
		/// <returns>resource or a "string ###prefix.key### not found"</returns>
		public static string GetString(string prefix, Enum key) {
			if (key == null) {
				return null;
			}
			return GetString(prefix + "." + key);
		}

		/// <summary>
		/// Get the resource for prefix.key
		/// </summary>
		/// <param name="prefix"></param>
		/// <param name="key"></param>
		/// <returns>resource or a "string ###prefix.key### not found"</returns>
		public static string GetString(string prefix, string key) {
			return GetString(prefix + "." + key);
		}

		/// <summary>
		/// Get the resource for key
		/// </summary>
		/// <param name="key"></param>
		/// <returns>resource or a "string ###key### not found"</returns>
		public static string GetString(string key) {
			if (key == null) {
				return null;
			}
			string returnValue;
			if (!Resources.TryGetValue(key, out returnValue)) {
				return "string ###" + key + "### not found";
			}
			return returnValue;
		}

		/// <summary>
		/// Get the resource for key, format with with string.format an supply the parameters
		/// </summary>
		/// <param name="key"></param>
		/// <param name="param"></param>
		/// <returns>formatted resource or a "string ###key### not found"</returns>
		public static string GetFormattedString(Enum key, object param) {
			return GetFormattedString(key.ToString(), param);
		}

		/// <summary>
		/// Get the resource for prefix.key, format with with string.format an supply the parameters
		/// </summary>
		/// <param name="prefix"></param>
		/// <param name="key"></param>
		/// <param name="param"></param>
		/// <returns>formatted resource or a "string ###prefix.key### not found"</returns>
		public static string GetFormattedString(string prefix, Enum key, object param) {
			return GetFormattedString(prefix, key.ToString(), param);
		}

		/// <summary>
		/// Get the resource for prefix.key, format with with string.format an supply the parameters
		/// </summary>
		/// <param name="prefix"></param>
		/// <param name="key"></param>
		/// <param name="param"></param>
		/// <returns>formatted resource or a "string ###prefix.key### not found"</returns>
		public static string GetFormattedString(string prefix, string key, object param) {
			return GetFormattedString(prefix + "." + key, param);
		}

		/// <summary>
		/// Get the resource for key, format with with string.format an supply the parameters
		/// </summary>
		/// <param name="key"></param>
		/// <param name="param"></param>
		/// <returns>formatted resource or a "string ###key### not found"</returns>
		public static string GetFormattedString(string key, object param) {
			string returnValue;
			if (!Resources.TryGetValue(key, out returnValue)) {
				return "string ###" + key + "### not found";
			}
			return string.Format(returnValue, param);
		}
	}

	/// <summary>
	/// This class contains the information about a language file
	/// </summary>
	public class LanguageFile : IEquatable<LanguageFile> {
		public string Description {
			get;
			set;
		}

		public string Ietf {
			get;
			set;
		}

		public Version Version {
			get;
			set;
		}

		public string LanguageGroup {
			get;
			set;
		}

		public string Filepath {
			get;
			set;
		}

		public string Prefix {
			get;
			set;
		}

		/// <summary>
		/// Overload equals so we can delete a entry from a collection
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(LanguageFile other) {
			if (Prefix != null) {
				if (other != null && !Prefix.Equals(other.Prefix)) {
					return false;
				}
			} else if (other?.Prefix != null) {
				return false;
			}
			if (Ietf != null) {
				if (other != null && !Ietf.Equals(other.Ietf)) {
					return false;
				}
			} else if (other?.Ietf != null) {
				return false;
			}
			if (Version != null) {
				if (other != null && !Version.Equals(other.Version)) {
					return false;
				}
			} else if (other != null && other.Version != null) {
				return false;
			}
			if (Filepath != null) {
				if (other != null && !Filepath.Equals(other.Filepath)) {
					return false;
				}
			} else if (other?.Filepath != null) {
				return false;
			}
			return true;
		}
	}

}
