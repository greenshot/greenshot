// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.Structs;
using Greenshot.Addon.LegacyEditor.Drawing;
using Greenshot.Addon.LegacyEditor.Drawing.Fields;
using Greenshot.Addons.Interfaces.Drawing;

namespace Greenshot.Addon.LegacyEditor
{
    /// <summary>
    /// Extension methods for the IEditorConfiguration
    /// </summary>
    public static class EditorConfigurationExtensions
    {
        /// <summary>
        /// This is a factory method for IField which considers the defaults from the configuration
        /// </summary>
        /// <param name="editorConfiguration"></param>
        /// <param name="requestingType">Type of the class for which to create the field</param>
        /// <param name="fieldType">FieldType of the field to construct</param>
        /// <param name="preferredDefaultValue"></param>
        /// <returns>a new Field of the given fieldType, with the scope of it's value being restricted to the Type scope</returns>
        public static IField CreateField(this IEditorConfiguration editorConfiguration, Type requestingType, IFieldType fieldType, object preferredDefaultValue)
        {
            var requestingTypeName = requestingType.Name;
            var requestedField = requestingTypeName + "." + fieldType.Name;
            var fieldValue = preferredDefaultValue;

            // Check if the configuration exists
            if (editorConfiguration.LastUsedFieldValues == null)
            {
                editorConfiguration.LastUsedFieldValues = new Dictionary<string, object>();
            }

            // Check if settings for the requesting type exist, if not create!
            if (editorConfiguration.LastUsedFieldValues.ContainsKey(requestedField))
            {
                // Check if a value is set (not null)!
                if (editorConfiguration.LastUsedFieldValues[requestedField] != null)
                {
                    var preferredValue = editorConfiguration.LastUsedFieldValues[requestedField];
                    if (preferredValue is string preferredStringValue)
                    {
                        switch (fieldType.ValueType)
                        {
                            case var intType when fieldType.ValueType == typeof(int):
                                fieldValue = Convert.ToInt32(preferredValue);
                                break;
                            case var boolType when fieldType.ValueType == typeof(bool):
                                fieldValue = Convert.ToBoolean(preferredValue);
                                break;
                            case var colorType when fieldType.ValueType == typeof(Color):
                                var color = Color.FromName(preferredStringValue);
                                fieldValue = color;
                                if (Color.Empty == color)
                                {
                                    fieldValue = Color.FromArgb(Convert.ToInt32(preferredValue));
                                }
                                break;
                            case var alignType when fieldType.ValueType == typeof(StringAlignment):
                                fieldValue = Enum.Parse(typeof(StringAlignment), preferredStringValue, true);
                                break;
                            case var fieldFlagType when fieldType.ValueType == typeof(FieldFlag):
                                fieldValue = Enum.Parse(typeof(FieldFlag), preferredStringValue, true);
                                break;
                            case var preparedFilterType when fieldType.ValueType == typeof(PreparedFilter):
                                fieldValue = Enum.Parse(typeof(PreparedFilter), preferredStringValue, true);
                                break;
                            case var arrowHeadCombinationType when fieldType.ValueType == typeof(ArrowContainer.ArrowHeadCombination):
                                fieldValue = Enum.Parse(typeof(ArrowContainer.ArrowHeadCombination), preferredStringValue, true);
                                break;
                            case var floatType when fieldType.ValueType == typeof(float):
                                fieldValue = Convert.ToSingle(preferredValue, CultureInfo.InvariantCulture);
                                break;
                            case var doubleType when fieldType.ValueType == typeof(double):
                                fieldValue = Convert.ToDouble(preferredValue, CultureInfo.InvariantCulture);
                                break;
                            default:
                                fieldValue = preferredStringValue;
                                break;
                        }
                    }
                    else
                    {
                        fieldValue = preferredValue;
                    }
                }
                else
                {
                    // Overwrite null value
                    editorConfiguration.LastUsedFieldValues[requestedField] = fieldValue;
                }
            }
            else
            {
                editorConfiguration.LastUsedFieldValues.Add(requestedField, fieldValue);
            }
            return new Field(fieldType, requestingType)
            {
                Value = fieldValue
            };
        }

        /// <summary>
        /// Update the last field value in the configuration
        /// </summary>
        /// <param name="editorConfiguration">IEditorConfiguration</param>
        /// <param name="field">IField</param>
        public static void UpdateLastFieldValue(this IEditorConfiguration editorConfiguration, IField field)
        {
            var requestedField = field.Scope + "." + field.FieldType.Name;
            // Check if the configuration exists
            if (editorConfiguration.LastUsedFieldValues == null)
            {
                editorConfiguration.LastUsedFieldValues = new Dictionary<string, object>();
            }
            // check if settings for the requesting type exist, if not create!
            if (field.Value is Color color)
            {
                editorConfiguration.LastUsedFieldValues[requestedField] = color.ToArgb().ToString();
            }
            else
            {
                editorConfiguration.LastUsedFieldValues[requestedField] = field.Value.ToString();
            }
        }

        /// <summary>
        /// Reset the WindowPlacement for the editor
        /// </summary>
        /// <param name="editorConfiguration">IEditorConfiguration</param>
        public static void ResetEditorPlacement(this IEditorConfiguration editorConfiguration)
        {
            editorConfiguration.WindowNormalPosition = new NativeRect(100, 100, 400, 400);
            editorConfiguration.WindowMaxPosition = new NativePoint(-1, -1);
            editorConfiguration.WindowMinPosition = new NativePoint(-1, -1);
            editorConfiguration.WindowPlacementFlags = 0;
            editorConfiguration.ShowWindowCommand = ShowWindowCommands.Normal;
        }

        /// <summary>
        /// Retrieve the WindowPlacement from the configuration
        /// </summary>
        /// <param name="editorConfiguration">IEditorConfiguration</param>
        /// <returns>WindowPlacement</returns>
        public static WindowPlacement GetEditorPlacement(this IEditorConfiguration editorConfiguration)
        {
            var placement = WindowPlacement.Create();
            placement.NormalPosition = editorConfiguration.WindowNormalPosition;
            placement.MaxPosition = editorConfiguration.WindowMaxPosition;
            placement.MinPosition = editorConfiguration.WindowMinPosition;
            placement.ShowCmd = editorConfiguration.ShowWindowCommand;
            placement.Flags = editorConfiguration.WindowPlacementFlags;
            return placement;
        }

        /// <summary>
        /// Store the WindowPlacement for the editor
        /// </summary>
        /// <param name="editorConfiguration">IEditorConfiguration</param>
        /// <param name="placement">WindowPlacement</param>
        public static void SetEditorPlacement(this IEditorConfiguration editorConfiguration, WindowPlacement placement)
        {
            editorConfiguration.WindowNormalPosition = placement.NormalPosition;
            editorConfiguration.WindowMaxPosition = placement.MaxPosition;
            editorConfiguration.WindowMinPosition = placement.MinPosition;
            editorConfiguration.ShowWindowCommand = placement.ShowCmd;
            editorConfiguration.WindowPlacementFlags = placement.Flags;
        }
    }
}
