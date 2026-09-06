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

using Dapplo.Windows.Common.Structs;

namespace Greenshot.Base.Interfaces.Plugin;

/// <summary>
/// Represents an abstract feature detected on a screen capture (e.g. OCR text block, QR code, face).
/// This metadata is independent of any specific form or presentation layer, allowing it to be
/// stored in databases, serialized, or transformed into interactive UI hotspots.
/// </summary>
public interface IDetectedFeature
{
    /// <summary>
    /// Bounding rectangle of the detected feature in pixel coordinates relative to the captured image.
    /// </summary>
    NativeRect Bounds { get; }

    /// <summary>
    /// A string identifying the type of the detected feature (e.g., "Barcode", "OcrWord").
    /// </summary>
    string FeatureType { get; }

    /// <summary>
    /// The primary text content or payload associated with the feature.
    /// </summary>
    string Text { get; }

    /// <summary>
    /// Optional tooltip descriptive text to display when hovering over the feature in the UI.
    /// </summary>
    string ToolTipText { get; }

    /// <summary>
    /// Translates the coordinates of the feature by the specified offsets.
    /// </summary>
    /// <param name="x">The horizontal offset.</param>
    /// <param name="y">The vertical offset.</param>
    void Offset(int x, int y);
}
