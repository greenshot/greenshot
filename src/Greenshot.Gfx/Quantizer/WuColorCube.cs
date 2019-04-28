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

namespace Greenshot.Gfx.Quantizer
{
	internal class WuColorCube
	{
		/// <summary>
		///     Gets or sets the red minimum.
		/// </summary>
		/// <value>The red minimum.</value>
		public int RedMinimum { get; set; }

		/// <summary>
		///     Gets or sets the red maximum.
		/// </summary>
		/// <value>The red maximum.</value>
		public int RedMaximum { get; set; }

		/// <summary>
		///     Gets or sets the green minimum.
		/// </summary>
		/// <value>The green minimum.</value>
		public int GreenMinimum { get; set; }

		/// <summary>
		///     Gets or sets the green maximum.
		/// </summary>
		/// <value>The green maximum.</value>
		public int GreenMaximum { get; set; }

		/// <summary>
		///     Gets or sets the blue minimum.
		/// </summary>
		/// <value>The blue minimum.</value>
		public int BlueMinimum { get; set; }

		/// <summary>
		///     Gets or sets the blue maximum.
		/// </summary>
		/// <value>The blue maximum.</value>
		public int BlueMaximum { get; set; }

		/// <summary>
		///     Gets or sets the cube volume.
		/// </summary>
		/// <value>The volume.</value>
		public int Volume { get; set; }
	}
}