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
using System.Windows.Threading;
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
/// Add or change an own pattern, with a test text which shows what the pattern finds. The check and the test run a moment after
/// the last change, so typing stays fast.
/// </summary>
public sealed class SmartTextPatternViewModel : INotifyPropertyChanged
{
    /// <summary>The part of the capture "Custom": an area of a file which is none of the choices</summary>
    public const string AreaCustom = "Custom";
    public const string AreaEverywhere = "Everywhere";

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromMilliseconds(300);

    /// <summary>The parts of the capture to choose from, as fractions (left, top, width, height)</summary>
    private static readonly IReadOnlyList<(string Key, SmartTextArea Area)> Areas = new[]
    {
        ("TopHalf", NewArea(0, 0, 1, 0.5)),
        ("BottomHalf", NewArea(0, 0.5, 1, 0.5)),
        ("LeftHalf", NewArea(0, 0, 0.5, 1)),
        ("RightHalf", NewArea(0.5, 0, 0.5, 1)),
        ("TopLeft", NewArea(0, 0, 0.5, 0.5)),
        ("TopRight", NewArea(0.5, 0, 0.5, 0.5)),
        ("BottomLeft", NewArea(0, 0.5, 0.5, 0.5)),
        ("BottomRight", NewArea(0.5, 0.5, 0.5, 0.5))
    };

    private readonly SmartTextPattern _original;
    private readonly DispatcherTimer _recheck;
    private string _name;
    private string _pattern;
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
    private bool _showProblems;
    private string _problems = string.Empty;
    private string _testResult = string.Empty;

    public SmartTextPatternViewModel(SmartTextPattern definition, string originalId)
    {
        _original = definition?.Clone() ?? new SmartTextPattern();
        OriginalId = originalId;
        _name = _original.Name;
        _pattern = _original.Pattern;
        _label = _original.Label;
        _labelPosition = _original.LabelPosition ?? SmartTextLabelPosition.RightOrBelow;
        _area = AreaKeyOf(_original.Area);
        _matchCase = _original.MatchCase;
        _digitLookAlikes = _original.DigitLookAlikes;
        _minDigits = _original.MinDigits > 0 ? _original.MinDigits.ToString(CultureInfo.CurrentCulture) : string.Empty;
        _maxDigits = _original.MaxDigits > 0 ? _original.MaxDigits.ToString(CultureInfo.CurrentCulture) : string.Empty;
        _normalize = _original.Normalize ?? SmartTextNormalize.None;
        _validate = _original.Validate ?? SmartTextValidate.None;
        _openUrl = _original.OpenUrl;
        // A pattern which is changed shows what is wrong with it at once
        _showProblems = originalId != null;

        var areas = new List<SmartTextChoice> { new SmartTextChoice(AreaEverywhere, Language.AreaEverywhere) };
        areas.AddRange(Areas.Select(area => new SmartTextChoice(area.Key, AreaText(area.Key))));
        if (_area == AreaCustom) areas.Add(new SmartTextChoice(AreaCustom, string.Format(Language.AreaCustom, FormatArea(_original.Area))));
        AreaChoices = areas;

        _recheck = new DispatcherTimer { Interval = RecheckDelay };
        _recheck.Tick += (_, _) =>
        {
            _recheck.Stop();
            Recheck();
        };
        Recheck();
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

    public IReadOnlyList<SmartTextChoice> AreaChoices { get; }

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

    /// <summary>The key of the part of the capture, see <see cref="AreaChoices"/></summary>
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
    public string Problems => _problems;

    public bool HasProblems => _problems.Length > 0;

    /// <summary>The problems are shown once the user typed a pattern or tried to save, not for a new empty pattern</summary>
    public bool ShowProblems => _showProblems && HasProblems;

    /// <summary>What the pattern finds in the test text</summary>
    public string TestResult => _testResult;

    /// <summary>Check now (e.g. before Save) and show the problems from now on</summary>
    public void RevealProblems()
    {
        _recheck.Stop();
        _showProblems = true;
        Recheck();
    }

    /// <summary>
    /// Check the pattern and run it on the test text; raises the changes of the results
    /// </summary>
    private void Recheck()
    {
        var definition = ToDefinition();
        _problems = string.Join(" ", SmartTextPatternCheck.Problems(definition));
        _testResult = string.Empty;
        if (_problems.Length == 0 && !string.IsNullOrWhiteSpace(_testText))
        {
            try
            {
                definition.Area = null;
                var detector = new SmartTextDetector(definition.Name ?? "test", definition.Name ?? "test", definition, true);
                var items = SmartTextScanner.Scan(TextLayout.FromText(_testText), new[] { detector });
                _testResult = items.Count == 0
                    ? Language.EditorNoTestMatch
                    : string.Format(Language.EditorFound, items.Count, string.Join(", ", items.Select(item => item.Value)));
            }
            catch (ArgumentException ex)
            {
                _testResult = ex.Message;
            }
        }

        OnPropertyChanged(nameof(Problems));
        OnPropertyChanged(nameof(HasProblems));
        OnPropertyChanged(nameof(ShowProblems));
        OnPropertyChanged(nameof(TestResult));
    }

    /// <summary>The pattern as it is written into the file</summary>
    public SmartTextPattern ToDefinition()
    {
        var definition = _original.Clone();
        definition.Name = _name?.Trim();
        definition.Pattern = _pattern;
        definition.Label = Empty(_label);
        definition.LabelPosition = definition.Label == null || _labelPosition == SmartTextLabelPosition.RightOrBelow ? null : _labelPosition;
        definition.Area = _area == AreaCustom ? _original.Area : Areas.FirstOrDefault(area => area.Key == _area).Area;
        definition.MatchCase = _matchCase;
        definition.DigitLookAlikes = _digitLookAlikes;
        definition.MinDigits = Number(_minDigits);
        definition.MaxDigits = Number(_maxDigits);
        definition.Normalize = _normalize == SmartTextNormalize.None ? null : _normalize;
        definition.Validate = _validate == SmartTextValidate.None ? null : _validate;
        definition.OpenUrl = Empty(_openUrl);
        return definition;
    }

    private static SmartTextArea NewArea(double left, double top, double width, double height) =>
        new SmartTextArea { Left = left, Top = top, Width = width, Height = height };

    /// <summary>The key of the area: one of the choices, Everywhere, or Custom for another one</summary>
    internal static string AreaKeyOf(SmartTextArea area)
    {
        if (area == null) return AreaEverywhere;
        foreach (var (key, known) in Areas)
        {
            if (Math.Abs(known.Left - area.Left) < 0.001 && Math.Abs(known.Top - area.Top) < 0.001 &&
                Math.Abs(known.Width - area.Width) < 0.001 && Math.Abs(known.Height - area.Height) < 0.001)
            {
                return key;
            }
        }

        bool everywhere = area.Left <= 0.001 && area.Top <= 0.001 && area.Width >= 0.999 && area.Height >= 0.999;
        return everywhere ? AreaEverywhere : AreaCustom;
    }

    private static string AreaText(string key) => key switch
    {
        "TopHalf" => Language.AreaTopHalf,
        "BottomHalf" => Language.AreaBottomHalf,
        "LeftHalf" => Language.AreaLeftHalf,
        "RightHalf" => Language.AreaRightHalf,
        "TopLeft" => Language.AreaTopLeft,
        "TopRight" => Language.AreaTopRight,
        "BottomLeft" => Language.AreaBottomLeft,
        "BottomRight" => Language.AreaBottomRight,
        _ => key
    };

    private static string Empty(string text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static int Number(string text) =>
        int.TryParse(text?.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out int number) && number > 0 ? number : 0;

    internal static string FormatArea(SmartTextArea area) =>
        area == null
            ? string.Empty
            : string.Format(CultureInfo.CurrentCulture, "{0:0.##}, {1:0.##}, {2:0.##}, {3:0.##}", area.Left, area.Top, area.Width, area.Height);

    private void Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        if (propertyName == nameof(Pattern)) _showProblems = true;
        OnPropertyChanged(propertyName);
        // Check after a moment without typing
        _recheck.Stop();
        _recheck.Start();
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
