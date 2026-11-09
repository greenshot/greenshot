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
using System.Text.Json.Serialization;

namespace Greenshot.Plugin.SmartText.Detection;

/// <summary>
/// What a Smart text detector finds: a regular expression, optionally next to a label or in an area of the capture, with what
/// is done to the value (normalized, validated). The built-in detectors, the own patterns (a .gssmarttext.json file each in the
/// SmartText folder next to greenshot.ini) and the inline patterns of a recipe step are all this. See docs/vNext/smart-text.md.
/// </summary>
public sealed class SmartTextPattern
{
    /// <summary>The value of <see cref="Kind"/> in a pattern file</summary>
    public const string KindName = "SmartText";

    /// <summary>The format of the pattern files this Greenshot writes and reads</summary>
    public const int CurrentVersion = 1;

    /// <summary>A group of <see cref="Pattern"/> with this name is the value, e.g. "Order (?&lt;value&gt;\d+)"; without it the whole match</summary>
    public const string ValueGroupName = "value";

    /// <summary>"SmartText" in a pattern file, so another JSON file isn't taken for one; not used in recipes</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("kind")]
    public string Kind { get; set; }

    /// <summary>The format of a pattern file, not used in recipes</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("version")]
    public int Version { get; set; }

    /// <summary>The name, e.g. "Customer number": shown in the picker and the settings, recipes use it in Detectors</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>The .NET regular expression of the value</summary>
    [JsonPropertyName("pattern")]
    public string Pattern { get; set; }


    /// <summary>A .NET regular expression of the label the value is next to, e.g. "Customer (no\.?|number)"; null: everywhere</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("label")]
    public string Label { get; set; }

    /// <summary>Where the value is, seen from the label: Right, Below, RightOrBelow (default) or Anywhere</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("labelPosition")]
    public string LabelPosition { get; set; }

    /// <summary>Only in this part of the capture, null: the whole capture</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("area")]
    public SmartTextArea Area { get; set; }

    /// <summary>Match the pattern and the label case-sensitively</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("matchCase")]
    public bool MatchCase { get; set; }

    /// <summary>
    /// In a word which is mostly digits, read the letters OCR confuses with digits as those digits (O as 0, l and I as 1, S as 5, B as 8)
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("digitLookAlikes")]
    public bool DigitLookAlikes { get; set; }

    /// <summary>The fewest digits the value has, 0: any</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("minDigits")]
    public int MinDigits { get; set; }

    /// <summary>The most digits the value has, 0: any</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("maxDigits")]
    public int MaxDigits { get; set; }

    /// <summary>What is done to the value, see <see cref="SmartTextNormalize"/>; null: nothing</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("normalize")]
    public string Normalize { get; set; }

    /// <summary>The check the value must pass, see <see cref="SmartTextValidate"/>; null: none</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("validate")]
    public string Validate { get; set; }

    /// <summary>
    /// A web address to open the value with, e.g. https://crm.example.com/customers/{value}: {value} is the value, encoded for a web
    /// address. Only http and https, only in pattern files (the user approves the file), not inline in a recipe.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("openUrl")]
    public string OpenUrl { get; set; }

    public SmartTextPattern Clone() => (SmartTextPattern)MemberwiseClone();

    public override string ToString() => $"{Name}: {Pattern}";
}

/// <summary>
/// A part of the capture, in fractions of its width and height (0 to 1), so it stays the same part when the window is larger
/// </summary>
public sealed class SmartTextArea
{
    [JsonPropertyName("left")]
    public double Left { get; set; }

    [JsonPropertyName("top")]
    public double Top { get; set; }

    [JsonPropertyName("width")]
    public double Width { get; set; } = 1;

    [JsonPropertyName("height")]
    public double Height { get; set; } = 1;

    public override string ToString() => $"{Left:0.##},{Top:0.##} {Width:0.##}x{Height:0.##}";
}

/// <summary>
/// Where the value is, seen from the label
/// </summary>
public static class SmartTextLabelPosition
{
    public const string Right = "Right";
    public const string Below = "Below";
    public const string RightOrBelow = "RightOrBelow";
    public const string Anywhere = "Anywhere";

    public static readonly IReadOnlyList<string> All = new[] { RightOrBelow, Right, Below, Anywhere };
}

/// <summary>
/// What is done to a found value before it is used
/// </summary>
public static class SmartTextNormalize
{
    public const string None = "None";

    /// <summary>Without spaces, dashes, dots and slashes</summary>
    public const string RemoveSeparators = "RemoveSeparators";

    public const string Upper = "Upper";

    public const string Lower = "Lower";

    /// <summary>Upper case, without spaces, dashes, dots and slashes (e.g. an IBAN)</summary>
    public const string UpperWithoutSeparators = "UpperWithoutSeparators";

    /// <summary>A web address: punctuation at its end dropped, https:// added to www.</summary>
    public const string Url = "Url";

    /// <summary>An e-mail address: the domain in lower case</summary>
    public const string Email = "Email";

    /// <summary>A phone number: only digits, + for 00 and international numbers, (0) after the country code dropped</summary>
    public const string Phone = "Phone";

    /// <summary>A date: as yyyy-MM-dd, a value which is no date is dropped</summary>
    public const string Date = "Date";

    public static readonly IReadOnlyList<string> All = new[] { None, RemoveSeparators, Upper, Lower, UpperWithoutSeparators, Url, Email, Phone, Date };
}

/// <summary>
/// The check a found value must pass, which keeps out what only looks like it
/// </summary>
public static class SmartTextValidate
{
    public const string None = "None";

    /// <summary>ISO 7064 mod 97-10, as an IBAN</summary>
    public const string Mod97 = "Mod97";

    /// <summary>The Luhn check digit, as a payment card number</summary>
    public const string Luhn = "Luhn";

    /// <summary>The check digit of an ISBN-10 or ISBN-13</summary>
    public const string Isbn = "Isbn";

    /// <summary>The check digit of an EAN-8, EAN-13 (GTIN)</summary>
    public const string Ean = "Ean";

    /// <summary>An IPv4 or IPv6 address</summary>
    public const string IpAddress = "IpAddress";

    /// <summary>A BIC: its country is a known one</summary>
    public const string Bic = "Bic";

    /// <summary>Latitude and longitude in their ranges</summary>
    public const string Coordinates = "Coordinates";

    /// <summary>Hexadecimal with at least one digit and one letter, as a hash</summary>
    public const string Hex = "Hex";

    /// <summary>The checks a pattern of the user can use</summary>
    public static readonly IReadOnlyList<string> All = new[] { None, Mod97, Luhn, Isbn, Ean };
}

/// <summary>
/// What the picker offers for a found value next to Copy
/// </summary>
public enum SmartTextAction
{
    None,
    /// <summary>Open the web address (http or https) in the browser</summary>
    Open,
    /// <summary>Write an e-mail (mailto:)</summary>
    Mail,
    /// <summary>Call the number (tel:, the phone app of Windows)</summary>
    Call,
    /// <summary>Open the web address of the pattern (<see cref="SmartTextPattern.OpenUrl"/>) with the value</summary>
    OpenUrl
}
