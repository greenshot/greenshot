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
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Greenshot.Base.Languages;
using Greenshot.Base.Threading;
using Greenshot.Base.Wpf;
using Greenshot.Plugin.CustomUploader.ViewModels;
using log4net;
using Microsoft.Win32;

namespace Greenshot.Plugin.CustomUploader.Views;

/// <summary>
/// The settings page: import and remove uploaders, open, delete and forget uploads.
/// An import is always confirmed: the user sees where the captures go before anything is stored.
/// </summary>
public partial class CustomUploaderSettingsView : UserControl
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(CustomUploaderSettingsView));
    private static readonly TimeSpan ImportArmDelay = TimeSpan.FromSeconds(1);
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
        var dialog = new OpenFileDialog
        {
            Filter = UploaderTexts.ImportFileFilter,
            CheckFileExists = true,
            Multiselect = false
        };
        var window = Window.GetWindow(this);
        if ((window != null ? dialog.ShowDialog(window) : dialog.ShowDialog()) != true)
        {
            return;
        }

        var definition = CustomUploaderSettingsViewModel.ReadFile(dialog.FileName, out string error);
        if (definition == null)
        {
            ThemedMessageBox.Show(window, string.Format(UploaderTexts.ImportFailed, Path.GetFileName(dialog.FileName), error), UploaderTexts.ImportTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // The gate of the import: Cancel is the default, the buttons only react after a moment, so a double click doesn't import
        // Uploads which can't be deleted: the user can have the capture shown before each upload
        bool offerReminder = CustomUploaderSettingsViewModel.HasNoDeletion(definition);
        var buttons = offerReminder
            ? new[] { UploaderTexts.ImportWithReminder, UploaderTexts.ImportButton, Texts.Core.Cancel }
            : new[] { UploaderTexts.ImportButton, Texts.Core.Cancel };
        int cancel = buttons.Length - 1;
        int choice = ThemedMessageBox.ShowChoice(window, UploaderTexts.ImportTitle, _viewModel.DescribeImport(definition), MessageBoxImage.Warning,
            buttons, defaultIndex: cancel, cancelIndex: cancel, armDelay: ImportArmDelay);
        if (choice >= 0 && choice < cancel)
        {
            _viewModel.Import(definition, remindBeforeUpload: offerReminder && choice == 0);
        }
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        var uploader = _viewModel.SelectedUploader;
        if (uploader == null) return;
        if (ThemedMessageBox.Show(Window.GetWindow(this), string.Format(UploaderTexts.RemoveQuestion, uploader.Name), UploaderTexts.UploadersTitle, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            _viewModel.RemoveSelectedUploader();
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
