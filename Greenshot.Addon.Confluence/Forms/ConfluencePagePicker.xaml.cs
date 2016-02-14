/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Collections.Generic;
using GreenshotConfluencePlugin.Model;

namespace GreenshotConfluencePlugin.Forms
{
	/// <summary>
	/// Interaction logic for ConfluencePagePicker.xaml
	/// </summary>
	public partial class ConfluencePagePicker : System.Windows.Controls.Page
	{
		private static readonly Serilog.ILogger LOG = Serilog.Log.Logger.ForContext(typeof(ConfluencePagePicker));
		private ConfluenceUpload confluenceUpload = null;

		public ConfluencePagePicker(ConfluenceUpload confluenceUpload, IList<Content> pagesToPick)
		{
			this.confluenceUpload = confluenceUpload;
			this.DataContext = pagesToPick;
			InitializeComponent();
		}

		private void PageListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			SelectionChanged();
		}

		private void SelectionChanged()
		{
			if (PageListView.HasItems && PageListView.SelectedItems.Count > 0)
			{
				confluenceUpload.SelectedPage = (Content) PageListView.SelectedItem;
				// Make sure the uploader knows we selected an already opened page
				confluenceUpload.isOpenPageSelected = true;
			}
			else
			{
				confluenceUpload.SelectedPage = null;
			}
		}

		private void Page_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			SelectionChanged();
		}
	}
}