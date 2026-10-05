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

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Dapplo.Ini.Attributes;
using Dapplo.Ini.Interfaces;

namespace Greenshot.Plugin.Pdf.Configuration;

[IniSection("Pdf")]
[Description("Greenshot PDF Plugin configuration")]
public interface IPdfConfiguration : IIniSection
{
    [Description("Selected PDF page size (Image, Custom, A3, A4, A5, A6, Letter)")]
    [DefaultValue(PdfPageSizes.Image)]
    string PageSize { get; set; }

    [Description("Measurement unit displayed in PDF settings (mm, cm, inch)")]
    [DefaultValue(PdfMeasurementUnits.Mm)]
    string MeasurementUnit { get; set; }

    [Description("Custom page width in millimeters")]
    [DefaultValue(210.0)]
    [Range(0.01, 10000)]
    double PageWidthMm { get; set; }

    [Description("Custom page height in millimeters")]
    [DefaultValue(297.0)]
    [Range(0.01, 10000)]
    double PageHeightMm { get; set; }

    [Description("Top margin in millimeters")]
    [DefaultValue(0.0)]
    [Range(0, 10000)]
    double MarginTopMm { get; set; }

    [Description("Bottom margin in millimeters")]
    [DefaultValue(0.0)]
    [Range(0, 10000)]
    double MarginBottomMm { get; set; }

    [Description("Left margin in millimeters")]
    [DefaultValue(0.0)]
    [Range(0, 10000)]
    double MarginLeftMm { get; set; }

    [Description("Right margin in millimeters")]
    [DefaultValue(0.0)]
    [Range(0, 10000)]
    double MarginRightMm { get; set; }

    [Description("How the image is scaled to fit a fixed page (OnlyShrinkToFit, FitToPage)")]
    [DefaultValue(PdfScalingModes.OnlyShrinkToFit)]
    string ScalingMode { get; set; }

    [Description("Show the save dialog when exporting to the PDF destination")]
    [DefaultValue(true)]
    bool ShowSaveDialog { get; set; }
}
