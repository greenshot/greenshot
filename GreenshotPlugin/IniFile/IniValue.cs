#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using log4net;

#endregion

namespace GreenshotPlugin.IniFile
{
	/// <summary>
	///     A container to be able to pass the value from a IniSection around.
	/// </summary>
	public class IniValue
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(IniValue));
		private readonly FieldInfo _fieldInfo;
		private readonly PropertyInfo _propertyInfo;

		public IniValue(IniSection containingIniSection, PropertyInfo propertyInfo, IniPropertyAttribute iniPropertyAttribute)
		{
			ContainingIniSection = containingIniSection;
			_propertyInfo = propertyInfo;
			Attributes = iniPropertyAttribute;
		}

		public IniValue(IniSection containingIniSection, FieldInfo fieldInfo, IniPropertyAttribute iniPropertyAttribute)
		{
			ContainingIniSection = containingIniSection;
			_fieldInfo = fieldInfo;
			Attributes = iniPropertyAttribute;
		}

		/// <summary>
		///     Return true when the value is fixed
		/// </summary>
		public bool IsFixed
		{
			get
			{
				if (Attributes != null)
				{
					return Attributes.FixedValue;
				}
				return false;
			}
			set
			{
				if (Attributes != null)
				{
					Attributes.FixedValue = value;
				}
			}
		}

		/// <summary>
		///     Return true when the value is for experts
		/// </summary>
		public bool IsExpert
		{
			get
			{
				if (Attributes != null)
				{
					return Attributes.Expert;
				}
				return false;
			}
			set
			{
				if (Attributes != null)
				{
					Attributes.Expert = value;
				}
			}
		}


		/// <summary>
		///     Return true when the value is can be changed by the GUI
		/// </summary>
		public bool IsEditable => !IsFixed;

		/// <summary>
		///     Return true when the value is visible in the GUI
		/// </summary>
		public bool IsVisible => !IsExpert;

		public MemberInfo MemberInfo
		{
			get
			{
				if (_propertyInfo == null)
				{
					return _fieldInfo;
				}
				return _propertyInfo;
			}
		}

		/// <summary>
		///     Returns the IniSection this value is contained in
		/// </summary>
		public IniSection ContainingIniSection { get; }

		/// <summary>
		///     Get the in the ini file defined attributes
		/// </summary>
		public IniPropertyAttribute Attributes { get; }

		/// <summary>
		///     Get the value for this IniValue
		/// </summary>
		public object Value
		{
			get
			{
				if (_propertyInfo == null)
				{
					return _fieldInfo.GetValue(ContainingIniSection);
				}
				return _propertyInfo.GetValue(ContainingIniSection, null);
			}
			set
			{
				if (_propertyInfo == null)
				{
					_fieldInfo.SetValue(ContainingIniSection, value);
				}
				else
				{
					_propertyInfo.SetValue(ContainingIniSection, value, null);
				}
			}
		}

		/// <summary>
		///     Get the Type of the value
		/// </summary>
		public Type ValueType
		{
			get
			{
				var valueType = _propertyInfo?.PropertyType ?? _fieldInfo.FieldType;
				if (!valueType.IsGenericType)
				{
					return valueType;
				}
				var genericTypeDefinition = valueType.GetGenericTypeDefinition();
				if (genericTypeDefinition != null && genericTypeDefinition == typeof(Nullable<>))
				{
					// We are dealing with a generic type that is nullable
					valueType = Nullable.GetUnderlyingType(valueType);
				}
				return valueType;
			}
		}

		/// <summary>
		///     Write the value to the text writer
		/// </summary>
		/// <param name="writer">TextWriter to write to</param>
		/// <param name="onlyProperties">true if we do not want the comment</param>
		public void Write(TextWriter writer, bool onlyProperties)
		{
			var myValue = Value;
			var valueType = ValueType;
			if (myValue == null)
			{
				if (Attributes.ExcludeIfNull)
				{
					return;
				}
				if (Attributes.DefaultValue != null)
				{
					myValue = Attributes.DefaultValue;
					valueType = typeof(string);
				}
				else
				{
					myValue = ContainingIniSection.GetDefault(Attributes.Name);
					if (myValue != null)
					{
						valueType = myValue.GetType();
					}
				}
			}
			if (myValue == null)
			{
				if (Attributes.ExcludeIfNull)
				{
					return;
				}
			}
			if (!onlyProperties)
			{
				writer.WriteLine("; {0}", Attributes.Description);
			}
			if (myValue == null)
			{
				writer.WriteLine("{0}=", Attributes.Name);
				return;
			}
			if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
			{
				// Handle dictionaries.
				var valueType1 = valueType.GetGenericArguments()[0];
				var valueType2 = valueType.GetGenericArguments()[1];
				// Get the methods we need to deal with dictionaries.
				var keys = valueType.GetProperty("Keys").GetValue(myValue, null);
				var item = valueType.GetProperty("Item");
				var enumerator = keys.GetType().GetMethod("GetEnumerator").Invoke(keys, null);
				var moveNext = enumerator.GetType().GetMethod("MoveNext");
				var current = enumerator.GetType().GetProperty("Current").GetGetMethod();
				// Get all the values.
				while ((bool) moveNext.Invoke(enumerator, null))
				{
					var key = current.Invoke(enumerator, null);
					var valueObject = item.GetValue(myValue, new[] {key});
					// Write to ini file!
					writer.WriteLine("{0}.{1}={2}", Attributes.Name, ConvertValueToString(valueType1, key, Attributes.Separator),
						ConvertValueToString(valueType2, valueObject, Attributes.Separator));
				}
			}
			else
			{
				writer.WriteLine("{0}={1}", Attributes.Name, ConvertValueToString(valueType, myValue, Attributes.Separator));
			}
		}

		/// <summary>
		///     Set the value to the value in the ini file, or default
		/// </summary>
		/// <returns></returns>
		public void SetValueFromProperties(IDictionary<string, string> properties)
		{
			var propertyName = Attributes.Name;
			string propertyValue = null;
			if (properties.ContainsKey(propertyName) && properties[propertyName] != null)
			{
				propertyValue = ContainingIniSection.PreCheckValue(propertyName, properties[propertyName]);
			}
			UseValueOrDefault(propertyValue);
		}

		/// <summary>
		///     This method will set the ini value to the supplied value or use the default if non supplied
		/// </summary>
		/// <param name="propertyValue"></param>
		public void UseValueOrDefault(string propertyValue)
		{
			var valueType = ValueType;
			var propertyName = Attributes.Name;
			var defaultValue = Attributes.DefaultValue;
			var defaultUsed = false;
			var defaultValueFromConfig = ContainingIniSection.GetDefault(propertyName);

			if (string.IsNullOrEmpty(propertyValue))
			{
				if (defaultValue != null && defaultValue.Trim().Length != 0)
				{
					propertyValue = defaultValue;
					defaultUsed = true;
				}
				else if (defaultValueFromConfig != null)
				{
					Log.DebugFormat("Default for Property {0} implemented!", propertyName);
				}
				else
				{
					if (Attributes.ExcludeIfNull)
					{
						Value = null;
						return;
					}
					Log.DebugFormat("Property {0} has no value or default value!", propertyName);
				}
			}
			// Now set the value
			if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
			{
				// Logic for Dictionary<,>
				var type1 = valueType.GetGenericArguments()[0];
				var type2 = valueType.GetGenericArguments()[1];
				//LOG.Info(String.Format("Found Dictionary<{0},{1}>", type1.Name, type2.Name));
				var dictionary = Activator.CreateInstance(valueType);
				var addMethodInfo = valueType.GetMethod("Add");
				var addedElements = false;
				var properties = IniConfig.PropertiesForSection(ContainingIniSection);
				foreach (var key in properties.Keys)
				{
					if (key != null && key.StartsWith(propertyName + "."))
					{
						// What "key" do we need to store it under?
						var subPropertyName = key.Substring(propertyName.Length + 1);
						var stringValue = properties[key];
						object newValue1 = null;
						object newValue2 = null;
						try
						{
							newValue1 = ConvertStringToValueType(type1, subPropertyName, Attributes.Separator);
						}
						catch (Exception ex)
						{
							Log.Warn(ex);
							//LOG.Error("Problem converting " + subPropertyName + " to type " + type1.FullName, e);
						}
						try
						{
							newValue2 = ConvertStringToValueType(type2, stringValue, Attributes.Separator);
						}
						catch (Exception ex)
						{
							Log.Warn(ex);
							//LOG.Error("Problem converting " + stringValue + " to type " + type2.FullName, e);
						}
						addMethodInfo.Invoke(dictionary, new[] {newValue1, newValue2});
						addedElements = true;
					}
				}
				// No need to return something that isn't filled!
				if (addedElements)
				{
					Value = dictionary;
					return;
				}
				if (defaultValueFromConfig != null)
				{
					Value = defaultValueFromConfig;
					return;
				}
			}
			else if (!string.IsNullOrEmpty(propertyValue))
			{
				if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(Nullable<>))
				{
					// We are dealing with a generic type that is nullable
					valueType = Nullable.GetUnderlyingType(valueType);
				}
				object newValue;
				try
				{
					newValue = ConvertStringToValueType(valueType, propertyValue, Attributes.Separator);
				}
				catch (Exception ex1)
				{
					newValue = null;
					if (!defaultUsed)
					{
						try
						{
							Log.WarnFormat("Problem '{0}' while converting {1} to type {2} trying fallback...", ex1.Message, propertyValue, valueType.FullName);
							newValue = ConvertStringToValueType(valueType, defaultValue, Attributes.Separator);
							ContainingIniSection.IsDirty = true;
							Log.InfoFormat("Used default value {0} for property {1}", defaultValue, propertyName);
						}
						catch (Exception ex2)
						{
							Log.Warn("Problem converting fallback value " + defaultValue + " to type " + valueType.FullName, ex2);
						}
					}
					else
					{
						Log.Warn("Problem converting " + propertyValue + " to type " + valueType.FullName, ex1);
					}
				}
				Value = newValue;
				return;
			}

			// If nothing is set, we can use the default value from the config (if we habe one)
			if (defaultValueFromConfig != null)
			{
				Value = defaultValueFromConfig;
				return;
			}
			if (ValueType != typeof(string))
			{
				try
				{
					Value = Activator.CreateInstance(ValueType);
				}
				catch (Exception)
				{
					Log.WarnFormat("Couldn't create instance of {0} for {1}, using default value.", ValueType.FullName, Attributes.Name);
					Value = default(ValueType);
				}
			}
			else
			{
				Value = default(ValueType);
			}
		}

		/// <summary>
		///     Convert a string to a value of type "valueType"
		/// </summary>
		/// <param name="valueType">Type to convert tp</param>
		/// <param name="valueString">string to convert from</param>
		/// <param name="separator"></param>
		/// <returns>Value</returns>
		private static object ConvertStringToValueType(Type valueType, string valueString, string separator)
		{
			if (valueString == null)
			{
				return null;
			}
			if (valueType == typeof(string))
			{
				return valueString;
			}
			if (string.IsNullOrEmpty(valueString))
			{
				return null;
			}

			// The following makes the enum string values a bit less restrictive
			if (valueType.IsEnum)
			{
				var searchingEnumString = valueString.Replace("_", "").ToLowerInvariant();
				foreach (var possibleValue in Enum.GetValues(valueType))
				{
					var possibleString = possibleValue.ToString().Replace("_", "").ToLowerInvariant();
					if (possibleString.Equals(searchingEnumString))
					{
						return possibleValue;
					}
				}
			}

			if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(List<>))
			{
				var arraySeparator = separator;
				var list = Activator.CreateInstance(valueType);
				// Logic for List<>
				var arrayValues = valueString.Split(new[] {arraySeparator}, StringSplitOptions.None);
				if (arrayValues.Length == 0)
				{
					return list;
				}
				var addMethodInfo = valueType.GetMethod("Add");

				foreach (var arrayValue in arrayValues)
				{
					if (!string.IsNullOrEmpty(arrayValue))
					{
						object newValue = null;
						try
						{
							newValue = ConvertStringToValueType(valueType.GetGenericArguments()[0], arrayValue, separator);
						}
						catch (Exception ex)
						{
							Log.Warn("Problem converting " + arrayValue + " to type " + valueType.FullName, ex);
						}
						if (newValue != null)
						{
							addMethodInfo.Invoke(list, new[] {newValue});
						}
					}
				}
				return list;
			}
			//LOG.Debug("No convertor for " + fieldType.ToString());
			if (valueType == typeof(object) && valueString.Length > 0)
			{
				//LOG.Debug("Parsing: " + valueString);
				var values = valueString.Split(':');
				//LOG.Debug("Type: " + values[0]);
				//LOG.Debug("Value: " + values[1]);
				var fieldTypeForValue = Type.GetType(values[0], true);
				//LOG.Debug("Type after GetType: " + fieldTypeForValue);
				return ConvertStringToValueType(fieldTypeForValue, values[1], separator);
			}
			var converter = TypeDescriptor.GetConverter(valueType);
			return converter.ConvertFromInvariantString(valueString);
		}

		/// <summary>
		///     Override of ToString which calls the ConvertValueToString
		/// </summary>
		/// <returns>string representation of this</returns>
		public override string ToString()
		{
			return ConvertValueToString(ValueType, Value, Attributes.Separator);
		}

		/// <summary>
		///     Convert the supplied value to a string
		/// </summary>
		/// <param name="valueType">Type to convert</param>
		/// <param name="valueObject">Value to convert</param>
		/// <param name="separator">separator for lists</param>
		/// <returns>string representation of the value</returns>
		private static string ConvertValueToString(Type valueType, object valueObject, string separator)
		{
			if (valueObject == null)
			{
				// If there is nothing, deliver nothing!
				return "";
			}

			if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(List<>))
			{
				var stringBuilder = new StringBuilder();
				var specificValueType = valueType.GetGenericArguments()[0];
				var listCount = (int) valueType.GetProperty("Count").GetValue(valueObject, null);
				// Loop though generic list
				for (var index = 0; index < listCount; index++)
				{
					var item = valueType.GetMethod("get_Item").Invoke(valueObject, new object[] {index});

					// Now you have an instance of the item in the generic list
					if (index < listCount - 1)
					{
						stringBuilder.AppendFormat("{0}{1}", ConvertValueToString(specificValueType, item, separator), separator);
					}
					else
					{
						stringBuilder.AppendFormat("{0}", ConvertValueToString(specificValueType, item, separator));
					}
				}
				return stringBuilder.ToString();
			}
			if (valueType == typeof(object))
			{
				// object to String, this is the hardest
				// Format will be "FQTypename[,Assemblyname]:Value"

				// Get the type so we can call ourselves recursive
				var objectType = valueObject.GetType();

				// Get the value as string
				var ourValue = ConvertValueToString(objectType, valueObject, separator);

				// Get the valuetype as string
				var valueTypeName = objectType.FullName;
				// Find the assembly name and only append it if it's not already in the fqtypename (like System.Drawing)
				var assemblyName = objectType.Assembly.FullName;
				// correct assemblyName, this also has version information etc.
				if (assemblyName.StartsWith("Green"))
				{
					assemblyName = assemblyName.Substring(0, assemblyName.IndexOf(','));
				}
				return $"{valueTypeName},{assemblyName}:{ourValue}";
			}
			var converter = TypeDescriptor.GetConverter(valueType);
			return converter.ConvertToInvariantString(valueObject);
		}
	}
}