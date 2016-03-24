/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Dapplo.Confluence.Entities;

namespace Greenshot.Addon.Confluence.Windows
{
	/// <summary>
	/// Interaction logic for ConfluenceUpload.xaml
	/// </summary>
	public partial class ConfluenceUpload : Window
	{
		private Page _pickerPage;

		public Page PickerPage
		{
			get
			{
				if (_pickerPage == null)
				{
					// TODO: Do not run async code from synchronous code
					var pages = Task.Run(async () => await ConfluenceUtils.GetCurrentPages()).Result;
					if (pages != null && pages.Count > 0)
					{
						_pickerPage = new ConfluencePagePicker(this, pages);
					}
				}
				return _pickerPage;
			}
		}

		private Page _browsePage;

		public Page BrowsePage
		{
			get
			{
				if (_browsePage == null)
				{
					_browsePage = new ConfluenceTreePicker(this);
				}
				return _browsePage;
			}
		}

		private Content _selectedPage;

		public Content SelectedPage
		{
			get
			{
				return _selectedPage;
			}
			set
			{
				_selectedPage = value;
				if (_selectedPage != null)
				{
					Upload.IsEnabled = true;
				}
				else
				{
					Upload.IsEnabled = false;
				}
				IsOpenPageSelected = false;
			}
		}

		public bool IsOpenPageSelected
		{
			get;
			set;
		}

		public string Filename
		{
			get;
			set;
		}

		public ConfluenceUpload(string filename)
		{
			Filename = filename;
			InitializeComponent();
			DataContext = this;
			if (PickerPage == null)
			{
				BrowseTab.IsSelected = true;
			}
		}

		private void Upload_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}
	}
}