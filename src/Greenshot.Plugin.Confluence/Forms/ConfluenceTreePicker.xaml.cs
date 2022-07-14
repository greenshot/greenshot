﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Greenshot.Plugin.Confluence.Entities;
using Page = Greenshot.Plugin.Confluence.Entities.Page;

namespace Greenshot.Plugin.Confluence.Forms
{
    /// <summary>
    /// Interaction logic for ConfluenceTreePicker.xaml
    /// </summary>
    public partial class ConfluenceTreePicker
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ConfluenceTreePicker));
        private readonly ConfluenceConnector _confluenceConnector;
        private readonly ConfluenceUpload _confluenceUpload;
        private bool _isInitDone;

        public ConfluenceTreePicker(ConfluenceUpload confluenceUpload)
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
            new Thread(() =>
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)(() => { ShowBusy.Visibility = Visibility.Visible; }));
                var pages = _confluenceConnector.GetPageChildren(page).OrderBy(p => p.Title);
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)(() =>
                {
                    foreach (var childPage in pages)
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

                    ShowBusy.Visibility = Visibility.Collapsed;
                }));
            })
            {
                Name = "Loading childpages for confluence page " + page.Title
            }.Start();
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

            ShowBusy.Visibility = Visibility.Visible;
            new Thread(() =>
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)(() =>
                {
                    foreach (Space space in _confluenceUpload.Spaces)
                    {
                        TreeViewItem spaceTreeViewItem = new()
                        {
                            Header = space.Name,
                            Tag = space
                        };

                        // Get homepage
                        try
                        {
                            Page page = _confluenceConnector.GetSpaceHomepage(space);
                            TreeViewItem pageTreeViewItem = new()
                            {
                                Header = page.Title,
                                Tag = page
                            };
                            pageTreeViewItem.PreviewMouseDoubleClick += PageTreeViewItem_DoubleClick;
                            pageTreeViewItem.PreviewMouseLeftButtonDown += PageTreeViewItem_Click;
                            spaceTreeViewItem.Items.Add(pageTreeViewItem);
                            ConfluenceTreeView.Items.Add(spaceTreeViewItem);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Can't get homepage for space : " + space.Name + " (" + ex.Message + ")");
                        }
                    }

                    ShowBusy.Visibility = Visibility.Collapsed;
                    _isInitDone = true;
                }));
            })
            {
                Name = "Loading spaces for confluence"
            }.Start();
        }
    }
}