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

using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Dapplo.Config.Ini;
using Dapplo.Confluence.Entities;
using Dapplo.LogFacade;

namespace Greenshot.Addon.Confluence.Windows
{
	/// <summary>
	/// Interaction logic for ConfluenceTreePicker.xaml
	/// </summary>
	public partial class ConfluenceTreePicker
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly IConfluenceConfiguration Config = IniConfig.Current.Get<IConfluenceConfiguration>();
		private readonly ConfluenceUpload _confluenceUpload;

		public ConfluenceTreePicker(ConfluenceUpload confluenceUpload)
		{
			_confluenceUpload = confluenceUpload;
			InitializeComponent();
		}

		private void pageTreeViewItem_Click(object sender, RoutedEventArgs eventArgs)
		{
			Log.Debug().WriteLine("pageTreeViewItem_Click is called!");
			var clickedItem = sender as TreeViewItem;
			var page = clickedItem?.Tag as Content;
			if (page == null)
			{
				return;
			}
			_confluenceUpload.SelectedPage = page;
			Log.Debug().WriteLine("Page selected: {0}", page.Title);

			if (!clickedItem.HasItems)
			{
				Log.Debug().WriteLine("Loading pages for page: {0}", page.Title);
				Task.Factory.StartNew(async () =>
				{
					var pages = await ConfluencePlugin.ConfluenceAPI.GetChildrenAsync(page.Id);
					foreach (var childPage in pages)
					{
						Log.Debug().WriteLine("Adding page: {0}", childPage.Title);
						var pageTreeViewItem = new TreeViewItem
						{
							Header = childPage.Title,
							Tag = childPage
						};
						clickedItem.Items.Add(pageTreeViewItem);
						pageTreeViewItem.MouseUp += pageTreeViewItem_Click;
					}
					if (clickedItem.HasItems)
					{
						clickedItem.ExpandSubtree();
					}
				}, default(CancellationToken), TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
			}
			eventArgs.Handled = true;
		}

		private void Page_Loaded(object sender, RoutedEventArgs e)
		{
			_confluenceUpload.SelectedPage = null;
			foreach (var space in ConfluencePlugin.Instance.Model.Spaces.Values)
			{
				if (space.IsPersonal && !Config.IncludePersonSpaces)
				{
					continue;
				}

				var spaceTreeViewItem = new TreeViewItem
				{
					Header = space.Name, Tag = space
				};
				ConfluenceTreeView.Items.Add(spaceTreeViewItem);

				// Get homepage, in background
				// ReSharper disable once UnusedVariable
				var loadPageTask = Task.Factory.StartNew(async () =>
				{
					var page = await ConfluencePlugin.ConfluenceAPI.GetContentAsync(space.Id);
					if (page != null)
					{
						var pageTreeViewItem = new TreeViewItem
						{
							Header = page.Title,
							Tag = page
						};
						pageTreeViewItem.MouseUp += pageTreeViewItem_Click;
						spaceTreeViewItem.Items.Add(pageTreeViewItem);
					}
					else
					{
						spaceTreeViewItem.IsEnabled = false;
					}
				}, default(CancellationToken), TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
			}
		}
	}
}