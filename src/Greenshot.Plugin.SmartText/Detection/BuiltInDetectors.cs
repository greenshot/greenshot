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
using System.Globalization;
using System.Linq;

namespace Greenshot.Plugin.SmartText.Detection;

/// <summary>
/// A detector which comes with Greenshot: its id (used in recipes and greenshot.ini), whether it is on without a setting, what it finds
/// </summary>
public sealed class BuiltInDetector
{
    public BuiltInDetector(string id, bool isOnByDefault, SmartTextPattern pattern)
    {
        Id = id;
        IsOnByDefault = isOnByDefault;
        Pattern = pattern;
        Pattern.Name ??= id;
    }

    /// <summary>The id, e.g. Phone; its name in the language packs is name_&lt;id&gt; of [SmartText]</summary>
    public string Id { get; }

    public bool IsOnByDefault { get; }

    public SmartTextPattern Pattern { get; }
}

/// <summary>
/// The detectors which come with Greenshot. On by default: those with few false hits (web, e-mail, phone, IBAN, date, issue key).
/// </summary>
public static class BuiltInDetectors
{
    public const string Url = "Url";
    public const string Email = "Email";
    public const string Phone = "Phone";
    public const string Iban = "Iban";
    public const string Date = "Date";
    public const string IssueKey = "IssueKey";
    public const string IpAddress = "IpAddress";
    public const string MacAddress = "MacAddress";
    public const string Guid = "Guid";
    public const string Hash = "Hash";
    public const string ErrorCode = "ErrorCode";
    public const string Path = "Path";
    public const string Money = "Money";
    public const string VatId = "VatId";
    public const string Bic = "Bic";
    public const string Isbn = "Isbn";
    public const string Ean = "Ean";
    public const string CardNumber = "CardNumber";
    public const string Coordinates = "Coordinates";

    /// <summary>The top level domains a domain without https:// or a path is taken with; others would find file names</summary>
    private const string CommonTopLevelDomains = "com|org|net|edu|gov|info|biz|io|dev|app|eu|de|at|ch|nl|be|fr|it|es|uk|us|ca|au|jp|cn|ru|pl|se|no|dk|fi|cz";

    /// <summary>
    /// All built-in detectors, the ones on by default first. The date pattern has the month names of the culture.
    /// </summary>
    public static IReadOnlyList<BuiltInDetector> Create(CultureInfo culture = null)
    {
        string months = SmartTextNormalizers.MonthNames(culture ?? CultureInfo.CurrentCulture);
        return new List<BuiltInDetector>
        {
            new BuiltInDetector(Url, true, new SmartTextPattern
            {
                // With a scheme or www., a domain with a path, or a domain of a common top level domain (getgreenshot.org);
                // not the domain of an e-mail address, not a file name like readme.txt
                Pattern = @"(?<![@\w.:/-])(?:https?://[^\s<>""'`]+|www\.[a-z0-9-]+(?:\.[a-z0-9-]+)+[^\s<>""'`]*|[a-z0-9-]+(?:\.[a-z0-9-]+)*\.[a-z]{2,24}/[^\s<>""'`]*|" +
                          @"[a-z0-9-]+(?:\.[a-z0-9-]+)*\.(?:" + CommonTopLevelDomains + @")(?![\w@-]|\.\w))",
                Normalize = SmartTextNormalize.Url
            }),
            new BuiltInDetector(Email, true, new SmartTextPattern
            {
                Pattern = @"(?<![\w.+-])[a-z0-9._%+-]+@[a-z0-9-]+(?:\.[a-z0-9-]+)*\.[a-z]{2,24}\b",
                Normalize = SmartTextNormalize.Email
            }),
            new BuiltInDetector(Phone, true, new SmartTextPattern
            {
                // +49 30 1234567, 0049 (0)30 123 45-67, (030) 1234567, 555-123-4567; no dots (IP addresses, versions, dates)
                Pattern = @"(?<![\w+.,/-])(?!\d{4}-\d{1,2}-\d{1,2}(?!\d))(?:\+|00)?(?:\(\s*\d{1,5}\s*\)[ \-/]?)?\d[\d \-/()]{4,22}\d(?![\w.,]\d|[\w/-])",
                Normalize = SmartTextNormalize.Phone,
                MinDigits = 7,
                MaxDigits = 15
            }),
            new BuiltInDetector(Iban, true, new SmartTextPattern
            {
                Pattern = @"\b[A-Z]{2}\d{2}(?: ?[A-Z0-9]){11,30}\b",
                MatchCase = true,
                Normalize = SmartTextNormalize.UpperWithoutSeparators,
                Validate = SmartTextValidate.Mod97
            }),
            new BuiltInDetector(Date, true, new SmartTextPattern
            {
                Pattern = @"\b(?:\d{4}-\d{1,2}-\d{1,2}|\d{1,2}\.\d{1,2}\.(?:\d{4}|\d{2})|\d{1,2}/\d{1,2}/(?:\d{4}|\d{2})|\d{1,2}\.? (?:" + months + @")\.? \d{4}|(?:" + months + @")\.? \d{1,2},? \d{4})\b",
                Normalize = SmartTextNormalize.Date
            }),
            new BuiltInDetector(IssueKey, true, new SmartTextPattern
            {
                // Jira and the like: PROJ-123; not the names of encodings, standards and algorithms (UTF-8, ISO-8601, SHA-256)
                Pattern = @"(?<![\w-])(?!(?:UTF|UCS|ISO|SHA|AES|RSA|MD|RFC|DIN|EN|IEC|IEEE)-)[A-Z][A-Z0-9]{1,9}-\d{1,7}(?![\w-])",
                MatchCase = true
            }),
            new BuiltInDetector(IpAddress, false, new SmartTextPattern
            {
                Pattern = @"(?<![\w.:])(?:(?:\d{1,3}\.){3}\d{1,3}|(?:[0-9a-f]{1,4}:){7}[0-9a-f]{1,4}|(?:[0-9a-f]{1,4}:){1,7}:(?:[0-9a-f]{1,4}(?::[0-9a-f]{1,4}){0,6})?)(?![\w.:])",
                Validate = SmartTextValidate.IpAddress
            }),
            new BuiltInDetector(MacAddress, false, new SmartTextPattern
            {
                Pattern = @"(?<![\w:-])[0-9a-f]{2}(?:([:-])[0-9a-f]{2})(?:\1[0-9a-f]{2}){4}(?![\w:-])",
                Normalize = SmartTextNormalize.Upper
            }),
            new BuiltInDetector(Guid, false, new SmartTextPattern
            {
                Pattern = @"\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b",
                Normalize = SmartTextNormalize.Lower
            }),
            new BuiltInDetector(Hash, false, new SmartTextPattern
            {
                // A git commit (7 to 40) or a SHA-256 (64)
                Pattern = @"\b(?:[0-9a-f]{64}|[0-9a-f]{7,40})\b",
                Normalize = SmartTextNormalize.Lower,
                Validate = SmartTextValidate.Hex
            }),
            new BuiltInDetector(ErrorCode, false, new SmartTextPattern
            {
                Pattern = @"\b0x[0-9a-f]{8}\b"
            }),
            new BuiltInDetector(Path, false, new SmartTextPattern
            {
                // C:\folder\file.txt or \\server\share\folder; only copied, never opened
                Pattern = @"(?<![\w\\])(?:[a-z]:\\|\\\\[\w.$-]+\\)[^\s<>""|?*:]*"
            }),
            new BuiltInDetector(Money, false, new SmartTextPattern
            {
                Pattern = @"(?:[€$£¥]\s?\d{1,3}(?:[.,' ]\d{3})*(?:[.,]\d{1,2})?|\b\d{1,3}(?:[.,' ]\d{3})*(?:[.,]\d{1,2})?\s?(?:€|EUR|USD|GBP|CHF|JPY|\$|£))(?!\w)"
            }),
            new BuiltInDetector(VatId, false, new SmartTextPattern
            {
                Pattern = @"\b(?:ATU|BE|BG|CHE|CY|CZ|DE|DK|EE|EL|ES|FI|FR|GB|HR|HU|IE|IT|LT|LU|LV|MT|NL|PL|PT|RO|SE|SI|SK) ?[0-9A-Z][0-9A-Z .-]{6,14}[0-9A-Z]\b",
                MatchCase = true,
                Normalize = SmartTextNormalize.UpperWithoutSeparators,
                MinDigits = 7
            }),
            new BuiltInDetector(Bic, false, new SmartTextPattern
            {
                Pattern = @"\b[A-Z]{4}[A-Z]{2}[A-Z0-9]{2}(?:[A-Z0-9]{3})?\b",
                MatchCase = true,
                Validate = SmartTextValidate.Bic
            }),
            new BuiltInDetector(Isbn, false, new SmartTextPattern
            {
                Pattern = @"(?<![\w-])(?:97[89][- ]?)?\d{1,5}[- ]?\d{1,7}[- ]?\d{1,7}[- ]?[\dX](?![\w-])",
                MatchCase = true,
                Normalize = SmartTextNormalize.RemoveSeparators,
                Validate = SmartTextValidate.Isbn
            }),
            new BuiltInDetector(Ean, false, new SmartTextPattern
            {
                Pattern = @"(?<!\d)(?:\d{13}|\d{8})(?!\d)",
                Validate = SmartTextValidate.Ean
            }),
            new BuiltInDetector(CardNumber, false, new SmartTextPattern
            {
                Pattern = @"(?<!\d)\d{4}(?:[ -]?\d{4}){2}(?:[ -]?\d{1,7})(?!\d)",
                Normalize = SmartTextNormalize.RemoveSeparators,
                Validate = SmartTextValidate.Luhn,
                MinDigits = 13,
                MaxDigits = 19
            }),
            new BuiltInDetector(Coordinates, false, new SmartTextPattern
            {
                Pattern = @"(?<![\w.])-?\d{1,2}\.\d{3,}\s*,\s*-?\d{1,3}\.\d{3,}(?![\w.])",
                Validate = SmartTextValidate.Coordinates
            })
        };
    }

    /// <summary>
    /// The built-in detector with the id (ignoring case), null when there is none
    /// </summary>
    public static BuiltInDetector Find(IEnumerable<BuiltInDetector> builtIns, string id) =>
        builtIns.FirstOrDefault(builtIn => string.Equals(builtIn.Id, id, System.StringComparison.OrdinalIgnoreCase));
}
