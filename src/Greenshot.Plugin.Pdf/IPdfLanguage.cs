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

using System.ComponentModel;
using Dapplo.Ini.Internationalization.Attributes;

namespace Greenshot.Plugin.Pdf;

/// <summary>
/// The texts of the [Pdf] section of greenshot.pdf.{ietf}.ini, with the English text shown per property.
/// Add a key to the en-US file and a corresponding property here.
/// </summary>
[IniLanguageSection("Pdf", ModuleName = "pdf")]
public interface IPdfLanguage : INotifyPropertyChanged
{
    /// <summary>
    /// Save as PDF
    /// </summary>
    string Destination { get; }

    /// <summary>
    /// Save as (displaying dialog)
    /// </summary>
    string DestinationOptionFileAs { get; }

    /// <summary>
    /// Only shrink to fit
    /// </summary>
    string ScaleShrink { get; }

    /// <summary>
    /// Fit to page
    /// </summary>
    string ScaleFit { get; }

    /// <summary>
    /// A3
    /// </summary>
    string PageA3 { get; }

    /// <summary>
    /// A4
    /// </summary>
    string PageA4 { get; }

    /// <summary>
    /// A5
    /// </summary>
    string PageA5 { get; }

    /// <summary>
    /// A6
    /// </summary>
    string PageA6 { get; }

    /// <summary>
    /// Automatic (image size)
    /// </summary>
    string PageImage { get; }

    /// <summary>
    /// Custom
    /// </summary>
    string PageCustom { get; }

    /// <summary>
    /// Letter
    /// </summary>
    string PageLetter { get; }

    /// <summary>
    /// PDF settings
    /// </summary>
    string SettingsTitle { get; }

    /// <summary>
    /// PDF document
    /// </summary>
    string LabelPdfDocument { get; }

    /// <summary>
    /// Unit
    /// </summary>
    string LabelUnit { get; }

    /// <summary>
    /// Page size
    /// </summary>
    string LabelPageSize { get; }

    /// <summary>
    /// Width
    /// </summary>
    string LabelPageWidth { get; }

    /// <summary>
    /// Height
    /// </summary>
    string LabelPageHeight { get; }

    /// <summary>
    /// Margins
    /// </summary>
    string LabelMargins { get; }

    /// <summary>
    /// Top
    /// </summary>
    string LabelTop { get; }

    /// <summary>
    /// Bottom
    /// </summary>
    string LabelBottom { get; }

    /// <summary>
    /// Left
    /// </summary>
    string LabelLeft { get; }

    /// <summary>
    /// Right
    /// </summary>
    string LabelRight { get; }

    /// <summary>
    /// Image scaling
    /// </summary>
    string LabelScaling { get; }

    /// <summary>
    /// Millimeters (mm)
    /// </summary>
    string UnitMm { get; }

    /// <summary>
    /// Centimeters (cm)
    /// </summary>
    string UnitCm { get; }

    /// <summary>
    /// Inches (in)
    /// </summary>
    string UnitInch { get; }
}
