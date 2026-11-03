/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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

namespace Greenshot.Base.Native;

/// <summary>
/// Provides conversion utilities for IEEE 754 half-precision (16-bit) floating-point values.
/// </summary>
/// <remarks>
/// .NET Framework 4.8 does not include <c>System.Half</c>, so this helper provides the necessary
/// conversions between half-float (used in <c>DXGI_FORMAT_R16G16B16A16_FLOAT</c> textures and HDR files) and single-precision float.
/// </remarks>
internal static class HalfHelper
{
    /// <summary>
    /// Converts an IEEE 754 half-precision (16-bit) floating-point value to a single-precision (32-bit) float.
    /// </summary>
    /// <remarks>
    /// Uses direct bit manipulation to construct the IEEE 754 single-precision representation
    /// from the half-precision sign, exponent, and mantissa fields. Correctly handles normalized values,
    /// subnormals (renormalized to float), signed zeros, infinities, and NaNs.
    /// </remarks>
    /// <param name="value">The half-float value encoded as a <see cref="ushort"/>.</param>
    /// <returns>The equivalent single-precision float value.</returns>
    public static unsafe float HalfToFloat(ushort value)
    {
        // Extract sign, exponent, and mantissa from the 16-bit half-float
        uint sign = ((uint)value & 0x8000u) << 16; // Sign bit shifted to float position (bit 31)
        uint exp = (uint)((value >> 10) & 0x1F);    // 5-bit exponent
        uint mant = (uint)(value & 0x3FF);           // 10-bit mantissa

        uint f;
        if (exp == 0)
        {
            if (mant == 0)
            {
                // ±0
                f = sign;
            }
            else
            {
                // Subnormal half-float → normalized single-precision float.
                // Half subnormal: value = (-1)^s × 2^(-14) × (0.mantissa)
                // Shift the mantissa left until the implicit 1 bit appears at bit 10,
                // adjusting the exponent downward for each shift.
                uint e = 113u; // Float bias (127) - half bias offset (14) = 113
                while ((mant & 0x0400) == 0)
                {
                    mant <<= 1;
                    e--;
                }

                mant &= 0x03FFu; // Remove the now-implicit leading 1 bit
                f = sign | (e << 23) | (mant << 13);
            }
        }
        else if (exp == 31)
        {
            // Infinity or NaN: map half exponent 31 → float exponent 255 (0xFF)
            f = sign | 0x7F800000u | (mant << 13);
        }
        else
        {
            // Normalized: rebias exponent from half bias (15) to float bias (127)
            // Exponent adjustment: +112 (= 127 - 15)
            f = sign | ((exp + 112u) << 23) | (mant << 13);
        }

        return *(float*)&f;
    }

    /// <summary>
    /// Converts a single-precision float to an IEEE 754 half-precision value, rounding to the nearest (ties to even).
    /// Too large values become infinity, NaN stays NaN, too small values become (signed) zero or a subnormal.
    /// </summary>
    /// <param name="value">The float value.</param>
    /// <returns>The half-float encoded as a <see cref="ushort"/>.</returns>
    public static unsafe ushort FloatToHalf(float value)
    {
        uint f = *(uint*)&value;
        uint sign = (f >> 16) & 0x8000u;
        int exponent = (int)((f >> 23) & 0xFF);
        uint mantissa = f & 0x7FFFFFu;

        if (exponent == 0xFF)
        {
            // Infinity or NaN (keep a mantissa bit for NaN)
            return (ushort)(sign | 0x7C00u | (mantissa != 0 ? 0x200u : 0u));
        }

        int halfExponent = exponent - 127 + 15;
        if (halfExponent >= 31)
        {
            return (ushort)(sign | 0x7C00u);
        }

        if (halfExponent <= 0)
        {
            if (halfExponent < -10)
            {
                return (ushort)sign;
            }
            // Subnormal: the implicit 1 becomes explicit, shifted into place
            mantissa |= 0x800000u;
            int shift = 14 - halfExponent;
            uint halfMantissa = mantissa >> shift;
            uint remainder = mantissa & ((1u << shift) - 1);
            uint halfway = 1u << (shift - 1);
            if (remainder > halfway || (remainder == halfway && (halfMantissa & 1) != 0))
            {
                halfMantissa++;
            }
            return (ushort)(sign | halfMantissa);
        }

        uint result = sign | ((uint)halfExponent << 10) | (mantissa >> 13);
        uint rest = mantissa & 0x1FFFu;
        // Round to nearest, ties to even; a carry into the exponent is correct (it may become infinity)
        if (rest > 0x1000u || (rest == 0x1000u && (result & 1) != 0))
        {
            result++;
        }
        return (ushort)result;
    }
}
