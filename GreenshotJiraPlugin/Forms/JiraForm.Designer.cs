/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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
namespace GreenshotJiraPlugin {
	partial class JiraForm {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.jiraFilterBox = new System.Windows.Forms.ComboBox();
			this.label_jirafilter = new System.Windows.Forms.Label();
			this.label_jira = new System.Windows.Forms.Label();
			this.uploadButton = new System.Windows.Forms.Button();
			this.jiraListView = new System.Windows.Forms.ListView();
			this.jiraFilenameBox = new System.Windows.Forms.TextBox();
			this.label_filename = new System.Windows.Forms.Label();
			this.label_comment = new System.Windows.Forms.Label();
			this.jiraCommentBox = new System.Windows.Forms.TextBox();
			this.cancelButton = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.jiraKey = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// jiraFilterBox
			// 
			this.jiraFilterBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
									| System.Windows.Forms.AnchorStyles.Right)));
			this.jiraFilterBox.DisplayMember = "name";
			this.jiraFilterBox.FormattingEnabled = true;
			this.jiraFilterBox.Location = new System.Drawing.Point(102, 11);
			this.jiraFilterBox.Name = "jiraFilterBox";
			this.jiraFilterBox.Size = new System.Drawing.Size(604, 21);
			this.jiraFilterBox.TabIndex = 23;
			this.jiraFilterBox.SelectedIndexChanged += new System.EventHandler(this.jiraFilterBox_SelectedIndexChanged);
			// 
			// label_jirafilter
			// 
			this.label_jirafilter.AutoSize = true;
			this.label_jirafilter.Location = new System.Drawing.Point(14, 14);
			this.label_jirafilter.Name = "label_jirafilter";
			this.label_jirafilter.Size = new System.Drawing.Size(52, 13);
			this.label_jirafilter.TabIndex = 24;
			this.label_jirafilter.Text = "JIRA filter";
			// 
			// label_jira
			// 
			this.label_jira.AutoSize = true;
			this.label_jira.Location = new System.Drawing.Point(14, 41);
			this.label_jira.Name = "label_jira";
			this.label_jira.Size = new System.Drawing.Size(30, 13);
			this.label_jira.TabIndex = 31;
			this.label_jira.Text = "JIRA";
			// 
			// uploadButton
			// 
			this.uploadButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.uploadButton.Enabled = false;
			this.uploadButton.Location = new System.Drawing.Point(550, 281);
			this.uploadButton.Name = "uploadButton";
			this.uploadButton.Size = new System.Drawing.Size(75, 23);
			this.uploadButton.TabIndex = 32;
			this.uploadButton.Text = "Upload";
			this.uploadButton.UseVisualStyleBackColor = true;
			this.uploadButton.Click += new System.EventHandler(this.uploadButton_Click);
			// 
			// jiraListView
			// 
			this.jiraListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
									| System.Windows.Forms.AnchorStyles.Left) 
									| System.Windows.Forms.AnchorStyles.Right)));
			this.jiraListView.FullRowSelect = true;
			this.jiraListView.Location = new System.Drawing.Point(102, 41);
			this.jiraListView.MultiSelect = false;
			this.jiraListView.Name = "jiraListView";
			this.jiraListView.Size = new System.Drawing.Size(604, 172);
			this.jiraListView.TabIndex = 33;
			this.jiraListView.UseCompatibleStateImageBehavior = false;
			this.jiraListView.View = System.Windows.Forms.View.Details;
			this.jiraListView.SelectedIndexChanged += new System.EventHandler(this.jiraListView_SelectedIndexChanged);
			this.jiraListView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.jiraListView_ColumnClick);
			// 
			// jiraFilenameBox
			// 
			this.jiraFilenameBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
									| System.Windows.Forms.AnchorStyles.Right)));
			this.jiraFilenameBox.Location = new System.Drawing.Point(102, 219);
			this.jiraFilenameBox.Name = "jiraFilenameBox";
			this.jiraFilenameBox.Size = new System.Drawing.Size(604, 20);
			this.jiraFilenameBox.TabIndex = 34;
			// 
			// label_filename
			// 
			this.label_filename.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label_filename.AutoSize = true;
			this.label_filename.Location = new System.Drawing.Point(14, 222);
			this.label_filename.Name = "label_filename";
			this.label_filename.Size = new System.Drawing.Size(49, 13);
			this.label_filename.TabIndex = 35;
			this.label_filename.Text = "Filename";
			// 
			// label_comment
			// 
			this.label_comment.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label_comment.AutoSize = true;
			this.label_comment.Location = new System.Drawing.Point(14, 248);
			this.label_comment.Name = "label_comment";
			this.label_comment.Size = new System.Drawing.Size(51, 13);
			this.label_comment.TabIndex = 36;
			this.label_comment.Text = "Comment";
			// 
			// jiraCommentBox
			// 
			this.jiraCommentBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
									| System.Windows.Forms.AnchorStyles.Right)));
			this.jiraCommentBox.Location = new System.Drawing.Point(102, 245);
			this.jiraCommentBox.Name = "jiraCommentBox";
			this.jiraCommentBox.Size = new System.Drawing.Size(604, 20);
			this.jiraCommentBox.TabIndex = 37;
			// 
			// cancelButton
			// 
			this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.cancelButton.Location = new System.Drawing.Point(631, 281);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(75, 23);
			this.cancelButton.TabIndex = 38;
			this.cancelButton.Text = "Cancel";
			this.cancelButton.UseVisualStyleBackColor = true;
			this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(14, 274);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(23, 13);
			this.label1.TabIndex = 39;
			this.label1.Text = "Jira";
			// 
			// jiraKey
			// 
			this.jiraKey.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
									| System.Windows.Forms.AnchorStyles.Right)));
			this.jiraKey.Location = new System.Drawing.Point(102, 271);
			this.jiraKey.Name = "jiraKey";
			this.jiraKey.Size = new System.Drawing.Size(158, 20);
			this.jiraKey.TabIndex = 40;
			this.jiraKey.TextChanged += new System.EventHandler(this.JiraKeyTextChanged);
			// 
			// JiraForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.ClientSize = new System.Drawing.Size(720, 316);
			this.Controls.Add(this.jiraKey);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.cancelButton);
			this.Controls.Add(this.jiraCommentBox);
			this.Controls.Add(this.label_comment);
			this.Controls.Add(this.label_filename);
			this.Controls.Add(this.jiraFilenameBox);
			this.Controls.Add(this.jiraListView);
			this.Controls.Add(this.uploadButton);
			this.Controls.Add(this.label_jira);
			this.Controls.Add(this.label_jirafilter);
			this.Controls.Add(this.jiraFilterBox);
			this.Name = "JiraForm";
			this.Text = "JIRA Upload";
			this.TopMost = true;
			this.ResumeLayout(false);
			this.PerformLayout();
		}
		private System.Windows.Forms.TextBox jiraKey;
		private System.Windows.Forms.Label label1;

		#endregion

		private System.Windows.Forms.ComboBox jiraFilterBox;
		private System.Windows.Forms.Label label_jirafilter;
		private System.Windows.Forms.Label label_jira;
		private System.Windows.Forms.Button uploadButton;
		private System.Windows.Forms.ListView jiraListView;
		private System.Windows.Forms.TextBox jiraFilenameBox;
		private System.Windows.Forms.Label label_filename;
		private System.Windows.Forms.Label label_comment;
		private System.Windows.Forms.TextBox jiraCommentBox;
		private System.Windows.Forms.Button cancelButton;
	}
}