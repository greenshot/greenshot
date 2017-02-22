#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Confluence;
using GreenshotPlugin.IniFile;
using Page = Confluence.Page;

#endregion

namespace GreenshotConfluencePlugin
{
	public partial class ConfluenceSearch
	{
		private static readonly ConfluenceConfiguration ConfluenceConfig = IniConfig.GetIniSection<ConfluenceConfiguration>();
		private readonly ConfluenceUpload _confluenceUpload;

		public ConfluenceSearch(ConfluenceUpload confluenceUpload)
		{
			_confluenceUpload = confluenceUpload;
			DataContext = this;
			InitializeComponent();
			if (ConfluenceConfig.SearchSpaceKey == null)
			{
				SpaceComboBox.SelectedItem = Spaces.FirstOrDefault();
			}
			else
			{
				foreach (var space in Spaces)
				{
					if (space.Key.Equals(ConfluenceConfig.SearchSpaceKey))
					{
						SpaceComboBox.SelectedItem = space;
					}
				}
			}
		}

		public IEnumerable<Space> Spaces => _confluenceUpload.Spaces;

		public ObservableCollection<Page> Pages { get; } = new ObservableCollection<Page>();

		private void PageListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SelectionChanged();
		}

		private void SelectionChanged()
		{
			if (PageListView.HasItems && PageListView.SelectedItems.Count > 0)
			{
				_confluenceUpload.SelectedPage = (Page) PageListView.SelectedItem;
			}
			else
			{
				_confluenceUpload.SelectedPage = null;
			}
		}

		private void Search_Click(object sender, RoutedEventArgs e)
		{
			DoSearch();
		}

		private void DoSearch()
		{
			var spaceKey = (string) SpaceComboBox.SelectedValue;
			ConfluenceConfig.SearchSpaceKey = spaceKey;
			Pages.Clear();
			foreach (var page in ConfluencePlugin.ConfluenceConnector.SearchPages(searchText.Text, spaceKey).OrderBy(p => p.Title))
			{
				Pages.Add(page);
			}
		}

		private void SearchText_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return && Search.IsEnabled)
			{
				DoSearch();
				e.Handled = true;
			}
		}

		private void Page_Loaded(object sender, RoutedEventArgs e)
		{
			SelectionChanged();
		}

		private void searchText_TextChanged(object sender, TextChangedEventArgs e)
		{
			Search.IsEnabled = !string.IsNullOrEmpty(searchText.Text);
		}
	}
}