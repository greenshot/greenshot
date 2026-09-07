/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.Structs;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Fields;

namespace Greenshot.Editor.Configuration
{
    /// <summary>
    /// Helper methods for working with <see cref="IEditorConfiguration"/> that are
    /// not pure configuration state, and therefore do not belong on the configuration
    /// interface itself.
    /// </summary>
    public static class EditorConfigurationHelper
    {
        /// <summary>
        /// Creates a field of the given type for <paramref name="requestingType"/>, reusing
        /// the last-used value from the configuration if one is stored.
        /// </summary>
        /// <param name="config">The editor configuration section.</param>
        /// <param name="requestingType">Type of the class for which to create the field.</param>
        /// <param name="fieldType">FieldType of the field to construct.</param>
        /// <param name="preferredDefaultValue">Value to use when no stored value exists.</param>
        /// <returns>A new <see cref="IField"/> with its value initialised from the stored last-used value or the preferred default.</returns>
        public static IField CreateField(IEditorConfiguration config, Type requestingType, IFieldType fieldType, object preferredDefaultValue)
        {
            string requestingTypeName = requestingType.Name;
            string requestedField = requestingTypeName + "." + fieldType.Name;
            object fieldValue = preferredDefaultValue;

            config.LastUsedFieldValues ??= new Dictionary<string, object>();

            if (config.LastUsedFieldValues.ContainsKey(requestedField))
            {
                if (config.LastUsedFieldValues[requestedField] != null)
                {
                    fieldValue = config.LastUsedFieldValues[requestedField];
                }
                else
                {
                    config.LastUsedFieldValues[requestedField] = fieldValue;
                }
            }
            else
            {
                config.LastUsedFieldValues.Add(requestedField, fieldValue);
            }

            return new Field(fieldType, requestingType)
            {
                Value = fieldValue
            };
        }

        /// <summary>
        /// Persists the current value of <paramref name="field"/> into the
        /// <see cref="IEditorConfiguration.LastUsedFieldValues"/> dictionary so it can
        /// be reused the next time the editor opens.
        /// </summary>
        /// <param name="config">The editor configuration section.</param>
        /// <param name="field">The field whose value should be stored.</param>
        public static void UpdateLastFieldValue(IEditorConfiguration config, IField field)
        {
            string requestedField = field.Scope + "." + field.FieldType.Name;
            config.LastUsedFieldValues ??= new Dictionary<string, object>();

            if (config.LastUsedFieldValues.ContainsKey(requestedField))
            {
                config.LastUsedFieldValues[requestedField] = field.Value;
            }
            else
            {
                config.LastUsedFieldValues.Add(requestedField, field.Value);
            }
        }

        /// <summary>
        /// Resets all editor window placement properties to their defaults.
        /// </summary>
        /// <param name="config">The editor configuration section.</param>
        public static void ResetEditorPlacement(IEditorConfiguration config)
        {
            config.WindowNormalPosition = new NativeRect(100, 100, 400, 400);
            config.WindowMaxPosition = new NativePoint(-1, -1);
            config.WindowMinPosition = new NativePoint(-1, -1);
            config.WindowPlacementFlags = 0;
            config.ShowWindowCommand = ShowWindowCommands.Normal;
        }

        /// <summary>
        /// Constructs a <see cref="WindowPlacement"/> from the stored editor window position values.
        /// </summary>
        /// <param name="config">The editor configuration section.</param>
        /// <returns>A <see cref="WindowPlacement"/> reflecting the stored position.</returns>
        public static WindowPlacement GetEditorPlacement(IEditorConfiguration config)
        {
            WindowPlacement placement = WindowPlacement.Create();
            placement.NormalPosition = config.WindowNormalPosition;
            placement.MaxPosition = config.WindowMaxPosition;
            placement.MinPosition = config.WindowMinPosition;
            placement.ShowCmd = config.ShowWindowCommand;
            placement.Flags = config.WindowPlacementFlags;
            return placement;
        }

        /// <summary>
        /// Stores the given <see cref="WindowPlacement"/> into the editor configuration.
        /// </summary>
        /// <param name="config">The editor configuration section.</param>
        /// <param name="placement">The window placement to persist.</param>
        public static void SetEditorPlacement(IEditorConfiguration config, WindowPlacement placement)
        {
            config.WindowNormalPosition = placement.NormalPosition;
            config.WindowMaxPosition = placement.MaxPosition;
            config.WindowMinPosition = placement.MinPosition;
            config.ShowWindowCommand = placement.ShowCmd;
            config.WindowPlacementFlags = placement.Flags;
        }
    }
}
