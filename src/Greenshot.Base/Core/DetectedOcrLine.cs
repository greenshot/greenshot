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
using System.Linq;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Base.Core;

/// <summary>
/// A line of text found by an OCR engine, or read from the application through UI Automation
/// </summary>
public class DetectedOcrLine : IOcrLineFeature
{
    /// <param name="box">where the line is; empty: around its words, at the angle of the first word</param>
    /// <param name="text">the text of the line</param>
    /// <param name="words">the words of the line</param>
    /// <param name="score">how sure the OCR is, 0 to 1, null when it doesn't say</param>
    public DetectedOcrLine(OcrBox box, string text, List<OcrWordInfo> words, double? score = null)
    {
        Text = text;
        Words = words ?? new List<OcrWordInfo>();
        Score = score;
        Box = box.IsEmpty ? Words.Aggregate(default(OcrBox), (line, word) => line.Union(word.Box)) : box;
    }

    /// <summary>
    /// A line of horizontal text
    /// </summary>
    public DetectedOcrLine(NativeRect bounds, string text, List<OcrWordInfo> words, double? score = null) : this(OcrBox.FromRect(bounds), text, words, score)
    {
    }

    public OcrBox Box { get; private set; }
    public NativeRect Bounds => Box.Bounds;
    public double? Score { get; }

    /// <summary>The OCR, unless set to UI Automation for text read from the application</summary>
    public TextSource Source { get; set; } = TextSource.Ocr;

    public string FeatureType => "OcrLine";
    public string Text { get; }
    public string ToolTipText => Text;
    public List<OcrWordInfo> Words { get; }

    public void Offset(int x, int y)
    {
        Box = Box.Offset(x, y);
        foreach (var word in Words)
        {
            word.Offset(x, y);
        }
    }
}
