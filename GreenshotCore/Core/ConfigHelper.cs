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
using System.Drawing;
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
	
	/// <summary>
	/// Base class for all IniSections
	/// </summary>
	public abstract class IniSection {
		/// Flag to specify if values have been changed
		public bool IsDirty = false;

		/// <summary>
		/// Supply values we can't put as defaults
		/// </summary>
		/// <param name="property">The property to return a default for</param>
		/// <returns>object with the default value for the supplied property</returns>
		public virtual object GetDefault(string property) {
			return null;
		}
	}
	
	public class IniConfig {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(IniConfig));
		private const string CONFIG_FILE_NAME = "greenshot.ini";
		private const string DEFAULTS_CONFIG_FILE_NAME = "greenshot-defaults.ini";

		/// <summary>
		/// Static code for loading
		/// </summary>
		static IniConfig() {
			iniLocation = CreateIniLocation(CONFIG_FILE_NAME);
			// Load the defaults
			Read(CreateIniLocation(DEFAULTS_CONFIG_FILE_NAME));
			// Load the normal
			Read(CreateIniLocation(iniLocation));
		}
		
		private static string iniLocation = null;
		private static Dictionary<string, IniSection> sectionMap = new Dictionary<string, IniSection>();
		private static Dictionary<string, Dictionary<string, string >> iniProperties = new Dictionary<string, Dictionary<string, string>>();

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
		/// Read the ini file into the Dictionary
		/// </summary>
		/// <param name="iniLocation">Path & Filename of ini file</param>
		private static void Read(string iniLocation) {
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
		public static T GetIniSection<T>() where T : IniSection {
			T section;
			
			Type iniSectionType = typeof(T);
			string sectionName = getSectionName(iniSectionType);
			LOG.Debug("Trying to find section for: " + sectionName);
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
						string propertyDefaultValue = iniPropertyAttribute.DefaultValue;

						string propertyValue = null;
						// Get the value from the ini file, if there is none take the default
						if (properties != null && properties.ContainsKey(propertyName)) {
							propertyValue = properties[propertyName];
						} else if (propertyDefaultValue != null) {
							// Mark as dirty, we didn't use properties from the file (even defaults from the default file are allowed)
							section.IsDirty = true;
							propertyValue = propertyDefaultValue;
							LOG.Debug("Using default: " + propertyName + "=" + propertyValue);
						} else if (propertyValue == null) {
							// Use GetDefault to fill the field if none is set
							object defaultValue = section.GetDefault(propertyName);
							if (defaultValue != null) {
								field.SetValue(section, defaultValue);
								continue;
							} else {
								LOG.Debug("No default value, setting to null");
								continue;
							}
						}

						// Get the type, or the underlying type for nullables
						Type fieldType = field.FieldType;
						try {
							field.SetValue(section, CreateFieldValue(fieldType, sectionName, propertyName, propertyValue));
						} catch (Exception) {
							// Mark as dirty, we didn't use properties from the file (even defaults from the default file are allowed)
							section.IsDirty = true;
							field.SetValue(section, CreateFieldValue(fieldType, sectionName, propertyName, propertyDefaultValue));
						}
					}
				}
			}
			return section;
		}
		
		/// <summary>
		/// Helper method for creating a value
		/// </summary>
		/// <param name="fieldType">Type of the value to create</param>
		/// <param name="propertyValue">Value as string</param>
		/// <returns>object instance of the value</returns>
		private static object CreateFieldValue(Type fieldType, string sectionName, string propertyName, string propertyValue) {
			// Now set the value
			if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>)) {
				// Logic for List<>
				string[] arrayValues = propertyValue.Split(new Char[] {','});
				if (arrayValues != null) {
					object list = Activator.CreateInstance(fieldType);
					MethodInfo addMethodInfo = fieldType.GetMethod("Add");
					
					foreach(string arrayValue in arrayValues) {
						if (arrayValue != null && arrayValue.Length > 0) {
							object newValue = null;
							try {
								newValue = ConvertValueToFieldType(fieldType.GetGenericArguments()[0], arrayValue);
							} catch (Exception e) {
								LOG.Error("Problem converting " + fieldType.FullName, e);
							}
							if (newValue != null) {
								addMethodInfo.Invoke(list, new object[] {newValue});
							}
						}
					}
					return list;
				}
			} else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
				// Logic for Dictionary<,>
				Type type1 = fieldType.GetGenericArguments()[0];
				Type type2 = fieldType.GetGenericArguments()[1];
				LOG.Info(String.Format("Found Dictionary<{0},{1}>",type1.Name, type2.Name));
				object dictionary = Activator.CreateInstance(fieldType);
				MethodInfo addMethodInfo = fieldType.GetMethod("Add");
				Dictionary<string, string> properties = iniProperties[sectionName];
				foreach(string key in properties.Keys) {
					if (key != null && key.StartsWith(propertyName + ".")) {
						// What "key" do we need to store it under?
						string subPropertyName = key.Substring(propertyName.Length + 1);
						string stringValue = properties[key];
						object newValue1 = ConvertValueToFieldType(type1, subPropertyName);
						object newValue2 = ConvertValueToFieldType(type2, stringValue);
						addMethodInfo.Invoke(dictionary, new object[] {newValue1, newValue2});
				    }
				}
				return dictionary;
			} else {
				if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition().Equals(typeof(Nullable<>))) {
					// We are dealing with a generic type that is nullable
					fieldType = Nullable.GetUnderlyingType(fieldType);
				}
				return ConvertValueToFieldType(fieldType, propertyValue);
			}
			return null;
		}

		private static object ConvertValueToFieldType(Type fieldType, string valueString) {
			if (valueString == null || valueString.Length == 0) {
				return null;
			}
			if (fieldType == typeof(string)) {
				return valueString;
			} else if (fieldType == typeof(bool) || fieldType == typeof(bool?)) {
				return bool.Parse(valueString);
			} else if (fieldType == typeof(int) || fieldType == typeof(int?)) {
				return int.Parse(valueString);
			} else if (fieldType == typeof(uint) || fieldType == typeof(uint?)) {
				return uint.Parse(valueString);
			} else if (fieldType == typeof(Point)) {
				string[] pointValues = valueString.Split(new Char[] {','});
				int x = int.Parse(pointValues[0].Trim());
				int y = int.Parse(pointValues[1].Trim());
				return new Point(x, y);
			} else if (fieldType == typeof(Size)) {
				string[] sizeValues = valueString.Split(new Char[] {','});
				int width = int.Parse(sizeValues[0].Trim());
				int height = int.Parse(sizeValues[1].Trim());
				return new Size(width, height);
			} else if (fieldType == typeof(Rectangle)) {
				string[] rectValues = valueString.Split(new Char[] {','});
				int x = int.Parse(rectValues[0].Trim());
				int y = int.Parse(rectValues[1].Trim());
				int width = int.Parse(rectValues[2].Trim());
				int height = int.Parse(rectValues[3].Trim());
				return new Rectangle(x, y, width, height);
			} else if (fieldType.IsEnum) {
				try {
					return Enum.Parse(fieldType, valueString);
				} catch (Exception e) {
					LOG.Error("Can't parse value: " + valueString, e);
				}
			}
			return null;
		}

		private static string ConvertValueToString(object valueObject) {
			if (valueObject == null) {
				return "";
			}
			Type fieldType = valueObject.GetType();
			if (fieldType == typeof(Point)) {
				Point p = (Point)valueObject;
				return String.Format("{0},{1}", p.X, p.Y);
			} else if (fieldType == typeof(Size)) {
				Size size = (Size)valueObject;
				return String.Format("{0},{1}", size.Width, size.Height);
			} else if (fieldType == typeof(Rectangle)) {
				Rectangle rectangle = (Rectangle)valueObject;
				return String.Format("{0},{1},{2},{3}", rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
			}
			return valueObject.ToString();
		}

		private static string getSectionName(Type iniSectionType) {
			Attribute[] classAttributes = Attribute.GetCustomAttributes(iniSectionType);
			foreach(Attribute attribute in classAttributes) {
				if (attribute is IniSectionAttribute) {
					IniSectionAttribute iniSectionAttribute = (IniSectionAttribute)attribute;
					return iniSectionAttribute.Name;
				}
			}
			return null;
		}

		private static Dictionary<string, string> getProperties(string section) {
			if (iniProperties.ContainsKey(section)) {
				return iniProperties[section];
			}
			return null;
		}

		public static void ChangeProperty(string section, string name, string value) {
			if (section != null) {
				Dictionary<string, string> properties = iniProperties[section];
				properties[name] = value;
				LOG.Debug(String.Format("Changed property {0} to section: {0}.", name, section));
			} else {
				LOG.Debug("Property without section: " + name);
			}
		}

		public static void SetProperty(string section, string name, string value) {
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


		public static bool HasProperty(string section, string name) {
			Dictionary<string, string> properties = getProperties(section);
			if (properties != null && properties.ContainsKey(name)) {
				return true;
			}
			return false;
		}
		
		public static string GetProperty(string section, string name) {
			Dictionary<string, string> properties = getProperties(section);
			if (properties != null && properties.ContainsKey(name)) {
				return properties[name];
			}
			return null;
		}

		/// <summary>
		/// Split property with ',' and return the splitted string as a string[]
		/// </summary>
		public static string[] GetPropertyAsArray(string section, string key) {
			Dictionary<string, string> properties = getProperties(section);
			string value = GetProperty(section, key);
			if (value != null) {
				return value.Split(new Char[] {','});
			}
			return null;
		}

		public static bool GetBoolProperty(string section, string key) {
			Dictionary<string, string> properties = getProperties(section);
			string value = GetProperty(section, key);
			return bool.Parse(value);
		}

		public static int GetIntProperty(string section, string key) {
			Dictionary<string, string> properties = getProperties(section);
			string value = GetProperty(section, key);
			return int.Parse(value);
		}

		public static void Save() {
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
									   	writer.Write("{0},", ConvertValueToString(item));
									   } else {
										   writer.Write("{0}", ConvertValueToString(item));
									   }
									}
									writer.WriteLine();
								} else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
									// Handle dictionaries.

									// Get the methods we need to deal with dictionaries.
									var keys = fieldType.GetProperty("Keys").GetValue(value, null);
									var item = fieldType.GetProperty("Item");
									var enumerator = keys.GetType().GetMethod("GetEnumerator").Invoke(keys, null);
									var moveNext = enumerator.GetType().GetMethod("MoveNext");
									var current = enumerator.GetType().GetProperty("Current").GetGetMethod();
									// Get all the values.
									while ((bool)moveNext.Invoke(enumerator, null)) {
										var key = current.Invoke(enumerator, null);
										var valueObject = item.GetValue(value, new object[] { key });
										// Write to ini file!
										writer.WriteLine("{0}.{1}={2}", iniPropertyAttribute.Name, ConvertValueToString(key), ConvertValueToString(valueObject));
									}
								} else {
									writer.WriteLine("{0}={1}", iniPropertyAttribute.Name,  ConvertValueToString(value));
								}
							}
						}
					}
				}
				writer.WriteLine();
				section.IsDirty = false;
			}
			writer.WriteLine();
			// Write left over properties
			foreach(string sectionName in iniProperties.Keys) {
				// Check if the section is one that is "registered", if so skip it!
				if (!sectionMap.ContainsKey(sectionName)) {
					writer.WriteLine("; The section {0} is not registered, maybe a plugin hasn't claimed it due to errors or some functionality isn't used yet.", sectionName);
					// Write section name
					writer.WriteLine("[{0}]", sectionName);
					Dictionary<string, string> properties = iniProperties[sectionName];
					// Loop and write properties
					foreach(string propertyName in properties.Keys) {
						writer.WriteLine("{0}={1}", propertyName, properties[propertyName]);
					}
					writer.WriteLine();
				}
			}
			writer.Close();
		}
	}
}
