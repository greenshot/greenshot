// Dapplo - building blocks for desktop applications
// Copyright (C) 2019 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot-Full
// 
// Greenshot-Full is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot-Full is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot-Full. If not, see <http://www.gnu.org/licenses/lgpl.txt>.
// 
namespace Greenshot.Addon.LegacyEditor.Drawing
{
    /// <summary>
    /// Defines the filter to use
    /// </summary>
    public enum PreparedFilter
    {
        /// <summary>
        /// Use the blur filter
        /// </summary>
        Blur,
        /// <summary>
        /// Use the pixelize filter
        /// </summary>
        Pixelize,
        /// <summary>
        /// Use the text highlight filter
        /// </summary>
        TextHightlight,
        /// <summary>
        /// Use the area highlight filter
        /// </summary>
        AreaHighlight,
        /// <summary>
        /// Use the gray-scale filter
        /// </summary>
        Grayscale,
        /// <summary>
        /// Use the magnification filter
        /// </summary>
        Magnification
    }
}