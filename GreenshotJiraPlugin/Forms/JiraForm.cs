/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Globalization;
using System.Windows.Forms;
using Dapplo.Jira.Entities;
using Greenshot.IniFile;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GreenshotJiraPlugin.Forms {
	public partial class JiraForm : Form {
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(JiraForm));
		private readonly JiraConnector _jiraConnector;
		private Issue _selectedIssue;
		private readonly GreenshotColumnSorter _columnSorter;
		private readonly JiraConfiguration _config = IniConfig.GetIniSection<JiraConfiguration>();

		public JiraForm(JiraConnector jiraConnector) {
			InitializeComponent();
			Icon = GreenshotResources.getGreenshotIcon();
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
					await _jiraConnector.Login();
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
				if (_config.LastUsedJira != null)
				{
					_selectedIssue = await _jiraConnector.GetIssueAsync(_config.LastUsedJira);
					if (_selectedIssue != null)
					{
						jiraKey.Text = _config.LastUsedJira;
						uploadButton.Enabled = true;
					}
				}
			}
		}

		private void InitializeComponentText() {
			label_jirafilter.Text = Language.GetString("jira", LangKey.label_jirafilter);
			label_comment.Text = Language.GetString("jira", LangKey.label_comment);
			label_filename.Text = Language.GetString("jira", LangKey.label_filename);
		}

		private void ChangeModus(bool enabled) {
			jiraFilterBox.Enabled = enabled;
			jiraListView.Enabled = enabled;
			jiraFilenameBox.Enabled = enabled;
			jiraCommentBox.Enabled = enabled;
		}

		public void SetFilename(string filename) {
			jiraFilenameBox.Text = filename;
		}

		public void SetComment(string comment) {
			jiraCommentBox.Text = comment;
		}

		public Issue GetJiraIssue() {
			return _selectedIssue;
		}

		public async Task UploadAsync(IBinaryContainer attachment) {
			_config.LastUsedJira = _selectedIssue.Key;
			using (var memoryStream = new MemoryStream())
			{
				attachment.WriteToStream(memoryStream);
				memoryStream.Seek(0, SeekOrigin.Begin);
				await _jiraConnector.AttachAsync(_selectedIssue.Key, memoryStream, jiraFilenameBox.Text, attachment.ContentType);
			}

			if (!string.IsNullOrEmpty(jiraCommentBox.Text)) {
				await _jiraConnector.AddCommentAsync(_selectedIssue.Key, jiraCommentBox.Text);
			}
		}

		private void selectJiraToolStripMenuItem_Click(object sender, EventArgs e) {
			ToolStripMenuItem clickedItem = (ToolStripMenuItem)sender;
			_selectedIssue = (Issue)clickedItem.Tag;
			jiraKey.Text = _selectedIssue.Key;
		}

		private async void jiraFilterBox_SelectedIndexChanged(object sender, EventArgs e) {
			if (_jiraConnector.IsLoggedIn) {

				uploadButton.Enabled = false;
				var filter = (Filter)jiraFilterBox.SelectedItem;
				if (filter == null) {
					return;
				}
				IList<Issue> issues = null;
				try
				{
					var searchResult = await _jiraConnector.SearchAsync(filter.Jql);
					issues = searchResult.Issues;
				}
				catch (Exception ex)
				{
					Log.Error(ex);
					MessageBox.Show(this, ex.Message, "Error in filter", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}

				jiraListView.BeginUpdate();
				jiraListView.Items.Clear();
				if (issues?.Count > 0) {
					jiraListView.Columns.Clear();
					LangKey[] columns = { LangKey.column_id, LangKey.column_created, LangKey.column_assignee, LangKey.column_reporter, LangKey.column_summary };
					foreach (LangKey column in columns) {
						jiraListView.Columns.Add(Language.GetString("jira", column));
					}
					foreach (var issue in issues) {
						var item = new ListViewItem(issue.Key)
						{
							Tag = issue
						};
						item.SubItems.Add(issue.Fields.Created.ToString("d", DateTimeFormatInfo.InvariantInfo));
						item.SubItems.Add(issue.Fields.Assignee?.DisplayName);
						item.SubItems.Add(issue.Fields.Reporter?.DisplayName);
						item.SubItems.Add(issue.Fields.Summary);
						jiraListView.Items.Add(item);
					}
					for (int i = 0; i < columns.Length; i++) {
						jiraListView.AutoResizeColumn(i, ColumnHeaderAutoResizeStyle.ColumnContent);
					}
				}
				jiraListView.EndUpdate();
				jiraListView.Refresh();
			}
		}

		private void jiraListView_SelectedIndexChanged(object sender, EventArgs e) {
			if (jiraListView.SelectedItems.Count > 0) {
				_selectedIssue = (Issue)jiraListView.SelectedItems[0].Tag;
				jiraKey.Text = _selectedIssue.Key;
				uploadButton.Enabled = true;
			} else {
				uploadButton.Enabled = false;
			}
		}

		private void jiraListView_ColumnClick(object sender, ColumnClickEventArgs e) {
			// Determine if clicked column is already the column that is being sorted.
			if (e.Column == _columnSorter.SortColumn) {
				// Reverse the current sort direction for this column.
				if (_columnSorter.Order == SortOrder.Ascending) {
					_columnSorter.Order = SortOrder.Descending;
				} else {
					_columnSorter.Order = SortOrder.Ascending;
				}
			} else {
				// Set the column number that is to be sorted; default to ascending.
				_columnSorter.SortColumn = e.Column;
				_columnSorter.Order = SortOrder.Ascending;
			}

			// Perform the sort with these new sort options.
			jiraListView.Sort();
		}

		private async void JiraKeyTextChanged(object sender, EventArgs e) {
			string jiranumber = jiraKey.Text;
			uploadButton.Enabled = false;
			int dashIndex = jiranumber.IndexOf('-');
			if (dashIndex > 0 && jiranumber.Length > dashIndex+1) {
				_selectedIssue = await _jiraConnector.GetIssueAsync(jiraKey.Text);
				if (_selectedIssue != null) {
					uploadButton.Enabled = true;
				}
			}
		}
	}
}
