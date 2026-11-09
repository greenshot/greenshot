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
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Greenshot.Plugin.SmartText.Detection;

namespace Greenshot.Plugin.SmartText.ViewModels;

/// <summary>
/// A choice of a combo box: the value written into the file and its text
/// </summary>
public sealed class SmartTextChoice
{
    public SmartTextChoice(string value, string text)
    {
        Value = value;
        Text = text;
    }

    public string Value { get; }

    public string Text { get; }

    public override string ToString() => Text;
}

/// <summary>
/// Add or change an own pattern, with a test text which shows at once what the pattern finds
/// </summary>
public sealed class SmartTextPatternViewModel : INotifyPropertyChanged
{
    private readonly SmartTextPattern _original;
    private string _name;
    private string _pattern;
    private string _valueGroup;
    private string _label;
    private string _labelPosition;
    private string _area;
    private bool _matchCase;
    private bool _digitLookAlikes;
    private string _minDigits;
    private string _maxDigits;
    private string _normalize;
    private string _validate;
    private string _openUrl;
    private string _testText = string.Empty;

    public SmartTextPatternViewModel(SmartTextPattern definition, string originalId)
    {
        _original = definition?.Clone() ?? new SmartTextPattern();
        OriginalId = originalId;
        _name = _original.Name;
        _pattern = _original.Pattern;
        _valueGroup = _original.ValueGroup;
        _label = _original.Label;
        _labelPosition = _original.LabelPosition ?? SmartTextLabelPosition.RightOrBelow;
        _area = _original.Area == null ? string.Empty : FormatArea(_original.Area);
        _matchCase = _original.MatchCase;
        _digitLookAlikes = _original.DigitLookAlikes;
        _minDigits = _original.MinDigits > 0 ? _original.MinDigits.ToString(CultureInfo.CurrentCulture) : string.Empty;
        _maxDigits = _original.MaxDigits > 0 ? _original.MaxDigits.ToString(CultureInfo.CurrentCulture) : string.Empty;
        _normalize = _original.Normalize ?? SmartTextNormalize.None;
        _validate = _original.Validate ?? SmartTextValidate.None;
        _openUrl = _original.OpenUrl;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private static ISmartTextLanguage Language => SmartTextPlugin.Language;

    /// <summary>The id of the changed pattern, null for a new one</summary>
    public string OriginalId { get; }

    public string Title => Language.EditorTitle;

    public IReadOnlyList<SmartTextChoice> LabelPositions { get; } = new[]
    {
        new SmartTextChoice(SmartTextLabelPosition.RightOrBelow, Language.PositionRightOrBelow),
        new SmartTextChoice(SmartTextLabelPosition.Right, Language.PositionRight),
        new SmartTextChoice(SmartTextLabelPosition.Below, Language.PositionBelow),
        new SmartTextChoice(SmartTextLabelPosition.Anywhere, Language.PositionAnywhere)
    };

    public IReadOnlyList<SmartTextChoice> NormalizeChoices { get; } = new[]
    {
        new SmartTextChoice(SmartTextNormalize.None, Language.NormalizeNone),
        new SmartTextChoice(SmartTextNormalize.RemoveSeparators, Language.NormalizeRemoveSeparators),
        new SmartTextChoice(SmartTextNormalize.Upper, Language.NormalizeUpper),
        new SmartTextChoice(SmartTextNormalize.Lower, Language.NormalizeLower),
        new SmartTextChoice(SmartTextNormalize.UpperWithoutSeparators, Language.NormalizeUpperWithoutSeparators)
    };

    public IReadOnlyList<SmartTextChoice> ValidateChoices { get; } = new[]
    {
        new SmartTextChoice(SmartTextValidate.None, Language.ValidateNone),
        new SmartTextChoice(SmartTextValidate.Mod97, Language.ValidateMod97),
        new SmartTextChoice(SmartTextValidate.Luhn, Language.ValidateLuhn),
        new SmartTextChoice(SmartTextValidate.Isbn, Language.ValidateIsbn),
        new SmartTextChoice(SmartTextValidate.Ean, Language.ValidateEan)
    };

    public string Name { get => _name; set => Set(ref _name, value); }
    public string Pattern { get => _pattern; set => Set(ref _pattern, value); }
    public string ValueGroup { get => _valueGroup; set => Set(ref _valueGroup, value); }

    public string Label
    {
        get => _label;
        set
        {
            Set(ref _label, value);
            OnPropertyChanged(nameof(HasLabel));
        }
    }

    public bool HasLabel => !string.IsNullOrWhiteSpace(_label);
    public string LabelPosition { get => _labelPosition; set => Set(ref _labelPosition, value); }
    public string Area { get => _area; set => Set(ref _area, value); }
    public bool MatchCase { get => _matchCase; set => Set(ref _matchCase, value); }
    public bool DigitLookAlikes { get => _digitLookAlikes; set => Set(ref _digitLookAlikes, value); }
    public string MinDigits { get => _minDigits; set => Set(ref _minDigits, value); }
    public string MaxDigits { get => _maxDigits; set => Set(ref _maxDigits, value); }
    public string Normalize { get => _normalize; set => Set(ref _normalize, value); }
    public string Validate { get => _validate; set => Set(ref _validate, value); }
    public string OpenUrl { get => _openUrl; set => Set(ref _openUrl, value); }
    public string TestText { get => _testText; set => Set(ref _testText, value); }

    /// <summary>What is wrong with the pattern, empty when it can be saved</summary>
    public string Problems
    {
        get
        {
            var problems = new List<string>(SmartTextPatternCheck.Problems(ToDefinition()));
            if (!string.IsNullOrWhiteSpace(_area) && ParseArea(_area) == null) problems.Add(Language.EditorArea);
            return string.Join(" ", problems);
        }
    }

    public bool HasProblems => Problems.Length > 0;

    /// <summary>What the pattern finds in the test text</summary>
    public string TestResult
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_testText) || HasProblems) return string.Empty;
            try
            {
                var definition = ToDefinition();
                definition.Area = null;
                var detector = new SmartTextDetector(definition.Name ?? "test", definition.Name ?? "test", definition, true);
                var items = SmartTextScanner.Scan(TextLayout.FromText(_testText), new[] { detector });
                return items.Count == 0
                    ? Language.EditorNoTestMatch
                    : string.Format(Language.EditorFound, items.Count, string.Join(", ", items.Select(item => item.Value)));
            }
            catch (ArgumentException ex)
            {
                return ex.Message;
            }
        }
    }

    /// <summary>The pattern as it is written into the file</summary>
    public SmartTextPattern ToDefinition()
    {
        var definition = _original.Clone();
        definition.Name = _name?.Trim();
        definition.Pattern = _pattern;
        definition.ValueGroup = Empty(_valueGroup);
        definition.Label = Empty(_label);
        definition.LabelPosition = definition.Label == null || _labelPosition == SmartTextLabelPosition.RightOrBelow ? null : _labelPosition;
        definition.Area = ParseArea(_area);
        definition.MatchCase = _matchCase;
        definition.DigitLookAlikes = _digitLookAlikes;
        definition.MinDigits = Number(_minDigits);
        definition.MaxDigits = Number(_maxDigits);
        definition.Normalize = _normalize == SmartTextNormalize.None ? null : _normalize;
        definition.Validate = _validate == SmartTextValidate.None ? null : _validate;
        definition.OpenUrl = Empty(_openUrl);
        return definition;
    }

    private static string Empty(string text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static int Number(string text) =>
        int.TryParse(text?.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out int number) && number > 0 ? number : 0;

    internal static string FormatArea(SmartTextArea area) =>
        string.Join("; ", new[] { area.Left, area.Top, area.Width, area.Height }.Select(value => value.ToString("0.###", CultureInfo.CurrentCulture)));

    /// <summary>"left; top; width; height" as fractions from 0 to 1 (also with commas when the decimal separator is a dot), null when empty or wrong</summary>
    internal static SmartTextArea ParseArea(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var culture = CultureInfo.CurrentCulture;
        char[] separators = culture.NumberFormat.NumberDecimalSeparator == "," ? new[] { ';', ' ' } : new[] { ';', ',', ' ' };
        var parts = text.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4) return null;
        var values = new double[4];
        for (int i = 0; i < 4; i++)
        {
            if (!double.TryParse(parts[i], NumberStyles.Float, culture, out values[i]) || values[i] < 0 || values[i] > 1) return null;
        }

        if (values[2] <= 0 || values[3] <= 0) return null;
        return new SmartTextArea { Left = values[0], Top = values[1], Width = values[2], Height = values[3] };
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnPropertyChanged(propertyName);
        OnPropertyChanged(nameof(Problems));
        OnPropertyChanged(nameof(HasProblems));
        OnPropertyChanged(nameof(TestResult));
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
