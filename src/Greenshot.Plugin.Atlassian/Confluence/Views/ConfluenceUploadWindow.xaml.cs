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

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Greenshot.Plugin.Atlassian.Confluence.Api.Entities;
using Greenshot.Plugin.Atlassian.Confluence.Destinations;

namespace Greenshot.Plugin.Atlassian.Confluence.Views;

/// <summary>
/// Interaction logic for ConfluenceUploadWindow.xaml
/// </summary>
public partial class ConfluenceUploadWindow
{
    private readonly ConfluenceUploadRequest _request;

    /// <summary>
    /// The view of a <see cref="ConfluenceUploadRequest"/>: shows the dialog modally (on the UI thread).
    /// </summary>
    /// <returns>the choice of the user, null when canceled</returns>
    public static ConfluenceUploadChoice Show(ConfluenceUploadRequest request)
    {
        var confluenceUpload = new ConfluenceUploadWindow(request);
        if (confluenceUpload.ShowDialog() != true || confluenceUpload.SelectedPage == null)
        {
            return null;
        }

        return new ConfluenceUploadChoice(confluenceUpload.SelectedPage, confluenceUpload.Filename);
    }

    private System.Windows.Controls.Page _searchPage;

    public System.Windows.Controls.Page SearchPage
    {
        get { return _searchPage ??= new ConfluenceSearchPage(this); }
    }

    private System.Windows.Controls.Page _browsePage;

    public System.Windows.Controls.Page BrowsePage
    {
        get { return _browsePage ??= new ConfluenceTreePickerPage(this); }
    }

    private Page _selectedPage;

    public Page SelectedPage
    {
        get => _selectedPage;
        set
        {
            _selectedPage = value;
            Upload.IsEnabled = _selectedPage != null;
        }
    }

    public string Filename { get; set; }

    /// <summary>
    /// The spaces, loaded before the dialog opened
    /// </summary>
    public IList<Space> Spaces => _request.Spaces;

    public ConfluenceUploadWindow(ConfluenceUploadRequest request)
    {
        _request = request;
        Filename = request.Filename;
        InitializeComponent();
        DataContext = this;
    }

    private void Upload_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}