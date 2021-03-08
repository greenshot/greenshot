/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using GreenshotPlugin.IniFile;

namespace GreenshotImgurPlugin.Forms {
	/// <summary>
	/// Imgur history form
	/// </summary>
	public sealed partial class ImgurHistory : ImgurForm {
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ImgurHistory));
		private readonly GreenshotColumnSorter _columnSorter;
		private static readonly object Lock = new object();
		private static readonly ImgurConfiguration Config = IniConfig.GetIniSection<ImgurConfiguration>();
		private static ImgurHistory _instance;
		
		public static void ShowHistory() {
			lock (Lock)
			{
				if (ImgurUtils.IsHistoryLoadingNeeded())
				{
					// Run upload in the background
					new PleaseWaitForm().ShowAndWait("Imgur " + Language.GetString("imgur", LangKey.history), Language.GetString("imgur", LangKey.communication_wait),
						ImgurUtils.LoadHistory
					);
				}

				// Make sure the history is loaded, will be done only once
				if (_instance == null)
				{
					_instance = new ImgurHistory();
				}
				if (!_instance.Visible)
				{
					_instance.Show();
				}
				_instance.Redraw();
			}
		}
		
		private ImgurHistory() {
			ManualLanguageApply = true;
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			AcceptButton = finishedButton;
			CancelButton = finishedButton;
			// Init sorting
			_columnSorter = new GreenshotColumnSorter();
			listview_imgur_uploads.ListViewItemSorter = _columnSorter;
			_columnSorter.SortColumn = 3;
			_columnSorter.Order = SortOrder.Descending;
			Redraw();
			if (listview_imgur_uploads.Items.Count > 0) {
				listview_imgur_uploads.Items[0].Selected = true;
			}
			ApplyLanguage();
			if (Config.Credits > 0) {
				Text = Text + " (" + Config.Credits + " credits)";
			}
		}

		private void Redraw() {
			// Should fix Bug #3378699 
			pictureBox1.Image = pictureBox1.ErrorImage;
			listview_imgur_uploads.BeginUpdate();
			listview_imgur_uploads.Items.Clear();
			listview_imgur_uploads.Columns.Clear();
			string[] columns = { "hash", "title", "deleteHash", "Date"};
			foreach (string column in columns) {
				listview_imgur_uploads.Columns.Add(column);
			}
			foreach (ImgurInfo imgurInfo in Config.runtimeImgurHistory.Values) {
				var item = new ListViewItem(imgurInfo.Hash)
				{
					Tag = imgurInfo
				};
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
			if (listview_imgur_uploads.SelectedItems.Count > 0) {
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
			if (listview_imgur_uploads.SelectedItems.Count > 0) {
				for (int i = 0; i < listview_imgur_uploads.SelectedItems.Count; i++) {
					ImgurInfo imgurInfo = (ImgurInfo)listview_imgur_uploads.SelectedItems[i].Tag;
					DialogResult result = MessageBox.Show(Language.GetFormattedString("imgur", LangKey.delete_question, imgurInfo.Title), Language.GetFormattedString("imgur", LangKey.delete_title, imgurInfo.Hash), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
					if (result != DialogResult.Yes)
					{
						continue;
					}
					// Should fix Bug #3378699 
					pictureBox1.Image = pictureBox1.ErrorImage;
					try {
						new PleaseWaitForm().ShowAndWait("Imgur", Language.GetString("imgur", LangKey.communication_wait), 
							delegate {
								ImgurUtils.DeleteImgurImage(imgurInfo);
							}
						);
					} catch (Exception ex) {
						Log.Warn("Problem communicating with Imgur: ", ex);
					}

					imgurInfo.Dispose();
				}
			}
			Redraw();
		}

		private void ClipboardButtonClick(object sender, EventArgs e) {
			StringBuilder links = new StringBuilder();
			if (listview_imgur_uploads.SelectedItems.Count > 0) {
				for (int i = 0; i < listview_imgur_uploads.SelectedItems.Count; i++)
				{
					ImgurInfo imgurInfo = (ImgurInfo)listview_imgur_uploads.SelectedItems[i].Tag;
					links.AppendLine(Config.UsePageLink ? imgurInfo.Page : imgurInfo.Original);
				}
			}
			ClipboardHelper.SetClipboardData(links.ToString());
		}

		private void ClearHistoryButtonClick(object sender, EventArgs e) {
			DialogResult result = MessageBox.Show(Language.GetString("imgur", LangKey.clear_question), "Imgur", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (result == DialogResult.Yes) {
				Config.runtimeImgurHistory.Clear();
				Config.ImgurUploadHistory.Clear();
				IniConfig.Save();
				Redraw();
			}
		}

		private void FinishedButtonClick(object sender, EventArgs e)
		{
			Hide();
		}

		private void OpenButtonClick(object sender, EventArgs e) {
			if (listview_imgur_uploads.SelectedItems.Count > 0) {
				for (int i = 0; i < listview_imgur_uploads.SelectedItems.Count; i++) {
					ImgurInfo imgurInfo = (ImgurInfo)listview_imgur_uploads.SelectedItems[i].Tag;
					System.Diagnostics.Process.Start(imgurInfo.Page);
				}
			}
		}
		
		private void listview_imgur_uploads_ColumnClick(object sender, ColumnClickEventArgs e) {
			// Determine if clicked column is already the column that is being sorted.
			if (e.Column == _columnSorter.SortColumn) {
				// Reverse the current sort direction for this column.
				_columnSorter.Order = _columnSorter.Order == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
			} else {
				// Set the column number that is to be sorted; default to ascending.
				_columnSorter.SortColumn = e.Column;
				_columnSorter.Order = SortOrder.Ascending;
			}

			// Perform the sort with these new sort options.
			listview_imgur_uploads.Sort();
		}


		private void ImgurHistoryFormClosing(object sender, FormClosingEventArgs e)
		{
			_instance = null;
		}
	}
}
