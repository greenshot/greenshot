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
using System.ComponentModel;
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Editor.Drawing.Fields
{
    /// <summary>
    /// Represents a single field of a drawable element, i.e. 
    /// line thickness of a rectangle.
    /// </summary>
    [Serializable]
    public class Field : IField
    {
        [field: NonSerialized] public event PropertyChangedEventHandler PropertyChanged;

        private object _myValue;

        public object Value
        {
            get { return _myValue; }
            set
            {
                if (!Equals(_myValue, value))
                {
                    _myValue = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
                }
            }
        }

        public IFieldType FieldType { get; set; }
        public string Scope { get; set; }

        /// <summary>
        /// Constructs a new Field instance, usually you should be using FieldFactory
        /// to create Fields.
        /// </summary>
        /// <param name="fieldType">FieldType of the Field to be created</param>
        /// <param name="scope">The scope to which the value of this Field is relevant.
        /// Depending on the scope the Field's value may be shared for other elements
        /// containing the same FieldType for defaulting to the last used value.
        /// When scope is set to a Type (e.g. typeof(RectangleContainer)), its value
        /// should not be reused for FieldHolders of another Type (e.g. typeof(EllipseContainer))
        /// </param>
        public Field(IFieldType fieldType, Type scope)
        {
            FieldType = fieldType;
            Scope = scope.Name;
        }

        public Field(IFieldType fieldType, string scope)
        {
            FieldType = fieldType;
            Scope = scope;
        }

        public Field(IFieldType fieldType)
        {
            FieldType = fieldType;
        }

        /// <summary>
        /// Returns true if this field holds a value other than null.
        /// </summary>
        public bool HasValue => Value != null;

        /// <summary>
        /// Creates a flat clone of this Field. The fields value itself is not cloned.
        /// </summary>
        /// <returns></returns>
        public Field Clone()
        {
            return new Field(FieldType, Scope)
            {
                Value = Value
            };
        }

        public override int GetHashCode()
        {
            int hashCode = 0;
            unchecked
            {
                hashCode += 1000000009 * FieldType.GetHashCode();
                if (Scope != null)
                    hashCode += 1000000021 * Scope.GetHashCode();
            }

            return hashCode;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Field other))
            {
                return false;
            }

            return FieldType == other.FieldType && Equals(Scope, other.Scope);
        }

        public override string ToString()
        {
            return string.Format("[Field FieldType={1} Value={0} Scope={2}]", _myValue, FieldType, Scope);
        }
    }
}