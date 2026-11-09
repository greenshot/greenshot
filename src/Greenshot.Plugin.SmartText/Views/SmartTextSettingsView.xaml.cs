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
using Greenshot.Plugin.SmartText.Detection;
using Greenshot.Plugin.SmartText.ViewModels;

namespace Greenshot.Plugin.SmartText.Views;

/// <summary>
/// The settings page: the built-in detectors on or off; own patterns added, imported, changed, exported and removed.
/// An import is always confirmed: the user sees the pattern and where it sends values before anything is stored.
/// </summary>
public partial class SmartTextSettingsView : UserControl
{
    private readonly SmartTextSettingsViewModel _viewModel;

    internal SmartTextSettingsView(SmartTextSettingsViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = viewModel;
        InitializeComponent();
    }

    private static ISmartTextLanguage SmartTexts => SmartTextPlugin.Language;

    private void Add_Click(object sender, RoutedEventArgs e) => Edit(new SmartTextPattern(), null);

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

    private void ApproveSelected()
    {
        if (!_viewModel.CanApproveSelected) return;
        var definition = _viewModel.GetSelectedDefinition();
        if (definition == null) return;
        var buttons = new[] { SmartTexts.ApproveButton, Texts.Core.Cancel };
        if (DefinitionImport.Confirm(Window.GetWindow(this), SmartTextSettingsViewModel.DescribeApproval(definition, _viewModel.SelectedPattern?.Id), buttons) == 0)
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
            Edit(definition, _viewModel.SelectedPattern.Id);
        }
    }

    private void Edit(SmartTextPattern definition, string originalId)
    {
        var editor = new SmartTextPatternWindow(new SmartTextPatternViewModel(definition, originalId), _viewModel.Save) { Owner = Window.GetWindow(this) };
        editor.ShowDialog();
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        string path = DefinitionImport.PickFile(window, SmartTexts.ImportFileFilter);
        if (path == null) return;

        var definition = SmartTextSettingsViewModel.ReadFile(path, out string error);
        if (definition != null && !_viewModel.CanImport(definition))
        {
            definition = null;
            error = SmartTexts.DuplicateName;
        }

        if (definition == null)
        {
            ThemedMessageBox.Show(window, string.Format(SmartTexts.ImportFailed, Path.GetFileName(path), error), SmartTexts.ImportTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Cancel is the default, the buttons only react after a moment, so a double click doesn't import
        var buttons = new[] { SmartTexts.ImportButton, Texts.Core.Cancel };
        if (DefinitionImport.Confirm(window, _viewModel.DescribeImport(definition, path), buttons) != 0) return;

        try
        {
            _viewModel.Import(definition);
        }
        catch (DefinitionFileException ex)
        {
            ThemedMessageBox.Show(window, string.Format(SmartTexts.ImportFailed, Path.GetFileName(path), ex.Message), SmartTexts.ImportTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var pattern = _viewModel.SelectedPattern;
        if (pattern == null || !_viewModel.CanExportSelected) return;
        var window = Window.GetWindow(this);
        string path = DefinitionImport.PickExportFile(window, SmartTexts.ImportFileFilter, DefinitionImport.FileNameFor(pattern.Name, SmartTextKind.FileExtension));
        if (path == null) return;

        try
        {
            _viewModel.ExportSelected(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ThemedMessageBox.Show(window, string.Format(SmartTexts.ExportFailed, ex.Message), SmartTexts.WindowTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        var pattern = _viewModel.SelectedPattern;
        if (pattern == null || !_viewModel.CanRemoveSelected) return;
        if (ThemedMessageBox.Show(Window.GetWindow(this), string.Format(SmartTexts.RemoveQuestion, pattern.Name), SmartTexts.LabelOwn, MessageBoxButton.YesNo,
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
            ThemedMessageBox.Show(Window.GetWindow(this), ex.Message, SmartTexts.LabelOwn, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
