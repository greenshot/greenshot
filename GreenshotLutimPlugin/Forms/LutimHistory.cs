/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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

namespace GreenshotLutimPlugin {
	/// <summary>
	/// Lutim history form
	/// </summary>
	public sealed partial class LutimHistory : LutimForm {
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(LutimHistory));
		private readonly GreenshotColumnSorter _columnSorter;
		private static readonly object Lock = new object();
		private static readonly LutimConfiguration Config = IniConfig.GetIniSection<LutimConfiguration>();
		private static LutimHistory _instance;
		
		public static void ShowHistory() {
			lock (Lock)
			{
				if (LutimUtils.IsHistoryLoadingNeeded())
				{
					// Run upload in the background
					new PleaseWaitForm().ShowAndWait("Lutim " + Language.GetString("lutim", LangKey.history), Language.GetString("lutim", LangKey.communication_wait),
						LutimUtils.LoadHistory
					);
				}

				// Make sure the history is loaded, will be done only once
				if (_instance == null)
				{
					_instance = new LutimHistory();
				}
				if (!_instance.Visible)
				{
					_instance.Show();
				}
				_instance.Redraw();
			}
		}
		
		private LutimHistory() {
			ManualLanguageApply = true;
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			AcceptButton = finishedButton;
			CancelButton = finishedButton;
			// Init sorting
			_columnSorter = new GreenshotColumnSorter();
			listview_lutim_uploads.ListViewItemSorter = _columnSorter;
			_columnSorter.SortColumn = 3;
			_columnSorter.Order = SortOrder.Descending;
			Redraw();
			if (listview_lutim_uploads.Items.Count > 0) {
				listview_lutim_uploads.Items[0].Selected = true;
			}
			ApplyLanguage();
		}

		private void Redraw() {
			// Should fix Bug #3378699 
			pictureBox1.Image = pictureBox1.ErrorImage;
			listview_lutim_uploads.BeginUpdate();
			listview_lutim_uploads.Items.Clear();
			listview_lutim_uploads.Columns.Clear();
			string[] columns = { "hash", "title", "deleteHash", "Date"};
			foreach (string column in columns) {
				listview_lutim_uploads.Columns.Add(column);
			}
			foreach (LutimInfo lutimInfo in Config.runtimeLutimHistory.Values) {
				var item = new ListViewItem(lutimInfo.Short)
				{
					Tag = lutimInfo
				};
				item.SubItems.Add(lutimInfo.Filename);
				item.SubItems.Add(lutimInfo.Token);
				item.SubItems.Add(lutimInfo.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", DateTimeFormatInfo.InvariantInfo));
				listview_lutim_uploads.Items.Add(item);
			}
			for (int i = 0; i < columns.Length; i++) {
				listview_lutim_uploads.AutoResizeColumn(i, ColumnHeaderAutoResizeStyle.ColumnContent);
			}
	
			listview_lutim_uploads.EndUpdate();
			listview_lutim_uploads.Refresh();
			deleteButton.Enabled = false;
			openButton.Enabled = false;
			clipboardButton.Enabled = false;
		}

		private void Listview_lutim_uploadsSelectedIndexChanged(object sender, EventArgs e) {
			pictureBox1.Image = pictureBox1.ErrorImage;
			if (listview_lutim_uploads.SelectedItems.Count > 0) {
				deleteButton.Enabled = true;
				openButton.Enabled = true;
				clipboardButton.Enabled = true;
				if (listview_lutim_uploads.SelectedItems.Count == 1) {
					LutimInfo lutimInfo = (LutimInfo)listview_lutim_uploads.SelectedItems[0].Tag;
					pictureBox1.Image = lutimInfo.Thumb;
				}
			} else {
				pictureBox1.Image = pictureBox1.ErrorImage;
				deleteButton.Enabled = false;
				openButton.Enabled = false;
				clipboardButton.Enabled = false;
			}
		}

		private void DeleteButtonClick(object sender, EventArgs e) {
			if (listview_lutim_uploads.SelectedItems.Count > 0) {
				for (int i = 0; i < listview_lutim_uploads.SelectedItems.Count; i++) {
					LutimInfo lutimInfo = (LutimInfo)listview_lutim_uploads.SelectedItems[i].Tag;
					DialogResult result = MessageBox.Show(Language.GetFormattedString("lutim", LangKey.delete_question, lutimInfo.Filename), Language.GetFormattedString("lutim", LangKey.delete_title, lutimInfo.Token), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
					if (result != DialogResult.Yes)
					{
						continue;
					}
					// Should fix Bug #3378699 
					pictureBox1.Image = pictureBox1.ErrorImage;
					try {
						new PleaseWaitForm().ShowAndWait(LutimPlugin.Attributes.Name, Language.GetString("lutim", LangKey.communication_wait), 
							delegate {
								LutimUtils.DeleteLutimImage(lutimInfo);
							}
						);
					} catch (Exception ex) {
						Log.Warn("Problem communicating with Lutim: ", ex);
					}

					lutimInfo.Dispose();
				}
			}
			Redraw();
		}

		private void ClipboardButtonClick(object sender, EventArgs e) {
			StringBuilder links = new StringBuilder();
			if (listview_lutim_uploads.SelectedItems.Count > 0) {
				for (int i = 0; i < listview_lutim_uploads.SelectedItems.Count; i++)
				{
					LutimInfo lutimInfo = (LutimInfo)listview_lutim_uploads.SelectedItems[i].Tag;
					links.AppendLine(lutimInfo.Uri.AbsoluteUri);
				}
			}
			ClipboardHelper.SetClipboardData(links.ToString());
		}

		private void ClearHistoryButtonClick(object sender, EventArgs e) {
			DialogResult result = MessageBox.Show(Language.GetString("lutim", LangKey.clear_question), "Lutim", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (result == DialogResult.Yes) {
				Config.runtimeLutimHistory.Clear();
				Config.LutimUploadHistory.Clear();
				IniConfig.Save();
				Redraw();
			}
		}

		private void FinishedButtonClick(object sender, EventArgs e)
		{
			Hide();
		}

		private void OpenButtonClick(object sender, EventArgs e) {
			if (listview_lutim_uploads.SelectedItems.Count > 0) {
				for (int i = 0; i < listview_lutim_uploads.SelectedItems.Count; i++) {
					LutimInfo lutimInfo = (LutimInfo)listview_lutim_uploads.SelectedItems[i].Tag;
					System.Diagnostics.Process.Start(lutimInfo.Uri.AbsoluteUri);
				}
			}
		}
		
		private void listview_lutim_uploads_ColumnClick(object sender, ColumnClickEventArgs e) {
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
			listview_lutim_uploads.Sort();
		}


		private void LutimHistoryFormClosing(object sender, FormClosingEventArgs e)
		{
			_instance = null;
		}
	}
}
