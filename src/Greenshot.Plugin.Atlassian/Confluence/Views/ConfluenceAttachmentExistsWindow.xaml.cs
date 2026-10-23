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

using System.Windows;
using Greenshot.Base.Languages;
using Greenshot.Plugin.Atlassian.Confluence.Destinations;

namespace Greenshot.Plugin.Atlassian.Confluence.Views;

/// <summary>
/// The view of a <see cref="ConfluenceAttachmentExistsRequest"/>: a new version of the attachment or another file name
/// </summary>
public partial class ConfluenceAttachmentExistsWindow : Window
{
    public ConfluenceAttachmentExistsWindow(ConfluenceAttachmentExistsRequest request)
    {
        InitializeComponent();
        MessageText.Text = string.Format(Texts.Get<IConfluenceLanguage>().AttachmentExistsMessage, request.PageTitle, request.Filename);
        OtherNameBox.Text = request.OtherName;
        Loaded += (_, _) =>
        {
            Activate();
            NewVersionButton.Focus();
        };
    }

    /// <summary>
    /// Show the window modally (on the UI thread)
    /// </summary>
    /// <returns>the choice of the user, null when canceled</returns>
    public static ConfluenceAttachmentExistsChoice Show(ConfluenceAttachmentExistsRequest request)
    {
        var window = new ConfluenceAttachmentExistsWindow(request);
        if (window.ShowDialog() != true)
        {
            return null;
        }

        return window.NewVersionButton.IsChecked == true
            ? new ConfluenceAttachmentExistsChoice(true, request.Filename)
            : new ConfluenceAttachmentExistsChoice(false, window.OtherNameBox.Text.Trim());
    }

    private void OtherNameButton_Checked(object sender, RoutedEventArgs e)
    {
        // The name can be typed right away
        OtherNameBox.Focus();
        OtherNameBox.SelectAll();
    }

    private void Upload_Click(object sender, RoutedEventArgs e)
    {
        if (OtherNameButton.IsChecked == true && string.IsNullOrWhiteSpace(OtherNameBox.Text))
        {
            OtherNameBox.Focus();
            return;
        }

        DialogResult = true;
    }
}
