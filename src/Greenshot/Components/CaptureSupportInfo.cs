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

using System.Collections.Generic;
using Greenshot.Addon.InternetExplorer;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;

namespace Greenshot.Components
{
    /// <summary>
    /// This is the bundled information which is needed for making captures possible.
    /// </summary>
    public class CaptureSupportInfo
    {
        /// <summary>
        /// Constructor for DI
        /// </summary>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="internetExplorerCaptureHelper">InternetExplorerCaptureHelper</param>
        /// <param name="formEnhancers">IEnumerable with IFormEnhancer</param>
        public CaptureSupportInfo(
            ICoreConfiguration coreConfiguration, 
            InternetExplorerCaptureHelper internetExplorerCaptureHelper,
            IEnumerable<IFormEnhancer> formEnhancers = null
            )
        {
            CoreConfiguration = coreConfiguration;
            InternetExplorerCaptureHelper = internetExplorerCaptureHelper;
            FormEnhancers = formEnhancers;
        }

        public ICoreConfiguration CoreConfiguration { get; }
        public InternetExplorerCaptureHelper InternetExplorerCaptureHelper { get; }

        public IEnumerable<IFormEnhancer> FormEnhancers { get; }
    }
}
