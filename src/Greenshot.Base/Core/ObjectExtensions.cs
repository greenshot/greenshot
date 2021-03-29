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
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Extension methods which work for objects
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Perform a deep Copy of the object.
        /// </summary>
        /// <typeparam name="T">The type of object being copied.</typeparam>
        /// <param name="source">The object instance to copy.</param>
        /// <returns>The copied object.</returns>
        public static T Clone<T>(this T source)
        {
            var typeparam = typeof(T);
            if (!typeparam.IsInterface && !typeparam.IsSerializable)
            {
                throw new ArgumentException("The type must be serializable.", nameof(source));
            }

            // Don't serialize a null object, simply return the default for that object
            if (source == null)
            {
                return default;
            }

            IFormatter formatter = new BinaryFormatter();
            using var stream = new MemoryStream();
            formatter.Serialize(stream, source);
            stream.Seek(0, SeekOrigin.Begin);
            return (T) formatter.Deserialize(stream);
        }

        /// <summary>
        /// Clone the content from source to destination
        /// </summary>
        /// <typeparam name="T">Type to clone</typeparam>
        /// <param name="source">Instance to copy from</param>
        /// <param name="destination">Instance to copy to</param>
        public static void CloneTo<T>(this T source, T destination)
        {
            var type = typeof(T);
            var myObjectFields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            foreach (var fieldInfo in myObjectFields)
            {
                fieldInfo.SetValue(destination, fieldInfo.GetValue(source));
            }

            var myObjectProperties = type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            foreach (var propertyInfo in myObjectProperties)
            {
                if (propertyInfo.CanWrite)
                {
                    propertyInfo.SetValue(destination, propertyInfo.GetValue(source, null), null);
                }
            }
        }

        /// <summary>
        /// Compare two lists
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="l1">IList</param>
        /// <param name="l2">IList</param>
        /// <returns>true if they are the same</returns>
        public static bool CompareLists<T>(IList<T> l1, IList<T> l2)
        {
            if (l1.Count != l2.Count)
            {
                return false;
            }

            int matched = 0;
            foreach (T item in l1)
            {
                if (!l2.Contains(item))
                {
                    return false;
                }

                matched++;
            }

            return matched == l1.Count;
        }
    }
}