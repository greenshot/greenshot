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

namespace Greenshot.Core.Enums
{
    /// <summary>
    /// The output formats we support
    /// </summary>
	public enum OutputFormats
	{
        /// <summary>
        /// Specify bmp to write bitmap files
        /// </summary>
		bmp,
        /// <summary>
        /// Specify gif to write gif files
        /// </summary>
        gif,
        /// <summary>
        /// Specify jpg to write bitjpgmap files
        /// </summary>
        jpg,
        /// <summary>
        /// Specify png to write png files
        /// </summary>
        png,
        /// <summary>
        /// Specify tiff to write tiff files
        /// </summary>
        tiff,
        /// <summary>
        /// Specify greenshot to write greenshot files with annotations and a PNG bitmap
        /// </summary>
        greenshot,
        /// <summary>
        /// Specify ico to write icon files
        /// </summary>
        ico
    }
}