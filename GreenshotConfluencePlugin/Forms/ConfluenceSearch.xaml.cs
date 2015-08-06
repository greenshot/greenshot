/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.ObjectModel;
using System.Windows;
using Dapplo.Config.Ini;
using System.Threading.Tasks;

namespace GreenshotConfluencePlugin
{
	public partial class ConfluenceSearch : System.Windows.Controls.Page {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ConfluenceSearch));
		private static ConfluenceConfiguration config = IniConfig.Get("Greenshot", "greenshot").Get<ConfluenceConfiguration>();
		private ConfluenceUpload _confluenceUpload;
		
		public dynamic Spaces {
			get {
				return _confluenceUpload.Spaces;
			}
		}

		private ObservableCollection<PageDetails> pages = new ObservableCollection<PageDetails>();
		public ObservableCollection<PageDetails> Pages {
			get {
				return pages;
			}
		}

		public ConfluenceSearch(ConfluenceUpload confluenceUpload) {
			this._confluenceUpload = confluenceUpload;
			this.DataContext = this;
			InitializeComponent();
			if (config.SearchSpaceKey == null) {
				this.SpaceComboBox.SelectedItem = Spaces[0];
			} else {
				foreach(var space in Spaces) {
					if (space.Key.Equals(config.SearchSpaceKey)) {
						this.SpaceComboBox.SelectedItem = space;
					}
				}
			}
		}

		void PageListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
			SelectionChanged();
		}
		
		void SelectionChanged() {
			if (PageListView.HasItems && PageListView.SelectedItems.Count > 0) {
				_confluenceUpload.SelectedPage = (PageDetails)PageListView.SelectedItem;
			} else {
				_confluenceUpload.SelectedPage = null;
			}
		}
		
		async void Search_Click(object sender, RoutedEventArgs e) {
			await doSearch();
		}

		async Task doSearch() {
			string spaceKey = (string)SpaceComboBox.SelectedValue;
			config.SearchSpaceKey = spaceKey;
			var searchResult = await ConfluencePlugin.ConfluenceAPI.SearchAsync(searchText.Text);
			pages.Clear();
			foreach(var page in searchResult) {
				pages.Add(page);
			}
		}

		async void SearchText_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
			if (e.Key == System.Windows.Input.Key.Return && Search.IsEnabled) {
        		await doSearch();
        		e.Handled = true;
			}
		}
		
		void Page_Loaded(object sender,  System.Windows.RoutedEventArgs e) {
			SelectionChanged();
		}

		private void searchText_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {
			Search.IsEnabled = !string.IsNullOrEmpty(searchText.Text);
		}
	}
}