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
using System.ComponentModel;
using System.Drawing;
using System.Runtime.Serialization;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Configuration;
using log4net;

namespace Greenshot.Editor.Drawing.Fields
{
    /// <summary>
    /// Basic IFieldHolder implementation, providing access to a set of fields
    /// </summary>
    [Serializable]
    public abstract class AbstractFieldHolder : IFieldHolder
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(AbstractFieldHolder));
        private static readonly EditorConfiguration EditorConfig = IniConfig.GetIniSection<EditorConfiguration>();
        [NonSerialized] private readonly IDictionary<IField, PropertyChangedEventHandler> _handlers = new Dictionary<IField, PropertyChangedEventHandler>();

        /// <summary>
        /// called when a field's value has changed
        /// </summary>
        [NonSerialized] private FieldChangedEventHandler _fieldChanged;

        public event FieldChangedEventHandler FieldChanged
        {
            add { _fieldChanged += value; }
            remove { _fieldChanged -= value; }
        }

        // we keep two Collections of our fields, dictionary for quick access, list for serialization
        // this allows us to use default serialization
        [NonSerialized] private IDictionary<IFieldType, IField> _fieldsByType = new Dictionary<IFieldType, IField>();
        private readonly IList<IField> fields = new List<IField>();

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            _fieldsByType = new Dictionary<IFieldType, IField>();
            // listen to changing properties
            foreach (var field in fields)
            {
                field.PropertyChanged += delegate { _fieldChanged?.Invoke(this, new FieldChangedEventArgs(field)); };
                _fieldsByType[field.FieldType] = field;
            }
        }

        public void AddField(Type requestingType, IFieldType fieldType, object fieldValue)
        {
            AddField(EditorConfig.CreateField(requestingType, fieldType, fieldValue));
        }

        public virtual void AddField(IField field)
        {
            fields.Add(field);
            if (_fieldsByType == null)
            {
                return;
            }

            if (_fieldsByType.ContainsKey(field.FieldType))
            {
                if (LOG.IsDebugEnabled)
                {
                    LOG.DebugFormat("A field with of type '{0}' already exists in this {1}, will overwrite.", field.FieldType, GetType());
                }
            }

            _fieldsByType[field.FieldType] = field;

            _handlers[field] = (sender, args) => { _fieldChanged?.Invoke(this, new FieldChangedEventArgs(field)); };
            field.PropertyChanged += _handlers[field];
        }

        public void RemoveField(IField field)
        {
            fields.Remove(field);
            _fieldsByType.Remove(field.FieldType);
            field.PropertyChanged -= _handlers[field];
            _handlers.Remove(field);
        }

        public IList<IField> GetFields()
        {
            return fields;
        }


        public IField GetField(IFieldType fieldType)
        {
            try
            {
                return _fieldsByType[fieldType];
            }
            catch (KeyNotFoundException e)
            {
                throw new ArgumentException("Field '" + fieldType + "' does not exist in " + GetType(), e);
            }
        }

        public object GetFieldValue(IFieldType fieldType)
        {
            return GetField(fieldType)?.Value;
        }

        public string GetFieldValueAsString(IFieldType fieldType)
        {
            return Convert.ToString(GetFieldValue(fieldType));
        }

        public int GetFieldValueAsInt(IFieldType fieldType)
        {
            return Convert.ToInt32(GetFieldValue(fieldType));
        }

        public decimal GetFieldValueAsDecimal(IFieldType fieldType)
        {
            return Convert.ToDecimal(GetFieldValue(fieldType));
        }

        public double GetFieldValueAsDouble(IFieldType fieldType)
        {
            return Convert.ToDouble(GetFieldValue(fieldType));
        }

        public float GetFieldValueAsFloat(IFieldType fieldType)
        {
            return Convert.ToSingle(GetFieldValue(fieldType));
        }

        public bool GetFieldValueAsBool(IFieldType fieldType)
        {
            return Convert.ToBoolean(GetFieldValue(fieldType));
        }

        public Color GetFieldValueAsColor(IFieldType fieldType, Color defaultColor = default)
        {
            return (Color) (GetFieldValue(fieldType) ?? defaultColor);
        }

        public bool HasField(IFieldType fieldType)
        {
            return _fieldsByType.ContainsKey(fieldType);
        }

        public bool HasFieldValue(IFieldType fieldType)
        {
            return HasField(fieldType) && _fieldsByType[fieldType].HasValue;
        }

        public void SetFieldValue(IFieldType fieldType, object value)
        {
            try
            {
                _fieldsByType[fieldType].Value = value;
            }
            catch (KeyNotFoundException e)
            {
                throw new ArgumentException("Field '" + fieldType + "' does not exist in " + GetType(), e);
            }
        }

        protected void OnFieldChanged(object sender, FieldChangedEventArgs e)
        {
            _fieldChanged?.Invoke(sender, e);
        }
    }
}