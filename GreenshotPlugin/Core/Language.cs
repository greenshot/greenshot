#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using Dapplo.Ini;
using Dapplo.Log;
using Microsoft.Win32;

#endregion

namespace GreenshotPlugin.Core
{
    public delegate void LanguageChangedHandler(object sender, EventArgs e);

    /// <summary>
    ///     This class supplies the GUI with translations, based upon keys.
    ///     The language resources are loaded from the language files found on fixed or supplied paths
    /// </summary>
    public class Language
    {
        private const string DefaultLanguage = "en-US";
        private const string HelpFilenamePattern = @"help-*.html";
        private const string LanguageFilenamePattern = @"language*.xml";
        private const string LanguageGroupsKey = @"SYSTEM\CurrentControlSet\Control\Nls\Language Groups";
        private static readonly LogSource Log = new LogSource();
        private static readonly IList<string> LanguagePaths = new List<string>();
        private static readonly IDictionary<string, List<LanguageFile>> LanguageFiles = new Dictionary<string, List<LanguageFile>>();
        private static readonly IDictionary<string, string> HelpFiles = new Dictionary<string, string>();
        private static readonly Regex PrefixRegexp = new Regex(@"language_([a-zA-Z0-9]+).*");
        private static readonly Regex IetfCleanRegexp = new Regex(@"[^a-zA-Z]+");
        private static readonly Regex IetfRegexp = new Regex(@"^.*([a-zA-Z]{2,3}-[a-zA-Z]{1,2})\.xml$");
        private static readonly IList<string> UnsupportedLanguageGroups = new List<string>();
        private static readonly IDictionary<string, string> Resources = new Dictionary<string, string>();
        private static string _currentLanguage;

        /// <summary>
        ///     Static initializer for the language code
        /// </summary>
        static Language()
        {
            try
            {
                var location = string.IsNullOrEmpty(Assembly.GetExecutingAssembly().Location) ? new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath : Assembly.GetExecutingAssembly().Location;

                var applicationFolder = Path.GetDirectoryName(location);

                // PAF Path
                if (applicationFolder != null)
                {
                    AddPath(Path.Combine(applicationFolder, @"App\Greenshot\Languages"));
                }
                // Application data path
                var applicationDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                AddPath(Path.Combine(applicationDataFolder, @"Greenshot\Languages\"));

                // Startup path
                if (applicationFolder != null)
                {
                    AddPath(Path.Combine(applicationFolder, @"Languages"));
                }
            }
            catch (Exception pathException)
            {
                Log.Error().WriteLine(pathException);
            }

            try
            {
                using (var languageGroupsKey = Registry.LocalMachine.OpenSubKey(LanguageGroupsKey, false))
                {
                    if (languageGroupsKey != null)
                    {
                        var groups = languageGroupsKey.GetValueNames();
                        foreach (var group in groups)
                        {
                            var groupValue = (string) languageGroupsKey.GetValue(group);
                            var isGroupNotInstalled = "0".Equals(groupValue);
                            if (isGroupNotInstalled)
                            {
                                UnsupportedLanguageGroups.Add(group.ToLower());
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warn().WriteLine(e, "Couldn't read the installed language groups.");
            }

            var coreConfig = IniConfig.Current.Get<ICoreConfiguration>();
            ScanFiles();
            if (!string.IsNullOrEmpty(coreConfig.Language))
            {
                CurrentLanguage = coreConfig.Language;
                if (CurrentLanguage != null && CurrentLanguage != coreConfig.Language)
                {
                    coreConfig.Language = CurrentLanguage;
                }
            }

            if (CurrentLanguage == null)
            {
                Log.Warn().WriteLine("Couldn't set language from configuration, changing to default. Installation problem?");
                CurrentLanguage = DefaultLanguage;
                if (CurrentLanguage != null)
                {
                    coreConfig.Language = CurrentLanguage;
                }
            }

            if (CurrentLanguage == null)
            {
                Log.Error().WriteLine(null, "Couldn't set language, installation problem?");
            }
        }

        /// <summary>
        ///     Get or set the current language
        /// </summary>
        public static string CurrentLanguage
        {
            get { return _currentLanguage; }
            set
            {
                var ietf = FindBestIETFMatch(value);
                if (!LanguageFiles.ContainsKey(ietf))
                {
                    Log.Warn().WriteLine("No match for language {0} found!", ietf);
                }
                else
                {
                    if (_currentLanguage == null || !_currentLanguage.Equals(ietf))
                    {
                        _currentLanguage = ietf;
                        Reload();
                        if (LanguageChanged == null)
                        {
                            return;
                        }
                        try
                        {
                            LanguageChanged(null, null);
                        }
                        catch
                        {
                            // ignored
                        }
                        return;
                    }
                }
                Log.Debug().WriteLine("CurrentLanguage not changed!");
            }
        }

        /// <summary>
        ///     Return a list of all the supported languages
        /// </summary>
        public static IList<LanguageFile> SupportedLanguages
        {
            get
            {
                IList<LanguageFile> languages = new List<LanguageFile>();
                // Loop over all languages with all the files in there
                foreach (var langs in LanguageFiles.Values)
                {
                    // Loop over all the files for a language
                    foreach (var langFile in langs)
                    {
                        // Only take the ones without prefix, these are the "base" language files
                        if (langFile.Prefix == null)
                        {
                            languages.Add(langFile);
                            break;
                        }
                    }
                }
                return languages;
            }
        }

        /// <summary>
        ///     Return the path to the help-file
        /// </summary>
        public static string HelpFilePath
        {
            get
            {
                if (HelpFiles.ContainsKey(_currentLanguage))
                {
                    return HelpFiles[_currentLanguage];
                }
                return HelpFiles[DefaultLanguage];
            }
        }

        public static event LanguageChangedHandler LanguageChanged;

        /// <summary>
        ///     Internal method to add a path to the paths that will be scanned for language files!
        /// </summary>
        /// <param name="path"></param>
        /// <returns>true if the path exists and is added</returns>
        private static bool AddPath(string path)
        {
            if (!LanguagePaths.Contains(path))
            {
                if (Directory.Exists(path))
                {
                    Log.Debug().WriteLine("Adding language path {0}", path);
                    LanguagePaths.Add(path);
                    return true;
                }
                Log.Info().WriteLine("Not adding non existing language path {0}", path);
            }
            return false;
        }

        /// <summary>
        ///     Add a new path to the paths that will be scanned for language files!
        /// </summary>
        /// <param name="path"></param>
        /// <returns>true if the path exists and is added</returns>
        public static bool AddLanguageFilePath(string path)
        {
            if (!LanguagePaths.Contains(path))
            {
                Log.Debug().WriteLine("New language path {0}", path);
                if (AddPath(path))
                {
                    ScanFiles();
                    Reload();
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        ///     Load the files for the specified ietf
        /// </summary>
        /// <param name="ietf"></param>
        private static void LoadFiles(string ietf)
        {
            ietf = ReformatIETF(ietf);
            if (!LanguageFiles.ContainsKey(ietf))
            {
                Log.Error().WriteLine("No language {0} available.", ietf);
                return;
            }
            var filesToLoad = LanguageFiles[ietf];
            foreach (var fileToLoad in filesToLoad)
            {
                LoadResources(fileToLoad);
            }
        }

        /// <summary>
        ///     Load the language resources from the scanned files
        /// </summary>
        private static void Reload()
        {
            Resources.Clear();
            LoadFiles(DefaultLanguage);
            if (_currentLanguage != null && !_currentLanguage.Equals(DefaultLanguage))
            {
                LoadFiles(_currentLanguage);
            }
        }

        /// <summary>
        ///     Try to find the best match for the supplied IETF
        /// </summary>
        /// <param name="inputIETF"></param>
        /// <returns>IETF</returns>
        private static string FindBestIETFMatch(string inputIETF)
        {
            var returnIETF = inputIETF;
            if (string.IsNullOrEmpty(returnIETF))
            {
                returnIETF = DefaultLanguage;
            }
            returnIETF = ReformatIETF(returnIETF);
            if (!LanguageFiles.ContainsKey(returnIETF))
            {
                Log.Warn().WriteLine("Unknown language {0}, trying best match!", returnIETF);
                if (returnIETF.Length == 5)
                {
                    returnIETF = returnIETF.Substring(0, 2);
                }
                foreach (var availableIETF in LanguageFiles.Keys)
                {
                    if (availableIETF.StartsWith(returnIETF))
                    {
                        Log.Info().WriteLine("Found language {0}, best match for {1}!", availableIETF, returnIETF);
                        returnIETF = availableIETF;
                        break;
                    }
                }
            }
            return returnIETF;
        }

        /// <summary>
        ///     This helper method clears all non alpha characters from the IETF, and does a reformatting.
        ///     This prevents problems with multiple formats or typos.
        /// </summary>
        /// <param name="inputIETF"></param>
        /// <returns></returns>
        private static string ReformatIETF(string inputIETF)
        {
            string returnIETF = null;
            if (!string.IsNullOrEmpty(inputIETF))
            {
                returnIETF = inputIETF.ToLower();
                returnIETF = IetfCleanRegexp.Replace(returnIETF, "");
                if (returnIETF.Length == 4)
                {
                    returnIETF = returnIETF.Substring(0, 2) + "-" + returnIETF.Substring(2, 2).ToUpper();
                }
            }
            return returnIETF;
        }

        /// <summary>
        ///     Load the resources from the language file
        /// </summary>
        /// <param name="languageFile">File to load from</param>
        private static void LoadResources(LanguageFile languageFile)
        {
            Log.Info().WriteLine("Loading language file {0}", languageFile.Filepath);
            try
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.Load(languageFile.Filepath);
                var resourceNodes = xmlDocument.GetElementsByTagName("resource");
                foreach (XmlNode resourceNode in resourceNodes)
                {
                    var key = resourceNode.Attributes["name"].Value;
                    if (!string.IsNullOrEmpty(languageFile.Prefix))
                    {
                        key = languageFile.Prefix + "." + key;
                    }
                    var text = resourceNode.InnerText;
                    if (!string.IsNullOrEmpty(text))
                    {
                        text = text.Trim();
                    }
                    if (!Resources.ContainsKey(key))
                    {
                        Resources.Add(key, text);
                    }
                    else
                    {
                        Resources[key] = text;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error().WriteLine(e, "Could not load language file " + languageFile.Filepath);
            }
        }

        /// <summary>
        ///     Load the language file information
        /// </summary>
        /// <param name="languageFilePath"></param>
        /// <returns></returns>
        private static LanguageFile LoadFileInfo(string languageFilePath)
        {
            try
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.Load(languageFilePath);
                var nodes = xmlDocument.GetElementsByTagName("language");
                if (nodes.Count > 0)
                {
                    var languageFile = new LanguageFile
                    {
                        Filepath = languageFilePath
                    };
                    var node = nodes.Item(0);
                    if (node?.Attributes != null)
                    {
                        languageFile.Description = node.Attributes["description"].Value;
                        if (node.Attributes["ietf"] != null)
                        {
                            languageFile.Ietf = ReformatIETF(node.Attributes["ietf"].Value);
                        }
                        if (node.Attributes["version"] != null)
                        {
                            languageFile.Version = new Version(node.Attributes["version"].Value);
                        }
                        if (node.Attributes["prefix"] != null)
                        {
                            languageFile.Prefix = node.Attributes["prefix"].Value.ToLower();
                        }
                        if (node.Attributes["languagegroup"] != null)
                        {
                            var languageGroup = node.Attributes["languagegroup"].Value;
                            languageFile.LanguageGroup = languageGroup.ToLower();
                        }
                    }
                    return languageFile;
                }
                throw new XmlException("Root element <language> is missing");
            }
            catch (Exception e)
            {
                Log.Error().WriteLine(e, "Could not load language file " + languageFilePath);
            }
            return null;
        }

        /// <summary>
        ///     Scan the files in all directories
        /// </summary>
        private static void ScanFiles()
        {
            LanguageFiles.Clear();
            HelpFiles.Clear();
            foreach (var languagePath in LanguagePaths)
            {
                if (!Directory.Exists(languagePath))
                {
                    Log.Info().WriteLine("Skipping non existing language path {0}", languagePath);
                    continue;
                }
                Log.Info().WriteLine("Searching language directory '{0}' for language files with pattern '{1}'", languagePath, LanguageFilenamePattern);
                try
                {
                    foreach (var languageFilepath in Directory.GetFiles(languagePath, LanguageFilenamePattern, SearchOption.AllDirectories))
                    {
                        //Log.Debug().WriteLine("Found language file: {0}", languageFilepath);
                        var languageFile = LoadFileInfo(languageFilepath);
                        if (languageFile == null)
                        {
                            continue;
                        }
                        if (string.IsNullOrEmpty(languageFile.Ietf))
                        {
                            Log.Warn().WriteLine("Fixing missing ietf in language-file {0}", languageFilepath);
                            var languageFilename = Path.GetFileName(languageFilepath);
                            if (IetfRegexp.IsMatch(languageFilename))
                            {
                                var replacementIETF = IetfRegexp.Replace(languageFilename, "$1");
                                languageFile.Ietf = ReformatIETF(replacementIETF);
                                Log.Info().WriteLine("Fixed IETF to {0}", languageFile.Ietf);
                            }
                            else
                            {
                                Log.Error().WriteLine("Missing ietf , no recover possible... skipping language-file {0}!", languageFilepath);
                                continue;
                            }
                        }

                        // Check if we can display the file
                        if (!string.IsNullOrEmpty(languageFile.LanguageGroup) && UnsupportedLanguageGroups.Contains(languageFile.LanguageGroup))
                        {
                            Log.Info().WriteLine("Skipping unsuported (not able to display) language {0} from file {1}", languageFile.Description, languageFilepath);
                            continue;
                        }

                        // build prefix, based on the filename, but only if it's not set in the file itself.
                        if (string.IsNullOrEmpty(languageFile.Prefix))
                        {
                            var languageFilename = Path.GetFileNameWithoutExtension(languageFilepath);
                            if (PrefixRegexp.IsMatch(languageFilename))
                            {
                                languageFile.Prefix = PrefixRegexp.Replace(languageFilename, "$1");
                                if (!string.IsNullOrEmpty(languageFile.Prefix))
                                {
                                    languageFile.Prefix = languageFile.Prefix.Replace("plugin", "").ToLower();
                                }
                            }
                        }
                        List<LanguageFile> currentFiles = null;
                        if (LanguageFiles.ContainsKey(languageFile.Ietf))
                        {
                            currentFiles = LanguageFiles[languageFile.Ietf];
                            var needToAdd = true;
                            var deleteList = new List<LanguageFile>();
                            foreach (var compareWithLangfile in currentFiles)
                            {
                                if (languageFile.Prefix == null && compareWithLangfile.Prefix == null || languageFile.Prefix != null && languageFile.Prefix.Equals(compareWithLangfile.Prefix))
                                {
                                    if (compareWithLangfile.Version > languageFile.Version)
                                    {
                                        Log.Warn().WriteLine("Skipping {0}:{1}:{2} as {3}:{4}:{5} is newer", languageFile.Filepath, languageFile.Prefix, languageFile.Version, compareWithLangfile.Filepath,
                                            compareWithLangfile.Prefix, compareWithLangfile.Version);
                                        needToAdd = false;
                                        break;
                                    }
                                    Log.Warn().WriteLine("Found {0}:{1}:{2} and deleting {3}:{4}:{5}", languageFile.Filepath, languageFile.Prefix, languageFile.Version, compareWithLangfile.Filepath,
                                        compareWithLangfile.Prefix, compareWithLangfile.Version);
                                    deleteList.Add(compareWithLangfile);
                                }
                            }
                            if (needToAdd)
                            {
                                foreach (var deleteFile in deleteList)
                                {
                                    currentFiles.Remove(deleteFile);
                                }
                                Log.Info().WriteLine("Added language definition {0} from: {1}", languageFile.Description, languageFile.Filepath);
                                currentFiles.Add(languageFile);
                            }
                        }
                        else
                        {
                            currentFiles = new List<LanguageFile> {languageFile};
                            LanguageFiles.Add(languageFile.Ietf, currentFiles);
                            Log.Info().WriteLine("Added language definition {0} from: {1}", languageFile.Description, languageFile.Filepath);
                        }
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    Log.Info().WriteLine("Non existing language directory: {0}", languagePath);
                }
                catch (Exception e)
                {
                    Log.Error().WriteLine(e, "Error trying for read directory " + languagePath);
                }

                // Now find the help files
                Log.Info().WriteLine("Searching language directory '{0}' for help files with pattern '{1}'", languagePath, HelpFilenamePattern);
                try
                {
                    foreach (var helpFilepath in Directory.GetFiles(languagePath, HelpFilenamePattern, SearchOption.AllDirectories))
                    {
                        Log.Debug().WriteLine("Found help file: {0}", helpFilepath);
                        var helpFilename = Path.GetFileName(helpFilepath);
                        var ietf = ReformatIETF(helpFilename.Replace(".html", "").Replace("help-", ""));
                        if (!HelpFiles.ContainsKey(ietf))
                        {
                            HelpFiles.Add(ietf, helpFilepath);
                        }
                        else
                        {
                            Log.Warn().WriteLine("skipping help file {0}, already a file with the same IETF {1} found!", helpFilepath, ietf);
                        }
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    Log.Info().WriteLine("Non existing language directory: {0}", languagePath);
                }
                catch (Exception e)
                {
                    Log.Error().WriteLine(e, "Error trying for read directory " + languagePath);
                }
            }
        }

        /// <summary>
        ///     Check if a resource with prefix.key exists
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="key"></param>
        /// <returns>true if available</returns>
        public static bool HasKey(string prefix, Enum key)
        {
            if (key == null)
            {
                return false;
            }
            return HasKey(prefix + "." + key);
        }

        /// <summary>
        ///     Check if a resource with key exists
        /// </summary>
        /// <param name="key"></param>
        /// <returns>true if available</returns>
        public static bool HasKey(Enum key)
        {
            if (key == null)
            {
                return false;
            }
            return HasKey(key.ToString());
        }

        /// <summary>
        ///     Check if a resource with prefix.key exists
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="key"></param>
        /// <returns>true if available</returns>
        public static bool HasKey(string prefix, string key)
        {
            return HasKey(prefix + "." + key);
        }

        /// <summary>
        ///     Check if a resource with key exists
        /// </summary>
        /// <param name="key"></param>
        /// <returns>true if available</returns>
        public static bool HasKey(string key)
        {
            if (key == null)
            {
                return false;
            }
            return Resources.ContainsKey(key);
        }

        /// <summary>
        ///     TryGet method which combines HasKey & GetString
        /// </summary>
        /// <param name="key"></param>
        /// <param name="languageString">out string</param>
        /// <returns></returns>
        public static bool TryGetString(string key, out string languageString)
        {
            return Resources.TryGetValue(key, out languageString);
        }

        /// <summary>
        ///     TryGet method which combines HasKey & GetString
        /// </summary>
        /// <param name="prefix">string with prefix</param>
        /// <param name="key">string with key</param>
        /// <param name="languageString">out string</param>
        /// <returns></returns>
        public static bool TryGetString(string prefix, string key, out string languageString)
        {
            return Resources.TryGetValue(prefix + "." + key, out languageString);
        }

        /// <summary>
        ///     TryGet method which combines HasKey & GetString
        /// </summary>
        /// <param name="prefix">string with prefix</param>
        /// <param name="key">Enum with key</param>
        /// <param name="languageString">out string</param>
        /// <returns></returns>
        public static bool TryGetString(string prefix, Enum key, out string languageString)
        {
            return Resources.TryGetValue(prefix + "." + key, out languageString);
        }


        public static string Translate(object key)
        {
            var typename = key.GetType().Name;
            var enumKey = typename + "." + key;
            if (HasKey(enumKey))
            {
                return GetString(enumKey);
            }
            return key.ToString();
        }

        /// <summary>
        ///     Get the resource for key
        /// </summary>
        /// <param name="key"></param>
        /// <returns>resource or a "string ###key### not found"</returns>
        public static string GetString(Enum key)
        {
            if (key == null)
            {
                return null;
            }
            return GetString(key.ToString());
        }

        /// <summary>
        ///     Get the resource for prefix.key
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="key"></param>
        /// <returns>resource or a "string ###prefix.key### not found"</returns>
        public static string GetString(string prefix, Enum key)
        {
            if (key == null)
            {
                return null;
            }
            return GetString(prefix + "." + key);
        }

        /// <summary>
        ///     Get the resource for prefix.key
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="key"></param>
        /// <returns>resource or a "string ###prefix.key### not found"</returns>
        public static string GetString(string prefix, string key)
        {
            return GetString(prefix + "." + key);
        }

        /// <summary>
        ///     Get the resource for key
        /// </summary>
        /// <param name="key"></param>
        /// <returns>resource or a "string ###key### not found"</returns>
        public static string GetString(string key)
        {
            if (key == null)
            {
                return null;
            }
            string returnValue;
            if (!Resources.TryGetValue(key, out returnValue))
            {
                return "string ###" + key + "### not found";
            }
            return returnValue;
        }

        /// <summary>
        ///     Get the resource for key, format with with string.format an supply the parameters
        /// </summary>
        /// <param name="key"></param>
        /// <param name="param"></param>
        /// <returns>formatted resource or a "string ###key### not found"</returns>
        public static string GetFormattedString(Enum key, object param)
        {
            return GetFormattedString(key.ToString(), param);
        }

        /// <summary>
        ///     Get the resource for prefix.key, format with with string.format an supply the parameters
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="key"></param>
        /// <param name="param"></param>
        /// <returns>formatted resource or a "string ###prefix.key### not found"</returns>
        public static string GetFormattedString(string prefix, Enum key, object param)
        {
            return GetFormattedString(prefix, key.ToString(), param);
        }

        /// <summary>
        ///     Get the resource for prefix.key, format with with string.format an supply the parameters
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="key"></param>
        /// <param name="param"></param>
        /// <returns>formatted resource or a "string ###prefix.key### not found"</returns>
        public static string GetFormattedString(string prefix, string key, object param)
        {
            return GetFormattedString(prefix + "." + key, param);
        }

        /// <summary>
        ///     Get the resource for key, format with with string.format an supply the parameters
        /// </summary>
        /// <param name="key"></param>
        /// <param name="param"></param>
        /// <returns>formatted resource or a "string ###key### not found"</returns>
        public static string GetFormattedString(string key, object param)
        {
            string returnValue;
            if (!Resources.TryGetValue(key, out returnValue))
            {
                return "string ###" + key + "### not found";
            }
            return string.Format(returnValue, param);
        }
    }

    /// <summary>
    ///     This class contains the information about a language file
    /// </summary>
    public class LanguageFile : IEquatable<LanguageFile>
    {
        public string Description { get; set; }

        public string Ietf { get; set; }

        public Version Version { get; set; }

        public string LanguageGroup { get; set; }

        public string Filepath { get; set; }

        public string Prefix { get; set; }

        /// <summary>
        ///     Overload equals so we can delete a entry from a collection
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(LanguageFile other)
        {
            if (Prefix != null)
            {
                if (other != null && !Prefix.Equals(other.Prefix))
                {
                    return false;
                }
            }
            else if (other?.Prefix != null)
            {
                return false;
            }
            if (Ietf != null)
            {
                if (other != null && !Ietf.Equals(other.Ietf))
                {
                    return false;
                }
            }
            else if (other?.Ietf != null)
            {
                return false;
            }
            if (Version != null)
            {
                if (other != null && !Version.Equals(other.Version))
                {
                    return false;
                }
            }
            else if (other != null && other.Version != null)
            {
                return false;
            }
            if (Filepath != null)
            {
                if (other != null && !Filepath.Equals(other.Filepath))
                {
                    return false;
                }
            }
            else if (other?.Filepath != null)
            {
                return false;
            }
            return true;
        }
    }
}