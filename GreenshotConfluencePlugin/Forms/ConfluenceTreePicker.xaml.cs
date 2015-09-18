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

using Dapplo.Config.Ini;
using GreenshotConfluencePlugin.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace GreenshotConfluencePlugin {
	/// <summary>
	/// Interaction logic for ConfluenceTreePicker.xaml
	/// </summary>
	public partial class ConfluenceTreePicker : System.Windows.Controls.Page {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ConfluenceTreePicker));
		private static readonly IConfluenceConfiguration config = IniConfig.Current.Get<IConfluenceConfiguration>();
		private ConfluenceUpload _confluenceUpload;

		public ConfluenceTreePicker(ConfluenceUpload confluenceUpload) {
			_confluenceUpload = confluenceUpload;
			InitializeComponent();
		}

		void pageTreeViewItem_Click(object sender, RoutedEventArgs eventArgs) {
			LOG.Debug("pageTreeViewItem_Click is called!");
			TreeViewItem clickedItem = sender as TreeViewItem;
			if (clickedItem ==null) {
				return;
			}
			var page = clickedItem.Tag as Content;
			if (page == null) {
				return;
			}
			_confluenceUpload.SelectedPage = page;
			LOG.Debug("Page selected: " + page.Title);

			if (!clickedItem.HasItems) {
				LOG.Debug("Loading pages for page: " + page.Title);
				Task.Factory.StartNew(async () => {
					var pages = await ConfluencePlugin.ConfluenceAPI.GetChildrenAsync(page.Id);
					foreach (var childPage in pages) {
						LOG.Debug("Adding page: " + childPage.Title);
						TreeViewItem pageTreeViewItem = new TreeViewItem();
						pageTreeViewItem.Header = childPage.Title;
						pageTreeViewItem.Tag = childPage;
						clickedItem.Items.Add(pageTreeViewItem);
						pageTreeViewItem.MouseUp += pageTreeViewItem_Click;
					}
					if (clickedItem.HasItems) {
						clickedItem.ExpandSubtree();
					}
				}, default(CancellationToken), TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
			}
			eventArgs.Handled = true;
		}
		
		void Page_Loaded(object sender, RoutedEventArgs e) {
			_confluenceUpload.SelectedPage = null;
			foreach (var spaceLV in ConfluencePlugin.ConfluenceAPI.Model.Spaces.Values) {
				var space = spaceLV;	// Capture loop variable for the lambda later
				if (space.IsPersonal && !config.IncludePersonSpaces) {
					continue;
				}

				var spaceTreeViewItem = new TreeViewItem();
				spaceTreeViewItem.Header = space.Name;
				spaceTreeViewItem.Tag = space;
				ConfluenceTreeView.Items.Add(spaceTreeViewItem);

				// Get homepage, in background
				var loadPageTask = Task.Factory.StartNew(async () => {
					var page = await ConfluencePlugin.ConfluenceAPI.GetContentAsync(space.ContentId);
					if (page != null) {
						var pageTreeViewItem = new TreeViewItem();
						pageTreeViewItem.Header = page.Title;
						pageTreeViewItem.Tag = page;
						pageTreeViewItem.MouseUp += pageTreeViewItem_Click;
						spaceTreeViewItem.Items.Add(pageTreeViewItem);
					} else {
						spaceTreeViewItem.IsEnabled = false;
					}
				}, default(CancellationToken), TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
			}
		}
	}
}