/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Greenshot.Base.Controls;
using Greenshot.Base.Core;
using Greenshot.Base.Threading;

namespace Greenshot.Plugin.Imgur.Forms;

/// <summary>
/// Imgur history form
/// </summary>
public sealed partial class ImgurHistory : ImgurForm
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ImgurHistory));
    private readonly GreenshotColumnSorter _columnSorter;
    private static readonly IImgurConfiguration Config = IniConfigHelper.EnsureSection<IImgurConfiguration>(() => new ImgurConfigurationImpl());
    private static ImgurHistory _instance;

    // Only accessed on the UI thread: a second request while the history loads is ignored
    private static bool _isLoading;

    /// <summary>
    /// Load the history (if needed) and show it, call on the UI thread.
    /// </summary>
    public static async Task ShowHistoryAsync()
    {
        if (_isLoading)
        {
            return;
        }

        if (ImgurUtils.IsHistoryLoadingNeeded())
        {
            _isLoading = true;
            try
            {
                await UserInteraction.Current.RunWithProgressAsync("Imgur " + Language.GetString("imgur", LangKey.history),
                    (progress, token) => ImgurUtils.LoadHistoryAsync(token), CancellationToken.None);
            }
            finally
            {
                _isLoading = false;
            }
        }

        // Make sure the history is loaded, will be done only once
        if (_instance == null || _instance.IsDisposed)
        {
#pragma warning disable CS0618 // the one place which may create the form
            _instance = new ImgurHistory();
#pragma warning restore CS0618
        }

        if (!_instance.Visible)
        {
            _instance.Show();
        }

        _instance.Redraw();
        _instance.BringToFront();
    }

    /// <summary>
    /// Parameterless constructor required for Windows Forms designer support.
    /// Callers should use <see cref="ShowHistoryAsync"/> to display the singleton instance.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use ImgurHistory.ShowHistoryAsync() instead.", false)]
    public ImgurHistory()
    {
        // Only the UI thread creates the form, no lock needed
        if (_instance != null && !_instance.IsDisposed && _instance != this)
        {
            try
            {
                _instance.Close();
            }
            catch
            {
                // Ignore
            }
        }
        _instance = this;

        //
        // The InitializeComponent() call is required for Windows Forms designer support.
        //
        InitializeComponent();
        InitializeLanguage();
        AcceptButton = finishedButton;
        CancelButton = finishedButton;
        // Init sorting
        _columnSorter = new GreenshotColumnSorter();
        listview_imgur_uploads.ListViewItemSorter = _columnSorter;
        _columnSorter.SortColumn = 3;
        _columnSorter.Order = SortOrder.Descending;
        Redraw();
        if (listview_imgur_uploads.Items.Count > 0)
        {
            listview_imgur_uploads.Items[0].Selected = true;
        }
    }

    /// <inheritdoc />
    protected override void InitializeLanguage()
    {
        Text = Language.GetString("imgur.history");
        deleteButton.Text = Language.GetString("imgur.history.delete");
        openButton.Text = Language.GetString("imgur.history.open");
        finishedButton.Text = Language.GetString("imgur.OK");
        clipboardButton.Text = Language.GetString("imgur.history.copy_to_clipboard");
        clearHistoryButton.Text = Language.GetString("imgur.history.clear");
    }

    private void Redraw()
    {
        // Should fix Bug #3378699 
        pictureBox1.Image = pictureBox1.ErrorImage;
        listview_imgur_uploads.BeginUpdate();
        listview_imgur_uploads.Items.Clear();
        listview_imgur_uploads.Columns.Clear();
        string[] columns =
        {
            "hash", "title", "deleteHash", "Date"
        };
        foreach (string column in columns)
        {
            listview_imgur_uploads.Columns.Add(column);
        }

        if (Config.RuntimeImgurHistory != null)
        {
            foreach (ImgurInfo imgurInfo in Config.RuntimeImgurHistory.Values)
            {
                var item = new ListViewItem(imgurInfo.Hash)
                {
                    Tag = imgurInfo
                };
                item.SubItems.Add(imgurInfo.Title);
                item.SubItems.Add(imgurInfo.DeleteHash);
                item.SubItems.Add(imgurInfo.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", DateTimeFormatInfo.InvariantInfo));
                listview_imgur_uploads.Items.Add(item);
            }
        }

        for (int i = 0; i < columns.Length; i++)
        {
            listview_imgur_uploads.AutoResizeColumn(i, ColumnHeaderAutoResizeStyle.ColumnContent);
        }

        listview_imgur_uploads.EndUpdate();
        listview_imgur_uploads.Refresh();
        deleteButton.Enabled = false;
        openButton.Enabled = false;
        clipboardButton.Enabled = false;
    }

    private void Listview_imgur_uploadsSelectedIndexChanged(object sender, EventArgs e)
    {
        pictureBox1.Image = pictureBox1.ErrorImage;
        if (listview_imgur_uploads.SelectedItems.Count > 0)
        {
            deleteButton.Enabled = true;
            openButton.Enabled = true;
            clipboardButton.Enabled = true;
            if (listview_imgur_uploads.SelectedItems.Count == 1)
            {
                ImgurInfo imgurInfo = (ImgurInfo) listview_imgur_uploads.SelectedItems[0].Tag;
                pictureBox1.Image = imgurInfo.Image;
            }
        }
        else
        {
            pictureBox1.Image = pictureBox1.ErrorImage;
            deleteButton.Enabled = false;
            openButton.Enabled = false;
            clipboardButton.Enabled = false;
        }
    }

    private void DeleteButtonClick(object sender, EventArgs e)
    {
        var toDelete = new List<ImgurInfo>();
        for (int i = 0; i < listview_imgur_uploads.SelectedItems.Count; i++)
        {
            ImgurInfo imgurInfo = (ImgurInfo) listview_imgur_uploads.SelectedItems[i].Tag;
            DialogResult result = MessageBox.Show(Language.GetFormattedString("imgur", LangKey.delete_question, imgurInfo.Title),
                Language.GetFormattedString("imgur", LangKey.delete_title, imgurInfo.Hash), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                toDelete.Add(imgurInfo);
            }
        }

        AsyncCommand.Run(() => DeleteAsync(toDelete), "Delete Imgur images");
    }

    private async Task DeleteAsync(IList<ImgurInfo> toDelete)
    {
        foreach (var imgurInfo in toDelete)
        {
            // Should fix Bug #3378699
            pictureBox1.Image = pictureBox1.ErrorImage;
            try
            {
                await UserInteraction.Current.RunWithProgressAsync("Imgur", (progress, token) => ImgurUtils.DeleteImgurImageAsync(imgurInfo, token), CancellationToken.None);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Warn("Problem communicating with Imgur: ", ex);
            }

            imgurInfo.Dispose();
        }

        Redraw();
    }

    private void ClipboardButtonClick(object sender, EventArgs e)
    {
        StringBuilder links = new StringBuilder();
        if (listview_imgur_uploads.SelectedItems.Count > 0)
        {
            for (int i = 0; i < listview_imgur_uploads.SelectedItems.Count; i++)
            {
                ImgurInfo imgurInfo = (ImgurInfo) listview_imgur_uploads.SelectedItems[i].Tag;
                links.AppendLine(Config.UsePageLink ? imgurInfo.Page : imgurInfo.Original);
            }
        }

        ClipboardHelper.SetClipboardData(links.ToString());
    }

    private void ClearHistoryButtonClick(object sender, EventArgs e)
    {
        DialogResult result = MessageBox.Show(Language.GetString("imgur", LangKey.clear_question), "Imgur", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result == DialogResult.Yes)
        {
            Config.RuntimeImgurHistory.Clear();
            Config.ImgurUploadHistory.Clear();
            Redraw();
        }
    }

    private void FinishedButtonClick(object sender, EventArgs e)
    {
        Hide();
    }

    private void OpenButtonClick(object sender, EventArgs e)
    {
        if (listview_imgur_uploads.SelectedItems.Count > 0)
        {
            for (int i = 0; i < listview_imgur_uploads.SelectedItems.Count; i++)
            {
                ImgurInfo imgurInfo = (ImgurInfo) listview_imgur_uploads.SelectedItems[i].Tag;
                System.Diagnostics.Process.Start(imgurInfo.Page);
            }
        }
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


    private void ImgurHistoryFormClosing(object sender, FormClosingEventArgs e)
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
