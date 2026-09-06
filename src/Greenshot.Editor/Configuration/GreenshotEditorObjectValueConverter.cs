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
using System.ComponentModel;
using System.Reflection;
using System.ServiceModel.Security;
using Dapplo.Ini.Converters;
using Greenshot.Editor.Helpers;
using log4net;

namespace Greenshot.Editor.Configuration
{
    /// <summary>
    /// Converter for <see cref="object"/>-typed dictionary values in
    /// <c>IEditorConfiguration.LastUsedFieldValues</c>.
    ///
    /// The format is the one produced by the old Greenshot IniFile subsystem:
    /// <c>TypeFullName,AssemblyName:InvariantStringValue</c>
    /// (e.g. <c>System.Drawing.Color,System.Drawing:Red</c>).
    ///
    /// When deserialising, the type and assembly are resolved and the value is
    /// converted using the type's own <see cref="TypeConverter"/>.
    /// On failure the raw string is returned unchanged.
    /// </summary>
    internal class GreenshotEditorObjectValueConverter : ValueConverterBase<object>
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(GreenshotEditorObjectValueConverter));
        public override object ConvertFromString(string raw, object defaultValue = default)
        {
            if (string.IsNullOrWhiteSpace(raw)) return defaultValue;

            // Expected format: "FullTypeName,AssemblyName:value"
            int colonIdx = raw.IndexOf(':');
            if (colonIdx <= 0) return raw;

            string typePart = raw.Substring(0, colonIdx);
            string valuePart = raw.Substring(colonIdx + 1);
            try
            {
                if (BinaryFormatterHelper.TryGetType(typePart, out var type))
                {
                    var converter = TypeDescriptor.GetConverter(type);
                    return converter.ConvertFromInvariantString(valuePart);
                }
            }
            catch
            {
                return raw;
            }
            LOG.Warn($"Unexpected type in .ini file detected, maybe vulnerability attack? Suspicious information: {raw}");
            throw new SecurityAccessDeniedException($"Unexpected type in .ini file detected, maybe vulnerability attack? Suspicious information: {raw}");
        }

        public override string ConvertToString(object value)
        {
            if (value is null) return string.Empty;
            var type = value.GetType();
            var converter = TypeDescriptor.GetConverter(type);
            var valueStr = converter.ConvertToInvariantString(value);

            // Build assembly qualifier, abbreviated for Greenshot assemblies
            var assemblyName = type.Assembly.FullName ?? string.Empty;
            if (assemblyName.StartsWith("Greenshot", StringComparison.OrdinalIgnoreCase))
            {
                int commaIdx = assemblyName.IndexOf(',');
                if (commaIdx > 0) assemblyName = assemblyName.Substring(0, commaIdx);
            }

            return $"{type.FullName},{assemblyName}:{valueStr}";
        }
    }
}
