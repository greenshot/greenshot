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
using System.Windows;
using System.Windows.Controls;

namespace GreenshotConfluencePlugin {
	/// <summary>
	/// Interaction logic for ConfluenceUpload.xaml
	/// </summary>
	public partial class ConfluenceUpload : Window {
		private Page pickerPage = null;
		public Page PickerPage {
			get {
				if (pickerPage == null) {
					IList<PageDetails> pages = null; // TODO: ConfluenceUtils.GetCurrentPages();
					if (pages != null && pages.Count > 0) {
						pickerPage = new ConfluencePagePicker(this, pages);
					}
				}
				return pickerPage;
			}
		}

		private Page searchPage = null;
		public Page SearchPage {
			get {
				if (searchPage == null) {
					searchPage = new ConfluenceSearch(this);
				}
				return searchPage;
			}
		}

		private Page browsePage = null;
		public Page BrowsePage {
			get {
				if (browsePage == null) {
					browsePage = new ConfluenceTreePicker(this);
				}
				return browsePage;
			}
		}

		private PageDetails selectedPage = null;
		public PageDetails SelectedPage {
			get {
				return selectedPage;
			}
			set {
				selectedPage = value;
				if (selectedPage != null) {
					Upload.IsEnabled = true;
				} else {
					Upload.IsEnabled = false;
				}
				isOpenPageSelected = false;
			}
		}

		public bool isOpenPageSelected {
			get;
			set;
		}
		public string Filename {
			get;
			set;
		}
		
		public dynamic Spaces {
			get {
				return ConfluencePlugin.Spaces;
			}
		}

		public ConfluenceUpload(string filename) {
			this.Filename = filename;
			InitializeComponent();
			this.DataContext = this;
			//var ignoreTask = updateSpaces();
			if (PickerPage == null) {
				PickerTab.Visibility = System.Windows.Visibility.Collapsed;
				SearchTab.IsSelected = true;
			}
		}
		
		void Upload_Click(object sender, RoutedEventArgs e) {
			DialogResult = true;
		}
	}
}