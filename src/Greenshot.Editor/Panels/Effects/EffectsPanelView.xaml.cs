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
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Greenshot.Base.Core;
using Greenshot.Base.Threading;
using Greenshot.Editor.Views;
using DrawingColor = System.Drawing.Color;

namespace Greenshot.Editor.Panels.Effects
{
    /// <summary>
    /// The view of the Effects panel, for an <see cref="EffectsPanelViewModel"/>. Here only what needs WPF: the color swatches,
    /// the zoomed preview, the color picker and the mouse wheel on the size fields.
    /// </summary>
    public partial class EffectsPanelView
    {
        private EffectsPanelViewModel _viewModel;

        public EffectsPanelView()
        {
            InitializeComponent();
            PreviewImage.ZoomChanged += (_, _) => UpdateZoomText();
            DataContextChanged += OnDataContextChanged;
            // The preview is only made while the panel is shown
            IsVisibleChanged += (_, _) => UpdateShown();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelChanged;
                _viewModel.PreviewChanged -= OnPreviewChanged;
                _viewModel.IsShown = false;
            }

            _viewModel = DataContext as EffectsPanelViewModel;
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelChanged;
                _viewModel.PreviewChanged += OnPreviewChanged;
            }

            UpdateSwatches();
            OnPreviewChanged(this, EventArgs.Empty);
            UpdateShown();
        }

        private void UpdateShown()
        {
            if (_viewModel != null)
            {
                _viewModel.IsShown = IsVisible;
            }
        }

        private void OnViewModelChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(EffectsPanelViewModel.BorderColor) or nameof(EffectsPanelViewModel.TransparencyColor))
            {
                UpdateSwatches();
            }
        }

        private void OnPreviewChanged(object sender, EventArgs e)
        {
            PreviewImage.Source = _viewModel?.Preview?.ToBitmapSource();
        }

        private void UpdateZoomText()
        {
            ZoomText.Text = PreviewImage.Source == null ? string.Empty : string.Format(CultureInfo.CurrentCulture, "{0:0} %", PreviewImage.Zoom * 100);
        }

        private void ShowAll_Click(object sender, RoutedEventArgs e) => PreviewImage.ShowAll();

        private void ActualSize_Click(object sender, RoutedEventArgs e) => PreviewImage.ShowActualSize();

        private void UpdateSwatches()
        {
            if (_viewModel == null)
            {
                return;
            }

            BorderColorSwatch.Background = ToBrush(_viewModel.BorderColor);
            TransparencySwatch.Background = ToBrush(_viewModel.TransparencyColor);
        }

        private static Brush ToBrush(DrawingColor color) => new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));

        private void Preset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: EffectPreset preset })
            {
                _viewModel?.ApplyPreset(preset);
            }
        }

        private void BorderColor_Click(object sender, RoutedEventArgs e) => PickColor(_viewModel?.BorderColor, color => _viewModel.BorderColor = color);

        private void TransparencyColor_Click(object sender, RoutedEventArgs e) => PickColor(_viewModel?.TransparencyColor, color => _viewModel.TransparencyColor = color);

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = _viewModel;
            if (viewModel != null)
            {
                AsyncCommand.Run(viewModel.ApplyAsync, "Apply the effect of the Effects panel");
            }
        }

        /// <summary>
        /// The wheel changes the number by one, with Shift by ten
        /// </summary>
        private void ResizeBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta == 0 || sender is not TextBox textBox || !int.TryParse(textBox.Text, out int value))
            {
                return;
            }

            int step = (Keyboard.Modifiers & ModifierKeys.Shift) != 0 ? 10 : 1;
            int next = Math.Max(1, Math.Min(EffectsPanelViewModel.MaximumDimension, value + Math.Sign(e.Delta) * step));
            textBox.Text = next.ToString();
            textBox.CaretIndex = textBox.Text.Length;
            e.Handled = true;
        }

        private void PickColor(DrawingColor? current, Action<DrawingColor> apply)
        {
            if (_viewModel == null || current == null)
            {
                return;
            }

            var picker = new ColorPickerWindow { SelectedColor = current.Value };
            var window = Window.GetWindow(this);
            if (window != null)
            {
                picker.Owner = window;
            }
            else if (PresentationSource.FromVisual(this) is HwndSource source)
            {
                // In the panel of the editor (a WinForms window): its top level window becomes the owner
                new WindowInteropHelper(picker) { Owner = source.Handle };
            }

            if (picker.ShowDialog() == true)
            {
                apply(picker.SelectedColor);
            }
        }
    }
}
