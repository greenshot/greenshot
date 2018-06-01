#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Greenshot.Addons.Controls;
using Greenshot.Gfx;

#endregion

namespace Greenshot.Addon.ExternalCommand
{
	/// <summary>
	///     External Command settings form
	/// </summary>
	public partial class SettingsForm : GreenshotForm
	{
	    private readonly IExternalCommandLanguage _externalCommandLanguage;
	    private readonly IExternalCommandConfiguration _externalCommandConfiguration;
		private readonly IList<Image> _images = new List<Image>();
		public SettingsForm(IExternalCommandConfiguration externalCommandConfiguration, IExternalCommandLanguage externalCommandLanguage) : base(externalCommandLanguage)
		{
		    _externalCommandConfiguration = externalCommandConfiguration;
            _externalCommandLanguage = externalCommandLanguage;
		    //
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			AcceptButton = buttonOk;
			CancelButton = buttonCancel;
			UpdateView();
		}

		private void ButtonOkClick(object sender, EventArgs e)
		{
			//IniConfig.Save();
		}

		private void ButtonAddClick(object sender, EventArgs e)
		{
			var form = new SettingsFormDetail(null, _externalCommandLanguage);
			form.ShowDialog();

			UpdateView();
		}

		private void ButtonDeleteClick(object sender, EventArgs e)
		{
			foreach (ListViewItem item in listView.SelectedItems)
			{
				var commando = item.Tag as string;

				_externalCommandConfiguration.Delete(commando);
			}
			UpdateView();
		}

		private void DisposeImages()
		{
			// Dispose all images
			foreach (var image in _images)
			{
				image.Dispose();
			}
			_images.Clear();
		}

		private void UpdateView()
		{
			listView.Items.Clear();
			DisposeImages();
			if (_externalCommandConfiguration.Commands != null)
			{
				listView.ListViewItemSorter = new ListviewComparer();
				var imageList = new ImageList();
				listView.SmallImageList = imageList;
				var imageNr = 0;
				foreach (var commando in _externalCommandConfiguration.Commands)
				{
					ListViewItem item;
					var iconForExe = IconCache.IconForCommand(commando, FormDpiHandler.Dpi > 100);
					if (iconForExe != null)
					{
						var image = iconForExe.ScaleIconForDisplaying(FormDpiHandler.Dpi);
						if (!Equals(image, iconForExe))
						{
							_images.Add(image);
						}
						imageList.Images.Add(image);
						item = new ListViewItem(commando, imageNr++);
					}
					else
					{
						item = new ListViewItem(commando);
					}
					item.Tag = commando;
					listView.Items.Add(item);
				}
			}
			// Fix for bug #1484, getting an ArgumentOutOfRangeException as there is nothing selected but the edit button was still active.
			button_edit.Enabled = listView.SelectedItems.Count > 0;
		}

		private void ListView1ItemSelectionChanged(object sender, EventArgs e)
		{
			button_edit.Enabled = listView.SelectedItems.Count > 0;
		}

		private void ButtonEditClick(object sender, EventArgs e)
		{
			ListView1DoubleClick(sender, e);
		}

		private void ListView1DoubleClick(object sender, EventArgs e)
		{
			// Safety check for bug #1484
			var selectionActive = listView.SelectedItems.Count > 0;
			if (!selectionActive)
			{
				button_edit.Enabled = false;
				return;
			}
			var commando = listView.SelectedItems[0].Tag as string;

			var form = new SettingsFormDetail(commando, _externalCommandLanguage);
			form.ShowDialog();

			UpdateView();
		}
	}
}