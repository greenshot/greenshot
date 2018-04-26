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
using Greenshot.IniFile;

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
			listview_Lutim_uploads.ListViewItemSorter = _columnSorter;
			_columnSorter.SortColumn = 3;
			_columnSorter.Order = SortOrder.Descending;
			Redraw();
			if (listview_Lutim_uploads.Items.Count > 0) {
				listview_Lutim_uploads.Items[0].Selected = true;
			}
			ApplyLanguage();
			if (Config.Credits > 0) {
				Text = Text + " (" + Config.Credits + " credits)";
			}
		}

		private void Redraw() {
			// Should fix Bug #3378699 
			pictureBox1.Image = pictureBox1.ErrorImage;
			listview_Lutim_uploads.BeginUpdate();
			listview_Lutim_uploads.Items.Clear();
			listview_Lutim_uploads.Columns.Clear();
			string[] columns = { "hash", "title", "deleteHash", "Date"};
			foreach (string column in columns) {
				listview_Lutim_uploads.Columns.Add(column);
			}
			foreach (LutimInfo LutimInfo in Config.runtimeLutimHistory.Values) {
				var item = new ListViewItem(LutimInfo.Hash)
				{
					Tag = LutimInfo
				};
				item.SubItems.Add(LutimInfo.Title);
				item.SubItems.Add(LutimInfo.DeleteHash);
				item.SubItems.Add(LutimInfo.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", DateTimeFormatInfo.InvariantInfo));
				listview_Lutim_uploads.Items.Add(item);
			}
			for (int i = 0; i < columns.Length; i++) {
				listview_Lutim_uploads.AutoResizeColumn(i, ColumnHeaderAutoResizeStyle.ColumnContent);
			}
	
			listview_Lutim_uploads.EndUpdate();
			listview_Lutim_uploads.Refresh();
			deleteButton.Enabled = false;
			openButton.Enabled = false;
			clipboardButton.Enabled = false;
		}

		private void Listview_Lutim_uploadsSelectedIndexChanged(object sender, EventArgs e) {
			pictureBox1.Image = pictureBox1.ErrorImage;
			if (listview_Lutim_uploads.SelectedItems.Count > 0) {
				deleteButton.Enabled = true;
				openButton.Enabled = true;
				clipboardButton.Enabled = true;
				if (listview_Lutim_uploads.SelectedItems.Count == 1) {
					LutimInfo LutimInfo = (LutimInfo)listview_Lutim_uploads.SelectedItems[0].Tag;
					pictureBox1.Image = LutimInfo.Image;
				}
			} else {
				pictureBox1.Image = pictureBox1.ErrorImage;
				deleteButton.Enabled = false;
				openButton.Enabled = false;
				clipboardButton.Enabled = false;
			}
		}

		private void DeleteButtonClick(object sender, EventArgs e) {
			if (listview_Lutim_uploads.SelectedItems.Count > 0) {
				for (int i = 0; i < listview_Lutim_uploads.SelectedItems.Count; i++) {
					LutimInfo LutimInfo = (LutimInfo)listview_Lutim_uploads.SelectedItems[i].Tag;
					DialogResult result = MessageBox.Show(Language.GetFormattedString("lutim", LangKey.delete_question, LutimInfo.Title), Language.GetFormattedString("lutim", LangKey.delete_title, LutimInfo.Hash), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
					if (result != DialogResult.Yes)
					{
						continue;
					}
					// Should fix Bug #3378699 
					pictureBox1.Image = pictureBox1.ErrorImage;
					try {
						new PleaseWaitForm().ShowAndWait(LutimPlugin.Attributes.Name, Language.GetString("lutim", LangKey.communication_wait), 
							delegate {
								LutimUtils.DeleteLutimImage(LutimInfo);
							}
						);
					} catch (Exception ex) {
						Log.Warn("Problem communicating with Lutim: ", ex);
					}

					LutimInfo.Dispose();
				}
			}
			Redraw();
		}

		private void ClipboardButtonClick(object sender, EventArgs e) {
			StringBuilder links = new StringBuilder();
			if (listview_Lutim_uploads.SelectedItems.Count > 0) {
				for (int i = 0; i < listview_Lutim_uploads.SelectedItems.Count; i++)
				{
					LutimInfo LutimInfo = (LutimInfo)listview_Lutim_uploads.SelectedItems[i].Tag;
					links.AppendLine(Config.UsePageLink ? LutimInfo.Page : LutimInfo.Original);
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
			if (listview_Lutim_uploads.SelectedItems.Count > 0) {
				for (int i = 0; i < listview_Lutim_uploads.SelectedItems.Count; i++) {
					LutimInfo LutimInfo = (LutimInfo)listview_Lutim_uploads.SelectedItems[i].Tag;
					System.Diagnostics.Process.Start(LutimInfo.Page);
				}
			}
		}
		
		private void listview_Lutim_uploads_ColumnClick(object sender, ColumnClickEventArgs e) {
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
			listview_Lutim_uploads.Sort();
		}


		private void LutimHistoryFormClosing(object sender, FormClosingEventArgs e)
		{
			_instance = null;
		}
	}
}
