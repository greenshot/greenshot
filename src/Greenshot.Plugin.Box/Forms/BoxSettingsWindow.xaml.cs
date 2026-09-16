/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using System.Windows;
using System.Windows.Input;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;

namespace Greenshot.Plugin.Box.Forms;

public partial class BoxSettingsWindow : Window
{
    private readonly IBoxConfiguration _config;
    private readonly OutputFormat _initialFormat;
    private readonly bool _initialAfterUpload;

    public BoxSettingsWindow(IBoxConfiguration config)
    {
        _config = config;
        _initialFormat = config.UploadFormat;
        _initialAfterUpload = config.AfterUploadLinkToClipBoard;
        DataContext = config;
        InitializeComponent();

        try
        {
            Icon = GreenshotResources.GetGreenshotIcon()?.ToBitmapSource();
        }
        catch
        {
            // Ignore in headless/test environments
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Button_OK_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (DialogResult != true)
        {
            _config.UploadFormat = _initialFormat;
            _config.AfterUploadLinkToClipBoard = _initialAfterUpload;
        }
    }
}
