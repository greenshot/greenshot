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
using Greenshot.Base.Definitions;
using Greenshot.Base.Wpf;
using Greenshot.Plugin.SmartText.Detection;
using Greenshot.Plugin.SmartText.ViewModels;

namespace Greenshot.Plugin.SmartText.Views;

/// <summary>
/// Add or change an own pattern. Save checks it like an imported file, the window stays open until it passes or the user cancels.
/// </summary>
public partial class SmartTextPatternWindow : Window
{
    private readonly SmartTextPatternViewModel _viewModel;
    private readonly Action<SmartTextPattern, string> _save;

    internal SmartTextPatternWindow(SmartTextPatternViewModel viewModel, Action<SmartTextPattern, string> save)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _save = save ?? throw new ArgumentNullException(nameof(save));
        DataContext = viewModel;
        InitializeComponent();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _viewModel.RevealProblems();
            if (_viewModel.HasProblems) throw new DefinitionFileException(_viewModel.Problems);
            _save(_viewModel.ToDefinition(), _viewModel.OriginalId);
            DialogResult = true;
        }
        catch (DefinitionFileException ex)
        {
            ThemedMessageBox.Show(this, string.Format(SmartTextPlugin.Language.EditorFailed, ex.Message), _viewModel.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
