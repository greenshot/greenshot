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

using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Plugin.SmartText.Detection;

/// <summary>
/// A piece of data found in the OCR text of a capture: a web address, a phone number, the value of an own pattern. A detected
/// feature like a barcode, so it is kept with the capture and has the bounds of its words.
/// </summary>
public sealed class SmartTextItem : IDetectedFeature
{
    public const string Type = "SmartText";

    public SmartTextItem(string detectorId, string name, string value, string readText, OcrBox box, int lineIndex, int start, int length,
        bool isValidated, SmartTextAction action, string actionTarget, bool isOwnPattern)
    {
        DetectorId = detectorId;
        Name = name;
        Value = value;
        ReadText = readText;
        Box = box;
        LineIndex = lineIndex;
        Start = start;
        Length = length;
        IsValidated = isValidated;
        Action = action;
        ActionTarget = actionTarget;
        IsOwnPattern = isOwnPattern;
    }

    /// <summary>The id of the built-in detector (e.g. Phone) or the name of the own pattern</summary>
    public string DetectorId { get; }

    /// <summary>The name of the detector, in the language of the user</summary>
    public string Name { get; }

    /// <summary>The value, normalized (e.g. a phone number without spaces)</summary>
    public string Value { get; }

    /// <summary>The value as OCR read it</summary>
    public string ReadText { get; }

    /// <summary>Where the value is, turned with the text</summary>
    public OcrBox Box { get; private set; }

    /// <summary>The upright rectangle around the value</summary>
    public NativeRect Bounds => Box.Bounds;

    /// <summary>The OCR line, in the order of the text layout</summary>
    public int LineIndex { get; }

    /// <summary>The first character in the text of the line</summary>
    public int Start { get; }

    /// <summary>The characters in the text of the line</summary>
    public int Length { get; }

    /// <summary>A check digit or a parsed value confirmed it</summary>
    public bool IsValidated { get; }

    /// <summary>What the picker offers next to Copy</summary>
    public SmartTextAction Action { get; }

    /// <summary>What the action opens: the web address, mailto: or tel: link; null without action</summary>
    public string ActionTarget { get; }

    /// <summary>Found by an own pattern of the user (or the recipe), which comes before the built-in detectors</summary>
    public bool IsOwnPattern { get; }

    public string FeatureType => Type;

    public string Text => Value;

    public string ToolTipText => $"{Name}: {Value}";

    public void Offset(int x, int y)
    {
        Box = Box.Offset(x, y);
    }

    /// <summary>
    /// The item with a value the user corrected; the action follows the new value
    /// </summary>
    public SmartTextItem WithValue(string value)
    {
        if (value == null || value == Value) return this;
        string target = Action switch
        {
            SmartTextAction.Open => value,
            SmartTextAction.Mail => "mailto:" + value,
            SmartTextAction.Call => "tel:" + value,
            _ => null
        };
        // An own web address can't be rendered again here: the corrected value is copy-only
        var action = Action == SmartTextAction.OpenUrl ? SmartTextAction.None : Action;
        return new SmartTextItem(DetectorId, Name, value, ReadText, Box, LineIndex, Start, Length, IsValidated, action, target, IsOwnPattern);
    }

    public override string ToString() => ToolTipText;
}
