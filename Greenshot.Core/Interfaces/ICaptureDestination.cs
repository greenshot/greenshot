//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Threading;
using System.Threading.Tasks;

namespace Greenshot.Core.Interfaces
{
	/// <summary>
	/// This interface defines the capture destination, which takes care of exporting a capture.
	/// </summary>
	public interface ICaptureDestination : ICaptureModule
	{
		/// <summary>
		/// Export the capture, as specified in the capture flow
		/// </summary>
		/// <param name="captureFlow">ICaptureFlow which contains a context for the export</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Task</returns>
		Task ExportCaptureAsync(ICaptureFlow captureFlow, CancellationToken cancellationToken = default(CancellationToken));
	}
}
