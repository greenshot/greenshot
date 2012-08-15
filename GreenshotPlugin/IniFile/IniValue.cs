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
using System.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace Greenshot.IniFile {
	/// <summary>
	/// A container to be able to pass the value from a IniSection around.
	/// </summary>
	public class IniValue {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(IniValue));
		private PropertyInfo propertyInfo;
		private FieldInfo fieldInfo;
		private IniSection containingIniSection;
		private IniPropertyAttribute attributes;

		public IniValue(IniSection containingIniSection, PropertyInfo propertyInfo, IniPropertyAttribute iniPropertyAttribute) {
			this.containingIniSection = containingIniSection;
			this.propertyInfo = propertyInfo;
			this.attributes = iniPropertyAttribute;
		}

		public IniValue(IniSection containingIniSection, FieldInfo fieldInfo, IniPropertyAttribute iniPropertyAttribute) {
			this.containingIniSection = containingIniSection;
			this.fieldInfo = fieldInfo;
			this.attributes = iniPropertyAttribute;
		}
		
		public MemberInfo MemberInfo {
			get {
				if (propertyInfo == null) {
					return fieldInfo;
				} else {
					return propertyInfo;					
				}
			}
		}

		/// <summary>
		/// Returns the IniSection this value is contained in
		/// </summary>
		public IniSection ContainingIniSection {
			get {
				return containingIniSection;
			}
		}
		
		/// <summary>
		/// Get the in the ini file defined attributes
		/// </summary>
		public IniPropertyAttribute Attributes {
			get {
				return attributes;
			}
		}
		
		/// <summary>
		/// Get the value for this IniValue
		/// </summary>
		public object Value {
			get {
				if (propertyInfo == null) {
					return fieldInfo.GetValue(containingIniSection);
				} else {
					return propertyInfo.GetValue(containingIniSection, null);					
				}
			}
			set {
				if (propertyInfo == null) {
					fieldInfo.SetValue(containingIniSection, value);
				} else {
					propertyInfo.SetValue(containingIniSection, value, null);
				}
			}
		}
		
		/// <summary>
		/// Get the Type of the value
		/// </summary>
		public Type ValueType {
			get {
				Type valueType = null;
				if (propertyInfo == null) {
					valueType = fieldInfo.FieldType;
				} else {
					valueType = propertyInfo.PropertyType;					
				}
				if (valueType.IsGenericType && valueType.GetGenericTypeDefinition().Equals(typeof(Nullable<>))) {
					// We are dealing with a generic type that is nullable
					valueType = Nullable.GetUnderlyingType(valueType);
				}
				return valueType;
			}
		}

		/// <summary>
		/// Write the value to the text writer
		/// </summary>
		/// <param name="writer">TextWriter to write to</param>
		/// <param name="onlyProperties">true if we do not want the comment</param>
		public void Write(TextWriter writer, bool onlyProperties) {
			object myValue = Value;
			Type valueType = ValueType;
			if (myValue == null) {
				if (attributes.ExcludeIfNull) {
					return;
				}
				if (attributes.DefaultValue != null) {
					myValue = attributes.DefaultValue;
					valueType = typeof(string);
				} else {
					myValue = containingIniSection.GetDefault(attributes.Name);
					if (myValue != null) {
						valueType = myValue.GetType();
					}
				}
			}
			if (myValue == null) {
				if (attributes.ExcludeIfNull) {
					return;
				}
			}
			if (!onlyProperties) {
				writer.WriteLine("; {0}", attributes.Description);
			}
			if (myValue == null) {
				writer.WriteLine("{0}=", attributes.Name);
				return;
			}
			if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(List<>)) {
				Type specificValueType = valueType.GetGenericArguments()[0];
				writer.Write("{0}=", attributes.Name);
				int listCount = (int)valueType.GetProperty("Count").GetValue(myValue, null);
				// Loop though generic list
				for (int index = 0; index < listCount; index++) {
					object item = valueType.GetMethod("get_Item").Invoke(myValue, new object[] { index });

					// Now you have an instance of the item in the generic list
					if (index < listCount - 1) {
						writer.Write("{0}" + attributes.Separator, ConvertValueToString(specificValueType, item));
					} else {
						writer.Write("{0}", ConvertValueToString(specificValueType, item));
					}
				}
				writer.WriteLine();
			} else if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
				// Handle dictionaries.
				Type valueType1 = valueType.GetGenericArguments()[0];
				Type valueType2 = valueType.GetGenericArguments()[1];
				// Get the methods we need to deal with dictionaries.
				var keys = valueType.GetProperty("Keys").GetValue(myValue, null);
				var item = valueType.GetProperty("Item");
				var enumerator = keys.GetType().GetMethod("GetEnumerator").Invoke(keys, null);
				var moveNext = enumerator.GetType().GetMethod("MoveNext");
				var current = enumerator.GetType().GetProperty("Current").GetGetMethod();
				// Get all the values.
				while ((bool)moveNext.Invoke(enumerator, null)) {
					var key = current.Invoke(enumerator, null);
					var valueObject = item.GetValue(myValue, new object[] { key });
					// Write to ini file!
					writer.WriteLine("{0}.{1}={2}", attributes.Name, ConvertValueToString(valueType1, key), ConvertValueToString(valueType2, valueObject));
				}
			} else {
				writer.WriteLine("{0}={1}", attributes.Name, ConvertValueToString(valueType, myValue));
			}

		}

		/// <summary>
		/// Set the value to the value in the ini file, or default
		/// </summary>
		/// <returns></returns>
		public void SetValueFromProperties(Dictionary<string, string> properties) {
			string propertyName = attributes.Name;
			string defaultValue = attributes.DefaultValue;

			// Get the value from the ini file, if there is none take the default
			if (!properties.ContainsKey(propertyName) && defaultValue != null) {
				// Mark as dirty, we didn't use properties from the file (even defaults from the default file are allowed)
				containingIniSection.IsDirty = true;
				//LOG.Debug("Passing default: " + propertyName + "=" + propertyDefaultValue);
			}

			string propertyValue = null;
			if (properties.ContainsKey(propertyName) && properties[propertyName] != null) {
				propertyValue = containingIniSection.PreCheckValue(propertyName, properties[propertyName]);
			}
			UseValueOrDefault(propertyValue);
		}

		/// <summary>
		/// This method will set the ini value to the supplied value or use the default if non supplied
		/// </summary>
		/// <param name="propertyValue"></param>
		public void UseValueOrDefault(string propertyValue) {
			Type valueType = ValueType;
			string propertyName = attributes.Name;
			string defaultValue = attributes.DefaultValue;
			bool defaultUsed = false;
			object defaultValueFromConfig = containingIniSection.GetDefault(propertyName);

			if (string.IsNullOrEmpty(propertyValue)) {
				if (defaultValue != null && defaultValue.Trim().Length != 0) {
					propertyValue = defaultValue;
					defaultUsed = true;
				} else if (defaultValueFromConfig != null) {
					LOG.DebugFormat("Default for Property {0} implemented!", propertyName);
				} else {
					if (attributes.ExcludeIfNull) {
						Value = null;
						return;
					}
					LOG.DebugFormat("Property {0} has no value or default value!", propertyName);
				}
			}
			// Now set the value
			if (valueType.IsGenericType && ValueType.GetGenericTypeDefinition() == typeof(List<>)) {
				string arraySeparator = attributes.Separator;
				object list = Activator.CreateInstance(ValueType);
				// Logic for List<>
				if (propertyValue == null) {
					if (defaultValueFromConfig != null) {
						Value = defaultValueFromConfig;
						return;
					}
					Value = list;
					return;
				}
				string[] arrayValues = propertyValue.Split(new string[] { arraySeparator }, StringSplitOptions.None);
				if (arrayValues == null || arrayValues.Length == 0) {
					Value = list;
					return;
				}
				bool addedElements = false;
				bool parseProblems = false;
				MethodInfo addMethodInfo = valueType.GetMethod("Add");

				foreach (string arrayValue in arrayValues) {
					if (arrayValue != null && arrayValue.Length > 0) {
						object newValue = null;
						try {
							newValue = ConvertStringToValueType(valueType.GetGenericArguments()[0], arrayValue);
						} catch (Exception ex) {
							LOG.Warn(ex);
							//LOG.Error("Problem converting " + arrayValue + " to type " + fieldType.FullName, e);
							parseProblems = true;
						}
						if (newValue != null) {
							addMethodInfo.Invoke(list, new object[] { newValue });
							addedElements = true;
						}
					}
				}
				// Try to fallback on a default
				if (!addedElements && parseProblems) {
					try {
						object fallbackValue = ConvertStringToValueType(valueType.GetGenericArguments()[0], defaultValue);
						addMethodInfo.Invoke(list, new object[] { fallbackValue });
						Value = list;
						return;
					} catch (Exception ex) {
						LOG.Warn(ex);
						//LOG.Error("Problem converting " + defaultValue + " to type " + fieldType.FullName, e);
					}
				}
				Value = list;
				return;
			} else if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
				// Logic for Dictionary<,>
				Type type1 = valueType.GetGenericArguments()[0];
				Type type2 = valueType.GetGenericArguments()[1];
				//LOG.Info(String.Format("Found Dictionary<{0},{1}>", type1.Name, type2.Name));
				object dictionary = Activator.CreateInstance(valueType);
				MethodInfo addMethodInfo = valueType.GetMethod("Add");
				bool addedElements = false;
				Dictionary<string, string> properties = IniConfig.PropertiesForSection(containingIniSection);
				foreach (string key in properties.Keys) {
					if (key != null && key.StartsWith(propertyName + ".")) {
						// What "key" do we need to store it under?
						string subPropertyName = key.Substring(propertyName.Length + 1);
						string stringValue = properties[key];
						object newValue1 = null;
						object newValue2 = null;
						try {
							newValue1 = ConvertStringToValueType(type1, subPropertyName);
						} catch (Exception ex) {
							LOG.Warn(ex);
							//LOG.Error("Problem converting " + subPropertyName + " to type " + type1.FullName, e);
						}
						try {
							newValue2 = ConvertStringToValueType(type2, stringValue);
						} catch (Exception ex) {
							LOG.Warn(ex);
							//LOG.Error("Problem converting " + stringValue + " to type " + type2.FullName, e);
						}
						addMethodInfo.Invoke(dictionary, new object[] { newValue1, newValue2 });
						addedElements = true;
					}
				}
				// No need to return something that isn't filled!
				if (addedElements) {
					Value = dictionary;
					return;
				} else if (defaultValueFromConfig != null) {
					Value = defaultValueFromConfig;
					return;
				}
			} else if (propertyValue != null) {
				if (valueType.IsGenericType && valueType.GetGenericTypeDefinition().Equals(typeof(Nullable<>))) {
					// We are dealing with a generic type that is nullable
					valueType = Nullable.GetUnderlyingType(valueType);
				}
				object newValue = null;
				try {
					newValue = ConvertStringToValueType(valueType, propertyValue);
				} catch (Exception ex1) {
					newValue = null;
					if (!defaultUsed) {
						try {
							newValue = ConvertStringToValueType(valueType, defaultValue);
						} catch (Exception ex2) {
							LOG.Warn("Problem converting " + propertyValue + " to type " + valueType.FullName, ex2);
						}
					} else {
						LOG.Warn("Problem converting " + propertyValue + " to type " + valueType.FullName, ex1);
					}
				}
				Value = newValue;
				return;
			}

			// If nothing is set, we can use the default value from the config (if we habe one)
			if (defaultValueFromConfig != null) {
				Value = defaultValueFromConfig;
				return;
			} 
		}

		/// <summary>
		/// Helper method for conversion
		/// </summary>
		/// <param name="valueType"></param>
		/// <param name="valueString"></param>
		/// <returns></returns>
		private static object ConvertStringToValueType(Type valueType, string valueString) {
			if (valueString == null) {
				return null;
			}
			if (valueType == typeof(string)) {
				return valueString;
			}
			TypeConverter converter = TypeDescriptor.GetConverter(valueType);
			//LOG.Debug("No convertor for " + fieldType.ToString());
			if (valueType == typeof(object) && valueString.Length > 0) {
				//LOG.Debug("Parsing: " + valueString);
				string[] values = valueString.Split(new Char[] { ':' });
				//LOG.Debug("Type: " + values[0]);
				//LOG.Debug("Value: " + values[1]);
				Type fieldTypeForValue = Type.GetType(values[0], true);
				//LOG.Debug("Type after GetType: " + fieldTypeForValue);
				return ConvertStringToValueType(fieldTypeForValue, values[1]);
			} else if (converter != null) {
				return converter.ConvertFromInvariantString(valueString);
			} else if (valueType.IsEnum) {
				if (valueString.Length > 0) {
					try {
						return Enum.Parse(valueType, valueString);
					} catch (ArgumentException ae) {
						//LOG.InfoFormat("Couldn't match {0} to {1}, trying case-insentive match", valueString, fieldType);
						foreach (Enum enumValue in Enum.GetValues(valueType)) {
							if (enumValue.ToString().Equals(valueString, StringComparison.InvariantCultureIgnoreCase)) {
								//LOG.Info("Match found...");
								return enumValue;
							}
						}
						throw ae;
					}
				}
			}
			return null;
		}

		public override string ToString() {
			return ConvertValueToString(ValueType, Value);
		}

		private static string ConvertValueToString(Type valueType, object valueObject) {
			if (valueObject == null) {
				// If there is nothing, deliver nothing!
				return "";
			}
			if (valueType == typeof(object)) {
				// object to String, this is the hardest
				// Format will be "FQTypename[,Assemblyname]:Value"

				// Get the type so we can call ourselves recursive
				Type objectType = valueObject.GetType();

				// Get the value as string
				string ourValue = ConvertValueToString(objectType, valueObject);

				// Get the valuetype as string
				string valueTypeName = objectType.FullName;
				// Find the assembly name and only append it if it's not already in the fqtypename (like System.Drawing)
				string assemblyName = objectType.Assembly.FullName;
				// correct assemblyName, this also has version information etc.
				if (assemblyName.StartsWith("Green")) {
					assemblyName = assemblyName.Substring(0, assemblyName.IndexOf(','));
				}
				return String.Format("{0},{1}:{2}", valueTypeName, assemblyName, ourValue);
			} else {
				TypeConverter converter = TypeDescriptor.GetConverter(valueType);
				if (converter != null) {
					return converter.ConvertToInvariantString(valueObject);
				}
			}
			// All other types
			return valueObject.ToString();
		}
	}
}