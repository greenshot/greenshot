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

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Greenshot.Linux.RecipeEditor.ViewModels;

namespace Greenshot.Linux.RecipeEditing;

public partial class RecipeManagerWindow : Window
{
    public RecipeManagerWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public RecipeManagerWindow(RecipeManagerViewModel viewModel)
        : this()
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        viewModel.RequestClose += Close;
        Icon = Helpers.AppIcons.Window;
    }

    public RecipeManagerViewModel? ViewModel { get; }

    /// <summary>
    /// The filter chips: the one checked is the category shown
    /// </summary>
    private void OnFilterChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { IsChecked: true, Tag: string category } && ViewModel != null)
        {
            ViewModel.SelectedFilterCategory = category;
        }
    }
}
