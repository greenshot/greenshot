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
using GreenshotLanguage = Greenshot.Base.Core.Language;

namespace Greenshot.Editor.Forms
{
    public partial class ResizeSettingsWindow : Window
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(ResizeSettingsWindow));
        private readonly ResizeEffect _effect;
        private readonly string _valuePixel;
        private readonly string _valuePercent;
        private double _newWidth;
        private double _newHeight;
        private bool _isUpdating;
        private bool _isInitializing = true;

        public ResizeSettingsWindow() : this(new ResizeEffect(100, 100, true))
        {
        }

        public ResizeSettingsWindow(ResizeEffect effect)
        {
            _effect = effect ?? new ResizeEffect(100, 100, true);
            _valuePixel = GreenshotLanguage.GetString("editor_resize_pixel");
            _valuePercent = GreenshotLanguage.GetString("editor_resize_percent");

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
                WidthTextBox.Text = ((int)Math.Round(displayValue)).ToString(CultureInfo.InvariantCulture);
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
                HeightTextBox.Text = ((int)Math.Round(displayValue)).ToString(CultureInfo.InvariantCulture);
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void WidthTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _isUpdating) return;

            if (!double.TryParse(WidthTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed) &&
                !double.TryParse(WidthTextBox.Text, out parsed))
            {
                return;
            }

            bool isPercent = _valuePercent.Equals(WidthUnitComboBox.SelectedItem);
            if (isPercent)
            {
                _newWidth = _effect.Width / 100.0 * parsed;
            }
            else
            {
                _newWidth = parsed;
            }

            if (MaintainAspectRatioCheckBox.IsChecked == true && _effect.Width > 0)
            {
                double percent = _newWidth / _effect.Width;
                _newHeight = _effect.Height * percent;
                DisplayHeight();
            }
        }

        private void HeightTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _isUpdating) return;

            if (!double.TryParse(HeightTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed) &&
                !double.TryParse(HeightTextBox.Text, out parsed))
            {
                return;
            }

            bool isPercent = _valuePercent.Equals(HeightUnitComboBox.SelectedItem);
            if (isPercent)
            {
                _newHeight = _effect.Height / 100.0 * parsed;
            }
            else
            {
                _newHeight = parsed;
            }

            if (MaintainAspectRatioCheckBox.IsChecked == true && _effect.Height > 0)
            {
                double percent = _newHeight / _effect.Height;
                _newWidth = _effect.Width * percent;
                DisplayWidth();
            }
        }

        private void UnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            DisplayWidth();
            DisplayHeight();
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
