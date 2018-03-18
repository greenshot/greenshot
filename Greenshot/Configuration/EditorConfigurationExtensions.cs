#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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

#endregion

using System;
using System.Collections.Generic;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.Structs;
using Greenshot.Addons.Interfaces.Drawing;
using Greenshot.Drawing.Fields;

namespace Greenshot.Configuration
{
    public static class EditorConfigurationExtensions
    {
        /// <param name="configuration"></param>
        /// <param name="requestingType">Type of the class for which to create the field</param>
        /// <param name="fieldType">FieldType of the field to construct</param>
        /// <param name="preferredDefaultValue"></param>
        /// <returns>a new Field of the given fieldType, with the scope of it's value being restricted to the Type scope</returns>
        public static IField CreateField(this IEditorConfiguration configuration, Type requestingType, IFieldType fieldType, object preferredDefaultValue)
        {
            var requestingTypeName = requestingType.Name;
            var requestedField = requestingTypeName + "." + fieldType.Name;
            var fieldValue = preferredDefaultValue;

            // Check if the configuration exists
            if (configuration.LastUsedFieldValues == null)
            {
                configuration.LastUsedFieldValues = new Dictionary<string, object>();
            }

            // Check if settings for the requesting type exist, if not create!
            if (configuration.LastUsedFieldValues.ContainsKey(requestedField))
            {
                // Check if a value is set (not null)!
                if (configuration.LastUsedFieldValues[requestedField] != null)
                {
                    fieldValue = configuration.LastUsedFieldValues[requestedField];
                }
                else
                {
                    // Overwrite null value
                    configuration.LastUsedFieldValues[requestedField] = fieldValue;
                }
            }
            else
            {
                configuration.LastUsedFieldValues.Add(requestedField, fieldValue);
            }
            return new Field(fieldType, requestingType)
            {
                Value = fieldValue
            };
        }


        public static void UpdateLastFieldValue(this IEditorConfiguration configuration, IField field)
        {
            var requestedField = field.Scope + "." + field.FieldType.Name;
            // Check if the configuration exists
            if (configuration.LastUsedFieldValues == null)
            {
                configuration.LastUsedFieldValues = new Dictionary<string, object>();
            }
            // check if settings for the requesting type exist, if not create!
            if (configuration.LastUsedFieldValues.ContainsKey(requestedField))
            {
                configuration.LastUsedFieldValues[requestedField] = field.Value;
            }
            else
            {
                configuration.LastUsedFieldValues.Add(requestedField, field.Value);
            }
        }

        public static void ResetEditorPlacement(this IEditorConfiguration configuration)
        {
            configuration.WindowNormalPosition = new NativeRect(100, 100, 400, 400);
            configuration.WindowMaxPosition = new NativePoint(-1, -1);
            configuration.WindowMinPosition = new NativePoint(-1, -1);
            configuration.WindowPlacementFlags = 0;
            configuration.ShowWindowCommand = ShowWindowCommands.Normal;
        }

        public static WindowPlacement GetEditorPlacement(this IEditorConfiguration configuration)
        {
            var placement = WindowPlacement.Create();
            placement.NormalPosition = configuration.WindowNormalPosition;
            placement.MaxPosition = configuration.WindowMaxPosition;
            placement.MinPosition = configuration.WindowMinPosition;
            placement.ShowCmd = configuration.ShowWindowCommand;
            placement.Flags = configuration.WindowPlacementFlags;
            return placement;
        }

        public static void SetEditorPlacement(this IEditorConfiguration configuration, WindowPlacement placement)
        {
            configuration.WindowNormalPosition = placement.NormalPosition;
            configuration.WindowMaxPosition = placement.MaxPosition;
            configuration.WindowMinPosition = placement.MinPosition;
            configuration.ShowWindowCommand = placement.ShowCmd;
            configuration.WindowPlacementFlags = placement.Flags;
        }
    }
}
