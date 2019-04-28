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

using System.ComponentModel;
using Dapplo.CaliburnMicro.Metro.Configuration;
using Dapplo.Config.Ini;

namespace Greenshot.Configuration
{
    /// <summary>
    /// This specifies the configuration for MahApps.Metro
    /// </summary>
    [IniSection("Metro")]
    public interface IMetroConfiguration : IIniSection, IMetroUiConfiguration
    {
        /// <inheritdoc />
        [DefaultValue("Light")]
        new string Theme { get; set; }

        /// <inheritdoc />
        [DefaultValue("Olive")]
        new string ThemeColor{ get; set; }
    }
}
