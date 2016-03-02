/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016  Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.ServiceModel;

namespace Greenshot.Addon.Interfaces
{
	/// <summary>
	/// This interface specifies the interface to Greenshot 1.3+, and is used by Greenshot itself but can also be used from the PowerShell
	/// </summary>
	[ServiceContract]
	public interface IGreenshotContract
	{
		/// <summary>
		/// Exit Greenshot
		/// </summary>
		[OperationContract]
		void Exit();

		/// <summary>
		/// Open the specified file
		/// </summary>
		/// <param name="filename"></param>
		[OperationContract]
		void OpenFile(string filename);
	}
}