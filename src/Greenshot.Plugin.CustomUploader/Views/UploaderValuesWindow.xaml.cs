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

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Greenshot.Plugin.CustomUploader.ViewModels;

namespace Greenshot.Plugin.CustomUploader.Views;

/// <summary>
/// Before the import: the user fills in the values of the uploader (the key where the file has "YOUR_API_KEY").
/// The values are changed in place, the import checks them again.
/// </summary>
public partial class UploaderValuesWindow : Window
{
    internal UploaderValuesWindow(string title, IReadOnlyList<UploaderValue> values)
    {
        InitializeComponent();
        Title = title;
        ValueList.ItemsSource = values;
    }

    // Each time the mask comes back, it takes over what the user typed while the value was shown
    private void Secret_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var passwordBox = (PasswordBox)sender;
        if (!passwordBox.IsVisible) return;
        if (passwordBox.DataContext is UploaderValue value && passwordBox.Password != (value.Value ?? string.Empty))
        {
            passwordBox.Password = value.Value ?? string.Empty;
        }
    }

    private void Secret_PasswordChanged(object sender, RoutedEventArgs e)
    {
        var passwordBox = (PasswordBox)sender;
        if (passwordBox.DataContext is UploaderValue value)
        {
            value.Value = passwordBox.Password;
        }
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
