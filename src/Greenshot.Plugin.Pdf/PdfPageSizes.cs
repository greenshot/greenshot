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

using System;
using System.Globalization;

namespace Greenshot.Plugin.Pdf;

internal readonly struct PdfPageSize
{
    public PdfPageSize(double widthMm, double heightMm)
    {
        WidthMm = widthMm;
        HeightMm = heightMm;
    }

    public double WidthMm { get; }

    public double HeightMm { get; }
}

internal static class PdfPageSizes
{
    public const string Image = nameof(Image);
    public const string Custom = nameof(Custom);

    public const string A3 = nameof(A3);
    public const string A4 = nameof(A4);
    public const string A5 = nameof(A5);
    public const string A6 = nameof(A6);
    public const string Letter = nameof(Letter);

    public static PdfPageSize ForPreset(string pageSize)
    {
        switch (pageSize)
        {
            case A3:
                return new PdfPageSize(297, 420);
            case A4:
                return new PdfPageSize(210, 297);
            case A5:
                return new PdfPageSize(148, 210);
            case A6:
                return new PdfPageSize(105, 148);
            case Letter:
                return new PdfPageSize(215.9, 279.4);
            default:
                throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Unknown PDF page size.");
        }
    }

    public static bool IsPreset(string pageSize)
    {
        return string.Equals(pageSize, A3, StringComparison.OrdinalIgnoreCase)
            || string.Equals(pageSize, A4, StringComparison.OrdinalIgnoreCase)
            || string.Equals(pageSize, A5, StringComparison.OrdinalIgnoreCase)
            || string.Equals(pageSize, A6, StringComparison.OrdinalIgnoreCase)
            || string.Equals(pageSize, Letter, StringComparison.OrdinalIgnoreCase);
    }

    public static double MillimetersToPoints(double millimeters)
    {
        return millimeters * 72.0 / 25.4;
    }

    public static string FormatNumber(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
