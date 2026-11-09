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
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Base.Interfaces.Plugin;

/// <summary>
/// A line of text found on a capture, with its words. <see cref="IDetectedFeature.Bounds"/> is the upright rectangle around
/// <see cref="Box"/>, for code which works with rectangles only.
/// </summary>
public interface IOcrLineFeature : IDetectedFeature
{
    /// <summary>
    /// The individual words contained in this line, along with their coordinates.
    /// Used for cursor collision detection and partial text selection.
    /// </summary>
    List<OcrWordInfo> Words { get; }

    /// <summary>
    /// Where the line is: rotated with the text
    /// </summary>
    OcrBox Box { get; }

    /// <summary>
    /// How sure the OCR is about the text, 0 to 1; null when the OCR doesn't say (Windows OCR)
    /// </summary>
    double? Score { get; }

    /// <summary>
    /// Where the text comes from: the OCR of the pixels, or the application itself through UI Automation
    /// </summary>
    TextSource Source { get; }
}

/// <summary>
/// Where the text of a line comes from
/// </summary>
public enum TextSource
{
    /// <summary>Recognized in the pixels of the capture</summary>
    Ocr,

    /// <summary>Read from the application through UI Automation: the text as the application knows it</summary>
    UiAutomation
}

/// <summary>
/// A word of an OCR line
/// </summary>
public class OcrWordInfo
{
    /// <param name="text">the text of the word</param>
    /// <param name="box">where the word is, rotated with the text</param>
    /// <param name="score">how sure the OCR is, 0 to 1, null when it doesn't say</param>
    public OcrWordInfo(string text, OcrBox box, double? score = null)
    {
        Text = text;
        Box = box;
        Score = score;
    }

    /// <summary>
    /// A word of horizontal text
    /// </summary>
    public OcrWordInfo(string text, NativeRect bounds, double? score = null) : this(text, OcrBox.FromRect(bounds), score)
    {
    }

    /// <summary>
    /// The text content of the word.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Where the word is, rotated with the text, in pixels of the captured image
    /// </summary>
    public OcrBox Box { get; private set; }

    /// <summary>
    /// The upright rectangle around the word, in pixels of the captured image; for rotated text a little larger than the word
    /// </summary>
    public NativeRect Bounds => Box.Bounds;

    /// <summary>
    /// How sure the OCR is about the word, 0 to 1; null when the OCR doesn't say (Windows OCR)
    /// </summary>
    public double? Score { get; }

    /// <summary>
    /// Where the link of the word goes (e.g. the web address of a link in a browser), null when the word isn't part of a link;
    /// only text read through UI Automation knows it
    /// </summary>
    public string LinkTarget { get; set; }

    /// <summary>
    /// Move the word, e.g. when the capture is cropped
    /// </summary>
    public void Offset(int x, int y) => Box = Box.Offset(x, y);
}
