#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System.Drawing;
using System.Drawing.Drawing2D;

#endregion

namespace GreenshotPlugin.Effects
{
	/// <summary>
	///     Interface to describe an effect
	/// </summary>
	public interface IEffect
	{
		/// <summary>
		///     Apply this IEffect to the supplied sourceImage.
		///     In the process of applying the supplied matrix will be modified to represent the changes.
		/// </summary>
		/// <param name="sourceImage">Image to apply the effect to</param>
		/// <param name="matrix">
		///     Matrix with the modifications like rotate, translate etc. this can be used to calculate the new
		///     location of elements on a canvas
		/// </param>
		/// <returns>new image with applied effect</returns>
		Image Apply(Image sourceImage, Matrix matrix);

		/// <summary>
		///     Reset all values to their defaults
		/// </summary>
		void Reset();
	}
}