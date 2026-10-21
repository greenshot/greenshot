/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Greenshot.Base.Threading;
using Greenshot.Plugin.Confluence.Api;
using Greenshot.Plugin.Confluence.Api.Entities;
using Page = Greenshot.Plugin.Confluence.Api.Entities.Page;

namespace Greenshot.Plugin.Confluence.Views;

/// <summary>
/// Interaction logic for ConfluenceTreePickerPage.xaml
/// </summary>
public partial class ConfluenceTreePickerPage
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ConfluenceTreePickerPage));
    private readonly ConfluenceConnector _confluenceConnector;
    private readonly ConfluenceUploadWindow _confluenceUpload;
    private bool _isInitDone;

    public ConfluenceTreePickerPage(ConfluenceUploadWindow confluenceUpload)
    {
        _confluenceConnector = ConfluencePlugin.ConfluenceConnector;
        _confluenceUpload = confluenceUpload;
        InitializeComponent();
    }

    private void PageTreeViewItem_DoubleClick(object sender, MouseButtonEventArgs eventArgs)
    {
        Log.Debug("spaceTreeViewItem_MouseLeftButtonDown is called!");
        TreeViewItem clickedItem = eventArgs.Source as TreeViewItem;
        if (clickedItem?.Tag is not Page page)
        {
            return;
        }

        if (clickedItem.HasItems)
        {
            return;
        }

        Log.Debug("Loading pages for page: " + page.Title);
        AsyncCommand.Run(() => LoadChildPagesAsync(clickedItem, page), "Loading childpages for confluence page " + page.Title);
    }

    /// <summary>
    /// Load the child pages, the continuations run on the UI thread
    /// </summary>
    private async Task LoadChildPagesAsync(TreeViewItem clickedItem, Page page)
    {
        ShowBusy.Visibility = Visibility.Visible;
        try
        {
            var pages = await _confluenceConnector.GetPageChildrenAsync(page);
            foreach (var childPage in pages.OrderBy(p => p.Title))
            {
                Log.Debug("Adding page: " + childPage.Title);
                var pageTreeViewItem = new TreeViewItem
                {
                    Header = childPage.Title,
                    Tag = childPage
                };
                clickedItem.Items.Add(pageTreeViewItem);
                pageTreeViewItem.PreviewMouseDoubleClick += PageTreeViewItem_DoubleClick;
                pageTreeViewItem.PreviewMouseLeftButtonDown += PageTreeViewItem_Click;
            }
        }
        finally
        {
            ShowBusy.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// A page selected with the keyboard is the target too, like a clicked one
    /// </summary>
    private void ConfluenceTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        // A space is no target: without this the page selected before would stay the target, while the space is shown selected
        _confluenceUpload.SelectedPage = (e.NewValue as TreeViewItem)?.Tag as Page;
    }

    /// <summary>
    /// Right arrow (expand) loads the child pages of the selected page, like the double click; Enter stays the upload
    /// </summary>
    private void ConfluenceTreeView_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Right or Key.Add) || ConfluenceTreeView.SelectedItem is not TreeViewItem { Tag: Page page } selectedItem || selectedItem.HasItems)
        {
            return;
        }

        e.Handled = true;
        AsyncCommand.Run(() => LoadChildPagesAsync(selectedItem, page), "Loading childpages for confluence page " + page.Title);
    }

    private void PageTreeViewItem_Click(object sender, MouseButtonEventArgs eventArgs)
    {
        Log.Debug("pageTreeViewItem_PreviewMouseDoubleClick is called!");
        if (eventArgs.Source is not TreeViewItem clickedItem)
        {
            return;
        }

        Page page = clickedItem.Tag as Page;
        _confluenceUpload.SelectedPage = page;
        if (page != null)
        {
            Log.Debug("Page selected: " + page.Title);
        }
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _confluenceUpload.SelectedPage = null;
        if (_isInitDone)
        {
            return;
        }

        _isInitDone = true;
        AsyncCommand.Run(LoadSpacesAsync, "Loading spaces for confluence");
    }

    /// <summary>
    /// Add the spaces with their homepage, the continuations run on the UI thread
    /// </summary>
    private async Task LoadSpacesAsync()
    {
        ShowBusy.Visibility = Visibility.Visible;
        try
        {
            foreach (Space space in _confluenceUpload.Spaces)
            {
                TreeViewItem spaceTreeViewItem = new TreeViewItem
                {
                    Header = space.Name,
                    Tag = space
                };

                // Get homepage
                try
                {
                    Page page = await _confluenceConnector.GetSpaceHomepageAsync(space);
                    if (page != null)
                    {
                        TreeViewItem pageTreeViewItem = new TreeViewItem
                        {
                            Header = page.Title,
                            Tag = page
                        };
                        pageTreeViewItem.PreviewMouseDoubleClick += PageTreeViewItem_DoubleClick;
                        pageTreeViewItem.PreviewMouseLeftButtonDown += PageTreeViewItem_Click;
                        spaceTreeViewItem.Items.Add(pageTreeViewItem);
                    }

                    ConfluenceTreeView.Items.Add(spaceTreeViewItem);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Log.Error("Can't get homepage for space : " + space.Name + " (" + ex.Message + ")");
                }
            }
        }
        finally
        {
            ShowBusy.Visibility = Visibility.Collapsed;
        }
    }
}
