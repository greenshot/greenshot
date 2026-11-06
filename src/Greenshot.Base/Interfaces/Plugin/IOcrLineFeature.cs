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
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Base.Interfaces.Plugin;

/// <summary>
/// Represents a single line of OCR text detected on a screenshot.
/// Enables line-based feature mapping while retaining detailed word-level coordinates inside.
/// For rotated text the OCR reports the text deskewed: TextBounds is where the text is after rotating it by Angle around RotationCenter.
/// Bounds is the axis-aligned box around the rotated text, a little larger than the text.
/// </summary>
public interface IOcrLineFeature : IDetectedFeature
{
    /// <summary>
    /// The individual words contained in this line, along with their coordinates.
    /// Used for cursor collision detection and partial text selection.
    /// </summary>
    List<OcrWordInfo> Words { get; }

    /// <summary>
    /// The line before the rotation (the union of the words), in pixel coordinates relative to the captured image
    /// </summary>
    Rect TextBounds { get; }

    /// <summary>
    /// The angle of the text in degrees, clockwise, 0 for horizontal text
    /// </summary>
    double Angle { get; }

    /// <summary>
    /// The centre of the rotation, in pixel coordinates relative to the captured image
    /// </summary>
    Point RotationCenter { get; }
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
    /// <param name="textBounds">The word before the rotation, in pixel coordinates relative to the captured image</param>
    /// <param name="angle">The angle of the text in degrees, clockwise, 0 for horizontal text</param>
    /// <param name="rotationCenter">The centre of the rotation</param>
    public OcrWordInfo(string text, Rect textBounds, double angle = 0, Point rotationCenter = default)
    {
        Text = text;
        TextBounds = textBounds;
        Bounds = GetBounds(textBounds, angle, rotationCenter);
    }

    /// <summary>
    /// The axis-aligned box around the word, in pixel coordinates relative to the captured image.
    /// For rotated text it is a little larger than the word.
    /// </summary>
    public NativeRect Bounds { get; private set; }

    /// <summary>
    /// The word before the rotation of its line, in pixel coordinates relative to the captured image
    /// </summary>
    public Rect TextBounds { get; private set; }

    /// <summary>
    /// The text content of the word.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Translates the coordinates of the word by the specified offsets, the line moves the centre of the rotation.
    /// </summary>
    public void Offset(int x, int y)
    {
        Bounds = new NativeRect(Bounds.X + x, Bounds.Y + y, Bounds.Width, Bounds.Height);
        if (!TextBounds.IsEmpty)
        {
            TextBounds = new Rect(TextBounds.X + x, TextBounds.Y + y, TextBounds.Width, TextBounds.Height);
        }
    }

    /// <summary>
    /// The axis-aligned box around the rectangle rotated by angle around the centre, rounded outwards
    /// </summary>
    private static NativeRect GetBounds(Rect rect, double angle, Point center)
    {
        if (rect.IsEmpty)
        {
            return NativeRect.Empty;
        }
        if (angle != 0)
        {
            var matrix = Matrix.Identity;
            // y points down, so a positive angle rotates clockwise on the screen
            matrix.RotateAt(angle, center.X, center.Y);
            rect = Rect.Transform(rect, matrix);
        }
        // The rotation has rounding errors, e.g. 59.99999 for 60: they shouldn't add a pixel
        const double tolerance = 1e-6;
        int left = (int)Math.Floor(rect.Left + tolerance);
        int top = (int)Math.Floor(rect.Top + tolerance);
        return new NativeRect(left, top, (int)Math.Ceiling(rect.Right - tolerance) - left, (int)Math.Ceiling(rect.Bottom - tolerance) - top);
    }
}
