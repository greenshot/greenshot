/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Threading;
using System.Windows.Forms;

namespace GreenshotPlugin.Core {
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
		public string Separator = ",";
		public string DefaultValue;
		public string Name {
			get {return name;}
			set {name = value;}
		}
	}
	public static class IniReader{
        private const string SECTION_START = "[";
        private const string SECTION_END = "]";
        private const string COMMENT = ";";
        private static char[] ASSIGNMENT = new char[] { '=' };

        /**
         * Read an ini file to a Dictionary, each key is a section and the value is a Dictionary with name and values.
         */
        public static Dictionary<string, Dictionary<string, string>> read(string path, Encoding encoding) {
            Dictionary<string, Dictionary<string, string>> ini = new Dictionary<string, Dictionary<string,string>>();
            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1024)) {
	            using (StreamReader reader = new StreamReader(fileStream, encoding)) {
	                Dictionary<string, string> nameValues = new Dictionary<string, string>();
	                while (!reader.EndOfStream) {
	                    string line = reader.ReadLine();
	                    if (line != null) {
	                        string cleanLine = line.Trim();
	                        if (cleanLine.Length == 0 || cleanLine.StartsWith(COMMENT)) {
	                            continue;
	                        }
	                        if (cleanLine.StartsWith(SECTION_START)) {
	                            string section = line.Replace(SECTION_START, "").Replace(SECTION_END, "").Trim();
	                            nameValues = new Dictionary<string, string>();
	                            ini.Add(section, nameValues);
	                        } else {
	                            string[] keyvalueSplitter = line.Split(ASSIGNMENT, 2);
	                            string name = keyvalueSplitter[0];
	                            string inivalue = keyvalueSplitter.Length > 1 ? keyvalueSplitter[1] : null;
	                            if (nameValues.ContainsKey(name)) {
	                            	nameValues[name] = inivalue;
	                            } else {
		                            nameValues.Add(name, inivalue);
	                            }
	                        }
	                    }
	                }
	            }
            }
            return ini;
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

		/// <summary>
		/// This method will be called before converting the property, making to possible to correct a certain value
		/// Can be used when migration is needed
		/// </summary>
		/// <param name="propertyName">The name of the property</param>
		/// <param name="propertyValue">The string value of the property</param>
		/// <returns>string with the propertyValue, modified or not...</returns>
		public virtual string PreCheckValue(string propertyName, string propertyValue) {
			return propertyValue;
		}		

		/// <summary>
		/// This method will be called after reading the configuration, so eventually some corrections can be made
		/// </summary>
		public virtual void PostCheckValues() {
		}
	}
	
	public class IniConfig {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(IniConfig));
		private const string CONFIG_FILE_NAME = "greenshot.ini";
		private const string DEFAULTS_CONFIG_FILE_NAME = "greenshot-defaults.ini";
		private const string FIXED_CONFIG_FILE_NAME = "greenshot-fixed.ini";
		private const string SUBDIRECTORY_NAME = "Greenshot";
		private static FileSystemWatcher watcher;

		/// <summary>
		/// Static code for loading
		/// </summary>
		static IniConfig() {
			iniLocation = CreateIniLocation(CONFIG_FILE_NAME);
			// Load the defaults
			Read(CreateIniLocation(DEFAULTS_CONFIG_FILE_NAME));
			// Load the normal
			Read(CreateIniLocation(iniLocation));
			// Load the fixed settings
			Read(CreateIniLocation(FIXED_CONFIG_FILE_NAME));
			WatchConfigFile(true);
		}
		
		private static string iniLocation = null;
		private static Dictionary<string, IniSection> sectionMap = new Dictionary<string, IniSection>();
		private static Dictionary<string, Dictionary<string, string >> iniProperties = new Dictionary<string, Dictionary<string, string>>();
		public static event FileSystemEventHandler IniChanged;

		private static void WatchConfigFile(bool sendEvents) {
 			// Wait with watching untill the file is there
 			if (Directory.Exists(Path.GetDirectoryName(iniLocation))) {
 				if (watcher == null) {
 					LOG.DebugFormat("Starting FileSystemWatcher for {0}", iniLocation);
					// Monitor the ini file
					watcher = new FileSystemWatcher();
					watcher.Path = Path.GetDirectoryName(iniLocation);
					watcher.Filter = "*.ini";
					watcher.NotifyFilter = NotifyFilters.LastWrite;
		 			watcher.Changed += new FileSystemEventHandler(ConfigFileChanged);
 				}
			}
 			if (watcher != null) {
 				watcher.EnableRaisingEvents = sendEvents;
 			}
		}

		private static void ConfigFileChanged(object source, FileSystemEventArgs e) {
			if (iniLocation.Equals(e.FullPath)) {
				LOG.InfoFormat("Config file {0} was changed, reloading", e.FullPath);

				// Try to reread the configuration
				int retries = 10;
				bool configRead = false;
				while(!configRead && retries != 0) {
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
			// check if file is in the same location as started from, if this is the case
			// we will use this file instead of the Applicationdate folder
			// Done for Feature Request #2741508
			if (File.Exists(Path.Combine(Application.StartupPath, configFilename))) {
				return Path.Combine(Application.StartupPath, configFilename);
			}
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), SUBDIRECTORY_NAME + @"\" + configFilename);
		}
		
		/// <summary>
		/// Reload the Ini file
		/// </summary>
		public static void Reload() {
			// Load the normal
			Read(iniLocation);
			// Load the fixed settings
			Read(CreateIniLocation(FIXED_CONFIG_FILE_NAME));
			foreach(IniSection section in sectionMap.Values) {
				FillIniSection(section);
			}
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
			iniProperties = IniReader.read(iniLocation, Encoding.UTF8);
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
				LOG.Debug("Returning pre-mapped section " + sectionName);
				section = (T)sectionMap[sectionName];
			} else {
				// Create instance of this type
				section = (T)Activator.CreateInstance(iniSectionType);

				// Store for later save & retrieval
				sectionMap.Add(sectionName, section);
				FillIniSection(section);
				LOG.Debug("Returning newly mapped section " + sectionName);
			}
			if (section.IsDirty) {
				IniConfig.Save();
			}
			return section;
		}
		
		private static void FillIniSection(IniSection section) {
			Type iniSectionType = section.GetType();
			string sectionName = getSectionName(iniSectionType);
			// Get the properties for the section
			Dictionary<string, string> properties = null;
			if (iniProperties.ContainsKey(sectionName)) {
				properties = iniProperties[sectionName];
			} else {
				iniProperties.Add(sectionName, new Dictionary<string, string>());
				properties = iniProperties[sectionName];
			}

			// Iterate over the fields and fill them
			FieldInfo[] fields = iniSectionType.GetFields();
			foreach(FieldInfo field in fields) {
				if (Attribute.IsDefined(field, typeof(IniPropertyAttribute))) {
					IniPropertyAttribute iniPropertyAttribute = (IniPropertyAttribute)field.GetCustomAttributes(typeof(IniPropertyAttribute), false)[0];
					string propertyName = iniPropertyAttribute.Name;
					string propertyDefaultValue = iniPropertyAttribute.DefaultValue;
					string fieldSeparator = iniPropertyAttribute.Separator;
					// Get the type, or the underlying type for nullables
					Type fieldType = field.FieldType;

					// Get the value from the ini file, if there is none take the default
					if (!properties.ContainsKey(propertyName) && propertyDefaultValue != null) {
						// Mark as dirty, we didn't use properties from the file (even defaults from the default file are allowed)
						section.IsDirty = true;
						LOG.Debug("Passing default: " + propertyName + "=" + propertyDefaultValue);
					}
					
					// Try to get the field value from the properties or use the default value
					object fieldValue = null;
					try {
						fieldValue = CreateFieldValue(fieldType, section, sectionName, propertyName, propertyDefaultValue, fieldSeparator);
					} catch (Exception e) {
						LOG.Warn("Couldn't parse field: " + sectionName + "." + propertyName, e);
					}

					// If still no value, check if the GetDefault delivers a value
					if (fieldValue == null) {
						// Use GetDefault to fill the field if none is set
						fieldValue = section.GetDefault(propertyName);
					}

					// Set the value
					try {
						field.SetValue(section,fieldValue);
					} catch (Exception e) {
						LOG.Warn("Couldn't set field: " + sectionName + "." + propertyName, e);
					}
				}
			}
		}
		
		/// <summary>
		/// Helper method for creating a value
		/// </summary>
		/// <param name="fieldType">Type of the value to create</param>
		/// <param name="propertyValue">Value as string</param>
		/// <returns>object instance of the value</returns>
		private static object CreateFieldValue(Type fieldType, IniSection section, string sectionName, string propertyName, string defaultValue, string arraySeparator) {
			Dictionary<string, string> properties = iniProperties[sectionName];
			bool defaultUsed = false;
			string propertyValue = null;
			if (properties.ContainsKey(propertyName) && properties[propertyName] != null) {
				propertyValue = section.PreCheckValue(propertyName, properties[propertyName]);
			} else if (defaultValue != null && defaultValue.Trim().Length != 0) {
				propertyValue = defaultValue;
				defaultUsed = true;
			} else {
				LOG.DebugFormat("Property {0} has no value or default value", propertyName);
			}

			// Now set the value
			if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>)) {
				object list = Activator.CreateInstance(fieldType);
				// Logic for List<>
				if (propertyValue == null) {
					return list;
				}
				string[] arrayValues = propertyValue.Split(new string[] {arraySeparator}, StringSplitOptions.None);
				if (arrayValues == null || arrayValues.Length == 0) {
					return list;
				}
				bool addedElements = false;
				bool parseProblems = false;
				MethodInfo addMethodInfo = fieldType.GetMethod("Add");
				
				foreach(string arrayValue in arrayValues) {
					if (arrayValue != null && arrayValue.Length > 0) {
						object newValue = null;
						try {
							newValue = ConvertValueToFieldType(fieldType.GetGenericArguments()[0], arrayValue);
						} catch (Exception e) {
							LOG.Error("Problem converting " + arrayValue + " to type " + fieldType.FullName, e);
							parseProblems = true;
						}
						if (newValue != null) {
							addMethodInfo.Invoke(list, new object[] {newValue});
							addedElements = true;
						}
					}
				}
				// Try to fallback on a default
				if (!addedElements && parseProblems) {
					try {
						object fallbackValue = ConvertValueToFieldType(fieldType.GetGenericArguments()[0], defaultValue);
						addMethodInfo.Invoke(list, new object[] {fallbackValue});
						return list;
					} catch (Exception e) {
						LOG.Error("Problem converting " + defaultValue + " to type " + fieldType.FullName, e);
					}
				}
				
				return list;
			} else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
				// Logic for Dictionary<,>
				Type type1 = fieldType.GetGenericArguments()[0];
				Type type2 = fieldType.GetGenericArguments()[1];
				LOG.Info(String.Format("Found Dictionary<{0},{1}>",type1.Name, type2.Name));
				object dictionary = Activator.CreateInstance(fieldType);
				MethodInfo addMethodInfo = fieldType.GetMethod("Add");
				bool addedElements = false;
				foreach(string key in properties.Keys) {
					if (key != null && key.StartsWith(propertyName + ".")) {
						// What "key" do we need to store it under?
						string subPropertyName = key.Substring(propertyName.Length + 1);
						string stringValue = properties[key];
						object newValue1 = null;
						object newValue2 = null;
						try {
							newValue1 = ConvertValueToFieldType(type1, subPropertyName);
						} catch (Exception e) {
							LOG.Error("Problem converting " + subPropertyName + " to type " + type1.FullName, e);
						}
						try {
							newValue2 = ConvertValueToFieldType(type2, stringValue);
						} catch (Exception e) {
							LOG.Error("Problem converting " + stringValue + " to type " + type2.FullName, e);
						}
						addMethodInfo.Invoke(dictionary, new object[] {newValue1, newValue2});
						addedElements = true;
				    }
				}
				// No need to return something that isn't filled!
				if (addedElements) {
					return dictionary;
				}
			} else {
				if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition().Equals(typeof(Nullable<>))) {
					// We are dealing with a generic type that is nullable
					fieldType = Nullable.GetUnderlyingType(fieldType);
				}
				object newValue = null;
				try {
					newValue = ConvertValueToFieldType(fieldType, propertyValue);
				} catch (Exception e1) {
					newValue = null;
					if (!defaultUsed) {
						try {
							newValue = ConvertValueToFieldType(fieldType, defaultValue);
						} catch (Exception e2) {
							LOG.Error("Problem converting " + propertyValue + " to type " + fieldType.FullName, e2);
						}
					} else {
						LOG.Error("Problem converting " + propertyValue + " to type " + fieldType.FullName, e1);
					}
				}
				return newValue;
			}
			return null;
		}

		private static object ConvertValueToFieldType(Type fieldType, string valueString) {
			if (valueString == null) {
				return null;
			}
			if (fieldType == typeof(string)) {
				return valueString;
			} else if (fieldType == typeof(bool) || fieldType == typeof(bool?)) {
				if (valueString.Length > 0) {
					return bool.Parse(valueString);
				}
			} else if (fieldType == typeof(int) || fieldType == typeof(int?)) {
				if (valueString.Length > 0) {
					return int.Parse(valueString);
				}
			} else if (fieldType == typeof(uint) || fieldType == typeof(uint?)) {
				if (valueString.Length > 0) {
					return uint.Parse(valueString);
				}
				return 0;
			} else if (fieldType == typeof(Point)) {
				if (valueString.Length > 0) {
					string[] pointValues = valueString.Split(new Char[] {','});
					int x = int.Parse(pointValues[0].Trim());
					int y = int.Parse(pointValues[1].Trim());
					return new Point(x, y);
				}
			} else if (fieldType == typeof(DateTime)) {
				if (valueString.Length > 0) {
					return DateTime.Parse(valueString);
				}
			} else if (fieldType == typeof(Size)) {
				if (valueString.Length > 0) {
					string[] sizeValues = valueString.Split(new Char[] {','});
					int width = int.Parse(sizeValues[0].Trim());
					int height = int.Parse(sizeValues[1].Trim());
					return new Size(width, height);
				}
			} else if (fieldType == typeof(Rectangle)) {
				if (valueString.Length > 0) {
					string[] rectValues = valueString.Split(new Char[] {','});
					int x = int.Parse(rectValues[0].Trim());
					int y = int.Parse(rectValues[1].Trim());
					int width = int.Parse(rectValues[2].Trim());
					int height = int.Parse(rectValues[3].Trim());
					return new Rectangle(x, y, width, height);
				}
			} else if (fieldType == typeof(Color)) {
				if (valueString.Length > 0) {
					string[] colorValues = valueString.Split(new Char[] {','});
					int alpha = int.Parse(colorValues[0].Trim());
					int red = int.Parse(colorValues[1].Trim());
					int green = int.Parse(colorValues[2].Trim());
					int blue = int.Parse(colorValues[3].Trim());
					return Color.FromArgb(alpha, red, green, blue);
				}
			} else if (fieldType == typeof(object)) {
				if (valueString.Length > 0) {
					//LOG.Debug("Parsing: " + valueString);
					string[] values = valueString.Split(new Char[] {':'});
					//LOG.Debug("Type: " + values[0]);
					//LOG.Debug("Value: " + values[1]);
					Type fieldTypeForValue = Type.GetType(values[0], true);
					//LOG.Debug("Type after GetType: " + fieldTypeForValue);
					return ConvertValueToFieldType(fieldTypeForValue, values[1]);
				}
			} else if (fieldType.IsEnum) {
				if (valueString.Length > 0) {
					try {
						return Enum.Parse(fieldType, valueString);
					} catch (ArgumentException ae) {
						LOG.InfoFormat("Couldn't match {0} to {1}, trying case-insentive match", valueString, fieldType);
						foreach(Enum enumValue in Enum.GetValues(fieldType)) {
							if (enumValue.ToString().Equals(valueString, StringComparison.InvariantCultureIgnoreCase)) {
								LOG.Info("Match found...");
								return enumValue;
							}
						}
						throw ae;
					}
				}
			}
			return null;
		}

		private static string ConvertValueToString(Type fieldType, object valueObject) {
			if (valueObject == null) {
				// If there is nothing, deliver nothing!
				return "";
			}
			if (fieldType == typeof(Point)) {
				// Point to String
				Point p = (Point)valueObject;
				return String.Format("{0},{1}", p.X, p.Y);
			} else if (fieldType == typeof(Size)) {
				// Size to String
				Size size = (Size)valueObject;
				return String.Format("{0},{1}", size.Width, size.Height);
			} else if (fieldType == typeof(Rectangle)) {
				// Rectangle to String
				Rectangle rectangle = (Rectangle)valueObject;
				return String.Format("{0},{1},{2},{3}", rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
			} else if (fieldType == typeof(Color)) {
				// Color to String
				Color color = (Color)valueObject;
				return String.Format("{0},{1},{2},{3}", color.A, color.R, color.G, color.B);
			} else if (fieldType == typeof(object)) {
				// object to String, this is the hardest
				// Format will be "FQTypename[,Assemblyname]:Value"

				// Get the type so we can call ourselves recursive
				Type valueType = valueObject.GetType();
				
				// Get the value as string
				string ourValue = ConvertValueToString(valueType, valueObject);

				// Get the valuetype as string
				string valueTypeName = valueType.FullName;
				// Find the assembly name and only append it if it's not already in the fqtypename (like System.Drawing)
				string assemblyName = valueType.Assembly.FullName;
				// correct assemblyName, this also has version information etc.
				if (assemblyName.StartsWith("Green")) {
					assemblyName = assemblyName.Substring(0,assemblyName.IndexOf(','));
				}
				return String.Format("{0},{1}:{2}", valueTypeName, assemblyName, ourValue);
			}
			// All other types
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
			try {
				SaveInternally();
			} catch (Exception e) {
				LOG.Error("A problem occured while writing the configuration file to: " + iniLocation, e);
			}
		}

		private static void SaveInternally() {
			WatchConfigFile(false);

			LOG.Info("Saving configuration to: " + iniLocation);
			if (!Directory.Exists(Path.GetDirectoryName(iniLocation))) {
				Directory.CreateDirectory(Path.GetDirectoryName(iniLocation));
			}
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
									Type valueType = fieldType.GetGenericArguments()[0];
									writer.Write("{0}=", iniPropertyAttribute.Name);
									int listCount = (int)fieldType.GetProperty("Count").GetValue(value, null);
									// Loop though generic list
									for (int index = 0; index < listCount; index++) {
									   object item = fieldType.GetMethod("get_Item").Invoke(value, new object[] { index });
									   
									   // Now you have an instance of the item in the generic list
									   if (index < listCount -1) {
									   	writer.Write("{0}" + iniPropertyAttribute.Separator, ConvertValueToString(valueType, item));
									   } else {
										   writer.Write("{0}", ConvertValueToString(valueType, item));
									   }
									}
									writer.WriteLine();
								} else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
									// Handle dictionaries.
									Type valueType1 = fieldType.GetGenericArguments()[0];
									Type valueType2 = fieldType.GetGenericArguments()[1];
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
										writer.WriteLine("{0}.{1}={2}", iniPropertyAttribute.Name, ConvertValueToString(valueType1, key), ConvertValueToString(valueType2, valueObject));
									}
								} else {
									writer.WriteLine("{0}={1}", iniPropertyAttribute.Name,  ConvertValueToString(fieldType, value));
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
			WatchConfigFile(true);
		}
	}
}
