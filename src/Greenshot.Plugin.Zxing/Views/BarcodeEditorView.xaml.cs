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
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Greenshot.Base.Core;
using Greenshot.Editor.Views;
using Greenshot.Plugin.Zxing.ViewModels;
using DrawingColor = System.Drawing.Color;

namespace Greenshot.Plugin.Zxing.Views
{
    /// <summary>
    /// The fields of a barcode, for a <see cref="BarcodeEditorViewModel"/>. Here only what needs WPF: the color swatches, the
    /// preview image and the color picker.
    /// </summary>
    public partial class BarcodeEditorView
    {
        private BarcodeEditorViewModel _viewModel;
        private BarcodePanelViewModel _panelViewModel;

        public BarcodeEditorView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        /// <summary>
        /// Show the Insert button of the Barcode panel
        /// </summary>
        internal void ShowInsert(BarcodePanelViewModel panelViewModel)
        {
            _panelViewModel = panelViewModel;
            InsertButton.DataContext = panelViewModel;
            InsertButton.Visibility = Visibility.Visible;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelChanged;
            }

            _viewModel = DataContext as BarcodeEditorViewModel;
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelChanged;
            }

            UpdateLook();
        }

        private void OnViewModelChanged(object sender, PropertyChangedEventArgs e) => UpdateLook();

        private void UpdateLook()
        {
            if (_viewModel == null)
            {
                return;
            }

            ForeColorSwatch.Background = ToBrush(_viewModel.ForeColor);
            BackColorSwatch.Background = ToBrush(_viewModel.BackColor);
            PreviewImage.Source = _viewModel.Preview?.ToBitmapSource();
        }

        private static Brush ToBrush(DrawingColor color) => new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));

        private void ForeColor_Click(object sender, RoutedEventArgs e) => PickColor(_viewModel.ForeColor, color => _viewModel.ForeColor = color);

        private void BackColor_Click(object sender, RoutedEventArgs e) => PickColor(_viewModel.BackColor, color => _viewModel.BackColor = color);

        private void PickColor(DrawingColor current, Action<DrawingColor> apply)
        {
            if (_viewModel == null)
            {
                return;
            }

            var picker = new ColorPickerWindow { SelectedColor = current };
            var window = Window.GetWindow(this);
            if (window != null)
            {
                picker.Owner = window;
            }
            else if (PresentationSource.FromVisual(this) is HwndSource source)
            {
                // In the panel of the editor (a WinForms window): Windows makes its top level window the owner
                new WindowInteropHelper(picker) { Owner = source.Handle };
            }

            if (picker.ShowDialog() == true)
            {
                apply(picker.SelectedColor);
            }
        }

        private void Insert_Click(object sender, RoutedEventArgs e) => _panelViewModel?.Insert();
    }
}
