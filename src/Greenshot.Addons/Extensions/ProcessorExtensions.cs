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

using System.Collections.Generic;
using System.Linq;
using Greenshot.Addons.Interfaces;

namespace Greenshot.Addons.Extensions
{
    /// <summary>
    /// Extensions for the IProcessor
    /// </summary>
    public static class ProcessorExtensions
    {
        /// <summary>
        /// Find the matching IProcessor
        /// </summary>
        /// <param name="processors">IEnumerable of IProcessor</param>
        /// <param name="designation">Name</param>
        /// <returns>IProcessor or null</returns>
        public static IProcessor Find(this IEnumerable<IProcessor> processors, string designation)
        {
            return processors.FirstOrDefault(p => p.Designation == designation && p.IsActive);
        }

        /// <summary>
        ///     A simple helper method which will call ProcessCapture for the Processor
        /// </summary>
        /// <param name="processor">The processor</param>
        /// <param name="surface"></param>
        /// <param name="captureDetails"></param>
        public static void ProcessCapture(this IProcessor processor, ISurface surface, ICaptureDetails captureDetails)
        {
            processor?.ProcessCapture(surface, captureDetails);
        }
    }
}
