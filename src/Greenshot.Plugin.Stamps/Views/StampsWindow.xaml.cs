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
using Greenshot.Plugin.Stamps.ViewModels;

namespace Greenshot.Plugin.Stamps.Views;

/// <summary>
/// The stamps in a window of their own, without an editor (from the tray menu or the hotkey): to give stamps to other programs
/// with the clipboard or by dragging. In the editor they are a panel, see <see cref="StampsPanel"/>.
/// </summary>
public partial class StampsWindow
{
    internal StampsWindow(StampsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Stamps.CloseRequested += (s, e) => Close();
        Loaded += (s, e) => Stamps.FocusSearch();
    }

    /// <summary>
    /// Bring the window to the front with the keyboard in the search
    /// </summary>
    internal void ShowAndFocus()
    {
        if (!IsVisible)
        {
            Show();
        }

        if (WindowState == System.Windows.WindowState.Minimized)
        {
            WindowState = System.Windows.WindowState.Normal;
        }

        Activate();
        Stamps.FocusSearch();
    }
}
