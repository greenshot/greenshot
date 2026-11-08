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
using Greenshot.Base.Definitions;
using Greenshot.Base.Languages;
using Greenshot.Base.Wpf;
using Greenshot.Plugin.ExternalCommand.Running;
using Greenshot.Plugin.ExternalCommand.ViewModels;
using Microsoft.Win32;

namespace Greenshot.Plugin.ExternalCommand.Views;

/// <summary>
/// Add or change a command. Save checks it like an imported file, the window stays open until it passes or the user cancels.
/// </summary>
public partial class CommandEditorWindow : Window
{
    private readonly CommandEditorViewModel _viewModel;
    private readonly Action<CommandDefinition, string> _save;

    /// <param name="viewModel">the command</param>
    /// <param name="save">saves the command (the definition, the id of the changed one), throws a <see cref="DefinitionFileException"/> when it can't</param>
    internal CommandEditorWindow(CommandEditorViewModel viewModel, Action<CommandDefinition, string> save)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _save = save ?? throw new ArgumentNullException(nameof(save));
        DataContext = viewModel;
        InitializeComponent();
    }

    private static IExternalCommandLanguage CommandTexts => Texts.Get<IExternalCommandLanguage>();

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _save(_viewModel.ToDefinition(), _viewModel.OriginalId);
            DialogResult = true;
        }
        catch (DefinitionFileException ex)
        {
            ThemedMessageBox.Show(this, string.Format(CommandTexts.EditorFailed, ex.Message), _viewModel.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Executables (*.exe, *.bat, *.com, *.cmd)|*.exe;*.bat;*.com;*.cmd|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        string folder = null;
        try
        {
            folder = string.IsNullOrWhiteSpace(_viewModel.Program) ? null : Path.GetDirectoryName(_viewModel.Program);
        }
        catch (ArgumentException)
        {
            // Not a path, e.g. a template
        }

        dialog.InitialDirectory = folder != null && Directory.Exists(folder) ? folder : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        _viewModel.Program = dialog.FileName;
        if (string.IsNullOrWhiteSpace(_viewModel.Name))
        {
            _viewModel.Name = Path.GetFileNameWithoutExtension(dialog.FileName);
        }
    }
}
