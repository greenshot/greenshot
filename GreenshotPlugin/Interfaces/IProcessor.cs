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

using System;

#endregion

namespace GreenshotPlugin.Interfaces
{
	/// <summary>
	///     Description of IProcessor.
	/// </summary>
	public interface IProcessor : IDisposable, IComparable
	{
		/// <summary>
		///     Simple "designation" like "FixTitle"
		/// </summary>
		string Designation { get; }

		/// <summary>
		///     Description which will be shown in the settings form, destination picker etc
		/// </summary>
		string Description { get; }

		/// <summary>
		///     Priority, used for sorting
		/// </summary>
		int Priority { get; }

		/// <summary>
		///     Returns if the destination is active
		/// </summary>
		bool isActive { get; }

		/// <summary>
		///     If a capture is made, and the destination is enabled, this method is called.
		/// </summary>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <returns>true if the processor has "processed" the capture</returns>
		bool ProcessCapture(ISurface surface, ICaptureDetails captureDetails);
	}
}