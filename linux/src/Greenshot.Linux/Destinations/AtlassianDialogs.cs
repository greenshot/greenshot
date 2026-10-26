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

using System.Globalization;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Greenshot.Linux.Core;
using Greenshot.Linux.Core.Http;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Helpers;
using Greenshot.Linux.Plugins.Atlassian;

namespace Greenshot.Linux.Destinations;

/// <summary>
/// The windows of Jira and Confluence: sign in, choose the issue or the page, an attachment name which is used
/// </summary>
internal sealed class AtlassianDialogs : IAtlassianDialogs
{
    private static string T(string section, string key, params object?[] args) => Texts.Label(Texts.Get(section, key, args));

    public ServerSignInPrompt SignIn => (request, cancellationToken) => Dispatcher.UIThread.InvokeAsync(() => SignInWindow.ShowAsync(request, cancellationToken));

    public Task<JiraUploadChoice?> ChooseJiraIssueAsync(JiraUploadRequest request) =>
        Dispatcher.UIThread.InvokeAsync(() => new JiraUploadWindow(request).ShowAsync());

    public Task<ConfluenceUploadChoice?> ChooseConfluencePageAsync(ConfluenceUploadRequest request) =>
        Dispatcher.UIThread.InvokeAsync(() => new ConfluenceUploadWindow(request).ShowAsync());

    public Task<AttachmentExistsChoice?> AttachmentExistsAsync(AttachmentExistsRequest request) =>
        Dispatcher.UIThread.InvokeAsync(() => ShowAttachmentExistsAsync(request));

    /// <summary>
    /// A window with the result of its buttons; closing it is canceling
    /// </summary>
    private static Window CreateWindow<T>(string title, TaskCompletionSource<T?> result, bool resizable = false)
    {
        var window = new Window
        {
            Title = title,
            Icon = AppIcons.Window,
            SizeToContent = resizable ? SizeToContent.Manual : SizeToContent.WidthAndHeight,
            CanResize = resizable,
            Topmost = true,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
        };
        window.Closed += (_, _) => result.TrySetResult(default);
        return window;
    }

    private static StackPanel Buttons(params Button[] buttons)
    {
        var panel = Ui.Horizontal(buttons);
        panel.HorizontalAlignment = HorizontalAlignment.Right;
        return panel;
    }

    private static TextBlock ErrorText() => new() { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.IndianRed, MaxWidth = 520, IsVisible = false };

    private static void ShowError(TextBlock error, string? message)
    {
        error.Text = message;
        error.IsVisible = !string.IsNullOrEmpty(message);
    }

    private static bool IsConnectionError(Exception ex) =>
        ex is HttpRequestException or UnauthorizedAccessException or JsonException or TaskCanceledException or UriFormatException;

    #region Sign in

    private static class SignInWindow
    {
        private enum Kind
        {
            Cloud,
            Password,
            Token,
        }

        public static Task<ServerSignInAnswer?> ShowAsync(ServerSignInRequest request, CancellationToken cancellationToken)
        {
            var result = new TaskCompletionSource<ServerSignInAnswer?>();
            var window = CreateWindow(Texts.Core("signin_title", request.ServiceName), result);
            bool hasCloud = request.Options.ApiTokenUrl != null;
            var kinds = new List<(Kind Kind, string Text)>();
            if (hasCloud)
            {
                kinds.Add((Kind.Cloud, Texts.Core("signin_kind_cloud")));
            }

            kinds.Add((Kind.Password, Texts.Core("signin_kind_password")));
            kinds.Add((Kind.Token, Texts.Core("signin_kind_token")));
            var kind = new ComboBox { ItemsSource = kinds.Select(k => k.Text).ToList(), Width = 320 };
            var initial = request.Credentials?.IsToken == true ? Kind.Token
                : hasCloud && (request.Options.IsCloudUrl(request.Url) || request.Credentials?.UserName?.Contains('@') == true) ? Kind.Cloud
                : Kind.Password;
            kind.SelectedIndex = kinds.FindIndex(k => k.Kind == initial);

            var user = new TextBox { Text = request.Credentials?.UserName ?? string.Empty, Width = 320 };
            var secret = new TextBox { Text = request.Credentials?.Secret ?? string.Empty, Width = 320, PasswordChar = '•' };
            var userLabel = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
            var secretLabel = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
            var userRow = Row(userLabel, user);
            var createToken = new Button { Content = Texts.Core("signin_create_api_token"), Classes = { "link" } };
            createToken.Click += (_, _) => ProcessRunner.StartDetached("xdg-open", request.Options.ApiTokenUrl!);
            var remember = Ui.Check(Texts.Linux("signin_remember"), request.Remember);
            var error = ErrorText();
            ShowError(error, request.Error);

            void Update()
            {
                var selected = kinds[Math.Max(0, kind.SelectedIndex)].Kind;
                userRow.IsVisible = selected != Kind.Token;
                userLabel.Text = Texts.Core(selected == Kind.Cloud ? "signin_email" : "signin_user_name");
                secretLabel.Text = Texts.Core(selected switch
                {
                    Kind.Cloud => "signin_api_token",
                    Kind.Token => "signin_access_token",
                    _ => "signin_password",
                });
                createToken.IsVisible = selected == Kind.Cloud;
            }

            kind.SelectionChanged += (_, _) => Update();
            Update();

            var signIn = new Button { Content = Texts.Core("signin_button"), IsDefault = true };
            var cancel = new Button { Content = Texts.Linux("cancel"), IsCancel = true };
            cancel.Click += (_, _) => window.Close();
            signIn.Click += async (_, _) =>
            {
                bool isToken = kinds[Math.Max(0, kind.SelectedIndex)].Kind == Kind.Token;
                var credentials = new ServerCredentials(isToken ? null : user.Text, secret.Text);
                if (string.IsNullOrEmpty(credentials.Secret) || (!isToken && credentials.UserName == null))
                {
                    ShowError(error, Texts.Core("signin_missing"));
                    return;
                }

                signIn.IsEnabled = false;
                ShowError(error, Texts.Core("signin_connecting"));
                try
                {
                    await request.ConnectAsync(credentials, cancellationToken);
                    result.TrySetResult(new ServerSignInAnswer(credentials, remember.IsChecked == true));
                    window.Close();
                }
                catch (Exception ex) when (IsConnectionError(ex))
                {
                    ShowError(error, ServerSignIn.DescribeError(ex));
                }
                finally
                {
                    signIn.IsEnabled = true;
                }
            };

            window.Content = Ui.Page(
                Ui.Note(request.Url),
                Ui.Row(Texts.Linux("signin_kind"), kind),
                userRow,
                Row(secretLabel, secret),
                createToken,
                remember,
                error,
                Buttons(signIn, cancel));
            window.Opened += (_, _) => (string.IsNullOrEmpty(user.Text) && userRow.IsVisible ? user : secret).Focus();
            window.Show();
            window.Activate();
            return result.Task;
        }

        private static Grid Row(TextBlock label, Control control)
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("220,*") };
            Grid.SetColumn(control, 1);
            grid.Children.Add(label);
            grid.Children.Add(control);
            return grid;
        }
    }

    #endregion

    #region Jira

    /// <summary>
    /// The Jira upload window of Windows: the favourite filters with their issues, or an issue key, the file name and a comment
    /// </summary>
    private sealed class JiraUploadWindow
    {
        private readonly JiraUploadRequest _request;
        private readonly TaskCompletionSource<JiraUploadChoice?> _result = new();
        private readonly Window _window;
        private readonly ComboBox _filters = new() { MinWidth = 300 };
        private readonly ListBox _issues = new() { Height = 260 };
        private readonly TextBox _key = new() { Width = 160 };
        private readonly TextBlock _keySummary = new() { VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 420 };
        private readonly TextBox _filename = new() { Width = 420 };
        private readonly TextBox _comment = new() { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 90 };
        private readonly Button _upload = new() { Content = T("Jira", "upload"), IsDefault = true, IsEnabled = false };
        private readonly TextBlock _error = ErrorText();
        private readonly DispatcherTimer _keyLookup = new() { Interval = TimeSpan.FromMilliseconds(400) };
        private JiraIssue? _selected;
        private bool _closed;

        public JiraUploadWindow(JiraUploadRequest request)
        {
            _request = request;
            _window = CreateWindow(T("Jira", "upload_menu_item"), _result, resizable: true);
            _window.Width = 820;
            _window.Height = 640;
            _filename.Text = request.Filename;
            _issues.ItemTemplate = new FuncDataTemplate<JiraIssue>((issue, _) => IssueRow(issue), true);

            var insertCapture = new Button { Content = T("Jira", "insert_capture") };
            insertCapture.Click += (_, _) =>
            {
                int caret = _comment.CaretIndex;
                _comment.Text = (_comment.Text ?? string.Empty).Insert(caret, JiraDestination.CapturePlaceholder);
                _comment.Focus();
                _comment.CaretIndex = caret + JiraDestination.CapturePlaceholder.Length;
            };
            var cancel = new Button { Content = Texts.Linux("cancel"), IsCancel = true };
            cancel.Click += (_, _) => _window.Close();
            _upload.Click += (_, _) =>
            {
                if (_selected != null)
                {
                    _result.TrySetResult(new JiraUploadChoice(_selected, _filename.Text ?? string.Empty, _comment.Text));
                    _window.Close();
                }
            };

            _filters.SelectionChanged += async (_, _) =>
            {
                if (_filters.SelectedItem is JiraFilter filter)
                {
                    await LoadFilterAsync(filter);
                }
            };
            _issues.SelectionChanged += (_, _) =>
            {
                if (_issues.SelectedItem is JiraIssue issue)
                {
                    Select(issue);
                }
            };
            _key.TextChanged += (_, _) =>
            {
                if (_selected?.Key != _key.Text?.Trim())
                {
                    _keyLookup.Stop();
                    _keyLookup.Start();
                }
            };
            _keyLookup.Tick += async (_, _) =>
            {
                _keyLookup.Stop();
                await LookUpKeyAsync();
            };
            _window.Closed += (_, _) =>
            {
                _closed = true;
                _keyLookup.Stop();
            };

            var commentHeader = new DockPanel();
            DockPanel.SetDock(insertCapture, Dock.Right);
            commentHeader.Children.Add(insertCapture);
            commentHeader.Children.Add(new TextBlock { Text = T("Jira", "label_comment"), VerticalAlignment = VerticalAlignment.Bottom });

            var content = Ui.Page(
                Ui.Row(T("Jira", "label_jirafilter"), _filters),
                new StackPanel { Spacing = 2, Children = { IssueHeader(), _issues } },
                Ui.Row(T("Jira", "label_jira"), Ui.Horizontal(_key, _keySummary)),
                Ui.Row(T("Jira", "label_filename"), _filename),
                commentHeader,
                _comment,
                _error,
                Buttons(_upload, cancel));
            _window.Content = new ScrollViewer { Content = content };
        }

        public Task<JiraUploadChoice?> ShowAsync()
        {
            _window.Show();
            _window.Activate();
            _ = LoadAsync();
            return _result.Task;
        }

        private static Grid Columns(params Control[] cells)
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("100,110,*,150,90") };
            for (int i = 0; i < cells.Length; i++)
            {
                Grid.SetColumn(cells[i], i);
                grid.Children.Add(cells[i]);
            }

            return grid;
        }

        private static TextBlock Cell(string? text, FontWeight weight = FontWeight.Normal) =>
            new() { Text = text ?? string.Empty, FontWeight = weight, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 0, 8, 0) };

        private static Control IssueHeader()
        {
            var header = Columns(
                Cell(T("Jira", "column_id"), FontWeight.SemiBold),
                Cell(T("Jira", "column_issueType"), FontWeight.SemiBold),
                Cell(T("Jira", "column_summary"), FontWeight.SemiBold),
                Cell(T("Jira", "column_assignee"), FontWeight.SemiBold),
                Cell(T("Jira", "column_created"), FontWeight.SemiBold));
            header.Margin = new Thickness(12, 0, 12, 0);
            return header;
        }

        private static Control IssueRow(JiraIssue issue) => Columns(
            Cell(issue.Key),
            Cell(issue.Fields?.IssueType?.Name),
            Cell(issue.Fields?.Summary),
            Cell(issue.Fields?.Assignee?.DisplayName),
            Cell(issue.Fields?.Created?.ToLocalTime().ToString("d", CultureInfo.CurrentCulture)));

        private void Select(JiraIssue? issue)
        {
            _selected = issue;
            _upload.IsEnabled = issue != null;
            _keySummary.Text = issue?.Fields?.Summary ?? string.Empty;
            if (issue != null && _key.Text != issue.Key)
            {
                _key.Text = issue.Key;
                // The issue is known, no need to look the key up
                _keyLookup.Stop();
            }
        }

        private async Task LoadAsync()
        {
            Select(_request.SuggestedIssue);
            try
            {
                var filters = await _request.Connector.GetFavoriteFiltersAsync();
                if (_closed)
                {
                    return;
                }

                _filters.ItemsSource = filters;
                if (filters.Count > 0)
                {
                    // Selecting the filter loads its issues
                    _filters.SelectedIndex = 0;
                }
            }
            catch (Exception ex) when (IsConnectionError(ex))
            {
                Log.Error("Loading the Jira filters failed", ex);
                ShowError(_error, T("Jira", "login_error", ex.Message));
            }
        }

        private async Task LoadFilterAsync(JiraFilter filter)
        {
            try
            {
                var issues = await _request.Connector.SearchAsync(filter);
                // Another filter was selected in the meantime
                if (!_closed && ReferenceEquals(_filters.SelectedItem, filter))
                {
                    _issues.ItemsSource = issues;
                    ShowError(_error, null);
                }
            }
            catch (Exception ex) when (IsConnectionError(ex))
            {
                Log.Error($"Jira filter {filter.Name} failed", ex);
                ShowError(_error, ex.Message);
            }
        }

        private async Task LookUpKeyAsync()
        {
            string key = (_key.Text ?? string.Empty).Trim().ToUpperInvariant();
            if (key.Length == 0)
            {
                Select(null);
                return;
            }

            var issue = (_issues.ItemsSource as IEnumerable<JiraIssue>)?.FirstOrDefault(i => i.Key == key);
            if (issue == null)
            {
                try
                {
                    issue = await _request.Connector.GetIssueAsync(key);
                }
                catch (Exception ex) when (IsConnectionError(ex))
                {
                    Log.Debug($"Looking up {key} failed: {ex.Message}");
                }
            }

            // The user typed on in the meantime
            if (!_closed && (_key.Text ?? string.Empty).Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                _selected = issue;
                _upload.IsEnabled = issue != null;
                _keySummary.Text = issue?.Fields?.Summary ?? string.Empty;
            }
        }
    }

    #endregion

    #region Confluence

    /// <summary>
    /// The Confluence upload window of Windows: search the pages of a space or browse the page trees, and the file name
    /// </summary>
    private sealed class ConfluenceUploadWindow
    {
        private readonly ConfluenceUploadRequest _request;
        private readonly TaskCompletionSource<ConfluenceUploadChoice?> _result = new();
        private readonly Window _window;
        private readonly Button _upload = new() { Content = T("Confluence", "upload"), IsDefault = true, IsEnabled = false };
        private readonly TextBox _filename = new() { Width = 420 };
        private readonly TextBlock _error = ErrorText();
        private readonly TextBlock _selectedText = new() { TextTrimming = TextTrimming.CharacterEllipsis, FontWeight = FontWeight.SemiBold };
        private ConfluencePage? _selected;

        public ConfluenceUploadWindow(ConfluenceUploadRequest request)
        {
            _request = request;
            _window = CreateWindow(T("Confluence", "upload_menu_item"), _result, resizable: true);
            _window.Width = 760;
            _window.Height = 620;
            _filename.Text = request.Filename;
            var spaces = request.Spaces.Where(space => request.Settings.IncludePersonSpaces || !space.IsPersonal).ToList();

            var cancel = new Button { Content = Texts.Linux("cancel"), IsCancel = true };
            cancel.Click += (_, _) => _window.Close();
            _upload.Click += (_, _) =>
            {
                if (_selected != null && !string.IsNullOrWhiteSpace(_filename.Text))
                {
                    _result.TrySetResult(new ConfluenceUploadChoice(_selected, _filename.Text));
                    _window.Close();
                }
            };

            var tabs = new TabControl
            {
                Height = 380,
                ItemsSource = new[]
                {
                    new TabItem { Header = T("Confluence", "search_pages"), FontSize = 15, Content = SearchPage(spaces) },
                    new TabItem { Header = T("Confluence", "browse_pages"), FontSize = 15, Content = BrowsePage(spaces) },
                },
            };
            _window.Content = Ui.Page(
                tabs,
                _selectedText,
                Ui.Row(T("Confluence", "filename"), _filename),
                _error,
                Buttons(_upload, cancel));
        }

        public Task<ConfluenceUploadChoice?> ShowAsync()
        {
            _window.Show();
            _window.Activate();
            return _result.Task;
        }

        private void Select(ConfluencePage? page)
        {
            _selected = page;
            _upload.IsEnabled = page != null;
            _selectedText.Text = page == null ? string.Empty : $"{page.SpaceKey}: {page.Title}";
        }

        private Control SearchPage(List<ConfluenceSpaceInfo> spaces)
        {
            var space = new ComboBox { ItemsSource = spaces, MinWidth = 260 };
            space.SelectedItem = spaces.FirstOrDefault(s => s.Key == _request.Settings.SearchSpaceKey) ?? spaces.FirstOrDefault();
            var text = new TextBox { Width = 300 };
            var search = new Button { Content = T("Confluence", "search"), IsEnabled = false };
            var results = new ListBox { Height = 250 };
            results.ItemTemplate = new FuncDataTemplate<ConfluencePage>((page, _) => new TextBlock { Text = page.Title }, true);
            results.SelectionChanged += (_, _) => Select(results.SelectedItem as ConfluencePage);
            text.TextChanged += (_, _) => search.IsEnabled = !string.IsNullOrWhiteSpace(text.Text);

            async Task SearchAsync()
            {
                var spaceKey = (space.SelectedItem as ConfluenceSpaceInfo)?.Key;
                _request.Settings.SearchSpaceKey = spaceKey;
                search.IsEnabled = false;
                try
                {
                    var pages = await _request.Connector.SearchPagesAsync(text.Text ?? string.Empty, spaceKey);
                    results.ItemsSource = pages.OrderBy(p => p.Title, StringComparer.CurrentCultureIgnoreCase).ToList();
                    ShowError(_error, null);
                }
                catch (Exception ex) when (IsConnectionError(ex))
                {
                    Log.Error("Confluence search failed", ex);
                    ShowError(_error, ex.Message);
                }
                finally
                {
                    search.IsEnabled = !string.IsNullOrWhiteSpace(text.Text);
                }
            }

            search.Click += async (_, _) => await SearchAsync();
            text.KeyDown += async (_, e) =>
            {
                if (e.Key == Key.Return && search.IsEnabled)
                {
                    e.Handled = true;
                    await SearchAsync();
                }
            };

            return Ui.Page(
                Ui.Row(T("Confluence", "space"), space),
                Ui.Row(T("Confluence", "search_text"), Ui.Horizontal(text, search)),
                results);
        }

        private Control BrowsePage(List<ConfluenceSpaceInfo> spaces)
        {
            var tree = new TreeView { Height = 330 };
            tree.ItemsSource = spaces.Select(space => LazyItem(space.Name, space, null)).ToList();
            tree.SelectionChanged += (_, _) => Select((tree.SelectedItem as TreeViewItem)?.Tag as ConfluencePage);
            return new Border { Padding = new Thickness(8), Child = tree };
        }

        /// <summary>
        /// A space (its home page is the child) or a page (the child pages), loaded when it's opened
        /// </summary>
        private TreeViewItem LazyItem(string header, ConfluenceSpaceInfo? space, ConfluencePage? page)
        {
            var loading = new TreeViewItem { Header = T("Confluence", "loading"), IsEnabled = false };
            var item = new TreeViewItem { Header = header, Tag = page, ItemsSource = new List<TreeViewItem> { loading } };
            bool loaded = false;
            item.PropertyChanged += async (_, e) =>
            {
                if (e.Property != TreeViewItem.IsExpandedProperty || !item.IsExpanded || loaded)
                {
                    return;
                }

                loaded = true;
                try
                {
                    List<TreeViewItem> children;
                    if (space != null)
                    {
                        var home = await _request.Connector.GetSpaceHomepageAsync(space);
                        children = home == null ? [] : [LazyItem(home.Title, null, home)];
                    }
                    else
                    {
                        var pages = await _request.Connector.GetPageChildrenAsync(page!);
                        children = pages.OrderBy(p => p.Title, StringComparer.CurrentCultureIgnoreCase).Select(p => LazyItem(p.Title, null, p)).ToList();
                    }

                    item.ItemsSource = children;
                }
                catch (Exception ex) when (IsConnectionError(ex))
                {
                    loaded = false;
                    Log.Error("Loading Confluence pages failed", ex);
                    ShowError(_error, ex.Message);
                    item.IsExpanded = false;
                }
            };
            return item;
        }
    }

    #endregion

    private static Task<AttachmentExistsChoice?> ShowAttachmentExistsAsync(AttachmentExistsRequest request)
    {
        var result = new TaskCompletionSource<AttachmentExistsChoice?>();
        var window = CreateWindow(T("Confluence", "attachment_exists_title"), result);
        var newVersion = new RadioButton { Content = T("Confluence", "attachment_new_version"), GroupName = "exists" };
        var otherName = new RadioButton { Content = T("Confluence", "attachment_other_name"), GroupName = "exists", IsChecked = true };
        var name = new TextBox { Text = request.OtherName, Width = 360, Margin = new Thickness(28, 0, 0, 0) };
        otherName.IsCheckedChanged += (_, _) => name.IsEnabled = otherName.IsChecked == true;
        var upload = new Button { Content = T("Confluence", "upload"), IsDefault = true };
        var cancel = new Button { Content = Texts.Linux("cancel"), IsCancel = true };
        cancel.Click += (_, _) => window.Close();
        upload.Click += (_, _) =>
        {
            bool asNewVersion = newVersion.IsChecked == true;
            if (!asNewVersion && string.IsNullOrWhiteSpace(name.Text))
            {
                return;
            }

            result.TrySetResult(new AttachmentExistsChoice(asNewVersion, asNewVersion ? request.Filename : name.Text!.Trim()));
            window.Close();
        };
        window.Content = Ui.Page(
            new TextBlock { Text = Texts.Get("Confluence", "attachment_exists_message", request.PageTitle, request.Filename), TextWrapping = TextWrapping.Wrap, MaxWidth = 480 },
            newVersion,
            otherName,
            name,
            Buttons(upload, cancel));
        window.Show();
        window.Activate();
        return result.Task;
    }
}
