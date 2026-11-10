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

using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using Greenshot.Base.Threading;
using Greenshot.Plugin.RapidOcr.ViewModels;

namespace Greenshot.Plugin.RapidOcr.Views;

/// <summary>
/// The download of RapidOCR, see <see cref="RapidOcrDownloadViewModel"/>. Not modal: closing it while the files are downloaded
/// lets the download go on.
/// </summary>
public partial class RapidOcrDownloadWindow : Window
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(RapidOcrDownloadWindow));
    private readonly RapidOcrDownloadViewModel _viewModel;
    private bool _closed;

    internal RapidOcrDownloadWindow(RapidOcrDownloadViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        viewModel.CloseRequested += (_, _) =>
        {
            if (!_closed) Close();
        };
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        Loaded += (_, _) =>
        {
            Activate();
            DownloadButton.Focus();
        };
        Closed += (_, _) =>
        {
            _closed = true;
            viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            viewModel.WindowClosed();
        };
    }

    /// <summary>
    /// The focus moves to the buttons of the new stage
    /// </summary>
    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(RapidOcrDownloadViewModel.Stage) || _closed) return;
        // After the layout, when the buttons of the stage are visible
        Dispatcher.InvokeAsync(() =>
        {
            if (_viewModel.IsDownloading)
            {
                InformButton.Focus();
            }
            else if (_viewModel.IsFinished)
            {
                CloseButton.Focus();
            }
        }, DispatcherPriority.Loaded).Task.FireAndLog("RapidOCR download focus", Log);
    }

    private void Download_Click(object sender, RoutedEventArgs e) => _viewModel.DownloadAsync().FireAndLog("RapidOCR download", Log);

    private void Later_Click(object sender, RoutedEventArgs e) => _viewModel.Decline();

    private void Inform_Click(object sender, RoutedEventArgs e) => _viewModel.InformWhenDone();

    private void Cancel_Click(object sender, RoutedEventArgs e) => _viewModel.Cancel();

    private void Close_Click(object sender, RoutedEventArgs e) => _viewModel.Close();
}
