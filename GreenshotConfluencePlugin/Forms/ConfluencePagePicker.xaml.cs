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
using System;
using Confluence;

namespace GreenshotConfluencePlugin {
	/// <summary>
	/// Interaction logic for ConfluencePagePicker.xaml
	/// </summary>
	public partial class ConfluencePagePicker : System.Windows.Controls.Page {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ConfluencePagePicker));
		private ConfluenceUpload confluenceUpload = null;

		public ConfluencePagePicker(ConfluenceUpload confluenceUpload) {
			this.confluenceUpload = confluenceUpload;
			this.DataContext = ConfluenceUtils.GetCurrentPages();
			InitializeComponent();
		}
		
		void PageListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
			SelectionChanged();
		}
		
		void SelectionChanged() {
			if (PageListView.HasItems && PageListView.SelectedItems.Count > 0) {
				confluenceUpload.SelectedPage = (Page)PageListView.SelectedItem;
				// Make sure the uploader knows we selected an already opened page
				confluenceUpload.isOpenPageSelected = true;
			} else {
				confluenceUpload.SelectedPage = null;
			}
		}
		
		void Page_Loaded(object sender, System.Windows.RoutedEventArgs e) {
			SelectionChanged();
		}
	}
}