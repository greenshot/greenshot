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
using System.Drawing;
using System.Globalization;
using Dapplo.Ini.Converters;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Effects;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Registers all custom Dapplo.Ini value converters required by Greenshot's
    /// configuration interfaces.  Converters bridge the Dapplo.Ini framework to .NET
    /// types that have no built-in support in the framework.
    ///
    /// Call <see cref="Register"/> once at startup before constructing any
    /// <see cref="Dapplo.Ini.IniConfig"/> instance.
    /// </summary>
    public static class IniValueConverters
    {
        private static bool s_registered;

        /// <summary>
        /// Registers all Greenshot-specific value converters with
        /// <see cref="ValueConverterRegistry"/> so that Dapplo.Ini can serialise and
        /// deserialise every type that appears in the configuration interfaces.
        /// Safe to call multiple times — registration is performed only once.
        /// </summary>
        public static void Register()
        {
            if (s_registered) return;
            s_registered = true;

            ValueConverterRegistry.Register(new ByteValueConverter());
            ValueConverterRegistry.Register(new DateTimeOffsetValueConverter());
            ValueConverterRegistry.Register(new ColorValueConverter());
            ValueConverterRegistry.Register(new NativePointValueConverter());
            ValueConverterRegistry.Register(new NativeRectValueConverter());
            ValueConverterRegistry.Register(new NativeSizeValueConverter());
            ValueConverterRegistry.Register(new DropShadowEffectValueConverter());
            ValueConverterRegistry.Register(new TornEdgeEffectValueConverter());
            ValueConverterRegistry.Register(new GreenshotObjectValueConverter());
        }
    }

    // ── Primitive gaps ───────────────────────────────────────────────────────

    /// <summary>
    /// Converter for <see cref="byte"/>.
    /// Dapplo.Ini does not ship a built-in byte converter; this fills the gap for
    /// <see cref="ICoreConfiguration.OutputPrintMonochromeThreshold"/>.
    /// </summary>
    public sealed class ByteValueConverter : ValueConverterBase<byte>
    {
        public override byte ConvertFromString(string raw, byte defaultValue = default)
            => byte.TryParse(raw?.Trim(), out var result) ? result : defaultValue;

        public override string ConvertToString(byte value)
            => value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converter for <see cref="DateTimeOffset"/> using ISO 8601 round-trip format.
    /// Required for <c>AccessTokenExpires</c> properties (RuntimeOnly) in Box and Dropbox configs.
    /// </summary>
    public sealed class DateTimeOffsetValueConverter : ValueConverterBase<DateTimeOffset>
    {
        public override DateTimeOffset ConvertFromString(string raw, DateTimeOffset defaultValue = default)
        {
            if (string.IsNullOrWhiteSpace(raw)) return defaultValue;
            return DateTimeOffset.TryParse(raw.Trim(), CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var result)
                ? result
                : defaultValue;
        }

        public override string ConvertToString(DateTimeOffset value)
            => value.ToString("O", CultureInfo.InvariantCulture);
    }

    // ── System.Drawing ───────────────────────────────────────────────────────

    /// <summary>
    /// Converter for <see cref="System.Drawing.Color"/>.
    /// Delegates to <see cref="System.Drawing.ColorConverter"/> so that the INI file
    /// format is identical to what the old Greenshot code produced (named colours such
    /// as <c>Red</c>, or ARGB tuples like <c>255, 0, 0, 0</c>).
    /// </summary>
    public sealed class ColorValueConverter : ValueConverterBase<Color>
    {
        private static readonly TypeConverter s_converter = new ColorConverter();

        public override Color ConvertFromString(string raw, Color defaultValue = default)
        {
            if (string.IsNullOrWhiteSpace(raw)) return defaultValue;
            try
            {
                return (Color)s_converter.ConvertFromInvariantString(raw);
            }
            catch
            {
                return defaultValue;
            }
        }

        public override string ConvertToString(Color value)
            => s_converter.ConvertToInvariantString(value);
    }

    // ── Dapplo.Windows structs ────────────────────────────────────────────────

    /// <summary>
    /// Converter for <see cref="NativePoint"/> bridging to
    /// <c>NativePointTypeConverter</c> so the format matches existing INI files
    /// (<c>x,y</c>).
    /// </summary>
    public sealed class NativePointValueConverter : ValueConverterBase<NativePoint>
    {
        private static readonly TypeConverter s_converter =
            TypeDescriptor.GetConverter(typeof(NativePoint));

        public override NativePoint ConvertFromString(string raw, NativePoint defaultValue = default)
        {
            if (string.IsNullOrWhiteSpace(raw)) return defaultValue;
            try
            {
                return (NativePoint)s_converter.ConvertFromInvariantString(raw);
            }
            catch
            {
                return defaultValue;
            }
        }

        public override string ConvertToString(NativePoint value)
            => s_converter.ConvertToInvariantString(value);
    }

    /// <summary>
    /// Converter for <see cref="NativeRect"/> bridging to
    /// <c>NativeRectTypeConverter</c> so the format matches existing INI files.
    /// </summary>
    public sealed class NativeRectValueConverter : ValueConverterBase<NativeRect>
    {
        private static readonly TypeConverter s_converter =
            TypeDescriptor.GetConverter(typeof(NativeRect));

        public override NativeRect ConvertFromString(string raw, NativeRect defaultValue = default)
        {
            if (string.IsNullOrWhiteSpace(raw)) return defaultValue;
            try
            {
                return (NativeRect)s_converter.ConvertFromInvariantString(raw);
            }
            catch
            {
                return defaultValue;
            }
        }

        public override string ConvertToString(NativeRect value)
            => s_converter.ConvertToInvariantString(value);
    }

    /// <summary>
    /// Converter for <see cref="NativeSize"/> bridging to
    /// <c>NativeSizeTypeConverter</c> so the format matches existing INI files.
    /// </summary>
    public sealed class NativeSizeValueConverter : ValueConverterBase<NativeSize>
    {
        private static readonly TypeConverter s_converter =
            TypeDescriptor.GetConverter(typeof(NativeSize));

        public override NativeSize ConvertFromString(string raw, NativeSize defaultValue = default)
        {
            if (string.IsNullOrWhiteSpace(raw)) return defaultValue;
            try
            {
                return (NativeSize)s_converter.ConvertFromInvariantString(raw);
            }
            catch
            {
                return defaultValue;
            }
        }

        public override string ConvertToString(NativeSize value)
            => s_converter.ConvertToInvariantString(value);
    }

    // ── Effect types ─────────────────────────────────────────────────────────

    /// <summary>
    /// Converter for <see cref="DropShadowEffect"/> delegating to the existing
    /// <see cref="EffectConverter"/> TypeConverter.
    /// </summary>
    public sealed class DropShadowEffectValueConverter : ValueConverterBase<DropShadowEffect>
    {
        private static readonly EffectConverter s_converter = new EffectConverter();

        public override DropShadowEffect ConvertFromString(string raw, DropShadowEffect defaultValue = default)
        {
            if (string.IsNullOrWhiteSpace(raw)) return defaultValue ?? new DropShadowEffect();
            try
            {
                return (DropShadowEffect)s_converter.ConvertFrom(null, CultureInfo.InvariantCulture, raw);
            }
            catch
            {
                return defaultValue ?? new DropShadowEffect();
            }
        }

        public override string ConvertToString(DropShadowEffect value)
            => (string)s_converter.ConvertTo(null, CultureInfo.InvariantCulture, value, typeof(string));
    }

    /// <summary>
    /// Converter for <see cref="TornEdgeEffect"/> delegating to the existing
    /// <see cref="EffectConverter"/> TypeConverter.
    /// </summary>
    public sealed class TornEdgeEffectValueConverter : ValueConverterBase<TornEdgeEffect>
    {
        private static readonly EffectConverter s_converter = new EffectConverter();

        public override TornEdgeEffect ConvertFromString(string raw, TornEdgeEffect defaultValue = default)
        {
            if (string.IsNullOrWhiteSpace(raw)) return defaultValue ?? new TornEdgeEffect();
            try
            {
                return (TornEdgeEffect)s_converter.ConvertFrom(null, CultureInfo.InvariantCulture, raw);
            }
            catch
            {
                return defaultValue ?? new TornEdgeEffect();
            }
        }

        public override string ConvertToString(TornEdgeEffect value)
            => (string)s_converter.ConvertTo(null, CultureInfo.InvariantCulture, value, typeof(string));
    }

    // ── Dictionary<string, object> value type ────────────────────────────────

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
    public sealed class GreenshotObjectValueConverter : ValueConverterBase<object>
    {
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
                var type = Type.GetType(typePart, throwOnError: true);
                var converter = TypeDescriptor.GetConverter(type);
                return converter.ConvertFromInvariantString(valuePart);
            }
            catch
            {
                return raw;
            }
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
