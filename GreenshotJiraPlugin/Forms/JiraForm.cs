/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

using GreenshotPlugin.Core;
using Jira;

namespace GreenshotJiraPlugin {
	public partial class JiraForm : Form {
		private JiraConnector jiraConnector;
		private JiraIssue selectedIssue;
		private ListViewColumnSorter columnSorter;
		private ILanguage language = Language.GetInstance();

		public JiraForm(JiraConnector jiraConnector) {
			InitializeComponent();
			language.SynchronizeLanguageToCulture();
			initializeComponentText();

			this.columnSorter = new ListViewColumnSorter();
			this.jiraListView.ListViewItemSorter = columnSorter;

			this.jiraConnector = jiraConnector;

			changeModus(false);
			try {
				if (!jiraConnector.isLoggedIn()) {
					jiraConnector.login();
				}
			} catch (Exception e) {
				MessageBox.Show(language.GetFormattedString(LangKey.login_error, e.Message));
			}
			updateFilter();

			uploadButton.Enabled = false;
		}

		private void initializeComponentText() {
			this.label_jirafilter.Text = language.GetString(LangKey.label_jirafilter);
			this.label_comment.Text = language.GetString(LangKey.label_comment);
			this.label_filename.Text = language.GetString(LangKey.label_filename);
		}

		private void updateFilter() {
			if (jiraConnector.isLoggedIn()) {
				JiraFilter[] filters = jiraConnector.getFilters();
				if (filters.Length > 0) {
					foreach (JiraFilter filter in filters) {
						jiraFilterBox.Items.Add(filter);
					}
					jiraFilterBox.SelectedIndex = 0;
				}
				changeModus(true);
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

		public void upload(string text) {
			jiraConnector.addAttachment(selectedIssue.key, jiraFilenameBox.Text, text);
			if (jiraCommentBox.Text != null && jiraCommentBox.Text.Length > 0) {
				jiraConnector.addComment(selectedIssue.key, jiraCommentBox.Text);
			}
		}

		public void upload(byte [] attachment) {
			jiraConnector.addAttachment(selectedIssue.key, jiraFilenameBox.Text, attachment);
			if (jiraCommentBox.Text != null && jiraCommentBox.Text.Length > 0) {
				jiraConnector.addComment(selectedIssue.key, jiraCommentBox.Text);
			}
		}

		public void logout() {
			jiraConnector.logout();
		}

		private void selectJiraToolStripMenuItem_Click(object sender, EventArgs e) {
			ToolStripMenuItem clickedItem = (ToolStripMenuItem)sender;
			selectedIssue = (JiraIssue)clickedItem.Tag;
		}

		private void jiraFilterBox_SelectedIndexChanged(object sender, EventArgs e) {
			if (jiraConnector.isLoggedIn()) {
				JiraFilter filter = (JiraFilter)jiraFilterBox.SelectedItem;
				if (filter != null) {
					JiraIssue[] issues = jiraConnector.getIssuesForFilter(filter.id);
					jiraListView.BeginUpdate();
					jiraListView.Items.Clear();
					if (issues.Length > 0) {
						jiraListView.Columns.Clear();
						LangKey[] columns = { LangKey.column_id, LangKey.column_created, LangKey.column_assignee, LangKey.column_reporter, LangKey.column_summary };
						foreach (LangKey column in columns) {
							jiraListView.Columns.Add(language.GetString(column));
						}
						foreach (JiraIssue issue in issues) {
							ListViewItem item = new ListViewItem(issue.key);
							item.Tag = issue;
							item.SubItems.Add(issue.created.Value.ToString("d", DateTimeFormatInfo.InvariantInfo));
							item.SubItems.Add(issue.assignee);
							item.SubItems.Add(issue.reporter);
							item.SubItems.Add(issue.summary);
							jiraListView.Items.Add(item);
						}
						for (int i = 0; i < columns.Length; i++) {
							jiraListView.AutoResizeColumn(i, ColumnHeaderAutoResizeStyle.ColumnContent);
						}
					}

					jiraListView.EndUpdate();
					jiraListView.Refresh();
				}
				uploadButton.Enabled = false;
			}
		}

		private void jiraListView_SelectedIndexChanged(object sender, EventArgs e) {
			if (jiraListView.SelectedItems != null && jiraListView.SelectedItems.Count > 0) {
				selectedIssue = (JiraIssue)jiraListView.SelectedItems[0].Tag;
				uploadButton.Enabled = true;
			} else {
				uploadButton.Enabled = false;
			}
		}

		private void uploadButton_Click(object sender, EventArgs e) {
			this.DialogResult = DialogResult.OK;
		}

		private void cancelButton_Click(object sender, EventArgs e) {
			this.DialogResult = DialogResult.Cancel;
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
	}
}
