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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Greenshot.Base.Core;
using Greenshot.Base.Languages;
using Greenshot.Base.Threading;
using Greenshot.Base.Wpf;
using Greenshot.SelfService.ViewModels;
using log4net;
using Microsoft.Win32;

namespace Greenshot.SelfService.Views
{
    /// <summary>
    /// The earlier anonymous Imgur uploads with delete, open, save as a web page and forget; the DataContext is an <see cref="ImgurSectionViewModel"/>.
    /// </summary>
    public partial class ImgurUploadsView : UserControl
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ImgurUploadsView));

        public ImgurUploadsView()
        {
            InitializeComponent();
            UpdateButtons();
        }

        private ImgurSectionViewModel ViewModel => DataContext as ImgurSectionViewModel;

        private ImgurUploadViewModel[] SelectedUploads => UploadsList.SelectedItems.OfType<ImgurUploadViewModel>().ToArray();

        private void UpdateButtons()
        {
            DeleteButton.IsEnabled = OpenButton.IsEnabled = UploadsList.SelectedItems.Count > 0;
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateButtons();

        private void OnDeleteClicked(object sender, RoutedEventArgs e)
        {
            var uploads = SelectedUploads;
            if (ViewModel == null || uploads.Length == 0)
            {
                return;
            }

            var answer = ThemedMessageBox.Show(Window.GetWindow(this), string.Format(Texts.SelfService.ImgurDeleteQuestion, uploads.Length),
                Texts.SelfService.ImgurDelete, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer == MessageBoxResult.Yes)
            {
                AsyncCommand.Run(() => ViewModel.DeleteAsync(uploads), "Delete Imgur uploads");
            }
        }

        private void OnOpenClicked(object sender, RoutedEventArgs e)
        {
            foreach (var upload in SelectedUploads)
            {
                ExceptionHelper.OpenUrl(upload.Link);
            }
        }

        private void OnExportClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                FileName = Texts.SelfService.ImgurTitle,
                DefaultExt = ".html",
                Filter = "HTML (*.html)|*.html"
            };
            if (ViewModel == null || dialog.ShowDialog(Window.GetWindow(this)) != true)
            {
                return;
            }

            try
            {
                ViewModel.Export(dialog.FileName);
                ExceptionHelper.OpenUrl(dialog.FileName);
            }
            catch (Exception ex)
            {
                Log.Error("Couldn't save the Imgur uploads", ex);
                ThemedMessageBox.Show(Window.GetWindow(this), ex.Message, Texts.SelfService.ImgurExport, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnForgetClicked(object sender, RoutedEventArgs e)
        {
            var answer = ThemedMessageBox.Show(Window.GetWindow(this), Texts.SelfService.ImgurForgetQuestion, Texts.SelfService.ImgurForget,
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer == MessageBoxResult.Yes)
            {
                ViewModel?.ForgetAll();
            }
        }
    }
}
