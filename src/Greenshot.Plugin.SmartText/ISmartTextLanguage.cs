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


using System.ComponentModel;
using Dapplo.Ini.Internationalization.Attributes;

namespace Greenshot.Plugin.SmartText
{
    /// <summary>
    /// The texts of the [SmartText] section of greenshot.smarttext.{ietf}.ini, the English text is shown per property.
    /// Add a key to every language file and a property here.
    /// </summary>
    [IniLanguageSection("SmartText", ModuleName = "smarttext")]
    public interface ISmartTextLanguage : INotifyPropertyChanged
    {
        /// <summary>
        /// Call
        /// </summary>
        string ActionCall { get; }

        /// <summary>
        /// Write e-mail
        /// </summary>
        string ActionMail { get; }

        /// <summary>
        /// Open
        /// </summary>
        string ActionOpen { get; }

        /// <summary>
        /// Approve
        /// </summary>
        string ApproveButton { get; }

        /// <summary>
        /// Not now
        /// </summary>
        string ApproveLater { get; }

        /// <summary>
        /// Approve a pattern
        /// </summary>
        string ApproveTitle { get; }

        /// <summary>
        /// Bottom half
        /// </summary>
        string AreaBottomHalf { get; }

        /// <summary>
        /// Bottom left quarter
        /// </summary>
        string AreaBottomLeft { get; }

        /// <summary>
        /// Bottom right quarter
        /// </summary>
        string AreaBottomRight { get; }

        /// <summary>
        /// Custom: {0}
        /// </summary>
        string AreaCustom { get; }

        /// <summary>
        /// Everywhere
        /// </summary>
        string AreaEverywhere { get; }

        /// <summary>
        /// Left half
        /// </summary>
        string AreaLeftHalf { get; }

        /// <summary>
        /// Right half
        /// </summary>
        string AreaRightHalf { get; }

        /// <summary>
        /// Top half
        /// </summary>
        string AreaTopHalf { get; }

        /// <summary>
        /// Top left quarter
        /// </summary>
        string AreaTopLeft { get; }

        /// <summary>
        /// Top right quarter
        /// </summary>
        string AreaTopRight { get; }

        /// <summary>
        /// Close
        /// </summary>
        string Close { get; }

        /// <summary>
        /// Copied to the clipboard.
        /// </summary>
        string Copied { get; }

        /// <summary>
        /// Copy
        /// </summary>
        string Copy { get; }

        /// <summary>
        /// Copy all text
        /// </summary>
        string CopyAllText { get; }

        /// <summary>
        /// Copy selected ({0})
        /// </summary>
        string CopySelected { get; }

        /// <summary>
        /// File
        /// </summary>
        string DetailFile { get; }

        /// <summary>
        /// Label
        /// </summary>
        string DetailLabel { get; }

        /// <summary>
        /// Opens
        /// </summary>
        string DetailOpenUrl { get; }

        /// <summary>
        /// Pattern
        /// </summary>
        string DetailPattern { get; }

        /// <summary>
        /// A pattern or a built-in detector already has this name.
        /// </summary>
        string DuplicateName { get; }

        /// <summary>
        /// Enter takes the value, Space checks it, F2 corrects it, Ctrl+O opens it, Ctrl+A checks all.
        /// </summary>
        string EditHint { get; }

        /// <summary>
        /// Where to look in the capture
        /// </summary>
        string EditorArea { get; }

        /// <summary>
        /// In numbers, read O as 0 and l as 1
        /// </summary>
        string EditorDigitLookAlikes { get; }

        /// <summary>
        /// The pattern can't be saved: {0}
        /// </summary>
        string EditorFailed { get; }

        /// <summary>
        /// {0} found: {1}
        /// </summary>
        string EditorFound { get; }

        /// <summary>
        /// Label (regular expression; empty: no label)
        /// </summary>
        string EditorLabel { get; }

        /// <summary>
        /// Where the value is
        /// </summary>
        string EditorLabelPosition { get; }

        /// <summary>
        /// Match case
        /// </summary>
        string EditorMatchCase { get; }

        /// <summary>
        /// The value has at most this many digits (empty: any)
        /// </summary>
        string EditorMaxDigits { get; }

        /// <summary>
        /// The value has at least this many digits (empty: any)
        /// </summary>
        string EditorMinDigits { get; }

        /// <summary>
        /// Name
        /// </summary>
        string EditorName { get; }

        /// <summary>
        /// Nothing found in the test text.
        /// </summary>
        string EditorNoTestMatch { get; }

        /// <summary>
        /// Change the value
        /// </summary>
        string EditorNormalize { get; }

        /// <summary>
        /// Open with (a web address with {value})
        /// </summary>
        string EditorOpenUrl { get; }

        /// <summary>
        /// Pattern (regular expression)
        /// </summary>
        string EditorPattern { get; }

        /// <summary>
        /// Tip: with a label you only need the value itself, e.g. \d{8}. To take only a part of the match, name that part value: Order (?&lt;value&gt;\d+)
        /// </summary>
        string EditorPatternHint { get; }

        /// <summary>
        /// Test text
        /// </summary>
        string EditorTest { get; }

        /// <summary>
        /// Smart text pattern
        /// </summary>
        string EditorTitle { get; }

        /// <summary>
        /// Check the value
        /// </summary>
        string EditorValidate { get; }

        /// <summary>
        /// The file is too large.
        /// </summary>
        string ErrorFileTooLarge { get; }

        /// <summary>
        /// This pattern is for a newer Greenshot (version {0}).
        /// </summary>
        string ErrorNewerVersion { get; }

        /// <summary>
        /// This is not a Smart text pattern.
        /// </summary>
        string ErrorNotAPattern { get; }

        /// <summary>
        /// The pattern can't be exported: {0}
        /// </summary>
        string ExportFailed { get; }

        /// <summary>
        /// All ({0})
        /// </summary>
        string FilterAll { get; }

        /// <summary>
        /// {0} ({1})
        /// </summary>
        string FilterKind { get; }

        /// <summary>
        /// Import
        /// </summary>
        string ImportButton { get; }

        /// <summary>
        /// {0} can't be imported: {1}
        /// </summary>
        string ImportFailed { get; }

        /// <summary>
        /// Smart text patterns (*.gssmarttext.json)|*.gssmarttext.json
        /// </summary>
        string ImportFileFilter { get; }

        /// <summary>
        /// It replaces the pattern with the same name.
        /// </summary>
        string ImportReplaces { get; }

        /// <summary>
        /// Import a pattern
        /// </summary>
        string ImportTitle { get; }

        /// <summary>
        /// Only import patterns from people you trust: a pattern with a web address sends the values it finds there.
        /// </summary>
        string ImportTrust { get; }

        /// <summary>
        /// {0}: {1}, {2} of {3}
        /// </summary>
        string ItemName { get; }

        /// <summary>
        /// {0} for "{1}": the kind of item and the text it was found under, when that isn't the value itself (a link "FAQ" with its address)
        /// </summary>
        string ItemFoundUnder { get; }

        /// <summary>
        /// {0} for "{4}": {1}, {2} of {3}
        /// </summary>
        string ItemNameFoundUnder { get; }

        /// <summary>
        /// Smart text pattern
        /// </summary>
        string KindPattern { get; }

        /// <summary>
        /// Built-in detectors
        /// </summary>
        string LabelBuiltIn { get; }

        /// <summary>
        /// Own patterns
        /// </summary>
        string LabelOwn { get; }

        /// <summary>
        /// Found items
        /// </summary>
        string ListName { get; }

        /// <summary>
        /// BIC
        /// </summary>
        string NameBic { get; }

        /// <summary>
        /// Card number
        /// </summary>
        string NameCardNumber { get; }

        /// <summary>
        /// Coordinates
        /// </summary>
        string NameCoordinates { get; }

        /// <summary>
        /// Date
        /// </summary>
        string NameDate { get; }

        /// <summary>
        /// EAN
        /// </summary>
        string NameEan { get; }

        /// <summary>
        /// E-mail address
        /// </summary>
        string NameEmail { get; }

        /// <summary>
        /// Error code
        /// </summary>
        string NameErrorCode { get; }

        /// <summary>
        /// GUID
        /// </summary>
        string NameGuid { get; }

        /// <summary>
        /// Hash
        /// </summary>
        string NameHash { get; }

        /// <summary>
        /// IBAN
        /// </summary>
        string NameIban { get; }

        /// <summary>
        /// IP address
        /// </summary>
        string NameIpAddress { get; }

        /// <summary>
        /// ISBN
        /// </summary>
        string NameIsbn { get; }

        /// <summary>
        /// Issue key
        /// </summary>
        string NameIssueKey { get; }

        /// <summary>
        /// MAC address
        /// </summary>
        string NameMacAddress { get; }

        /// <summary>
        /// Amount of money
        /// </summary>
        string NameMoney { get; }

        /// <summary>
        /// File path
        /// </summary>
        string NamePath { get; }

        /// <summary>
        /// Phone number
        /// </summary>
        string NamePhone { get; }

        /// <summary>
        /// Web address
        /// </summary>
        string NameUrl { get; }

        /// <summary>
        /// VAT ID
        /// </summary>
        string NameVatId { get; }

        /// <summary>
        /// No smart text was found in the capture.
        /// </summary>
        string NoItems { get; }

        /// <summary>
        /// There are no own patterns yet.
        /// </summary>
        string NoOwn { get; }

        /// <summary>
        /// OCR found no text in the capture. Is a Windows OCR language installed?
        /// </summary>
        string NoText { get; }

        /// <summary>
        /// Lower case
        /// </summary>
        string NormalizeLower { get; }

        /// <summary>
        /// Keep as read
        /// </summary>
        string NormalizeNone { get; }

        /// <summary>
        /// Remove spaces, dashes and dots
        /// </summary>
        string NormalizeRemoveSeparators { get; }

        /// <summary>
        /// Upper case
        /// </summary>
        string NormalizeUpper { get; }

        /// <summary>
        /// Upper case without spaces, dashes and dots
        /// </summary>
        string NormalizeUpperWithoutSeparators { get; }

        /// <summary>
        /// Not approved: {0}
        /// </summary>
        string NotApproved { get; }

        /// <summary>
        /// Find {0}
        /// </summary>
        string Offer { get; }

        /// <summary>
        /// Anywhere, when the label is in the capture
        /// </summary>
        string PositionAnywhere { get; }

        /// <summary>
        /// Below the label
        /// </summary>
        string PositionBelow { get; }

        /// <summary>
        /// Right of the label
        /// </summary>
        string PositionRight { get; }

        /// <summary>
        /// Right of the label, else below it
        /// </summary>
        string PositionRightOrBelow { get; }

        /// <summary>
        /// The capture with the found items
        /// </summary>
        string PreviewName { get; }

        /// <summary>
        /// This pattern was changed outside Greenshot. Do you want to use it?
        /// </summary>
        string QuestionApprove { get; }

        /// <summary>
        /// Do you want to import this pattern?
        /// </summary>
        string QuestionImport { get; }

        /// <summary>
        /// Select a region, then pick the web addresses, phone numbers and more in its text
        /// </summary>
        string RecipeDescription { get; }

        /// <summary>
        /// Capture smart text
        /// </summary>
        string RecipeName { get; }

        /// <summary>
        /// Do you want to remove the pattern {0}?
        /// </summary>
        string RemoveQuestion { get; }

        /// <summary>
        /// Save
        /// </summary>
        string Save { get; }

        /// <summary>
        /// Approve...
        /// </summary>
        string SettingsApprove { get; }

        /// <summary>
        /// Remove
        /// </summary>
        string SettingsDelete { get; }

        /// <summary>
        /// Smart text finds data in the text of a capture: web addresses, phone numbers, your own patterns and more. Pick it in the Smart text window, or use it in a recipe.
        /// </summary>
        string SettingsDescription { get; }

        /// <summary>
        /// Edit...
        /// </summary>
        string SettingsEdit { get; }

        /// <summary>
        /// Export...
        /// </summary>
        string SettingsExport { get; }

        /// <summary>
        /// Import...
        /// </summary>
        string SettingsImport { get; }

        /// <summary>
        /// New...
        /// </summary>
        string SettingsNew { get; }

        /// <summary>
        /// (can't be read)
        /// </summary>
        string Unreadable { get; }

        /// <summary>
        /// Use
        /// </summary>
        string Use { get; }

        /// <summary>
        /// Use selected ({0})
        /// </summary>
        string UseSelected { get; }

        /// <summary>
        /// EAN check digit
        /// </summary>
        string ValidateEan { get; }

        /// <summary>
        /// ISBN check digit
        /// </summary>
        string ValidateIsbn { get; }

        /// <summary>
        /// Check digit like a card number (Luhn)
        /// </summary>
        string ValidateLuhn { get; }

        /// <summary>
        /// Check digits like an IBAN (mod 97)
        /// </summary>
        string ValidateMod97 { get; }

        /// <summary>
        /// No check
        /// </summary>
        string ValidateNone { get; }

        /// <summary>
        /// Value
        /// </summary>
        string ValueLabel { get; }

        /// <summary>
        /// Smart text
        /// </summary>
        string WindowTitle { get; }
    }
}
