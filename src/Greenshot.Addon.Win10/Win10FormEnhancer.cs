#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#if !NETCOREAPP3_0

using System;
using System.Drawing;
using System.Windows.Forms;
using Greenshot.Addons.Interfaces;
using Microsoft.Toolkit.Forms.UI.XamlHost;

namespace Greenshot.Addon.Win10
{
    public class Win10FormEnhancer : IFormEnhancer
    {
        private WindowsXamlHost inkCanvasHost;
        private WindowsXamlHost inkToolbarHost;
        private Windows.UI.Xaml.Controls.InkCanvas inkCanvas;
        private Windows.UI.Xaml.Controls.InkToolbar inkToolbar;

        public void InitializeComponent(Form target)
        {
            // TODO: Fix the code below, when the following is solved: https://github.com/windows-toolkit/Microsoft.Toolkit.Win32/issues/16#issuecomment-451337186
            return;
            // InkCanvas
            inkCanvasHost = new WindowsXamlHost();
            inkCanvasHost.InitialTypeName = "Windows.UI.Xaml.Controls.InkCanvas";
            inkCanvasHost.ChildChanged += InkCanvas_ChildChanged;
            // InkToolbar
            inkToolbarHost = new WindowsXamlHost();
            inkToolbarHost.InitialTypeName = "Windows.UI.Xaml.Controls.InkToolbar";
            inkToolbarHost.ChildChanged += InkToolbar_ChildChanged;
            // Layout
            inkToolbarHost.Top = 0;
            inkToolbarHost.Left = 0;
            inkToolbarHost.Height = 50;
            inkToolbarHost.Dock = DockStyle.Top;

            inkCanvasHost.Dock = DockStyle.Fill;
            inkCanvasHost.BackColor = Color.Transparent;

            // Add to Window
            target.Controls.Add(inkToolbarHost);
            target.Controls.Add(inkCanvasHost);
        }

        private void InkToolbar_ChildChanged(object sender, EventArgs e)
        {
            inkToolbar = ((WindowsXamlHost)sender).Child as Windows.UI.Xaml.Controls.InkToolbar;
            InitializeUwpControls();
        }

        private void InkCanvas_ChildChanged(object sender, EventArgs e)
        {
            inkCanvas = ((WindowsXamlHost)sender).Child as Windows.UI.Xaml.Controls.InkCanvas;
            InitializeUwpControls();
        }

        private void InitializeUwpControls()
        {
            if (inkToolbar == null || inkCanvas == null)
            {
                return;
            }

            inkToolbar.TargetInkCanvas = inkCanvas;
        }
    }
}
#endif