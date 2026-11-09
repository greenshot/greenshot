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

using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Greenshot.Plugin.SmartText.Detection;

/// <summary>
/// The checks of <see cref="SmartTextValidate"/>: a check digit or a parsed value keeps out what only looks like the data
/// </summary>
public static class SmartTextValidators
{
    /// <summary>
    /// Does the (normalized) value pass the check
    /// </summary>
    public static bool IsValid(string validate, string value)
    {
        if (string.IsNullOrEmpty(validate) || Is(validate, SmartTextValidate.None)) return true;
        if (string.IsNullOrEmpty(value)) return false;
        if (Is(validate, SmartTextValidate.Mod97)) return IsMod97(value);
        if (Is(validate, SmartTextValidate.Luhn)) return IsLuhn(Digits(value));
        if (Is(validate, SmartTextValidate.Isbn)) return IsIsbn(value);
        if (Is(validate, SmartTextValidate.Ean)) return IsEan(Digits(value));
        if (Is(validate, SmartTextValidate.IpAddress)) return IsIpAddress(value);
        if (Is(validate, SmartTextValidate.Bic)) return IsBic(value);
        if (Is(validate, SmartTextValidate.Coordinates)) return IsCoordinates(value);
        if (Is(validate, SmartTextValidate.Hex)) return value.Any(char.IsDigit) && value.Any(char.IsLetter);
        return false;
    }

    /// <summary>
    /// Is this the name of a check (ignoring case)
    /// </summary>
    public static bool IsKnown(string validate) =>
        string.IsNullOrEmpty(validate) || new[]
        {
            SmartTextValidate.None, SmartTextValidate.Mod97, SmartTextValidate.Luhn, SmartTextValidate.Isbn, SmartTextValidate.Ean,
            SmartTextValidate.IpAddress, SmartTextValidate.Bic, SmartTextValidate.Coordinates, SmartTextValidate.Hex
        }.Any(known => Is(validate, known));

    internal static bool Is(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    internal static string Digits(string value) => new string((value ?? string.Empty).Where(c => c >= '0' && c <= '9').ToArray());

    /// <summary>
    /// ISO 7064 mod 97-10 as used by the IBAN: the first four characters moved to the end, letters as 10 to 35, the number mod 97 is 1
    /// </summary>
    public static bool IsMod97(string value)
    {
        string compact = new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (compact.Length < 5 || compact.Length > 34) return false;
        var rearranged = new StringBuilder();
        foreach (char c in compact.Substring(4) + compact.Substring(0, 4))
        {
            if (c >= '0' && c <= '9') rearranged.Append(c);
            else if (c >= 'A' && c <= 'Z') rearranged.Append((c - 'A' + 10).ToString(CultureInfo.InvariantCulture));
            else return false;
        }

        // Digit by digit, the number is too long for a long
        int remainder = 0;
        foreach (char digit in rearranged.ToString())
        {
            remainder = (remainder * 10 + (digit - '0')) % 97;
        }

        return remainder == 1;
    }

    /// <summary>
    /// The Luhn check digit (payment cards)
    /// </summary>
    public static bool IsLuhn(string digits)
    {
        if (digits.Length < 2) return false;
        int sum = 0;
        bool doubled = false;
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            int digit = digits[i] - '0';
            if (doubled)
            {
                digit *= 2;
                if (digit > 9) digit -= 9;
            }

            sum += digit;
            doubled = !doubled;
        }

        return sum % 10 == 0;
    }

    /// <summary>
    /// The check digit of an ISBN-10 (mod 11, X for 10) or an ISBN-13 (EAN)
    /// </summary>
    public static bool IsIsbn(string value)
    {
        string compact = new string(value.Where(c => char.IsDigit(c) || c == 'X' || c == 'x').ToArray()).ToUpperInvariant();
        if (compact.Length == 13) return IsEan(compact) && (compact.StartsWith("978", StringComparison.Ordinal) || compact.StartsWith("979", StringComparison.Ordinal));
        if (compact.Length != 10) return false;
        int sum = 0;
        for (int i = 0; i < 10; i++)
        {
            char c = compact[i];
            int digit;
            if (c == 'X')
            {
                if (i != 9) return false;
                digit = 10;
            }
            else
            {
                digit = c - '0';
            }

            sum += digit * (10 - i);
        }

        return sum % 11 == 0;
    }

    /// <summary>
    /// The check digit of an EAN-8, UPC-A (12), EAN-13 or GTIN-14
    /// </summary>
    public static bool IsEan(string digits)
    {
        if (digits.Length != 8 && digits.Length != 12 && digits.Length != 13 && digits.Length != 14) return false;
        int sum = 0;
        // From the right, without the check digit: weights 3, 1, 3, ...
        for (int i = digits.Length - 2, weight = 3; i >= 0; i--, weight = 4 - weight)
        {
            sum += (digits[i] - '0') * weight;
        }

        return (10 - sum % 10) % 10 == digits[digits.Length - 1] - '0';
    }

    private static bool IsIpAddress(string value)
    {
        if (!IPAddress.TryParse(value, out var address)) return false;
        // IPAddress also takes "1" or "1.2" as IPv4: only the full form with four parts
        return address.AddressFamily == AddressFamily.InterNetworkV6 || value.Count(c => c == '.') == 3;
    }

    private static bool IsBic(string value)
    {
        string compact = value.Trim().ToUpperInvariant();
        if (compact.Length != 8 && compact.Length != 11) return false;
        string country = compact.Substring(4, 2);
        try
        {
            _ = new RegionInfo(country);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool IsCoordinates(string value)
    {
        var parts = value.Split(',');
        return parts.Length == 2 &&
               double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double latitude) &&
               double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double longitude) &&
               Math.Abs(latitude) <= 90 && Math.Abs(longitude) <= 180;
    }
}
