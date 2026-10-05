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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Greenshot.Base.Languages;
using Greenshot.Plugin.Pdf.Configuration;

namespace Greenshot.Plugin.Pdf.ViewModels;

public sealed class PdfConfigurationViewModel : INotifyPropertyChanged
{
    private readonly IPdfConfiguration _configuration;

    public PdfConfigurationViewModel(IPdfConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public ObservableCollection<LocalizedOption> MeasurementUnits { get; } = new();

    public ObservableCollection<LocalizedOption> PageSizes { get; } = new();

    public ObservableCollection<LocalizedOption> ScalingModes { get; } = new();

    public string PageSize
    {
        get => _configuration.PageSize;
        set
        {
            if (string.Equals(_configuration.PageSize, value, StringComparison.Ordinal))
            {
                return;
            }

            _configuration.PageSize = value;
            OnPropertyChanged(nameof(PageSize));
            OnPropertyChanged(nameof(PageWidth));
            OnPropertyChanged(nameof(PageHeight));
            OnPropertyChanged(nameof(IsPageDimensionsEnabled));
            OnPropertyChanged(nameof(IsFixedPage));
        }
    }

    public string MeasurementUnit
    {
        get => _configuration.MeasurementUnit;
        set
        {
            if (string.Equals(_configuration.MeasurementUnit, value, StringComparison.Ordinal))
            {
                return;
            }

            _configuration.MeasurementUnit = value;
            OnPropertyChanged(nameof(MeasurementUnit));
            OnPropertyChanged(nameof(PageWidth));
            OnPropertyChanged(nameof(PageHeight));
            OnPropertyChanged(nameof(MarginTop));
            OnPropertyChanged(nameof(MarginBottom));
            OnPropertyChanged(nameof(MarginLeft));
            OnPropertyChanged(nameof(MarginRight));
        }
    }

    public double PageWidth
    {
        get => FromMillimeters(PdfPageSizes.IsPreset(PageSize) ? PdfPageSizes.ForPreset(PageSize).WidthMm : _configuration.PageWidthMm);
        set => SetPageDimension(value, true);
    }

    public double PageHeight
    {
        get => FromMillimeters(PdfPageSizes.IsPreset(PageSize) ? PdfPageSizes.ForPreset(PageSize).HeightMm : _configuration.PageHeightMm);
        set => SetPageDimension(value, false);
    }

    public double MarginTop
    {
        get => FromMillimeters(_configuration.MarginTopMm);
        set => SetMargin(value, nameof(MarginTop), valueInMm => _configuration.MarginTopMm = valueInMm);
    }

    public double MarginBottom
    {
        get => FromMillimeters(_configuration.MarginBottomMm);
        set => SetMargin(value, nameof(MarginBottom), valueInMm => _configuration.MarginBottomMm = valueInMm);
    }

    public double MarginLeft
    {
        get => FromMillimeters(_configuration.MarginLeftMm);
        set => SetMargin(value, nameof(MarginLeft), valueInMm => _configuration.MarginLeftMm = valueInMm);
    }

    public double MarginRight
    {
        get => FromMillimeters(_configuration.MarginRightMm);
        set => SetMargin(value, nameof(MarginRight), valueInMm => _configuration.MarginRightMm = valueInMm);
    }

    public string ScalingMode
    {
        get => _configuration.ScalingMode;
        set
        {
            if (string.Equals(_configuration.ScalingMode, value, StringComparison.Ordinal))
            {
                return;
            }

            _configuration.ScalingMode = value;
            OnPropertyChanged(nameof(ScalingMode));
        }
    }

    public bool ShowSaveDialog
    {
        get => _configuration.ShowSaveDialog;
        set
        {
            if (_configuration.ShowSaveDialog == value)
            {
                return;
            }

            _configuration.ShowSaveDialog = value;
            OnPropertyChanged(nameof(ShowSaveDialog));
        }
    }

    public bool IsPageDimensionsEnabled => string.Equals(PageSize, PdfPageSizes.Custom, StringComparison.OrdinalIgnoreCase);

    public bool IsFixedPage => !string.Equals(PageSize, PdfPageSizes.Image, StringComparison.OrdinalIgnoreCase);

    public void UpdateLocalizedTexts()
    {
        IPdfLanguage language = Texts.Get<IPdfLanguage>();
        UpdateLocalizedOptions(MeasurementUnits, new[]
        {
            new KeyValuePair<string, string>(PdfMeasurementUnits.Mm, language.UnitMm),
            new KeyValuePair<string, string>(PdfMeasurementUnits.Cm, language.UnitCm),
            new KeyValuePair<string, string>(PdfMeasurementUnits.Inch, language.UnitInch)
        });
        UpdateLocalizedOptions(PageSizes, new[]
        {
            new KeyValuePair<string, string>(PdfPageSizes.Image, language.PageImage),
            new KeyValuePair<string, string>(PdfPageSizes.Custom, language.PageCustom),
            new KeyValuePair<string, string>(PdfPageSizes.A3, language.PageA3),
            new KeyValuePair<string, string>(PdfPageSizes.A4, language.PageA4),
            new KeyValuePair<string, string>(PdfPageSizes.A5, language.PageA5),
            new KeyValuePair<string, string>(PdfPageSizes.A6, language.PageA6),
            new KeyValuePair<string, string>(PdfPageSizes.Letter, language.PageLetter)
        });
        UpdateLocalizedOptions(ScalingModes, new[]
        {
            new KeyValuePair<string, string>(PdfScalingModes.OnlyShrinkToFit, language.ScaleShrink),
            new KeyValuePair<string, string>(PdfScalingModes.FitToPage, language.ScaleFit)
        });
    }

    private void SetPageDimension(double value, bool isWidth)
    {
        double millimeters = ToMillimeters(value);
        if (!IsValidDimension(millimeters, 1.0, 10000.0))
        {
            return;
        }

        _configuration.PageSize = PdfPageSizes.Custom;
        if (isWidth)
        {
            _configuration.PageWidthMm = millimeters;
        }
        else
        {
            _configuration.PageHeightMm = millimeters;
        }

        OnPropertyChanged(nameof(PageSize));
        OnPropertyChanged(nameof(PageWidth));
        OnPropertyChanged(nameof(PageHeight));
        OnPropertyChanged(nameof(IsPageDimensionsEnabled));
    }

    private void SetMargin(double value, string propertyName, Action<double> setValue)
    {
        double millimeters = ToMillimeters(value);
        if (!IsValidDimension(millimeters, 0.0, 10000.0))
        {
            return;
        }

        setValue(millimeters);
        OnPropertyChanged(propertyName);
    }

    private double ToMillimeters(double value)
    {
        if (string.Equals(MeasurementUnit, PdfMeasurementUnits.Cm, StringComparison.OrdinalIgnoreCase))
        {
            return value * 10.0;
        }

        if (string.Equals(MeasurementUnit, PdfMeasurementUnits.Inch, StringComparison.OrdinalIgnoreCase))
        {
            return value * 25.4;
        }

        return value;
    }

    private double FromMillimeters(double value)
    {
        if (string.Equals(MeasurementUnit, PdfMeasurementUnits.Cm, StringComparison.OrdinalIgnoreCase))
        {
            return value / 10.0;
        }

        if (string.Equals(MeasurementUnit, PdfMeasurementUnits.Inch, StringComparison.OrdinalIgnoreCase))
        {
            return value / 25.4;
        }

        return value;
    }

    private static bool IsValidDimension(double value, double minimum, double maximum)
    {
        return value >= minimum && value <= maximum && !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    private static void UpdateLocalizedOptions(ObservableCollection<LocalizedOption> options, IReadOnlyList<KeyValuePair<string, string>> localizedOptions)
    {
        bool hasSameKeys = options.Count == localizedOptions.Count;
        for (int index = 0; hasSameKeys && index < options.Count; index++)
        {
            hasSameKeys = string.Equals(options[index].Key, localizedOptions[index].Key, StringComparison.Ordinal);
        }

        if (!hasSameKeys)
        {
            options.Clear();
            foreach (KeyValuePair<string, string> option in localizedOptions)
            {
                options.Add(new LocalizedOption(option.Key, option.Value));
            }

            return;
        }

        for (int index = 0; index < options.Count; index++)
        {
            options[index].Value = localizedOptions[index].Value;
        }
    }

    public sealed class LocalizedOption : INotifyPropertyChanged
    {
        private string _value;

        public LocalizedOption(string key, string value)
        {
            Key = key;
            _value = value;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Key { get; }

        public string Value
        {
            get => _value;
            set
            {
                if (string.Equals(_value, value, StringComparison.Ordinal))
                {
                    return;
                }

                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }
    }
}
