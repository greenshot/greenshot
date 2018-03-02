#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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

#endregion

using System;
using System.ComponentModel.Composition;
using Dapplo.Addons;

namespace GreenshotPlugin.Addons
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class DestinationAttribute : ModuleAttribute, IDestinationMetadata
    {
        /// <summary>
        ///     Default constructor
        /// </summary>
        public DestinationAttribute() : base(typeof(IDestination))
        {
        }

        /// <summary>
        ///     Use a specific contract name for the Destination
        /// </summary>
        /// <param name="designation">string</param>
        public DestinationAttribute(string designation) : base(typeof(IDestination))
        {
            Designation = designation;
        }

        /// <summary>
        ///     Use a specific contract name for the Destination
        /// </summary>
        /// <param name="designation">string</param>
        /// <param name="priority">int</param>
        public DestinationAttribute(string designation, int priority) : base(typeof(IDestination))
        {
            Designation = designation;
            Priority = priority;
        }

        public string Designation { get; set; }
        public int Priority { get; set; } = 10;
    }
}
