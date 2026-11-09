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
using System.Windows.Controls;
using System.Windows.Input;
using Greenshot.Base.Definitions;
using Greenshot.Base.Languages;
using Greenshot.Base.Wpf;
using Greenshot.Plugin.ExternalCommand.Running;
using Greenshot.Plugin.ExternalCommand.ViewModels;

namespace Greenshot.Plugin.ExternalCommand.Views;

/// <summary>
/// The settings page: add, import, change, export and remove commands.
/// An import is always confirmed: the user sees the program and its arguments before anything is stored.
/// </summary>
public partial class CommandSettingsView : UserControl
{
    private readonly CommandSettingsViewModel _viewModel;

    internal CommandSettingsView(CommandSettingsViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = viewModel;
        InitializeComponent();
    }

    private static IExternalCommandLanguage CommandTexts => Texts.Get<IExternalCommandLanguage>();

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        Edit(new CommandDefinition { RunInBackground = true }, null);
    }

    private void Edit_Click(object sender, RoutedEventArgs e) => EditSelected();

    private void List_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.CanApproveSelected)
        {
            ApproveSelected();
        }
        else
        {
            EditSelected();
        }
    }

    private void Approve_Click(object sender, RoutedEventArgs e) => ApproveSelected();

    /// <summary>
    /// A command file added or changed outside Greenshot is only used after the user saw what it runs, like an import
    /// </summary>
    private void ApproveSelected()
    {
        if (!_viewModel.CanApproveSelected) return;
        var definition = _viewModel.GetSelectedDefinition();
        if (definition == null) return;
        var buttons = new[] { CommandTexts.ApproveButton, Texts.Core.Cancel };
        if (DefinitionImport.Confirm(Window.GetWindow(this), CommandSettingsViewModel.DescribeApproval(definition, _viewModel.SelectedCommand?.Id), buttons) == 0)
        {
            _viewModel.ApproveSelected();
        }
    }

    private void EditSelected()
    {
        if (!_viewModel.CanEditSelected) return;
        var definition = _viewModel.GetSelectedDefinition();
        if (definition != null)
        {
            Edit(definition, _viewModel.SelectedCommand.Id);
        }
    }

    /// <summary>
    /// Show the editor until the command is saved or the user cancels
    /// </summary>
    private void Edit(CommandDefinition definition, string originalId)
    {
        var editor = new CommandEditorWindow(new CommandEditorViewModel(definition, originalId), _viewModel.Save) { Owner = Window.GetWindow(this) };
        editor.ShowDialog();
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        string path = DefinitionImport.PickFile(window, CommandTexts.ImportFileFilter);
        if (path == null)
        {
            return;
        }

        var definition = CommandSettingsViewModel.ReadFile(path, out string error);
        if (definition != null && !_viewModel.CanImport(definition))
        {
            definition = null;
            error = CommandTexts.TooltipDuplicateName;
        }

        if (definition == null)
        {
            ThemedMessageBox.Show(window, string.Format(CommandTexts.ImportFailed, Path.GetFileName(path), error), CommandTexts.ImportTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // The gate of the import: Cancel is the default, the buttons only react after a moment, so a double click doesn't import
        var buttons = new[] { CommandTexts.ImportButton, Texts.Core.Cancel };
        if (DefinitionImport.Confirm(window, _viewModel.DescribeImport(definition, path), buttons) != 0)
        {
            return;
        }

        try
        {
            _viewModel.Import(definition);
        }
        catch (DefinitionFileException ex)
        {
            ThemedMessageBox.Show(window, string.Format(CommandTexts.ImportFailed, Path.GetFileName(path), ex.Message), CommandTexts.ImportTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var command = _viewModel.SelectedCommand;
        if (command == null || !_viewModel.CanExportSelected) return;
        var window = Window.GetWindow(this);
        string path = DefinitionImport.PickExportFile(window, CommandTexts.ImportFileFilter, DefinitionImport.FileNameFor(command.Name, CommandKind.FileExtension));
        if (path == null)
        {
            return;
        }

        try
        {
            _viewModel.ExportSelected(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ThemedMessageBox.Show(window, string.Format(CommandTexts.ExportFailed, ex.Message), CommandTexts.ExportTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        var command = _viewModel.SelectedCommand;
        if (command == null || !_viewModel.CanRemoveSelected) return;
        if (ThemedMessageBox.Show(Window.GetWindow(this), string.Format(CommandTexts.RemoveQuestion, command.Name), CommandTexts.LabelCommands, MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            _viewModel.RemoveSelected();
        }
        catch (DefinitionFileException ex)
        {
            ThemedMessageBox.Show(Window.GetWindow(this), ex.Message, CommandTexts.LabelCommands, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
