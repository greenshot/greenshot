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

using System.Collections.Generic;
using System.Windows;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Base.Core;

public class DetectedOcrLine : IOcrLineFeature
{
    public NativeRect Bounds { get; private set; }
    public Rect TextBounds { get; private set; }
    public double Angle { get; }
    public Point RotationCenter { get; private set; }
    public string FeatureType => "OcrLine";
    public string Text { get; }
    public string ToolTipText => Text;
    public List<OcrWordInfo> Words { get; }

    /// <summary>
    /// A line of text, where it is comes from its words
    /// </summary>
    /// <param name="text">The text of the line</param>
    /// <param name="words">The words of the line, rotated by angle around rotationCenter</param>
    /// <param name="angle">The angle of the text in degrees, clockwise, 0 for horizontal text</param>
    /// <param name="rotationCenter">The centre of the rotation</param>
    public DetectedOcrLine(string text, List<OcrWordInfo> words, double angle = 0, Point rotationCenter = default)
    {
        Text = text;
        Words = words;
        Angle = angle;
        RotationCenter = rotationCenter;
        var bounds = NativeRect.Empty;
        var textBounds = Rect.Empty;
        foreach (var word in words)
        {
            bounds = bounds.IsEmpty ? word.Bounds : bounds.Union(word.Bounds);
            textBounds.Union(word.TextBounds);
        }
        Bounds = bounds;
        TextBounds = textBounds;
    }

    public void Offset(int x, int y)
    {
        Bounds = Bounds.Offset(x, y);
        if (!TextBounds.IsEmpty)
        {
            TextBounds = new Rect(TextBounds.X + x, TextBounds.Y + y, TextBounds.Width, TextBounds.Height);
        }
        RotationCenter = new Point(RotationCenter.X + x, RotationCenter.Y + y);

        foreach (var word in Words)
        {
            word.Offset(x, y);
        }
    }
}
