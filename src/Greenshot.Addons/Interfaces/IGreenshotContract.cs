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

#if !NETCOREAPP3_0

using System.ServiceModel;

namespace Greenshot.Addons.Interfaces
{
    /// <summary>
    /// This interface specifies the functional interface that Greenshot provides
    /// </summary>
    [ServiceContract]
    public interface IGreenshotContract
    {
        /// <summary>
        /// Start a capture
        /// </summary>
        /// <param name="parameters"></param>
        [OperationContract]
        void Capture(string parameters);

        /// <summary>
        /// Exit the instance
        /// </summary>
        [OperationContract]
        void Exit();

        /// <summary>
        /// Reload configuration
        /// </summary>
        [OperationContract]
        void ReloadConfig();

        /// <summary>
        /// Open a file
        /// </summary>
        /// <param name="filename"></param>
        [OperationContract]
        void OpenFile(string filename);
    }
}

#endif