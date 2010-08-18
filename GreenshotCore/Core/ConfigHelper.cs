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
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace Greenshot.Core {
	
	/// <summary>
	/// Attribute for telling that this class is linked to a section in the ini-configuration
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
	public class IniSectionAttribute : Attribute {
		private string name;
		public IniSectionAttribute(string name) {
			this.name = name;
		}
		public string Description;
		public string Name {
			get {return name;}
			set {name = value;}
		}
	}
	
	/// <summary>
	/// Attribute for telling that a field is linked to a property in the ini-configuration selection
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple=false)]
	public class IniPropertyAttribute : Attribute {
		private string name;
		public IniPropertyAttribute(string name) {
			this.name = name;
		}
		public string Description;
		public string DefaultValue;
		public string Name {
			get {return name;}
			set {name = value;}
		}
	}
	
	// Interface for 
	public class IniSection {
		// Flag to specify if values have been changed
		public bool IsDirty = false;
	}
	
	public class IniConfig {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(IniConfig));
		private const string CONFIG_FILE_NAME = "greenshot.ini";
		private const string DEFAULTS_CONFIG_FILE_NAME = "greenshot-defaults.ini";

		private static IniConfig instance = null;
		
		private string iniLocation = null;
		private Dictionary<string, IniSection> sectionMap = new Dictionary<string, IniSection>();
		private Dictionary<string, Dictionary<string, string >> iniProperties = new Dictionary<string, Dictionary<string, string>>();

		/// <summary>
		/// get an instance of IniConfig
		/// </summary>
		/// <returns></returns>
		public static IniConfig GetInstance() {
			if (instance == null) {
				instance = new IniConfig();
			}
			return instance;
		}
		
		/// <summary>
		/// Create the location of the configuration file
		/// </summary>
		private static string CreateIniLocation(string configFilename) {
			// check if file is in the same location as started from, if this is the case
			// we will use this file instead of the Applicationdate folder
			// Done for Feature Request #2741508
			if (File.Exists(Path.Combine(Application.StartupPath, configFilename))) {
				return Path.Combine(Application.StartupPath, configFilename);
			}
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),@"Greenshot\" + configFilename);
		}
		
		/// <summary>
		/// Private Constuctor, 
		/// </summary>
		/// <param name="iniLocation"></param>
		private IniConfig() {
			this.iniLocation = CreateIniLocation(CONFIG_FILE_NAME);
			// Load the defaults
			Read(CreateIniLocation(DEFAULTS_CONFIG_FILE_NAME));
			// Load the normal
			Read(CreateIniLocation(iniLocation));
		}
		
		/// <summary>
		/// Read the ini file into the Dictionary
		/// </summary>
		/// <param name="iniLocation">Path & Filename of ini file</param>
		private void Read(string iniLocation) {
			if (!File.Exists(iniLocation)) {
				LOG.Info("Can't find file: " + iniLocation);
				return;
			}
			LOG.Info("Reading ini-properties from file: " + iniLocation);

			String currentSection = null;
			foreach (string line in File.ReadAllLines(iniLocation, Encoding.UTF8)) {
				if (line == null) {
					continue;
				}
				string currentLine = line.TrimStart();
				if (currentLine.StartsWith("[")) {
					currentSection = currentLine.Substring(1, currentLine.IndexOf(']')-1);
					LOG.Debug("Found section: " + currentSection);
				} else if (!currentLine.StartsWith(";") && currentLine.IndexOf('=') > 0) {
					string [] split = currentLine.Split(new Char[] {'='}, 2);
					if (split != null && split.Length == 2) {
						string name = split[0];
						if (name == null || name.Length < 1) {
							continue;
						}
						name = name.Trim();
						String value = split[1];
						if (!HasProperty(currentSection, name)) {
							SetProperty(currentSection, name, value);
						} else {
							ChangeProperty(currentSection, name, value);
						}
					}
				}
			}
		}

		/// <summary>
		/// A generic method which returns an instance of the supplied type, filled with it's configuration
		/// </summary>
		/// <returns>Filled instance of IniSection type which was supplied</returns>
		public T GetSection<T>() where T : IniSection {
			T section;
			Type iniSectionType = typeof(T);
			string sectionName = getSectionName(iniSectionType);
			if (sectionMap.ContainsKey(sectionName)) {
				section = (T)sectionMap[sectionName];
			} else {
				// Create instance of this type
				section = (T)Activator.CreateInstance(iniSectionType);

				// Store for later save & retrieval
				sectionMap.Add(sectionName, section);

				// Get the properties for the section
				Dictionary<string, string> properties = null;
				if (iniProperties.ContainsKey(sectionName)) {
					properties = iniProperties[sectionName];
				}

				// Iterate over the fields and fill them
				FieldInfo[] fields = iniSectionType.GetFields();
				foreach(FieldInfo field in fields) {
					if (Attribute.IsDefined(field, typeof(IniPropertyAttribute))) {
						IniPropertyAttribute iniPropertyAttribute = (IniPropertyAttribute)field.GetCustomAttributes(typeof(IniPropertyAttribute), false)[0];
						string propertyName = iniPropertyAttribute.Name;
						string propertyValue = null;
						// Get the value from the ini file, if there is none take the default
						if (properties != null && properties.ContainsKey(propertyName)) {
							propertyValue = properties[propertyName];
						} else {
							// Mark as dirty, we didn't use properties from the file (even defaults from the default file are allowed)
							section.IsDirty = true;
							propertyValue = iniPropertyAttribute.DefaultValue;
							LOG.Debug("Using default property for " + propertyName + " : " + propertyValue);
						}

						// Get the type, or the underlying type for nullables
						Type fieldType = field.FieldType;

						// Now set the value
						if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>)) {
							string[] arrayValues = propertyValue.Split(new Char[] {','});
							if (arrayValues != null) {
								object list = Activator.CreateInstance(fieldType);
								MethodInfo methodInfo = fieldType.GetMethod("Add");
								
								foreach(string arrayValue in arrayValues) {
									if (arrayValue != null && arrayValue.Length > 0) {
										object newValue = ConvertValueToFieldType(fieldType.GetGenericArguments()[0], arrayValue);
										LOG.Debug("Adding: " + newValue);
										if (newValue != null) {
											methodInfo.Invoke(list, new object[] {newValue});
										}
									}
								}
								field.SetValue(section, list);
								
							}
						} else {
							if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition().Equals(typeof(Nullable<>))) {
								// We are dealing with a generic type that is nullable
								fieldType = Nullable.GetUnderlyingType(fieldType);
							}
							field.SetValue(section,ConvertValueToFieldType(fieldType, propertyValue));
						}
					}
				}
			}
			return section;
		}
		
		private List<T> CreateList<T>() {
			List<T> mylist = new List<T>();
			return mylist;
		}
		private object ConvertValueToFieldType(Type fieldType, string value) {
			if (value == null && value.Length == 0) {
				return null;
			}
			if (fieldType == typeof(string)) {
				return value;
			} else if (fieldType == typeof(bool) || fieldType == typeof(bool?)) {
				return bool.Parse(value);
			} else if (fieldType == typeof(int) || fieldType == typeof(int?)) {
				return int.Parse(value);
			} else if (fieldType.IsEnum) {
				try {
					return Enum.Parse(fieldType, value);
				} catch (Exception e) {
					LOG.Error("Can't parse value: " + value, e);
				}
			}
			return null;
		}

		private string getSectionName(Type iniSectionType) {
			Attribute[] classAttributes = Attribute.GetCustomAttributes(iniSectionType);
			foreach(Attribute attribute in classAttributes) {
				if (attribute is IniSectionAttribute) {
					IniSectionAttribute iniSectionAttribute = (IniSectionAttribute)attribute;
					return iniSectionAttribute.Name;
				}
			}
			return null;
		}

		private Dictionary<string, string> getProperties(string section) {
			if (iniProperties.ContainsKey(section)) {
				return iniProperties[section];
			}
			return null;
		}

		public void ChangeProperty(string section, string name, string value) {
			if (section != null) {
				Dictionary<string, string> properties = iniProperties[section];
				properties[name] = value;
				LOG.Debug(String.Format("Changed property {0} to section: {0}.", name, section));
			} else {
				LOG.Debug("Property without section: " + name);
			}
		}

		public void SetProperty(string section, string name, string value) {
			if (section != null) {
				Dictionary<string, string> properties = null;
				if (!iniProperties.ContainsKey(section)) {
					properties = new Dictionary<string, string>();
					iniProperties.Add(section, properties);
				} else {
					properties = iniProperties[section];
				}
				properties.Add(name, value);
				LOG.Debug(String.Format("Added property {0} to section: {0}.", name, section));
			} else {
				LOG.Debug("Property without section: " + name);
			}
		}


		public bool HasProperty(string section, string name) {
			Dictionary<string, string> properties = getProperties(section);
			if (properties != null && properties.ContainsKey(name)) {
				return true;
			}
			return false;
		}
		
		public string GetProperty(string section, string name) {
			Dictionary<string, string> properties = getProperties(section);
			if (properties != null && properties.ContainsKey(name)) {
				return properties[name];
			}
			return null;
		}

		/// <summary>
		/// Split property with ',' and return the splitted string as a string[]
		/// </summary>
		public string[] GetPropertyAsArray(string section, string key) {
			Dictionary<string, string> properties = getProperties(section);
			string value = GetProperty(section, key);
			if (value != null) {
				return value.Split(new Char[] {','});
			}
			return null;
		}

		public bool GetBoolProperty(string section, string key) {
			Dictionary<string, string> properties = getProperties(section);
			string value = GetProperty(section, key);
			return bool.Parse(value);
		}

		public int GetIntProperty(string section, string key) {
			Dictionary<string, string> properties = getProperties(section);
			string value = GetProperty(section, key);
			return int.Parse(value);
		}

		public void Save() {
			LOG.Info("Saving configuration to: " + iniLocation);
			TextWriter writer = new StreamWriter(iniLocation, false, Encoding.UTF8);
			foreach(IniSection section in sectionMap.Values) {
				Type classType = section.GetType();
				Attribute[] classAttributes = Attribute.GetCustomAttributes(classType);
				foreach(Attribute attribute in classAttributes) {
					if (attribute is IniSectionAttribute) {
						IniSectionAttribute iniSectionAttribute = (IniSectionAttribute)attribute;
						writer.WriteLine("; {0}", iniSectionAttribute.Description);
						writer.WriteLine("[{0}]", iniSectionAttribute.Name);
						FieldInfo[] fields = classType.GetFields();
						foreach(FieldInfo field in fields) {
							if (Attribute.IsDefined(field, typeof(IniPropertyAttribute))) {
								IniPropertyAttribute iniPropertyAttribute = (IniPropertyAttribute)field.GetCustomAttributes(typeof(IniPropertyAttribute), false)[0];
								writer.WriteLine("; {0}", iniPropertyAttribute.Description);
								object value = field.GetValue(section);
								Type fieldType = field.FieldType;
								if (value == null) {
									value = iniPropertyAttribute.DefaultValue;
									fieldType = typeof(string);
								}
								if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>)) {
									writer.Write("{0}=", iniPropertyAttribute.Name);
									int listCount = (int)fieldType.GetProperty("Count").GetValue(value, null);
									// Loop though generic list
									for (int index = 0; index < listCount; index++) {
									   object item = fieldType.GetMethod("get_Item").Invoke(value, new object[] { index });
									   // Now you have an instance of the item in the generic list
									   if (index < listCount -1) {
										   writer.Write("{0},", item);
									   } else {
										   writer.Write("{0}", item);
									   }
									}
									writer.WriteLine();
								} else {
									writer.WriteLine("{0}={1}", iniPropertyAttribute.Name, value);
								}
							}
						}
					}
				}
				writer.WriteLine();
				section.IsDirty = false;
			}
			
			// Write left over properties
			foreach(string sectionName in iniProperties.Keys) {
				// Check if the section is one that is "registered", if so skip it!
				if (!sectionMap.ContainsKey(sectionName)) {
					// Write section name
					writer.WriteLine("[{0}]", sectionName);
					Dictionary<string, string> properties = iniProperties[sectionName];
					// Loop and write properties
					foreach(string propertyName in properties.Keys) {
						writer.WriteLine("{0}={1}", propertyName, properties[propertyName]);
					}
				}
			}
			writer.Close();
		}
	}
}
