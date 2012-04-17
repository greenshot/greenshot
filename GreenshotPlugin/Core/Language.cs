using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using System.Xml;
using Greenshot.IniFile;
using System.Text.RegularExpressions;

namespace GreenshotPlugin.Core {
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

		public bool Equals(LanguageFile other) {
			if (Prefix != null) {
				if (!Prefix.Equals(other.Prefix)) {
					return false;
				}
			} else if (other.Prefix != null) {
				return false;
			}
			if (Ietf != null) {
				if (!Ietf.Equals(other.Ietf)) {
					return false;
				}
			} else if (other.Ietf != null) {
				return false;
			}
			if (Version != null) {
				if (!Version.Equals(other.Version)) {
					return false;
				}
			} else if (other.Version != null) {
				return false;
			}
			if (Filepath != null) {
				if (!Filepath.Equals(other.Filepath)) {
					return false;
				}
			} else if (other.Filepath != null) {
				return false;
			}
			return true;
		}
	}

	class HelpFile {
		public string Ietf {
			get;
			set;
		}

		public string Filepath {
			get;
			set;
		}
	}

	public class Language {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(Language));
		private static List<string> languagePaths = new List<string>();
		private static IDictionary<string, List<LanguageFile>> languageFiles = new Dictionary<string, List<LanguageFile>>();
		private static IDictionary<string, HelpFile> helpFiles = new Dictionary<string, HelpFile>();
		private const string DEFAULT_LANGUAGE = "en-US";
		private const string HELP_FILENAME_PATTERN = @"help-*.html";
		private const string LANGUAGE_FILENAME_PATTERN = @"language*.xml";
		private static Regex PREFIX_REGEXP = new Regex(@"language_([a-zA-Z0-9]+).*");
		private const string LANGUAGE_GROUPS_KEY = @"SYSTEM\CurrentControlSet\Control\Nls\Language Groups";
		private static List<string> unsupportedLanguageGroups = new List<string>();
		private static IDictionary<string, string> resources = new Dictionary<string, string>();
		private static string currentLanguage = null;
		private static CoreConfiguration coreConfig = null;

		static Language() {
			if (!LogHelper.isInitialized) {
				LOG.Warn("Log4net hasn't been initialized yet! (Design mode?)");
				LogHelper.InitializeLog4NET();
			}
			if (!IniConfig.IsInited) {
				LOG.Warn("IniConfig hasn't been initialized yet! (Design mode?)");
				IniConfig.Init("greenshot", "greenshot");
			}
			string applicationDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string applicationFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

			// PAF Path
			AddPath(Path.Combine(applicationFolder, @"App\Greenshot\Languages"));

			// Application data path
			AddPath(Path.Combine(applicationDataFolder, @"Greenshot\Languages\"));

			// Startup path
			AddPath(Path.Combine(applicationFolder, @"Languages"));

			try {
				using (RegistryKey languageGroupsKey = Registry.LocalMachine.OpenSubKey(LANGUAGE_GROUPS_KEY, false)) {
					if (languageGroupsKey != null) {
						string [] groups = languageGroupsKey.GetValueNames();
						foreach(string group in groups) {
							string groupValue = (string)languageGroupsKey.GetValue(group);
							bool isGroupNotInstalled = "0".Equals(groupValue);
							if (isGroupNotInstalled) {
								unsupportedLanguageGroups.Add(group.ToLower());
							}
						}
					}
				}
			} catch(Exception e) {
				LOG.Warn("Couldn't read the installed language groups.", e);
			}

			coreConfig = IniConfig.GetIniSection<CoreConfiguration>();
			ScanFiles();
			if (!string.IsNullOrEmpty(coreConfig.Language)) {
				currentLanguage = coreConfig.Language;
			} else {
				currentLanguage = DEFAULT_LANGUAGE;
			}
			Reload();
		}

		private static bool AddPath(string path) {
			if (!languagePaths.Contains(path)) {
				if (Directory.Exists(path)) {
					LOG.DebugFormat("Adding language path {0}", path);
					languagePaths.Add(path);
				} else {
					LOG.InfoFormat("Not adding non existing language path {0}", path);
					return false;
				}
			}
			return true;
		}

		public static bool AddLanguageFilePath(string path) {
			if (!languagePaths.Contains(path)) {
				LOG.DebugFormat("New language path {0}", path);
				if (AddPath(path)) {
					ScanFiles();
					Reload();
				} else {
					return false;
				}
			}
			return true;
		}

		private static void LoadFiles(string ietf) {
			if (!languageFiles.ContainsKey(ietf)) {
				LOG.ErrorFormat("No language {0} available.", ietf);
				return;
			}
			List<LanguageFile> filesToLoad = languageFiles[ietf];
			foreach (LanguageFile fileToLoad in filesToLoad) {
				LoadResources(fileToLoad);
			}
		}

		private static void Reload() {
			resources.Clear();
			LoadFiles(DEFAULT_LANGUAGE);
			if (currentLanguage != null && !currentLanguage.Equals(DEFAULT_LANGUAGE)) {
				LoadFiles(currentLanguage);
			}
		}

		public static string CurrentLanguage {
			get {
				return currentLanguage;
			}
			set {
				LOG.DebugFormat("CurrentLangue = {0}", value);
				// Change the resources, if we can find the wanted value
				if (!string.IsNullOrEmpty(value) && currentLanguage == null || (currentLanguage != null && !currentLanguage.Equals(value))) {
					currentLanguage = value;
				}
				Reload();
			}
		}

		public static string LanguageName(string ietf) {
			return languageFiles[ietf][0].Description;
		}

		public static IList<LanguageFile> SupportedLanguages {
			get {
				IList<LanguageFile> languages = new List<LanguageFile>();
				foreach (List<LanguageFile> langs in languageFiles.Values) {
					foreach (LanguageFile langFile in langs) {
						if (langFile.Prefix == null) {
							languages.Add(langFile);
							break;
						}
					}
				}
				return languages;
			}
		}
		public static string HelpFilePath {
			get {
				if (helpFiles.ContainsKey(currentLanguage)) {
					return helpFiles[currentLanguage].Filepath;
				}
				return helpFiles[DEFAULT_LANGUAGE].Filepath;
			}
		}

		/// <summary>
		/// Load the resources from the language file
		/// </summary>
		/// <param name="languageFile">File to load from</param>
		private static void LoadResources(LanguageFile languageFile) {
			LOG.InfoFormat("Loading language file {0}", languageFile.Filepath);
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
					if (!resources.ContainsKey(key)) {
						resources.Add(key, text);
					} else {
						resources[key] = text;
					}
				}
			} catch (Exception e) {
				LOG.Error("Could not load language file " + languageFile.Filepath, e);
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
					LanguageFile languageFile = new LanguageFile();
					languageFile.Filepath = languageFilePath;
					XmlNode node = nodes.Item(0);
					languageFile.Description = node.Attributes["description"].Value;
					languageFile.Ietf = node.Attributes["ietf"].Value;
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
					return languageFile;
				} else {
					throw new XmlException("Root element <language> is missing");
				}
			} catch (Exception e) {
				LOG.Error("Could not load language file " + languageFilePath, e);
			}
			return null;
		}

		/// <summary>
		/// Scan the files in all directories
		/// </summary>
		private static void ScanFiles() {
			languageFiles.Clear();
			helpFiles.Clear();
			foreach (string languagePath in languagePaths) {
				if (!Directory.Exists(languagePath)) {
					LOG.InfoFormat("Skipping non existing language path {0}", languagePath);
					continue;
				}
				LOG.InfoFormat("Searching language directory '{0}' for language files with pattern '{1}'", languagePath, LANGUAGE_FILENAME_PATTERN);
				try {
					foreach (string languageFilepath in Directory.GetFiles(languagePath, LANGUAGE_FILENAME_PATTERN, SearchOption.AllDirectories)) {
						LOG.DebugFormat("Found language file: {0}", languageFilepath);
						LanguageFile languageFile = LoadFileInfo(languageFilepath);
						if (languageFile != null) {
							// build prefix
							if (string.IsNullOrEmpty(languageFile.Prefix)) {
								string languageFilename = Path.GetFileNameWithoutExtension(languageFilepath);
								if (PREFIX_REGEXP.IsMatch(languageFilename)) {
									languageFile.Prefix = PREFIX_REGEXP.Replace(languageFilename, "$1");
									if (!string.IsNullOrEmpty(languageFile.Prefix)) {
										languageFile.Prefix = languageFile.Prefix.Replace("plugin", "").ToLower();
									}
								}
							}
							if (string.IsNullOrEmpty(languageFile.LanguageGroup) || !unsupportedLanguageGroups.Contains(languageFile.LanguageGroup)) {
								List<LanguageFile> currentFiles = null;
								if (languageFiles.ContainsKey(languageFile.Ietf)) {
									currentFiles = languageFiles[languageFile.Ietf];
									bool needToAdd = true;
									List<LanguageFile> deleteList = new List<LanguageFile>();
									foreach (LanguageFile compareWithLangfile in currentFiles) {
										if ((languageFile.Prefix == null && compareWithLangfile.Prefix == null) || (languageFile.Prefix != null && languageFile.Prefix.Equals(compareWithLangfile.Prefix))) {
											if (compareWithLangfile.Version > languageFile.Version) {
												LOG.WarnFormat("Skipping {0}:{1}:{2} as {3}:{4}:{5} is newer", languageFile.Filepath, languageFile.Prefix, languageFile.Version, compareWithLangfile.Filepath, compareWithLangfile.Prefix, compareWithLangfile.Version);
												needToAdd = false;
												break;
											} else {
												LOG.WarnFormat("Found {0}:{1}:{2} and deleting {3}:{4}:{5}", languageFile.Filepath, languageFile.Prefix, languageFile.Version, compareWithLangfile.Filepath, compareWithLangfile.Prefix, compareWithLangfile.Version);
												deleteList.Add(compareWithLangfile);
											}
										}
									}
									if (needToAdd) {
										foreach (LanguageFile deleteFile in deleteList) {
											currentFiles.Remove(deleteFile);
										}
										LOG.InfoFormat("Added language {0} from: {1}", languageFile.Description, languageFile.Filepath);
										currentFiles.Add(languageFile);
									}
								} else {
									currentFiles = new List<LanguageFile>();
									currentFiles.Add(languageFile);
									languageFiles.Add(languageFile.Ietf, currentFiles);
									LOG.InfoFormat("Added language {0} from: {1}", languageFile.Description, languageFile.Filepath);
								}
							} else {
								LOG.InfoFormat("Skipping unsupported language: {0}", languageFile.Description);
							}
						}
					}
				} catch (DirectoryNotFoundException) {
					LOG.InfoFormat("Non existing language directory: {0}", languagePath);
				} catch (Exception e) {
					LOG.Error("Error trying for read directory " + languagePath, e);
				}
				LOG.InfoFormat("Searching language directory '{0}' for help files with pattern '{1}'", languagePath, HELP_FILENAME_PATTERN);
				try {
					foreach (string helpFilepath in Directory.GetFiles(languagePath, HELP_FILENAME_PATTERN, SearchOption.AllDirectories)) {
						LOG.DebugFormat("Found help file: {0}", helpFilepath);
						HelpFile helpFile = new HelpFile();
						helpFile.Filepath = helpFilepath;
						string helpFilename = Path.GetFileName(helpFilepath);
						helpFile.Ietf = helpFilename.Replace(".html", "").Replace("help-", "");
						if (!helpFiles.ContainsKey(helpFile.Ietf)) {
							helpFiles.Add(helpFile.Ietf, helpFile);
						} else {
							LOG.WarnFormat("skipping help file {0}, already a file with the same IETF found!", helpFilepath);
						}
					}
				} catch (DirectoryNotFoundException) {
					LOG.InfoFormat("Non existing language directory: {0}", languagePath);
				} catch (Exception e) {
					LOG.Error("Error trying for read directory " + languagePath, e);
				}
			}
		}

		public static string Dump() {
			int max = 40;
			StringBuilder dump = new StringBuilder();
			foreach (string key in resources.Keys) {
				dump.AppendFormat("{0}={1}", key, resources[key]).AppendLine();
				if (max-- == 0) {
					break;
				}
			}
			return dump.ToString();
		}
		public static bool hasKey(string prefix, Enum key) {
			if (key == null) {
				return false;
			}
			return hasKey(prefix + "." + key.ToString());
		}

		public static bool hasKey(Enum key) {
			if (key == null) {
				return false;
			}
			return hasKey(key.ToString());
		}

		public static bool hasKey(string prefix, string key) {
			return hasKey(prefix + "." + key);
		}

		public static bool hasKey(string key) {
			if (key == null) {
				return false;
			}
			return resources.ContainsKey(key);
		}

		public static string GetString(Enum key) {
			if (key == null) {
				return null;
			}
			return GetString(key.ToString());
		}

		public static string GetString(string prefix, Enum key) {
			if (key == null) {
				return null;
			}
			return GetString(prefix + "." + key.ToString());
		}

		public static string GetString(string prefix, string key) {
			return GetString(prefix + "." + key);
		}

		public static string GetString(string key) {
			if (key == null) {
				return null;
			}
			string returnValue;
			if (!resources.TryGetValue(key, out returnValue)) {
				return "string ###" + key + "### not found";
			}
			return returnValue;
		}

		public static string GetFormattedString(Enum key, object param) {
			return GetFormattedString(key.ToString(), param);
		}

		public static string GetFormattedString(string prefix, Enum key, object param) {
			return GetFormattedString(prefix, key.ToString(), param);
		}

		public static string GetFormattedString(string prefix, string key, object param) {
			return GetFormattedString(prefix + "." + key, param);
		}

		public static string GetFormattedString(string key, object param) {
			string returnValue;
			if (!resources.TryGetValue(key, out returnValue)) {
				return "string ###" + key + "### not found";
			}
			return String.Format(returnValue, param); ;
		}
	}
}
