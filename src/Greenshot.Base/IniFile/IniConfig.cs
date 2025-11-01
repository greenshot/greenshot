/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using log4net;

namespace Greenshot.Base.IniFile
{
    public class IniConfig
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(IniConfig));
        private const string IniExtension = ".ini";
        private const string DefaultsPostfix = "-defaults";
        private const string FixedPostfix = "-fixed";

        /// <summary>
        /// A lock object for the ini file saving
        /// </summary>
        private static readonly object IniLock = new object();

        /// <summary>
        /// A lock object for the section map
        /// </summary>
        private static readonly object SectionMapLock = new object();

        /// <summary>
        /// As the ini implementation is kept someone generic, for reusing, this holds the name of the application
        /// </summary>
        private static string _applicationName;

        /// <summary>
        /// As the ini implementation is kept someone generic, for reusing, this holds the name of the configuration
        /// </summary>
        private static string _configName;

        /// <summary>
        /// A Dictionary with all the sections stored by section name
        /// </summary>
        private static readonly IDictionary<string, IniSection> SectionMap = new Dictionary<string, IniSection>();

        /// <summary>
        /// A Dictionary with the properties for a section stored by section name
        /// </summary>
        private static IDictionary<string, IDictionary<string, string>> _sections = new Dictionary<string, IDictionary<string, string>>();

        /// <summary>
        /// A Dictionary with the fixed-properties for a section stored by section name
        /// </summary>
        private static IDictionary<string, IDictionary<string, string>> _fixedProperties;

        /// <summary>
        /// Stores if we checked for portable
        /// </summary>
        private static bool _portableCheckMade;

        /// <summary>
        /// Is the configuration portable (meaning we don't store it in the AppData directory)
        /// </summary>
        public static bool IsPortable { get; private set; }

        /// <summary>
        /// Config directory when set from external
        /// </summary>
        public static string IniDirectory { get; set; }

        /// <summary>
        /// Initialize the ini config
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="configName"></param>
        public static void Init(string appName, string configName)
        {
            _applicationName = appName;
            _configName = configName;
            Reload();
        }

        /// <summary>
        /// Checks if we initialized the ini
        /// </summary>
        public static bool IsInitialized => _applicationName != null && _configName != null && SectionMap.Count > 0;

        /// <summary>
        /// This forces the ini to be stored in the startup path, used for portable applications
        /// </summary>
        public static void ForceIniInStartupPath()
        {
            if (_portableCheckMade)
            {
                throw new Exception("ForceLocal should be called before any file is read");
            }

            IsPortable = false;
            _portableCheckMade = true;
            string applicationStartupPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if (_applicationName == null || _configName == null)
            {
                Init();
            }

            if (applicationStartupPath == null)
            {
                return;
            }

            string forcedIni = Path.Combine(applicationStartupPath, _applicationName + IniExtension);
            if (!File.Exists(forcedIni))
            {
                using (File.Create(forcedIni))
                {
                }
            }
        }

        /// <summary>
        /// Default init
        /// </summary>
        public static void Init()
        {
            AssemblyProductAttribute[] assemblyProductAttributes =
                Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false) as AssemblyProductAttribute[];
            if (assemblyProductAttributes is { Length: > 0 })
            {
                string productName = assemblyProductAttributes[0].Product;
                Log.InfoFormat("Using ProductName {0}", productName);
                Init(productName, productName);
            }
            else
            {
                throw new InvalidOperationException("Assembly ProductName not set.");
            }
        }

        /// <summary>
        /// Get the location of the configuration
        /// </summary>
        public static string ConfigLocation
        {
            get
            {
                if (IsInitialized)
                {
                    return CreateIniLocation(_configName + IniExtension, false);
                }

                throw new InvalidOperationException("Ini configuration was not initialized!");
            }
        }

        /// <summary>
        /// Create the location of the configuration file
        /// </summary>
        private static string CreateIniLocation(string configFilename, bool isReadOnly)
        {
            if (_applicationName == null || _configName == null)
            {
                throw new InvalidOperationException("IniConfig.Init not called!");
            }

            string iniFilePath = null;

            // Check if a Ini-Directory was supplied, and it's valid, use this before any others.
            try
            {
                if (IniDirectory != null && Directory.Exists(IniDirectory))
                {
                    // If the greenshot.ini is requested, use the supplied directory even if empty
                    if (!isReadOnly)
                    {
                        return Path.Combine(IniDirectory, configFilename);
                    }

                    iniFilePath = Path.Combine(IniDirectory, configFilename);
                    if (File.Exists(iniFilePath))
                    {
                        return iniFilePath;
                    }

                    iniFilePath = null;
                }
            }
            catch (Exception exception)
            {
                Log.WarnFormat("The ini-directory {0} can't be used due to: {1}", IniDirectory, exception.Message);
            }

            string applicationStartupPath;
            try
            {
                applicationStartupPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            }
            catch (Exception exception)
            {
                Log.WarnFormat("Problem retrieving the AssemblyLocation: {0} (Designer mode?)", exception.Message);
                applicationStartupPath = @".";
            }

            if (applicationStartupPath != null)
            {
                string pafPath = Path.Combine(applicationStartupPath, @"App\" + _applicationName);

                if (IsPortable || !_portableCheckMade)
                {
                    if (!IsPortable)
                    {
                        Log.Info("Checking for portable mode.");
                        _portableCheckMade = true;
                        if (Directory.Exists(pafPath))
                        {
                            IsPortable = true;
                            Log.Info("Portable mode active!");
                        }
                    }

                    if (IsPortable)
                    {
                        string pafConfigPath = Path.Combine(applicationStartupPath, @"Data\Settings");
                        try
                        {
                            if (!Directory.Exists(pafConfigPath))
                            {
                                Directory.CreateDirectory(pafConfigPath);
                            }

                            iniFilePath = Path.Combine(pafConfigPath, configFilename);
                        }
                        catch (Exception e)
                        {
                            Log.InfoFormat("Portable mode NOT possible, couldn't create directory '{0}'! Reason: {1}", pafConfigPath, e.Message);
                        }
                    }
                }
            }

            if (iniFilePath == null)
            {
                // check if file is in the same location as started from, if this is the case
                // we will use this file instead of the ApplicationData folder
                // Done for Feature Request #2741508
                if (applicationStartupPath != null)
                {
                    iniFilePath = Path.Combine(applicationStartupPath, configFilename);
                }

                if (!File.Exists(iniFilePath))
                {
                    string iniDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _applicationName);
                    if (!Directory.Exists(iniDirectory))
                    {
                        Directory.CreateDirectory(iniDirectory);
                    }

                    iniFilePath = Path.Combine(iniDirectory, configFilename);
                }
            }

            Log.InfoFormat("Using ini file {0}", iniFilePath);
            return iniFilePath;
        }

        /// <summary>
        /// Reload the Ini file
        /// </summary>
        public static void Reload()
        {
            // Clear the current properties
            _sections = new Dictionary<string, IDictionary<string, string>>();
            // Load the defaults
            Read(CreateIniLocation(_configName + DefaultsPostfix + IniExtension, true));
            // Load the normal
            Read(CreateIniLocation(_configName + IniExtension, false));
            // Load the fixed settings
            _fixedProperties = Read(CreateIniLocation(_configName + FixedPostfix + IniExtension, true));

            lock (SectionMapLock)
            {
                foreach (IniSection section in SectionMap.Values)
                {
                    try
                    {
                        section.Fill(PropertiesForSection(section));
                        FixProperties(section);
                    }
                    catch (Exception ex)
                    {
                        string sectionName = "unknown";
                        if (section?.IniSectionAttribute?.Name != null)
                        {
                            sectionName = section.IniSectionAttribute.Name;
                        }

                        Log.WarnFormat("Problem reading the ini section {0}", sectionName);
                        Log.Warn("Exception", ex);
                    }
                }
            }
        }

        /// <summary>
        /// This "fixes" the properties of the section, meaning any properties in the fixed file can't be changed.
        /// </summary>
        /// <param name="section">IniSection</param>
        private static void FixProperties(IniSection section)
        {
            // Make properties unchangeable
            if (_fixedProperties == null)
            {
                return;
            }

            if (!_fixedProperties.TryGetValue(section.IniSectionAttribute.Name, out var fixedPropertiesForSection))
            {
                return;
            }

            foreach (string fixedPropertyKey in fixedPropertiesForSection.Keys)
            {
                if (section.Values.ContainsKey(fixedPropertyKey))
                {
                    section.Values[fixedPropertyKey].IsFixed = true;
                }
            }
        }

        /// <summary>
        /// Read the ini file into the Dictionary
        /// </summary>
        /// <param name="iniLocation">Path & Filename of ini file</param>
        private static IDictionary<string, IDictionary<string, string>> Read(string iniLocation)
        {
            if (!File.Exists(iniLocation))
            {
                Log.Info("Can't find file: " + iniLocation);
                return null;
            }

            Log.InfoFormat("Loading ini-file: {0}", iniLocation);
            //LOG.Info("Reading ini-properties from file: " + iniLocation);
            var newSections = IniReader.Read(iniLocation, Encoding.UTF8);
            // Merge the newly loaded properties to the already available
            foreach (string section in newSections.Keys)
            {
                IDictionary<string, string> newProperties = newSections[section];
                if (!_sections.ContainsKey(section))
                {
                    // This section is not yet loaded, simply add the complete section
                    _sections.Add(section, newProperties);
                }
                else
                {
                    // Overwrite or add every property from the newly loaded section to the available one
                    var currentProperties = _sections[section];
                    foreach (string propertyName in newProperties.Keys)
                    {
                        string propertyValue = newProperties[propertyName];
                        if (currentProperties.ContainsKey(propertyName))
                        {
                            // Override current value as we are loading in a certain order which insures the default, current and fixed
                            currentProperties[propertyName] = propertyValue;
                        }
                        else
                        {
                            // Add "new" value
                            currentProperties.Add(propertyName, propertyValue);
                        }
                    }
                }
            }

            return newSections;
        }

        public static IEnumerable<string> IniSectionNames
        {
            get
            {
                List<string> sectionNames;
                lock (SectionMapLock)
                {
                    sectionNames = [.. SectionMap.Keys];
                }
                foreach (string sectionName in sectionNames)
                {
                    yield return sectionName;
                }
            }
        }

        /// <summary>
        /// Method used for internal tricks...
        /// </summary>
        /// <param name="sectionName"></param>
        /// <returns></returns>
        public static IniSection GetIniSection(string sectionName)
        {
            lock (SectionMapLock)
            {
                SectionMap.TryGetValue(sectionName, out var returnValue);
                return returnValue;
            }
        }
                    
        /// <summary>
        /// A generic method which returns an instance of the supplied type, filled with it's configuration
        /// </summary>
        /// <typeparam name="T">IniSection Type to get the configuration for</typeparam>
        /// <returns>Filled instance of IniSection type which was supplied</returns>
        public static T GetIniSection<T>() where T : IniSection
        {
            return GetIniSection<T>(true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">IniSection Type to get the configuration for</typeparam>
        /// <param name="allowSave">false to skip saving</param>
        /// <returns>IniSection</returns>
        public static T GetIniSection<T>(bool allowSave) where T : IniSection
        {
            T section;

            Type iniSectionType = typeof(T);
            string sectionName = IniSection.GetIniSectionAttribute(iniSectionType).Name;

            lock (SectionMapLock)
            {
                if (SectionMap.ContainsKey(sectionName))
                {
                //LOG.Debug("Returning pre-mapped section " + sectionName);
                    section = (T)SectionMap[sectionName];
                }
                else
                {
                // Create instance of this type
                    section = (T)Activator.CreateInstance(iniSectionType);

                // Store for later save & retrieval
                    SectionMap.Add(sectionName, section);
                    section.Fill(PropertiesForSection(section));
                    FixProperties(section);
                }
            }

            if (allowSave && section.IsDirty)
            {
                Log.DebugFormat("Section {0} is marked dirty, saving!", sectionName);
                Save();
            }

            return section;
        }

        /// <summary>
        /// Get the raw properties for a section
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        public static IDictionary<string, string> PropertiesForSection(IniSection section)
        {
            string sectionName = section.IniSectionAttribute.Name;
            // Get the properties for the section
            IDictionary<string, string> properties;
            if (_sections.ContainsKey(sectionName))
            {
                properties = _sections[sectionName];
            }
            else
            {
                _sections.Add(sectionName, new Dictionary<string, string>());
                properties = _sections[sectionName];
            }

            return properties;
        }

        /// <summary>
        /// Save the ini file
        /// </summary>
        public static void Save()
        {
            bool acquiredLock = false;
            try
            {
                acquiredLock = Monitor.TryEnter(IniLock, TimeSpan.FromMilliseconds(200));
                if (acquiredLock)
                {
                    // Code that accesses resources that are protected by the lock.
                    string iniLocation = CreateIniLocation(_configName + IniExtension, false);
                    try
                    {
                        SaveInternally(iniLocation);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("A problem occurred while writing the configuration file to: " + iniLocation);
                        Log.Error(ex);
                    }
                }
                else
                {
                    // Code to deal with the fact that the lock was not acquired.
                    Log.Warn("A second thread tried to save the ini-file, we blocked as the first took too long.");
                }
            }
            finally
            {
                if (acquiredLock)
                {
                    Monitor.Exit(IniLock);
                }
            }
        }

        /// <summary>
        /// The real save implementation
        /// </summary>
        /// <param name="iniLocation"></param>
        private static void SaveInternally(string iniLocation)
        {
            Log.Info("Saving configuration to: " + iniLocation);
            var iniPath = Path.GetDirectoryName(iniLocation);
            if (iniPath != null && !Directory.Exists(iniPath))
            {
                Directory.CreateDirectory(iniPath);
            }

            using var memoryStream = new MemoryStream();
            using TextWriter writer = new StreamWriter(memoryStream, Encoding.UTF8);
            foreach (var section in SectionMap.Values)
            {
                section.Write(writer, false);
                // Add empty line after section
                writer.WriteLine();
                section.IsDirty = false;
            }

            writer.WriteLine();
            // Write left over properties
            foreach (string sectionName in _sections.Keys)
            {
                // Check if the section is one that is "registered", if so skip it!
                if (SectionMap.ContainsKey(sectionName))
                {
                    continue;
                }

                writer.WriteLine("; The section {0} hasn't been 'claimed' since the last start of Greenshot, therefor it doesn't have additional information here!", sectionName);
                writer.WriteLine(
                    "; The reason could be that the section {0} just hasn't been used, a plugin has an error and can't claim it or maybe the whole section {0} is obsolete.",
                    sectionName);
                // Write section name
                writer.WriteLine("[{0}]", sectionName);
                var properties = _sections[sectionName];
                // Loop and write properties
                foreach (string propertyName in properties.Keys)
                {
                    writer.WriteLine("{0}={1}", propertyName, properties[propertyName]);
                }

                writer.WriteLine();
            }

            // Don't forget to flush the buffer
            writer.Flush();
            // Now write the created .ini string to the real file
            using FileStream fileStream = new FileStream(iniLocation, FileMode.Create, FileAccess.Write);
            memoryStream.WriteTo(fileStream);
        }
    }
}