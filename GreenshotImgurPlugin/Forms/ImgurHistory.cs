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
using System.Text;
using System.Windows.Forms;

using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using Greenshot.IniFile;

namespace GreenshotImgurPlugin {
	/// <summary>
	/// Description of ImgurHistory.
	/// </summary>
	public partial class ImgurHistory : ImgurForm {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ImgurHistory));
		private GreenshotColumnSorter columnSorter;
		private static ImgurConfiguration config = IniConfig.GetIniSection<ImgurConfiguration>();
		private static ImgurHistory instance;
		
		public static void ShowHistory() {
			// Make sure the history is loaded, will be done only once
			ImgurUtils.LoadHistory();
			if (instance == null) {
				instance = new ImgurHistory();
			}
			instance.Show();
			instance.redraw();
		}
		
		private ImgurHistory() {
			this.ManualLanguageApply = true;
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			AcceptButton = finishedButton;
			CancelButton = finishedButton;
			// Init sorting
			columnSorter = new GreenshotColumnSorter();
			this.listview_imgur_uploads.ListViewItemSorter = columnSorter;
			columnSorter.SortColumn = 3;
			columnSorter.Order = SortOrder.Descending;
			redraw();
			if (listview_imgur_uploads.Items.Count > 0) {
				listview_imgur_uploads.Items[0].Selected = true;
			}
			ApplyLanguage();
			if (config.Credits > 0) {
				this.Text = this.Text + " (" + config.Credits + " credits)";
			}
		}

		private void redraw() {
			// Should fix Bug #3378699 
			pictureBox1.Image = pictureBox1.ErrorImage;
			listview_imgur_uploads.BeginUpdate();
			listview_imgur_uploads.Items.Clear();
			listview_imgur_uploads.Columns.Clear();
			string[] columns = { "hash", "title", "deleteHash", "Date"};
			foreach (string column in columns) {
				listview_imgur_uploads.Columns.Add(column);
			}
			foreach (ImgurInfo imgurInfo in config.runtimeImgurHistory.Values) {
				ListViewItem item = new ListViewItem(imgurInfo.Hash);
				item.Tag = imgurInfo;
				item.SubItems.Add(imgurInfo.Title);
				item.SubItems.Add(imgurInfo.DeleteHash);
				item.SubItems.Add(imgurInfo.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", DateTimeFormatInfo.InvariantInfo));
				listview_imgur_uploads.Items.Add(item);
			}
			for (int i = 0; i < columns.Length; i++) {
				listview_imgur_uploads.AutoResizeColumn(i, ColumnHeaderAutoResizeStyle.ColumnContent);
			}
	
			listview_imgur_uploads.EndUpdate();
			listview_imgur_uploads.Refresh();
			deleteButton.Enabled = false;
			openButton.Enabled = false;
			clipboardButton.Enabled = false;
		}

		private void Listview_imgur_uploadsSelectedIndexChanged(object sender, EventArgs e) {
			pictureBox1.Image = pictureBox1.ErrorImage;
			if (listview_imgur_uploads.SelectedItems != null && listview_imgur_uploads.SelectedItems.Count > 0) {
				deleteButton.Enabled = true;
				openButton.Enabled = true;
				clipboardButton.Enabled = true;
				if (listview_imgur_uploads.SelectedItems.Count == 1) {
					ImgurInfo imgurInfo = (ImgurInfo)listview_imgur_uploads.SelectedItems[0].Tag;
					pictureBox1.Image = imgurInfo.Image;
				}
			} else {
				pictureBox1.Image = pictureBox1.ErrorImage;
				deleteButton.Enabled = false;
				openButton.Enabled = false;
				clipboardButton.Enabled = false;
			}
		}

		private void DeleteButtonClick(object sender, EventArgs e) {
			if (listview_imgur_uploads.SelectedItems != null && listview_imgur_uploads.SelectedItems.Count > 0) {
				for (int i = 0; i < listview_imgur_uploads.SelectedItems.Count; i++) {
					ImgurInfo imgurInfo = (ImgurInfo)listview_imgur_uploads.SelectedItems[i].Tag;
					DialogResult result = MessageBox.Show(Language.GetFormattedString("imgur", LangKey.delete_question, imgurInfo.Title), Language.GetFormattedString("imgur", LangKey.delete_title, imgurInfo.Hash), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
					if (result == DialogResult.Yes) {
						// Should fix Bug #3378699 
						pictureBox1.Image = pictureBox1.ErrorImage;
						try {
							new PleaseWaitForm().ShowAndWait(ImgurPlugin.Attributes.Name, Language.GetString("imgur", LangKey.communication_wait), 
								delegate() {
									ImgurUtils.DeleteImgurImage(imgurInfo);
								}
							);
						} catch (Exception ex) {
							LOG.Warn("Problem communicating with Imgur: ", ex);
						}

						imgurInfo.Dispose();
					}
				}
			}
			redraw();
		}

		private void ClipboardButtonClick(object sender, EventArgs e) {
			StringBuilder links = new StringBuilder();
			if (listview_imgur_uploads.SelectedItems != null && listview_imgur_uploads.SelectedItems.Count > 0) {
				for (int i = 0; i < listview_imgur_uploads.SelectedItems.Count; i++) {
					ImgurInfo imgurInfo = (ImgurInfo)listview_imgur_uploads.SelectedItems[i].Tag;
					if (config.UsePageLink) {
						links.AppendLine(imgurInfo.Page);
					} else {
						links.AppendLine(imgurInfo.Original);
					}
				}
			}
			ClipboardHelper.SetClipboardData(links.ToString());
		}

		private void ClearHistoryButtonClick(object sender, EventArgs e) {
			DialogResult result = MessageBox.Show(Language.GetString("imgur", LangKey.clear_question), "Imgur", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (result == DialogResult.Yes) {
				config.runtimeImgurHistory.Clear();
				config.ImgurUploadHistory.Clear();
				IniConfig.Save();
				redraw();
			}
		}

		private void FinishedButtonClick(object sender, EventArgs e)
		{
			this.Hide();
		}

		private void OpenButtonClick(object sender, EventArgs e) {
			if (listview_imgur_uploads.SelectedItems != null && listview_imgur_uploads.SelectedItems.Count > 0) {
				for (int i = 0; i < listview_imgur_uploads.SelectedItems.Count; i++) {
					ImgurInfo imgurInfo = (ImgurInfo)listview_imgur_uploads.SelectedItems[i].Tag;
					System.Diagnostics.Process.Start(imgurInfo.Page);
				}
			}
		}
		
		private void listview_imgur_uploads_ColumnClick(object sender, ColumnClickEventArgs e) {
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
			this.listview_imgur_uploads.Sort();
		}

		
		void ImgurHistoryFormClosing(object sender, FormClosingEventArgs e)
		{
			instance = null;
		}
	}
}
