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

#region Usings

using System;
using System.ComponentModel.Composition;

#endregion

namespace Greenshot.Addon.Interfaces.Destination
{
	[MetadataAttribute]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false)]
	public sealed class DestinationAttribute : InheritedExportAttribute, IDestinationMetadata
	{
		public DestinationAttribute(string name, int order = int.MaxValue) : base(typeof(IDestination))
		{
			Name = name;
			Order = order;
		}

		public string Name { get; set; }

		/// <summary>
		///     Order of the destination when shown in a list
		/// </summary>
		public int Order { get; }
	}
}