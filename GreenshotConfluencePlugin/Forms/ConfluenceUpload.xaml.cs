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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace GreenshotConfluencePlugin {
	/// <summary>
	/// Interaction logic for ConfluenceUpload.xaml
	/// </summary>
	public partial class ConfluenceUpload : Window {
		private Page _pickerPage;
		public Page PickerPage {
			get {
				if (_pickerPage == null) {
					List<Confluence.Page> pages = ConfluenceUtils.GetCurrentPages();
					if (pages != null && pages.Count > 0) {
						_pickerPage = new ConfluencePagePicker(this, pages);
					}
				}
				return _pickerPage;
			}
		}

		private Page _searchPage;
		public Page SearchPage {
			get {
				if (_searchPage == null) {
					_searchPage = new ConfluenceSearch(this);
				}
				return _searchPage;
			}
		}

		private Page _browsePage;
		public Page BrowsePage {
			get {
				if (_browsePage == null) {
					_browsePage = new ConfluenceTreePicker(this);
				}
				return _browsePage;
			}
		}

		private Confluence.Page _selectedPage;
		public Confluence.Page SelectedPage {
			get {
				return _selectedPage;
			}
			set {
				_selectedPage = value;
				if (_selectedPage != null) {
					Upload.IsEnabled = true;
				} else {
					Upload.IsEnabled = false;
				}
				IsOpenPageSelected = false;
			}
		}

		public bool IsOpenPageSelected {
			get;
			set;
		}
		public string Filename {
			get;
			set;
		}
		
		private static DateTime _lastLoad = DateTime.Now;
		private static IList<Confluence.Space> _spaces;
		public IList<Confluence.Space> Spaces {
			get {
				UpdateSpaces();
				while (_spaces == null) {
					Thread.Sleep(300);
				}
				return _spaces;
			}
		}

		public ConfluenceUpload(string filename) {
			Filename = filename;
			InitializeComponent();
			DataContext = this;
			UpdateSpaces();
			if (PickerPage == null) {
				PickerTab.Visibility = Visibility.Collapsed;
				SearchTab.IsSelected = true;
			}
		}

		private void UpdateSpaces() {
			if (_spaces != null && DateTime.Now.AddMinutes(-60).CompareTo(_lastLoad) > 0) {
				// Reset
				_spaces = null;
			}
			// Check if load is needed
			if (_spaces == null) {
				(new Thread(() => {
				     _spaces = ConfluencePlugin.ConfluenceConnector.GetSpaceSummaries().OrderBy(s => s.Name).ToList();
				     _lastLoad = DateTime.Now;
				  }) { Name = "Loading spaces for confluence"}).Start();
			}
		}

		private void Upload_Click(object sender, RoutedEventArgs e) {
			DialogResult = true;
		}
	}
}