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
using System.Windows;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Definitions;
using Greenshot.Base.Languages;
using Greenshot.Base.Wpf;
using Greenshot.Plugin.ExternalCommand.Running;
using Greenshot.Plugin.ExternalCommand.ViewModels;

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

    /// <summary>
    /// Pick one of the apps Windows knows, which open the image format, or browse for a program
    /// </summary>
    private void ChooseApp_Click(object sender, RoutedEventArgs e)
    {
        string extension = FileFormatRegistry.GetPreferredExtensionWithDot(_viewModel.Format ?? WellKnownFileFormats.Png);
        var picker = new AppPickerWindow(new AppPickerViewModel(InstalledApps.Find(extension))) { Owner = this };
        if (picker.ShowDialog() == true)
        {
            _viewModel.TakeApp(picker.ChosenApp);
        }
    }
}
