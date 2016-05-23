/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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

using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using Greenshot.IniFile;
using Jira;

namespace GreenshotJiraPlugin {
	public partial class JiraForm : Form {
		private readonly JiraConnector jiraConnector;
		private JiraIssue selectedIssue;
		private readonly GreenshotColumnSorter columnSorter;
		private readonly JiraConfiguration config = IniConfig.GetIniSection<JiraConfiguration>();

		public JiraForm(JiraConnector jiraConnector) {
			InitializeComponent();
			this.Icon = GreenshotPlugin.Core.GreenshotResources.getGreenshotIcon();
			AcceptButton = uploadButton;
			CancelButton = cancelButton;

			initializeComponentText();

			this.columnSorter = new GreenshotColumnSorter();
			this.jiraListView.ListViewItemSorter = columnSorter;

			this.jiraConnector = jiraConnector;

			changeModus(false);
			try {
				if (!jiraConnector.isLoggedIn) {
					jiraConnector.login();
				}
			} catch (Exception e) {
				MessageBox.Show(Language.GetFormattedString("jira", LangKey.login_error, e.Message));
			}
			uploadButton.Enabled = false;
			updateForm();
		}

		private void initializeComponentText() {
			this.label_jirafilter.Text = Language.GetString("jira", LangKey.label_jirafilter);
			this.label_comment.Text = Language.GetString("jira", LangKey.label_comment);
			this.label_filename.Text = Language.GetString("jira", LangKey.label_filename);
		}

		private void updateForm() {
			if (jiraConnector.isLoggedIn) {
				JiraFilter[] filters = jiraConnector.getFilters();
				if (filters.Length > 0) {
					foreach (JiraFilter filter in filters) {
						jiraFilterBox.Items.Add(filter);
					}
					jiraFilterBox.SelectedIndex = 0;
				}
				changeModus(true);
				if (config.LastUsedJira != null) {
					selectedIssue = jiraConnector.getIssue(config.LastUsedJira);
					if (selectedIssue != null) {
						jiraKey.Text = config.LastUsedJira;
						uploadButton.Enabled = true;
					}
				}
			}
		}

		private void changeModus(bool enabled) {
			jiraFilterBox.Enabled = enabled;
			jiraListView.Enabled = enabled;
			jiraFilenameBox.Enabled = enabled;
			jiraCommentBox.Enabled = enabled;
		}

		public void setFilename(string filename) {
			jiraFilenameBox.Text = filename;
		}

		public void setComment(string comment) {
			jiraCommentBox.Text = comment;
		}

		public JiraIssue getJiraIssue() {
			return selectedIssue;
		}

		public void upload(IBinaryContainer attachment) {
			config.LastUsedJira = selectedIssue.Key;
			jiraConnector.addAttachment(selectedIssue.Key, jiraFilenameBox.Text, attachment);
			if (jiraCommentBox.Text != null && jiraCommentBox.Text.Length > 0) {
				jiraConnector.addComment(selectedIssue.Key, jiraCommentBox.Text);
			}
		}

		public void logout() {
			jiraConnector.logout();
		}

		private void selectJiraToolStripMenuItem_Click(object sender, EventArgs e) {
			ToolStripMenuItem clickedItem = (ToolStripMenuItem)sender;
			selectedIssue = (JiraIssue)clickedItem.Tag;
			jiraKey.Text = selectedIssue.Key;
		}

		private void jiraFilterBox_SelectedIndexChanged(object sender, EventArgs e) {
			if (jiraConnector.isLoggedIn) {
				JiraIssue[] issues = null;
				uploadButton.Enabled = false;
				JiraFilter filter = (JiraFilter)jiraFilterBox.SelectedItem;
				if (filter == null) {
					return;
				}
				// Run upload in the background
				new PleaseWaitForm().ShowAndWait(JiraPlugin.Instance.JiraPluginAttributes.Name, Language.GetString("jira", LangKey.communication_wait),
					delegate() {
						issues = jiraConnector.getIssuesForFilter(filter.Id);
					}
				);
				jiraListView.BeginUpdate();
				jiraListView.Items.Clear();
				if (issues.Length > 0) {
					jiraListView.Columns.Clear();
					LangKey[] columns = { LangKey.column_id, LangKey.column_created, LangKey.column_assignee, LangKey.column_reporter, LangKey.column_summary };
					foreach (LangKey column in columns) {
						jiraListView.Columns.Add(Language.GetString("jira", column));
					}
					foreach (JiraIssue issue in issues) {
						ListViewItem item = new ListViewItem(issue.Key);
						item.Tag = issue;
						item.SubItems.Add(issue.Created.Value.ToString("d", DateTimeFormatInfo.InvariantInfo));
						item.SubItems.Add(issue.Assignee);
						item.SubItems.Add(issue.Reporter);
						item.SubItems.Add(issue.Summary);
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
			if (jiraListView.SelectedItems != null && jiraListView.SelectedItems.Count > 0) {
				selectedIssue = (JiraIssue)jiraListView.SelectedItems[0].Tag;
				jiraKey.Text = selectedIssue.Key;
				uploadButton.Enabled = true;
			} else {
				uploadButton.Enabled = false;
			}
		}

		private void jiraListView_ColumnClick(object sender, ColumnClickEventArgs e) {
			// Determine if clicked column is already the column that is being sorted.
			if (e.Column == columnSorter.SortColumn) {
				// Reverse the current sort direction for this column.
				if (columnSorter.Order == SortOrder.Ascending) {
					columnSorter.Order = SortOrder.Descending;
				} else {
					columnSorter.Order = SortOrder.Ascending;
				}
			} else {
				// Set the column number that is to be sorted; default to ascending.
				columnSorter.SortColumn = e.Column;
				columnSorter.Order = SortOrder.Ascending;
			}

			// Perform the sort with these new sort options.
			this.jiraListView.Sort();
		}

		void JiraKeyTextChanged(object sender, EventArgs e) {
			string jiranumber = jiraKey.Text;
			uploadButton.Enabled = false;
			int dashIndex = jiranumber.IndexOf('-');
			if (dashIndex > 0 && jiranumber.Length > dashIndex+1) {
				selectedIssue = jiraConnector.getIssue(jiraKey.Text);
				if (selectedIssue != null) {
					uploadButton.Enabled = true;
				}
			}
		}
	}
}
