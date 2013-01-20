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
using System.Threading;
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
					pickerPage = new ConfluencePagePicker(this);
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

		private Confluence.Page selectedPage = null;
		public Confluence.Page SelectedPage {
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
		
		private static DateTime lastLoad = DateTime.Now;
		private static List<Confluence.Space> spaces;
		public List<Confluence.Space> Spaces {
			get {
				updateSpaces();
				while (spaces == null) {
					Thread.Sleep(300);
				}
				return spaces;
			}
		}

		public ConfluenceUpload(string filename) {
			this.Filename = filename;
			InitializeComponent();
			this.DataContext = this;
			updateSpaces();
		}
		
		void updateSpaces() {
			if (spaces != null && DateTime.Now.AddMinutes(-60).CompareTo(lastLoad) > 0) {
				// Reset
				spaces = null;
			}
			// Check if load is needed
			if (spaces == null) {
				(new Thread(() => {
				     spaces = ConfluencePlugin.ConfluenceConnector.getSpaceSummaries();
				     lastLoad = DateTime.Now;
				  }) { Name = "Loading spaces for confluence"}).Start();
			}
		}

		void Upload_Click(object sender, RoutedEventArgs e) {
			DialogResult = true;
		}
	}
}