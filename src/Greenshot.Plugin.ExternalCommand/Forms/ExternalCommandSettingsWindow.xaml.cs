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
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Dapplo.Ini;
using Greenshot.Base.Core;

namespace Greenshot.Plugin.ExternalCommand.Forms;

public partial class ExternalCommandSettingsWindow : Window
{
    private static readonly IExternalCommandConfiguration ExternalCommandConfig = IniConfigRegistry.GetSection<IExternalCommandConfiguration>();

    public class CommandViewModel
    {
        public string Name { get; set; }
        public ImageSource Icon { get; set; }
    }

    private readonly ObservableCollection<CommandViewModel> _commands = new ObservableCollection<CommandViewModel>();

    public ExternalCommandSettingsWindow()
    {
        InitializeComponent();
        CommandsListView.ItemsSource = _commands;
        UpdateView();

        try
        {
            Icon = GreenshotResources.GetGreenshotIcon()?.ToBitmapSource();
        }
        catch
        {
            // Ignore in headless/test environments
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void UpdateView()
    {
        _commands.Clear();
        if (ExternalCommandConfig.Commands != null)
        {
            foreach (string commando in ExternalCommandConfig.Commands)
            {
                ImageSource iconSource = null;
                try
                {
                    var icon = IconCache.IconForCommand(commando);
                    if (icon != null)
                    {
                        iconSource = icon.ToBitmapSource();
                    }
                }
                catch
                {
                    // Ignore icon loading failures
                }

                _commands.Add(new CommandViewModel
                {
                    Name = commando,
                    Icon = iconSource
                });
            }
        }

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        bool hasSelection = CommandsListView.SelectedItem is CommandViewModel;
        ButtonEdit.IsEnabled = hasSelection;
        ButtonDelete.IsEnabled = hasSelection;
    }

    private void CommandsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateButtons();
    }

    private void CommandsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (CommandsListView.SelectedItem is CommandViewModel)
        {
            EditSelected();
        }
    }

    private void ButtonNew_Click(object sender, RoutedEventArgs e)
    {
        var detailWindow = new ExternalCommandDetailWindow(null)
        {
            Owner = this
        };

        if (detailWindow.ShowDialog() == true)
        {
            UpdateView();
        }
    }

    private void ButtonEdit_Click(object sender, RoutedEventArgs e)
    {
        EditSelected();
    }

    private void EditSelected()
    {
        if (CommandsListView.SelectedItem is CommandViewModel selected)
        {
            var detailWindow = new ExternalCommandDetailWindow(selected.Name)
            {
                Owner = this
            };

            if (detailWindow.ShowDialog() == true)
            {
                UpdateView();
            }
        }
    }

    private void ButtonDeleteClick(object sender, RoutedEventArgs e)
    {
        ButtonDelete_Click(sender, e);
    }

    private void ButtonDelete_Click(object sender, RoutedEventArgs e)
    {
        if (CommandsListView.SelectedItem is CommandViewModel selected)
        {
            ExternalCommandConfig.Delete(selected.Name);
            UpdateView();
        }
    }

    private void ButtonOk_Click(object sender, RoutedEventArgs e)
    {
        IniConfigRegistry.Get().Save();
        DialogResult = true;
    }
}
