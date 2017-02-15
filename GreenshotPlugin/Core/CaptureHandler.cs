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

#endregion

namespace GreenshotPlugin.Core
{
	/// <summary>
	///     This is the method signature which is used to capture a rectangle from the screen.
	/// </summary>
	/// <param name="captureBounds"></param>
	/// <returns>Captured Bitmap</returns>
	public delegate Bitmap CaptureScreenRectangleHandler(Rectangle captureBounds);

	/// <summary>
	///     This is a hack to experiment with different screen capture routines
	/// </summary>
	public static class CaptureHandler
	{
		/// <summary>
		///     By changing this value, null is default
		/// </summary>
		public static CaptureScreenRectangleHandler CaptureScreenRectangle { get; set; }
	}
}