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

using Greenshot.Addons.Interfaces;

namespace Greenshot.Addons.Core
{
    /// <summary>
    /// This implements a basic IProcessor
    /// </summary>
    public abstract class AbstractProcessor : IProcessor
	{
        /// <inheritdoc />
        public virtual int CompareTo(object obj)
		{
            if (!(obj is IProcessor other))
            {
                return 1;
            }
            if (Priority == other.Priority)
			{
				return Description.CompareTo(other.Description);
			}
			return Priority - other.Priority;
		}

        /// <inheritdoc />
        public abstract string Designation { get; }

        /// <inheritdoc />
        public abstract string Description { get; }

        /// <inheritdoc />
        public virtual int Priority => 10;

        /// <inheritdoc />
        public void Dispose()
		{
			Dispose(true);
		}

        /// <inheritdoc />
        public virtual bool IsActive => true;

        /// <inheritdoc />
        public abstract bool ProcessCapture(ISurface surface, ICaptureDetails captureDetails);

		/// <summary>
		/// Override this to have dispose functionality
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
			//if (disposing) {}
		}
	}
}