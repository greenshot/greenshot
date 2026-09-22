/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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

namespace Greenshot.Native;

/// <summary>
/// Provides conversion utilities for IEEE 754 half-precision (16-bit) floating-point values.
/// </summary>
/// <remarks>
/// .NET Framework 4.8 does not include <c>System.Half</c>, so this helper provides the necessary
/// conversion from half-float (used in <c>DXGI_FORMAT_R16G16B16A16_FLOAT</c> textures) to single-precision float.
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
}
