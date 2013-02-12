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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

using Confluence;
using GreenshotPlugin.Core;
using Greenshot.IniFile;

namespace GreenshotConfluencePlugin {
	public partial class ConfluenceSearch : System.Windows.Controls.Page {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ConfluenceSearch));
		private static ConfluenceConfiguration config = IniConfig.GetIniSection<ConfluenceConfiguration>();
		private ConfluenceUpload confluenceUpload;
		
		public List<Confluence.Space> Spaces {
			get {
				return confluenceUpload.Spaces;
			}
		}

		private ObservableCollection<Confluence.Page> pages = new ObservableCollection<Confluence.Page>();
		public ObservableCollection<Confluence.Page> Pages {
			get {
				return pages;
			}
		}

		public ConfluenceSearch(ConfluenceUpload confluenceUpload) {
			this.confluenceUpload = confluenceUpload;
			this.DataContext = this;
			InitializeComponent();
			if (config.SearchSpaceKey == null) {
				this.SpaceComboBox.SelectedItem = Spaces[0];
			} else {
				foreach(Confluence.Space space in Spaces) {
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
				confluenceUpload.SelectedPage = (Confluence.Page)PageListView.SelectedItem;
			} else {
				confluenceUpload.SelectedPage = null;
			}
		}
		
		void Search_Click(object sender, RoutedEventArgs e) {
			doSearch();
		}
		void doSearch() {
			string spaceKey = (string)SpaceComboBox.SelectedValue;
			config.SearchSpaceKey = spaceKey;
			List<Confluence.Page> searchResult = ConfluencePlugin.ConfluenceConnector.searchPages(searchText.Text, spaceKey);
			pages.Clear();
			foreach(Confluence.Page page in searchResult) {
				pages.Add(page);
			}
		}
		void SearchText_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
			if (e.Key == System.Windows.Input.Key.Return) {
        		doSearch();
        		e.Handled = true;
    		}
		}
		
		void Page_Loaded(object sender,  System.Windows.RoutedEventArgs e) {
			SelectionChanged();
		}
	}
}