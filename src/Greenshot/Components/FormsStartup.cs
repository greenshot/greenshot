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

using System.Windows.Forms;
using Dapplo.Addons;
using Dapplo.Log;
using System.Windows.Forms.Integration;
using Greenshot.Addons.Core;

namespace Greenshot.Components
{
    /// <summary>
    /// This startup action starts Windows.Forms
    /// </summary>
    [Service(nameof(FormsStartup))]
    public class FormsStartup : IStartup
    {
        private static readonly LogSource Log = new LogSource();

        /// <inheritdoc />
        public void Startup()
        {
            Log.Debug().WriteLine("Starting Windows.Forms");

            // Make sure we can use forms
            WindowsFormsHost.EnableWindowsFormsInterop();
            // Other small fixes
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // BUG-1809: Add message filter, to filter out all the InputLangChanged messages which go to a target control with a handle > 32 bit.
            Application.AddMessageFilter(new WmInputLangChangeRequestFilter());
        }
    }
}
