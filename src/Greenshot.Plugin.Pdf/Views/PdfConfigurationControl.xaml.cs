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

using System.Windows;
using System.Windows.Controls;
using Greenshot.Plugin.Pdf.Configuration;
using Greenshot.Plugin.Pdf.ViewModels;

namespace Greenshot.Plugin.Pdf.Views;

public partial class PdfConfigurationControl : UserControl
{
    public PdfConfigurationControl(IPdfConfiguration configuration)
    {
        InitializeComponent();
        DataContext = new PdfConfigurationViewModel(configuration);
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // If the language is changed, while the settings window is open, we need to update the localized texts in the view model (ComboBoxes).
        ((PdfConfigurationViewModel)DataContext).UpdateLocalizedTexts();
    }

}
