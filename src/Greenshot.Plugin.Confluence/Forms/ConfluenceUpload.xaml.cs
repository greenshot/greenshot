/*
 * Greenshot - a free and open source screenshot tool
 * Copyright © 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using Greenshot.Plugin.Confluence.Entities;

namespace Greenshot.Plugin.Confluence.Forms
{
    /// <summary>
    /// Interaction logic for ConfluenceUpload.xaml
    /// </summary>
    public partial class ConfluenceUpload
    {
        private ConfluencePagePicker _pickerPage;

        public ConfluencePagePicker PickerPage
        {
            get
            {
                if (_pickerPage == null)
                {
                    List<Page> pages = ConfluenceUtils.GetCurrentPages();
                    if (pages != null && pages.Count > 0)
                    {
                        _pickerPage = new ConfluencePagePicker(this, pages);
                    }
                }

                return _pickerPage;
            }
        }

        private System.Windows.Controls.Page _searchPage;

        public System.Windows.Controls.Page SearchPage
        {
            get { return _searchPage ??= new ConfluenceSearch(this); }
        }

        private System.Windows.Controls.Page _browsePage;

        public System.Windows.Controls.Page BrowsePage
        {
            get { return _browsePage ??= new ConfluenceTreePicker(this); }
        }

        private Page _selectedPage;

        public Page SelectedPage
        {
            get => _selectedPage;
            set
            {
                _selectedPage = value;
                Upload.IsEnabled = _selectedPage != null;
                IsOpenPageSelected = false;
            }
        }

        public bool IsOpenPageSelected { get; set; }
        public string Filename { get; set; }

        private static DateTime _lastLoad = DateTime.Now;
        private static IList<Space> _spaces;

        public IList<Space> Spaces
        {
            get
            {
                UpdateSpaces();
                while (_spaces == null)
                {
                    Thread.Sleep(300);
                }

                return _spaces;
            }
        }

        public ConfluenceUpload(string filename)
        {
            Filename = filename;
            InitializeComponent();
            DataContext = this;
            UpdateSpaces();
            if (PickerPage != null)
            {
                return;
            }
            PickerTab.Visibility = Visibility.Collapsed;
            SearchTab.IsSelected = true;
        }

        private void UpdateSpaces()
        {
            if (_spaces != null && DateTime.Now.AddMinutes(-60).CompareTo(_lastLoad) > 0)
            {
                // Reset
                _spaces = null;
            }

            // Check if load is needed
            if (_spaces == null)
            {
                (new Thread(() =>
                {
                    _spaces = ConfluencePlugin.ConfluenceConnector.GetSpaceSummaries().OrderBy(s => s.Name).ToList();
                    _lastLoad = DateTime.Now;
                })
                {
                    Name = "Loading spaces for confluence"
                }).Start();
            }
        }

        private void Upload_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}