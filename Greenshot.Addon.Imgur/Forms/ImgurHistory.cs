//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Ini;
using Dapplo.Log;
using Greenshot.Addon.Controls;
using Greenshot.Addon.Core;
using Greenshot.Addon.Windows;
using Greenshot.CaptureCore;
using Greenshot.Legacy.Controls;
using Greenshot.Legacy.Utils;

#endregion

namespace Greenshot.Addon.Imgur.Forms
{
	/// <summary>
	///     Description of ImgurHistory.
	/// </summary>
	public partial class ImgurHistory : ImgurForm
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly IImgurConfiguration ImgurConfiguration = IniConfig.Current.Get<IImgurConfiguration>();

		private static readonly string[] Columns =
		{
			"hash", "title", "deleteHash", "Date"
		};

		private static readonly ImgurHistory Instance = new ImgurHistory();
		private readonly GreenshotColumnSorter _columnSorter;

		private ImgurHistory()
		{
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
			ApplyLanguage();
			ClearWindow();
			Shown += async (sender, eventArgs) =>
			{
				Text = ImgurLanguage.History + " Loading...";
				BeginRedrawWindow();
				ClearWindow();
				EndRedrawWindow();
				await LoadHistory();
				await ImgurUtils.RetrieveImgurCredits();
				if (ImgurConfiguration.Credits > 0)
				{
					Text = ImgurLanguage.History + " (" + ImgurConfiguration.Credits + " credits)";
				}
			};
		}

		private void AddImgurItem(ImageInfo imgurInfo)
		{
			listview_imgur_uploads.BeginUpdate();
			var item = new ListViewItem(imgurInfo.Id)
			{
				Tag = imgurInfo
			};
			item.SubItems.Add(imgurInfo.Title);
			item.SubItems.Add(imgurInfo.DeleteHash);
			item.SubItems.Add(imgurInfo.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", DateTimeFormatInfo.InvariantInfo));
			listview_imgur_uploads.Items.Add(item);
			for (int i = 0; i < Columns.Length; i++)
			{
				listview_imgur_uploads.Columns[i].Width = -2;
			}
			listview_imgur_uploads.EndUpdate();
			listview_imgur_uploads.Refresh();
		}

		private void BeginRedrawWindow()
		{
			// Should fix Bug #3378699 
			pictureBox1.Image = null;
			deleteButton.Enabled = false;
			openButton.Enabled = false;
			clipboardButton.Enabled = false;

			listview_imgur_uploads.BeginUpdate();
		}

		private void ClearHistoryButtonClick(object sender, EventArgs e)
		{
			var result = MessageBox.Show(ImgurLanguage.ClearQuestion, "Imgur", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (result == DialogResult.Yes)
			{
				ImgurConfiguration.RuntimeImgurHistory.Clear();
				ImgurConfiguration.ImgurUploadHistory.Clear();
				Redraw();
			}
		}

		private void ClearWindow()
		{
			BeginRedrawWindow();
			listview_imgur_uploads.Items.Clear();
			listview_imgur_uploads.Columns.Clear();
			foreach (var column in Columns)
			{
				listview_imgur_uploads.Columns.Add(column);
			}
			EndRedrawWindow();
		}

		private void ClipboardButtonClick(object sender, EventArgs e)
		{
			var links = new StringBuilder();
			if ((listview_imgur_uploads.SelectedItems.Count > 0))
			{
				for (int i = 0; i < listview_imgur_uploads.SelectedItems.Count; i++)
				{
					var imgurInfo = (ImageInfo) listview_imgur_uploads.SelectedItems[i].Tag;
					links.AppendLine(ImgurConfiguration.UsePageLink ? imgurInfo.Page.AbsoluteUri : imgurInfo.Original.AbsoluteUri);
				}
			}
			ClipboardHelper.SetClipboardData(links.ToString());
		}

		private async void DeleteButtonClick(object sender, EventArgs e)
		{
			if ((listview_imgur_uploads.SelectedItems.Count > 0))
			{
				for (int i = 0; i < listview_imgur_uploads.SelectedItems.Count; i++)
				{
					var imgurInfo = (ImageInfo) listview_imgur_uploads.SelectedItems[i].Tag;
					var result = MessageBox.Show(string.Format(ImgurLanguage.DeleteQuestion, imgurInfo.Title), string.Format(ImgurLanguage.DeleteTitle, imgurInfo.Id), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
					if (result == DialogResult.Yes)
					{
						// Should fix Bug #3378699 
						pictureBox1.Image = null;

						await PleaseWaitWindow.CreateAndShowAsync("Imgur plug-in", ImgurLanguage.CommunicationWait, async (progress, pleaseWaitToken) => { return await ImgurUtils.DeleteImgurImageAsync(imgurInfo, pleaseWaitToken); });

						imgurInfo.Dispose();
					}
				}
			}

			Redraw();
		}

		private void EndRedrawWindow()
		{
			listview_imgur_uploads.EndUpdate();
			listview_imgur_uploads.Refresh();
		}

		private void listview_imgur_uploads_ColumnClick(object sender, ColumnClickEventArgs e)
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
			listview_imgur_uploads.Sort();
		}

		private void Listview_imgur_uploadsSelectedIndexChanged(object sender, EventArgs e)
		{
			pictureBox1.Image = null;
			if ((listview_imgur_uploads.SelectedItems.Count > 0))
			{
				deleteButton.Enabled = true;
				openButton.Enabled = true;
				clipboardButton.Enabled = true;
				if (listview_imgur_uploads.SelectedItems.Count == 1)
				{
					var imgurInfo = (ImageInfo) listview_imgur_uploads.SelectedItems[0].Tag;
					pictureBox1.Image = imgurInfo.Image;
				}
			}
			else
			{
				pictureBox1.Image = null;
				deleteButton.Enabled = false;
				openButton.Enabled = false;
				clipboardButton.Enabled = false;
			}
		}

		/// <summary>
		///     Load the complete history of the imgur uploads, with the corresponding information
		/// </summary>
		private async Task LoadHistory(CancellationToken token = default(CancellationToken))
		{
			bool saveNeeded = false;

			// Load the ImUr history
			foreach (string hash in ImgurConfiguration.ImgurUploadHistory.Keys)
			{
				if (ImgurConfiguration.RuntimeImgurHistory.ContainsKey(hash))
				{
					// Already loaded, only add it to the view
					AddImgurItem(ImgurConfiguration.RuntimeImgurHistory[hash]);
					continue;
				}
				try
				{
					var imgurInfo = await ImgurUtils.RetrieveImgurInfoAsync(hash, ImgurConfiguration.ImgurUploadHistory[hash], token);
					if (imgurInfo != null)
					{
						await ImgurUtils.RetrieveImgurThumbnailAsync(imgurInfo, token);
						ImgurConfiguration.RuntimeImgurHistory.Add(hash, imgurInfo);
						// Already loaded, only add it to the view
						AddImgurItem(imgurInfo);
					}
					else
					{
						Log.Debug().WriteLine("Deleting not found ImgUr {0} from config.", hash);
						ImgurConfiguration.ImgurUploadHistory.Remove(hash);
						saveNeeded = true;
					}
				}
				catch (Exception e)
				{
					Log.Error().WriteLine(e, "Problem loading ImgUr history for hash {0}", hash);
				}
			}
			if (saveNeeded)
			{
				// Save needed changes
				// IniConfig.Save();
			}
		}

		private void OpenButtonClick(object sender, EventArgs e)
		{
			if ((listview_imgur_uploads.SelectedItems.Count > 0))
			{
				for (int i = 0; i < listview_imgur_uploads.SelectedItems.Count; i++)
				{
					var imgurInfo = (ImageInfo) listview_imgur_uploads.SelectedItems[i].Tag;
					Process.Start(imgurInfo.Page.AbsoluteUri);
				}
			}
		}

		/// <summary>
		///     Redraw all
		/// </summary>
		private void Redraw()
		{
			BeginRedrawWindow();
			ClearWindow();
			foreach (var imgurInfo in ImgurConfiguration.RuntimeImgurHistory.Values)
			{
				AddImgurItem(imgurInfo);
			}
			EndRedrawWindow();
		}

		public static void ShowHistory()
		{
			Instance.ShowDialog();
		}
	}
}