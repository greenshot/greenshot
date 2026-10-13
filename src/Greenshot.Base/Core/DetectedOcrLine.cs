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
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Base.Core;

public class DetectedOcrLine : IOcrLineFeature
{
    public RotatedRect RotatedBounds { get; private set; }
    public NativeRect Bounds => RotatedBounds.Bounds;
    public string FeatureType => "OcrLine";
    public string Text { get; }
    public string ToolTipText => Text;
    public List<OcrWordInfo> Words { get; }

    /// <summary>
    /// A line of text
    /// </summary>
    /// <param name="rotatedBounds">Where the line is, rotated like the text: for the OCR the union of the words</param>
    /// <param name="text">The text of the line</param>
    /// <param name="words">The words of the line</param>
    public DetectedOcrLine(RotatedRect rotatedBounds, string text, List<OcrWordInfo> words)
    {
        RotatedBounds = rotatedBounds;
        Text = text;
        Words = words;
    }

    public void Offset(int x, int y)
    {
        RotatedBounds = RotatedBounds.Offset(x, y);

        foreach (var word in Words)
        {
            word.Offset(x, y);
        }
    }
}
