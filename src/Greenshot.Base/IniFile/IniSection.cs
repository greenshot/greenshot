﻿/*
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
using Greenshot.Base.Core;
using log4net;
using System.Linq;

namespace Greenshot.Base.IniFile
{
    /// <summary>
    /// Base class for all IniSections
    /// </summary>
    [Serializable]
    public abstract class IniSection
    {
        protected static ILog LOG = LogManager.GetLogger(typeof(IniSection));

        [NonSerialized] private readonly IDictionary<string, IniValue> values = new Dictionary<string, IniValue>();
        [NonSerialized] private IniSectionAttribute iniSectionAttribute;

        public IniSectionAttribute IniSectionAttribute => iniSectionAttribute ??= GetIniSectionAttribute(GetType());

        /// <summary>
        /// Get the dictionary with all the IniValues
        /// </summary>
        public IDictionary<string, IniValue> Values => values;

        /// <summary>
        /// Flag to specify if values have been changed
        /// </summary>
        public bool IsDirty { get; set; }

        /// <summary>
        /// Supply values we can't put as defaults
        /// </summary>
        /// <param name="property">The property to return a default for</param>
        /// <returns>object with the default value for the supplied property</returns>
        public virtual object GetDefault(string property) => null;

        /// <summary>
        /// This method will be called before converting the property, making to possible to correct a certain value
        /// Can be used when migration is needed
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="propertyValue">The string value of the property</param>
        /// <returns>string with the propertyValue, modified or not...</returns>
        public virtual string PreCheckValue(string propertyName, string propertyValue) => propertyValue;

        /// <summary>
        /// This method will be called after reading the configuration, so eventually some corrections can be made
        /// </summary>
        public virtual void AfterLoad()
        {
        }

        /// <summary>
        /// This will be called before saving the Section, so we can encrypt passwords etc...
        /// </summary>
        public virtual void BeforeSave()
        {
        }

        /// <summary>
        /// This will be called before saving the Section, so we can decrypt passwords etc...
        /// </summary>
        public virtual void AfterSave()
        {
        }

        /// <summary>
        /// Helper method to get the IniSectionAttribute of a type
        /// </summary>
        /// <param name="iniSectionType"></param>
        /// <returns></returns>
        public static IniSectionAttribute GetIniSectionAttribute(Type iniSectionType)
        {
            Attribute[] classAttributes = Attribute.GetCustomAttributes(iniSectionType);
            foreach (Attribute attribute in classAttributes)
            {
                if (attribute is IniSectionAttribute iniSectionAttribute2)
                {
                    return iniSectionAttribute2;
                }
            }

            return null;
        }

        /// <summary>
        /// Fill the section with the supplied properties
        /// </summary>
        /// <param name="properties"></param>
        public void Fill(IDictionary<string, string> properties)
        {
            Type iniSectionType = GetType();
            // Iterate over the members and create IniValueContainers
            foreach (var (fieldInfo, iniPropertyAttribute) in
            from FieldInfo fieldInfo in iniSectionType.GetFields()
            where Attribute.IsDefined(fieldInfo, typeof(IniPropertyAttribute))
            let iniPropertyAttribute = (IniPropertyAttribute)fieldInfo.GetCustomAttributes(typeof(IniPropertyAttribute), false)[0]
            where !Values.ContainsKey(iniPropertyAttribute.Name)
            select (fieldInfo, iniPropertyAttribute))
            {
                Values[iniPropertyAttribute.Name] = new IniValue(this, fieldInfo, iniPropertyAttribute);
            }

            foreach (PropertyInfo propertyInfo in iniSectionType.GetProperties())
            {
                if (Attribute.IsDefined(propertyInfo, typeof(IniPropertyAttribute)) && !Values.ContainsKey(propertyInfo.Name))
                {
                    IniPropertyAttribute iniPropertyAttribute = (IniPropertyAttribute)propertyInfo.GetCustomAttributes(typeof(IniPropertyAttribute), false)[0];
                    Values[iniPropertyAttribute.Name] = new IniValue(this, propertyInfo, iniPropertyAttribute);
                }
            }

            foreach (string fieldName in Values.Keys)
            {
                IniValue iniValue = Values[fieldName];
                try
                {
                    iniValue.SetValueFromProperties(properties);
                    if (iniValue.Attributes.Encrypted && iniValue.Value is string stringValue && stringValue.Length > 2)
                    {
                        iniValue.Value = stringValue.Decrypt();
                    }
                }
                catch (Exception ex)
                {
                    LOG.Error(ex);
                }
            }

            AfterLoad();
        }

        /// <summary>
        /// Write the section to the writer
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="onlyProperties"></param>
        public void Write(TextWriter writer, bool onlyProperties)
        {
            if (IniSectionAttribute == null)
            {
                throw new ArgumentException("Section didn't implement the IniSectionAttribute");
            }

            BeforeSave();
            try
            {
                if (!onlyProperties)
                {
                    writer.WriteLine("; {0}", IniSectionAttribute.Description);
                }

                writer.WriteLine("[{0}]", IniSectionAttribute.Name);

                foreach (IniValue value in Values.Values)
                {
                    if (value.Attributes.Encrypted && value.Value is string stringValue && stringValue.Length > 2)
                    {
                        value.Value = stringValue.Encrypt();
                    }

                    // Write the value
                    value.Write(writer, onlyProperties);
                    if (value.Attributes.Encrypted && value.Value is string stringValue2 && stringValue2.Length > 2)
                    {
                        value.Value = stringValue2.Decrypt();
                    }
                }
            }
            finally
            {
                AfterSave();
            }
        }
    }
}