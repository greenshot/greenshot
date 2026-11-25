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

using System.Windows;
using Greenshot.Base.Core;
using Greenshot.Plugin.Zxing.Drawing;
using Greenshot.Plugin.Zxing.ViewModels;

namespace Greenshot.Plugin.Zxing.Views
{
    /// <summary>
    /// The barcode options of a recipe step in a window: Apply keeps the changes in the model, Cancel doesn't use them
    /// (the model is a copy which the caller only takes on Apply).
    /// </summary>
    public partial class ZxingEditorWindow : Window
    {
        /// <param name="model">the barcode to change</param>
        public ZxingEditorWindow(ZxingModel model)
        {
            InitializeComponent();
            Icon = GreenshotResources.GetGreenshotIcon()?.ToBitmapSource();
            ViewModel = new BarcodeEditorViewModel(model);
            EditorView.DataContext = ViewModel;
        }

        public BarcodeEditorViewModel ViewModel { get; }

        private void Apply_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    }
}
