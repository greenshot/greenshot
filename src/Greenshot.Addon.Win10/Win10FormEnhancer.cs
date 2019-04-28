// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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
        private WindowsXamlHost _inkCanvasHost;
        private WindowsXamlHost _inkToolbarHost;
        private Windows.UI.Xaml.Controls.InkCanvas _inkCanvas;
        private Windows.UI.Xaml.Controls.InkToolbar _inkToolbar;

        public void InitializeComponent(Form target)
        {
            if (target is null)
            {
                return;
            }
            // InkCanvas
            _inkCanvasHost = new WindowsXamlHost
            {
                InitialTypeName = "Windows.UI.Xaml.Controls.InkCanvas",
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                
            };
            _inkCanvasHost.ChildChanged += InkCanvas_ChildChanged;

            // InkToolbar
            _inkToolbarHost = new WindowsXamlHost
            {
                InitialTypeName = "Windows.UI.Xaml.Controls.InkToolbar",
                // Layout
                Top = 0,
                Left = 0,
                Height = 50,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent
            };
            _inkToolbarHost.ChildChanged += InkToolbar_ChildChanged;

            // Add to Window
            target.Controls.Add(_inkToolbarHost);
            target.Controls.Add(_inkCanvasHost);
        }

        private void InkToolbar_ChildChanged(object sender, EventArgs e)
        {
            _inkToolbar = ((WindowsXamlHost)sender).Child as Windows.UI.Xaml.Controls.InkToolbar;
            InitializeUwpControls();
        }

        private void InkCanvas_ChildChanged(object sender, EventArgs e)
        {
            _inkCanvas = ((WindowsXamlHost)sender).Child as Windows.UI.Xaml.Controls.InkCanvas;
            InitializeUwpControls();
        }

        private void InitializeUwpControls()
        {
            if (_inkToolbar == null || _inkCanvas == null)
            {
                return;
            }

            _inkToolbar.TargetInkCanvas = _inkCanvas;
        }
    }
}
#endif