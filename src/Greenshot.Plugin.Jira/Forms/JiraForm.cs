/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Jira.Entities;
using Dapplo.Windows.Dpi;
using Greenshot.Base.Controls;
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;

namespace Greenshot.Plugin.Jira.Forms
{
    public partial class JiraForm : Form
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(JiraForm));
        private readonly JiraConnector _jiraConnector;
        private Issue _selectedIssue;
        private readonly GreenshotColumnSorter _columnSorter;
        private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();

        public JiraForm(JiraConnector jiraConnector)
        {
            InitializeComponent();
            Icon = GreenshotResources.GetGreenshotIcon();
            AcceptButton = uploadButton;
            CancelButton = cancelButton;

            InitializeComponentText();

            _columnSorter = new GreenshotColumnSorter();
            jiraListView.ListViewItemSorter = _columnSorter;

            _jiraConnector = jiraConnector;

            ChangeModus(false);

            uploadButton.Enabled = false;
            Load += OnLoad;
        }

        private async void OnLoad(object sender, EventArgs eventArgs)
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
                MessageBox.Show(Language.GetFormattedString("jira", LangKey.login_error, e.Message));
            }

            if (_jiraConnector.IsLoggedIn)
            {
                var filters = await _jiraConnector.GetFavoriteFiltersAsync();
                if (filters.Count > 0)
                {
                    foreach (var filter in filters)
                    {
                        jiraFilterBox.Items.Add(filter);
                    }

                    jiraFilterBox.SelectedIndex = 0;
                }

                ChangeModus(true);
                if (_jiraConnector.Monitor.RecentJiras.Any())
                {
                    _selectedIssue = _jiraConnector.Monitor.RecentJiras.First().JiraIssue;
                    jiraKey.Text = _selectedIssue.Key;
                    uploadButton.Enabled = true;
                }
            }
        }

        private void InitializeComponentText()
        {
            label_jirafilter.Text = Language.GetString("jira", LangKey.label_jirafilter);
            label_comment.Text = Language.GetString("jira", LangKey.label_comment);
            label_filename.Text = Language.GetString("jira", LangKey.label_filename);
        }

        private void ChangeModus(bool enabled)
        {
            jiraFilterBox.Enabled = enabled;
            jiraListView.Enabled = enabled;
            jiraFilenameBox.Enabled = enabled;
            jiraCommentBox.Enabled = enabled;
        }

        public void SetFilename(string filename)
        {
            jiraFilenameBox.Text = filename;
        }

        public Issue GetJiraIssue()
        {
            return _selectedIssue;
        }

        public async Task UploadAsync(IBinaryContainer attachment)
        {
            attachment.Filename = jiraFilenameBox.Text;
            await _jiraConnector.AttachAsync(_selectedIssue.Key, attachment);

            if (!string.IsNullOrEmpty(jiraCommentBox.Text))
            {
                await _jiraConnector.AddCommentAsync(_selectedIssue.Key, jiraCommentBox.Text);
            }
        }

        private async void JiraFilterBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_jiraConnector.IsLoggedIn)
            {
                uploadButton.Enabled = false;
                var filter = (Filter) jiraFilterBox.SelectedItem;
                if (filter == null)
                {
                    return;
                }

                IList<Issue> issues = null;
                try
                {
                    issues = await _jiraConnector.SearchAsync(filter);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    MessageBox.Show(this, ex.Message, "Error in filter", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                jiraListView.Items.Clear();
                if (issues?.Count > 0)
                {
                    jiraListView.Columns.Clear();
                    LangKey[] columns =
                    {
                        LangKey.column_issueType, LangKey.column_id, LangKey.column_created, LangKey.column_assignee, LangKey.column_reporter, LangKey.column_summary
                    };
                    foreach (LangKey column in columns)
                    {
                        if (!Language.TryGetString("jira", column, out var translation))
                        {
                            translation = string.Empty;
                        }

                        jiraListView.Columns.Add(translation);
                    }

                    var scaledIconSize = DpiCalculator.ScaleWithDpi(CoreConfig.IconSize, NativeDpiMethods.GetDpi(Handle));
                    var imageList = new ImageList
                    {
                        ImageSize = scaledIconSize
                    };
                    jiraListView.SmallImageList = imageList;
                    jiraListView.LargeImageList = imageList;

                    foreach (var issue in issues)
                    {
                        var item = new ListViewItem
                        {
                            Tag = issue
                        };
                        try
                        {
                            var issueIcon = await _jiraConnector.GetIssueTypeBitmapAsync(issue);
                            imageList.Images.Add(issueIcon);
                            item.ImageIndex = imageList.Images.Count - 1;
                        }
                        catch (Exception ex)
                        {
                            Log.Warn("Problem loading issue type, ignoring", ex);
                        }

                        item.SubItems.Add(issue.Key);
                        item.SubItems.Add(issue.Fields.Created.HasValue
                            ? issue.Fields.Created.Value.ToString("d", DateTimeFormatInfo.InvariantInfo)
                            : string.Empty);
                        item.SubItems.Add(issue.Fields.Assignee?.DisplayName);
                        item.SubItems.Add(issue.Fields.Reporter?.DisplayName);
                        item.SubItems.Add(issue.Fields.Summary);
                        jiraListView.Items.Add(item);
                        for (int i = 0; i < columns.Length; i++)
                        {
                            jiraListView.AutoResizeColumn(i, ColumnHeaderAutoResizeStyle.ColumnContent);
                        }

                        jiraListView.Invalidate();
                        jiraListView.Update();
                    }

                    jiraListView.Refresh();
                }
            }
        }

        private void JiraListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (jiraListView.SelectedItems.Count > 0)
            {
                _selectedIssue = (Issue) jiraListView.SelectedItems[0].Tag;
                jiraKey.Text = _selectedIssue.Key;
                uploadButton.Enabled = true;
            }
            else
            {
                uploadButton.Enabled = false;
            }
        }

        private void JiraListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == _columnSorter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                _columnSorter.Order = _columnSorter.Order == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                _columnSorter.SortColumn = e.Column;
                _columnSorter.Order = SortOrder.Ascending;
            }

            // Perform the sort with these new sort options.
            jiraListView.Sort();
        }

        private async void JiraKeyTextChanged(object sender, EventArgs e)
        {
            string jiranumber = jiraKey.Text;
            uploadButton.Enabled = false;
            int dashIndex = jiranumber.IndexOf('-');
            if (dashIndex > 0 && jiranumber.Length > dashIndex + 1)
            {
                _selectedIssue = await _jiraConnector.GetIssueAsync(jiraKey.Text);
                if (_selectedIssue != null)
                {
                    uploadButton.Enabled = true;
                }
            }
        }
    }
}