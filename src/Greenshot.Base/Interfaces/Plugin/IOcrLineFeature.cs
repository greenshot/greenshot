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

using System.Collections.Generic;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;

namespace Greenshot.Base.Interfaces.Plugin;

/// <summary>
/// Represents a single line of OCR text detected on a screenshot.
/// Enables line-based feature mapping while retaining detailed word-level coordinates inside.
/// Bounds is the axis-aligned box around the line: for rotated text a little larger than the text, RotatedBounds is where the text is.
/// </summary>
public interface IOcrLineFeature : IDetectedFeature
{
    /// <summary>
    /// The individual words contained in this line, along with their coordinates.
    /// Used for cursor collision detection and partial text selection.
    /// </summary>
    List<OcrWordInfo> Words { get; }

    /// <summary>
    /// The line as the OCR reported it, in pixel coordinates relative to the captured image: rotated like the text
    /// </summary>
    RotatedRect RotatedBounds { get; }
}

/// <summary>
/// Represents word-level metadata inside an OCR line feature.
/// </summary>
public class OcrWordInfo
{
    /// <summary>
    /// A word
    /// </summary>
    /// <param name="text">The text of the word</param>
    /// <param name="rotatedBounds">Where the word is, rotated like the text</param>
    public OcrWordInfo(string text, RotatedRect rotatedBounds)
    {
        Text = text;
        RotatedBounds = rotatedBounds;
    }

    /// <summary>
    /// The word as the OCR reported it, in pixel coordinates relative to the captured image: rotated like the text
    /// </summary>
    public RotatedRect RotatedBounds { get; private set; }

    /// <summary>
    /// The axis-aligned box around the word, in pixel coordinates relative to the captured image.
    /// For rotated text it is a little larger than the word.
    /// </summary>
    public NativeRect Bounds => RotatedBounds.Bounds;

    /// <summary>
    /// The text content of the word.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Translates the coordinates of the word by the specified offsets.
    /// </summary>
    public void Offset(int x, int y) => RotatedBounds = RotatedBounds.Offset(x, y);
}
