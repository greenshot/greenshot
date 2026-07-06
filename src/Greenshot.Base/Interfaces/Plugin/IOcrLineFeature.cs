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

using System.Collections.Generic;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Base.Interfaces.Plugin;

/// <summary>
/// Represents a single line of OCR text detected on a screenshot.
/// Enables line-based feature mapping while retaining detailed word-level coordinates inside.
/// </summary>
public interface IOcrLineFeature : IDetectedFeature
{
    /// <summary>
    /// The individual words contained in this line, along with their coordinates.
    /// Used for cursor collision detection and partial text selection.
    /// </summary>
    List<OcrWordInfo> Words { get; }
}

/// <summary>
/// Represents word-level metadata inside an OCR line feature.
/// </summary>
public class OcrWordInfo
{
    /// <summary>
    /// The bounding box of the word in pixel coordinates relative to the captured image.
    /// </summary>
    public NativeRect Bounds { get; set; }

    /// <summary>
    /// The text content of the word.
    /// </summary>
    public string Text { get; set; }
}
