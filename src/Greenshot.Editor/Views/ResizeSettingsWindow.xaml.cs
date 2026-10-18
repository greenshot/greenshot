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
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Greenshot.Base.Core;
using Greenshot.Base.Effects;
using log4net;
using Greenshot.Base.Languages;

namespace Greenshot.Editor.Views
{
    public partial class ResizeSettingsWindow : Window
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(ResizeSettingsWindow));
        private const int MaximumDimension = 30_000; // picking a random number. Probably the underlying image library and memory has a limit. On my PC GDI+ stops at round about 25k
        private readonly ResizeEffect _effect;
        private readonly string _valuePixel;
        private readonly string _valuePercent;
        private double _newWidth;
        private double _newHeight;
        private bool _isUpdating;
        private bool _isInitializing = true;
        private bool _isWidthInputValid = true;
        private bool _isHeightInputValid = true;

        public ResizeSettingsWindow() : this(new ResizeEffect(100, 100, true))
        {
        }

        public ResizeSettingsWindow(ResizeEffect effect)
        {
            _effect = effect ?? new ResizeEffect(100, 100, true);
            _valuePixel = Texts.Editor.ResizePixel;
            _valuePercent = Texts.Editor.ResizePercent;

            InitializeComponent();
            try
            {
                Icon = ImageHelper.ToBitmapSource(GreenshotResources.GetGreenshotIcon());
            }
            catch (Exception ex)
            {
                LOG.Debug("Could not set window icon", ex);
            }

            WidthUnitComboBox.Items.Add(_valuePixel);
            WidthUnitComboBox.Items.Add(_valuePercent);
            WidthUnitComboBox.SelectedItem = _valuePixel;

            HeightUnitComboBox.Items.Add(_valuePixel);
            HeightUnitComboBox.Items.Add(_valuePercent);
            HeightUnitComboBox.SelectedItem = _valuePixel;

            _newWidth = _effect.Width;
            _newHeight = _effect.Height;

            MaintainAspectRatioCheckBox.IsChecked = _effect.MaintainAspectRatio;

            DisplayWidth();
            DisplayHeight();

            WidthTextBox.TextChanged += WidthTextBox_TextChanged;
            HeightTextBox.TextChanged += HeightTextBox_TextChanged;
            WidthUnitComboBox.SelectionChanged += UnitComboBox_SelectionChanged;
            HeightUnitComboBox.SelectionChanged += UnitComboBox_SelectionChanged;
            MaintainAspectRatioCheckBox.Checked += MaintainAspectRatioCheckBox_Changed;
            MaintainAspectRatioCheckBox.Unchecked += MaintainAspectRatioCheckBox_Changed;

            _isInitializing = false;
            UpdateOkButton();
        }

        private void DisplayWidth()
        {
            if (WidthTextBox == null || WidthUnitComboBox == null || _valuePercent == null) return;
            _isUpdating = true;
            try
            {
                double displayValue = _valuePercent.Equals(WidthUnitComboBox.SelectedItem)
                    ? (_effect.Width > 0 ? (_newWidth / _effect.Width * 100.0) : 100.0)
                    : _newWidth;
                WidthTextBox.Text = Math.Round(displayValue).ToString("0", CultureInfo.InvariantCulture);
                _isWidthInputValid = IsValidDimension(_newWidth);
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void DisplayHeight()
        {
            if (HeightTextBox == null || HeightUnitComboBox == null || _valuePercent == null) return;
            _isUpdating = true;
            try
            {
                double displayValue = _valuePercent.Equals(HeightUnitComboBox.SelectedItem)
                    ? (_effect.Height > 0 ? (_newHeight / _effect.Height * 100.0) : 100.0)
                    : _newHeight;
                HeightTextBox.Text = Math.Round(displayValue).ToString("0", CultureInfo.InvariantCulture);
                _isHeightInputValid = IsValidDimension(_newHeight);
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void WidthTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _isUpdating) return;

            if (!TryGetDimension(WidthTextBox.Text, WidthUnitComboBox.SelectedItem, _effect.Width, out double parsed))
            {
                _isWidthInputValid = false;
                UpdateOkButton();
                return;
            }

            _isWidthInputValid = true;
            _newWidth = parsed;

            if (MaintainAspectRatioCheckBox.IsChecked == true && _effect.Width > 0)
            {
                double percent = _newWidth / _effect.Width;
                _newHeight = _effect.Height * percent;
                DisplayHeight();
            }

            UpdateOkButton();
        }

        private void HeightTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _isUpdating) return;

            if (!TryGetDimension(HeightTextBox.Text, HeightUnitComboBox.SelectedItem, _effect.Height, out double parsed))
            {
                _isHeightInputValid = false;
                UpdateOkButton();
                return;
            }

            _isHeightInputValid = true;
            _newHeight = parsed;

            if (MaintainAspectRatioCheckBox.IsChecked == true && _effect.Height > 0)
            {
                double percent = _newHeight / _effect.Height;
                _newWidth = _effect.Width * percent;
                DisplayWidth();
            }

            UpdateOkButton();
        }

        private void UnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            DisplayWidth();
            DisplayHeight();
            UpdateOkButton();
        }

        private void MaintainAspectRatioCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            if (MaintainAspectRatioCheckBox.IsChecked == true && _effect.Width > 0)
            {
                double percent = _newWidth / _effect.Width;
                _newHeight = _effect.Height * percent;
                DisplayHeight();
            }

            UpdateOkButton();
        }

        private void DimensionTextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta == 0 || !(sender is TextBox textBox))
            {
                return;
            }

            bool isWidth = textBox == WidthTextBox;
            ComboBox unitComboBox = isWidth ? WidthUnitComboBox : HeightUnitComboBox;
            int originalDimension = isWidth ? _effect.Width : _effect.Height;
            if (!TryParseInput(textBox.Text, out double currentValue))
            {
                e.Handled = true;
                return;
            }

            double maximumInput = GetMaximumInputValue(isWidth, unitComboBox.SelectedItem, originalDimension);
            double minimumInput = GetMinimumInputValue(isWidth, unitComboBox.SelectedItem, originalDimension);
            if (maximumInput <= 0 || minimumInput > maximumInput ||
                double.IsNaN(maximumInput) || double.IsInfinity(maximumInput))
            {
                e.Handled = true;
                return;
            }

            double step = (Keyboard.Modifiers & ModifierKeys.Shift) != 0 ? 10 : 1;
            double nextValue = currentValue + Math.Sign(e.Delta) * step;
            nextValue = Math.Max(minimumInput, Math.Min(nextValue, maximumInput));
            string nextText = nextValue.ToString("G", CultureInfo.InvariantCulture);

            if (!TryGetDimension(nextText, unitComboBox.SelectedItem, originalDimension, out double dimension) ||
                !IsAspectRatioDimensionValid(isWidth, dimension))
            {
                e.Handled = true;
                return;
            }

            textBox.Text = nextText;
            e.Handled = true;
        }

        private double GetMaximumInputValue(bool isWidth, object selectedUnit, int originalDimension)
        {
            double maximumDimension = MaximumDimension;
            if (MaintainAspectRatioCheckBox.IsChecked == true && _effect.Width > 0 && _effect.Height > 0)
            {
                double maximumForOtherDimension = isWidth
                    ? MaximumDimension * _effect.Width / _effect.Height
                    : MaximumDimension * _effect.Height / _effect.Width;
                maximumDimension = Math.Min(maximumDimension, maximumForOtherDimension);
            }

            return _valuePercent.Equals(selectedUnit)
                ? (originalDimension > 0 ? maximumDimension * 100.0 / originalDimension : 0)
                : maximumDimension;
        }

        private double GetMinimumInputValue(bool isWidth, object selectedUnit, int originalDimension)
        {
            const double minimumRoundedDimension = 0.500001;
            double minimumDimension = minimumRoundedDimension;
            if (MaintainAspectRatioCheckBox.IsChecked == true && _effect.Width > 0 && _effect.Height > 0)
            {
                double minimumForOtherDimension = isWidth
                    ? minimumRoundedDimension * _effect.Width / _effect.Height
                    : minimumRoundedDimension * _effect.Height / _effect.Width;
                minimumDimension = Math.Max(minimumDimension, minimumForOtherDimension);
            }

            if (_valuePercent.Equals(selectedUnit))
            {
                return originalDimension > 0 ? minimumDimension * 100.0 / originalDimension : double.PositiveInfinity;
            }

            return Math.Ceiling(minimumDimension);
        }

        private bool IsAspectRatioDimensionValid(bool isWidth, double dimension)
        {
            if (MaintainAspectRatioCheckBox.IsChecked != true)
            {
                return true;
            }

            if (isWidth && _effect.Width > 0)
            {
                return IsValidDimension(_effect.Height * (dimension / _effect.Width));
            }

            if (!isWidth && _effect.Height > 0)
            {
                return IsValidDimension(_effect.Width * (dimension / _effect.Height));
            }

            return true;
        }

        private bool TryGetDimension(string text, object selectedUnit, int originalDimension, out double dimension)
        {
            dimension = 0;
            if (!TryParseInput(text, out double input))
            {
                return false;
            }

            if (double.IsNaN(input) || double.IsInfinity(input) || input <= 0)
            {
                return false;
            }

            dimension = _valuePercent.Equals(selectedUnit)
                ? originalDimension / 100.0 * input
                : input;

            return IsValidDimension(dimension);
        }

        private static bool TryParseInput(string text, out double input)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out input) ||
                double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out input);
        }

        private static bool IsValidDimension(double dimension)
        {
            if (double.IsNaN(dimension) || double.IsInfinity(dimension))
            {
                return false;
            }

            double roundedDimension = Math.Round(dimension);
            return roundedDimension >= 1 && roundedDimension <= MaximumDimension;
        }

        private void UpdateOkButton()
        {
            if (OkButton != null)
            {
                bool isValid = _isWidthInputValid && _isHeightInputValid &&
                    IsValidDimension(_newWidth) && IsValidDimension(_newHeight);
                OkButton.IsEnabled = isValid;
                OkButton.IsDefault = isValid;
            }
        }

        private void ResizeSettingsWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (!IsValidDimension(_newWidth) || !IsValidDimension(_newHeight) ||
                !_isWidthInputValid || !_isHeightInputValid))
            {
                e.Handled = true;
            }
        }

        public bool? ShowDialog(System.Windows.Forms.IWin32Window owner)
        {
            if (owner != null)
            {
                new WindowInteropHelper(this) { Owner = owner.Handle };
            }
            return ShowDialog();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isWidthInputValid || !_isHeightInputValid ||
                !IsValidDimension(_newWidth) || !IsValidDimension(_newHeight))
            {
                UpdateOkButton();
                return;
            }

            const double tolerance = 3 * double.Epsilon;
            if (Math.Abs(_newWidth - _effect.Width) > tolerance || Math.Abs(_newHeight - _effect.Height) > tolerance ||
                _effect.MaintainAspectRatio != (MaintainAspectRatioCheckBox.IsChecked == true))
            {
                _effect.Width = Math.Max(1, (int)Math.Round(_newWidth));
                _effect.Height = Math.Max(1, (int)Math.Round(_newHeight));
                _effect.MaintainAspectRatio = MaintainAspectRatioCheckBox.IsChecked == true;
                DialogResult = true;
            }
            else
            {
                DialogResult = false;
            }
            Close();
        }
    }
}
