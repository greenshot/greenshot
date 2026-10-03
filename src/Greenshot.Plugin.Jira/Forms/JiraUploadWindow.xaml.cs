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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Dapplo.Jira.Entities;
using Greenshot.Base.Core;
using Greenshot.Base.Threading;
using Greenshot.Base.Wpf;
using Greenshot.Base.Languages;

namespace Greenshot.Plugin.Jira.Forms;

/// <summary>
/// The view of a <see cref="JiraUploadRequest"/>: pick the issue (from a favorite filter, the recent issues or by key),
/// the filename and an optional comment.
/// </summary>
public partial class JiraUploadWindow : Window
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(JiraUploadWindow));
    private readonly JiraConnector _jiraConnector;
    private readonly DispatcherTimer _keyLookupTimer;
    private IssueV2 _selectedIssue;
    private bool _closed;
    private string _sortProperty;
    private ListSortDirection _sortDirection = ListSortDirection.Ascending;

    public JiraUploadWindow(JiraConnector jiraConnector)
    {
        _jiraConnector = jiraConnector;
        InitializeComponent();
        try
        {
            Icon = GreenshotResources.GetGreenshotIcon().ToBitmapSource();
        }
        catch (Exception ex)
        {
            Log.Debug("Could not set the window icon", ex);
        }

        // Look the typed key up when the user stopped typing for a moment
        _keyLookupTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _keyLookupTimer.Tick += (s, e) =>
        {
            _keyLookupTimer.Stop();
            AsyncCommand.Run(LookUpKeyAsync, "Jira key changed");
        };

        SetConnected(false);
        UploadButton.IsEnabled = false;
        Loaded += (s, e) =>
        {
            Activate();
            if (_jiraConnector != null)
            {
                AsyncCommand.Run(LoadAsync, "Load the Jira window");
            }
        };
        Closed += (s, e) =>
        {
            _closed = true;
            _keyLookupTimer.Stop();
        };
    }

    /// <summary>
    /// Shows the window modally (on the UI thread).
    /// </summary>
    /// <returns>the choice of the user, null when cancelled</returns>
    public static JiraUploadChoice Show(JiraUploadRequest request)
    {
        var jiraConnector = SimpleServiceProvider.Current.GetInstance<JiraConnector>();
        var window = new JiraUploadWindow(jiraConnector);
        window.FilenameBox.Text = request.Filename;
        if (window.ShowDialog() != true || window._selectedIssue == null)
        {
            return null;
        }

        return new JiraUploadChoice(window._selectedIssue, window.FilenameBox.Text, window.CommentBox.Text);
    }

    /// <summary>
    /// A line in the issue list, a search result (Issue); the IssueV2 to upload to is looked up by its key when it is picked
    /// </summary>
    public sealed class IssueRow : INotifyPropertyChanged
    {
        private ImageSource _icon;

        public IssueRow(Issue issue)
        {
            Issue = issue;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Issue Issue { get; }

        /// <summary>
        /// The icon of the issue type, loaded after the row is shown
        /// </summary>
        public ImageSource Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon)));
            }
        }

        public string IssueType => Issue.Fields?.IssueType?.Name;

        public string Key => Issue.Key;

        public DateTimeOffset Created => Issue.Fields?.Created ?? DateTimeOffset.MinValue;

        public string CreatedText => Issue.Fields?.Created?.ToString("d", DateTimeFormatInfo.InvariantInfo) ?? string.Empty;

        public string Assignee => Issue.Fields?.Assignee?.DisplayName;

        public string Reporter => Issue.Fields?.Reporter?.DisplayName;

        public string Summary => Issue.Fields?.Summary;
    }

    private void SetConnected(bool connected)
    {
        FilterBox.IsEnabled = connected;
        IssueList.IsEnabled = connected;
        FilenameBox.IsEnabled = connected;
        CommentBox.IsEnabled = connected;
    }

    private void ShowError(string message, string caption = "Jira")
    {
        if (!_closed)
        {
            ThemedMessageBox.Show(this, message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            if (!_jiraConnector.IsLoggedIn)
            {
                await _jiraConnector.LoginAsync();
            }
        }
        catch (Exception e)
        {
            Log.Error("Error with login.", e);
            ShowError(string.Format(Texts.Get<IJiraLanguage>().LoginError, e.Message));
        }

        if (_closed || !_jiraConnector.IsLoggedIn)
        {
            return;
        }

        try
        {
            SetConnected(true);
            var recent = _jiraConnector.Monitor?.RecentJiras.FirstOrDefault();
            if (recent != null)
            {
                SelectIssue(recent.JiraIssue);
            }

            var filters = await _jiraConnector.GetFavoriteFiltersAsync();
            if (_closed)
            {
                return;
            }

            FilterBox.ItemsSource = filters;
            if (filters.Count > 0)
            {
                // Selecting the filter loads its issues
                FilterBox.SelectedIndex = 0;
            }
        }
        catch (Exception e)
        {
            Log.Error("Error getting favorites.", e);
            ShowError(string.Format(Texts.Get<IJiraLanguage>().LoginError, e.Message));
        }
    }

    /// <summary>
    /// The issue to upload to, its key is shown in the key box
    /// </summary>
    private void SelectIssue(IssueV2 issue)
    {
        _selectedIssue = issue;
        UploadButton.IsEnabled = issue != null;
        if (issue != null && KeyBox.Text != issue.Key)
        {
            KeyBox.Text = issue.Key;
            // The issue is known, no need to look the key up
            _keyLookupTimer.Stop();
        }
    }

    private void FilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FilterBox.SelectedItem is Filter filter)
        {
            AsyncCommand.Run(() => LoadFilterAsync(filter), "Jira filter selected");
        }
    }

    private async Task LoadFilterAsync(Filter filter)
    {
        if (!_jiraConnector.IsLoggedIn)
        {
            return;
        }

        IList<Issue> issues;
        try
        {
            issues = await _jiraConnector.SearchAsync(filter);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            ShowError(ex.Message, "Error in filter");
            return;
        }

        // Another filter was selected in the meantime
        if (_closed || !ReferenceEquals(FilterBox.SelectedItem, filter))
        {
            return;
        }

        var rows = issues.Select(issue => new IssueRow(issue)).ToList();
        IssueList.ItemsSource = rows;
        ApplySort();
        foreach (var row in rows)
        {
            try
            {
                var bitmap = row.Issue.Fields?.IssueType == null ? null : await _jiraConnector.GetIssueTypeBitmapAsync(row.Issue.Fields.IssueType);
                row.Icon = bitmap?.ToBitmapSource();
            }
            catch (Exception ex)
            {
                Log.Warn("Problem loading issue type, ignoring", ex);
            }
        }

    }

    private void ApplySort()
    {
        var view = CollectionViewSource.GetDefaultView(IssueList.ItemsSource);
        if (view == null)
        {
            return;
        }

        view.SortDescriptions.Clear();
        if (!string.IsNullOrEmpty(_sortProperty))
        {
            view.SortDescriptions.Add(new SortDescription(_sortProperty, _sortDirection));
        }
    }

    private void IssueList_HeaderClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not GridViewColumnHeader { Column: { } column })
        {
            return;
        }

        string property = (column.DisplayMemberBinding as Binding)?.Path?.Path ?? nameof(IssueRow.IssueType);
        if (property == nameof(IssueRow.CreatedText))
        {
            // Sort by the time, not the text
            property = nameof(IssueRow.Created);
        }

        _sortDirection = property == _sortProperty && _sortDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
        _sortProperty = property;
        ApplySort();
    }

    private void IssueList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IssueList.SelectedItem is IssueRow row && KeyBox.Text != row.Key)
        {
            // Look the picked issue up right away, the search result isn't the issue to upload to
            KeyBox.Text = row.Key;
            _keyLookupTimer.Stop();
            AsyncCommand.Run(LookUpKeyAsync, "Jira issue picked");
        }
    }

    private void IssueList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (IssueList.SelectedItem is IssueRow && UploadButton.IsEnabled)
        {
            DialogResult = true;
        }
    }

    private void KeyBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_selectedIssue != null && KeyBox.Text == _selectedIssue.Key)
        {
            return;
        }

        // Typed by the user: the issue is unknown until the key is looked up
        _selectedIssue = null;
        UploadButton.IsEnabled = false;
        _keyLookupTimer.Stop();
        _keyLookupTimer.Start();
    }

    private async Task LookUpKeyAsync()
    {
        string key = KeyBox.Text;
        int dashIndex = key.IndexOf('-');
        if (_jiraConnector == null || dashIndex <= 0 || key.Length <= dashIndex + 1)
        {
            return;
        }

        try
        {
            var issue = await _jiraConnector.GetIssueAsync(key);
            // Only if the key wasn't changed in the meantime
            if (!_closed && issue != null && KeyBox.Text == key)
            {
                SelectIssue(issue);
            }
        }
        catch (Exception ex)
        {
            Log.Warn("Error looking up Jira issue", ex);
        }
    }

    private void UploadButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedIssue != null)
        {
            DialogResult = true;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
