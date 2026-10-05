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
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Greenshot.Base.Core;
using Greenshot.Base.Threading;
using Greenshot.Base.Wpf;
using Greenshot.Base.Languages;
using Greenshot.Plugin.Imgur.Api;

namespace Greenshot.Plugin.Imgur.Views;

/// <summary>
/// The history of the Imgur uploads: open, copy the links, delete them on Imgur or clear the local history.
/// One window at a time, created and used on the UI thread.
/// </summary>
public partial class ImgurHistoryWindow : Window
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ImgurHistoryWindow));
    private static readonly IImgurConfiguration Config = IniConfigHelper.EnsureSection<IImgurConfiguration>(() => new ImgurConfigurationImpl());
    private static ImgurHistoryWindow _instance;

    // Only accessed on the UI thread: a second request while the history loads is ignored
    private static bool _isLoading;

    private string _sortProperty = nameof(HistoryRow.Timestamp);
    private ListSortDirection _sortDirection = ListSortDirection.Descending;

    /// <summary>
    /// Load the history (if needed) and show it, call on the UI thread.
    /// </summary>
    public static async Task ShowHistoryAsync()
    {
        if (_isLoading)
        {
            return;
        }

        if (ImgurUtils.IsHistoryLoadingNeeded())
        {
            _isLoading = true;
            try
            {
                await UserInteraction.Current.RunWithProgressAsync("Imgur " + Texts.Get<IImgurLanguage>().History,
                    (progress, token) => ImgurUtils.LoadHistoryAsync(token), CancellationToken.None);
            }
            finally
            {
                _isLoading = false;
            }
        }

        _instance ??= new ImgurHistoryWindow();
        if (!_instance.IsVisible)
        {
            _instance.Show();
        }

        _instance.Redraw();
        _instance.Activate();
    }

    /// <summary>
    /// A window with the current history; use <see cref="ShowHistoryAsync"/> to show the one window of the history.
    /// </summary>
    public ImgurHistoryWindow()
    {
        InitializeComponent();
        try
        {
            Icon = GreenshotResources.GetGreenshotIcon().ToBitmapSource();
        }
        catch (Exception ex)
        {
            Log.Debug("Could not set the window icon", ex);
        }

        KeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        };
        Closed += (s, e) =>
        {
            if (_instance == this)
            {
                _instance = null;
            }
        };
        Redraw();
    }

    /// <summary>
    /// A line in the list
    /// </summary>
    public sealed class HistoryRow
    {
        public HistoryRow(ImgurInfo info)
        {
            Info = info;
        }

        public ImgurInfo Info { get; }

        public string Title => Info.Title;

        public string Hash => Info.Hash;

        public DateTime Timestamp => Info.Timestamp;

        public string Date => Info.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", DateTimeFormatInfo.InvariantInfo);
    }

    private IEnumerable<ImgurInfo> SelectedInfos => UploadsList.SelectedItems.OfType<HistoryRow>().Select(row => row.Info).ToList();

    private void Redraw()
    {
        var rows = (Config.RuntimeImgurHistory?.Values ?? Enumerable.Empty<ImgurInfo>()).Select(info => new HistoryRow(info)).ToList();
        UploadsList.ItemsSource = rows;
        ApplySort();
        if (rows.Count > 0)
        {
            UploadsList.SelectedIndex = 0;
        }

        UpdateSelection();
    }

    private void ApplySort()
    {
        var view = CollectionViewSource.GetDefaultView(UploadsList.ItemsSource);
        if (view == null)
        {
            return;
        }

        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription(_sortProperty, _sortDirection));
    }

    private void UpdateSelection()
    {
        var selected = UploadsList.SelectedItems.OfType<HistoryRow>().ToList();
        bool hasSelection = selected.Count > 0;
        DeleteButton.IsEnabled = hasSelection;
        OpenButton.IsEnabled = hasSelection;
        ClipboardButton.IsEnabled = hasSelection;
        Thumbnail.Source = selected.Count == 1 ? selected[0].Info.Image?.ToBitmapSource() : null;
    }

    private void UploadsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateSelection();
    }

    private void UploadsList_HeaderClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not GridViewColumnHeader { Column: { } column })
        {
            return;
        }

        string property = (column.DisplayMemberBinding as Binding)?.Path?.Path;
        if (property == nameof(HistoryRow.Date))
        {
            // The date text sorts like the time, but the time is exact
            property = nameof(HistoryRow.Timestamp);
        }

        if (string.IsNullOrEmpty(property))
        {
            return;
        }

        _sortDirection = property == _sortProperty && _sortDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
        _sortProperty = property;
        ApplySort();
    }

    private void UploadsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (UploadsList.SelectedItems.Count > 0)
        {
            OpenButton_Click(sender, e);
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var toDelete = new List<ImgurInfo>();
        foreach (var imgurInfo in SelectedInfos)
        {
            var result = ThemedMessageBox.Show(this, string.Format(Texts.Get<IImgurLanguage>().DeleteQuestion, imgurInfo.Title),
                string.Format(Texts.Get<IImgurLanguage>().DeleteTitle, imgurInfo.Hash), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                toDelete.Add(imgurInfo);
            }
        }

        if (toDelete.Count > 0)
        {
            AsyncCommand.Run(() => DeleteAsync(toDelete), "Delete Imgur images");
        }
    }

    private async Task DeleteAsync(IList<ImgurInfo> toDelete)
    {
        Thumbnail.Source = null;
        foreach (var imgurInfo in toDelete)
        {
            try
            {
                await UserInteraction.Current.RunWithProgressAsync("Imgur", (progress, token) => ImgurUtils.DeleteImgurImageAsync(imgurInfo, token), CancellationToken.None);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Warn("Problem communicating with Imgur: ", ex);
            }

            imgurInfo.Dispose();
        }

        Redraw();
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var imgurInfo in SelectedInfos)
        {
            try
            {
                System.Diagnostics.Process.Start(imgurInfo.Page);
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't open " + imgurInfo.Page, ex);
            }
        }
    }

    private void ClipboardButton_Click(object sender, RoutedEventArgs e)
    {
        var links = new StringBuilder();
        foreach (var imgurInfo in SelectedInfos)
        {
            links.AppendLine(Config.UsePageLink ? imgurInfo.Page : imgurInfo.Original);
        }

        if (links.Length > 0)
        {
            ClipboardHelper.SetClipboardData(links.ToString());
        }
    }

    private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        var result = ThemedMessageBox.Show(this, Texts.Get<IImgurLanguage>().ClearQuestion, "Imgur", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        Config.RuntimeImgurHistory?.Clear();
        Config.ImgurUploadHistory.Clear();
        Redraw();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
