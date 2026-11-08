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
using System.IO;
using System.Windows;
using System.Windows.Input;
using Greenshot.Plugin.ExternalCommand.Running;
using Greenshot.Plugin.ExternalCommand.ViewModels;
using Microsoft.Win32;

namespace Greenshot.Plugin.ExternalCommand.Views;

/// <summary>
/// Pick the app of a command from the list, or browse for a program
/// </summary>
public partial class AppPickerWindow : Window
{
    private readonly AppPickerViewModel _viewModel;

    internal AppPickerWindow(AppPickerViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// The chosen app, null when the user cancelled
    /// </summary>
    internal InstalledApp ChosenApp { get; private set; }

    /// <summary>
    /// Let the user pick a program file, as an app with the default arguments
    /// </summary>
    /// <param name="owner">the window</param>
    /// <param name="current">the current program, its folder is where the dialog starts</param>
    internal static InstalledApp BrowseForProgram(Window owner, string current)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Programs (*.exe, *.bat, *.com, *.cmd)|*.exe;*.bat;*.com;*.cmd|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        string folder = null;
        try
        {
            folder = string.IsNullOrWhiteSpace(current) ? null : Path.GetDirectoryName(current);
        }
        catch (ArgumentException)
        {
            // Not a path, e.g. a template
        }

        dialog.InitialDirectory = !string.IsNullOrEmpty(folder) && Directory.Exists(folder) ? folder : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (dialog.ShowDialog(owner) != true)
        {
            return null;
        }

        return new InstalledApp(Path.GetFileNameWithoutExtension(dialog.FileName), dialog.FileName, CommandDefinition.DefaultArguments);
    }

    private void Choose()
    {
        if (_viewModel.SelectedApp == null) return;
        ChosenApp = _viewModel.SelectedApp.App;
        DialogResult = true;
    }

    private void Choose_Click(object sender, RoutedEventArgs e) => Choose();

    private void AppList_MouseDoubleClick(object sender, MouseButtonEventArgs e) => Choose();

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var app = BrowseForProgram(this, null);
        if (app == null) return;
        ChosenApp = app;
        DialogResult = true;
    }

    /// <summary>
    /// The arrow keys move through the list while the search has the focus
    /// </summary>
    private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        int count = _viewModel.Apps.Count;
        if (count == 0 || (e.Key != Key.Down && e.Key != Key.Up)) return;
        int index = _viewModel.SelectedApp == null ? -1 : _viewModel.Apps.IndexOf(_viewModel.SelectedApp);
        index = e.Key == Key.Down ? Math.Min(count - 1, index + 1) : Math.Max(0, index - 1);
        _viewModel.SelectedApp = _viewModel.Apps[index];
        AppList.ScrollIntoView(_viewModel.SelectedApp);
        e.Handled = true;
    }
}
