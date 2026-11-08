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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Greenshot.Base.Definitions;
using Greenshot.Base.Languages;
using Greenshot.Base.Threading;
using Greenshot.Base.Wpf;
using Greenshot.Plugin.CustomUploader.Sxcu;
using Greenshot.Plugin.CustomUploader.ViewModels;
using log4net;

namespace Greenshot.Plugin.CustomUploader.Views;

/// <summary>
/// The settings page: import and remove uploaders, open, delete and forget uploads.
/// An import is always confirmed: the user sees where the captures go before anything is stored.
/// </summary>
public partial class CustomUploaderSettingsView : UserControl
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(CustomUploaderSettingsView));
    private readonly CustomUploaderSettingsViewModel _viewModel;

    internal CustomUploaderSettingsView(CustomUploaderSettingsViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = viewModel;
        InitializeComponent();
        // An upload while the page is open shows up in the list
        Loaded += (_, _) => UploaderStore.Changed += OnStoreChanged;
        Unloaded += (_, _) => UploaderStore.Changed -= OnStoreChanged;
    }

    private void OnStoreChanged(object sender, EventArgs e)
    {
        Dispatcher.InvokeAsync(_viewModel.Refresh).Task.FireAndLog("Refresh the custom uploader settings", Log);
    }

    private static ICustomUploaderLanguage UploaderTexts => Texts.Get<ICustomUploaderLanguage>();

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        string path = DefinitionImport.PickFile(window, UploaderTexts.ImportFileFilter);
        if (path == null)
        {
            return;
        }

        var definition = CustomUploaderSettingsViewModel.ReadFile(path, out string error);
        if (definition == null)
        {
            ThemedMessageBox.Show(window, string.Format(UploaderTexts.ImportFailed, Path.GetFileName(path), error), UploaderTexts.ImportTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        definition = AskForValues(window, definition, Path.GetFileName(path));
        if (definition == null)
        {
            return;
        }

        // The gate of the import: Cancel is the default, the buttons only react after a moment, so a double click doesn't import
        // Uploads which can't be deleted: the user can have the capture shown before each upload
        bool offerReminder = CustomUploaderSettingsViewModel.HasNoDeletion(definition);
        var buttons = offerReminder
            ? new[] { UploaderTexts.ImportWithReminder, UploaderTexts.ImportButton, Texts.Core.Cancel }
            : new[] { UploaderTexts.ImportButton, Texts.Core.Cancel };
        int choice = DefinitionImport.Confirm(window, UploaderTexts.ImportTitle, _viewModel.DescribeImport(definition), buttons);
        if (choice >= 0 && choice < buttons.Length - 1)
        {
            try
            {
                _viewModel.Import(definition, remindBeforeUpload: offerReminder && choice == 0);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // The Uploaders folder can't be written
                Log.Warn("Can't write the uploader file", ex);
                ThemedMessageBox.Show(window, string.Format(UploaderTexts.ImportFailed, Path.GetFileName(path), ex.Message), UploaderTexts.ImportTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    /// <summary>
    /// An uploader file added or changed outside Greenshot is only used after the user saw where it sends the captures, like an import
    /// </summary>
    private void Approve_Click(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.CanApproveSelectedUploader) return;
        var definition = _viewModel.GetSelectedUploader();
        if (definition == null) return;
        var buttons = new[] { UploaderTexts.ApproveButton, Texts.Core.Cancel };
        if (DefinitionImport.Confirm(Window.GetWindow(this), UploaderTexts.ApproveTitle, CustomUploaderSettingsViewModel.DescribeApproval(definition), buttons) == 0)
        {
            _viewModel.ApproveSelectedUploader();
        }
    }

    /// <summary>
    /// Let the user fill in the values (keys) of the uploader, until they pass the checks. Null when the user cancels.
    /// </summary>
    private static UploaderDefinition AskForValues(Window owner, UploaderDefinition definition, string fileName)
    {
        var values = CustomUploaderSettingsViewModel.GetValues(definition);
        while (true)
        {
            var valuesWindow = new UploaderValuesWindow(string.Format(UploaderTexts.ValuesTitle, definition.Name), values) { Owner = owner };
            if (valuesWindow.ShowDialog() != true)
            {
                return null;
            }

            // Ask once when a value still looks like the text of the file (YOUR_API_KEY), No goes back to the values
            var placeholders = values.Where(value => value.LooksLikePlaceholder).Select(value => value.Label).ToList();
            if (placeholders.Count > 0 && ThemedMessageBox.Show(owner, string.Format(UploaderTexts.ValuesPlaceholderQuestion, string.Join(", ", placeholders)),
                    UploaderTexts.ImportTitle, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                continue;
            }

            try
            {
                return CustomUploaderSettingsViewModel.WithValues(definition, values);
            }
            catch (UploaderFileException ex)
            {
                ThemedMessageBox.Show(owner, string.Format(UploaderTexts.ImportFailed, fileName, ex.Message), UploaderTexts.ImportTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        var uploader = _viewModel.SelectedUploader;
        if (uploader == null) return;
        if (ThemedMessageBox.Show(Window.GetWindow(this), string.Format(UploaderTexts.RemoveQuestion, uploader.Name), UploaderTexts.UploadersTitle, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            _viewModel.RemoveSelectedUploader();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn("Can't delete the uploader file", ex);
            ThemedMessageBox.Show(Window.GetWindow(this), ex.Message, UploaderTexts.UploadersTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        CustomUploaderSettingsViewModel.OpenInBrowser(_viewModel.SelectedUpload?.Url);
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        var upload = _viewModel.SelectedUpload;
        if (upload?.DeletionUrl == null) return;
        // The service decides how its deletion link works, most show a page which deletes the upload
        if (ThemedMessageBox.Show(Window.GetWindow(this), string.Format(UploaderTexts.DeleteQuestion, upload.Url), UploaderTexts.UploadsTitle, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            CustomUploaderSettingsViewModel.OpenInBrowser(upload.DeletionUrl);
        }
    }

    private void Forget_Click(object sender, RoutedEventArgs e)
    {
        var upload = _viewModel.SelectedUpload;
        if (upload == null) return;
        string question = upload.DeletionUrl == null ? UploaderTexts.ForgetQuestion : UploaderTexts.ForgetQuestion + "\n\n" + UploaderTexts.ForgetLosesDeletion;
        if (ThemedMessageBox.Show(Window.GetWindow(this), question, UploaderTexts.UploadsTitle, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            _viewModel.ForgetSelectedUpload();
        }
    }
}
